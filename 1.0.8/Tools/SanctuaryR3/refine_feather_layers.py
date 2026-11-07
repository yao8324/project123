"""R9 feather surface and layered distribution, before animation baking."""
import bpy,math,json
from pathlib import Path
from mathutils import Vector
P=Path(__file__).resolve().parents[2];bpy.ops.wm.open_mainfile(filepath=str(P/'Blender/SanctuaryR3/Paladin_R3_Detail.blend'));s=bpy.context.scene;rig=bpy.data.objects['Paladin_R3_Rig'];gold=bpy.data.materials['R3_Wings_AntiqueGold'];report=[]
for o in list(s.objects):
 if o.type=='MESH' and o.name.startswith(('W01_R_Primary','W01_R_Secondary','W01_R_Covert','W01_L_Primary','W01_L_Secondary','W01_L_Covert')):bpy.data.objects.remove(o,do_unlink=True)
for side,sg in [('R',1),('L',-1)]:
 for group,count in [('Primary',8),('Secondary',6),('Covert',5)]:
  for j in range(count):
   t=j/(count-1)
   if group=='Primary':mount=1.35+1.15*t;length=1.485-.585*t;width=.22;angle=math.radians(9+10.5*t);layer=.035+.025*j;back=.36
   elif group=='Secondary':mount=.65+1.70*t;length=.62-.10*t;width=.30;angle=math.radians(46);layer=-.025+.008*j;back=.14
   else:mount=.48+1.88*t;length=.32+.05*t;width=.245;angle=math.radians(54);layer=-.075+.006*j;back=.08
   # Local corrections for R9 measured fold extrema and innermost shoulder contact.
   if group=='Secondary' and j==2:width=.16
   if group=='Covert' and j==2:width=.215
   if group=='Covert' and j==0:mount=.75
   part='Root' if mount<1.3 else 'Elbow' if mount<2.1 else 'Wrist';parent='Wing'+part+'.'+side;b=rig.data.bones[parent];axis=(b.tail_local-b.head_local).normalized();distance=(mount-abs(b.head_local.x))/abs(axis.x);head=b.head_local+axis*distance+Vector((0,-layer,0))
   direction=Vector((sg*math.sin(angle),-back,-math.sqrt(1-math.sin(angle)**2-back**2)));across=Vector((1,0,0));across=(across-direction*across.dot(direction)).normalized();front=direction.cross(across).normalized()
   if front.y<0:front=-front
   def half(u):return width*.5*(.72+.28*math.sin(math.pi*min(u/.55,1)/2))*(1 if u<=.65 else max(.0001,(1-u)/.35)**.7)
   def point(u,q,lift=0):
    # Curved tip and arched center, with asymmetric shallow edge notches.
    w=half(u);rib=.015*math.sin(math.pi*u)*(1-abs(q));return head+direction*(length*u)+across*(w*q)+front*(rib+lift)
   v=[];f=[]
   for k in range(49):
    u=k/48
    for q in [-1,0,1]:v.append(point(u,q))
   for k in range(48):
    for q in range(2):a=k*3+q;f.append((a,a+1,a+4,a+3))
   # Surface relief is part of the same rigid feather mesh, so it cannot detach while fanning.
   def ridge(path,hw,height):
    base=len(v)
    for u,q in path:
     dq=hw/max(half(u),.001);v.extend([point(u,q-dq,.001),point(u,q,height),point(u,q+dq,.001)])
    for k in range(len(path)-1):
     for q in range(2):a=base+k*3+q;f.append((a,a+1,a+4,a+3))
   ridge([(.03+.91*k/40,0) for k in range(41)],.003,.006)
   for k in range(12 if group=='Primary' else 7):
    u=.11+k*(.068 if group=='Primary' else .105)
    for sign in [-1,1]:ridge([(u+.06*t,sign*.86*t) for t in [a/8 for a in range(9)]],.0011,.0023)
   for sign in [-1,1]:ridge([(.035+.91*k/40,sign*.94) for k in range(41)],.0012,.002)
   name=f'W01_{side}_{group}_{j+1:02d}';me=bpy.data.meshes.new(name);me.from_pydata(v,[],f);me.update();o=bpy.data.objects.new(name,me);s.collection.objects.link(o);me.materials.append(gold);o.parent=rig;vg=o.vertex_groups.new(name=parent);vg.add(list(range(len(v))),1,'REPLACE');o.modifiers.new('Rigid wing attachment','ARMATURE').object=rig;o.modifiers.new('Metal feather thickness','SOLIDIFY').thickness=.006;be=o.modifiers.new('Rounded gold edges','BEVEL');be.width=.0007;be.segments=2
   for face in me.polygons:face.use_smooth=True
   o['feather_direction']=list(direction);o['feather_length']=length;o['fold_fan_angle']=sg*math.radians((j-(count-1)/2)*1.5)
   report.append({'name':name,'parent':parent,'mount_x':mount,'width':width,'length':length,'front_layer_offset':-layer})
bpy.ops.wm.save_as_mainfile(filepath=str(P/'Blender/SanctuaryR3/Paladin_R3_FeatherDetail.blend'))
(P/'Docs/SanctuaryR3Build/FeatherLayersR9.json').write_text(json.dumps({'feathers':report,'count':len(report),'scope':'New distribution and same-mesh relief; action envelopes and collisions pending'},indent=2));print('FEATHER_LAYERS_R9_COMPLETE',len(report))
