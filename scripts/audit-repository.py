"""Scoped, auditable source/history boundary and secret-pattern check.
This is not a guarantee that all possible secrets can be recognized.
"""
from pathlib import Path, PurePosixPath
import subprocess, re, json, hashlib
root=Path(__file__).resolve().parents[1]
def git(*args):return subprocess.check_output(['git','-C',str(root),*args])
allowed_roots={'bridge','installer','shared','tests','docs','licenses','scripts','.github'}
allowed_files={'README.md','LICENSE','.gitignore','global.json','NuGet.Config','Directory.Build.props'}
secret_patterns={
 'private key':rb'-----BEGIN (?:RSA |EC |OPENSSH |DSA |ENCRYPTED )?PRIVATE KEY-----',
 'GitHub token':rb'\b(?:gh[pousr]_[A-Za-z0-9]{30,}|github_pat_[A-Za-z0-9_]{40,})\b',
 'cloud credential':rb'\b(?:AKIA|ASIA)[A-Z0-9]{16}\b',
 'literal credential':rb'(?i)(?:password|client_secret|api[_-]?key|bearer|access_token)\s*[:=]\s*["\'][A-Za-z0-9_+/=-]{24,}["\']',
 'private Linux path':rb'/(?:home|mnt)/(?:etranger|ADATA|Crucial)',
}
forbidden_extensions={'.exe','.dll','.pfx','.p12','.pem','.key','.bin','.zip','.7z','.ttf','.png','.jpg','.mp3','.wav','.ts','.tsx','.js'}
def scan(path,data):
 parts=PurePosixPath(path).parts
 if not parts or (parts[0] not in allowed_roots and path not in allowed_files):raise RuntimeError('Unexpected path: '+path)
 if PurePosixPath(path).suffix.lower() in forbidden_extensions:raise RuntimeError('Disallowed binary/private-web path: '+path)
 if any(part in {'.git','obj','bin','node_modules','dist','artifacts'} for part in parts):raise RuntimeError('Generated/private directory: '+path)
 if len(data)>2_000_000:raise RuntimeError('Unexpected large source file: '+path)
 for label,pattern in secret_patterns.items():
  # Avoid the scanner recognizing its own literal definitions.
  if path=='scripts/audit-repository.py':continue
  if re.search(pattern,data):raise RuntimeError(label+' detected in '+path)
 if path.startswith('bridge/') and PurePosixPath(path).suffix=='.cs' and b'.EndsWith(".mothersys-classic-web.pages.dev"' in data:raise RuntimeError('Wildcard family origin policy')
paths=git('ls-files','-z').decode().split('\0');paths=[p for p in paths if p]
for path in paths:scan(path,(root/path).read_bytes())
commits=git('rev-list','--all').decode().splitlines()
blobs=set()
for commit in commits:
 metadata=git('show','-s','--format=%ae%n%ce',commit).decode().splitlines()
 message=git('show','-s','--format=%B',commit)
 for label,pattern in secret_patterns.items():
  if re.search(pattern,message):raise RuntimeError(label+' detected in commit message')
 if any(not email.endswith('@users.noreply.github.com') for email in metadata):raise RuntimeError('Non-public Git email in history')
 for row in git('ls-tree','-rz','--full-tree',commit).split(b'\0'):
  if not row:continue
  info,path=row.split(b'\t',1);mode,kind,oid=info.split();path=path.decode()
  if kind!=b'blob' or mode==b'120000':raise RuntimeError('Non-file/symlink history entry')
  key=(oid,path)
  if key not in blobs:scan(path,git('cat-file','blob',oid.decode()));blobs.add(key)
workflow=(root/'.github/workflows/build.yml').read_text()
for action in re.findall(r'uses:\s*(\S+)',workflow):
 if not re.fullmatch(r'actions/[a-z-]+@[a-f0-9]{40}',action):raise RuntimeError('Unpinned action')
if any(term in workflow for term in ['secrets.','upload-artifact@','gh release create','contents: write']):raise RuntimeError('Unexpected publishing/credential permission')
report={'status':'PASS','scope':'Tracked working tree, every reachable commit/blob, Git metadata, path allowlist, common secret patterns, action pins and absence of binary publication. Manual review is also required.','files':len(paths),'commits':len(commits),'uniquePathBlobs':len(blobs),'head':git('rev-parse','HEAD').decode().strip() if commits else None,'privateClassicWebAssets':False,'binaryAssets':False,'hashes':{p:hashlib.sha256((root/p).read_bytes()).hexdigest() for p in paths}}
print(json.dumps(report,indent=2))
