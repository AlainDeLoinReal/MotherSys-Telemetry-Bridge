using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Web.Script.Serialization;
using System.Windows.Forms;
using Microsoft.Win32;
using WixToolset.Dtf.WindowsInstaller;

namespace MotherSys.Installer {
 public static class Actions {
  internal static string ProductRoot => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"MotherSys","TelemetryBridge");
  internal static string InstallDir => Path.Combine(ProductRoot,"R1.3");
  internal static string DataDir => Path.Combine(ProductRoot,"R1.3-data");
  const string ProtocolKey=@"Software\Classes\mothersys-telemetry";
  const string RunKey=@"Software\Microsoft\Windows\CurrentVersion\Run";
  const string RunName="MotherSysTelemetryBridgeR13";
  static string Exe => Path.Combine(InstallDir,"MotherSys.TelemetryBridge.exe");
  static string ProtocolCommand => "\""+Exe+"\" --open";

  [CustomAction] public static ActionResult Choices(Session session) {
   try {
    // Only the fixed per-user directory is accepted, even if an MSI property is overridden.
    if(!LifecyclePolicy.SamePath(session["INSTALLFOLDER"],InstallDir))throw new IOException("Installation location must be the fixed current-user Bridge directory.");
    LifecyclePolicy.RefuseReparse(InstallDir);LifecyclePolicy.RefuseReparse(DataDir);
    bool removing=session["REMOVE"]=="ALL";
    if(!removing) {
     using(var command=Registry.CurrentUser.OpenSubKey(ProtocolKey+@"\shell\open\command")) {
      var prior=command?.GetValue("") as string;
      if(prior!=null&&prior!=ProtocolCommand)throw new IOException("Another application or Bridge version owns the launch protocol. Remove its launch registration using that application's normal uninstall controls first.");
     }
    }
    int level;int.TryParse(session["UILevel"],out level);
    if(level>=3) {
     using(var form=new Form{Text=removing?"Uninstall MotherSys Telemetry Bridge":"Install MotherSys Telemetry Bridge",Width=560,Height=270,StartPosition=FormStartPosition.CenterScreen,FormBorderStyle=FormBorderStyle.FixedDialog,MaximizeBox=false,MinimizeBox=false,TopMost=true}) {
      var info=new Label{Left=20,Top=18,Width=500,Height=90,Text=removing?"Remove MotherSys Telemetry Bridge 1.3.0 for this Windows account? Bridge and its local listener will stop. Active and saved website access will be revoked.":"Install MotherSys Telemetry Bridge 1.3.0 for this Windows account. Publisher: MotherSys. No administrator access or service. Website access requires a separate consent."};
      var choice=new CheckBox{Left=20,Top=115,Width=510,Checked=removing,Text=removing?"Remove saved settings and website permissions":"Start Bridge when I sign in to Windows"};
      var detail=new Label{Left=20,Top=143,Width=500,Height=40,Text=removing?"If unchecked, only the sensor preference remains locally. No website keeps authorization.":"Startup is optional and disabled by default. This candidate has not been qualified on Windows."};
      var ok=new Button{Left=300,Top=190,Width=110,Text=removing?"Uninstall":"Install",DialogResult=DialogResult.OK};var cancel=new Button{Left=420,Top=190,Width=95,Text="Cancel",DialogResult=DialogResult.Cancel};form.Controls.AddRange(new Control[]{info,choice,detail,ok,cancel});form.AcceptButton=ok;form.CancelButton=cancel;
      if(form.ShowDialog()!=DialogResult.OK)return ActionResult.UserExit;
      session[removing?"REMOVE_SAVED_DATA":"START_WITH_WINDOWS"]=choice.Checked?"1":"0";
     }
    }
    session["REGISTER_ON_COMMIT"]=!removing&&session["Installed"]==""?"1":"0";
    session["PrepareRemoval"]="RemoveData="+session["REMOVE_SAVED_DATA"];
    session["RegisterLaunch"]="Startup="+session["START_WITH_WINDOWS"];
    session["RollbackLaunch"]="Startup="+session["START_WITH_WINDOWS"];
    return ActionResult.Success;
   } catch(Exception e){ShowFailure(e);return ActionResult.Failure;}
  }

  [CustomAction] public static ActionResult RegisterLaunch(Session session) {
   try {LifecyclePolicy.RefuseReparse(InstallDir);LifecyclePolicy.RefuseReparse(DataDir);
    using(var key=Registry.CurrentUser.CreateSubKey(ProtocolKey+@"\shell\open\command")) {
     var prior=key.GetValue("") as string;if(prior!=null&&prior!=ProtocolCommand)throw new IOException("Launch protocol ownership changed during installation.");key.SetValue("",ProtocolCommand);
    }
    using(var key=Registry.CurrentUser.CreateSubKey(ProtocolKey)) {key.SetValue("","MotherSys Telemetry Bridge");key.SetValue("URL Protocol","");}
    if(session.CustomActionData["Startup"]=="1")using(var key=Registry.CurrentUser.CreateSubKey(RunKey))key.SetValue(RunName,"\""+Exe+"\" --background");
    return ActionResult.Success;
   }catch(Exception e){ShowFailure(e);return ActionResult.Failure;}
  }
  [CustomAction] public static ActionResult RollbackLaunch(Session session) {
   try {RemoveOwnedRegistration();return ActionResult.Success;}catch{return ActionResult.Failure;}
  }
  [CustomAction] public static ActionResult PrepareRemoval(Session session) {
   try {LifecyclePolicy.RefuseReparse(InstallDir);LifecyclePolicy.RefuseReparse(DataDir);StopOwnedBridge();RevokeOrDelete(session.CustomActionData["RemoveData"]!="0");RemoveOwnedRegistration();return ActionResult.Success;}
   catch(Exception e){ShowFailure(e);return ActionResult.Failure;}
  }
  static void StopOwnedBridge() {
   try {using(var pipe=new NamedPipeClientStream(".","MotherSys.TelemetryBridge.R13."+WindowsIdentity.GetCurrent().User.Value,PipeDirection.Out)) {pipe.Connect(1000);using(var w=new StreamWriter(pipe){AutoFlush=true})w.WriteLine("STOP");}}catch(TimeoutException){}catch(IOException){}
   foreach(var process in Process.GetProcessesByName("MotherSys.TelemetryBridge"))using(process) {
    if(process.HasExited)continue;
    string path;
    try{path=process.MainModule.FileName;}catch {if(process.SessionId==Process.GetCurrentProcess().SessionId)throw new IOException("Cannot verify a running Bridge process. Uninstall stopped without targeting an unverified process.");continue;}
    if(!LifecyclePolicy.SamePath(path,Exe))continue;
    if(!process.WaitForExit(5000)) {if(!LifecyclePolicy.SamePath(process.MainModule.FileName,Exe))throw new IOException("Process identity changed.");process.Kill();if(!process.WaitForExit(5000))throw new IOException("Bridge did not stop.");}
   }
  }
  static void RevokeOrDelete(bool remove) {
   var file=Path.Combine(DataDir,"grants.bin");var temporary=file+".tmp";LifecyclePolicy.RefuseReparse(file);LifecyclePolicy.RefuseReparse(temporary);
   if(remove) {File.Delete(file);File.Delete(temporary);if(Directory.Exists(DataDir)&&!Directory.EnumerateFileSystemEntries(DataDir).Any())Directory.Delete(DataDir);return;}
   if(File.Exists(file)) {
    var json=Encoding.UTF8.GetString(ProtectedData.Unprotect(File.ReadAllBytes(file),null,DataProtectionScope.CurrentUser));
    var serializer=new JavaScriptSerializer{MaxJsonLength=1048576};var state=serializer.Deserialize<Dictionary<string,object>>(json);
    if(state==null||!state.ContainsKey("Sensors")||!(state["Sensors"] is bool))throw new IOException("Saved settings cannot be read. Retry uninstall with data removal selected.");
    // Keep the sensor preference, revoke every persisted grant and discard old public-key records.
    var revoked=LifecyclePolicy.RetainSettingsWithoutGrants(state);
    var encrypted=ProtectedData.Protect(Encoding.UTF8.GetBytes(serializer.Serialize(revoked)),null,DataProtectionScope.CurrentUser);
    using(var stream=new FileStream(temporary,FileMode.Create,FileAccess.Write,FileShare.None)){stream.Write(encrypted,0,encrypted.Length);stream.Flush(true);}
    File.Replace(temporary,file,null);
   }
   File.Delete(temporary);
  }
  static void RemoveOwnedRegistration() {
   using(var run=Registry.CurrentUser.OpenSubKey(RunKey,true)) {
    var value=run?.GetValue(RunName) as string;
    if(value=="\""+Exe+"\" --background")run.DeleteValue(RunName,false);
   }
   var commandPath=ProtocolKey+@"\shell\open\command";
   using(var command=Registry.CurrentUser.OpenSubKey(commandPath,true)) {
    if(command?.GetValue("") as string!=ProtocolCommand)return;
    command.DeleteValue("",false);
   }
   DeleteKeyIfEmpty(commandPath);DeleteKeyIfEmpty(ProtocolKey+@"\shell\open");DeleteKeyIfEmpty(ProtocolKey+@"\shell");
   using(var protocol=Registry.CurrentUser.OpenSubKey(ProtocolKey,true)) {
    if(protocol!=null) {
     if(protocol.GetValue("") as string=="MotherSys Telemetry Bridge")protocol.DeleteValue("",false);
     if(protocol.GetValue("URL Protocol") as string=="")protocol.DeleteValue("URL Protocol",false);
    }
   }
   DeleteKeyIfEmpty(ProtocolKey);
  }
  static void DeleteKeyIfEmpty(string path) {
   bool empty;using(var key=Registry.CurrentUser.OpenSubKey(path))empty=key!=null&&key.ValueCount==0&&key.SubKeyCount==0;
   if(empty)Registry.CurrentUser.DeleteSubKey(path,false);
  }
  static void ShowFailure(Exception error) {MessageBox.Show("Operation stopped: "+error.GetType().Name+". "+error.Message+" No unrelated application paths were targeted.","MotherSys Telemetry Bridge",MessageBoxButtons.OK,MessageBoxIcon.Error);}
 }
}
