"""Generate MSI file components only from the reviewed local publish directory."""
from pathlib import Path
import hashlib, uuid, xml.etree.ElementTree as ET
root=Path(__file__).resolve().parents[1];publish=root/'artifacts/Bridge';ns='http://wixtoolset.org/schemas/v4/wxs';ET.register_namespace('',ns)
wix=ET.Element('{'+ns+'}Wix');fragment=ET.SubElement(wix,'{'+ns+'}Fragment');group=ET.SubElement(fragment,'{'+ns+'}ComponentGroup',Id='BridgeFiles',Directory='INSTALLFOLDER')
files=sorted(p for p in publish.rglob('*') if p.is_file())
assert any(p.name=='MotherSys.TelemetryBridge.exe' for p in files)
for p in files:
 relative=p.relative_to(publish).as_posix()
 if p.suffix.lower() in ['.pdb'] or p.name=='web.config':continue
 assert not p.is_symlink() and '..' not in p.relative_to(publish).parts
 suffix=hashlib.sha256(relative.encode()).hexdigest()[:20]
 component=ET.SubElement(group,'{'+ns+'}Component',Id='C'+suffix,Guid=str(uuid.uuid5(uuid.UUID('f9c82f67-d347-42b9-9956-5a2334f68cf4'),relative)))
 # HKCU key paths are used for per-user file components (ICE38).
 ET.SubElement(component,'{'+ns+'}RegistryValue',Root='HKCU',Key='Software\\MotherSys\\TelemetryBridge\\R1.3\\Files',Name=suffix,Type='integer',Value='1',KeyPath='yes')
 parent=Path(relative).parent
 if str(parent)!='.':component.set('Subdirectory',parent.as_posix().replace('/','\\'))
 ET.SubElement(component,'{'+ns+'}File',Id='BridgeExe' if p.name=='MotherSys.TelemetryBridge.exe' else 'F'+suffix,Source='$(var.BridgePublishDir)\\'+relative.replace('/','\\'))
 ET.SubElement(component,'{'+ns+'}RemoveFolder',Id='R'+suffix,On='uninstall')
ET.indent(wix);ET.ElementTree(wix).write(root/'installer/Files.generated.wxs',encoding='utf-8',xml_declaration=True)
