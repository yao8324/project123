"""R7 knight priority: physical visor aperture, fitted botanical gilt and shoulder crests."""
import bpy,bmesh,math,json
from pathlib import Path
from mathutils import Vector
from mathutils.bvhtree import BVHTree
P=Path(__file__).resolve().parents[2];bpy.ops.wm.open_mainfile(filepath=str(P/'Blender/SanctuaryR3/Paladin_R3_LowerBody.blend'))
s=bpy.context.scene;rig=bpy.data.objects['Paladin_R3_Rig'];ivory=bpy.data.materials['R3_Armor_IvoryEnamel'];gold=bpy.data.materials['R3_Wings_AntiqueGold'];steel=bpy.data.materials['R3_Armor_ShadowSteel'];created=[]
def mesh(name,v,f,mat,bone,thickness=0):
 me=bpy.data.meshes.new(name);me.from_pydata(v,[],f);me.update();o=bpy.data.objects.new(name,me);s.collection.objects.link(o);me.materials.append(mat)
 for p in me.polygons:p.use_smooth=True
 if thickness:o.modifiers.new('Forged thickness','SOLIDIFY').thickness=thickness
 be=o.modifiers.new('Fine edge bevel','BEVEL');be.width=.0007;be.segments=2
 vg=o.vertex_groups.new(name=bone);vg.add(list(range(len(v))),1,'REPLACE');o.modifiers.new('Armor follows bone','ARMATURE').object=rig;o.parent=rig;created.append(o);return o
rows=[(1.92,.055,.080),(1.945,.09,.11),(1.99,.125,.132),(2.055,.14,.16),(2.095,.14,.156),(2.15,.13,.144),(2.195,.10,.115),(2.24,.055,.07),(2.28,.001,.006)]
chest=[(1.315,.205,.138),(1.37,.215,.151),(1.45,.242,.176),(1.53,.275,.195),(1.63,.308,.208),(1.71,.323,.208),(1.78,.304,.189),(1.835,.261,.150),(1.86,.225,.122)]
def profile(z,rs):
 for a,b in zip(rs,rs[1:]):
  if a[0]<=z<=b[0]:
   t=(z-a[0])/(b[0]-a[0]);return a[1]*(1-t)+b[1]*t,a[2]*(1-t)+b[2]*t
 return rs[-1][1:]
def surface(x,z,rs):
 w,d=profile(z,rs);return d*math.sqrt(max(0,1-(x/w)**2))
for o in list(s.objects):
 if o.name.startswith(('R01_SweptHelmetShell','R01_ContinuousEyeSlit','R01_CentralVisorSlit')):bpy.data.objects.remove(o,do_unlink=True)
# Apertures are absent shell faces, backed by a separate recessed dark inner liner.
zs=sorted(set([r[0] for r in rows]+[1.95,2.00,2.035,2.050,2.055,2.075,2.085]));N=256;v=[];faces=[];removed=0
for z in zs:
 w,d=profile(z,rows)
 for i in range(N):a=i*math.tau/N;v.append((w*math.cos(a),d*math.sin(a),z))
for j in range(len(zs)-1):
 for i in range(N):
  ids=(j*N+i,j*N+(i+1)%N,(j+1)*N+(i+1)%N,(j+1)*N+i);mid=sum((Vector(v[k]) for k in ids),Vector())/4
  hole=mid.y>0 and ((1.95<mid.z<2.075 and abs(mid.x)<.0075) or (2.055<mid.z<2.075 and abs(mid.x)<.118))
  if hole:removed+=1
  else:faces.append(ids)
mesh('R01_R7_OpenVisorHelmet',v,faces,ivory,'Head',.003)
lining=steel.copy();lining.name='R3_Visor_RecessBlack';bs=next(n for n in lining.node_tree.nodes if n.type=='BSDF_PRINCIPLED');bs.inputs['Base Color'].default_value=(.006,.008,.011,1);bs.inputs['Metallic'].default_value=0;bs.inputs['Roughness'].default_value=.75
lv=[(x*.92,y*.92,z) for x,y,z in v];lf=[(j*N+i,j*N+(i+1)%N,(j+1)*N+(i+1)%N,(j+1)*N+i) for j in range(len(zs)-1) for i in range(N)];mesh('R01_R7_RecessedVisorLiner',lv,lf,lining,'Head')
def strip(name,points,width,surf,bone):
 vv=[]
 for i,(x,z) in enumerate(points):
  before=points[max(0,i-1)];after=points[min(len(points)-1,i+1)];dx=after[0]-before[0];dz=after[1]-before[1];L=max(1e-8,math.hypot(dx,dz));nx=-dz/L*width/2;nz=dx/L*width/2
  for sign in [-1,1]:xx=x+sign*nx;zz=z+sign*nz;vv.append((xx,surf(xx,zz),zz))
 return mesh(name,vv,[(2*i,2*i+1,2*i+3,2*i+2) for i in range(len(points)-1)],gold,bone,.0012)
def leaf(name,a,b,width,surf,bone):
 # Raised central rib and pointed taper: three longitudinal vertex strips.
 a,b=Vector(a),Vector(b);d=b-a;side=Vector((-d.y,d.x)).normalized();vv=[]
 for j in range(13):
  t=j/12;p=a.lerp(b,t);w=width*math.sin(math.pi*t)**.8
  for u in [-1,0,1]:q=p+side*u*w;vv.append((q.x,surf(q.x,q.y)+(.0025 if u==0 else 0)*math.sin(math.pi*t),q.y))
 return mesh(name,vv,[(j*3+k,j*3+k+1,(j+1)*3+k+1,(j+1)*3+k) for j in range(12) for k in range(2)],gold,bone,.0012)
# Swept brow frames the actual horizontal aperture.
for sg in [-1,1]:
 pts=[(sg*(.01+.108*j/24),2.082+.014*(j/24)) for j in range(25)]
 strip('R01_R7_GiltBrow',pts,.006,lambda x,z:surface(x,z,rows)+.005,'Head')
 for k in range(3):
  a=(sg*(.025+k*.017),2.15+k*.022);b=(sg*(.07-k*.012),2.20+k*.017)
  leaf('R01_R7_CrownLaurel',a,b,.006,lambda x,z:surface(x,z,rows)+.005,'Head')
 # Chest laurel grows out of existing central keel, retaining ivory negative space.
 for k in range(6):
  z=1.43+k*.055;x=.035+k*.025
  leaf('R02_R7_ChestLaurel',(sg*x,z),(sg*(x+.055),z+.071),.012,lambda x,z:surface(x,z,chest)+.006,'Spine')
 # Articulated pointed shoulder front crest, within1.08m total width.
 cx=sg*.405
 def capsurface(x,z):return .172+.035*max(0,1-((x-cx)/.14)**2)+.012*max(0,1-((z-1.82)/.14)**2)
 outline=[(sg*.285,1.845),(sg*.34,1.93),(sg*.43,1.94),(sg*.54,1.84),(sg*.48,1.76),(sg*.405,1.69),(sg*.33,1.76)]
 vv=[(cx,capsurface(cx,1.825),1.825)]+[(x,capsurface(x,z),z) for x,z in outline]
 ff=[(0,i+1,(i+1)%len(outline)+1) for i in range(len(outline))]
 mesh('R03_R7_PointShoulderCrest',vv,ff,ivory,'UpperArm.'+('R' if sg==1 else 'L'),.004)
 cap_tree=BVHTree.FromPolygons(vv,ff)
 def capfit(x,z):
  hit=cap_tree.ray_cast(Vector((x,1,z)),Vector((0,-1,0)))[0]
  return hit.y if hit is not None else capsurface(x,z)
 points=[]
 for a,b in zip(outline,outline[1:]+outline[:1]):
  for j in range(12):points.append((a[0]+(b[0]-a[0])*j/12,a[1]+(b[1]-a[1])*j/12))
 points.append(points[0]);bone='UpperArm.'+('R' if sg==1 else 'L')
 strip('R03_R7_CrestGiltBorder',points,.009,lambda x,z:capfit(x,z)+.003,bone)
 for k in range(3):leaf('R03_R7_CrestLaurel',(cx,1.74+k*.045),(cx+sg*.06,1.80+k*.04),.009,lambda x,z:capfit(x,z)+.003,bone)
assert all(o.vertex_groups[0].name in rig.data.bones for o in created)
bpy.ops.wm.save_as_mainfile(filepath=str(P/'Blender/SanctuaryR3/Paladin_R3_Detail.blend'))
(P/'Docs/SanctuaryR3Build/KnightDetailRound7.json').write_text(json.dumps({'new_skinned_parts':len(created),'removed_visor_shell_faces':removed,'helmet_shell_thickness_m':.003,'nominal_gilt_thickness_m':.0012,'scope':'Actual aperture and fitted relief; not all-action clearance or final resemblance acceptance'},indent=2))
s.cycles.samples=24;s.render.resolution_x=1200;s.render.resolution_y=1200;s.render.resolution_percentage=100;s.camera.data.type='ORTHO';s.camera.data.ortho_scale=1.35;s.camera.location=(1.2,5,2.4);s.camera.rotation_euler=(Vector((0,0,1.84))-s.camera.location).to_track_quat('-Z','Y').to_euler();s.render.filepath=str(P/'Docs/SanctuaryR3Build/Knight_Detail_R7.png');bpy.ops.render.render(write_still=True)
print('KNIGHT_DETAIL_R7_COMPLETE',len(created),removed)
