from pathlib import Path
import hashlib,json,zipfile
root=Path(__file__).resolve().parents[1];out=root.parent/'downloads/Project_HINA_Unity_Project_Draft.zip';out.parent.mkdir(exist_ok=True)
validation=json.loads((root/'VALIDATION.json').read_text());assert validation['core_checks_failed']==0
with zipfile.ZipFile(out,'w',zipfile.ZIP_DEFLATED,compresslevel=6) as z:
 for p in sorted(root.rglob('*')):
  if not p.is_file():continue
  rel=p.relative_to(root)
  if any(v in rel.parts for v in ['bin','obj','Library','Temp','Logs','Builds','__pycache__','.git']):continue
  z.write(p,'Project_HINA_Unity/'+str(rel))
with zipfile.ZipFile(out) as z:
 assert z.testzip() is None
 assert 'Project_HINA_Unity/ProjectHINA/ProjectSettings/ProjectVersion.txt' in z.namelist()
 assert 'Project_HINA_Unity/ProjectHINA/Assets/Hina/Resources/Hina/Sheets/player.png' in z.namelist()
print(out)
print('Bytes:',out.stat().st_size)
print('SHA256:',hashlib.file_digest(out.open('rb'),'sha256').hexdigest())
