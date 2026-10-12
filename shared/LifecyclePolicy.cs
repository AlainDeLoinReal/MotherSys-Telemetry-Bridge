using System;using System.Collections.Generic;using System.IO;
namespace MotherSys.Installer { internal static class LifecyclePolicy {
  internal static bool SamePath(string a,string b) {if(string.IsNullOrWhiteSpace(a))return false;return string.Equals(Path.GetFullPath(a).TrimEnd(Path.DirectorySeparatorChar),Path.GetFullPath(b).TrimEnd(Path.DirectorySeparatorChar),StringComparison.OrdinalIgnoreCase);}
  internal static void RefuseReparse(string path) {
   for(var current=Path.GetFullPath(path);!string.IsNullOrEmpty(current);current=Path.GetDirectoryName(current)) {
    if((File.Exists(current)||Directory.Exists(current))&&(File.GetAttributes(current)&FileAttributes.ReparsePoint)!=0)throw new IOException("A redirected installation/data path was detected. No files were deleted.");
   }
  }
  internal static Dictionary<string,object> RetainSettingsWithoutGrants(Dictionary<string,object> state) {
   if(state==null||!state.ContainsKey("Sensors")||!(state["Sensors"] is bool))throw new IOException("Invalid saved settings");
   return new Dictionary<string,object>{{"Grants",new object[0]},{"Blocked",new object[0]},{"Sensors",state["Sensors"]}};
  }
} }
