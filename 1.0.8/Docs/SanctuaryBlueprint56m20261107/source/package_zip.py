from pathlib import Path
import hashlib,json,zipfile
p=Path(__file__).resolve().parents[1]
files=[]
for f in sorted(p.rglob('*')):
 if not f.is_file() or 'qa' in f.relative_to(p).parts or f.suffix=='.zip' or f.name=='.gitignore' or f.relative_to(p).as_posix() in ['source/atlas.pdf','source/spec.pdf','data/file_manifest.json']:continue
 files.append(f)
manifest=[{'file':f.relative_to(p).as_posix(),'bytes':f.stat().st_size,'sha256':hashlib.sha256(f.read_bytes()).hexdigest()} for f in files]
(p/'data/file_manifest.json').write_text(json.dumps(manifest,ensure_ascii=False,indent=2)+'\n')
files.append(p/'data/file_manifest.json')
z=p/'终扉圣堂_56m_黄金翼圣骑士_建模输入包_R3.zip'
with zipfile.ZipFile(z,'w',zipfile.ZIP_DEFLATED,compresslevel=6) as out:
 for f in files:out.write(f,'Sanctuary_R3_56m/'+f.relative_to(p).as_posix())
with zipfile.ZipFile(z) as out:
 assert out.testzip() is None
 for m in manifest:assert hashlib.sha256(out.read('Sanctuary_R3_56m/'+m['file'])).hexdigest()==m['sha256']
print('ZIP files:',len(files),'MB:',round(z.stat().st_size/1e6,2),'CRC and SHA-256 verified')
