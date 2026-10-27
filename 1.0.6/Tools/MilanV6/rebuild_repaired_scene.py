"""Run in the independent project copy, with --blender pointing to matched Blender5.2+."""
import argparse,subprocess,json,hashlib,os,shutil
from pathlib import Path
p=argparse.ArgumentParser();p.add_argument('--blender',required=True);args=p.parse_args()
root=Path(__file__).resolve().parents[2];tools=root/'Tools/MilanV6';docs=root/'Docs/MilanV6Unity';docs.mkdir(parents=True,exist_ok=True)
source=root/'Blender/MilanLibrary_Magic_V6/MilanLibrary_Magic_V6_Assembly.blend'
expected='0fdd3effbe31f03d91f4adc0d56852b94c021e23a7bdc88b918f370c2ae7ab93'
assert hashlib.sha256(source.read_bytes()).hexdigest()==expected,'Baseline Assembly mismatch'
for name,command in [('native_repair',[args.blender,'-b','--factory-startup','-noaudio','--disable-autoexec','--python-exit-code','1','--python',str(tools/'repair_walkability.py')]),('playtest_review',[args.blender,'-b','--factory-startup','-noaudio','--disable-autoexec','--python-exit-code','1','--python',str(tools/'apply_playtest_review.py')]),('glass_floor_review',[args.blender,'-b','--factory-startup','-noaudio','--disable-autoexec','--python-exit-code','1','--python',str(tools/'apply_glass_floor_review.py')]),('repaired_export',[args.blender,'-b','-noaudio','--disable-autoexec',str(source.with_name('MilanLibrary_Magic_V6_GlassFloorReview.blend')),'--python-exit-code','1','--python',str(tools/'export_scene.py')])]:
 if name=='repaired_export':assert json.loads((docs/'walkability_geometry_validation.json').read_text())['static_pass'],'Native static validation failed; export stopped'
 with (docs/(name+'.log')).open('w') as out:subprocess.run(command,cwd=root,stdout=out,stderr=subprocess.STDOUT,check=True)
assert hashlib.sha256(source.read_bytes()).hexdigest()==expected,'Original source changed'
m=json.loads((root/'Assets/Art/MilanLibraryV6/manifest.json').read_text());assert m['formatVersion']==2 and m['repairRevision']=='walkability_v1'
# Rebuild the same minimal hand from readable source, avoiding binary transfer.
hand=tools/'CharacterBuild';env=os.environ.copy();env['MAGE_ASSET_ONLY']='1'
for name in ['build_mage_hand.py','validate_roundtrip.py']:
 with (docs/('character_'+name+'.log')).open('w') as out:
  subprocess.run([args.blender,'-b','--factory-startup','-noaudio','--disable-autoexec','--python-exit-code','1','--python',str(hand/name)],cwd=root,env=env,stdout=out,stderr=subprocess.STDOUT,check=True)
h=json.loads((hand/'fbx_roundtrip_validation.json').read_text());assert h['six_actions_present'] and h['all_roots_stationary'] and h['grip_locked_all_frames'] and 0 < h['total_triangles'] <= 8000 and h['total_triangles'] == json.loads((hand/'validation_manifest.json').read_text())['total_triangles']
art=root/'Assets/Art/Characters/FirstPersonMage';art.mkdir(parents=True,exist_ok=True);shutil.copy2(hand/'MageHand_Generic.fbx',art/'MageHand_Generic.fbx')
print('NATIVE_EXPORT_AND_HAND_READY',len(m['chunks']),'chunks; Unity compilation and Play still required')
