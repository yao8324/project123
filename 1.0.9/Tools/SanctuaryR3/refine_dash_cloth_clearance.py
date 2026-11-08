"""R17 bounded dash cloth-root correction, verified against actual evaluated leg surfaces."""
import bpy,math,json,shutil
from pathlib import Path
from mathutils import Quaternion
from mathutils.bvhtree import BVHTree
P=Path(__file__).resolve().parents[2];current=P/'Blender/SanctuaryR3/Paladin_R3_Animated.blend';baseline=P/'Blender/SanctuaryR3/Paladin_R3_PreDashClothR17.blend'
bpy.ops.wm.open_mainfile(filepath=str(current))
if bpy.data.objects['Paladin_R3_Rig'].get('dash_cloth_R17_applied'):
 assert baseline.exists();bpy.ops.wm.open_mainfile(filepath=str(baseline))
else:shutil.copy2(current,baseline)
s=bpy.context.scene;r=bpy.data.objects['Paladin_R3_Rig'];r.animation_data.action=bpy.data.actions['A02_Dash'];pb=r.pose.bones['Cloth.Front.R.0'];panel=bpy.data.objects['C01_FoldedPanel_Front.R']
legs=[o for o in s.objects if o.name.startswith(('R05_ThighArmor','R05_PointKneeShield','R05_TaperedGreave','R05_R11_HipShell'))]
# Geometry unrelated to selected cloth/leg surfaces is hidden only for evaluation speed.
hidden=[(o,o.hide_viewport) for o in s.objects if o.type=='MESH' and o!=panel and o not in legs]
for o,_ in hidden:o.hide_viewport=True
base={}
for frame in range(1,59):
 s.frame_set(frame);base[frame]=pb.rotation_quaternion.copy()
def tree(o):
 ev=o.evaluated_get(bpy.context.evaluated_depsgraph_get());me=ev.to_mesh();v=[ev.matrix_world@p.co for p in me.vertices];f=[tuple(x.vertices) for x in me.polygons];ev.to_mesh_clear();return BVHTree.FromPolygons(v,f)
def mask(frame):
 t=max(0,min(1,(frame-1)/8,(58-frame)/8));return t*t*(3-2*t)
def bake(amount):
 for frame,q in base.items():
  pb.rotation_quaternion=q@Quaternion((1,0,0),amount*mask(frame));pb.keyframe_insert(data_path='rotation_quaternion',frame=frame,group=pb.name)
def check():
 hits=[]
 for j in range(115):
  frame=1+j/2;s.frame_set(int(frame),subframe=frame-int(frame));bpy.context.view_layer.update();p=tree(panel)
  for o in legs:
   if p.overlap(tree(o)):hits.append({'frame':frame,'armor':o.name})
 return hits
trials=[];chosen=None
for amount in [0,.04,.08,.12,.16,.20]:
 bake(amount);hits=check();trials.append({'bias_radians':amount,'surface_hits':len(hits),'examples':hits[:20]});print('CLOTH_CLEARANCE_TRIAL',amount,len(hits),flush=True)
 if not hits:chosen=amount;break
assert chosen is not None,'No bounded clearance correction found; native delivery unchanged'
for o,value in hidden:o.hide_viewport=value
r['dash_cloth_R17_applied']=True
bpy.ops.wm.save_as_mainfile(filepath=str(current))
report={'scope':'Right front cloth vs selected leg/hip armor at115half-frame samples in dash; no whole-body, containment or continuous-time certification','chosen_bias_radians':chosen,'chosen_bias_degrees':math.degrees(chosen),'zero_bias_at_clip_endpoints':True,'trials':trials}
(P/'Docs/SanctuaryR3Build/DashClothClearance_R17.json').write_text(json.dumps(report,indent=2));print('DASH_CLOTH_CLEARANCE_R17_COMPLETE',report)
