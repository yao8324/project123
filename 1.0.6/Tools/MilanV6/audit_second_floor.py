import bpy,json
from pathlib import Path
from mathutils import Vector
R=Path(__file__).resolve().parents[2]
bpy.ops.wm.open_mainfile(filepath=str(R/'Blender/MilanLibrary_Magic_V6/MilanLibrary_Magic_V6_PlaytestReview.blend'),load_ui=False,use_scripts=False)
S=bpy.data.scenes['MilanLibrary_Final'];bpy.context.window.scene=S;bpy.context.window.view_layer=S.view_layers['Full_Collection'];S.frame_set(35)
rows=[]
for o in S.objects:
 if o.type!='MESH':continue
 if not (o.name.startswith(('G0','WV1_','V5_Floor','IV4_')) or 'floor' in o.name.lower()):continue
 vs=[o.matrix_world@v.co for v in o.data.vertices]
 if not vs:continue
 lo=[min(v[i]for v in vs)for i in range(3)];hi=[max(v[i]for v in vs)for i in range(3)]
 if hi[2]<5.65 or lo[2]>6.5:continue
 rows.append({'name':o.name,'lo':lo,'hi':hi,'materials':[s.material.name if s.material else ''for s in o.material_slots],'collision_role':o.get('collision_role'),'collections':[c.name for c in o.users_collection],'hide_render':o.hide_render})
(R.parent/'Reports/GlassReview_20261007/floor_native.json').write_text(json.dumps(rows,indent=2))
print('FLOOR_AUDIT',len(rows))
