import bpy,json,math
from mathutils import Vector
from pathlib import Path
P=Path(__file__).resolve().parents[2];bpy.ops.wm.open_mainfile(filepath=str(P/'Blender/SanctuaryR3/Sanctuary_R3_Arcades.blend'))
bpy.context.view_layer.update();dg=bpy.context.evaluated_depsgraph_get();results=[]
for o in bpy.context.scene.objects:
 if '_R4_' not in o.name or o.type!='MESH':continue
 ev=o.evaluated_get(dg);vv=[ev.matrix_world@Vector(v) for v in ev.bound_box];lo=[min(v[i] for v in vv) for i in range(3)];hi=[max(v[i] for v in vv) for i in range(3)];near=[0 if lo[i]<=0<=hi[i] else min(abs(lo[i]),abs(hi[i])) for i in range(2)];gap=math.hypot(*near)-28
 results.append({'name':o.name,'combat_circle_clearance_m':gap})
assert len(results)==164,len(results);assert min(x['combat_circle_clearance_m'] for x in results)>0
(P/'Docs/SanctuaryR3Build/ArcadeClearance.json').write_text(json.dumps({'scope':'New architecture evaluated AABBs outside radius28m only; not inter-object or window-opening clearance','objects':len(results),'min_clearance_m':min(x['combat_circle_clearance_m'] for x in results),'parts':results},indent=2));print('ARCADE_CLEARANCE_PASS',len(results),min(x['combat_circle_clearance_m'] for x in results))
