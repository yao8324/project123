import bpy
from pathlib import Path
P=Path(__file__).resolve().parents[2];bpy.ops.wm.open_mainfile(filepath=str(P/'Blender/SanctuaryR3/Paladin_R3_Animated.blend'));s=bpy.context.scene;r=bpy.data.objects['Paladin_R3_Rig'];r.animation_data.action=bpy.data.actions['A01_SlowWalk'];s.frame_set(34);bpy.context.view_layer.update();dg=bpy.context.evaluated_depsgraph_get();rows=[]
for o in s.objects:
 if o.type=='MESH' and o.name.startswith('W01_R_'):
  ev=o.evaluated_get(dg);me=ev.to_mesh();xs=[(ev.matrix_world@v.co).x for v in me.vertices];rows.append((max(xs),o.name));ev.to_mesh_clear()
print(sorted(rows,reverse=True))
