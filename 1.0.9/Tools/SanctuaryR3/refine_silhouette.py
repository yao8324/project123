import bpy,math,json
from pathlib import Path
from mathutils import Vector
P=Path(__file__).resolve().parents[2]
bpy.ops.wm.open_mainfile(filepath=str(P/'Blender/SanctuaryR3/Paladin_R3_Armor.blend'))
s=bpy.context.scene;rig=bpy.data.objects['Paladin_R3_Rig'];ivory=bpy.data.materials['R3_Armor_IvoryEnamel'];gold=bpy.data.materials['R3_Wings_AntiqueGold'];steel=bpy.data.materials['R3_Armor_ShadowSteel'];new=[]
def mesh(n,v,f,m,b,solid=0,smooth=True):
 me=bpy.data.meshes.new(n);me.from_pydata(v,[],f);me.update();o=bpy.data.objects.new(n,me);s.collection.objects.link(o);o.data.materials.append(m)
 for p in me.polygons:p.use_smooth=smooth
 if solid:o.modifiers.new('Forged plate thickness','SOLIDIFY').thickness=solid
 be=o.modifiers.new('Polished edges','BEVEL');be.width=.001;be.segments=2
 vg=o.vertex_groups.new(name=b);vg.add(list(range(len(v))),1,'REPLACE');o.modifiers.new('Rigid armor attachment','ARMATURE').object=rig;o.parent=rig;new.append(o);return o
def strip(n,pts,w,m,b):
 dense=[]
 for a,c in zip(pts,pts[1:]):
  for k in range(8):dense.append(Vector(a).lerp(Vector(c),k/8))
 dense.append(Vector(pts[-1]));pts=dense
 v=[]
 for i,pp in enumerate(pts):
  p=Vector(pp);d=Vector(pts[min(i+1,len(pts)-1)])-Vector(pts[max(0,i-1)]);a=Vector((-d.z,0,d.x)).normalized()*w/2;v.extend([p-a,p+a])

 if n.startswith(('R01_','R02_')):
  rows=headrows if n.startswith('R01_') else chestrows
  for p in v:p.y=surf(p.x,p.z,rows,.006 if m==gold else .004)
 return mesh(n,v,[(i*2,i*2+1,i*2+3,i*2+2) for i in range(len(pts)-1)],m,b,.002)
def loft(n,rows,m,b,segments=64):
 v=[]
 for z,w,d in rows:
  for i in range(segments):
   a=i*math.tau/segments;v.append((w*math.cos(a),d*math.sin(a),z))
 f=[(j*segments+i,j*segments+(i+1)%segments,(j+1)*segments+(i+1)%segments,(j+1)*segments+i) for j in range(len(rows)-1) for i in range(segments)]
 f.extend([tuple(reversed(range(segments))),tuple((len(rows)-1)*segments+i for i in range(segments))]);return mesh(n,v,f,m,b)
remove=('R01_ClosedHelmet','R01_TVisor','R01_CentralGoldRidge','R01_CheekGoldBorder','R02_Breastplate','R02_CentralLeafBorder','R02_LeafVein','R02_ChestGoldBoundary')
for o in list(s.objects):
 if o.name.startswith(remove) or (o.name.startswith('R04_') and o.vertex_groups and o.vertex_groups[0].name=='Hand.R'):bpy.data.objects.remove(o,do_unlink=True)
# Segmented helmet shell: swept crown, projecting brow, tapered chin.
headrows=[(1.92,.055,.080),(1.945,.09,.11),(1.99,.125,.132),(2.055,.14,.16),(2.095,.14,.156),(2.15,.13,.144),(2.195,.10,.115),(2.24,.055,.07),(2.28,.001,.006)]
loft('R01_SweptHelmetShell',headrows,ivory,'Head')
def profile(z,rows):
 for a,b in zip(rows,rows[1:]):
  if a[0]<=z<=b[0]:
   t=(z-a[0])/(b[0]-a[0]);return(a[1]*(1-t)+b[1]*t,a[2]*(1-t)+b[2]*t)
 return rows[0][1:]
def surf(x,z,rows,offset=.003):
 w,d=profile(z,rows);return d*math.sqrt(max(0,1-(x/w)**2))+offset
# Visor is a dark recessed-looking strip following the curved shell, continuous T form.
pts=[(x,surf(x,2.065,headrows,.003),2.065-.018*abs(x)/.115) for x in [-.115,-.09,-.06,-.03,0,.03,.06,.09,.115]]
strip('R01_ContinuousEyeSlit',pts,.009,steel,'Head')
strip('R01_CentralVisorSlit',[(0,surf(0,z,headrows,.004),z) for z in [2.063,2.035,2.00,1.965,1.94]],.009,steel,'Head')
for sg in [-1,1]:
 pts=[(sg*x,surf(sg*x,z,headrows,.006),z) for x,z in [(.009,1.943),(.012,2.00),(.016,2.05),(.028,2.09),(.018,2.18),(.003,2.275)]]
 strip('R01_VisorGoldFlange',pts,.010,gold,'Head')
 pts=[(sg*x,surf(sg*x,z,headrows,.005),z) for x,z in [(.055,1.945),(.102,1.99),(.125,2.05),(.118,2.12),(.075,2.20)]]
 strip('R01_CheekSweepGold',pts,.011,gold,'Head')
# Torso front and rear shells have a narrower waist and curved pectoral volume.
chestrows=[(1.315,.205,.138),(1.37,.215,.151),(1.45,.242,.176),(1.53,.275,.195),(1.63,.308,.208),(1.71,.323,.208),(1.78,.304,.189),(1.835,.261,.150),(1.86,.225,.122)]
for front in [True,False]:
 v=[];N=49
 for z,w,d in chestrows:
  for i in range(N):
   a=math.pi*i/(N-1);x=w*math.cos(a);y=d*math.sin(a)*(1 if front else -1);v.append((x,y,z))
 mesh('R02_CurvedBreastShell' if front else 'R02_SeparateBackShell',v,[(j*N+i,j*N+i+1,(j+1)*N+i+1,(j+1)*N+i) for j in range(len(chestrows)-1) for i in range(N-1)],ivory,'Spine',.004)
for sg in [-1,1]:
 pts=[(sg*x,surf(sg*x,z,chestrows,.006),z) for x,z in [(.01,1.32),(.045,1.42),(.072,1.52),(.12,1.64),(.20,1.74),(.22,1.80)]]
 strip('R02_SweptChestLeaf',pts,.012,gold,'Spine')
 for j in range(5):
  z=1.42+j*.065;x=.035+j*.027
  pts=[(sg*xx,surf(sg*xx,zz,chestrows,.006),zz) for xx,zz in [(x,z),(x+.035,z+.028),(x+.06,z+.075)]]
  strip('R02_TaperedLeafBranch',pts,.008,gold,'Spine')
strip('R02_CentralKeel',[(0,surf(0,z,chestrows,.007),z) for z in [1.32,1.42,1.52,1.62,1.72,1.81]],.012,gold,'Spine')
# Gauntlet phalanges wrap around the existing sword handle with fixed 3mm nominal clearance.
grip=Vector((.60,.16,1.20));axis=Vector((0,.8,-.6));u=Vector((1,0,0));v=Vector((0,.6,.8));radius=.041
fingerinfo=[]
def capsule(n,a,b,r,m):
 a,b=Vector(a),Vector(b);axis=(b-a).normalized();uu=axis.cross(Vector((0,0,1)))
 if uu.length<.01:uu=Vector((1,0,0))
 uu.normalize();vv=axis.cross(uu);verts=[]
 for p,rr in [(a,r*.75),(a+(b-a)*.2,r),(b-(b-a)*.2,r),(b,r*.75)]:
  for k in range(12):ang=k*math.tau/12;verts.append(p+rr*(uu*math.cos(ang)+vv*math.sin(ang)))
 faces=[(j*12+k,j*12+(k+1)%12,(j+1)*12+(k+1)%12,(j+1)*12+k) for j in range(3) for k in range(12)];faces.extend([tuple(reversed(range(12))),tuple(36+k for k in range(12))]);return mesh(n,verts,faces,m,'Hand.R')
for i,offset in enumerate([-.045,-.015,.015,.045]):
 center=grip+axis*offset
 for j in range(3):
  a=math.radians(-55+j*70);b=math.radians(-55+(j+1)*70-4);start=center+radius*(u*math.cos(a)+v*math.sin(a));end=center+radius*(u*math.cos(b)+v*math.sin(b));capsule('R04_RightGripFinger_%d_%d'%(i,j),start,end,.010,ivory)
 fingerinfo.append({'finger':i,'grip_axis_offset':offset})
# Thumb opposes the four finger pads; cuff reaches the wrist without changing hand bone.
capsule('R04_RightOpposedThumb_0',grip+u*.05-axis*.058,grip+u*.023-v*.034-axis*.035,.014,ivory)
capsule('R04_RightOpposedThumb_1',grip+u*.023-v*.034-axis*.035,grip-u*.005-v*.037,.012,ivory)
capsule('R04_RightPalm',grip+u*.058-axis*.05,grip+u*.058+axis*.05,.025,steel)
capsule('R04_RightDorsalPlate',grip+u*.078-axis*.05,grip+u*.078+axis*.05,.018,ivory)
# Geometry is evaluated in the same rest coordinate frame as the original skeleton.
assert all(o.vertex_groups[0].name in rig.data.bones for o in new)
bpy.ops.wm.save_as_mainfile(filepath=str(P/'Blender/SanctuaryR3/Paladin_R3_Silhouette.blend'))
(P/'Docs/SanctuaryR3Build/SilhouetteRound.json').write_text(json.dumps({'added_parts':len(new),'helmet_height_m':.36,'helmet_top_m':2.28,'chest_max_depth_m':.416,'right_fingers':4,'right_thumb_segments':2,'status':'Silhouette refinement, not final acceptance','pending':'Visor physical opening, curvature gold fit, wrist/palm anatomy, left grip and weapon collision, lower body silhouette, all-action clearance'},indent=2))
s.cycles.samples=24;s.render.resolution_x=1200;s.render.resolution_y=1400;s.render.resolution_percentage=100
s.camera.location=(2.4,5,2.35);s.camera.rotation_euler=(Vector((0,0,1.5))-s.camera.location).to_track_quat('-Z','Y').to_euler();s.camera.data.type='ORTHO';s.camera.data.ortho_scale=2.45;s.render.filepath=str(P/'Docs/SanctuaryR3Build/Armor_Round3.png');bpy.ops.render.render(write_still=True)
s.camera.location=(1.1,1.0,1.55);s.camera.rotation_euler=(grip-s.camera.location).to_track_quat('-Z','Y').to_euler();s.camera.data.ortho_scale=.35;s.render.filepath=str(P/'Docs/SanctuaryR3Build/SwordGrip_Round3.png');bpy.ops.render.render(write_still=True)
print('SILHOUETTE_COMPLETE',len(new))
