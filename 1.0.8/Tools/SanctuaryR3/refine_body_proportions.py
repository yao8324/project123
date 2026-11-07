"""R10 user-priority body proportions and fitted armor, preserving skeleton/action lengths."""
import bpy,bmesh,math,json
from pathlib import Path
from mathutils import Vector
P=Path(__file__).resolve().parents[2];bpy.ops.wm.open_mainfile(filepath=str(P/'Blender/SanctuaryR3/Paladin_R3_Hardware.blend'));s=bpy.context.scene;rig=bpy.data.objects['Paladin_R3_Rig'];gold=bpy.data.materials['R3_Wings_AntiqueGold'];ivory=bpy.data.materials['R3_Armor_IvoryEnamel'];steel=bpy.data.materials['R3_Armor_ShadowSteel'];created=[];changed=[]
# Always run from a fresh animation/hardware output, never repeatedly shrink an existing R10 result.
assert not rig.get('body_proportions_R10_applied',False),'Rebuild animation/hardware baseline before applying R10 again'
for o in list(s.objects):
 if o.type!='MESH':continue
 bone=o.vertex_groups[0].name if o.vertex_groups else ''
 if o.name.startswith(('R02_WaistLame','R03_Vambrace','R03_ElbowCap')):bpy.data.objects.remove(o,do_unlink=True);continue
 kind=None
 if o.name.startswith('R01_') and bone=='Head' and 'Neck' not in o.name:kind='helmet'
 elif o.name.startswith('R02_'):kind='torso'
 elif o.name.startswith('R03_ArmUnderlayer'):kind='upperarm'
 elif o.name.startswith(('R05_CurvedTassetScale','R05_TassetGiltEdge')):kind='hiparmor'
 elif o.name.startswith('R05_ThighArmor'):kind='thigh'
 if kind:
  o.data=o.data.copy();inv=o.matrix_world.inverted()
  for v in o.data.vertices:
   p=o.matrix_world@v.co
   if kind=='helmet':p.x*=.86;p.y*=.90;p.z=2.28+(p.z-2.28)*.86
   elif kind=='torso':f=.80+.10*max(0,min(1,(p.z-1.315)/.40));p.x*=f;p.y*=.86
   elif kind=='hiparmor':p.x*=.86;p.y*=.90
   elif kind=='thigh':cx=.18 if p.x>0 else -.18;p.x=cx+(p.x-cx)*.82;p.y*=.85
   else:cx=.49 if p.x>0 else -.49;p.x=cx+(p.x-cx)*.72;p.y*=.74
   v.co=inv@p
  changed.append({'object':o.name,'change':kind})
def mesh(n,v,f,m,b,solid=0):
 me=bpy.data.meshes.new(n);me.from_pydata(v,[],f);me.update();o=bpy.data.objects.new(n,me);s.collection.objects.link(o);me.materials.append(m)
 for p in me.polygons:p.use_smooth=True
 if solid:o.modifiers.new('Forged shell','SOLIDIFY').thickness=solid
 be=o.modifiers.new('Fine edge','BEVEL');be.width=.0008;be.segments=2;vg=o.vertex_groups.new(name=b);vg.add(list(range(len(v))),1,'REPLACE');o.modifiers.new('Rigid armor skin','ARMATURE').object=rig;o.parent=rig;created.append(o);return o
def loft(n,rows,m,b):
 v=[];N=64
 for z,rx,ry in rows:
  for i in range(N):a=i*math.tau/N;v.append((rx*math.cos(a),ry*math.sin(a),z-.018*max(0,math.sin(a))**4))
 return mesh(n,v,[(j*N+i,j*N+(i+1)%N,(j+1)*N+(i+1)%N,(j+1)*N+i) for j in range(len(rows)-1) for i in range(N)],m,b,.003)
loft('R02_R10_WaistFlexibleUnderlayer',[(1.17,.174,.115),(1.38,.16,.119)],steel,'Pelvis')
for j in range(3):
 z=1.19+j*.055;rx=.184-j*.005
 loft('R02_R10_TaperedWaistLame',[(z,rx,.126),(z+.035,rx-.002,.13),(z+.075,rx-.007,.128)],ivory,'Pelvis')
 loft('R02_R10_FlatWaistGilt',[(z+.001,rx+.001,.128),(z+.006,rx+.001,.128)],gold,'Pelvis')
loft('R02_R10_HighGorget',[(1.865,.112,.101),(1.90,.096,.090),(1.945,.087,.084)],ivory,'Spine')
loft('R02_R10_GorgetGiltLip',[(1.941,.088,.085),(1.946,.088,.085)],gold,'Spine')
for side,sg in [('R',1),('L',-1)]:
 bone='Forearm.'+side;b=rig.data.bones[bone];a=b.head_local.copy();end=b.tail_local.copy();axis=(end-a).normalized();front=Vector((0,1,0));front=(front-axis*front.dot(axis)).normalized();wide=axis.cross(front).normalized();N=64;v=[]
 for j in range(9):
  t=.12+.91*j/8;rx=.086-(t-.12)/.91*.031;ry=.070-(t-.12)/.91*.022
  for k in range(N):
   angle=k*math.tau/N;tt=t-(.075*max(0,math.sin(angle))**4 if j==0 else 0);p=a.lerp(end,tt)+wide*rx*math.cos(angle)+front*ry*math.sin(angle);v.append(p)
 mesh('R03_R10_TaperedVambrace',v,[(j*N+k,j*N+(k+1)%N,(j+1)*N+(k+1)%N,(j+1)*N+k) for j in range(8) for k in range(N)],ivory,bone,.004)
 for row in [0,8]:
  vv=[]
  for k in range(N):
   p=Vector(v[row*N+k]);vv.extend([p+front*.001,p+front*.001+axis*(.006 if row==0 else -.006)])
  mesh('R03_R10_VambraceEdge',vv,[(2*k,2*((k+1)%N),2*((k+1)%N)+1,2*k+1) for k in range(N)],gold,bone,.001)
 vv=[]
 for j in range(9):
  p=Vector(v[j*N+N//4])+front*.003;vv.extend([p-wide*.004,p+wide*.004])
 mesh('R03_R10_VambraceGoldKeel',vv,[(2*j,2*j+1,2*j+3,2*j+2) for j in range(8)],gold,bone,.001)
 # Replace the bulbous gold elbow with a pointed ivory couter.
 c=a+front*.082;outline=[(-.07,.035),(-.045,.09),(.02,.085),(.09,.005),(.04,-.07),(-.04,-.055)];vv=[c+front*.026]+[c+wide*x-axis*z for x,z in outline]
 mesh('R03_R10_PointElbowCouter',vv,[(0,i+1,(i+1)%len(outline)+1) for i in range(len(outline))],ivory,bone,.004)
rig['body_proportions_R10_applied']=True
bpy.ops.wm.save_as_mainfile(filepath=str(P/'Blender/SanctuaryR3/Paladin_R3_Animated.blend'))
bpy.ops.object.select_all(action='DESELECT');rig.select_set(True)
for o in s.objects:
 if o.type=='MESH' and any(m.type=='ARMATURE' and m.object==rig for m in o.modifiers):o.select_set(True)
bpy.context.view_layer.objects.active=rig
bpy.ops.export_scene.fbx(filepath=str(P/'Assets/Art/Bosses/Knight/SanctuaryR3/Paladin_R3_Animated.fbx'),use_selection=True,object_types={'MESH','ARMATURE'},add_leaf_bones=False,bake_anim=True,bake_anim_use_all_actions=True,bake_anim_use_nla_strips=False,bake_anim_simplify_factor=0,axis_forward='-Z',axis_up='Y',apply_unit_scale=True)
# Unobscured body review in rest pose; hide accessories only for these review images.
rig.data.pose_position='REST'
for o in s.objects:
 if o.name.startswith(('W01_','W02_','S01_','S02_','R01_R8_')):o.hide_render=True
bpy.context.view_layer.update();dg=bpy.context.evaluated_depsgraph_get();bounds={}
for label,prefix in [('helmet','R01_R7_OpenVisorHelmet'),('chest','R02_CurvedBreastShell'),('waist','R02_R10_TaperedWaistLame')]:
 points=[]
 for o in s.objects:
  if o.type=='MESH' and o.name.startswith(prefix):
   ev=o.evaluated_get(dg);me=ev.to_mesh();points.extend(ev.matrix_world@v.co for v in me.vertices);ev.to_mesh_clear()
 lo=[min(p[i] for p in points) for i in range(3)];hi=[max(p[i] for p in points) for i in range(3)];bounds[label]={'min':lo,'max':hi,'size':[hi[i]-lo[i] for i in range(3)]}
(P/'Docs/SanctuaryR3Build/BodyProportionsR10.json').write_text(json.dumps({'changed':changed,'new_parts':len(created),'rest_bounds_blender':bounds,'scope':'User-requested visual proportion revision; skeleton lengths and action timing preserved, collisions not certified'},indent=2))
s.cycles.samples=24;s.render.resolution_x=1000;s.render.resolution_y=1400;s.render.resolution_percentage=100;s.camera.data.type='ORTHO';s.camera.data.ortho_scale=2.55
for label,pos in [('Front',(0,6,1.2)),('ThreeQuarter',(3,6,1.6))]:
 s.camera.location=pos;s.camera.rotation_euler=(Vector((0,0,1.18))-s.camera.location).to_track_quat('-Z','Y').to_euler();s.render.filepath=str(P/f'Docs/SanctuaryR3Build/Knight_Body_R10_{label}.png');bpy.ops.render.render(write_still=True)
print('BODY_PROPORTIONS_R10_COMPLETE',len(changed),len(created),bounds)
