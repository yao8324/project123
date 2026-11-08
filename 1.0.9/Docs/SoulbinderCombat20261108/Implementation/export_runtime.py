import bpy,sys,os,json,math
from mathutils import Vector
base=os.path.abspath(os.path.join(os.path.dirname(__file__),'../../..'))
out=os.path.join(base,'Assets/Art/SoulbinderRuntime');os.makedirs(out,exist_ok=True)
items=[('Hall','Blender/SceneBuild20261107/SoulbinderHall/SoulbinderHall_Detailed_R10.blend')]+[(n,'Blender/CharacterRebuild20261107/Models/'+n+'_TargetRebuild_R'+str(r)+'.blend') for n,r in [('Soulbinder',18),('Cleaver',10),('SwordShield',9),('Archer',14),('Hammer',10),('Wraith',15)]]
if "--hall" in sys.argv: items=items[:1]
allreport=[]
for name,path in items:
 bpy.ops.wm.open_mainfile(filepath=os.path.join(base,path))
 dg=bpy.context.evaluated_depsgraph_get();groups={};mats={};manifest=[]
 for o in list(bpy.context.scene.objects):
  if o.type not in {'MESH','CURVE','SURFACE'} or o.hide_render:continue
  low=o.name.lower()
  if name!='Hall' and (any(w in low for w in ['studio','floor','ground','pedestal','plinth','backdrop','turntable']) or max(o.dimensions)>20):continue
  if name=='Hall' and (any(w in low for w in ['mist','haze','fog','atmosphere','vapor']) or any(any(w in m.name.lower() for w in ['volume','haze','mist','ribbon']) for m in o.data.materials if m)):continue
  ev=o.evaluated_get(dg)
  try: me=ev.to_mesh()
  except:continue
  if me is None:continue
  me.calc_loop_triangles()
  center=o.matrix_world.translation
  key='Body' if name!='Hall' else 'Cell_%s_%s_%s'%(math.floor(center.x/8),math.floor(center.y/8),math.floor(center.z/5))
  g=groups.setdefault(key,[[],[],[]]);verts,faces,mi=g;off=len(verts)
  verts.extend(tuple(o.matrix_world@v.co) for v in me.vertices)
  for t in me.loop_triangles:
   faces.append(tuple(off+i for i in t.vertices))
   mat=me.materials[t.material_index] if len(me.materials)>t.material_index else None
   if mat and mat.name not in mats:mats[mat.name]=mat
   mi.append(mat.name if mat else '')
  ev.to_mesh_clear()
 bpy.ops.object.select_all(action='DESELECT');created=[]
 for key,(verts,faces,mi) in groups.items():
  mesh=bpy.data.meshes.new(name+'_'+key);mesh.from_pydata(verts,[],faces);mesh.update()
  obj=bpy.data.objects.new(name+'_'+key,mesh);bpy.context.collection.objects.link(obj)
  used=list(dict.fromkeys(mi));idx={s:i for i,s in enumerate(used)}
  for s in used:mesh.materials.append(mats.get(s) or bpy.data.materials.new('Default'))
  for p,s in zip(mesh.polygons,mi):p.material_index=idx[s];p.use_smooth=(name!='Hall')
  # World planar UVs for stone/cloth texture transfer.
  uv=mesh.uv_layers.new(name='UVMap')
  for p in mesh.polygons:
   axis=max(range(3),key=lambda a:abs(p.normal[a]));axes=[a for a in range(3) if a!=axis]
   for li in p.loop_indices:
    co=mesh.vertices[mesh.loops[li].vertex_index].co;uv.data[li].uv=(co[axes[0]]*.4,co[axes[1]]*.4)
  budget=(480000 if name=='Hall' else 85000)*len(faces)/max(1,sum(len(x[1]) for x in groups.values()))
  if len(faces)>budget:
   dec=obj.modifiers.new('Runtime polygon budget','DECIMATE');dec.ratio=max(.04,budget/len(faces))
  created.append(obj)
 for obj in created:obj.select_set(True)
 bpy.context.view_layer.objects.active=created[0]
 bpy.ops.export_scene.fbx(filepath=os.path.join(out,name+'.fbx'),use_selection=True,object_types={'MESH'},axis_forward='-Z',axis_up='Y',apply_unit_scale=True,use_mesh_modifiers=True,bake_anim=False,add_leaf_bones=False,path_mode='STRIP')
 for s,m in mats.items():
  c=list(m.diffuse_color);metal=0.;rough=.65;em=0.
  if m.use_nodes:
   for nd in m.node_tree.nodes:
    if nd.type=='BSDF_PRINCIPLED':
     c=list(nd.inputs['Base Color'].default_value);metal=nd.inputs['Metallic'].default_value;rough=nd.inputs['Roughness'].default_value
     em=nd.inputs['Emission Strength'].default_value
  manifest.append({'name':s,'color':c,'metallic':metal,'roughness':rough,'emission':min(em,2.)})
 with open(os.path.join(out,name+'.json'),'w') as f:json.dump({'materials':manifest},f)
 report={'name':name,'source':path,'objects':len(created),'source_triangles':sum(len(x[1]) for x in groups.values()),'fbx_bytes':os.path.getsize(os.path.join(out,name+'.fbx'))}
 allreport.append(report);print('EXPORTED',report,flush=True)
with open(os.path.join(out,'export-report.json'),'w') as f:json.dump(allreport,f,indent=2)
