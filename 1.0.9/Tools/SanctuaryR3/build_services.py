"""R3 perimeter services and archival niche furniture; isolated Blender pass."""
import bpy, bmesh, math, json, random
from pathlib import Path
from mathutils import Vector
P=Path(__file__).resolve().parents[2]
bpy.ops.wm.open_mainfile(filepath=str(P/'Blender/SanctuaryR3/Sanctuary_R3_Props.blend'))
s=bpy.context.scene
G=bpy.data.materials['R3_Antique_Gold_Foundation']; S=bpy.data.materials['R3_Gunmetal_Foundation']; W=bpy.data.materials['R3_Prop_Walnut']; L=bpy.data.materials['R3_Prop_DarkLeather']; PAPER=bpy.data.materials['R3_Prop_InventoryPaper']; BLUE=bpy.data.materials['R3_Prop_BlueCord']
created=[]
def box(n,p,d,m):
 v=[(p[0]+x*d[0]/2,p[1]+y*d[1]/2,p[2]+z*d[2]/2) for x,y,z in [(-1,-1,-1),(-1,-1,1),(-1,1,-1),(-1,1,1),(1,-1,-1),(1,-1,1),(1,1,-1),(1,1,1)]]
 me=bpy.data.meshes.new(n);me.from_pydata(v,[],[(0,4,6,2),(1,3,7,5),(0,1,5,4),(2,6,7,3),(0,2,3,1),(4,5,7,6)]);me.update();bm=bmesh.new();bm.from_mesh(me);bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(me);bm.free();me.update();o=bpy.data.objects.new(n,me);s.collection.objects.link(o);o.data.materials.append(m);b=o.modifiers.new('Machined edges','BEVEL');b.width=.004;b.segments=2;created.append(o);return o

def tube(n,pts,r,m):
 c=bpy.data.curves.new(n,'CURVE');c.dimensions='3D';c.resolution_u=1;c.bevel_depth=r;c.bevel_resolution=3;c.use_fill_caps=True;sp=c.splines.new('POLY');sp.points.add(len(pts)-1)
 for p,v in zip(sp.points,pts):p.co=(*v,1)
 o=bpy.data.objects.new(n,c);s.collection.objects.link(o);o.data.materials.append(m);created.append(o);return o
def ring(n,p,r,axis,m,thick=.009):
 axis=Vector(axis).normalized();u=axis.cross(Vector((0,0,1)))
 if u.length<.1:u=Vector((1,0,0))
 u.normalize();v=axis.cross(u);p=Vector(p)
 return tube(n,[p+r*(u*math.cos(i*math.tau/48)+v*math.sin(i*math.tau/48)) for i in range(49)],thick,m)
def cylinder(n,p,r,h,axis,m):
 bpy.ops.mesh.primitive_cylinder_add(vertices=20,radius=r,depth=h,location=p);o=bpy.context.object;o.name=n;o.rotation_euler=Vector(axis).to_track_quat('Z','Y').to_euler();o.data.materials.append(m);created.append(o);return o
def rounded(points,r=.14):
 result=[Vector(points[0])]
 for a,b,c in zip(points,points[1:],points[2:]):
  a,b,c=map(Vector,(a,b,c));u=(b-a).normalized();v=(c-b).normalized()
  if abs(u.dot(v))>.99:result.append(b);continue
  rr=min(r,(b-a).length*.4,(c-b).length*.4);start=b-u*rr;end=b+v*rr
  result.extend([(1-t)**2*start+2*(1-t)*t*b+t*t*end for t in [j/12 for j in range(13)]])
 result.append(Vector(points[-1]));return result
paths=json.loads((P/'Docs/SanctuaryR3Build/pipe_paths_construction_R4.json').read_text())
for path in paths:
 pts=[(p[0],p[2],p[1]) for p in path['centerline_m']];low=pts[0][2]<.8
 # Both design paths share the final riser. Build shared span once, join upper branch via a tee.
 if not low:pts=pts[:-1]
 tube(path['id']+'_100mm_Conduit',rounded(pts),.05,G)
 for a,b in zip(pts,pts[1:]):
  a,b=Vector(a),Vector(b);d=b-a;length=d.length;axis=d.normalized()
  for j in range(1,math.ceil(length/1.8)):
   p=a+axis*min(j*1.8,length-.12)
   ring('P05_BrassClamp',p,.055,axis,S,.012)
   # Support shoes at wall; exclude vertical risers (already fixed to northern wall).
   if abs(p.x)>30:box('P05_WallShoe',(math.copysign(31.975,p.x),p.y,p.z),(.05,.15,.15),S)
   else:box('P05_NorthShoe',(p.x,35.95,p.z),(.15,.10,.15),S)
  for t in [.15,length-.15]:ring('P05_UnionCollar',a+axis*t,.057,axis,G,.014)
for sign in [-1,1]:
 x=next(path['centerline_m'][-1][0] for path in paths if path['centerline_m'][-1][0]*sign>0)
 cylinder('P05_ManifoldTee',(x,35.65,.90),.073,.28,(0,0,1),S)
 for z in [.76,1.04,7.8]:ring('P05_ManifoldUnion',(x,35.65,z),.073,(0,0,1),G,.014)
 # Meter cabinet exact 0.45 wide x0.20 deep x0.60 high.
 box('P05_MeterCabinet',(x,35.50,1.65),(.45,.20,.60),S)
 box('P05_MeterDoor',(x,35.389,1.65),(.42,.018,.56),G)
 cylinder('P05_GaugeFace',(x,35.369,1.76),.083,.018,(0,1,0),PAPER)
 ring('P05_GaugeBezel',(x,35.355,1.76),.087,(0,1,0),G,.009)
 for j in range(9):
  a=math.radians(25+j*16);tube('P05_GaugeTick',[(x+.063*math.cos(a),35.344,1.76+.063*math.sin(a)),(x+.074*math.cos(a),35.344,1.76+.074*math.sin(a))],.0017,S)
 tube('P05_GaugeNeedle',[(x,35.339,1.76),(x-.025,35.339,1.808)],.0025,S)
 cylinder('P05_ValveStem',(x,35.47,3.0),.024,.36,(0,1,0),G)
 ring('P05_220mm_ValveWheel',(x,35.28,3.0),.10,(0,1,0),G,.01)
 for j in range(5):
  a=j*math.tau/5;tube('P05_ValveSpoke',[(x,35.28,3),(x+.10*math.cos(a),35.28,3+.10*math.sin(a))],.008,S)
# Wall-only archive furnishings in bay2/4. Blank decorative books are prop geometry, not library data.
random.seed(108)
shelf_roots=[]
for side in [-1,1]:
 for along in [-12,12]:
  before=len(created)
  def bp(u,d,z):return(side*(31.55-d),along+u,z)
  def b(n,u,d,z,w,depth,h,m):return box(n,bp(u,d,z),(depth,w,h),m)
  b('P07_ArchiveBack',0,0,1.65,4.6,.10,3.3,W)
  for u in [-2.25,0,2.25]:b('P07_CarvedUpright',u,.30,1.65,.13,.65,3.3,W);b('P07_GiltPilaster',u,.64,1.70,.035,.012,3.08,G)
  for z in [.12,.75,1.38,2.01,2.64,3.25]:
   b('P07_ShelfBoard',0,.32,z,4.6,.72,.075,W);b('P07_ShelfGoldLip',0,.685,z,4.6,.014,.024,G)
  b('P07_CrownCornice',0,.28,3.35,4.8,.80,.15,W)
  b('P07_CrownInlay',0,.69,3.35,4.7,.018,.035,G)
  for tier,z in enumerate([.17,.80,1.43,2.06,2.69]):
   for k in range(22):
    # Deliberately empty gaps suggest evacuation in progress.
    if (k+3*tier)%9 in [0,1,2]:continue
    u=-2.05+k*.19;h=random.uniform(.26,.46);w=random.uniform(.09,.15)
    b('P07_DecorativeArchiveVolume',u,.37,z+h/2,w,.38,h,L if k%4 else BLUE)
    for zz in [z+.05,z+h-.05]:b('P07_BookSpineBand',u,.565,zz,w,.012,.012,G)
  shelf_roots.append({'side':side,'z_m':along,'objects':len(created)-before})
bpy.context.view_layer.update();dg=bpy.context.evaluated_depsgraph_get();clearance=[]
for obj in created:
 if obj.type=='CURVE':
  # Exact nearest point of every polyline segment minus swept tube radius.
  for spline in obj.data.splines:
   pp=[obj.matrix_world@Vector(p.co[:3]) for p in spline.points]
   for a,b in zip(pp,pp[1:]):
    aa=Vector((a.x,a.y));dd=Vector((b.x-a.x,b.y-a.y));t=max(0,min(1,-aa.dot(dd)/dd.length_squared)) if dd.length_squared else 0
    clearance.append((aa+t*dd).length-obj.data.bevel_depth-28)
 else:
  ob=obj.evaluated_get(dg);pts=[ob.matrix_world@Vector(v) for v in ob.bound_box];lo=[min(p[i] for p in pts) for i in range(2)];hi=[max(p[i] for p in pts) for i in range(2)];near=[0 if lo[i]<=0<=hi[i] else min(abs(lo[i]),abs(hi[i])) for i in range(2)];clearance.append(math.hypot(*near)-28)

assert min(clearance)>0,min(clearance)
# North service equipment must stand outside the complete gate frame footprint.
gate=[]
for ob in s.objects:
 if ob.type in ['MESH','CURVE'] and ob.name.startswith('E06_'):
  ev=ob.evaluated_get(dg);gate.extend(ev.matrix_world@Vector(v) for v in ev.bound_box)
gate_outer=max(abs(v.x) for v in gate)
service_inner=min(abs(path['centerline_m'][-1][0]) for path in paths)-.225
frame_gap=service_inner-gate_outer
assert frame_gap>.6,(frame_gap,gate_outer)
(P/'Docs/SanctuaryR3Build/GateServiceClearance.json').write_text(json.dumps({'scope':'Conservative lateral AABB separation of north service equipment from all E06 gate parts; not general architectural collision verification','gate_outer_abs_x_m':gate_outer,'equipment_inner_abs_x_m':service_inner,'gap_m':frame_gap,'minimum_required_m':.6,'north_path_z_m':35.65,'riser_abs_x_m':12.5,'design_correction':'Original x=+-9 intersected gate frames. Corrected construction routing x=+-12.5 supersedes only the original north terminal positions.'},indent=2))
s['services_pass']='P05 dual conduits/shared risers, P07 four partial archive shelves; not final acceptance'
bpy.ops.wm.save_as_mainfile(filepath=str(P/'Blender/SanctuaryR3/Sanctuary_R3_Services.blend'))
report={'objects':len(created),'pipes':len(paths),'shelf_groups':shelf_roots,'min_conservative_clearance_m':min(clearance),'routing_note':'Two specified routes share each north riser; union geometry emitted once with tee, no duplicate coplanar pipes.','pending':'Wall niche recess depth, clamp bolts/engravings, weathering, all final acceptance.'}
(P/'Docs/SanctuaryR3Build/Services.json').write_text(json.dumps(report,indent=2))
s.cycles.samples=24;s.render.resolution_x=1400;s.render.resolution_y=1000;s.render.resolution_percentage=100
for name,loc,target,lens in [('Services_Shelf',(-25,-17,3.0),(-31.2,-12,1.7),40),('Services_Manifold',(-16,28,3.5),(-12.5,35.5,2.1),48)]:
 s.camera.location=loc;s.camera.rotation_euler=(Vector(target)-s.camera.location).to_track_quat('-Z','Y').to_euler();s.camera.data.lens=lens;s.render.filepath=str(P/'Docs/SanctuaryR3Build'/f'{name}.png');bpy.ops.render.render(write_still=True)
print('SERVICES_COMPLETE',json.dumps(report))
