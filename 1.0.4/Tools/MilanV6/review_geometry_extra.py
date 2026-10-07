"""Read-only front entrance and table component audit for the current review."""
import bpy,json,sys
from pathlib import Path
from mathutils import Vector
ROOT=Path(__file__).resolve().parents[2];OUT=Path(sys.argv[sys.argv.index('--')+1])
bpy.ops.wm.open_mainfile(filepath=str(ROOT/'Blender/MilanLibrary_Magic_V6/MilanLibrary_Magic_V6_CloudRepair.blend'),load_ui=False,use_scripts=False)
s=bpy.data.scenes['MilanLibrary_Final'];bpy.context.window.scene=s;bpy.context.window.view_layer=s.view_layers['Full_Collection'];s.frame_set(35)
def bounds(vs):return [[min(v[i]for v in vs)for i in range(3)],[max(v[i]for v in vs)for i in range(3)]]if vs else None
visible=set()
def walk(lc,ok=True):
 ok=ok and not lc.exclude and not lc.collection.hide_render
 if ok:visible.update(o.name for o in lc.collection.objects if not o.hide_render)
 for c in lc.children:walk(c,ok)
walk(s.view_layers['Full_Collection'].layer_collection)
rows=[];dg=bpy.context.evaluated_depsgraph_get()
for o in s.objects:
 if o.type!='MESH' or o.name not in visible:continue
 if not (o.name.startswith(('E02','E09','GF14','GF27_RisalitFoundation','A05_Portal','GF06_Deep','GF23','IV4_W04_Table','V5_DeskConsole','V5_Console'))):continue
 ev=o.evaluated_get(dg);m=ev.to_mesh();vs=[v.co.copy()for v in m.vertices];world=[o.matrix_world@v for v in vs];parts=[]
 if o.name.startswith(('IV4_W04_Table','V5_Desk','V5_Console')):
  parents=list(range(len(vs)))
  def root(i):
   while parents[i]!=i:parents[i]=parents[parents[i]];i=parents[i]
   return i
  for edge in m.edges:
   a,b=edge.vertices;parents[root(a)]=root(b)
  groups={}
  for i in range(len(vs)):groups.setdefault(root(i),[]).append(i)
  for ids in groups.values():parts.append({'vertices':len(ids),'localBounds':bounds([vs[i]for i in ids]),'worldBounds':bounds([world[i]for i in ids])})
 rows.append({'name':o.name,'bounds':bounds(world),'localBounds':bounds(vs),'matrix':[list(r)for r in o.matrix_world],'parent':o.parent.name if o.parent else None,'parentMatrix':[list(r)for r in o.parent.matrix_world]if o.parent else None,'parentRotation':list(o.parent.rotation_euler)if o.parent else None,'materials':[sl.material.name if sl.material else None for sl in o.material_slots],'uvSample':[list(d.uv)for d in list(m.uv_layers.active.data)[:8]]if m.uv_layers.active else [],'components':parts})
 ev.to_mesh_clear()
OUT.write_text(json.dumps(rows,indent=2));print('REVIEW_GEOMETRY_EXTRA',len(rows),flush=True)
