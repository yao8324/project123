"""Append the two native module exports to a clean Blender scene and test dependencies."""
import bpy,json,hashlib
from pathlib import Path
P=Path(__file__).resolve().parents[1];s=bpy.context.scene
for fname,collection in [('V6_Updated_Workstations.blend','V6_EXPORT_Updated_Workstations'),('V6_ShelfMagic_Overlay.blend','V6_EXPORT_ShelfMagic_Overlay'),('V6_DirectMountedEmblem.blend','V6_EXPORT_DirectMountedEmblem')]:
    with bpy.data.libraries.load(str(P/'Exports'/fname),link=False) as (a,b):
        assert collection in a.collections
        b.collections=[collection]
    s.collection.children.link(b.collections[0])
names=['V5_FloatingBook','V5_TurningPage_1','V5_StarOrbit_0']
names += [o.name for o in s.objects if o.name.startswith('V6_ArchiveFloat_')]
samples={};invalid=[]
for f in [1,61,241]:
    s.frame_set(f);bpy.context.view_layer.update()
    samples[f]={n:[v for row in bpy.data.objects[n].matrix_world for v in row] for n in names}
    invalid += [o.name for o in s.objects if o.animation_data and any(not d.driver.is_valid for d in o.animation_data.drivers)]
err=max(abs(a-b) for n in samples[1] for a,b in zip(samples[1][n],samples[241][n]))
changed=all(any(abs(a-b)>.00001 for a,b in zip(samples[1][n],samples[61][n])) for n in names)
images=[i for i in bpy.data.images if i.source=='FILE' and i.users]
missing=[i.name for i in images if not i.packed_file or not i.size[0]]
r={'tested_library_sha256':{n:hashlib.sha256((P/'Exports'/n).read_bytes()).hexdigest() for n in ['V6_Updated_Workstations.blend','V6_ShelfMagic_Overlay.blend','V6_DirectMountedEmblem.blend']},'appended_objects':len(s.objects),'station_roots':[n for n in ('R04_WalnutCounter','R03_WalnutCounter') if n in s.objects],
    'tested_animated_objects':names,'motion_observed':changed,'loop_error':err,'invalid_object_drivers':sorted(set(invalid)),
    'packed_image_count':len(images),'missing_images':missing,'native_only':True}
r['pass']=len(r['station_roots'])==2 and changed and err<.001 and not invalid and not missing
r['direct_mount_emblem_complete']=all(n in s.objects for n in ['MN01_Shield_Back','V6_Emblem_OuterGoldFillet','V6_Maynooth_GoldWallLetters']) and 'EX11_EmblemWall' not in bpy.data.objects
r['pass']=r['pass'] and r['direct_mount_emblem_complete']
(P/'Audit/module_append_validation.json').write_text(json.dumps(r,ensure_ascii=False,indent=2));print(json.dumps(r,ensure_ascii=False))
