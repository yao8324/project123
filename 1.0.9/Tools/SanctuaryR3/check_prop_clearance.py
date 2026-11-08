import bpy,math,json
from pathlib import Path
from mathutils import Vector
P=Path(__file__).resolve().parents[2]
bpy.ops.wm.open_mainfile(filepath=str(P/'Blender/SanctuaryR3/Sanctuary_R3_Props.blend'))
s=bpy.context.scene;roots=[o for o in s.objects if o.type=='EMPTY' and o.name.startswith(('P01_','P02_','P03_','P04_','P06_'))]
for root in roots:
 if 'wall_lantern' not in root.name and not root.get('front_facing_hall',False) and abs(root.children[0].rotation_euler.z)<.01:
  for c in root.children:c.location.x*=-1;c.location.y*=-1;c.rotation_euler.z+=math.pi
 root['front_facing_hall']=True
bpy.context.view_layer.update();dg=bpy.context.evaluated_depsgraph_get();reports=[]
for root in roots:
 pts=[]
 for child in root.children:
  if child.type not in ['MESH','CURVE']:continue
  ob=child.evaluated_get(dg);pts.extend(ob.matrix_world@Vector(v) for v in ob.bound_box)
 lo=[min(v[i] for v in pts) for i in range(3)];hi=[max(v[i] for v in pts) for i in range(3)]
 near=[0 if lo[i]<=0<=hi[i] else min(abs(lo[i]),abs(hi[i])) for i in range(2)];dist=math.hypot(*near)
 reports.append({'root':root.name,'bounds_min':lo,'bounds_max':hi,'conservative_radial_clearance_m':dist-28})
assert all(r['conservative_radial_clearance_m']>0 for r in reports),reports
(P/'Docs/SanctuaryR3Build/PropClearance.json').write_text(json.dumps({'scope':'evaluated world-space AABB clearance against 28m combat radius; not art acceptance','props':reports},indent=2))
bpy.ops.wm.save_as_mainfile(filepath=str(P/'Blender/SanctuaryR3/Sanctuary_R3_Props.blend'))
cam=s.camera;cam.location=(-21,-24,3.1);cam.rotation_euler=(Vector((-25,-28.6,.8))-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.lens=42
s.render.filepath=str(P/'Docs/SanctuaryR3Build/Props_Detail.png');bpy.ops.render.render(write_still=True)
print('PROP_CLEARANCE_PASSED',len(reports),min(r['conservative_radial_clearance_m'] for r in reports))
