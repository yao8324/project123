"""Export the current native knight without regenerating or rendering geometry."""
import bpy
from pathlib import Path
P=Path(__file__).resolve().parents[2]
bpy.ops.wm.open_mainfile(filepath=str(P/'Blender/SanctuaryR3/Paladin_R3_Animated.blend'))
s=bpy.context.scene;r=bpy.data.objects['Paladin_R3_Rig']
r.data.pose_position='POSE';bpy.ops.object.select_all(action='DESELECT');r.select_set(True)
for o in s.objects:
 if o.type=='MESH' and any(m.type=='ARMATURE' and m.object==r for m in o.modifiers):o.select_set(True)
bpy.context.view_layer.objects.active=r
bpy.ops.export_scene.fbx(filepath=str(P/'Assets/Art/Bosses/Knight/SanctuaryR3/Paladin_R3_Animated.fbx'),use_selection=True,object_types={'MESH','ARMATURE'},add_leaf_bones=False,bake_anim=True,bake_anim_use_all_actions=True,bake_anim_use_nla_strips=False,bake_anim_simplify_factor=0,axis_forward='-Z',axis_up='Y',apply_unit_scale=True)

print('CURRENT_CHARACTER_EXPORT_COMPLETE')
