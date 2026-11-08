import bpy,json
from pathlib import Path
P=Path(__file__).resolve().parents[2];bpy.ops.wm.open_mainfile(filepath=str(P/'Blender/SanctuaryR3/Paladin_R3_Animated.blend'));r=bpy.data.objects['Paladin_R3_Rig'];r.data.pose_position='REST';bpy.context.view_layer.update();dg=bpy.context.evaluated_depsgraph_get();out={}
for label,prefix in [('helmet','R01_R7_OpenVisorHelmet'),('front_cloth','C01_FoldedPanel_Front'),('rear_cloth','C01_FoldedPanel_Rear')]:
 pts=[]
 for o in bpy.context.scene.objects:
  if o.type=='MESH' and o.name.startswith(prefix):
   ev=o.evaluated_get(dg);me=ev.to_mesh();pts.extend(ev.matrix_world@v.co for v in me.vertices);ev.to_mesh_clear()
 low=[min(p[i] for p in pts) for i in range(3)];high=[max(p[i] for p in pts) for i in range(3)];out[label]={'min_xyz_m':low,'max_xyz_m':high,'size_xyz_m':[high[i]-low[i] for i in range(3)]}
(P/'Docs/SanctuaryR3Build/MeasuredDimensions_R17.json').write_text(json.dumps({'scope':'Evaluated rest-pose geometry in Blender meters; XYZ means width/depth/height, no animation envelope','parts':out},indent=2));print(out)
