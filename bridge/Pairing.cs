using System.Security.Cryptography;
using System.Text;
namespace MotherSys;
public record Grant(string Origin, string KeyId, string PublicKey);
public interface IGrants { bool Contains(Grant grant); bool Blocked(Grant grant); void Allow(Grant grant); }
public record Challenge(string Id, string Nonce, string Session, string KeyId, string Origin, DateTimeOffset Expires);
public record Proof(string ChallengeId, string PublicKey, string Signature);
public sealed class Pairing {
 readonly TimeProvider clock; readonly object gate = new(); readonly IGrants grants;
 readonly Func<Grant,Task<bool>> consent;
 readonly Dictionary<string,Challenge> challenges = new();
 readonly Dictionary<string,(Grant grant,DateTimeOffset expires)> sessions = new();
 readonly HashSet<string> pending = new();
 readonly string session = Guid.NewGuid().ToString();
 int epoch;
 public Pairing(IGrants grants, Func<Grant,Task<bool>> consent,TimeProvider? clock=null) { this.grants=grants; this.consent=consent; this.clock=clock??TimeProvider.System; }
 static string Random() => Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant();
 public static string KeyId(byte[] spki) => Convert.ToHexString(SHA256.HashData(spki)).ToLowerInvariant();
 public static string Message(Challenge c) => $"mothersys.pairing.v1\n{c.Origin}\n{c.KeyId}\n{c.Session}\n{c.Id}\n{c.Nonce}";
 void Prune() { var now=clock.GetUtcNow(); foreach(var k in challenges.Where(x=>x.Value.Expires<now).Select(x=>x.Key).ToArray()) challenges.Remove(k); foreach(var k in sessions.Where(x=>x.Value.expires<now).Select(x=>x.Key).ToArray())sessions.Remove(k); }
 public Challenge? Issue(string origin,string keyId) { lock(gate) { Prune(); if(keyId.Length!=64||keyId.Any(c=>!char.IsAsciiHexDigit(c))||challenges.Count>=64)return null;var c=new Challenge(Random(),Random(),session,keyId,origin,clock.GetUtcNow().AddSeconds(30));challenges[c.Id]=c;return c; } }
 public async Task<(int status,string? token)> Authenticate(string origin,Proof proof) {
 Challenge c; int generation;
 lock(gate) { Prune(); if(!challenges.Remove(proof.ChallengeId,out c!)||c.Origin!=origin)return (401,null);generation=epoch; }
 Grant grant;
 try { var spki=Convert.FromBase64String(proof.PublicKey); if(spki.Length>256||KeyId(spki)!=c.KeyId)return(401,null);
 using var ec=ECDsa.Create();ec.ImportSubjectPublicKeyInfo(spki,out var consumed);var pub=ec.ExportParameters(false);
 if(consumed!=spki.Length||ec.KeySize!=256||pub.Curve.Oid.Value!="1.2.840.10045.3.1.7"||!ec.VerifyData(Encoding.UTF8.GetBytes(Message(c)),Convert.FromBase64String(proof.Signature),HashAlgorithmName.SHA256,DSASignatureFormat.IeeeP1363FixedFieldConcatenation))return(401,null);
 grant=new Grant(origin,c.KeyId,proof.PublicKey);
 } catch(Exception e) when(e is CryptographicException or FormatException or ArgumentException){return(401,null);}
 bool known;lock(gate) { if(grants.Blocked(grant))return(403,null);known=grants.Contains(grant);if(!known&&!pending.Add(origin))return(429,null); }
 if(!known) {
 bool allowed=false;try { allowed=await consent(grant); } finally { lock(gate)pending.Remove(origin); }
 lock(gate) { if(!allowed||generation!=epoch)return(403,null);grants.Allow(grant); }
 }
 lock(gate) { Prune();if(generation!=epoch||!grants.Contains(grant)||sessions.Count>=64)return(403,null);var token=Random();sessions[token]=(grant,clock.GetUtcNow().AddSeconds(60));return(200,token); }
 }
 public bool Authorized(string origin,string bearer) { lock(gate) { Prune();return bearer.StartsWith("Bearer ",StringComparison.Ordinal)&&sessions.TryGetValue(bearer[7..],out var s)&&s.grant.Origin==origin&&grants.Contains(s.grant); } }
 public void RevokeSessions() { lock(gate) { epoch++;sessions.Clear();challenges.Clear(); } }
}
