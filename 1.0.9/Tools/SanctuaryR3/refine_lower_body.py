import bpy,bmesh,math,json
from pathlib import Path
from mathutils import Vector
P=Path(__file__).resolve().parents[2]
bpy.ops.wm.open_mainfile(filepath=str(P/'Blender/SanctuaryR3/Paladin_R3_Silhouette.blend'))
s=bpy.context.scene;rig=bpy.data.objects['Paladin_R3_Rig'];ivory=bpy.data.materials['R3_Armor_IvoryEnamel'];gold=bpy.data.materials['R3_Wings_AntiqueGold'];steel=bpy.data.materials['R3_Armor_ShadowSteel'];cloth=bpy.data.materials['R3_Waist_IvoryCloth'];new=[]
def mesh(n,v,f,m,b=None,thick=0,closed=False):
 me=bpy.data.meshes.new(n);me.from_pydata(v,[],f);me.update()
 if closed:bm=bmesh.new();bm.from_mesh(me);bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(me);bm.free()
 o=bpy.data.objects.new(n,me);s.collection.objects.link(o);o.data.materials.append(m)
 for p in me.polygons:p.use_smooth=True
 if thick:o.modifiers.new('Plate thickness','SOLIDIFY').thickness=thick
 mod=o.modifiers.new('Soft crafted edge','BEVEL');mod.width=.001;mod.segments=2
 if b:vg=o.vertex_groups.new(name=b);vg.add(list(range(len(v))),1,'REPLACE')
 o.modifiers.new('Armor and cloth skin','ARMATURE').object=rig;o.parent=rig;new.append(o);return o
def tube(n,pts,r,m,b):
 v=[];N=8
 for i,p in enumerate(pts):
  p=Vector(p);d=(Vector(pts[min(i+1,len(pts)-1)])-Vector(pts[max(i-1,0)])).normalized();u=d.cross(Vector((0,1,0)))
  if u.length<.01:u=Vector((1,0,0))
  u.normalize();vv=d.cross(u)
  for k in range(N):a=k*math.tau/N;v.append(p+r*(u*math.cos(a)+vv*math.sin(a)))
 f=[(j*N+k,j*N+(k+1)%N,(j+1)*N+(k+1)%N,(j+1)*N+k) for j in range(len(pts)-1) for k in range(N)];return mesh(n,v,f,m,b)
def loft(n,rows,cx,cy,m,b):
 N=48;v=[]
 for z,w,d in rows:
  for k in range(N):a=k*math.tau/N;v.append((cx+w*math.cos(a),cy+d*math.sin(a),z))
 f=[(j*N+k,j*N+(k+1)%N,(j+1)*N+(k+1)%N,(j+1)*N+k) for j in range(len(rows)-1) for k in range(N)];f.extend([tuple(reversed(range(N))),tuple((len(rows)-1)*N+k for k in range(N))]);return mesh(n,v,f,m,b,closed=True)
for o in list(s.objects):
 if o.name.startswith(('R05_Knee','R05_Greave','R05_Tasset','R06_Sabaton','C01_')):bpy.data.objects.remove(o,do_unlink=True)
loft('R01_NeckFlexibleUnderlayer',[(1.83,.085,.08),(1.93,.077,.076),(1.98,.072,.07)],0,0,steel,'Head')
loft('R02_LayeredGorget',[(1.83,.17,.12),(1.855,.125,.105),(1.885,.10,.09)],0,0,ivory,'Spine')
tube('R02_GorgetGoldLip',[(.10*math.cos(i*math.tau/64),.09*math.sin(i*math.tau/64),1.886) for i in range(65)],.004,gold,'Spine')
for side,sg in [('R',1),('L',-1)]:
 cx=sg*.18
 # Pointed, convex knee shell replacing spherical gold caps.
 outline=[(-.09,.04),(-.06,.10),(0,.14),(.06,.10),(.09,.04),(.065,-.07),(0,-.12),(-.065,-.07)]
 vv=[(cx,.205,.62)]+[(cx+x,.145,.62+z) for x,z in outline]
 mesh('R05_PointKneeShield',vv,[(0,i+1,(i+1)%8+1) for i in range(8)],ivory,'Shin.'+side,.004)
 tube('R05_KneeGoldBorder',[(cx+x,.148,.62+z) for x,z in outline+[outline[0]]],.0045,gold,'Shin.'+side)
 tube('R05_KneeCenterRidge',[(cx,.15,.50),(cx,.207,.62),(cx,.15,.76)],.004,gold,'Shin.'+side)
 loft('R05_TaperedGreave',[(.14,.072,.083),(.23,.075,.086),(.43,.095,.105),(.56,.106,.115)],cx,0,ivory,'Shin.'+side)
 tube('R05_GreaveGoldKeel',[(cx,.086,.15),(cx,.089,.23),(cx,.108,.43),(cx,.118,.56)],.0045,gold,'Shin.'+side)
 for z,w,d in [(.145,.073,.084),(.555,.106,.115)]:tube('R05_GreaveEdge',[(cx+w*math.cos(k*math.tau/64),d*math.sin(k*math.tau/64),z) for k in range(65)],.004,gold,'Shin.'+side)
 # Four curved overlapping tasset scales on each hip, with pointed scalloped edges.
 for j in range(4):
  xx=sg*(.245+j*.018);zz=1.19-j*.078;outline=[(-.085,.06),(.065,.06),(.105,-.025),(.015,-.115),(-.075,-.07)]
  vv=[(xx,.185,zz)]+[(xx+x,.135,zz+z) for x,z in outline]
  mesh('R05_CurvedTassetScale',vv,[(0,i+1,(i+1)%5+1) for i in range(5)],ivory,'Pelvis',.004)
  tube('R05_TassetGiltEdge',[(xx+x,.138,zz+z) for x,z in outline+[outline[0]]],.004,gold,'Pelvis')
 # Five curved articulated foot plates; .34m heel-to-toe envelope.
 for j in range(5):
  a=-.065+j*.065;v=[];width=.19-.009*j
  for y in [a,a+.08]:
   for k in range(17):t=k/16;xx=cx+(t-.5)*width;z=.02+(.11-.013*j)*math.sin(math.pi*t)**.55;v.append((xx,y,z))
  mesh('R06_CurvedSabatonLame',v,[(k,k+1,k+18,k+17) for k in range(16)],ivory,'Foot.'+side,.004)
  tube('R06_SabatonGoldHem',[(cx+(k/16-.5)*width,a+.08,.022+(.11-.013*j)*math.sin(math.pi*k/16)**.55) for k in range(17)],.003,gold,'Foot.'+side)
 # Closed dark sole and toe/heel caps prevent the sabaton reading as an empty arch.
 outline=[(-.08,-.065),(.08,-.065),(.095,0),(.09,.20),(.065,.275),(-.065,.275),(-.09,.20),(-.095,0)]
 vv=[(cx+x,y,z) for z in [.001,.024] for x,y in outline]
 ff=[tuple(reversed(range(8))),tuple(range(8,16))]+[(k,(k+1)%8,(k+1)%8+8,k+8) for k in range(8)]
 mesh('R06_ClosedSole',vv,ff,steel,'Foot.'+side,closed=True)
 for y,w,h in [(.275,.154,.058),(-.065,.19,.11)]:
  vv=[(cx,y,.02)]+[(cx+(k/16-.5)*w,y,.02+h*math.sin(math.pi*k/16)**.55) for k in range(17)]
  mesh('R06_ToeHeelClosure',vv,[(0,k+1,k+2) for k in range(16)],ivory,'Foot.'+side,.003)
# Three-bone chains for each of four narrow cloth panels; real weighted deformation.
bpy.ops.object.select_all(action='DESELECT');rig.select_set(True);bpy.context.view_layer.objects.active=rig;bpy.ops.object.mode_set(mode='EDIT')
panels=[]
for side,sg in [('R',1),('L',-1)]:
 for back in [False,True]:
  name=('Rear' if back else 'Front')+'.'+side;length=1.15 if back else .95;y=-.22 if back else .205;cx=sg*(.24 if back else .09)
  for j in range(3):
   b=rig.data.edit_bones.new('Cloth.'+name+'.'+str(j));b.head=(cx,y,1.30-length*j/3);b.tail=(cx,y,1.30-length*(j+1)/3);b.parent=rig.data.edit_bones['Pelvis' if j==0 else 'Cloth.'+name+'.'+str(j-1)];b.align_roll(Vector((0,1,0)))
  panels.append((name,sg,cx,y,length))
bpy.ops.object.mode_set(mode='OBJECT')
def weight(o,name,length):
 groups=[o.vertex_groups.new(name='Cloth.'+name+'.'+str(j)) for j in range(3)]
 for vert in o.data.vertices:
  t=max(0,min(1,(1.3-vert.co.z)/length));raw=[max(0,1-abs(t*3-.5-j)) for j in range(3)];total=sum(raw)
  for g,w in zip(groups,raw):
   if w:g.add([vert.index],w/total,'REPLACE')
for name,sg,cx,y,length in panels:
 v=[];f=[];rows=31;cols=13
 def point(t,u):
  taper=1-.8*max(0,(t-.87)/.13);x=cx+(u-.5)*(.16 if y>0 else .22)*taper;z=1.3-length*t;yy=y+(.06*math.sin(t*math.pi*.7) if y>0 else 0)+.012*math.sin(u*math.pi*4+.3)*math.sin(t*math.pi*.7);return Vector((x,yy,z))
 for j in range(rows):
  for i in range(cols):v.append(point(j/(rows-1),i/(cols-1)))
 for j in range(rows-1):
  for i in range(cols-1):a=j*cols+i;f.append((a,a+1,a+1+cols,a+cols))
 o=mesh('C01_FoldedPanel_'+name,v,f,cloth,thick=.002);weight(o,name,length)
 for edge in [.025,.975]:
  vv=[]
  for j in range(rows):
   t=j/(rows-1)
   for u in [edge-.015,edge+.015]:p=point(t,u);p.y+=.003 if y>0 else -.003;vv.append(p)
  o=mesh('C01_EmbroideredHem_'+name,vv,[(j*2,j*2+1,j*2+3,j*2+2) for j in range(rows-1)],gold,thick=.001);weight(o,name,length)
bpy.context.view_layer.update();bpy.ops.wm.save_as_mainfile(filepath=str(P/'Blender/SanctuaryR3/Paladin_R3_LowerBody.blend'))
(P/'Docs/SanctuaryR3Build/LowerBodyRound.json').write_text(json.dumps({'new_parts':len(new),'cloth_bones':12,'pre_animation_total_bones':len(rig.data.bones),'foot_envelope_length_m':.34,'front_cloth_bottom_m':.35,'rear_cloth_bottom_m':.15,'pending':'Cloth/leg collision, foot IK, cloth embroidered ornament, underarmor textures and final resemblance'},indent=2))
s.cycles.samples=24;s.render.resolution_x=1100;s.render.resolution_y=1400;s.render.resolution_percentage=100;s.camera.data.type='ORTHO';s.camera.data.ortho_scale=2.75;s.camera.location=(2.4,5,2.1);s.camera.rotation_euler=(Vector((0,0,1.3))-s.camera.location).to_track_quat('-Z','Y').to_euler();s.render.filepath=str(P/'Docs/SanctuaryR3Build/Armor_LowerBody_R4.png');bpy.ops.render.render(write_still=True)
print('LOWER_BODY_COMPLETE',len(new),len(rig.data.bones))
