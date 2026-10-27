"""Read-only native window audit. Never saves either .blend; writes measured JSON only."""
import bpy,json,sys,hashlib
from pathlib import Path
from mathutils import Vector
ROOT=Path(__file__).resolve().parents[2]
OUT=Path(sys.argv[sys.argv.index('--')+1]);OUT.mkdir(parents=True,exist_ok=True)
prefixes=('GF','A05_Side','A05_ArchSpandrel','A01','A02','A03','A04','C05_WallPilaster')
def bounds(points):return [[min(v[i]for v in points)for i in range(3)],[max(v[i]for v in points)for i in range(3)]] if points else None
def inspect(path):
 bpy.ops.wm.open_mainfile(filepath=str(path),load_ui=False,use_scripts=False)
 s=bpy.data.scenes['MilanLibrary_Final'];bpy.context.window.scene=s;bpy.context.window.view_layer=s.view_layers['Full_Collection'];s.frame_set(35)
 visible=set()
 def walk(lc,ok=True):
  ok=ok and not lc.exclude and not lc.collection.hide_render
  if ok:visible.update(o.name for o in lc.collection.objects if not o.hide_render)
  for c in lc.children:walk(c,ok)
 walk(s.view_layers['Full_Collection'].layer_collection)
 dg=bpy.context.evaluated_depsgraph_get();rows=[]
 for o in s.objects:
  if o.name not in visible or not o.name.startswith(prefixes) or o.type not in('MESH','CURVE'):continue
  ev=o.evaluated_get(dg);m=ev.to_mesh();vs=[o.matrix_world@v.co for v in m.vertices]
  slots=[]
  for idx,slot in enumerate(o.material_slots):
   ids={v for f in m.polygons if f.material_index==idx for v in f.vertices}
   slots.append({'material':slot.material.name if slot.material else None,'bounds':bounds([vs[v]for v in ids]),'vertices':len(ids)})
  chain=[];q=o
  while q:
   chain.append({'name':q.name,'location':list(q.location),'rotationEuler':list(q.rotation_euler),'scale':list(q.scale),'worldMatrix':[list(x)for x in q.matrix_world]});q=q.parent
  rows.append({'name':o.name,'bounds':bounds(vs),'localBounds':bounds([v.co for v in m.vertices]),'localYLevels':sorted(set(round(v.co.y,5)for v in m.vertices)),'vertices':len(vs),'polygons':len(m.polygons),'materials':slots,'transformChain':chain,'collisionRole':o.get('collision_role'),'collections':[c.name for c in o.users_collection],'worldVertexSHA256':hashlib.sha256(repr([tuple(v)for v in vs]).encode()).hexdigest()})
  ev.to_mesh_clear()
 return rows
for name in ['MilanLibrary_Magic_V6_Assembly','MilanLibrary_Magic_V6_CloudRepair']:
 path=ROOT/'Blender/MilanLibrary_Magic_V6'/(name+'.blend')
 result={'source':str(path),'sha256':hashlib.sha256(path.read_bytes()).hexdigest(),'objects':inspect(path)}
 (OUT/(name+'_windows.json')).write_text(json.dumps(result,indent=2));print('WINDOW_AUDIT',name,len(result['objects']),flush=True)
