import bpy, json, sys, math
from pathlib import Path
from mathutils import Vector, Matrix
args=sys.argv[sys.argv.index('--')+1:];kind=args[0];out=Path(args[1]);out.mkdir(parents=True,exist_ok=True)
rig=next((o for o in bpy.context.scene.objects if o.type=='ARMATURE'),None) if kind=='Soulbinder' else None
if rig:
 rig.animation_data_clear()
 for b in rig.pose.bones:b.matrix_basis=Matrix.Identity(4)
 bpy.context.view_layer.update()
objects=[]
for o in list(bpy.context.scene.objects):
 if o.type not in {'MESH','CURVE','FONT'} or o.hide_render:continue
 if kind=='Soulbinder':
  if any(s in o.name.lower() for s in ['studio','floor','plinth','ground','reference','camera','backdrop']):continue
  if max(o.dimensions)>12:continue
 objects.append(o)
mats={};vertices=[];faces=[];face_mats=[];weights={}
deps=bpy.context.evaluated_depsgraph_get()
for o in objects:
 arm=[m for m in o.modifiers if m.type=='ARMATURE']
 for m in arm:m.show_viewport=False
 bpy.context.view_layer.update()
 ev=o.evaluated_get(bpy.context.evaluated_depsgraph_get());me=ev.to_mesh();matrix=(rig.matrix_world.inverted()@o.matrix_world) if rig else o.matrix_world
 offset=len(vertices);vertices.extend([matrix@v.co for v in me.vertices]);local=[]
 for slot in o.material_slots:
  mat=slot.material
  if mat and mat.name not in mats:mats[mat.name]=mat
  local.append(mat.name if mat else '')
 for poly in me.polygons:
  faces.append([offset+i for i in poly.vertices]);face_mats.append(local[min(poly.material_index,len(local)-1)] if local else '')
 if rig:
  groups={g.index:g.name for g in o.vertex_groups}
  rigid=o.parent_bone if o.parent_type=='BONE' else None
  for v in me.vertices:
   vw=[(groups[g.group],g.weight) for g in v.groups if g.group in groups and groups[g.group] in rig.data.bones]
   if not vw:vw=[(rigid or rig.data.bones[0].name,1)]
   for bone,w in vw:weights.setdefault(bone,[]).append((offset+v.index,w))
 ev.to_mesh_clear()
 for m in arm:m.show_viewport=True
mesh=bpy.data.meshes.new(kind+'_Runtime');mesh.from_pydata(vertices,[],faces);mesh.update()
obj=bpy.data.objects.new(kind+'_Runtime',mesh);bpy.context.collection.objects.link(obj)
keys=list(mats)
for mat in mats.values():mesh.materials.append(mat)
for poly,name in zip(mesh.polygons,face_mats):poly.material_index=keys.index(name) if name in mats else 0
if rig:
 obj.matrix_world=rig.matrix_world
 for bone,ws in weights.items():
  vg=obj.vertex_groups.new(name=bone)
  for i,w in ws:vg.add([i],w,'REPLACE')
 mod=obj.modifiers.new('Rig','ARMATURE');mod.object=rig;obj.parent=rig
 # Remove review-only actions from exported clips.
 for a in list(bpy.data.actions):
  if 'Study' not in a.name:bpy.data.actions.remove(a)
 rig.animation_data_create();rig.animation_data.action=next(iter(bpy.data.actions),None)
# Single renderer plus material submeshes avoids thousands of per-object draws.
bpy.ops.object.select_all(action='DESELECT');obj.select_set(True)
if rig:rig.select_set(True)
bpy.context.view_layer.objects.active=obj
materials=[]
for name,mat in mats.items():
 bs=next((n for n in mat.node_tree.nodes if n.type=='BSDF_PRINCIPLED'),None) if mat.use_nodes else None
 color=list(bs.inputs['Base Color'].default_value) if bs else list(mat.diffuse_color)
 metal=float(bs.inputs['Metallic'].default_value) if bs else mat.metallic
 rough=float(bs.inputs['Roughness'].default_value) if bs else mat.roughness
 em=list(bs.inputs['Emission Color'].default_value) if bs else [0,0,0,1]
 strength=float(bs.inputs['Emission Strength'].default_value) if bs else 0
 materials.append(dict(name=name,color=color,metallic=metal,roughness=rough,emission=[c*strength for c in em[:3]]))
(out/(kind+'_materials.json')).write_text(json.dumps({'materials':materials},indent=2))
bpy.ops.export_scene.fbx(filepath=str(out/(kind+'.fbx')),use_selection=True,object_types={'MESH','ARMATURE'},add_leaf_bones=False,bake_anim=bool(rig),bake_anim_use_all_actions=bool(rig),bake_anim_use_nla_strips=False,axis_forward='-Z',axis_up='Y',use_mesh_modifiers=True)
print('EXPORTED',kind,'vertices',len(vertices),'faces',len(faces),'materials',len(mats))
