"""R16 broad form pass: compact pauldrons, fitted upper arms/chest and anatomical greaves.
Re-runs from a saved pre-R16 baseline; no cumulative deformation or rig/action edits.
"""
import bpy, math, json, shutil
from pathlib import Path
from mathutils import Vector
from mathutils.bvhtree import BVHTree
P=Path(__file__).resolve().parents[2]
current=P/'Blender/SanctuaryR3/Paladin_R3_Animated.blend'
baseline=P/'Blender/SanctuaryR3/Paladin_R3_PreFormR16.blend'
bpy.ops.wm.open_mainfile(filepath=str(current))
if bpy.data.objects['Paladin_R3_Rig'].get('armor_form_R16_applied'):
 assert baseline.exists(), 'Missing pre-R16 baseline'
 bpy.ops.wm.open_mainfile(filepath=str(baseline))
else: shutil.copy2(current,baseline)
s=bpy.context.scene;rig=bpy.data.objects['Paladin_R3_Rig'];rig.data.pose_position='REST'
ivory=bpy.data.materials['R3_Armor_IvoryEnamel'];gold=bpy.data.materials['R3_Wings_AntiqueGold'];steel=bpy.data.materials['R3_Armor_ShadowSteel']
changed=[];removed=[];created=[]
def interp(z,rows):
 if z<=rows[0][0]:return rows[0][1]
 if z>=rows[-1][0]:return rows[-1][1]
 for (a,x),(b,y) in zip(rows,rows[1:]):
  if a<=z<=b:return x+(y-x)*(z-a)/(b-a)
def bounds(prefixes):
 points=[o.matrix_world@v.co for o in s.objects if o.type=='MESH' and o.name.startswith(prefixes) for v in o.data.vertices]
 return {'min':[min(p[k] for p in points) for k in range(3)],'max':[max(p[k] for p in points) for k in range(3)],'size':[max(p[k] for p in points)-min(p[k] for p in points) for k in range(3)]}
shoulders=('R03_LayeredShoulder','R03_ShoulderGold','R03_ShoulderOuter','R03_R11_Shoulder')
before={'shoulders':bounds(shoulders),'breastplate':bounds(('R02_CurvedBreastShell',)),'greaves':bounds(('R05_TaperedGreave',))}
for o in list(s.objects):
 if o.type!='MESH':continue
 if o.name.startswith(('R02_SweptChestLeaf','R02_TaperedLeafBranch','R02_CentralKeel','R02_R7_ChestLaurel','R03_ArmUnderlayer')):
  removed.append(o.name);bpy.data.objects.remove(o,do_unlink=True);continue
 kind=None
 if o.name.startswith(shoulders):kind='shoulder'
 elif o.name.startswith(('R02_CurvedBreastShell','R02_SeparateBackShell')):kind='torso'
 elif o.name.startswith(('R05_TaperedGreave','R05_GreaveGoldKeel','R05_GreaveEdge')):kind='greave'
 elif o.name.startswith(('R05_PointKneeShield','R05_KneeGoldBorder','R05_KneeCenterRidge')):kind='knee'
 if kind:
  o.data=o.data.copy();inv=o.matrix_world.inverted()
  for v in o.data.vertices:
   p=o.matrix_world@v.co;sg=1 if p.x>=0 else -1
   if kind=='shoulder':
    p.x=sg*(.370+(abs(p.x)-.405)*.82);p.y*=.78;p.z=1.79+(p.z-1.79)*.78
   elif kind=='torso':
    p.x*=interp(p.z,[(1.315,.96),(1.5,.95),(1.71,.94),(1.86,.97)])
    if p.y>0:
     chest=math.exp(-((p.z-1.67)/.14)**2)
     p.y*=1+.07*chest
     p.y+=.009*math.exp(-(p.x/.038)**2)*math.sin(math.pi*max(0,min(1,(p.z-1.315)/.545)))
   elif kind=='greave':
    p.x=sg*.18+(p.x-sg*.18)*interp(p.z,[(.14,.75),(.23,.78),(.43,.96),(.56,.83)])
    p.y*=interp(p.z,[(.14,.82),(.23,.88),(.43,1.05),(.56,.82)])
    p.z-=.022*max(0,1-(p.z-.14)/.18)
   else:p.x=sg*.18+(p.x-sg*.18)*.88;p.z=.62+(p.z-.62)*.9
   v.co=inv@p
  changed.append({'object':o.name,'form':kind})
def mesh(name,v,f,mat,bone,solid=.001):
 me=bpy.data.meshes.new(name);me.from_pydata(v,[],f);me.update()
 o=bpy.data.objects.new(name,me);s.collection.objects.link(o);me.materials.append(mat);o.parent=rig
 for face in me.polygons:face.use_smooth=True
 if solid:o.modifiers.new('Forged thickness','SOLIDIFY').thickness=solid
 be=o.modifiers.new('Crafted edge','BEVEL');be.width=.0005;be.segments=2
 g=o.vertex_groups.new(name=bone);g.add(list(range(len(v))),1,'REPLACE');o.modifiers.new('Existing armor attachment','ARMATURE').object=rig;created.append(o)
 return o
# Build surface-fitted relief in one mesh, rather than one renderer per ornament.
shell=bpy.data.objects['R02_CurvedBreastShell']
tree=BVHTree.FromPolygons([shell.matrix_world@v.co for v in shell.data.vertices],[tuple(f.vertices) for f in shell.data.polygons])
def fit(x,z,lift=.002):
 hit=tree.ray_cast(Vector((x,1,z)),Vector((0,-1,0)))[0]
 assert hit is not None,(x,z)
 return (x,hit.y+lift,z)
vv=[];ff=[]
def append(v,f):
 offset=len(vv);vv.extend(v);ff.extend(tuple(offset+i for i in face) for face in f)
def ribbon(points,width):
 v=[]
 for i,(x,z) in enumerate(points):
  d=Vector(points[min(i+1,len(points)-1)])-Vector(points[max(0,i-1)]);n=Vector((-d.y,d.x)).normalized()*width/2
  v.extend([fit(x-n.x,z-n.y),fit(x+n.x,z+n.y)])
 append(v,[(2*j,2*j+1,2*j+3,2*j+2) for j in range(len(points)-1)])
def bezier(a,b,c,d,N=36):
 a,b,c,d=map(Vector,(a,b,c,d))
 return [tuple((1-t)**3*a+3*(1-t)**2*t*b+3*(1-t)*t*t*c+t**3*d) for t in [i/(N-1) for i in range(N)]]
def leaf(a,b,width):
 a,b=Vector(a),Vector(b);n=Vector((-(b-a).y,(b-a).x)).normalized();v=[]
 for j in range(13):
  t=j/12;c=a.lerp(b,t);w=width*math.sin(math.pi*t)**.8
  for u in [-1,0,1]:
   q=c+n*u*w;v.append(fit(q.x,q.y,.002+(.0015 if u==0 else 0)*math.sin(math.pi*t)))
 append(v,[(j*3+k,j*3+k+1,(j+1)*3+k+1,(j+1)*3+k) for j in range(12) for k in range(2)])
ribbon([(0,1.34+i*.48/48) for i in range(49)],.004)
for sg in [-1,1]:
 ribbon(bezier((sg*.018,1.34),(sg*.022,1.55),(sg*.16,1.68),(sg*.21,1.79)),.003)
 ribbon(bezier((sg*.025,1.82),(sg*.055,1.75),(sg*.10,1.70),(sg*.16,1.72)),.0025)
 ribbon(bezier((sg*.025,1.67),(sg*.05,1.70),(sg*.065,1.735),(sg*.06,1.78)),.0025)
 for j in range(7):
  z=1.39+j*.052;x=.028+.006*j+.0015*j*j
  leaf((sg*x,z),(sg*(x+.026),z+.043),.0045)
  if j>2:leaf((sg*(x-.007),z+.015),(sg*(x-.025),z+.046),.0035)
 for j in range(3):
  leaf((sg*(.025+j*.019),1.755+j*.012),(sg*(.020+j*.026),1.79+j*.012),.004)
mesh('R02_R16_FittedGiltRelief',vv,ff,gold,'Spine')
# Short upper-arm sleeves bridge the shoulder lames and elbow, following actual bones.
for side in ['R','L']:
 b=rig.data.bones['UpperArm.'+side];a=b.head_local;end=b.tail_local;axis=(end-a).normalized();front=Vector((0,1,0));front=(front-axis*front.dot(axis)).normalized();wide=axis.cross(front).normalized();N=48;v=[]
 under=[]
 for t in [-.02,.15,.5,.85,1.02]:
  for k in range(N):
   ang=math.tau*k/N;under.append(a.lerp(end,t)+wide*(.052*math.cos(ang))+front*(.051*math.sin(ang)))
 mesh('R03_R16_UpperArmUnderlayer_'+side,under,[(j*N+k,j*N+(k+1)%N,(j+1)*N+(k+1)%N,(j+1)*N+k) for j in range(4) for k in range(N)],steel,b.name,0)
 for j in range(7):
  t=.18+j*.65/6;rx=.071-.012*j/6;ry=.068-.012*j/6
  for k in range(N):
   ang=math.tau*k/N;v.append(a.lerp(end,t)+wide*(rx*math.cos(ang))+front*(ry*math.sin(ang)))
 mesh('R03_R16_FittedRerebrace_'+side,v,[(j*N+k,j*N+(k+1)%N,(j+1)*N+(k+1)%N,(j+1)*N+k) for j in range(6) for k in range(N)],ivory,b.name,.003)
 goldv=[];goldf=[]
 for row in [0,6]:
  off=len(goldv)
  for k in range(N):
   p=Vector(v[row*N+k]);goldv.extend([p+front*.001,p+front*.001+axis*(.004 if row==0 else -.004)])
  goldf.extend((off+2*k,off+2*((k+1)%N),off+2*((k+1)%N)+1,off+2*k+1) for k in range(N))
 mesh('R03_R16_RerebraceGilt_'+side,goldv,goldf,gold,b.name)
rig['armor_form_R16_applied']=True
report={'scope':'R16 broad armor form refinement; no final resemblance or full contact acceptance','before_rest_mesh_bounds_m':before,'after_rest_mesh_bounds_m':{'shoulders':bounds(shoulders),'breastplate':bounds(('R02_CurvedBreastShell',)),'greaves':bounds(('R05_TaperedGreave',))},'changed':changed,'removed_meshes':len(removed),'added_meshes':len(created),'rig_bones':len(rig.data.bones),'skinned_meshes':sum(o.type=='MESH' and o.parent==rig for o in s.objects),'retained_actions':[a.name for a in bpy.data.actions]}
rig.data.pose_position='POSE';bpy.ops.wm.save_as_mainfile(filepath=str(current))
(P/'Docs/SanctuaryR3Build/ArmorFormR16.json').write_text(json.dumps(report,indent=2))
# Reviews are transient display changes and are not saved to the delivered blend.
rig.data.pose_position='REST'
for o in s.objects:
 if o.name.startswith(('W01_','W02_','S01_','S02_','R01_R8_')):o.hide_render=True
s.cycles.samples=24;s.render.resolution_x=1000;s.render.resolution_y=1400;s.render.resolution_percentage=100;s.camera.data.type='ORTHO';s.camera.data.ortho_scale=2.55
for label,pos in [('Front',(0,6,1.2)),('ThreeQuarter',(3,6,1.6))]:
 s.camera.location=pos;s.camera.rotation_euler=(Vector((0,0,1.18))-s.camera.location).to_track_quat('-Z','Y').to_euler();s.render.filepath=str(P/f'Docs/SanctuaryR3Build/Knight_ArmorForm_R16_{label}.png');bpy.ops.render.render(write_still=True)
print('ARMOR_FORM_R16_COMPLETE',json.dumps(report))
