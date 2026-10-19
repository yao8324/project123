"""Remove the earliest standalone search runes and add an entrance furnishing view."""
import bpy,json,runpy
from pathlib import Path
from mathutils import Vector
P=Path(__file__).resolve().parents[1];N=runpy.run_path(str(P/'Scripts/common.py'))
s=bpy.data.scenes['MilanLibrary_Final'];bpy.context.window.scene=s
records=json.loads((P/'Audit/removed_obsolete_objects.json').read_text())
old=[o for o in s.objects if o.name.startswith('D04_MagicRune') and o.get('asset_id')=='D04']
for o in old:records.append({'name':o.name,'asset_id':o.get('asset_id'),'type':o.type,'render_hidden':o.hide_render,'collections':[c.name for c in o.users_collection],'reason':'Standalone runes left at the earliest search installation position.'})
bpy.data.batch_remove(ids=old)
(P/'Audit/removed_obsolete_objects.json').write_text(json.dumps(records,ensure_ascii=False,indent=2))
changes=json.loads((P/'Audit/station_changes.json').read_text());changes['removed_objects']=len(records)
(P/'Audit/station_changes.json').write_text(json.dumps(changes,ensure_ascii=False,indent=2))
c=bpy.data.collections['V6_Inspection_Cameras'];o=bpy.data.objects.get('CAM_V6_Entrance')
if not o:
    d=bpy.data.cameras.new('CAM_V6_Entrance');o=bpy.data.objects.new(d.name,d);c.objects.link(o)
o.location=(0,12.7,4.3);o.rotation_euler=(Vector((0,7,1.2))-o.location).to_track_quat('-Z','Y').to_euler();o.data.lens=14;o.data.clip_end=200
s.frame_set(35);N['save']('V6_complete_workstations_and_shelf_magic')
print('Total obsolete objects removed:',len(records))
