namespace MotherSys;
public static class AccessPolicy {
 public static bool OriginAllowed(string origin) {
  if(!Uri.TryCreate(origin,UriKind.Absolute,out var u)||u.Scheme!="https"||u.Port!=443||u.UserInfo.Length!=0||u.GetLeftPart(UriPartial.Authority)!=origin)return false;
  return string.Equals(origin,"https://8a6d3033.mothersys-classic-web.pages.dev",StringComparison.Ordinal);
 }
 public static async Task Invoke(HttpContext ctx,Func<Task> next,Pairing pairing,Func<string,bool>? allowed=null) {
  ctx.Response.Headers.CacheControl="no-store";ctx.Response.Headers["X-Content-Type-Options"]="nosniff";
  var origin=ctx.Request.Headers.Origin.ToString();
  if(ctx.Connection.RemoteIpAddress is not { } ip||!System.Net.IPAddress.IsLoopback(ip)||ctx.Request.Host.Value!="127.0.0.1:47631"||!(allowed??OriginAllowed)(origin)){ctx.Response.StatusCode=403;return;}
  ctx.Response.Headers["Access-Control-Allow-Origin"]=origin;ctx.Response.Headers.Vary="Origin";
  var path=ctx.Request.Path.Value;var method=path=="/v1/session"?"POST":"GET";
  if(path is not ("/v1/challenge" or "/v1/session" or "/v1/telemetry")){ctx.Response.StatusCode=404;return;}
  if(ctx.Request.Method=="OPTIONS") {
   if(ctx.Request.Headers["Access-Control-Request-Method"]!=method||ctx.Request.Headers["Access-Control-Request-Headers"].ToString().Split(',',StringSplitOptions.TrimEntries|StringSplitOptions.RemoveEmptyEntries).Any(h=>!h.Equals("authorization",StringComparison.OrdinalIgnoreCase)&&!h.Equals("content-type",StringComparison.OrdinalIgnoreCase))){ctx.Response.StatusCode=403;return;}
   ctx.Response.Headers["Access-Control-Allow-Methods"]=method;ctx.Response.Headers["Access-Control-Allow-Headers"]="Authorization, Content-Type";ctx.Response.Headers["Access-Control-Allow-Private-Network"]="true";ctx.Response.StatusCode=204;return;
  }
  if(ctx.Request.Method!=method){ctx.Response.StatusCode=405;return;}
  if(path=="/v1/telemetry"&&!pairing.Authorized(origin,ctx.Request.Headers.Authorization.ToString())){ctx.Response.StatusCode=401;return;}
  await next();
 }
 public static void Map(WebApplication app,Pairing pairing,Func<string?> latest) {
  app.MapGet("/v1/challenge",(HttpContext ctx)=> {var c=pairing.Issue(ctx.Request.Headers.Origin.ToString(),ctx.Request.Query["keyId"].ToString());return c is null?Results.StatusCode(429):Results.Json(c);});
  app.MapPost("/v1/session",async (HttpContext ctx)=> {
   if(ctx.Request.ContentLength is null or >4096){return Results.StatusCode(413);}
   Proof? p;try {p=await ctx.Request.ReadFromJsonAsync<Proof>();}catch(Exception e)when(e is System.Text.Json.JsonException or BadHttpRequestException){return Results.StatusCode(400);}
   if(p is null||p.ChallengeId is null||p.PublicKey is null||p.Signature is null||p.PublicKey.Length>512||p.Signature.Length>128)return Results.StatusCode(400);
   var r=await pairing.Authenticate(ctx.Request.Headers.Origin.ToString(),p);return r.token is null?Results.StatusCode(r.status):Results.Json(new {token=r.token,expiresInSeconds=60});
  });
  app.MapGet("/v1/telemetry",()=>latest() is { } json?Results.Text(json,"application/json"):Results.StatusCode(503));
 }
}
