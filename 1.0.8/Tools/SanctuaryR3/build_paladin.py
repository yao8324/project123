import bpy,math,json
from pathlib import Path
from mathutils import Vector,Matrix
P=Path(__file__).resolve().parents[2]
bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
s=bpy.context.scene;s.unit_settings.system='METRIC';s.unit_settings.scale_length=1
D=json.loads((P/'Docs/SanctuaryBlueprint56m20261107/data/wing_rig.json').read_text())
def mat(name,c,metal=0,rough=.4):
 m=bpy.data.materials.new(name);m.diffuse_color=(*c,1);m.use_nodes=True;b=next(n for n in m.node_tree.nodes if n.type=='BSDF_PRINCIPLED');b.inputs['Base Color'].default_value=(*c,1);b.inputs['Metallic'].default_value=metal;b.inputs['Roughness'].default_value=rough;return m
ivory=mat('R3_Armor_IvoryEnamel',(.66,.64,.55),.35,.30);gold=mat('R3_Wings_AntiqueGold',(.65,.38,.10),1,.28);steel=mat('R3_Armor_ShadowSteel',(.045,.055,.065),.9,.42);cloth=mat('R3_Waist_IvoryCloth',(.56,.51,.40),0,.9);leather=mat('R3_Grip_Leather',(.028,.016,.009),0,.7)
def C(v):return Vector((v[0],v[2],v[1]))
# Real deform armature. Mesh vertices are rigidly assigned to articulated armor bones.
arm=bpy.data.armatures.new('Paladin_R3_Skeleton');rig=bpy.data.objects.new('Paladin_R3_Rig',arm);s.collection.objects.link(rig);bpy.context.view_layer.objects.active=rig;rig.select_set(True);bpy.ops.object.mode_set(mode='EDIT')
def bone(name,h,t,parent=None):
 b=arm.edit_bones.new(name);b.head=h;b.tail=t
 if parent:b.parent=arm.edit_bones[parent]
 return b
bone('Root',(0,0,0),(0,0,.2));bone('Pelvis',(0,0,1.13),(0,0,1.3),'Root');bone('Spine',(0,0,1.3),(0,0,1.78),'Pelvis');bone('Head',(0,0,1.94),(0,0,2.18),'Spine')
for side,sg in [('R',1),('L',-1)]:
 bone('UpperArm.'+side,(sg*.42,0,1.78),(sg*.53,.02,1.51),'Spine');bone('Forearm.'+side,(sg*.53,.02,1.51),(sg*.60,.08,1.25),'UpperArm.'+side);bone('Hand.'+side,(sg*.60,.08,1.25),(sg*.60,.13,1.12),'Forearm.'+side)
 bone('Thigh.'+side,(sg*.18,0,1.13),(sg*.18,.015,.62),'Pelvis');bone('Shin.'+side,(sg*.18,.015,.62),(sg*.18,0,.13),'Thigh.'+side);bone('Foot.'+side,(sg*.18,0,.13),(sg*.18,.22,.07),'Shin.'+side)
 js=[C(v) for v in D['states']['open'][side]['joint_positions_m']]
 for i,n in enumerate(['Root','Elbow','Wrist']):
  b=bone('Wing'+n+'.'+side,js[i],js[i+1],'Spine' if i==0 else 'Wing'+['Root','Elbow'][i-1]+'.'+side);b.align_roll(Vector((0,1,0)))
bpy.ops.object.mode_set(mode='OBJECT');rig.select_set(False)
objects=[]
def bind(o,b):
 if o.type=='CURVE':
  bpy.ops.object.select_all(action='DESELECT')
  bpy.context.view_layer.objects.active=o;o.select_set(True);bpy.ops.object.convert(target='MESH');o=bpy.context.object;o.select_set(False)
 group=o.vertex_groups.new(name=b);group.add(list(range(len(o.data.vertices))),1,'REPLACE');mod=o.modifiers.new('Rigid armor attachment','ARMATURE');mod.object=rig;o.parent=rig;objects.append(o);o.select_set(False);return o
def mesh(name,v,f,m,b):
 me=bpy.data.meshes.new(name);me.from_pydata(v,[],f);me.update();o=bpy.data.objects.new(name,me);s.collection.objects.link(o);o.data.materials.append(m);return bind(o,b)
def line(name,pts,r,m,b):
 cu=bpy.data.curves.new(name,'CURVE');cu.dimensions='3D';cu.bevel_depth=r;cu.bevel_resolution=2;sp=cu.splines.new('POLY');sp.points.add(len(pts)-1)
 for p,v in zip(sp.points,pts):p.co=(*v,1)
 o=bpy.data.objects.new(name,cu);s.collection.objects.link(o);o.data.materials.append(m);return bind(o,b)
def ellipsoid(name,p,d,m,b):
 bpy.ops.mesh.primitive_uv_sphere_add(segments=24,ring_count=12,radius=1,location=p);o=bpy.context.object;o.name=name;o.scale=d;bpy.ops.object.transform_apply(location=False,rotation=False,scale=True);o.data.materials.append(m)
 for f in o.data.polygons:f.use_smooth=True
 return bind(o,b)
def box(name,p,d,m,b):
 bpy.ops.mesh.primitive_cube_add(size=1,location=p);o=bpy.context.object;o.name=name;o.dimensions=d;bpy.ops.object.transform_apply(location=False,rotation=False,scale=True);o.data.materials.append(m);bev=o.modifiers.new('Armor bevel','BEVEL');bev.width=.006;bev.segments=2;return bind(o,b)
def loft(name,rows,m,b,center=(0,0),segments=16):
 v=[];f=[]
 for z,w,d in rows:
  for k in range(segments):a=k*math.tau/segments;v.append((center[0]+w*math.cos(a),center[1]+d*math.sin(a),z))
 for j in range(len(rows)-1):
  for k in range(segments):a=j*segments+k;bb=j*segments+(k+1)%segments;f.append((a,bb,bb+segments,a+segments))
 f.extend([tuple(reversed(range(segments))),tuple((len(rows)-1)*segments+k for k in range(segments))]);return mesh(name,v,f,m,b)
loft('R02_Breastplate',[(1.31,.22,.15),(1.5,.29,.19),(1.73,.36,.21),(1.86,.27,.14)],ivory,'Spine')
for i in range(3):loft('R02_WaistLame',[(1.20+i*.06,.23+i*.012,.15),(1.27+i*.06,.24+i*.012,.16)],ivory,'Pelvis')
line('R02_ChestGoldLeaf',[(-.22,.183,1.75),(-.12,.215,1.61),(0,.205,1.45),(.12,.215,1.61),(.22,.183,1.75)],.008,gold,'Spine')
box('W01_BackHarness',(0,-.23,1.64),(.40,.06,.58),steel,'Spine')
loft('R01_ClosedHelmet',[(1.92,.10,.12),(1.99,.14,.16),(2.13,.14,.15),(2.23,.09,.12),(2.28,.012,.035)],ivory,'Head',segments=12)
line('R01_TVisorHorizontal',[(-.105,.15,2.065),(0,.165,2.06),(.105,.15,2.065)],.009,steel,'Head');line('R01_TVisorVertical',[(0,.163,2.06),(0,.14,1.96)],.009,steel,'Head')
line('R01_CentralGoldRidge',[(0,.13,1.94),(0,.174,2.04),(0,.15,2.14),(0,.035,2.28)],.006,gold,'Head')
for a0,a1 in [(15,165),(195,345)]:line('R01_BrokenHalo',[(.3225*math.cos(math.radians(a0+(a1-a0)*k/64)),-.12,2.2+.3225*math.sin(math.radians(a0+(a1-a0)*k/64))) for k in range(65)],.0175,gold,'Spine')
for sg,side in [(1,'R'),(-1,'L')]:
 for j in range(3):ellipsoid('R03_PauldronLame',(sg*(.37+j*.035),0,1.80-j*.065),(.17,.18,.10),ivory,'UpperArm.'+side)
 ellipsoid('R03_ArmUnderlayer',(sg*.49,0,1.63),(.10,.10,.19),steel,'UpperArm.'+side)
 ellipsoid('R03_ElbowCap',(sg*.54,.07,1.51),(.11,.105,.09),gold,'Forearm.'+side)
 loft('R03_Vambrace',[(1.26,.075,.075),(1.48,.10,.10)],ivory,'Forearm.'+side,(sg*.575,.055))
 box('R04_HandPlate',(sg*.60,.105,1.22),(.13,.09,.12),ivory,'Hand.'+side)
 for finger in range(4):
  for joint in range(3):ellipsoid('R04_FingerJoint',(sg*(.55+finger*.031),.14+joint*.015,1.16-joint*.028),(.015,.022,.023),ivory,'Hand.'+side)
 for j in range(2):ellipsoid('R04_ThumbJoint',(sg*(.51-j*.012),.13,1.23-j*.03),(.021,.025,.026),ivory,'Hand.'+side)
 ellipsoid('R05_ThighArmor',(sg*.18,0,.91),(.14,.145,.245),ivory,'Thigh.'+side)
 ellipsoid('R05_Knee',(sg*.18,.12,.62),(.105,.085,.10),gold,'Shin.'+side)
 loft('R05_Greave',[(.16,.08,.085),(.50,.10,.12),(.58,.11,.12)],ivory,'Shin.'+side,(sg*.18,0))
 for j in range(5):box('R06_SabatonLame',(sg*.18,-.08+j*.065,.09-j*.009),(.19,.075,.11),ivory,'Foot.'+side)
 for j in range(4):
  x=sg*(.26+j*.015);o=box('R05_Tasset',(x,.12,1.18-j*.07),(.17,.06,.10),ivory,'Pelvis')
 # Split front skirt and two narrow rear waist streamers.
 for back in [False,True]:
  y=-.20 if back else .18;low=.15 if back else .35;v=[(sg*.03,y,1.3),(sg*.25,y,1.3),(sg*.27,y-.04,low),(sg*.04,y+.02,low+.04)]
  o=mesh('C01_WaistStreamer' if back else 'C01_SplitTabard',v,[(0,1,2,3)],cloth,'Pelvis');o.modifiers.new('Cloth thickness','SOLIDIFY').thickness=.003
# Sword aligned from grip toward a forward/downward tip; held by right hand bone.
grip=Vector((.60,.16,1.20));direction=Vector((0,.8,-.6));wide=Vector((1,0,0));normal=wide.cross(direction)
def swordp(x,length,depth=0):return grip+wide*x+direction*(length-.28)+normal*depth
v=[swordp(-.0425,.44),swordp(0,1.70),swordp(.0425,.44),swordp(0,.44,.006),swordp(0,.44,-.006)]
mesh('S01_DiamondSectionBlade',v,[(0,1,3),(3,1,2),(2,1,4),(4,1,0),(0,3,2,4)],steel,'Hand.R')
line('S01_LeatherGrip',[swordp(0,.12),swordp(0,.44)],.022,leather,'Hand.R');line('S01_GoldCrossguard',[swordp(-.27,.44),swordp(0,.44),swordp(.27,.44)],.024,gold,'Hand.R');ellipsoid('S01_Pommel',swordp(0,.06),(.045,.045,.06),gold,'Hand.R')
# Shield convex front, full rear backing, two distinct arm straps.
cx=-.66;cy=.23;cz=1.18;outline=[(-.39,.48),(-.24,.60),(.24,.60),(.39,.48),(.31,-.14),(0,-.60),(-.31,-.14)]
v=[(cx,cy+.12,cz)]+[(cx+x,cy,cz+z) for x,z in outline]
o=mesh('S02_IvoryKiteShield',v,[(0,i+1,(i+1)%len(outline)+1) for i in range(len(outline))],ivory,'Forearm.L');o.modifiers.new('4mm shield plate','SOLIDIFY').thickness=.004
line('S02_GoldRim',[(cx+x,cy+.005,cz+z) for x,z in outline+[outline[0]]],.015,gold,'Forearm.L')
for a0,a1 in [(20,160),(200,340)]:line('S02_BrokenRingEmblem',[(cx+.19*math.cos(math.radians(a0+(a1-a0)*k/32)),cy+.126,cz+.19*math.sin(math.radians(a0+(a1-a0)*k/32))) for k in range(33)],.012,gold,'Forearm.L')
for z in [cz-.17,cz+.17]:line('S02_BackArmStrap',[(cx-.20,cy-.015,z),(cx-.15,cy-.08,z),(cx+.15,cy-.08,z),(cx+.20,cy-.015,z)],.022,leather,'Forearm.L')
# Three rigid wing spars and nineteen separate raised-rib feather plates per side.
feathers=[]
for side,sg in [('R',1),('L',-1)]:
 js=[C(v) for v in D['states']['open'][side]['joint_positions_m']]
 for i,n in enumerate(['Root','Elbow','Wrist']):
  b='Wing'+n+'.'+side;line('W01_GoldSpar', [js[i],js[i+1]],.035,gold,b);ellipsoid('W01_ArticulatedHinge',js[i],(.065,.065,.065),steel,b)
 for group,count,x0,x1,z0,z1,l0,l1,w,bonepart,depth in [('Primary',8,1.72,2.86,2.65,3.08,1.55,.92,.19,'Wrist',-.26),('Secondary',6,.60,1.65,1.95,2.65,.90,.60,.17,'Elbow',-.30),('Covert',5,.42,1.35,1.86,2.24,.45,.27,.15,'Root',-.34)]:
  for j in range(count):
   t=j/(count-1);x=sg*(x0+(x1-x0)*t);z=z0+(z1-z0)*t;length=l0+(l1-l0)*t;vv=[]
   for k in range(13):
    u=k/12;half=w*.5*(math.sin(math.pi*(.08+.92*u))**.65 if u<1 else 0);xx=x-sg*.13*u;zz=z-length*u
    vv.extend([(xx-half,depth,zz),(xx,depth+.020*math.sin(math.pi*u),zz),(xx+half,depth,zz)])
   ff=[]
   for k in range(12):
    for q in range(2):a=k*3+q;ff.append((a,a+1,a+4,a+3))
   o=mesh(f'W01_{side}_{group}_{j+1:02d}',vv,ff,gold,'Wing'+bonepart+'.'+side);o.modifiers.new('6mm rigid feather','SOLIDIFY').thickness=.006;bev=o.modifiers.new('Feather edge bevel','BEVEL');bev.width=.0015;bev.segments=2;feathers.append(o)
rig['WingOpen']=1.0;rig['design_status']='First full character pass. Feather microbones/fold clearance/animation and final detail pending.'
# Neutral studio, isolated from environment for silhouette review.
floor=mat('StudioFloor',(.055,.065,.08),0,.65);bpy.ops.mesh.primitive_plane_add(size=200);bpy.context.object.data.materials.append(floor)
s.world.color=(.16,.16,.16)
for p,energy,size in [((4,5,6),900,5),((-4,2,4),700,4),((0,-4,5),1100,3)]:
 bpy.ops.object.light_add(type='AREA',location=p);o=bpy.context.object;o.data.energy=energy;o.data.shape='DISK';o.data.size=size;o.rotation_euler=(Vector((0,0,1.5))-o.location).to_track_quat('-Z','Y').to_euler()
bpy.ops.object.camera_add(location=(4,8,3.6));cam=bpy.context.object;cam.rotation_euler=(Vector((0,0,1.5))-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.type='ORTHO';cam.data.ortho_scale=6.7;s.camera=cam
s.render.engine='CYCLES';s.cycles.samples=32;s.cycles.use_denoising=True;s.render.resolution_x=1400;s.render.resolution_y=1000;s.render.resolution_percentage=100
bpy.ops.wm.save_as_mainfile(filepath=str(P/'Blender/SanctuaryR3/Paladin_R3_FirstPass.blend'))
s.render.filepath=str(P/'Docs/SanctuaryR3Build/Paladin_FirstPass.png');bpy.ops.render.render(write_still=True)
(P/'Docs/SanctuaryR3Build/Paladin.json').write_text(json.dumps({'stage':'first full character pass, not accepted','rig_bones':len(arm.bones),'feather_meshes':len(feathers),'rigged_meshes':len(objects),'pending':'individual feather bones, fold/animation, weapons fitting, anatomy silhouette and detail refinement, export/Unity'},indent=2));print('PALADIN_FIRST_PASS_COMPLETE',len(objects),len(feathers))
