"""Sample actual feather/body surfaces; report failures without treating them as acceptance."""
import bpy,json
from pathlib import Path
from mathutils.bvhtree import BVHTree
P=Path(__file__).resolve().parents[2];bpy.ops.wm.open_mainfile(filepath=str(P/'Blender/SanctuaryR3/Paladin_R3_Animated.blend'));s=bpy.context.scene;r=bpy.data.objects['Paladin_R3_Rig']
feathers=[o for o in s.objects if o.type=='MESH' and o.name.startswith('W01_') and any(k in o.name for k in ['Primary','Secondary','Covert'])]
body=[o for o in s.objects if o.type=='MESH' and o.name.startswith(('R01_R7_OpenVisor','R02_CurvedBreast','R02_SeparateBack','R03_LayeredShoulder','R03_R7_PointShoulder','R03_R11_ShoulderShell'))]
def tree(o,dg):
 ev=o.evaluated_get(dg);me=ev.to_mesh();v=[ev.matrix_world@p.co for p in me.vertices];f=[tuple(p.vertices) for p in me.polygons];ev.to_mesh_clear();return BVHTree.FromPolygons(v,f)
reports=[]
for name,end in [('A01_SlowWalk',67),('A02_Dash',58),('A03_JumpAttack',97)]:
 r.animation_data.action=bpy.data.actions[name];hits=[]
 for k in range(13):
  frame=1+(end-1)*k/12;s.frame_set(int(frame),subframe=frame-int(frame));bpy.context.view_layer.update();dg=bpy.context.evaluated_depsgraph_get();bs=[(o.name,tree(o,dg)) for o in body]
  for o in feathers:
   t=tree(o,dg)
   for n,b in bs:
    if t.overlap(b):hits.append({'frame':frame,'feather':o.name,'body':n})
 reports.append({'clip':name,'sampled_poses':13,'surface_overlap_records':len(hits),'hits':hits})
(P/'Docs/SanctuaryR3Build/WingBodyContactR9.json').write_text(json.dumps({'scope':'13poses/clip feather vs selected torso/head/shoulder surfaces only; no containment, inter-feather or continuous-time guarantee','reports':reports},indent=2));print('WING_BODY_CONTACT',[(x['clip'],x['surface_overlap_records']) for x in reports])
