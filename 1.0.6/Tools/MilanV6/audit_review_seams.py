"""Read-only measured aperture and instance audit of all painted windows."""
import bpy,json,sys,math
from pathlib import Path
from mathutils import Vector
from mathutils.bvhtree import BVHTree
ROOT=Path(__file__).resolve().parents[2]
OUT=ROOT/'Docs/MilanV6Unity/window_seam_audit.json'
reports=[]
reference_bounds={}
for filename in ['MilanLibrary_Magic_V6_CloudRepair.blend','MilanLibrary_Magic_V6_PlaytestReview.blend']:
 bpy.ops.wm.open_mainfile(filepath=str(ROOT/'Blender/MilanLibrary_Magic_V6'/filename),load_ui=False,use_scripts=False)
 s=bpy.data.scenes['MilanLibrary_Final'];bpy.context.window.scene=s;bpy.context.window.view_layer=s.view_layers['Full_Collection'];s.frame_set(35)
 windows=sorted([o for o in s.objects if o.name.startswith('GF21_FiguralGlazing')],key=lambda o:o.name)
 visible=set()
 def walk(lc,allowed=True):
  allowed=allowed and not lc.exclude and not lc.collection.hide_render
  if allowed:visible.update(o.name for o in lc.collection.objects if not o.hide_render)
  for child in lc.children:walk(child,allowed)
 walk(s.view_layers['Full_Collection'].layer_collection)
 candidates=[o for o in s.objects if o.type=='MESH' and o.name.startswith(('GF','A','E')) and o.name in visible]
 rows=[]
 for index,g in enumerate(windows):
  inv=g.matrix_world.inverted();local=[v.co for v in g.data.vertices];lo=[min(v[i]for v in local)for i in range(3)];hi=[max(v[i]for v in local)for i in range(3)]
  reference_bounds.setdefault(g.name,(lo,hi))
  roi_lo,roi_hi=reference_bounds[g.name]
  verts=[];polys=[];included=[]
  for o in candidates:
   box=[inv@o.matrix_world@Vector(p)for p in o.bound_box];a=[min(p[i]for p in box)for i in range(3)];b=[max(p[i]for p in box)for i in range(3)]
   if b[0]<lo[0]-.1 or a[0]>hi[0]+.1 or b[2]<lo[2]-.1 or a[2]>hi[2]+.1 or b[1]<-6 or a[1]>6:continue
   offset=len(verts);verts.extend(inv@o.matrix_world@v.co for v in o.data.vertices);polys.extend([i+offset for i in p.vertices]for p in o.data.polygons);included.append(o.name)
  tree=BVHTree.FromPolygons(verts,polys);miss=[];total=0
  for iz in range(101):
   z=roi_lo[2]+.01+(roi_hi[2]-roi_lo[2]-.02)*iz/100
   for ix in range(45):
    x=roi_lo[0]-.055+(roi_hi[0]-roi_lo[0]+.11)*ix/44
    hit=tree.ray_cast(Vector((x,6,z)),Vector((0,-1,0)),12)[0];total+=1
    if hit is None:miss.append([round(x,4),round(z,4)])
  rows.append({'glass':g.name,'zone':'side' if index<12 else 'apse' if index<22 else 'front','origin':list(g.matrix_world.translation),'localBounds':[lo,hi],'candidateObjects':included,'rays':total,'uncovered':len(miss),'uncoveredSamples':miss[:150], 'method':'Normal rays through pane bounding rectangle +55mm sides; includes arch corners and nearby stone, ±6m depth; identical baseline ROI for both revisions. This is a discrete geometric test, not proof against arbitrary sub-sample cracks.'})
 inventory={}
 for o in candidates:
  if any(k in o.name for k in ['Tracery','Window','BlindLancet','Belfry']):inventory.setdefault(o.name.split('.')[0],[]).append(o.name)
 reports.append({'source':filename,'windows':rows,'decorativeWindowInventory':inventory})
OUT.write_text(json.dumps(reports,indent=2));print('SEAM_AUDIT',[(r['source'],sum(x['uncovered'] for x in r['windows']))for r in reports],flush=True)
