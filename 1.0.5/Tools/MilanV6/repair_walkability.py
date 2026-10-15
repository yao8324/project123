"""Deterministic approved V6 repair. Run with Blender >=5.2 --background --python this_file.
Always opens the SHA-verified untouched Assembly, never repairs an already repaired file.
Output is a separate CloudRepair file. Static BVH tests are not CharacterController tests.
"""
import bpy,bmesh,math,json,hashlib,sys,time
from pathlib import Path
from mathutils import Vector,Matrix
from mathutils.bvhtree import BVHTree
ROOT=Path(__file__).resolve().parents[2];D=ROOT/'Docs/MilanV6Unity';D.mkdir(parents=True,exist_ok=True)
SRC=ROOT/'Blender/MilanLibrary_Magic_V6/MilanLibrary_Magic_V6_Assembly.blend'
OUT=SRC.with_name('MilanLibrary_Magic_V6_CloudRepair.blend')
EXPECTED='0fdd3effbe31f03d91f4adc0d56852b94c021e23a7bdc88b918f370c2ae7ab93'
assert hashlib.sha256(SRC.read_bytes()).hexdigest()==EXPECTED,'Input Assembly SHA mismatch'
assert bpy.app.version>=(5,2,0),'Blender 5.2+ required; no header patching'
bpy.ops.wm.open_mainfile(filepath=str(SRC),load_ui=False,use_scripts=False)
S=bpy.data.scenes['MilanLibrary_Final'];bpy.context.window.scene=S;bpy.context.window.view_layer=S.view_layers['Full_Collection'];S.frame_set(35)
start_count=len(S.objects);changes=[];created=[]
V=bpy.data.collections.new('Walkability_V1');S.collection.children.link(V)
C=bpy.data.collections.new('Walkability_Collision_V1');S.collection.children.link(C);C.hide_render=True
wood=bpy.data.materials['V5_Interior_Joinery'];stone=bpy.data.materials.get('MAT_Carved_Stone');trim=stone;masonry=bpy.data.materials['MAT_V2_Candoglia_Masonry']

def bounds(o):
 p=[o.matrix_world@Vector(v) for v in o.bound_box];return Vector([min(v[i]for v in p)for i in range(3)]),Vector([max(v[i]for v in p)for i in range(3)])
def world_edit(o,fn):
 assert o.type=='MESH';o.data=o.data.copy();inv=o.matrix_world.inverted()
 for v in o.data.vertices:v.co=inv@Vector(fn(o.matrix_world@v.co))
 o.data.update();changes.append(o.name)
def meta(o,aid,role='none'):
 o['asset_id']=aid;o['instance_id']=o.name;o['zone_id']='R02';o['floor']=int(o.location.z>=5.9);o['label_zh']='V6 通行结构修订';o['collision_role']=role;o['walkability_revision']='walkability_v1';return o
FACES=[(0,3,2,1),(4,5,6,7),(0,1,5,4),(1,2,6,5),(2,3,7,6),(3,0,4,7)]
def mesh(n,vs,fs,mat=wood,role='none',aid='WV1'):
 m=bpy.data.meshes.new(n);m.from_pydata(vs,[],fs);m.update()
 if mat:
  # Match Unity export fallback projection; only newly authored primitives get UVs.
  uv=m.uv_layers.new(name='UVMap')
  for poly in m.polygons:
   axis=max(range(3),key=lambda i:abs(poly.normal[i]))
   for li in poly.loop_indices:
    p=m.vertices[m.loops[li].vertex_index].co
    uv.data[li].uv=((p.y if axis==0 else p.x)*.5,(p.y if axis==2 else p.z)*.5)
 o=bpy.data.objects.new(n,m);(C if role!='none' else V).objects.link(o)
 if mat:m.materials.append(mat)
 meta(o,aid,role);o['floor']=int(min(v[2]for v in vs)>=5.5);created.append(o.name)
 if role!='none':o.hide_render=True;o.display_type='WIRE'
 return o
def box(n,lo,hi,mat=wood,role='none',aid='WV1'):
 x,y,z=lo;X,Y,Z=hi;return mesh(n,[(x,y,z),(X,y,z),(X,Y,z),(x,Y,z),(x,y,Z),(X,y,Z),(X,Y,Z),(x,Y,Z)],FACES,mat,role,aid)
def ramp(n,x0,x1,y0,y1,z0,z1,mat=wood,role='none',th=.22):
 if y1<y0:y0,y1,z0,z1=y1,y0,z1,z0
 return mesh(n,[(x0,y0,z0-th),(x1,y0,z0-th),(x1,y1,z1-th),(x0,y1,z1-th),(x0,y0,z0),(x1,y0,z0),(x1,y1,z1),(x0,y1,z1)],FACES,mat,role)
def beam(n,a,b,w=.08,mat=wood):
 a,b=Vector(a),Vector(b);d=b-a;q=d.to_track_quat('Z','Y');u=q@Vector((w/2,0,0));v=q@Vector((0,w/2,0));return mesh(n,[tuple(p+du*u+dv*v)for p in(a,b)for du,dv in[(-1,-1),(1,-1),(1,1),(-1,1)]],FACES,mat)
def rail(n,a,b):
 a,b=Vector(a),Vector(b);L=(b-a).length
 for h,w in[(.18,.075),(1.14,.12)]:beam(n+'_rail',a+Vector((0,0,h)),b+Vector((0,0,h)),w)
 count=max(1,math.ceil(L/.38))
 for i in range(count+1):
  p=a.lerp(b,i/count);beam(n+'_baluster',p,p+Vector((0,0,1.2)),.09 if i in(0,count)else .045)
 # Solid simplified static barrier, including sloping lower edge.
 d=b-a;perp=Vector((-d.y,d.x,0)).normalized()*.06
 vs=[tuple(p+side*perp+Vector((0,0,h)))for h in(0,1.2)for p,side in[(a,-1),(b,-1),(b,1),(a,1)]]
 return mesh(n+'_Barrier',vs,FACES,None,'barrier')
def remove(o):changes.append('replaced:'+o.name);bpy.data.objects.remove(o,do_unlink=True)
def visible_names():
 names=set()
 def walk(lc,ok=True):
  ok=ok and not lc.exclude and not lc.collection.hide_render
  if ok:names.update(o.name for o in lc.collection.objects if not o.hide_render)
  for c in lc.children:walk(c,ok)
 walk(S.view_layers['Full_Collection'].layer_collection);return names
# Per-object BVH in world coordinates; candidate bounding boxes only prune ray work.
def ray_index(region=None):
 bpy.context.view_layer.update();dg=bpy.context.evaluated_depsgraph_get();names=visible_names()|{o.name for o in S.objects if o.get('collision_role')in('walkable','barrier')};out=[]
 for o in S.objects:
  if o.type not in('MESH','CURVE')or o.name not in names:continue
  lo,hi=bounds(o)
  if region and any(hi[i]<region[0][i]or lo[i]>region[1][i]for i in range(3)):continue
  eo=o.evaluated_get(dg);m=eo.to_mesh()
  if m and m.polygons:
   vs=[o.matrix_world@v.co for v in m.vertices];fs=[tuple(p.vertices)for p in m.polygons];out.append((o.name,lo,hi,BVHTree.FromPolygons(vs,fs,all_triangles=False)))
  eo.to_mesh_clear()
 return out
def cast(index,p,d,dist):
 p,d=Vector(p),Vector(d);best=None
 for n,lo,hi,bvh in index:
  if any(abs(d[i])<1e-9 and (p[i]<lo[i]-1e-5 or p[i]>hi[i]+1e-5)for i in range(3)):continue
  h,no,face,t=bvh.ray_cast(p,d,dist)
  if h is not None and(best is None or t<best['distance']):best={'object':n,'point':list(h),'distance':t}
 return best
print('Sampling original entrance surfaces',flush=True)
front=ray_index(((-18,-10,-1),(18,1,1)))
entrances=[]
for x in(-12,-6,0,6,12):
 f=bpy.data.objects['GF27_RisalitFoundation_'+str(x)];lo,hi=bounds(f);edge=lo.y
 a=cast(front,(x,edge-.02,1),(0,0,-1),3);b=cast(front,(x,edge+.04,1),(0,0,-1),3)
 entrances.append({'x':x,'edge_y':edge,'outside':a,'inside':b,'rise_m':b['point'][2]-a['point'][2]if a and b else None})
del front
# Preserve every facade portal and front arch exactly; only the four SIDE bays are replaced.
def signature(o):
 return hashlib.sha256(repr(([list(v.co)for v in o.data.vertices],[tuple(p.vertices)for p in o.data.polygons],[list(r)for r in o.matrix_world])).encode()).hexdigest()
front_a05={o.name:signature(o)for o in S.objects if o.type=='MESH'and o.name.startswith('A05')and bounds(o)[1].y<1}
for o in list(S.objects):
 if o.type=='MESH'and o.name.startswith(('A05_Side_','A05_ArchSpandrel')):
  lo,hi=bounds(o)
  if 11.9<abs((lo.x+hi.x)/2)<12.5 and lo.y>=11.99 and hi.y<=18.01:remove(o)
for side in(-1,1):
 for fl in(0,1):
  z=fl*6;xa,xb=sorted((side*12,side*12.4))
  box(f'WV1_DoorWall_{side}_{fl}',(xa,13.90,z),(xb,18,z+6),masonry,'barrier','A05')
  # Visible and colliding masonry are separate objects, not invisible replacement walls.
  ob=bpy.data.objects[f'WV1_DoorWall_{side}_{fl}'];ob.hide_render=False;C.objects.unlink(ob);V.objects.link(ob)
  for j in range(24):
   y0=12+1.9*j/24;y1=12+1.9*(j+1)/24
   def arch(y):return z+2.9+.55*(1-abs((y-12.95)/.95))
   a,b=arch(y0),arch(y1);cap=z+6 if fl else 5.7
   q=mesh(f'WV1_PointArchMasonry_{side}_{fl}_{j}',[(xa,y0,a),(xa,y1,b),(xa,y1,cap),(xa,y0,cap),(xb,y0,a),(xb,y1,b),(xb,y1,cap),(xb,y0,cap)],FACES,masonry,'barrier','A05');q.hide_render=False;C.objects.unlink(q);V.objects.link(q)
  # Trim lies outside the specified 1.9m opening.
  for y in(11.94,13.96):box(f'WV1_DoorJamb_{side}_{fl}',(xa-.04,y-.07,z),(xb+.04,y+.07,z+2.9),stone)
  for x in(xa-.03,xb+.03):
   beam('WV1_PointArchTrim',(x,12,z+2.9),(x,12.95,z+3.45),.075,stone);beam('WV1_PointArchTrim',(x,12.95,z+3.45),(x,13.9,z+2.9),.075,stone)
# Move original pier/niche decoration out of doorway; preserve all its constituent geometry.
for o in list(S.objects):
 if o.type not in('MESH','CURVE'):continue
 lo,hi=bounds(o)
 if o.name.startswith(('C05_WallPilaster','IN04','IN11'))and 11.4<abs((lo.x+hi.x)/2)<12.3 and 11.5<(lo.y+hi.y)/2<12.5:
  o.location.y-=.65;changes.append(o.name)
 # Cut only cornice segments across door aperture, retaining remaining original parts.
 if o.type=='MESH'and o.name.startswith('A08')and 11.4<abs((lo.x+hi.x)/2)<12.4 and hi.y>12 and lo.y<13.9:
  # Cornice crossing the opening is above GF, but below upper-door head.
  if 6<lo.z<8.9:
   world_edit(o,lambda p:(p.x,13.94+(p.y-lo.y)*(hi.y-13.94)/(hi.y-lo.y),p.z))
# Shift lower buttress bodies outside complete stair envelope. Bisect at transitions first.
for o in list(S.objects):
 if o.type!='MESH'or not o.name.startswith(('GF10_AttachedBatteredButtress','GF19_ButtressWallBond')):continue
 lo,hi=bounds(o);y=(lo.y+hi.y)/2
 if abs(y-12)>.2 and abs(y-18)>.2:continue
 target=10.9 if y<15 else 22.35;delta=target-y;o.data=o.data.copy();bm=bmesh.new();bm.from_mesh(o.data)
 inv=o.matrix_world.inverted()
 for zz in(8.8,12.5):bmesh.ops.bisect_plane(bm,geom=list(bm.verts)+list(bm.edges)+list(bm.faces),dist=.00001,plane_co=inv@Vector((0,0,zz)),plane_no=o.matrix_world.to_3x3().transposed()@Vector((0,0,1)),clear_inner=False,clear_outer=False)
 bm.to_mesh(o.data);bm.free()
 world_edit(o,lambda p:(p.x,p.y+delta*max(0,min(1,(12.5-p.z)/3.7)),p.z));o['support_transition']='Lower body relocated; continuous stone transition z8.8-12.5 to unchanged upper bearing'
# Recess projecting window-frame/reveal portions that invade the stair-side volume.
# Preserve the tracery in the wall plane; do not hide its collider or delete the window.
for o in list(S.objects):
 if o.type!='MESH'or not o.name.startswith(('GF08_Side_frame','GF19_SolidWindowReveal')):continue
 lo,hi=bounds(o)
 if 11.8<abs((lo.x+hi.x)/2)<14 and lo.y<21.65 and hi.y>18 and lo.y>17:
  def recess(p):
   if p.z<12.5 and abs(p.x)>12.4:
    f=max(0,min(1,(12.5-p.z)/3.7));a=abs(p.x);a=a+((12.4+(a-12.4)*.10)-a)*f
    return (math.copysign(a,p.x),p.y,p.z)
   return p
  o.data=o.data.copy();bm=bmesh.new();bm.from_mesh(o.data);inv=o.matrix_world.inverted()
  for zz in(8.8,12.5):bmesh.ops.bisect_plane(bm,geom=list(bm.verts)+list(bm.edges)+list(bm.faces),dist=.00001,plane_co=inv@Vector((0,0,zz)),plane_no=o.matrix_world.to_3x3().transposed()@Vector((0,0,1)),clear_inner=False,clear_outer=False)
  bm.to_mesh(o.data);bm.free();world_edit(o,recess)
# Expand existing shell and its integrated roof/carving, leaving original nave wall fixed.
for o in list(S.objects):
 if o.type!='MESH':continue
 if o.name.startswith(('EX10','GF13','GF19_StairChapelWallTopClosure')):
  world_edit(o,lambda p:(math.copysign(12.2+(abs(p.x)-12.2)*1.19,p.x),16.35+(p.y-16.35)*1.20,p.z))
# Remove old stair geometry/proxies; replace as dark wood assembly and static ramps.
for o in list(S.objects):
 if (o.name.startswith(('S01','S02','S03','S04','S05'))and any(c.name in('Stairs','Collision')for c in o.users_collection))or (o.name.startswith(('G03','G04','G05'))and any(c.name=='Stairs'for c in o.users_collection)):remove(o)
# Stair flights: 19 visible risers of .15 + final landing riser, 6m total floor separation.
for side in(-1,1):
 def xx(a,b):return sorted((side*a,side*b))
 a,b=xx(12.40,16.68);sa,sb=xx(12.00,16.68)
 for z in(0,6):
  box('WV1_SouthLanding',(sa,11.12,z-.30),(sb,13.90,z),wood)
  box('WV1_SouthLanding_Proxy',(sa,11.12,z-.30),(sb,13.90,z),None,'walkable')
 box('WV1_MidLanding',(a,19.60,2.70),(b,21.48,3),wood);box('WV1_MidLanding_Proxy',(a,19.60,2.70),(b,21.48,3),None,'walkable')
 for idx,(l,r,y0,y1,z0,z1)in enumerate([(12.44,14.44,13.90,19.60,0,3),(14.44,16.44,19.60,13.90,3,6)]):
  xa,xb=xx(l,r)
  for j in range(19):
   yy0=y0+(y1-y0)*j/19;yy1=y0+(y1-y0)*(j+1)/19;za=z0+(j+1)*.15
   box('WV1_WoodTread',(xa,min(yy0,yy1),za-.15),(xb,max(yy0,yy1),za),wood)
  ramp(f'WV1_StairRamp_{side}_{idx}',xa,xb,y0,y1,z0,z1,None,'walkable')
  for x in(l+.02,r-.02):
   beam('WV1_WoodStringer',(side*x,y0,z0-.13),(side*x,y1,z1-.13),.12)
   rail(f'WV1_StairRail_{side}_{idx}',(side*x,y0,z0),(side*x,y1,z1))
 # Midlanding rear and side rails; no crossbar across turn area or exit.
 rail('WV1_MidRearRail',(side*12.49,21.48,3),(side*16.39,21.48,3))
 for x in(12.49,16.39):rail('WV1_MidSideRail',(side*x,19.60,3),(side*x,21.48,3))
 # Upper landing exposed flight edge only; opening to lower flight remains protected.
 rail('WV1_UpperLandingEdge',(side*12.49,13.90,6),(side*14.39,13.90,6))
# Existing 48 placed carved railing modules => 144 components, formerly 28 export batches.
roots=[o for o in S.objects if o.type=='EMPTY'and o.name.startswith(('IV4_Gallery_','IV4_ApseRail_'))]
for root in roots:
 for child in root.children:child['collision_role']='none'
 # Master module long axis is local X, endpoints +/-1.
 a=root.matrix_world@Vector((-1,0,0));b=root.matrix_world@Vector((1,0,0));d=b-a;p=Vector((-d.y,d.x,0)).normalized()*.10
 vs=[tuple(q+side*p+Vector((0,0,h)))for h in(0,1.23)for q,side in[(a,-1),(b,-1),(b,1),(a,1)]]
 mesh('WV1_ExistingRailBarrier_'+root.name,vs,FACES,None,'barrier')
# Clone the actual approved carved module for BOTH edges of the SouthBridge.
source=bpy.data.objects['IV4_Gallery_-1_0'];localparts=[(c,c.matrix_local.copy())for c in source.children]
for y in(6,9):
 for k in range(8):
  T=Matrix.Translation(Vector((-7+2*k,y,6)))
  for src,local in localparts:
   o=src.copy();o.name=f'WV1_SouthBridge_{y}_{k}_{src.name.split(".")[0]}';V.objects.link(o);o.parent=None;o.matrix_world=T@local;meta(o,'IV4_C01');created.append(o.name)
  box(f'WV1_SouthBridgeBarrier_{y}_{k}',(-8+2*k,y-.10,6),(-6+2*k,y+.10,7.23),None,'barrier')
# Original foundations contain buried core and coping boxes with coincident Z0 tops.
# Recess only the buried core top20mm; the visible coping and ramp datum remainZ0.
foundation_core_fixes=[]
for e in entrances:
 o=bpy.data.objects['GF27_RisalitFoundation_'+str(e['x'])];cx=e['x'];count=sum(1 for v in o.data.vertices if abs((o.matrix_world@v.co).x-cx)<3.08 and abs((o.matrix_world@v.co).z)<.001)
 if count!=4:raise RuntimeError('Unexpected foundation core topology: '+o.name+' '+str(count))
 world_edit(o,lambda p:(p.x,min(p.y,0),-.02 if abs(p.x-cx)<3.08 and abs(p.z)<.001 else p.z));foundation_core_fixes.append({'object':o.name,'recessed_core_vertices':count,'coping_surface_z':0,'core_top_z':-.02,'back_edge_y':0})
# Only actual measured facade rises receive ramp transitions, inside the existing courtyard.
for e in entrances:
 if e['rise_m'] is None:raise RuntimeError('Unmeasured entrance')
 if e['rise_m']>.28:
  x=e['x'];y=e['edge_y'];z0=e['outside']['point'][2];z1=e['inside']['point'][2]
  ramp('WV1_EntranceRamp_'+str(x),x-1.20,x+1.20,y-3.6,y,z0,z1,stone)
  ramp('WV1_EntranceRampProxy_'+str(x),x-1.20,x+1.20,y-3.6,y,z0,z1,None,'walkable')
# The highest old entry step sharesZ0 with all five coping tops; preserve only its exposed ends.
old_top=bpy.data.objects.get('E09_EntranceStep')
if old_top:
 lo,hi=bounds(old_top);mat=old_top.material_slots[0].material
 for xa,xb in((lo.x,-15.12),(15.12,hi.x)):
  end=box('WV1_RetainedOldStepEnd',(xa,lo.y,lo.z),(xb,hi.y,hi.z),mat,'none','E09');end['collision_role']='walkable'
 old_top.hide_render=True;old_top.hide_set(True);old_top['superseded_by']='walkability_v1_buried_step_cleanup';changes.append('buried_top_step_visual_replaced_by_exposed_ends:'+old_top.name)
for o in S.objects:
 if o.name.startswith('E09_EntranceStep'):
  o['collision_role']='none'if o==old_top else 'walkable';changes.append('buried_top_replaced_other_step_collisions_retained:'+o.name)
# Existing side doors were only 0.35rad ajar. Open their existing hinges to90deg.
# Preserve each attached bronze relief under the same rigid door transform.
door_updates=[]
for leaf in [o for o in S.objects if o.name.startswith('E02_DoorLeaf')]:
 center=round(leaf.matrix_world.translation.x/6)*6
 if center==0:continue
 hinge=leaf.parent
 if hinge is None:raise RuntimeError('Door leaf lacks existing hinge: '+leaf.name)
 suffix=leaf.name[len('E02_DoorLeaf'):];relief=bpy.data.objects.get('GF24_DoorBronzeRelief'+suffix)
 old=leaf.matrix_world.copy();relief_old=relief.matrix_world.copy()if relief else None;angle=hinge.rotation_euler.z
 hinge.rotation_euler.z=math.copysign(math.pi/2,angle);bpy.context.view_layer.update();delta=leaf.matrix_world@old.inverted()
 if relief:
  relief.parent=hinge;relief.matrix_world=delta@relief_old;changes.append(relief.name)
 door_updates.append({'hinge':hinge.name,'old_angle_rad':angle,'new_angle_rad':hinge.rotation_euler.z,'leaf':leaf.name,'relief':relief.name if relief else None});changes.append(hinge.name)
# Relink only packaged textures, packed images stay packed. No network fetching.
broken=[];relinked=[];tex=SRC.parent/'Textures'
for im in bpy.data.images:
 if im.source in('GENERATED','VIEWER')or im.packed_file:continue
 p=Path(bpy.path.abspath(im.filepath));target=tex/p.name
 if target.is_file():im.filepath=bpy.path.relpath(str(target),start=str(OUT.parent));relinked.append(im.name)
 elif not p.is_file():broken.append(im.name)
if broken:raise RuntimeError('Missing external images: '+str(broken))
bpy.context.view_layer.update()
print('GEOMETRY_BUILT',len(created),'created',len(changes),'changes',flush=True)
# Save candidate before validation so failures are inspectable; no success revision until checks pass.
S['walkability_candidate']='static_tests_pending';bpy.ops.wm.save_as_mainfile(filepath=str(OUT),compress=True)
# Validate actual evaluated visible meshes using surface rays across the path envelopes.
idx=ray_index(((-18,10,-1),(18,23,13)))
tests=[{'kind':'front_portal_geometry_preserved','count':len(front_a05),'pass':all(n in bpy.data.objects and signature(bpy.data.objects[n])==h for n,h in front_a05.items())}]
for side in(-1,1):
 for fl in(0,1):
  samples=[]
  for y in(12.03,12.25,12.95,13.65,13.83):
   for z in(.05,.50,1.60,2.79):
    h=cast(idx,(side*11.35,y,fl*6+z),(side,0,0),1.7);samples.append({'y':y,'z':fl*6+z,'hit':h})
  ground=[cast(idx,(side*x,12.95,fl*6+.10),(0,0,-1),.15)for x in(11.8,12,12.2,12.4,12.6,12.9)]
  tests.append({'kind':'door_ground_continuity','side':side,'floor':fl,'surface_hits':ground,'pass':all(h and abs(h['point'][2]-fl*6)<.02 for h in ground)})
  tests.append({'kind':'door_aperture','side':side,'floor':fl,'target_width_m':1.8,'target_height_m':2.8,'samples':samples,'pass':not any(x['hit']for x in samples)})
 for run,(x,y0,y1,z0,z1)in enumerate([(13.44,13.90,19.60,0,3),(15.44,19.60,13.90,3,6)]):
  samples=[];widths=[]
  for j in range(1,20):
   t=j/20;y=y0+(y1-y0)*t;z=z0+(z1-z0)*t
   left=cast(idx,(side*x,y,z+1.14),(-1,0,0),2.4);right=cast(idx,(side*x,y,z+1.14),(1,0,0),2.4)
   if left and right:widths.append({'y':y,'net_width_m':left['distance']+right['distance'],'left':left['object'],'right':right['object']})
   for dx in(-.84,0,.84):
    h=cast(idx,(side*(x+dx),y,z+.17),(0,0,1),2.2);samples.append({'position':[side*(x+dx),y,z],'headroom_hit':h})
  tests.append({'kind':'flight_headroom','side':side,'run':run,'target_net_width_m':1.8,'measured_widths':widths,'minimum_measured_width_m':min((q['net_width_m']for q in widths),default=0),'samples':samples,'pass':not any(x['headroom_hit']for x in samples)and len(widths)==19 and min(q['net_width_m']for q in widths)>=1.799})
for side in(-1,1):
 h=cast(idx,(side*15.4,19.6,3.6),(0,1,0),3)
 tests.append({'kind':'midlanding_depth','side':side,'measured_depth_m':h['distance']if h else None,'limiting_hit':h,'pass':bool(h and h['distance']>=1.8)})
del idx
# Independent explicit-proxy topology and signed-volume checks; no backface assumptions.
proxy_checks=[]
for o in S.objects:
 if o.type!='MESH'or o.get('collision_role')not in('walkable','barrier'):continue
 bm=bmesh.new();bm.from_mesh(o.data);bm.transform(o.matrix_world)
 finite=all(math.isfinite(c)for v in bm.verts for c in v.co);closed=all(e.is_manifold for e in bm.edges);volume=bm.calc_volume(signed=True);areas=all(f.calc_area()>1e-9 for f in bm.faces)
 proxy_checks.append({'name':o.name,'finite':finite,'closed':closed,'positive_volume':volume>1e-9,'signed_volume':volume,'nondegenerate':areas,'pass':finite and closed and volume>1e-9 and areas});bm.free()
tests.append({'kind':'explicit_proxy_topology','count':len(proxy_checks),'failures':[q for q in proxy_checks if not q['pass']],'pass':all(q['pass']for q in proxy_checks)})
# Routes are physical-ground centerlines in Unity X,Y,Z=(Blender X,Z,Y).
def uv(p):return [p[0],p[2],p[1]]
routes=[];probes=[]
for side in(-1,1):
 pts=[(side*10.8,12.95,0),(side*12.75,12.95,0),(side*13.44,13.6,0),(side*13.44,13.90,0)]
 pts +=[(side*13.44,13.9+5.7*j/20,3*j/20)for j in range(1,21)]
 pts +=[(side*13.44,20.52,3),(side*15.44,20.52,3),(side*15.44,19.60,3)]
 pts +=[(side*15.44,19.6-5.7*j/20,3+3*j/20)for j in range(1,21)]
 pts +=[(side*15.44,12.65,6),(side*12.75,12.65,6),(side*10.8,12.65,6)]
 routes +=[{'name':f'stair_{side}_up','waypoints':[uv(p)for p in pts]},{'name':f'stair_{side}_down','waypoints':[uv(p)for p in reversed(pts)]}]
 for fl in(0,1):routes.append({'name':f'door_{side}_{fl}','waypoints':[uv((side*x,12.95,fl*6))for x in(10.8,11.5,12.2,12.9,13.4)]})
for e in entrances:
 x=e['x'];y=e['edge_y'];z=e['outside']['point'][2];h=e['rise_m'];pts=[(x,y-4,z)]+[(x,y-3.6+3.6*j/12,z+h*j/12)for j in range(13)]+[(x,y+.5,z+h)]+([(math.copysign(11.3,x),-.6,z+h),(math.copysign(11.3,x),.6,z+h)]if abs(x)==12 else [(x,.6,z+h)])
 routes.append({'name':'entrance_'+str(x),'waypoints':[uv(p)for p in pts]})
# Ground support and corridor samples for both stairs and all four door routes.
stair_after=ray_index(((-18,10,-1),(18,23,13)))
for route in [r for r in routes if not r['name'].startswith('entrance_')]:
 pts=[Vector((p[0],p[2],p[1]))for p in route['waypoints']];hits=[];ground=[]
 for p in pts:
  h=cast(stair_after,p+Vector((0,0,.18)),(0,0,-1),.4);ground.append({'point':list(p),'surface':h,'pass':bool(h and abs(h['point'][2]-p.z)<=.16)})
 for a,b in zip(pts,pts[1:]):
  delta=b-a;distance=delta.length
  if distance<1e-6:continue
  direction=delta.normalized();perp=Vector((-direction.y,direction.x,0)).normalized()
  for off in(-.28,0,.28):
   for height in(.32,1.,1.70):
    h=cast(stair_after,a+perp*off+Vector((0,0,height)),direction,distance)
    if h:hits.append({'from':list(a),'to':list(b),'lateral_offset':off,'body_height':height,'hit':h})
 tests.append({'kind':'stair_route_samples','route':route['name'],'hits':hits,'ground_support':ground,'pass':not hits and all(q['pass']for q in ground)})
del stair_after
# Sample entrance corridor at capsule-side offsets and body heights against actual surfaces.
front_after=ray_index(((-18,-10,-1),(18,2,4)))
for route in [r for r in routes if r['name'].startswith('entrance_')]:
 pts=[Vector((p[0],p[2],p[1]))for p in route['waypoints']];hits=[]
 for a,b in zip(pts,pts[1:]):
  delta=b-a;distance=delta.length;direction=delta.normalized();perp=Vector((-direction.y,direction.x,0)).normalized()
  for off in(-.28,0,.28):
   for height in(.32,1.,1.70):
    hit=cast(front_after,a+perp*off+Vector((0,0,height)),direction,distance)
    if hit:hits.append({'from':list(a),'to':list(b),'lateral_offset':off,'body_height':height,'hit':hit})
 tests.append({'kind':'entrance_body_corridor_samples','entrance':route['name'],'sampled_radius':.28,'hits':hits,'pass':not hits})
del front_after
for y in(6,9):
 for x in range(-7,8,2):probes.append({'name':f'south_bridge_{x}_{y}','position':uv((x,y+(.6 if y==6 else -.6),6.6)),'direction':[0,0,-1 if y==6 else 1],'max_distance':1.2})
for root in roots:
 a=root.matrix_world@Vector((0,.6,.6));b=root.matrix_world.to_3x3()@Vector((0,-1,0));probes.append({'name':root.name,'position':uv(a),'direction':uv(b.normalized()),'max_distance':1.2})
# BEGIN_GENERATED_STAIR_RAIL_PROBES
for side in(-1,1):
 for run,(inner,outer,y0,y1,z0,z1)in enumerate([(12.46,14.42,13.90,19.60,0,3),(14.46,16.42,19.60,13.90,3,6)]):
  for edge,railx,inward in [('inner',inner,1),('outer',outer,-1)]:
   for j,t in enumerate((.25,.5,.75)):
    y=y0+(y1-y0)*t;z=z0+(z1-z0)*t
    probes.append({'name':f'stair_{side}_{run}_{edge}_{j}','position':uv((side*(railx+inward*.5),y,z+.6)),'direction':[-side*inward,0,0],'max_distance':1.2})
 for x in(13.,14.44,15.90):
  probes.append({'name':f'midlanding_{side}_rear_{x}','position':uv((side*x,20.93,3.6)),'direction':[0,0,1],'max_distance':1.2})
 for edge,railx,inward in [('inner',12.49,1),('outer',16.39,-1)]:
  probes.append({'name':f'midlanding_{side}_{edge}','position':uv((side*(railx+inward*.5),20.54,3.6)),'direction':[-side*inward,0,0],'max_distance':1.2})
 probes.append({'name':f'upperlanding_{side}_flight_edge','position':uv((side*13.44,13.35,6.6)),'direction':[0,0,1],'max_distance':1.2})
# END_GENERATED_STAIR_RAIL_PROBES
(D/'walkability_test_routes.json').write_text(json.dumps({'coordinate_system':'Unity XYZ; waypoint Y is ground surface, not capsule center','character_height':1.75,'character_radius':.28,'floor_levels_m':[0,3,6],'routes':routes,'rail_probes':probes,'runtime_status':'NOT_RUN_STATIC_GEOMETRY_ONLY'},indent=2))
report={'input_sha256':EXPECTED,'blender_version':bpy.app.version_string,'source_objects_all':start_count,'source_count_note':'All scene datablocks including masters/hidden objects. Historical 11675 is an investigation bounds subset; original actual exporter sourceObjects is21439; repaired export count must be read from regenerated manifest.','output_objects_all':len(S.objects),'created_objects':created,'changed_objects':changes,'entrance_surface_measurements':entrances,'side_door_opening_changes':door_updates,'foundation_coplanar_core_fixes':foundation_core_fixes,'door_jamb_return_projection_m':.01,'static_bvh_tests':tests,'static_pass':all(t['pass']for t in tests),'marker_meaning':'Static export readiness only; not runtime acceptance','explicit_proxy_checks':proxy_checks,'new_primitive_uv_mapping':'Dominant-normal box projection at0.5 UV units/m; existing cloned module UVs preserved','window_recess':'Positive10percent projection depth beyondX12.4; lower frame never flattened','broken_texture_refs':broken,'relinked_images':relinked,'existing_rail_modules_proxied':len(roots),'south_bridge_new_modules':16,'runtime_status':'NOT_RUN; ray samples are not swept capsule or Unity controller acceptance','candidate_output':str(OUT.relative_to(ROOT))}
(D/'walkability_geometry_validation.json').write_text(json.dumps(report,ensure_ascii=False,indent=2))
if report['static_pass']:
 S['walkability_revision']='walkability_v1';S['walkability_candidate']='static_tests_passed_runtime_pending'
else:S['walkability_candidate']='static_tests_failed'
bpy.ops.wm.save_as_mainfile(filepath=str(OUT),compress=True)
print('REPAIR_DONE static_pass=',report['static_pass'],flush=True)
for t in tests:
 if not t['pass']:print('FAILED',t['kind'],t.get('side'),t.get('floor',t.get('run')),[(q.get('hit')or q.get('headroom_hit'))for q in t.get('samples',[])if(q.get('hit')or q.get('headroom_hit'))][:8],flush=True)
assert hashlib.sha256(SRC.read_bytes()).hexdigest()==EXPECTED
