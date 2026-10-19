"""Read-only inventory of the complete native assembly, including retained hidden source objects."""
import bpy,json,csv,collections
from pathlib import Path
P=Path(__file__).resolve().parents[1];s=bpy.data.scenes['MilanLibrary_Final'];meshstats={};rows=[];groups={}
for o in s.objects:
 aid=str(o.get('asset_id','UNCLASSIFIED'));g=groups.setdefault(aid,{'asset_id':aid,'objects':0,'types':collections.Counter(),'labels_zh':set(),'revisions':set(),'render_hidden':0,'mesh_datablocks':set()})
 g['objects']+=1;g['types'][o.type]+=1;g['labels_zh'].add(str(o.get('label_zh','')));g['revisions'].add(str(o.get('revision','base')));g['render_hidden']+=int(o.hide_render)
 if o.type=='MESH':g['mesh_datablocks'].add(o.data.name)
 rows.append({'object_name':o.name,'asset_id':aid,'instance_id':o.get('instance_id',''),'zone_id':o.get('zone_id',''),'floor':o.get('floor',''),'label_zh':o.get('label_zh',''),'revision':o.get('revision','base'),'type':o.type,'parent':o.parent.name if o.parent else '', 'collections':'|'.join(c.name for c in o.users_collection),'render_hidden':o.hide_render,'full_collection_viewport_visible':o.visible_get(view_layer=s.view_layers['Full_Collection']),'mesh':o.data.name if o.type=='MESH' else '', 'position_m':','.join(f'{x:.5f}' for x in o.matrix_world.translation)})
with (P/'full_scene_inventory.csv').open('w',encoding='utf-8-sig',newline='') as f:
 w=csv.DictWriter(f,fieldnames=rows[0].keys());w.writeheader();w.writerows(rows)
manifest=[]
for aid,g in sorted(groups.items()):
 g['types']=dict(g['types']);g['labels_zh']=sorted(g['labels_zh']);g['revisions']=sorted(g['revisions']);g['unique_mesh_count']=len(g['mesh_datablocks']);g['mesh_datablocks']=sorted(g['mesh_datablocks']);manifest.append(g)
(P/'full_module_manifest.json').write_text(json.dumps({'scope':'All objects in MilanLibrary_Final, including hidden retained source and technical objects. Rendering is controlled by visibility and Full_Collection. UNCLASSIFIED lists non-asset technical objects.','total_objects':len(rows),'groups':manifest},ensure_ascii=False,indent=2))
print('Full scene inventory',len(rows),'objects',len(manifest),'asset groups')
