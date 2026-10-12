using MotherSys;using System.Security.Cryptography;using System.Text;using System.Text.Json;
var grants=new MemoryGrants();var prompts=0;var permit=true;
var pairing=new Pairing(grants,g=>{prompts++;return Task.FromResult(permit);});
const string origin="https://8a6d3033.mothersys-classic-web.pages.dev";
void Check(bool success,string label){if(!success)throw new Exception(label);Console.WriteLine("PASS "+label);}
using var key=ECDsa.Create(ECCurve.NamedCurves.nistP256);var spki=key.ExportSubjectPublicKeyInfo();var keyId=Pairing.KeyId(spki);
Proof Sign(Challenge c)=>new(c.Id,Convert.ToBase64String(spki),Convert.ToBase64String(key.SignData(Encoding.UTF8.GetBytes(Pairing.Message(c)),HashAlgorithmName.SHA256,DSASignatureFormat.IeeeP1363FixedFieldConcatenation)));
var challenge=pairing.Issue(origin,keyId)!;var proof=Sign(challenge);var first=await pairing.Authenticate(origin,proof);
Check(first.status==200&&prompts==1,"first key requires explicit consent callback");Check(pairing.Authorized(origin,"Bearer "+first.token),"granted session");Check(!pairing.Authorized("https://evil.example","Bearer "+first.token),"token bound to exact origin");
Check((await pairing.Authenticate(origin,proof)).status==401,"challenge replay rejected");
var again=await pairing.Authenticate(origin,Sign(pairing.Issue(origin,keyId)!));Check(again.status==200&&prompts==1,"known browser reconnect needs proof, no repeat prompt");
var wrong=pairing.Issue(origin,keyId)!;Check((await pairing.Authenticate("https://evil.example",Sign(wrong))).status==401,"origin substitution rejected");
var tamper=Sign(pairing.Issue(origin,keyId)!);Check((await pairing.Authenticate(origin,tamper with {Signature=Convert.ToBase64String(new byte[64])})).status==401,"invalid signature rejected");
pairing.RevokeSessions();grants.Revoke();Check(!pairing.Authorized(origin,"Bearer "+first.token),"revocation terminates all sessions");Check((await pairing.Authenticate(origin,Sign(pairing.Issue(origin,keyId)!))).status==403&&prompts==1,"revoked key cannot silently reauthorize");
grants.Reset();permit=false;Check((await pairing.Authenticate(origin,Sign(pairing.Issue(origin,keyId)!))).status==403,"denied native consent never grants");
Check(!AccessPolicy.OriginAllowed("http://mothersys-classic-web.pages.dev")&&!AccessPolicy.OriginAllowed("https://mothersys-classic-web.pages.dev.evil.example")&&!AccessPolicy.OriginAllowed("null")&&!AccessPolicy.OriginAllowed(origin+"/")&&!AccessPolicy.OriginAllowed(origin+":444"),"strict HTTPS exact origin");
Check(AccessPolicy.OriginAllowed(origin),"actual Cloudflare origin eligible for native pairing, not automatically granted");
Check(!AccessPolicy.OriginAllowed("https://0185b7b7.mothersys-classic-web.pages.dev")&&!AccessPolicy.OriginAllowed("https://mothersys-classic-web.pages.dev")&&!AccessPolicy.OriginAllowed("https://preview-r1-2-windows-qa.mothersys-classic-web.pages.dev"),"R1.1, production and alias rejected");
var bounded=new Pairing(new MemoryGrants(),_=>Task.FromResult(false));for(var i=0;i<64;i++)Check(bounded.Issue(origin,keyId)!=null,"bounded challenge "+i);Check(bounded.Issue(origin,keyId)==null,"challenge capacity capped");
var clock=new TestClock();var expiryGrants=new MemoryGrants();var expiring=new Pairing(expiryGrants,_=>Task.FromResult(true),clock);var expiredChallenge=expiring.Issue(origin,keyId)!;clock.Advance(31);Check((await expiring.Authenticate(origin,Sign(expiredChallenge))).status==401,"challenge expires after 30 seconds");var expiringSession=await expiring.Authenticate(origin,Sign(expiring.Issue(origin,keyId)!));clock.Advance(61);Check(!expiring.Authorized(origin,"Bearer "+expiringSession.token),"bearer expires after 60 seconds");
var approval=new TaskCompletionSource<bool>();var pendingPairing=new Pairing(new MemoryGrants(),_=>approval.Task);var pendingProof=Sign(pendingPairing.Issue(origin,keyId)!);var pendingAuth=pendingPairing.Authenticate(origin,pendingProof);pendingPairing.RevokeSessions();approval.SetResult(true);Check((await pendingAuth).status==403,"revocation invalidates an in-flight native approval");
Console.WriteLine("PAIRING SECURITY PASS; native Windows consent/DPAPI remain NOT RUN");
sealed class MemoryGrants:IGrants {readonly HashSet<Grant> grants=[];readonly HashSet<Grant> blocked=[];public bool Contains(Grant g)=>grants.Contains(g);public bool Blocked(Grant g)=>blocked.Contains(g);public void Allow(Grant g)=>grants.Add(g);public void Revoke(){blocked.UnionWith(grants);grants.Clear();}public void Reset(){grants.Clear();blocked.Clear();}}

sealed class TestClock:TimeProvider {DateTimeOffset now=DateTimeOffset.UtcNow;public override DateTimeOffset GetUtcNow()=>now;public void Advance(int seconds)=>now=now.AddSeconds(seconds);}
