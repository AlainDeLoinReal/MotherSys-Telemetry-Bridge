using Microsoft.Win32;
using System.Security.Cryptography;
using System.Text.Json;
using System.Windows.Forms;
namespace MotherSys;
public sealed class GrantStore : IGrants {
 readonly object gate=new(); readonly string path=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"MotherSys","TelemetryBridge","R1.3-data","grants.bin");
 List<Grant> grants=[]; List<Grant> blocked=[];
 public GrantStore(){if(File.Exists(path)){var state=JsonSerializer.Deserialize<State>(ProtectedData.Unprotect(File.ReadAllBytes(path),null,DataProtectionScope.CurrentUser))??throw new IOException("Invalid authorization store");grants=state.Grants;blocked=state.Blocked;Sensors=state.Sensors;}}
 record State(List<Grant> Grants,List<Grant> Blocked,bool Sensors);
 public bool Contains(Grant g){lock(gate)return grants.Contains(g);}
 public bool Blocked(Grant g){lock(gate)return blocked.Any(x=>x.Origin==g.Origin&&x.KeyId==g.KeyId);}
 void Save(){Directory.CreateDirectory(Path.GetDirectoryName(path)!);var bytes=ProtectedData.Protect(JsonSerializer.SerializeToUtf8Bytes(new State(grants,blocked,Sensors)),null,DataProtectionScope.CurrentUser);File.WriteAllBytes(path+".tmp",bytes);File.Move(path+".tmp",path,true);}
 public void Allow(Grant g){lock(gate){if(grants.Count>=64)throw new IOException("Authorization limit reached");grants.Add(g);Save();}}
 public void Deny(Grant g){lock(gate){if(blocked.Count<128&&!blocked.Contains(g)){blocked.Add(g);Save();}}}
 public void Revoke(){lock(gate){foreach(var g in grants)if(!blocked.Contains(g))blocked.Add(g);grants.Clear();Save();}}
 public bool Sensors {get;private set;}
 public void SetSensors(bool value){lock(gate){Sensors=value;Save();}}
 public void Reset(){lock(gate){grants.Clear();blocked.Clear();Save();}}
}
static class Program {
 const string RunName="MotherSysTelemetryBridgeR13";
 static string InstallDir=>Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"MotherSys","TelemetryBridge","R1.3");
 [STAThread] static void Main(string[] args) {
  ApplicationConfiguration.Initialize();
  if(args.Any(a=>a is not ("--background" or "--open"))){MessageBox.Show("Install and uninstall this version with the local MSI package or Windows Installed apps.");return;}
  using var mutex=new Mutex(true,"Local\\MotherSys.TelemetryBridge.R13",out var first);if(!first){MessageBox.Show("MotherSys Telemetry Bridge is already running. Use its tray icon.");return;}
  try{Application.Run(new Tray());}catch(Exception e){MessageBox.Show("Bridge could not start: "+e.GetType().Name+". No permissions were reset.");}
 }
 sealed class Tray : ApplicationContext {
  readonly NotifyIcon icon;readonly Control ui=new();readonly GrantStore grants=new();readonly Pairing pairing;readonly CancellationTokenSource stop=new();
  WebApplication? app;Task? worker;int prompt;
  public Tray(){
   ui.CreateControl();_ = ui.Handle;pairing=new Pairing(grants,Approve);
   var menu=new ContextMenuStrip();menu.Items.Add("MotherSys Telemetry Bridge R1.3 lifecycle candidate").Enabled=false;
   var auto=new ToolStripMenuItem("Start when I sign in"){Checked=AutoEnabled()};auto.Click+=(_,_)=>{var enable=!auto.Checked;if(MessageBox.Show(enable?"Allow Bridge to start at Windows sign-in?":"Disable Bridge start at Windows sign-in?","MotherSys",MessageBoxButtons.YesNo)!=DialogResult.Yes)return;using var run=Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run");if(enable)run.SetValue(RunName,$"\"{Environment.ProcessPath}\" --background");else run.DeleteValue(RunName,false);auto.Checked=enable;};menu.Items.Add(auto);
   var sensors=new ToolStripMenuItem("Optional hardware sensors (next launch)"){Checked=grants.Sensors};sensors.Click+=(_,_)=>{if(MessageBox.Show("Change optional hardware sensors for the next Bridge launch? Basic telemetry works without them. Sensors may remain N/A without supported hardware or permissions. Do not disable Windows security.","MotherSys",MessageBoxButtons.YesNo)!=DialogResult.Yes)return;grants.SetSensors(!sensors.Checked);sensors.Checked=grants.Sensors;MessageBox.Show("Stop and start Bridge to apply this choice.");};menu.Items.Add(sensors);
   menu.Items.Add("Revoke all website access",null,(_,_)=>{if(MessageBox.Show("Revoke all website permissions and disconnect LIVE?","MotherSys",MessageBoxButtons.YesNo)==DialogResult.Yes){pairing.RevokeSessions();grants.Revoke();}});
   menu.Items.Add("Allow a new pairing after revocation",null,(_,_)=>{if(MessageBox.Show("Clear blocked browser keys? Existing permissions will also be removed. Every new pairing will require approval.","MotherSys",MessageBoxButtons.YesNo)==DialogResult.Yes){pairing.RevokeSessions();grants.Reset();}});
   menu.Items.Add("Uninstall from Windows Settings",null,(_,_)=>System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("ms-settings:appsfeatures"){UseShellExecute=true}));
   menu.Items.Add("Stop Bridge",null,(_,_)=>ExitThread());
   icon=new NotifyIcon{Text="MotherSys Telemetry Bridge",Icon=System.Drawing.SystemIcons.Application,ContextMenuStrip=menu,Visible=true};
   worker=Task.Run(Serve);_ = Task.Run(WaitForUninstall);
  }
  static bool AutoEnabled(){using var run=Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run");return run?.GetValue(RunName) is string;}
  Task<bool> Approve(Grant grant){
   if(Interlocked.Exchange(ref prompt,1)!=0)return Task.FromResult(false);
   var done=new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
   ui.BeginInvoke(()=>{try{var approved=MessageBox.Show("Allow this exact website to read CPU, RAM, GPU, processes, disks and network rates?\n\n"+grant.Origin+"\n\nOnly approve an address you opened intentionally. Access is remembered for this browser. Revoke it from the tray menu.","MotherSys — website authorization",MessageBoxButtons.YesNo,MessageBoxIcon.Question,MessageBoxDefaultButton.Button2)==DialogResult.Yes;if(!approved)grants.Deny(grant);done.TrySetResult(approved);}catch{done.TrySetResult(false);}finally{Interlocked.Exchange(ref prompt,0);}});
   return done.Task.WaitAsync(stop.Token);
  }
  async Task WaitForUninstall(){
   while(!stop.IsCancellationRequested)try {using var pipe=new System.IO.Pipes.NamedPipeServerStream("MotherSys.TelemetryBridge.R13."+System.Security.Principal.WindowsIdentity.GetCurrent().User!.Value,System.IO.Pipes.PipeDirection.In,1,System.IO.Pipes.PipeTransmissionMode.Byte,System.IO.Pipes.PipeOptions.Asynchronous|System.IO.Pipes.PipeOptions.CurrentUserOnly);
    await pipe.WaitForConnectionAsync(stop.Token);using var reader=new StreamReader(pipe);var command=await reader.ReadLineAsync(stop.Token);if(command=="STOP"){ui.BeginInvoke(()=>{pairing.RevokeSessions();ExitThread();});return;}
   }catch(OperationCanceledException){return;}catch(IOException){} }
  async Task Serve(){try{
   var builder=WebApplication.CreateBuilder(new WebApplicationOptions{Args=[],ContentRootPath=AppContext.BaseDirectory});builder.WebHost.UseUrls("http://127.0.0.1:47631");builder.Logging.ClearProviders();builder.WebHost.ConfigureKestrel(o=>{o.Limits.MaxRequestBodySize=4096;o.Limits.MaxConcurrentConnections=32;o.Limits.RequestHeadersTimeout=TimeSpan.FromSeconds(10);});
   app=builder.Build();app.Use((ctx,next)=>AccessPolicy.Invoke(ctx,next,pairing));
   // Same R1.1 collector. Basic mode never attempts privileged hardware drivers.
   using var collector=new WindowsCollector(grants.Sensors);AccessPolicy.Map(app,pairing,()=>collector.Latest);
   var collection=collector.Run(stop.Token);try{await app.RunAsync(stop.Token);}finally{stop.Cancel();try{await collection;}catch(OperationCanceledException){}}
  }catch(OperationCanceledException){}catch(Exception e){if(!stop.IsCancellationRequested)ui.BeginInvoke(()=>{MessageBox.Show("Bridge stopped: "+e.GetType().Name+". Check that the local port is available.");ExitThread();});}}
  protected override void ExitThreadCore(){stop.Cancel();pairing.RevokeSessions();icon.Visible=false;icon.Dispose();base.ExitThreadCore();}
 }
}
