"""Measure actual deformed embroidery distance to its evaluated cloth panel."""
import bpy,json
from pathlib import Path
from mathutils.bvhtree import BVHTree
P=Path(__file__).resolve().parents[2];bpy.ops.wm.open_mainfile(filepath=str(P/'Blender/SanctuaryR3/Paladin_R3_Animated.blend'));s=bpy.context.scene;r=bpy.data.objects['Paladin_R3_Rig']
parts=[o for o in s.objects if o.name.startswith('C01_R17_WovenLaurel_')]
assert len(parts)==4
weight_error=max(abs(sum(g.weight for g in v.groups)-1) for o in parts for v in o.data.vertices)
reports=[]
def geometry(o,dg):
 ev=o.evaluated_get(dg);me=ev.to_mesh();v=[ev.matrix_world@p.co for p in me.vertices];faces=[tuple(f.vertices) for f in me.polygons];ev.to_mesh_clear();return v,faces
for name,end in [('A01_SlowWalk',67),('A02_Dash',58),('A03_JumpAttack',97)]:
 r.animation_data.action=bpy.data.actions[name];worst={'gap_m':0}
 for k in range(13):
  frame=1+(end-1)*k/12;s.frame_set(int(frame),subframe=frame-int(frame));bpy.context.view_layer.update();dg=bpy.context.evaluated_depsgraph_get()
  for o in parts:
   panel=bpy.data.objects['C01_FoldedPanel_'+o.name.split('C01_R17_WovenLaurel_')[1]]
   pv,pf=geometry(panel,dg);tree=BVHTree.FromPolygons(pv,pf);v,_=geometry(o,dg)
   for point in v:
    distance=tree.find_nearest(point)[3]
    if distance>worst['gap_m']:worst={'gap_m':distance,'frame':frame,'part':o.name}
 reports.append({'clip':name,'sampled_poses':13,'worst':worst})
report={'scope':'Actual evaluated embroidery vertices to nearest parent cloth surface; 13 poses/action. No penetration direction or whole-body collision guarantee.','normalized_weight_max_error':weight_error,'max_allowed_gap_m':.005,'reports':reports}
(P/'Docs/SanctuaryR3Build/ClothReliefCheck_R17.json').write_text(json.dumps(report,indent=2));print('CLOTH_RELIEF_CHECK',report)
assert weight_error<1e-5
assert all(x['worst']['gap_m']<.005 for x in reports)
