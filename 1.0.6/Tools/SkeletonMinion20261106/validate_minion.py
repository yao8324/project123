import bpy, sys, json, math
from pathlib import Path
from mathutils import Vector
root=Path(sys.argv[sys.argv.index('--')+1])
doc=root/'Docs/SkeletonMinion20261106'
bpy.ops.wm.open_mainfile(filepath=str(root/'Blender/Minions/Skeleton20261106/Skeleton_Machete.blend'))
assert bpy.data.objects.get('Skeleton_Rig')
assert len(bpy.data.objects['Skeleton_Rig'].data.bones)==27
assert len(bpy.data.actions)==6
assert len(bpy.data.collections['01_EDITABLE_PARTS'].objects)==190
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=str(root/'Assets/Art/Minions/Skeleton/Models/Skeleton_Machete.fbx'))
meshes=[o for o in bpy.context.scene.objects if o.type=='MESH']
arms=[o for o in bpy.context.scene.objects if o.type=='ARMATURE']
assert len(meshes)==6 and len(arms)==1
assert len(arms[0].data.bones)==27
assert len(bpy.data.actions)==6
rows=[]
for o in meshes:
    assert o.vertex_groups and o.data.uv_layers and all(m for m in o.data.materials)
    assert all(any(g.weight>0 for g in v.groups) for v in o.data.vertices)
    assert all(math.isfinite(x) for v in o.data.vertices for x in v.co)
    rows.append({'name':o.name,'vertices':len(o.data.vertices),'triangles':sum(len(p.vertices)-2 for p in o.data.polygons)})
assert not any(o.type in {'LIGHT','CAMERA'} for o in bpy.context.scene.objects)
rig=arms[0]
pose_report=[]
for a in bpy.data.actions:
    rig.animation_data.action=a
    for f in [a.frame_range[0],sum(a.frame_range)/2,a.frame_range[1]]:
        bpy.context.scene.frame_set(int(f))
        for o in meshes:
            evaluated=o.evaluated_get(bpy.context.evaluated_depsgraph_get())
            mesh=evaluated.to_mesh()
            assert all(math.isfinite(x) for v in mesh.vertices for x in v.co)
            evaluated.to_mesh_clear()
    pose_report.append({'name':a.name,'frames':list(a.frame_range)})
report={'passed':True,'bones':len(rig.data.bones),'meshes':rows,'actions':pose_report,'studioObjectsExcluded':True,'allVerticesWeighted':True}
(doc/'export-validation.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
print('SKELETON_ROUNDTRIP_PASS')
