import bpy,json,hashlib,runpy
from pathlib import Path
P=Path(__file__).resolve().parents[1];V5=P.parent/'MilanLibrary_Magic_V5';N=runpy.run_path(str(P/'Scripts/common.py'));s=bpy.data.scenes['MilanLibrary_Final'];bpy.context.window.scene=s
baseold=json.loads((V5/'Audit/baseline.json').read_text())
base={'source':bpy.data.filepath,'source_sha256':hashlib.sha256(Path(bpy.data.filepath).read_bytes()).hexdigest(),'protected':{n:N['signature'](bpy.data.objects[n]) for n in baseold['protected']},'layout':{o.name:[round(v,6) for r in o.matrix_world for v in r] for c in ['Shelves','Preview_FullCollection','InteractionAnchors'] for o in bpy.data.collections[c].objects},'original_objects':len(s.objects),'stations':{},'effects':{}}
for n in ['R04_WalnutCounter','R03_WalnutCounter']:
 o=bpy.data.objects[n];base['stations'][n]={'matrix':[list(r) for r in o.matrix_world],'children':[q.name for q in o.children_recursive]}
for o in bpy.data.collections['V5_Magic'].objects:
 if o.get('asset_id') in ['V5_FX01','V5_FX02']:base['effects'][o.name]={'matrix':[list(r) for r in o.matrix_world],'scale':list(o.scale),'parent':o.parent.name if o.parent else None}
(P/'Audit/baseline.json').write_text(json.dumps(base,ensure_ascii=False,indent=2))
bpy.ops.wm.save_as_mainfile(filepath=str(P/'MilanLibrary_Magic_V6_Before.blend'),compress=True)
N['save']('V6_start_from_live_V5')
print('Protected baseline saved')
