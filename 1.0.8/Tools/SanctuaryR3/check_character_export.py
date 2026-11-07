import bpy,json
from pathlib import Path
P=Path(__file__).resolve().parents[2]
bpy.ops.wm.open_mainfile(filepath=str(P/'Blender/SanctuaryR3/Paladin_R3_Animated.blend'))
rig=bpy.data.objects['Paladin_R3_Rig'];meshes=[o for o in bpy.context.scene.objects if o.type=='MESH' and o.parent==rig]
native_count=len(meshes);native_bones=len(rig.data.bones)
checks={'native_72_bones':native_bones==72,'native_12_cloth_bones':sum(b.name.startswith('Cloth.') for b in rig.data.bones)==12,'all_native_meshes_skinned':native_count>0 and all(any(m.type=='ARMATURE' and m.object==rig for m in o.modifiers) for o in meshes),'native_38_feather_bones':sum(b.name.startswith('Feather_') for b in rig.data.bones)==38}
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=str(P/'Assets/Art/Bosses/Knight/SanctuaryR3/Paladin_R3_Animated.fbx'))
rigs=[o for o in bpy.context.scene.objects if o.type=='ARMATURE'];meshes=[o for o in bpy.context.scene.objects if o.type=='MESH'];actions=[a.name for a in bpy.data.actions]
checks['roundtrip_one_armature']=len(rigs)==1;checks['roundtrip_bone_count']=len(rigs[0].data.bones)==native_bones if rigs else False;checks['roundtrip_preserves_native_mesh_count']=len(meshes)==native_count;checks['roundtrip_three_actions']=all(any(label in a for a in actions) for label in ['A01_SlowWalk','A02_Dash','A03_JumpAttack'])
report={'scope':'native skinning and FBX roundtrip structure only, not Unity/runtime/visual acceptance','checks':checks,'imported_actions':actions,'imported_mesh_count':len(meshes)}
(P/'Docs/SanctuaryR3Build/CharacterExportCheck.json').write_text(json.dumps(report,indent=2));print('CHARACTER_EXPORT_CHECK',report);assert all(checks.values())
