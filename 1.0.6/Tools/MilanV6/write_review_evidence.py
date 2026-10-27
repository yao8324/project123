"""Package evidence without full Editor logs or license data; never modifies source inputs."""
import csv,json,hashlib,shutil
from pathlib import Path
from PIL import Image,ImageDraw
ROOT=Path(__file__).resolve().parents[2];R=ROOT.parent/'Reports/WindowReview01';E=ROOT/'Docs/PlaytestReview_20261007/Evidence';E.mkdir(parents=True,exist_ok=True)
ref=Path('/Users/jack/Desktop/虚拟环境团队/attachments/a6e4b268-030c-47fb-905f-d22d4a80184e/01_第一人称视高_AI概念_v2.png')
canvas=Image.new('RGB',(1280,472),'#20252b');draw=ImageDraw.Draw(canvas)
for x,p,label in [(0,ref,'USER REFERENCE / AI CONCEPT'),(640,R/'HandReference08/hand_Idle_real_controller.png','ACTUAL UNITY / REAL CONTROLLER / SYNTHETIC INPUT')]:
 im=Image.open(p).convert('RGB');im.thumbnail((640,427));canvas.paste(im,(x,(427-im.height)//2+35));draw.text((x+12,10),label,fill='white')
canvas.save(E/'Reference_vs_Unity.png')
shots=['Idle','Walk','Sprint','Jump','Land','Crouch'];canvas=Image.new('RGB',(1440,590),'#20252b');draw=ImageDraw.Draw(canvas)
for i,n in enumerate(shots):
 im=Image.open(R/f'HandReference08/hand_{n}_real_controller.png');im.thumbnail((480,270));x=(i%3)*480;y=(i//3)*295;canvas.paste(im,(x,y+25));draw.text((x+8,y+7),n+' / real Controller',fill='white')
canvas.save(E/'Six_Real_States.png')
for name in ['Silent_Runtime_Preview.mp4','hand_Idle_real_controller.png','hand_NearWall_real_controller.png','recovery_crouch.png']:
 shutil.copy2(R/'HandReference08'/name,E/name)
for p in ['playtest_review_geometry.json','window_seam_audit.json','archive_validation.json']:
 shutil.copy2(ROOT/'Docs/MilanV6Unity'/p,E/p)
shutil.copy2(ROOT/'Tools/MilanV6/CharacterBuild/fbx_roundtrip_validation.json',E/'hand_fbx_roundtrip.json')
g=json.loads((E/'playtest_review_geometry.json').read_text());a=json.loads((E/'window_seam_audit.json').read_text())[-1]
with (E/'window_instances.csv').open('w')as f:
 w=csv.writer(f);w.writerow(['glass','zone','native_rays','uncovered','runtime_floor0','runtime_floor6','action'])
 for i,row in enumerate(a['windows']):w.writerow([row['glass'],row['zone'],row['rays'],row['uncovered'],i<22,i<22 and i not in [0,6],'coherent depth refit' if i<22 else 'original separate risalit retained'])
with (E/'table_instances.csv').open('w')as f:
 w=csv.writer(f);w.writerow(['source','proxy_count','min_xyz_blender','max_xyz_blender','open_under_table'])
 for row in g['tables']:w.writerow([row['source'],len(row['proxies']),row['worldBounds'][0],row['worldBounds'][1],True])
# Sanitize verification excerpts: only explicit project markers or C# compiler diagnostics.
lines=[]
for p in sorted(R.glob('*.log')):
 if p.name.startswith(('final_','reference_prepare08','hand_reference08','targeted_review06')):
  for line in p.read_text(errors='replace').splitlines():
   if line.startswith('MILAN_') or 'error CS' in line:lines.append(p.name+': '+line)
(E/'console_markers.txt').write_text('\n'.join(lines)+'\n')
paths=[ROOT/'Assets/Scenes/MilanLibrary_Magic_V6.unity',ROOT/'Assets/Art/MilanLibraryV6/manifest.json',ROOT/'Assets/Art/Characters/FirstPersonMage/MageHand_Generic.fbx']
paths+=list((ROOT/'Assets/Scripts').rglob('*.cs'))
paths+=list((ROOT/'Tools/MilanV6').rglob('*.py'))
paths+=list((ROOT/'Blender/MilanLibrary_Magic_V6').glob('*.blend'))
values={str(p.relative_to(ROOT)):hashlib.sha256(p.read_bytes()).hexdigest()for p in paths}
(E/'source_snapshot_sha256.json').write_text(json.dumps(values,indent=2))
print('EVIDENCE_PREPARED',len(values),'source hashes')
