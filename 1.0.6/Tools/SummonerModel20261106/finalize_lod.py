"""Keep the reduced FBX standalone: avoid Unity's automatic _LOD1 name heuristic."""
import bpy, json, sys
from pathlib import Path
root=Path(sys.argv[sys.argv.index('--')+1])
path=root/'Blender/Bosses/Summoner/OverlordStudy20261106/Soulbinder_OverlordStudy.blend'
bpy.ops.wm.open_mainfile(filepath=str(path))
col=bpy.data.collections['07_LOD1_EXPORT_ALTERNATIVE'];col.hide_viewport=False;col.hide_render=False
for o in col.objects:o.name=o.name.removesuffix('_LOD1') if o.name.endswith('_LOD1') else o.name;o.name=o.name if o.name.endswith('_Reduced') else o.name+'_Reduced'
bpy.ops.object.select_all(action='DESELECT')
rig=bpy.data.objects['Summoner_Rig'];rig.select_set(True)
for o in col.objects:o.select_set(True)
bpy.context.view_layer.objects.active=rig
bpy.ops.export_scene.fbx(filepath=str(root/'Assets/Art/Bosses/Summoner/Models/Soulbinder_OverlordStudy_LOD1.fbx'),use_selection=True,object_types={'ARMATURE','MESH'},add_leaf_bones=False,bake_anim=False,axis_forward='-Z',axis_up='Y',apply_scale_options='FBX_SCALE_UNITS',mesh_smooth_type='FACE',use_mesh_modifiers=True)
col.hide_viewport=True;col.hide_render=True
bpy.ops.object.select_all(action='DESELECT');bpy.data.objects['Soulbinder_Body_Skinned'].select_set(True);bpy.context.view_layer.objects.active=bpy.data.objects['Soulbinder_Body_Skinned']
bpy.ops.wm.save_as_mainfile(filepath=str(path),compress=True)
p=root/'Docs/SummonerModel20261106/model-report.json';report=json.loads(p.read_text(encoding='utf-8'))
for key in list(report['gameMeshes']):
    if key.endswith('_LOD1'):report['gameMeshes'][key.removesuffix('_LOD1')+'_Reduced']=report['gameMeshes'].pop(key)
p.write_text(json.dumps(report,ensure_ascii=False,indent=2),encoding='utf-8')
