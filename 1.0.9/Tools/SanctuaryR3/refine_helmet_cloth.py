"""R17 pointed helmet, shaped gorget and fitted embroidered cloth. Existing rig/actions retained."""
import bpy, math, json, shutil
from pathlib import Path
from mathutils import Vector
P=Path(__file__).resolve().parents[2];current=P/'Blender/SanctuaryR3/Paladin_R3_Animated.blend';baseline=P/'Blender/SanctuaryR3/Paladin_R3_PreHelmetClothR17.blend'
bpy.ops.wm.open_mainfile(filepath=str(current))
if bpy.data.objects['Paladin_R3_Rig'].get('helmet_cloth_R17_applied'):
 assert baseline.exists();bpy.ops.wm.open_mainfile(filepath=str(baseline))
else:shutil.copy2(current,baseline)
s=bpy.context.scene;r=bpy.data.objects['Paladin_R3_Rig'];r.data.pose_position='REST';gold=bpy.data.materials['R3_Wings_AntiqueGold'];changes=[]
for o in s.objects:
 if o.type!='MESH':continue
 bone=o.vertex_groups[0].name if o.vertex_groups else ''
 kind=None
 if o.name.startswith('R01_') and bone=='Head' and not o.name.startswith('R01_R8_') and 'Neck' not in o.name:kind='helmet'
 elif o.name.startswith(('R02_R10_HighGorget','R02_R10_GorgetGiltLip')):kind='gorget'
 elif o.name.startswith('C01_'):kind='cloth'
 if kind:
  o.data=o.data.copy();inv=o.matrix_world.inverted()
  for v in o.data.vertices:
   p=o.matrix_world@v.co
   if kind=='helmet':
    z=p.z;p.x*=.93-.10*math.exp(-((z-2.02)/.065)**2)
    if p.y>0:p.y=p.y*.96+.014*math.exp(-(p.x/.033)**2)*math.exp(-((z-2.07)/.11)**2)
    p.z=2.28+(z-2.28)*1.08+.005*(abs(p.x)/.12)*math.exp(-((z-2.10)/.055)**2)
   elif kind=='gorget':
    p.z-=.026*max(0,min(1,(p.z-1.89)/.056))*max(0,p.y/.085)**2
   else:
    front='Front.' in o.name;right='.R' in o.name;length=.95 if front else 1.15;cx=(1 if right else -1)*(.09 if front else .24);t=max(0,min(1,(1.3-p.z)/length));dx=p.x-cx
    p.x=cx+dx*(.90 if front else .94)
    p.y+=(.035 if front else -.012)*math.sin(math.pi*t*.85)+.008*math.sin(math.pi*t*2.4)*(dx/(.08 if front else .11))
    p.z+=.006*math.sin(dx/(.08 if front else .11)*math.pi)*t*t
   v.co=inv@p
  changes.append({'object':o.name,'revision':kind})
# Sample embroidery directly from the modified cloth grid and interpolate its actual skin weights.
created=[]
# Three thin articulation bands break up the exposed neck underlayer without filling the joint.
nv=[];nf=[];N=64
for z in [1.930,1.943,1.956]:
 off=len(nv)
 for zz in [z,z+.0025]:
  rx=.077-.005*(zz-1.93)/.05+.0015;ry=.076-.006*(zz-1.93)/.05+.0015
  for k in range(N):
   a=k*math.tau/N;nv.append((rx*math.cos(a),ry*math.sin(a),zz))
 nf.extend((off+k,off+(k+1)%N,off+N+(k+1)%N,off+N+k) for k in range(N))
me=bpy.data.meshes.new('R17_NeckArticulation');me.from_pydata(nv,[],nf);me.update();o=bpy.data.objects.new('R01_R17_NeckArticulationGilt',me);s.collection.objects.link(o);o.parent=r;me.materials.append(gold)
g=o.vertex_groups.new(name='Head');g.add(list(range(len(nv))),1,'REPLACE');o.modifiers.new('Neck seam thickness','SOLIDIFY').thickness=.0005;o.modifiers.new('Head attachment','ARMATURE').object=r;created.append(o.name)
for panel in [o for o in s.objects if o.name.startswith('C01_FoldedPanel_')]:
 label=panel.name.split('C01_FoldedPanel_')[1];front=label.startswith('Front');rows=31;cols=13;v=[];f=[];weights=[]
 def point(t,u,lift=.0015):
  yy=max(0,min(rows-1-1e-6,t*(rows-1)));xx=max(0,min(cols-1-1e-6,u*(cols-1)));j=int(yy);i=int(xx);a=yy-j;b=xx-i
  ids=[(j*cols+i,(1-a)*(1-b)),(j*cols+i+1,(1-a)*b),((j+1)*cols+i,a*(1-b)),((j+1)*cols+i+1,a*b)]
  p=Vector();w={}
  for index,mix in ids:
   vert=panel.data.vertices[index];p+=(panel.matrix_world@vert.co)*mix
   for g in vert.groups:
    name=panel.vertex_groups[g.group].name;w[name]=w.get(name,0)+g.weight*mix
  p.y+=lift if front else -lift
  return p,w
 def add(t,u,lift=.0015):
  p,w=point(t,u,lift);v.append(p);weights.append(w);return len(v)-1
 # One flowing stem and alternating raised leaves, together in one mesh per panel.
 off=len(v)
 for j in range(45):
  t=.09+j*.77/44;u=.5+.028*math.sin(t*math.pi*2)
  add(t,u-.008);add(t,u+.008)
 f.extend((off+2*j,off+2*j+1,off+2*j+3,off+2*j+2) for j in range(44))
 for side in [-1,1]:
  for k in range(8):
   start=.12+k*.086;off=len(v)
   for j in range(11):
    q=j/10;t=start+.047*q;u=.5+side*(.015+.14*q)
    for edge in [-1,0,1]:add(t,u+edge*.027*math.sin(math.pi*q),.0015+(.001 if edge==0 else 0)*math.sin(math.pi*q))
   f.extend((off+3*j+a,off+3*j+a+1,off+3*(j+1)+a+1,off+3*(j+1)+a) for j in range(10) for a in range(2))
 me=bpy.data.meshes.new('R17_ClothRelief_'+label);me.from_pydata(v,[],f);me.update();o=bpy.data.objects.new('C01_R17_WovenLaurel_'+label,me);s.collection.objects.link(o);o.parent=r;me.materials.append(gold)
 for poly in me.polygons:poly.use_smooth=True
 for name in {name for w in weights for name in w}:
  group=o.vertex_groups.new(name=name)
  for i,w in enumerate(weights):
   if w.get(name,0)>0:group.add([i],w[name],'REPLACE')
 o.modifiers.new('Embroidered thickness','SOLIDIFY').thickness=.0006;o.modifiers.new('Existing cloth skin','ARMATURE').object=r;created.append(o.name)
r['helmet_cloth_R17_applied']=True;r.data.pose_position='POSE';bpy.ops.wm.save_as_mainfile(filepath=str(current))
report={'scope':'Helmet/gorget/cloth geometry and weighted decoration only; not final art or collision acceptance','changed':changes,'added_meshes':created,'bones':len(r.data.bones),'skinned_meshes':sum(o.type=='MESH' and o.parent==r for o in s.objects),'actions':[a.name for a in bpy.data.actions]}
(P/'Docs/SanctuaryR3Build/HelmetClothR17.json').write_text(json.dumps(report,indent=2))
r.data.pose_position='REST'
for o in s.objects:
 if o.name.startswith(('W01_','W02_','S01_','S02_','R01_R8_')):o.hide_render=True
s.cycles.samples=24;s.render.resolution_x=1000;s.render.resolution_y=1400;s.render.resolution_percentage=100;s.camera.data.type='ORTHO';s.camera.data.ortho_scale=2.55;s.camera.location=(3,6,1.6);s.camera.rotation_euler=(Vector((0,0,1.18))-s.camera.location).to_track_quat('-Z','Y').to_euler();s.render.filepath=str(P/'Docs/SanctuaryR3Build/Knight_HelmetCloth_R17.png');bpy.ops.render.render(write_still=True)
s.render.resolution_y=1000;s.camera.data.ortho_scale=.70;s.camera.location=(.6,3,2.15);s.camera.rotation_euler=(Vector((0,0,2.01))-s.camera.location).to_track_quat('-Z','Y').to_euler();s.render.filepath=str(P/'Docs/SanctuaryR3Build/Knight_Head_R17.png');bpy.ops.render.render(write_still=True)
print('HELMET_CLOTH_R17_COMPLETE',json.dumps(report))
