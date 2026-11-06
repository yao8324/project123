import bpy, sys, json, math
from pathlib import Path
root=Path(sys.argv[sys.argv.index('--')+1]);doc=root/'Docs/SkeletonMinion20261106'
specs=[('Skeleton','Skeleton_Archer',27,7,7),('Skeleton','Skeleton_Hammer',27,6,7),('Skeleton','Skeleton_SwordShield',27,7,7),('Undead','Undead_Winged',10,3,4)]
report={}
for folder,name,bones,meshes,actions in specs:
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=str(root/f'Assets/Art/Minions/{folder}/Models/{name}.fbx'))
    ms=[o for o in bpy.context.scene.objects if o.type=='MESH'];arms=[o for o in bpy.context.scene.objects if o.type=='ARMATURE']
    assert len(ms)==meshes and len(arms)==1,(name,len(ms),len(arms))
    assert len(arms[0].data.bones)==bones and len(bpy.data.actions)==actions,name
    for o in ms:
        assert o.data.uv_layers and all(m for m in o.data.materials)
        assert all(v.groups and any(g.weight>0 for g in v.groups) for v in o.data.vertices),o.name
    for action in bpy.data.actions:
        arms[0].animation_data.action=action
        bpy.context.scene.frame_set(int(sum(action.frame_range)/2))
        for o in ms:
            evaluated=o.evaluated_get(bpy.context.evaluated_depsgraph_get());mesh=evaluated.to_mesh()
            assert all(math.isfinite(x) for v in mesh.vertices for x in v.co),(name,action.name)
            evaluated.to_mesh_clear()
    assert not any(o.type in {'LIGHT','CAMERA'} for o in bpy.context.scene.objects)
    report[name]={'passed':True,'bones':bones,'meshes':meshes,'actions':actions,'allVerticesWeighted':True}
for name in ['Skeleton_Arrow','Undead_Fireball','Boss_PurpleSkull','Boss_MagicDagger']:
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=str(root/f'Assets/Art/Minions/Projectiles/Models/{name}.fbx'))
    ms=[o for o in bpy.context.scene.objects if o.type=='MESH']
    assert ms and all(o.data.uv_layers and o.data.materials for o in ms),name
    assert not any(o.type in {'LIGHT','CAMERA','ARMATURE'} for o in bpy.context.scene.objects)
    report[name]={'passed':True,'meshes':len(ms)}
(doc/'variants-validation.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
print('ALL_VARIANTS_ROUNDTRIP_PASS')
