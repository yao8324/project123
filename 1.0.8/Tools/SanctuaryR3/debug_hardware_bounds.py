import bpy
from mathutils import Vector
bpy.ops.wm.open_mainfile(filepath='/Users/jack/Desktop/虚拟环境团队/圣骑士场地_20261107/1.0.8/Blender/SanctuaryR3/Paladin_R3_Animated.blend')
s=bpy.context.scene;r=bpy.data.objects['Paladin_R3_Rig'];r.animation_data.action=bpy.data.actions['A03_JumpAttack'];s.frame_set(40);bpy.context.view_layer.update();dg=bpy.context.evaluated_depsgraph_get()
for o in s.objects:
 if o.type=='MESH' and o.name.startswith(('W01','W02')):
  ev=o.evaluated_get(dg);me=ev.to_mesh();pts=[ev.matrix_world@v.co for v in me.vertices]
  if min(v.z for v in pts)<3:print('BAD',o.name,[g.name for g in o.vertex_groups],[(m.name,m.type) for m in o.modifiers],list(o.matrix_world.translation),min(v.z for v in pts))
  ev.to_mesh_clear()
print('ROOT',r.pose.bones['Root'].matrix.translation,'WingDeform',[(b.name,b.use_deform) for b in r.data.bones if b.name.startswith('Wing')])
