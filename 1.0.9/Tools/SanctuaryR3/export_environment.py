import bpy,json
from pathlib import Path
P=Path(__file__).resolve().parents[2]
bpy.ops.wm.open_mainfile(filepath=str(P/'Blender/SanctuaryR3/Sanctuary_R3_GateGallery.blend'))
s=bpy.context.scene;out=P/'Assets/Art/Bosses/Knight/SanctuaryR3';out.mkdir(parents=True,exist_ok=True)
# Export evaluated copies in this isolated process; preserve native construction assets.
bpy.ops.object.select_all(action='DESELECT')
for o in s.objects:
 if o.type in ['MESH','CURVE'] and not o.hide_render and not o.name.startswith('GUIDE_'):o.select_set(True)
bpy.context.view_layer.objects.active=next(o for o in s.objects if o.select_get());bpy.ops.object.convert(target='MESH')
meshes=[o for o in bpy.context.selected_objects if o.type=='MESH']
for o in meshes:
 # Isolate object-linked material layouts before FBX export.
 if o.data.users>1:o.data=o.data.copy()
 for slot in o.material_slots:
  material=slot.material;slot.link="DATA";slot.material=material
 me=o.data;uv=me.uv_layers.active or me.uv_layers.new(name='PhysicalBoxUV')
 for face in me.polygons:
  n=o.matrix_world.to_3x3().inverted().transposed()@face.normal;axis=max(range(3),key=lambda i:abs(n[i]));axes={0:(1,2),1:(0,2),2:(0,1)}[axis]
  for li in face.loop_indices:
   v=o.matrix_world@me.vertices[me.loops[li].vertex_index].co;uv.data[li].uv=(v[axes[0]]*.34,v[axes[1]]*.34)
materials=[]
for m in bpy.data.materials:
 if not m.use_nodes:continue
 bs=next((n for n in m.node_tree.nodes if n.type=='BSDF_PRINCIPLED'),None)
 if not bs:continue
 tex=next((n.image.name for n in m.node_tree.nodes if n.type=='TEX_IMAGE' and n.image and '_Albedo' in n.image.name),None)
 materials.append({'name':m.name,'color':list(bs.inputs['Base Color'].default_value),'metallic':bs.inputs['Metallic'].default_value,'roughness':bs.inputs['Roughness'].default_value,'texture':tex or ''})
lights=[{'name':o.name,'position':[o.location.x,o.location.z,o.location.y],'color':list(o.data.color),'energy':o.data.energy} for o in s.objects if o.type=='LIGHT' and o.parent is None]
bpy.ops.export_scene.fbx(filepath=str(out/'Sanctuary_R3_Environment.fbx'),use_selection=True,object_types={'MESH'},bake_anim=False,axis_forward='-Z',axis_up='Y',apply_unit_scale=True,path_mode='RELATIVE')
(out/'EnvironmentMaterials.json').write_text(json.dumps({'materials':materials},indent=2));(P/'Docs/SanctuaryR3Build/EnvironmentExport.json').write_text(json.dumps({'scope':'static geometry with physical UVs; not final engine fidelity','mesh_count':len(meshes),'vertices':sum(len(o.data.vertices) for o in meshes),'polygons':sum(len(o.data.polygons) for o in meshes),'remaining':'LOD, batching, final shader matching, collision and engine performance'},indent=2));print('ENVIRONMENT_FBX_COMPLETE',len(meshes))
