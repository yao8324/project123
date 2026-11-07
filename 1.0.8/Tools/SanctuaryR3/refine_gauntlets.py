"""R12 fitted gauntlets and real left shield handle; rigid grasp poses on existing hand bones."""
import bpy,bmesh,math,json,sys
from pathlib import Path
from mathutils import Vector
from mathutils.bvhtree import BVHTree
P=Path(__file__).resolve().parents[2];bpy.ops.wm.open_mainfile(filepath=str(P/'Blender/SanctuaryR3/Paladin_R3_Animated.blend'));s=bpy.context.scene;r=bpy.data.objects['Paladin_R3_Rig'];ivory=bpy.data.materials['R3_Armor_IvoryEnamel'];gold=bpy.data.materials['R3_Wings_AntiqueGold'];steel=bpy.data.materials['R3_Armor_ShadowSteel'];leather=bpy.data.materials['R3_Grip_Leather'];new=[]
for o in list(s.objects):
 bone=o.vertex_groups[0].name if o.type=='MESH' and o.vertex_groups else ''
 if o.name.startswith(('R04_R12_','S02_R12_','R04_RightDorsalPlate')) or (o.name.startswith('R04_') and bone=='Hand.L'):bpy.data.objects.remove(o,do_unlink=True)
def mesh(n,v,f,m,b,solid=0):
 me=bpy.data.meshes.new(n);me.from_pydata(v,[],f);me.update();o=bpy.data.objects.new(n,me);s.collection.objects.link(o);me.materials.append(m);o.parent=r
 for p in me.polygons:p.use_smooth=True
 if solid:o.modifiers.new('Plate thickness','SOLIDIFY').thickness=solid
 be=o.modifiers.new('Edge bevel','BEVEL');be.width=.0005;be.segments=2;g=o.vertex_groups.new(name=b);g.add(list(range(len(v))),1,'REPLACE');o.modifiers.new('Hand attachment','ARMATURE').object=r;new.append(o);return o
def tube(n,pts,radius,mat,bone):
 vv=[];N=12
 for j,p in enumerate(pts):
  p=Vector(p);d=(Vector(pts[min(j+1,len(pts)-1)])-Vector(pts[max(0,j-1)])).normalized();u=d.cross(Vector((0,0,1)))
  if u.length<.01:u=d.cross(Vector((0,1,0)))
  u.normalize();v=d.cross(u)
  for k in range(N):a=k*math.tau/N;vv.append(p+radius*(math.cos(a)*u+math.sin(a)*v))
 ff=[(j*N+k,j*N+(k+1)%N,(j+1)*N+(k+1)%N,(j+1)*N+k) for j in range(len(pts)-1) for k in range(N)];ff.extend([tuple(reversed(range(N))),tuple((len(pts)-1)*N+k for k in range(N))]);return mesh(n,vv,ff,mat,bone)
def dorsal(n,center,u,v,normal,bone):
 vv=[];nx=9;ny=13
 for j in range(ny):
  y=-.055+.11*j/(ny-1);width=.038*(1-.20*abs(y)/.055)
  for i in range(nx):x=(-1+2*i/(nx-1))*width;vv.append(center+u*x+v*y+normal*(.006*(1-(x/width)**2)))
 mesh(n,vv,[(j*nx+i,j*nx+i+1,(j+1)*nx+i+1,(j+1)*nx+i) for j in range(ny-1) for i in range(nx-1)],ivory,bone,.003)
 for i in [0,nx-1]:tube(n+'GoldEdge',[Vector(vv[j*nx+i])+normal*.001 for j in range(ny)],.0015,gold,bone)
# Left palm behind a horizontal handle; four three-piece fingers curl around it.
grip=Vector((-.60,.16,1.20));axis=Vector((1,0,0));u=Vector((0,1,0));v=Vector((0,0,1));radius=.030
for i,offset in enumerate([-.042,-.014,.014,.042]):
 c=grip+axis*offset
 for j in range(3):
  a=math.radians(-75+j*75);b=math.radians(-75+(j+1)*75-7);pts=[c+radius*(u*math.cos(a+(b-a)*k/8)+v*math.sin(a+(b-a)*k/8)) for k in range(9)]
  tube(f'R04_R12_LeftFinger_{i}_{j}',pts,.009,ivory,'Hand.L')
 tube('R04_R12_LeftKnuckleRim',[c+Vector((0,-.028,.016)),c+Vector((0,-.028,.032))],.003,gold,'Hand.L')
tube('R04_R12_LeftPalmUnderlayer',[(-.65,.115,1.21),(-.55,.115,1.21)],.023,steel,'Hand.L')
tube('R04_R12_LeftThumb',[(-.54,.12,1.25),(-.53,.15,1.24),(-.555,.186,1.232)],.012,ivory,'Hand.L')
dorsal('R04_R12_LeftHandBack',Vector((-.60,.099,1.22)),Vector((1,0,0)),Vector((0,0,1)),Vector((0,-1,0)),'Hand.L')
dorsal('R04_R12_RightHandBack',Vector((.684,.16,1.20)),Vector((0,.6,.8)),Vector((0,.8,-.6)),Vector((1,0,0)),'Hand.R')
# Two short overlapping cuffs follow each forearm axis, joining existing vambrace to the hand.
for side in ['R','L']:
 b=r.data.bones['Forearm.'+side];axis=(b.tail_local-b.head_local).normalized();front=Vector((0,1,0));front=(front-axis*front.dot(axis)).normalized();wide=axis.cross(front)
 for j in range(2):
  vv=[];N=48
  for t,rad in [(-.045+j*.028,.065-j*.006),(-.009+j*.028,.060-j*.006)]:
   c=b.tail_local+axis*t
   for k in range(N):a=k*math.tau/N;vv.append(c+wide*rad*math.cos(a)+front*(rad*.78)*math.sin(a))
  mesh('R04_R12_WristLame',vv,[(k,(k+1)%N,(k+1)%N+N,k+N) for k in range(N)],ivory,'Forearm.'+side,.003)
  tube('R04_R12_WristGilt',[Vector(p)+axis*.001 for p in vv[N:]]+[Vector(vv[N])+axis*.001],.0015,gold,'Forearm.'+side)
# Actual shield grip, anchored to the curved shield surface with two brackets.
axis=Vector((1,0,0))
tube('S02_R12_ShieldLeatherGrip',[grip-axis*.085,grip+axis*.085],.018,leather,'Forearm.L')
shield=bpy.data.objects['S02_IvoryKiteShield'];tree=BVHTree.FromPolygons([shield.matrix_world@v.co for v in shield.data.vertices],[tuple(p.vertices) for p in shield.data.polygons]);anchors=[]
for sign in [-1,1]:
 p=grip+axis*(sign*.085);hit=tree.ray_cast(Vector((p.x,1,p.z)),Vector((0,-1,0)))[0];assert hit is not None
 end=hit-Vector((0,.006,0));tube('S02_R12_GripMount',[p,end],.012,steel,'Forearm.L');anchors.append(list(end))
bpy.ops.wm.save_as_mainfile(filepath=str(P/'Blender/SanctuaryR3/Paladin_R3_Animated.blend'))
r.data.pose_position='REST';hidden=[]
for o in s.objects:
 if o.name.startswith(('W01_','W02_','S01_','S02_','R01_R8_')):hidden.append((o,o.hide_render));o.hide_render=True
s.cycles.samples=24;s.render.resolution_x=1200;s.render.resolution_y=1000;s.render.resolution_percentage=100;s.camera.data.type='ORTHO';s.camera.data.ortho_scale=.40
for label,focus,delta in [('Left',Vector((-.60,.14,1.23)),Vector((-1,-2,.4))),('Right',Vector((.60,.14,1.23)),Vector((2,1,.3)))]:
 s.camera.location=focus+delta;s.camera.rotation_euler=(focus-s.camera.location).to_track_quat('-Z','Y').to_euler();s.render.filepath=str(P/f'Docs/SanctuaryR3Build/Gauntlet_R12_{label}.png');bpy.ops.render.render(write_still=True)
for o,value in hidden:o.hide_render=value
r.data.pose_position='POSE';bpy.ops.object.select_all(action='DESELECT');r.select_set(True)
for o in s.objects:
 if o.type=='MESH' and any(m.type=='ARMATURE' and m.object==r for m in o.modifiers):o.select_set(True)
bpy.context.view_layer.objects.active=r
if '--skip-export' in sys.argv: print('GAUNTLET_R12_NATIVE_ONLY'); raise SystemExit(0)
bpy.ops.export_scene.fbx(filepath=str(P/'Assets/Art/Bosses/Knight/SanctuaryR3/Paladin_R3_Animated.fbx'),use_selection=True,object_types={'MESH','ARMATURE'},add_leaf_bones=False,bake_anim=True,bake_anim_use_all_actions=True,bake_anim_use_nla_strips=False,bake_anim_simplify_factor=0,axis_forward='-Z',axis_up='Y',apply_unit_scale=True)
(P/'Docs/SanctuaryR3Build/GauntletR12.json').write_text(json.dumps({'added_meshes':len(new),'left_finger_segments':12,'left_thumb':'single bent armored mesh','shield_handle_radius_m':.018,'finger_center_radius_m':.030,'finger_radius_m':.009,'shield_anchors':anchors,'scope':'Fixed rigid grasp poses, not independent finger articulation or collision certification'},indent=2));print('GAUNTLET_R12_COMPLETE',len(new))
