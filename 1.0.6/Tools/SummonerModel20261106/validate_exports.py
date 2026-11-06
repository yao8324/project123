"""Read-only round-trip verification of this delivery's Blender and FBX files."""
import bpy, json, sys, math, struct
from pathlib import Path
ROOT=Path(sys.argv[sys.argv.index('--')+1])
doc=ROOT/'Docs/SummonerModel20261106'
source=ROOT/'Blender/Bosses/Summoner/OverlordStudy20261106/Soulbinder_OverlordStudy.blend'
export=ROOT/'Assets/Art/Bosses/Summoner/Models'
report={'complete':False,'source':{},'exports':{}}
bpy.ops.wm.open_mainfile(filepath=str(source))
rig=bpy.data.objects['Summoner_Rig']
assert len(rig.data.bones)==50
assert bpy.data.objects.get('Grimoire_Open_Separate_Prop')
assert len(bpy.data.actions)>=2
materials=[]
for m in bpy.data.materials:
    if m.name.startswith('STUDIO_'):continue
    node=m.node_tree.nodes.get('Principled BSDF')
    materials.append({'name':m.name,'baseColor':list(node.inputs['Base Color'].default_value),'metallic':node.inputs['Metallic'].default_value,'roughness':node.inputs['Roughness'].default_value,'emission':node.inputs['Emission Strength'].default_value})
(doc/'material-manifest.json').write_text(json.dumps(materials,ensure_ascii=False,indent=2),encoding='utf-8')
report['source']={'bones':len(rig.data.bones),'actions':[a.name for a in bpy.data.actions],'separateBook':True,'missingExternalFiles':[]}
for image in bpy.data.images:
    if image.source=='FILE' and not image.packed_file and image.filepath and not Path(bpy.path.abspath(image.filepath)).exists():report['source']['missingExternalFiles'].append(image.filepath)
assert not report['source']['missingExternalFiles']
for filename in ['Soulbinder_OverlordStudy.fbx','Soulbinder_OverlordStudy_LOD1.fbx']:
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=str(export/filename))
    meshes=[o for o in bpy.context.scene.objects if o.type=='MESH']
    arms=[o for o in bpy.context.scene.objects if o.type=='ARMATURE']
    assert len(meshes)==2,(filename,len(meshes))
    assert len(arms)==1 and len(arms[0].data.bones)>=50
    rows=[]
    for o in meshes:
        assert o.vertex_groups and o.data.uv_layers and all(m for m in o.data.materials)
        assert all(math.isfinite(a) for v in o.data.vertices for a in v.co)
        unweighted=sum(not any(g.weight>0 for g in v.groups) for v in o.data.vertices)
        assert unweighted==0,(filename,o.name,unweighted)
        rows.append({'name':o.name,'vertices':len(o.data.vertices),'triangles':sum(len(p.vertices)-2 for p in o.data.polygons),'unweightedVertices':unweighted,'uvLayers':len(o.data.uv_layers)})
    actions=[a.name for a in bpy.data.actions]
    if 'LOD1' not in filename:assert len(actions)>=2,actions
    report['exports'][filename]={'meshCount':len(meshes),'bones':len(arms[0].data.bones),'actions':actions,'meshes':rows,'studioObjectsPresent':any(o.type in {'CAMERA','LIGHT'} for o in bpy.context.scene.objects)}
    assert not report['exports'][filename]['studioObjectsPresent']
with (export/'Soulbinder_OverlordStudy.glb').open('rb') as f:
    magic,version,total=struct.unpack('<4sII',f.read(12));length,kind=struct.unpack('<II',f.read(8));data=json.loads(f.read(length))
assert magic==b'glTF' and version==2 and len(data.get('meshes',[]))==2
report['exports']['Soulbinder_OverlordStudy.glb']={'meshCount':len(data['meshes']),'skins':len(data.get('skins',[])),'cameras':len(data.get('cameras',[]))}
report['complete']=True
(doc/'export-validation.json').write_text(json.dumps(report,ensure_ascii=False,indent=2),encoding='utf-8')
print('SOULBINDER_EXPORT_VALIDATION_PASS')
