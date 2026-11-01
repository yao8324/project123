"""Create a local reviewable source archive from an explicit whitelist. No Git mutation/network."""
from pathlib import Path
import zipfile,json,hashlib
R=Path(__file__).resolve().parents[2];D=R/'Docs/GlassFloorReview_20261007';files=[]
for root in ['Assets','Packages','ProjectSettings']:
 files += [p for p in (R/root).rglob('*')if p.is_file()]
for p in (R/'Tools/MilanV6').rglob('*'):
 if p.is_file()and p.suffix in {'.py','.cs','.json','.md','.sh'} and '__pycache__'not in p.parts:files.append(p)
for suffix in ['GlassFloorReview','PlaytestReview','Assembly']:
 files.append(R/f'Blender/MilanLibrary_Magic_V6/MilanLibrary_Magic_V6_{suffix}.blend')
for p in D.rglob('*'):
 if p.is_file()and p.suffix in {'.md','.json','.csv','.png','.jpg','.mp4'}and p.name not in {'source_package.json'}:files.append(p)
files += [R/p for p in ['README.md','AGENTS.md','.gitignore','.gitattributes','Docs/PlaytestReview_20261007/VERIFICATION.md','Docs/MilanV6Unity/glass_floor_geometry.json','Docs/MilanV6Unity/playtest_review_geometry.json']]
files=sorted(set(files));forbidden={'.pfx','.p12','.jks','.keystore','.ulf','.lic','.blend1','.pyc'}
assert all(p.exists()and p.suffix not in forbidden and not p.name.startswith('.env')for p in files)
entries=[{'path':str(p.relative_to(R)),'bytes':p.stat().st_size,'sha256':hashlib.sha256(p.read_bytes()).hexdigest()}for p in files]
(D/'SOURCE_WHITELIST.json').write_text(json.dumps(entries,indent=2));files.append(D/'SOURCE_WHITELIST.json')
out=R/'Builds/MilanLibrary_GlassFloor_Source_20261007.zip'
assert not out.exists(),'Refuse to overwrite an existing delivery'
with zipfile.ZipFile(out,'w',zipfile.ZIP_DEFLATED,compresslevel=4)as z:
 for p in files:z.write(p,'CandidateProject/'+str(p.relative_to(R)))
with zipfile.ZipFile(out)as z:assert z.testzip()is None
result={'archive':str(out.relative_to(R)),'bytes':out.stat().st_size,'sha256':hashlib.sha256(out.read_bytes()).hexdigest(),'files':len(files),'scope':'Reviewable local source package; not published; no cache/Player binaries/secrets.'}
(D/'source_package.json').write_text(json.dumps(result,indent=2));print(json.dumps(result))
