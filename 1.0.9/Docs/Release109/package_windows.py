from pathlib import Path
import zipfile,hashlib,json,shutil,struct
r=Path(__file__).resolve().parents[3];q=r/'1.0.9';b=q/'Builds/IMMonster_1.0.9_Windows';d=r/'完整版/1.0.9';out=d/'IMMonster_1.0.9_Windows.zip'
assert 'Result: Succeeded' in (q/'Docs/Release109/WindowsBuild.txt').read_text()
for rel in ['IMMonster.exe','UnityPlayer.dll','IMMonster_Data/data.unity3d','UnityCrashHandler64.exe']:
 p=b/rel;assert p.is_file(),rel
for rel in ['IMMonster.exe','UnityPlayer.dll']:
 data=(b/rel).read_bytes();off=struct.unpack_from('<I',data,0x3c)[0];assert data[off:off+4]==b'PE\0\0' and struct.unpack_from('<H',data,off+4)[0]==0x8664
assert (b/'IMMonster_Data/data.unity3d').open('rb').read(8)==b'UnityFS\0'
files=[p for p in b.rglob('*') if p.is_file() and not any('DoNotShip' in x for x in p.parts)]
with zipfile.ZipFile(out,'w',zipfile.ZIP_DEFLATED,compresslevel=6,allowZip64=True) as z:
 for p in files:z.write(p,Path(b.name)/p.relative_to(b))
with zipfile.ZipFile(out) as z:assert z.testzip() is None
h=hashlib.sha256()
with out.open('rb') as f:
 for block in iter(lambda:f.read(4*1024*1024),b''):h.update(block)
record={'version':'1.0.9','base_exe_commit':'0cb7d51336ccfe7e4e6bcd6b37a0939f9e6531d2','file':out.name,'bytes':out.stat().st_size,'sha256':h.hexdigest(),'zip_crc':'passed','architecture':'Windows x64 PE checked','file_count':len(files),'windows_native_test':False,'campaign_assertions':244,'previous_version':{'version':'1.0.8','path':'../三Boss剧情整合_20261108/IMMonster_ThreeBoss_Windows_20261108.zip','bytes':850113179,'sha256':'0e91f676ac60849ad75a4d9b7e73b511fab85f13a7b44f9e1da167424916518c'}}
(d/'PackageVerification.json').write_text(json.dumps(record,ensure_ascii=False,indent=2)+'\n');(d/'SHA256SUMS.txt').write_text(h.hexdigest()+'  '+out.name+'\n')
for name in ['Acceptance.txt','WindowsBuild.txt','SceneCheck.txt']:shutil.copy2(q/'Docs/Release109'/name,d/name)
print(json.dumps(record,ensure_ascii=False,indent=2))
