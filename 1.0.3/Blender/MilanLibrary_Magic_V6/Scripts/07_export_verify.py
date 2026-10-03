"""Run only in an independent Blender process opened on the saved V6 assembly."""
import bpy,json,runpy,csv,hashlib,math
from pathlib import Path
from mathutils import Vector
P=Path(__file__).resolve().parents[1];s=bpy.data.scenes['MilanLibrary_Final'];bpy.context.window.scene=s
s.frame_set(35);bpy.context.view_layer.update()
removed=json.loads((P/'Audit/removed_obsolete_objects.json').read_text())
report={'opened_filepath':bpy.data.filepath,'blender_version':bpy.app.version_string,
    'scene_objects':len(s.objects),'removed_names_absent':all(r['name'] not in bpy.data.objects for r in removed),
    'uniform_rigs':{n:list(bpy.data.objects[n].scale) for n in ['V6_R04_MagicScaleRig','V6_R03_MagicScaleRig']},
    'all_used_file_images_packed':all(im.packed_file for im in bpy.data.images if im.source=='FILE' and im.users),
    'all_used_external_fonts_packed':all(f.packed_file for f in bpy.data.fonts if f.users and f.filepath not in ('','<builtin>')),
    'native_fps':s.render.fps,'frame_range':[s.frame_start,s.frame_end],
    'native_units':s.unit_settings.system,'metres_per_unit':s.unit_settings.scale_length,
    'new_shelf_collections_present':all(n in bpy.data.collections for n in ['V6_Enchanted_Shelves','V6_Enchanted_BookSpines','V6_Floating_ArchiveBooks'])}
report['pass']=all([report['removed_names_absent'],report['all_used_file_images_packed'],report['all_used_external_fonts_packed'],report['new_shelf_collections_present'],all(max(abs(x-1.7) for x in v)<.00001 for v in report['uniform_rigs'].values())])
report['emblem_white_board_absent']=all(n not in bpy.data.objects for n in ['EX11_EmblemWall','COL_EX11_EmblemWall'])
report['gold_wall_letter_mesh_present']='V6_Maynooth_GoldWallLetters' in s.objects
report['emblem_source_text_hidden']=bpy.data.objects['MN11_Maynooth_EditableText'].hide_render and bpy.data.objects['MN11_Maynooth_EditableText'].hide_viewport
report['pass']=report['pass'] and report['emblem_white_board_absent'] and report['gold_wall_letter_mesh_present'] and report['emblem_source_text_hidden']
(P/'Audit/fresh_reopen.json').write_text(json.dumps(report,ensure_ascii=False,indent=2))
runpy.run_path(str(P/'Scripts/06_inventory.py'))
new=[o for o in s.objects if o.get('revision')=='Magic_V6']
rows=[{'object_name':o.name,'asset_id':o.get('asset_id'),'instance_id':o.get('instance_id'),'label_zh':o.get('label_zh'),'zone_id':o.get('zone_id'),'floor':o.get('floor'),'type':o.type,'parent':o.parent.name if o.parent else '', 'source_shelf_or_book':o.get('source_shelf_or_book','')} for o in new]
with (P/'V6_new_objects.csv').open('w',encoding='utf-8-sig',newline='') as f:
    w=csv.DictWriter(f,fieldnames=rows[0].keys());w.writeheader();w.writerows(rows)
# Updated stations are self-contained native assets, kept in their assembly world positions.
stations=bpy.data.collections.new('V6_EXPORT_Updated_Workstations')
visual=bpy.data.collections.new('V6_EXPORT_StationVisuals');stations.children.link(visual)
collision=bpy.data.collections.new('V6_EXPORT_StationCollision');stations.children.link(collision)
obs=set(bpy.data.collections['V6_Station_Enrichment'].objects)
for name in ['R04_WalnutCounter','R03_WalnutCounter','R04_DeskLamp','R03_DeskLamp']:
    r=bpy.data.objects[name];obs.add(r);obs.update(r.children_recursive)
for o in s.objects:
    if o.get('asset_id') in ('V5_FX01','V5_FX02','V5_W06') or o.name in ('IV4_CounterLabel_UPLOAD','IV4_CounterLabel_SEARCH','V5_Folio_AmberBounce','V5_Orb_BlueBounce') or o.name.startswith('IV4_CounterReadingGlow'):obs.add(o)
    if o.name.startswith(('COL_IV4_R04_WalnutCounter','COL_IV4_R03_WalnutCounter')):collision.objects.link(o)
for o in sorted(obs,key=lambda o:o.name):visual.objects.link(o)
stations['label_zh']='V6 放大的知识上传与检索柜台：原生动画、道具与独立碰撞代理'
stations['coordinate_note']='Assembly coordinates, metres, Z up. Append into a clean scene; already present in the full V6 assembly.'
bpy.data.libraries.write(str(P/'Exports/V6_Updated_Workstations.blend'),{stations},compress=True)
shelf=bpy.data.collections.new('V6_EXPORT_ShelfMagic_Overlay')
for n in ['V6_Enchanted_Shelves','V6_Enchanted_BookSpines','V6_Floating_ArchiveBooks']:shelf.children.link(bpy.data.collections[n])
shelf['label_zh']='书架魔法叠加模块：纹章、书脊符文、悬浮书与微光'
shelf['coordinate_note']='An overlay for the existing 72 shelf positions. Does not include base shelves. Already included in full V6 assembly.'
bpy.data.libraries.write(str(P/'Exports/V6_ShelfMagic_Overlay.blend'),{shelf},compress=True)
emblem=bpy.data.collections.new('V6_EXPORT_DirectMountedEmblem')
for name in ['ASSET_Maynooth_Emblem','V6_Emblem_GoldMount']:emblem.children.link(bpy.data.collections[name])
emblem['label_zh']='去白底、沿后殿弧面贴合、金色镶边的原院标与 Maynooth 字样'
bpy.data.libraries.write(str(P/'Exports/V6_DirectMountedEmblem.blend'),{emblem},compress=True)
manifest={'native_format':'Blender .blend, metres, Z-up, front -Y. Packed images. Native drivers require Blender.',
    'full_model':'MilanLibrary_Magic_V6_Assembly.blend',
    'exports':[{'file':'Exports/V6_Updated_Workstations.blend','collection':stations.name,'visual_objects':len(visual.objects),'collision_objects':len(collision.objects),'includes':'Two enlarged counter assemblies, displays, lamps, props, original driver animation and collision proxies.'},
    {'file':'Exports/V6_ShelfMagic_Overlay.blend','collection':shelf.name,'objects':len(shelf.all_objects),'includes':'Overlay only: 80 crown wards, 752 decorated spines in 4 meshes, 8 floating archive books, dust and lights. Base shelves remain in full assembly.'},
    {'file':'Exports/V6_DirectMountedEmblem.blend','collection':emblem.name,'objects':len(emblem.all_objects),'includes':'Supplied crest fitted to the apse, double gold edge, thin gold letters, concealed mounts. Editable font source is explicitly hidden. No rectangular white backing.'}],
    'external_engine_export_tested':False}
(P/'export_manifest.json').write_text(json.dumps(manifest,ensure_ascii=False,indent=2))
(P/'Audit/native_export_complete.json').write_text(json.dumps({'complete':True,'reopen_pass':report['pass'],'files':[r['file'] for r in manifest['exports']]}))
print('Fresh reopen and native module export complete:',report['pass'])
