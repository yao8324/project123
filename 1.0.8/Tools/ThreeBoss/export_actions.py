import bpy
from pathlib import Path
rig=bpy.data.objects['Summoner_Rig'];bpy.ops.object.select_all(action='DESELECT');rig.select_set(True);bpy.context.view_layer.objects.active=rig
for key in ['Idle','Invocation']:
 action=bpy.data.actions['Soulbinder_'+key+'_Study'];rig.animation_data_create();rig.animation_data.action=action;rig.animation_data.action_slot=action.slots[0]
 for pb in rig.pose.bones:
  pb.rotation_mode='XYZ'
 bpy.context.scene.frame_start=int(action.frame_range[0]);bpy.context.scene.frame_end=int(action.frame_range[1]);bpy.context.scene.frame_set(bpy.context.scene.frame_start)
 bpy.ops.export_scene.fbx(filepath=str(Path.cwd()/'Assets/Art/ThreeBoss/Imported'/('Soulbinder_'+key+'.fbx')),use_selection=True,object_types={'ARMATURE'},add_leaf_bones=False,bake_anim=True,bake_anim_use_all_actions=False,bake_anim_use_nla_strips=False,axis_forward='-Z',axis_up='Y')
 print('ACTION_EXPORTED',key)
