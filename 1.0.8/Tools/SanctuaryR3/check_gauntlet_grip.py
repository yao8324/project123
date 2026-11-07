import bpy,json
from pathlib import Path
from mathutils.bvhtree import BVHTree
P=Path(__file__).resolve().parents[2];bpy.ops.wm.open_mainfile(filepath=str(P/'Blender/SanctuaryR3/Paladin_R3_Animated.blend'));s=bpy.context.scene;r=bpy.data.objects['Paladin_R3_Rig'];hand=[o for o in s.objects if o.type=='MESH' and o.name.startswith(('R04_R12_LeftFinger','R04_R12_LeftThumb','R04_R12_LeftPalm'))];grip=bpy.data.objects['S02_R12_ShieldLeatherGrip']
def tree(o,dg):
 ev=o.evaluated_get(dg);me=ev.to_mesh();v=[ev.matrix_world@x.co for x in me.vertices];f=[tuple(x.vertices) for x in me.polygons];ev.to_mesh_clear();return BVHTree.FromPolygons(v,f)
reports=[]
for name,end in [('A01_SlowWalk',67),('A02_Dash',58),('A03_JumpAttack',97)]:
 r.animation_data.action=bpy.data.actions[name];hits=[]
 for k in range(13):
  frame=1+(end-1)*k/12;s.frame_set(int(frame),subframe=frame-int(frame));bpy.context.view_layer.update();dg=bpy.context.evaluated_depsgraph_get();g=tree(grip,dg)
  for o in hand:
   if tree(o,dg).overlap(g):hits.append({'frame':frame,'hand':o.name})
 reports.append({'clip':name,'sampled_poses':13,'surface_overlap_records':len(hits),'hits':hits})
(P/'Docs/SanctuaryR3Build/GauntletGripCheckR12.json').write_text(json.dumps({'scope':'Selected left hand vs shield handle surface overlap only,13poses/clip; no containment/all-equipment/continuous-time guarantee','reports':reports},indent=2));print([(x['clip'],x['surface_overlap_records']) for x in reports])
