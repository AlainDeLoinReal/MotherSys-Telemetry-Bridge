using MotherSys.Installer;
void Check(bool value,string name){if(!value)throw new Exception(name);Console.WriteLine("PASS "+name);}
var root=Path.Combine(Path.GetTempPath(),"mothersys-lifecycle-test-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(root);
try {
 var own=Path.Combine(root,"R1.3");Directory.CreateDirectory(own);
 Check(LifecyclePolicy.SamePath(own,own+Path.DirectorySeparatorChar),"canonical owned path");
 Check(!LifecyclePolicy.SamePath(Path.Combine(root,"R1.2"),own),"other revision excluded");
 Check(!LifecyclePolicy.SamePath(root,own),"general parent excluded");
 Check(!LifecyclePolicy.SamePath(Path.Combine(root,"R1.3-other"),own),"prefix collision excluded");
 LifecyclePolicy.RefuseReparse(Path.Combine(own,"missing","grants.bin"));Check(true,"missing paths safe");
 var state=new Dictionary<string,object>{{"Sensors",true},{"Grants",new object[]{"synthetic-grant"}},{"Blocked",new object[]{"synthetic-key"}}};var kept=LifecyclePolicy.RetainSettingsWithoutGrants(state);
 Check((bool)kept["Sensors"]&&((object[])kept["Grants"]).Length==0&&((object[])kept["Blocked"]).Length==0,"keep settings never keeps website grants");
 var invalid=false;try{LifecyclePolicy.RetainSettingsWithoutGrants(new Dictionary<string,object>());}catch(IOException){invalid=true;}Check(invalid,"corrupt settings fail closed");
 var target=Path.Combine(root,"other-app");Directory.CreateDirectory(target);var sentinel=Path.Combine(target,"sentinel.txt");File.WriteAllText(sentinel,"synthetic unrelated application");var link=Path.Combine(own,"redirect");
 try{Directory.CreateSymbolicLink(link,target);var refused=false;try{LifecyclePolicy.RefuseReparse(Path.Combine(link,"sentinel.txt"));}catch(IOException){refused=true;}Check(refused,"redirected data path refused");Check(File.ReadAllText(sentinel)=="synthetic unrelated application","unrelated sentinel preserved");Directory.Delete(link);}catch(UnauthorizedAccessException){Console.WriteLine("NOT RUN symlink creation requires a supported test privilege");}
}finally{Directory.Delete(root,true);}
Console.WriteLine("LIFECYCLE POLICY PASS; real Windows install/uninstall remains NOT RUN");
