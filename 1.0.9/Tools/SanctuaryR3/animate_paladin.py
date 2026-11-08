import bpy,math,json
from pathlib import Path
from mathutils import Vector,Matrix,Quaternion
from mathutils.bvhtree import BVHTree
P=Path(__file__).resolve().parents[2]
bpy.ops.wm.open_mainfile(filepath=str(P/'Blender/SanctuaryR3/Paladin_R3_FeatherDetail.blend'))
s=bpy.context.scene;rig=bpy.data.objects['Paladin_R3_Rig'];D=json.loads((P/'Docs/SanctuaryBlueprint56m20261107/data/wing_rig.json').read_text());fps=60;s.render.fps=fps
# Add a genuinely separate rigid attachment bone for every primary visible feather.
feathers=[o for o in s.objects if o.type=='MESH' and o.name.startswith(('W01_R_Primary','W01_R_Secondary','W01_R_Covert','W01_L_Primary','W01_L_Secondary','W01_L_Covert'))]
bpy.ops.object.select_all(action='DESELECT');rig.select_set(True);bpy.context.view_layer.objects.active=rig;bpy.ops.object.mode_set(mode='EDIT')
for o in feathers:
 parent=o.vertex_groups[0].name;head=sum((o.matrix_world@v.co for v in list(o.data.vertices)[:3]),Vector())/3
 b=rig.data.edit_bones.new('Feather_'+o.name[4:]);b.head=head;b.tail=head+Vector(o.get('feather_direction',(0,0,-1)))*.12;b.parent=rig.data.edit_bones[parent];b.align_roll(Vector((0,1,0)))
bpy.ops.object.mode_set(mode='OBJECT')
for o in feathers:o.vertex_groups[0].name='Feather_'+o.name[4:]
for pb in rig.pose.bones:pb.rotation_mode='QUATERNION'
def C(v):return Vector((v[0],v[2],v[1]))
def basis(a,b):
 y=(b-a).normalized();z=Vector((0,1,0));z=(z-y*z.dot(y)).normalized();x=y.cross(z);return Matrix((x,y,z)).transposed()
def sm(t):t=max(0,min(1,t));return t*t*(3-2*t)
def curve(t,points):
 if t<=points[0][0]:return points[0][1]
 for (a,v),(b,w) in zip(points,points[1:]):
  if t<=b:return v+(w-v)*sm((t-a)/(b-a))
 return points[-1][1]
rot={};pos={}
for state in ['open','folded']:
 for side in ['R','L']:
  js=[C(v) for v in D['states'][state][side]['joint_positions_m']];world=[basis(js[i],js[i+1]) for i in range(3)];rot[state,side]=[(world[i-1].inverted()@world[i] if i else world[i]).to_quaternion() for i in range(3)];pos[state,side]=js
lengths=D['segment_lengths_m'];windows=[D['windows'][x] for x in ['root','elbow','wrist']]
def setrot(name,axis,angle):rig.pose.bones[name].rotation_quaternion=Quaternion(axis,angle)
def wings(amount,frame):
 bpy.context.view_layer.update();spdelta=rig.pose.bones['Spine'].matrix@rig.data.bones['Spine'].matrix_local.inverted()
 for side in ['R','L']:
  p=pos['open',side][0].copy();world=Quaternion()
  for i,n in enumerate(['Root','Elbow','Wrist']):
   a,b=windows[i]
   # Low-open calibration: authored 0.08..0.12 breathing must stay inside folded envelope.
   # Preserve 0/1 endpoints, full-open onset .40 and all three segment lengths.
   fk_amount=amount*.70 if amount<=.12 else (.084+(amount-.12)*(.40-.084)/(.40-.12) if amount<.40 else amount)
   q=rot['folded',side][i].slerp(rot['open',side][i],sm((fk_amount-a)/(b-a)));world=world@q
   target=world.to_matrix().to_4x4();target.translation=p;pb=rig.pose.bones['Wing'+n+'.'+side];pb.matrix=spdelta@target;bpy.context.view_layer.update();p+=world@Vector((0,lengths[i],0))
 # Feather rotations compensate the parent fold; attachment offsets and lengths never change.
 fan=sm((amount-.45)/.55)
 for o in feathers:
  pb=rig.pose.bones['Feather_'+o.name[4:]];rest=pb.bone;parent=pb.parent
  local_head=parent.bone.matrix_local.inverted()@rest.head_local
  head=parent.matrix@local_head
  direction=Vector(o.get('feather_direction',(0,0,-1)))
  angle=o.get('fold_fan_angle',0);closed_direction=Vector((math.sin(angle),0,-math.cos(angle)))
  close_q=basis(Vector(),closed_direction).to_quaternion();open_q=basis(Vector(),direction).to_quaternion()
  desired_rotation=spdelta.to_quaternion()@close_q.slerp(open_q,fan)
  target=desired_rotation.to_matrix().to_4x4();target.translation=head
  pb.matrix=target
 bpy.context.view_layer.update()
 rig['WingOpen']=amount;rig.keyframe_insert(data_path='["WingOpen"]',frame=frame)
def reset():
 for pb in rig.pose.bones:pb.location=(0,0,0);pb.rotation_quaternion=(1,0,0,0);pb.scale=(1,1,1)
def keyall(frame):
 for pb in rig.pose.bones:
  pb.keyframe_insert(data_path='location',frame=frame);pb.keyframe_insert(data_path='rotation_quaternion',frame=frame)
def solve_leg(side,target):
 thigh=rig.pose.bones['Thigh.'+side];shin=rig.pose.bones['Shin.'+side];foot=rig.pose.bones['Foot.'+side]
 bpy.context.view_layer.update();hip=thigh.head.copy();a=thigh.bone.length;b=shin.bone.length;delta=target-hip;distance=min(delta.length,a+b-.00001);direction=delta.normalized();ankle=hip+direction*distance
 along=(a*a-b*b+distance*distance)/(2*distance);height=math.sqrt(max(0,a*a-along*along));pole=Vector((0,1,0));pole=(pole-direction*pole.dot(direction)).normalized();knee=hip+direction*along+pole*height
 for pb,start,end in [(thigh,hip,knee),(shin,knee,ankle)]:
  rest=pb.bone;rotation=(rest.tail_local-rest.head_local).rotation_difference(end-start)@rest.matrix_local.to_quaternion();matrix=rotation.to_matrix().to_4x4();matrix.translation=start;pb.matrix=matrix;bpy.context.view_layer.update()
 matrix=foot.bone.matrix_local.copy();matrix.translation=ankle;foot.matrix=matrix;bpy.context.view_layer.update()
def contacts(name,t):
 root_pos=rig.pose.bones['Root'].matrix.translation.copy()
 if name.startswith('A01'):drop=.17
 elif name.startswith('A02'):drop=.08*curve(t,[(0,0),(.18,1),(.68,1),(.95,0)])
 else:drop=.15*curve(t,[(0,0),(.15,1),(.23,0),(.65,.45),(1.1,0),(1.18,1),(1.6,0)])
 pelvis=rig.pose.bones['Pelvis'];pelvis.location=pelvis.bone.matrix_local.to_quaternion().inverted()@Vector((0,0,-drop));bpy.context.view_layer.update()
 for side,sg,shift in [('R',1,0),('L',-1,.55)]:
  if name.startswith('A01'):
   phase=((t+shift)%1.1)/1.1;cycle=t-phase*1.1;strike=2*cycle+.55
   u=max(0,min(1,(phase-.5)/.5));target=Vector((sg*.18,strike+2.2*sm(u),.134+.11*math.sin(math.pi*u)))
  elif name.startswith('A02'):
   spread=.42*curve(t,[(0,0),(.18,1),(.68,1),(.95,0)]);target=Vector((sg*.18,root_pos.y+sg*spread,.134))
  else:target=Vector((sg*.18,root_pos.y,.134+root_pos.z))
  solve_leg(side,target)
cloth_panels=[o for o in s.objects if o.name.startswith('C01_FoldedPanel_')]
leg_surfaces=[o for o in s.objects if o.name.startswith(('R05_ThighArmor','R05_PointKneeShield','R05_TaperedGreave','R05_CurvedTassetScale'))]
avoidance_records=[]
def surface_tree(o):
 ev=o.evaluated_get(bpy.context.evaluated_depsgraph_get());me=ev.to_mesh();vv=[ev.matrix_world@v.co for v in me.vertices];ff=[list(p.vertices) for p in me.polygons];ev.to_mesh_clear();return BVHTree.FromPolygons(vv,ff)
def cloth_avoidance(action,frame):
 bpy.context.view_layer.update();trees=[surface_tree(o) for o in leg_surfaces]
 for o in cloth_panels:
  label=o.name.split('C01_FoldedPanel_')[1];pb=rig.pose.bones['Cloth.'+label+'.0'];base=pb.rotation_quaternion.copy();back=label.startswith('Rear');axis=(0,0,1) if back else (1,0,0);sign=(-1 if '.R' in label else 1) if back else 1
  def hits(amount):
   pb.rotation_quaternion=base@Quaternion(axis,sign*amount);bpy.context.view_layer.update();tree=surface_tree(o);return sum(bool(tree.overlap(other)) for other in trees)
  initial=hits(0)
  if initial==0:continue
  best=(initial,0);lo=0;hi=None
  for j in range(1,16):
   a=j*.06;count=hits(a)
   if count<best[0]:best=(count,a)
   if count==0:hi=a;lo=a-.06;break
  if hi is not None:
   for _ in range(5):
    mid=(lo+hi)/2
    if hits(mid)==0:hi=mid
    else:lo=mid
   correction=hi+.012;remaining=hits(correction)
  else:correction=best[1];remaining=hits(correction)
  avoidance_records.append({'action':action,'frame':frame,'panel':label,'correction_rad':correction,'remaining_surface_pairs':remaining})
# Feather geometry is not part of the cloth/leg BVH search. Hide it only during baking to avoid repeated modifier evaluation.
bake_hidden=[(o,o.hide_viewport) for o in feathers]
for o,_ in bake_hidden:o.hide_viewport=True
clips=[]
for name,duration in [('A01_SlowWalk',1.1),('A02_Dash',.95),('A03_JumpAttack',1.6)]:
 rig.animation_data_create();act=bpy.data.actions.new(name);rig.animation_data.action=act;act.use_fake_user=True;events=[]
 for k in range(round(duration*fps)+1):
  t=k/fps;frame=k+1;reset();root=rig.pose.bones['Root']
  if name.startswith('A01'):
   root.location=(0,2*t,.008*(1-math.cos(t/1.1*4*math.pi)));wing=.08+.04*math.sin(math.pi*t/1.1)**2
   for side,phase in [('R',0),('L',math.pi)]:
    a=math.sin(t/1.1*2*math.pi+phase);setrot('Thigh.'+side,(1,0,0),.34*a);setrot('Shin.'+side,(1,0,0),-.32*max(0,-a));setrot('UpperArm.'+side,(1,0,0),-.10*a)
  elif name.startswith('A02'):
   root.location=(0,8*max(0,min(1,(t-.18)/.5)),-.06*curve(t,[(0,0),(.18,1),(.68,1),(.95,0)]));wing=curve(t,[(0,.1),(.18,.65),(.26,.9),(.68,.9),(.82,.7),(.95,.12)])
   lean=curve(t,[(0,0),(.18,1),(.68,1),(.95,0)]);setrot('Spine',(1,0,0),-.314*lean);setrot('Thigh.R',(1,0,0),.55*lean);setrot('Thigh.L',(1,0,0),-.45*lean);setrot('UpperArm.R',(1,0,0),.55*lean);setrot('Forearm.L',(1,0,0),-.30*lean)
  else:
   u=max(0,min(1,(t-.2)/.9));height=16*u*(1-u);travel=10*(3*u*u-2*u*u*u);root.location=(0,travel,height)
   wing=curve(t,[(0,.12),(.2,.65),(.45,1),(.7,1),(1.1,.75),(1.25,.55),(1.6,.12)])
   crouch=curve(t,[(0,0),(.15,1),(.23,0),(.65,.45),(1.1,0),(1.18,1),(1.6,0)])
   for side in ['R','L']:setrot('Thigh.'+side,(1,0,0),.5*crouch);setrot('Shin.'+side,(1,0,0),-.8*crouch)
   swing=curve(t,[(0,0),(.45,-2.2),(.7,-2.2),(1.02,.5),(1.6,0)]);setrot('UpperArm.R',(1,0,0),swing);setrot('Forearm.R',(1,0,0),-.25*math.sin(math.pi*t/1.6));setrot('UpperArm.L',(0,1,0),-.16);setrot('Forearm.L',(1,0,0),-.35)
  # Cloth chains use rotations only; front panels stay biased away from legs.
  for pb in rig.pose.bones:
   if not pb.name.startswith('Cloth.'):continue
   j=int(pb.name.rsplit('.',1)[1]);back='.Rear.' in pb.name;phase=0 if '.R.' in pb.name else math.pi
   if name.startswith('A01'):wind=(.18 if back else .05)+.025*math.sin(t/1.1*math.tau+phase-j*.5)
   elif name.startswith('A02'):wind=.04+.20*curve(t,[(0,0),(.18,1),(.68,1),(.95,0)])
   else:wind=.04+.16*curve(t,[(0,0),(.2,.5),(.65,1),(1.1,.6),(1.6,0)])
   angle=(-wind if back else wind*.42)*(1-j*.15)
   # Front tails open around the kneecap rather than repeatedly pitching through it.
   flare=.10 if back else (.18+.20*curve(t,[(0,0),(.18,1),(.68,1),(.95,0)]) if name.startswith('A02') else .18)
   side_angle=(-flare if '.R.' in pb.name else flare) if (back or j==0) else 0
   pb.rotation_quaternion=Quaternion((1,0,0),angle)@Quaternion((0,0,1),side_angle)
  root.location=rig.data.bones['Root'].matrix_local.to_quaternion().inverted()@root.location
  bpy.context.view_layer.update();contacts(name,t)
  cloth_avoidance(name,frame)
  wings(wing,frame);keyall(frame)
 if name.startswith('A03'):events=[('WingBeginOpen',0),('FootOff',.2),('Apex',.65),('SwingBegin',.7),('FootContact',1.1),('WingFoldComplete',1.6)]
 elif name.startswith('A02'):events=[('WingBeginOpen',0),('DashBegin',.18),('Brake',.68),('WingFoldComplete',.95)]
 for label,t in events:mark=act.pose_markers.new(label);mark.frame=1+round(t*fps)
 act['duration_seconds']=duration;act['root_motion_owner']='Root bone only; external controller must not add this displacement';clips.append({'name':name,'duration':duration,'frames':round(duration*fps)+1,'events':events})
for o,hidden in bake_hidden:o.hide_viewport=hidden
bpy.context.view_layer.update()
# Actual clip sample checks on the loaded armature, not only source equations.
checks=[]
for info in clips:
 rig.animation_data.action=bpy.data.actions[info['name']];end=info['frames'];s.frame_set(end);bpy.context.view_layer.update();root=rig.pose.bones['Root'];checks.append({'clip':info['name'],'end_root_world':list(root.matrix.translation),'end_WingOpen':rig['WingOpen']})
rig.animation_data.action=bpy.data.actions['A03_JumpAttack'];s.frame_set(40);bpy.context.view_layer.update();apex=list(rig.pose.bones['Root'].matrix.translation);assert abs(apex[2]-4)<1e-5,apex
assert abs(checks[1]['end_root_world'][1]-8)<1e-5;assert abs(checks[2]['end_root_world'][1]-10)<1e-5
rig.animation_data.action=bpy.data.actions['A01_SlowWalk'];s.frame_set(1);s.frame_start=1;s.frame_end=67
(P/'Docs/SanctuaryR3Build/ClothAvoidanceBake.json').write_text(json.dumps({'scope':'Per-frame minimal cloth-root rotation against evaluated leg surfaces; not continuous-time or full-scene collision proof','corrections':avoidance_records},indent=2))
rig['design_status']='Animated first pass. Folded feather bounds, foot IK and intersections NOT accepted.'
bpy.ops.wm.save_as_mainfile(filepath=str(P/'Blender/SanctuaryR3/Paladin_R3_Animated.blend'))
# Export all three baked clips, real skeleton and rigidly weighted meshes for the upcoming Unity pass.
bpy.ops.object.select_all(action='DESELECT');rig.select_set(True)
for o in s.objects:
 if o.type=='MESH' and any(m.type=='ARMATURE' and m.object==rig for m in o.modifiers):o.select_set(True)
bpy.context.view_layer.objects.active=rig
out=P/'Assets/Art/Bosses/Knight/SanctuaryR3';out.mkdir(parents=True,exist_ok=True)
bpy.ops.export_scene.fbx(filepath=str(out/'Paladin_R3_Animated.fbx'),use_selection=True,object_types={'MESH','ARMATURE'},add_leaf_bones=False,bake_anim=True,bake_anim_use_all_actions=True,bake_anim_use_nla_strips=False,bake_anim_simplify_factor=0,axis_forward='-Z',axis_up='Y',apply_unit_scale=True)
# One representative actual rendered frame for each action, with camera tracking Root motion.
s.cycles.samples=16;s.render.resolution_x=1000;s.render.resolution_y=800
for name,frame in [('A01_SlowWalk',1),('A02_Dash',25),('A03_JumpAttack',40)]:
 rig.animation_data.action=bpy.data.actions[name];s.frame_set(frame);bpy.context.view_layer.update();p=rig.pose.bones['Root'].matrix.translation;focus=Vector((0,p.y,1.5+p.z));cam=s.camera;cam.location=focus+Vector((4,8,2.1));cam.rotation_euler=(focus-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.ortho_scale=6.7;s.render.filepath=str(P/f'Docs/SanctuaryR3Build/{name}_FirstPass.png');bpy.ops.render.render(write_still=True)
(P/'Docs/SanctuaryR3Build/AnimationFirstPass.json').write_text(json.dumps({'stage':'first clips and FBX, not acceptance','bones':len(rig.data.bones),'individual_feather_bones':len(feathers),'clips':clips,'sampled_clip_values':checks,'jump_apex_root':apex,'pending':'foot locking, folded feather clearances, wing sweep, cloth motion, sword and shield poses, continuous videos, Unity integration'},indent=2));print('R3_ANIMATION_FIRST_PASS_COMPLETE')
