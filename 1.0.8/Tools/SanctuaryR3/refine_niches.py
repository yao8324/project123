import bpy,bmesh,math,json
from pathlib import Path
from mathutils import Vector
P=Path(__file__).resolve().parents[2]
bpy.ops.wm.open_mainfile(filepath=str(P/'Blender/SanctuaryR3/Sanctuary_R3_Arcades.blend'))
s=bpy.context.scene;stone=bpy.data.materials['R3_Warm_Limestone_Masonry'];marble=bpy.data.materials['R3_Ivory_Marble_Foundation'];gold=bpy.data.materials['R3_Antique_Gold_Foundation'];added=[]
def mesh(n,v,f,m):
 me=bpy.data.meshes.new(n);me.from_pydata(v,[],f);me.update();bm=bmesh.new();bm.from_mesh(me);bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(me);bm.free();o=bpy.data.objects.new(n,me);s.collection.objects.link(o);o.data.materials.append(m);b=o.modifiers.new('Dressed edges','BEVEL');b.width=.002;b.segments=2;added.append(o);return o
def box(v,f,p,d):
 ix=len(v);v.extend([(p[0]+a*d[0]/2,p[1]+b*d[1]/2,p[2]+c*d[2]/2) for a,b,c in [(-1,-1,-1),(-1,-1,1),(-1,1,-1),(-1,1,1),(1,-1,-1),(1,-1,1),(1,1,-1),(1,1,1)]]);f.extend([tuple(ix+i for i in face) for face in [(0,4,6,2),(1,3,7,5),(0,1,5,4),(2,6,7,3),(0,2,3,1),(4,5,7,6)]])
def archheight(u):return 5+3.2*math.sqrt(max(0,4-(1+abs(u)/3.2)**2))/math.sqrt(3)
reports=[]
for side in [-1,1]:
 for c in [-12,12]:
  label=f'{side}_{c}'
  # Real side walls, partitioned around two service holes at x31.87.
  for edge in [-1,1]:
   y=c+edge*3.30;v=[];f=[]
   for lo,hi in [(30.8,31.8),(31.94,32)]:box(v,f,(side*(lo+hi)/2,y,2.5),(hi-lo,.20,5))
   for lo,hi in [(0,.58),(.72,.83),(.97,5)]:box(v,f,(side*31.87,y,(lo+hi)/2),(.14,.20,hi-lo))
   mesh('E03_R5_NicheCheek_'+label,v,f,stone)
   # Sleeves span wall thickness; inner radius55mm gives5mm clearance around the100mm pipe.
   for z in [.65,.90]:
    vv=[];ff=[]
    for yy in [y-.106,y+.106]:
     for radius in [.055,.11]:
      for k in range(48):a=k*math.tau/48;vv.append((side*31.87+radius*math.cos(a),yy,z+radius*math.sin(a)))
    for k in range(48):
     n=(k+1)%48;ff.extend([(k,n,n+48,k+48),(96+k,144+k,144+n,96+n),(k,96+k,96+n,n),(48+k,48+n,144+n,144+k)])
    mesh('P05_R5_ThroughWallSleeve_'+label,vv,ff,gold)
  # Pointed soffit encloses the niche from mouth30.8 to structural back wall32.
  v=[];f=[];N=129
  for i in range(N):
   u=-3.2+6.4*i/(N-1);z=archheight(u)
   for x,zz in [(30.8,z),(32,z),(32,z+.20),(30.8,z+.20)]:v.append((side*x,c+u,zz))
  for i in range(N-1):
   for j in range(4):a=i*4+j;b=i*4+(j+1)%4;f.append((a,b,b+4,a+4))
  f.extend([tuple(reversed(range(4))),tuple((N-1)*4+j for j in range(4))]);mesh('E03_R5_NichePointedSoffit_'+label,v,f,stone)
  # Dressed mouth frame and thin gold band; opening stays6.4m clear.
  for material,extra,width,depth in [(marble,0,.12,.08),(gold,.035,.018,.008)]:
   v=[];f=[]
   for i in range(N):
    u=-3.2+6.4*i/(N-1);z=archheight(u)+extra
    for x,zz in [(30.8-depth,z),(30.8-depth,z+width),(30.8,z+width),(30.8,z)]:v.append((side*x,c+u,zz))
   for i in range(N-1):
    for j in range(4):a=i*4+j;b=i*4+(j+1)%4;f.append((a,b,b+4,a+4))
   f.extend([tuple(reversed(range(4))),tuple((N-1)*4+j for j in range(4))]);mesh('E03_R5_NicheMouthBand_'+label,v,f,material)
  reports.append({'side':side,'center_z_m':c,'mouth_abs_x_m':30.8,'back_wall_abs_x_m':32,'structural_depth_m':1.2,'facing_intrusion_m':.03,'finished_depth_m':1.17,'target_finished_depth_m':1.2,'clear_width_m':6.4,'spring_height_m':5,'crown_height_m':8.2,'service_hole_width_height_m':.14})
# Audit actual props: every closed mesh must have positive outward volume.
fixed=[]
for o in s.objects:
 if o.type!='MESH' or not o.name.startswith(('P01_','P02_','P03_','P04_','P05_','P06_','P07_')):continue
 bm=bmesh.new();bm.from_mesh(o.data)
 if bm.faces and all(e.is_manifold for e in bm.edges):
  before=bm.calc_volume(signed=True)
  if before<0:bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(o.data);fixed.append(o.name)
 bm.free()
bpy.context.view_layer.update();dg=bpy.context.evaluated_depsgraph_get();clearances=[]
for o in added:
 ev=o.evaluated_get(dg);pts=[ev.matrix_world@Vector(v) for v in ev.bound_box];lo=[min(v[i] for v in pts) for i in range(2)];hi=[max(v[i] for v in pts) for i in range(2)];near=[0 if lo[i]<=0<=hi[i] else min(abs(lo[i]),abs(hi[i])) for i in range(2)];clearances.append(math.hypot(*near)-28)
assert min(clearances)>0
bpy.ops.wm.save_as_mainfile(filepath=str(P/'Blender/SanctuaryR3/Sanctuary_R3_Niches.blend'))
(P/'Docs/SanctuaryR3Build/NicheRound.json').write_text(json.dumps({'niches':reports,'new_objects':len(added),'existing_closed_props_normal_repairs':fixed,'min_combat_clearance_m':min(clearances),'scope':'Actual alcove inside existing3m side aisle, not exterior-wall extension. Back remains existing structural wall. No full collision certification.'},indent=2))
s.cycles.samples=24;s.render.resolution_x=1400;s.render.resolution_y=1000;s.render.resolution_percentage=100;s.camera.data.type='PERSP';s.camera.data.lens=30;s.camera.location=(-24,-18,3.5);s.camera.rotation_euler=(Vector((-31.4,-12,3.5))-s.camera.location).to_track_quat('-Z','Y').to_euler();s.render.filepath=str(P/'Docs/SanctuaryR3Build/Niche_Round5.png');bpy.ops.render.render(write_still=True)
print('NICHE_ROUND_COMPLETE',len(added),'normal_repairs',len(fixed),'combat_gap',min(clearances))
