"""Export the approved V6 render geometry to Unity, without editing the source blend.
MLM1: gzip of uint32 magic, vertex count, index count; float32 xyz/normal/uv;
uint32 indices. Static meshes are batched spatially; animated meshes retain poses.
"""
import bpy, numpy as np, json, gzip, struct, re, math, time, sys
sys.path.insert(0, str(__import__("pathlib").Path(__file__).resolve().parent))
from collision_contract import collision_role
from pathlib import Path
from mathutils import Matrix, Vector
ROOT=Path(__file__).resolve().parents[2]
OUT=ROOT/'Assets/Art/MilanLibraryV6'
for directory in [OUT/'Meshes',OUT/'Textures',ROOT/'Docs/MilanV6Unity']:directory.mkdir(parents=True,exist_ok=True)
S=bpy.data.scenes['MilanLibrary_Final']; bpy.context.window.scene=S
bpy.context.window.view_layer=S.view_layers['Full_Collection']; S.frame_set(35)
DG=bpy.context.evaluated_depsgraph_get()
C=Matrix(((1,0,0,0),(0,0,1,0),(0,1,0,0),(0,0,0,1)))
visible=set()
def walk(lc,ok=True):
    ok=ok and not lc.exclude and not lc.collection.hide_render
    if ok: visible.update(o.name for o in lc.collection.objects if not o.hide_render)
    for ch in lc.children:walk(ch,ok)
walk(S.view_layers['Full_Collection'].layer_collection)
def safe(s):return re.sub(r'[^A-Za-z0-9_.-]','_',s)
def vec(v):return [float(v[0]),float(v[2]),float(v[1])]
# Explicit physical proxies are exported independently of rendering visibility.
proxies={o.name for o in S.objects if o.get('collision_role') in {'walkable','barrier'} and o.type=='MESH'}
objects=[o for o in S.objects if (o.name in visible or o.name in proxies) and o.type in {'MESH','CURVE','FONT'}]
# Unexclude only proxy-containing branches for evaluated geometry; this process never saves the source.
def enable_proxy_branches(lc):
    needed=any(o.name in proxies for o in lc.collection.all_objects)
    if needed:lc.exclude=False;lc.hide_viewport=False;lc.collection.hide_viewport=False
    for child in lc.children:enable_proxy_branches(child)
enable_proxy_branches(S.view_layers['Full_Collection'].layer_collection)
for o in objects:
    if o.name in proxies:o.hide_set(False);o.hide_viewport=False
bpy.context.view_layer.update();DG=bpy.context.evaluated_depsgraph_get()
materials=sorted({sl.material.name for o in objects for sl in o.material_slots if sl.material})
matid={n:i for i,n in enumerate(materials)}
def imagefile(im):
    name=safe(im.name)
    if not name.lower().endswith(('.png','.jpg','.jpeg')):name+='.png'
    dest=OUT/'Textures'/name
    if not dest.exists():
        if im.packed_file:dest.write_bytes(im.packed_file.data)
        else:
            src=Path(bpy.path.abspath(im.filepath))
            if src.exists():dest.write_bytes(src.read_bytes())
    return name if dest.exists() else ''
def socketcolor(sock):
    if not sock.is_linked:return list(sock.default_value)
    n=sock.links[0].from_node
    if n.type=='VALTORGB':return list(n.color_ramp.evaluate(.5))
    if n.type=='MIX_RGB':
        a=np.array(socketcolor(n.inputs[1])); b=np.array(socketcolor(n.inputs[2]));f=n.inputs[0].default_value
        return list(a*(1-f)+(a*b if n.blend_type=='MULTIPLY' else b)*f)
    if n.type=='TEX_IMAGE':return [1,1,1,1]
    return [.5,.5,.5,1]
md=[]
for name in materials:
    m=bpy.data.materials[name];p=next((n for n in m.node_tree.nodes if n.type=='BSDF_PRINCIPLED'),None)
    d={'name':name,'color':[.5,.5,.5,1],'metallic':0,'roughness':.6,'emission':[0,0,0,1],'strength':0,'baseTexture':'','normalTexture':'','emissionTexture':''}
    if p:
        d.update(color=socketcolor(p.inputs['Base Color']),metallic=float(p.inputs['Metallic'].default_value),roughness=float(p.inputs['Roughness'].default_value),emission=list(p.inputs['Emission Color'].default_value),strength=float(p.inputs['Emission Strength'].default_value))
        emission_socket=p.inputs['Emission Color']
        if emission_socket.is_linked:
            node=emission_socket.links[0].from_node
            if node.type=='TEX_IMAGE' and node.image:
                d['emissionTexture']=imagefile(node.image);d['emission']=[1,1,1,1]
        imgs=[n.image for n in m.node_tree.nodes if n.type=='TEX_IMAGE' and n.image]
        for im in imgs:
            f=imagefile(im)
            if 'nor_' in im.name.lower() or 'normal' in im.name.lower():d['normalTexture']=f
            elif 'rough' not in im.name.lower():d['baseTexture']=f
        # Reuse previously baked PBR material maps when the live material is procedural.
        if not d['baseTexture']:
            im=bpy.data.images.get(name+'_BaseColor')
            if im:d['baseTexture']=imagefile(im);d['color']=[1,1,1,1]
            im=bpy.data.images.get(name+'_Normal')
            if im:d['normalTexture']=imagefile(im)
        if d['baseTexture'].startswith('wood_'):
            mix=next((n for n in m.node_tree.nodes if n.type=='MIX_RGB'),None)
            if mix:d['color']=list(mix.inputs[2].default_value)
            d['roughness']=.4
    md.append(d)
def animated(o):
    while o:
        if o.animation_data and (o.animation_data.drivers or o.animation_data.action):return True
        o=o.parent
    return False
anim=[o for o in objects if animated(o)]; aid={o.name:i for i,o in enumerate(anim)}
initial={o.name:o.matrix_world.copy() for o in anim}
groups={};chunks=[];mapping=[];cache={};triangle_count=0
def flush(key):
    parts=groups.pop(key,None)
    if not parts:return
    arrays=parts['arrays'];indices=parts['indices'];shift=0;ii=[]
    for a,b in zip(arrays,indices):ii.append(b+shift);shift+=len(a)
    arr=np.concatenate(arrays).astype('<f4');idx=np.concatenate(ii).astype('<u4')
    # Welding retains hard normals and UV seams while shrinking repeated geometry.
    arr,inv=np.unique(arr,axis=0,return_inverse=True);idx=inv[idx].astype('<u4')
    fn=f'mesh_{len(chunks):04d}.mlmesh'
    with gzip.open(OUT/'Meshes'/fn,'wb',compresslevel=3) as f:
        f.write(struct.pack('<III',0x314D4C4D,len(arr),len(idx)));f.write(arr.astype('<f4').tobytes());f.write(idx.tobytes())
    chunks.append({'file':fn,'name':parts['name'],'group':key[0],'material':key[1],'animation':key[2],'vertices':len(arr),'triangles':len(idx)//3,'sources':parts['sources'],'collisionRole':key[4],'collisionOnly':key[5]})
def local_geometry(o):
    key=(o.data.as_pointer(),tuple((m.type,m.show_viewport,m.show_render,getattr(m,'width',0),getattr(m,'segments',0)) for m in o.modifiers))
    if key in cache:return cache[key]
    ev=o.evaluated_get(DG);me=ev.to_mesh(preserve_all_data_layers=True,depsgraph=DG)
    me.calc_loop_triangles()
    if not me.loop_triangles:ev.to_mesh_clear();return None
    pos=np.empty(len(me.vertices)*3,dtype=np.float32);me.vertices.foreach_get('co',pos);pos=pos.reshape(-1,3)
    vi=np.empty(len(me.loops),dtype=np.int32);me.loops.foreach_get('vertex_index',vi)
    norms=np.empty(len(me.corner_normals)*3,dtype=np.float32);me.corner_normals.foreach_get('vector',norms);norms=norms.reshape(-1,3)
    uv=np.zeros((len(me.loops),2),dtype=np.float32)
    if me.uv_layers.active:me.uv_layers.active.data.foreach_get('uv',uv.ravel())
    else:
        # Deterministic box projection for procedural-only surfaces.
        lp=pos[vi];axes=np.argmax(np.abs(norms),axis=1)
        uv[:,0]=np.where(axes==0,lp[:,1],lp[:,0])*.5
        uv[:,1]=np.where(axes==2,lp[:,1],lp[:,2])*.5
    tris=np.empty(len(me.loop_triangles)*3,dtype=np.int32);me.loop_triangles.foreach_get('loops',tris);tris=tris.reshape(-1,3)
    mi=np.empty(len(me.loop_triangles),dtype=np.int32);me.loop_triangles.foreach_get('material_index',mi)
    result=(pos[vi],norms,uv,tris,mi)
    ev.to_mesh_clear()
    if sum(x.nbytes for x in result)<12_000_000 and len(cache)<2200:cache[key]=result
    return result
start=time.time()
for k,o in enumerate(objects):
    data=local_geometry(o)
    if data is None:continue
    pos,norm,uv,tris,mi=data
    M=np.array(o.matrix_world,dtype=np.float32);N=np.linalg.inv(M[:3,:3]).T
    wp=pos@M[:3,:3].T+M[:3,3];wn=norm@N.T;wn/=np.maximum(np.linalg.norm(wn,axis=1,keepdims=True),1e-12)
    arr=np.concatenate((wp[:,[0,2,1]],wn[:,[0,2,1]],uv),axis=1)
    group=next((c.name for c in o.users_collection if c.name in {'V3_ReferenceExterior','Exterior','V5_Magic','V6_Emblem_GoldMount','ASSET_Maynooth_Emblem'}),o.users_collection[0].name if o.users_collection else 'Architecture')
    box=tuple(int(v//12) for v in M[:3,3]);an=aid.get(o.name,-1)
    role=collision_role(o.get('collision_role'),group,an>=0)
    collision_only=o.name not in visible
    if collision_only and role=='none':raise ValueError('Invisible object has no collision role: '+o.name)
    for slot in np.unique(mi):
        mat=o.material_slots[min(int(slot),len(o.material_slots)-1)].material if len(o.material_slots) else None
        im=matid.get(mat.name if mat else '',0);tt=tris[mi==slot]
        # Reflection changes winding; negative-scale source objects cancel it.
        if np.linalg.det(M[:3,:3])>0:tt=tt[:,[0,2,1]]
        used,remap=np.unique(tt.ravel(),return_inverse=True)
        key=(group,im,an,box,role,collision_only)
        p=groups.setdefault(key,{'name':o.name if an>=0 else group+'_'+md[im]['name'],'arrays':[],'indices':[],'sources':[],'count':0})
        p['arrays'].append(arr[used]);p['indices'].append(remap);p['sources'].append(o.name);p['count']+=len(used)
        triangle_count+=len(tt)
        if p['count']>90000:flush(key)
    mapping.append({'name':o.name,'asset_id':str(o.get('asset_id','')),'instance_id':str(o.get('instance_id','')),'zone_id':str(o.get('zone_id','')),'floor':str(o.get('floor','')),'collision_role':role,'collision_only':collision_only})
    if k%1000==0:print('EXPORT',k,len(objects),'chunks',len(chunks),'seconds',round(time.time()-start),flush=True)
for key in list(groups):flush(key)
poses=[{'name':o.name,'poses':[]} for o in anim]
for frame in range(1,242,6):
    S.frame_set(frame)
    for i,o in enumerate(anim):
        delta=C@o.matrix_world@initial[o.name].inverted()@C
        p,q,sc=delta.decompose();poses[i]['poses'].append({'p':list(p),'q':[q.x,q.y,q.z,q.w],'s':list(sc)})
S.frame_set(35)
lights=[]
for o in S.objects:
    if o.type!='LIGHT' or o.name not in visible or o.data.energy<=0:continue
    forward=o.matrix_world.to_quaternion()@Vector((0,0,-1))
    lights.append({'name':o.name,'type':o.data.type,'position':vec(o.matrix_world.translation),'forward':vec(forward),'color':list(o.data.color),'energy':float(o.data.energy),'size':float(getattr(o.data,'size',1))})
cameras=[]
for o in S.objects:
    if o.type=='CAMERA' and (o.name.startswith('CAM_V6') or o.name.startswith('CAM_V3_FacadeDepth')):
        cameras.append({'name':o.name,'position':vec(o.matrix_world.translation),'forward':vec(o.matrix_world.to_quaternion()@Vector((0,0,-1))),'up':vec(o.matrix_world.to_quaternion()@Vector((0,1,0))),'fov':math.degrees(o.data.angle_y)})
manifest={'formatVersion':2,'repairRevision':str(S.get('walkability_revision','')),'source':bpy.data.filepath,'sourceObjects':len(objects),'triangleCount':triangle_count,'materials':md,'chunks':chunks,'animations':poses,'lights':lights,'cameras':cameras,'sampleStep':.25,'duration':10,'frame':35,'axis':'Blender(x,y,z) -> Unity(x,z,y), winding reversed'}
(OUT/'manifest.json').write_text(json.dumps(manifest,ensure_ascii=False))
(ROOT/'Docs/MilanV6Unity/source_inventory.json').write_text(json.dumps(mapping,ensure_ascii=False))
(ROOT/'Docs/MilanV6Unity/export_summary.json').write_text(json.dumps({k:v for k,v in manifest.items() if k not in ['materials','chunks','animations','lights','cameras']},indent=2))
print('DONE',len(chunks),triangle_count,len(anim),'elapsed',time.time()-start,flush=True)
