import bpy,math,json
from pathlib import Path
from mathutils import Vector,Matrix
P=Path(__file__).resolve().parents[2]
bpy.ops.wm.open_mainfile(filepath=str(P/'Blender/SanctuaryR3/Paladin_R3_FirstPass.blend'))
rig=bpy.data.objects['Paladin_R3_Rig'];gold=bpy.data.materials['R3_Wings_AntiqueGold'];s=bpy.context.scene
# Fixed attachment points on real segments. Long feathers hang from high folded joints.
for o in list(s.objects):
 if o.type=='MESH' and o.name.startswith(('W01_R_Primary','W01_R_Secondary','W01_R_Covert','W01_L_Primary','W01_L_Secondary','W01_L_Covert')):bpy.data.objects.remove(o,do_unlink=True)
records=[]
for side,sg in [('R',1),('L',-1)]:
 for group,count in [('Primary',8),('Secondary',6),('Covert',5)]:
  for j in range(count):
   t=j/(count-1)
   if group=='Primary':
    mount_x=1.35+(1.15/7)*j;part='Elbow' if mount_x<2.10 else 'Wrist';b=rig.data.bones['Wing'+part+'.'+side];axis=(b.tail_local-b.head_local).normalized();d=(mount_x-abs(b.head_local.x))/abs(axis.x)
    length=1.485-(.585/7)*j;width=.22;angle=math.radians(9+1.5*j);layer=.025*j+.035;backsweep=.36
   elif group=='Secondary':part='Root';d=.35+.135*j;length=.55+.07*j;width=.20;angle=math.radians(9);layer=.025*j+.065;backsweep=.24
   else:part='Root';d=.20+.17*j;length=.27+.035*j;width=.18;angle=math.radians(9);layer=.025*j+.095;backsweep=.12
   parent='Wing'+part+'.'+side;b=rig.data.bones[parent];head=b.head_local+(b.tail_local-b.head_local).normalized()*d+Vector((0,-layer,0))
   direction=Vector((sg*math.sin(angle),-backsweep,-math.sqrt(1-math.sin(angle)**2-backsweep**2)));across=Vector((1,0,0));across=(across-direction*across.dot(direction)).normalized()
   v=[];faces=[]
   for k in range(25):
    u=k/24;w=width*.5*(.72+.28*math.sin(math.pi*min(u/.55,1)/2))*(1 if u<=.65 else ((1-u)/.35)**.7)
    center=head+direction*length*u
    for q in [-1,0,1]:v.append(center+across*(w*q)+Vector((0,.022*math.sin(math.pi*u)*(1-abs(q)),0)))
   for k in range(24):
    for q in range(2):a=k*3+q;faces.append((a,a+1,a+4,a+3))
   name=f'W01_{side}_{group}_{j+1:02d}';me=bpy.data.meshes.new(name);me.from_pydata(v,[],faces);me.update();o=bpy.data.objects.new(name,me);s.collection.objects.link(o);o.data.materials.append(gold);o.parent=rig;vg=o.vertex_groups.new(name=parent);vg.add(list(range(len(v))),1,'REPLACE');o.modifiers.new('Rigid wing attachment','ARMATURE').object=rig;o.modifiers.new('Metal feather thickness','SOLIDIFY').thickness=.006;bev=o.modifiers.new('Rounded gold edges','BEVEL');bev.width=.0015;bev.segments=2
   o['feather_direction']=list(direction);o['feather_length']=length;o['fold_fan_angle']=sg*math.radians((j-(count-1)/2)*1.5)
   records.append({'name':name,'parent':parent,'length':length,'attachment':list(head),'width_m':width,'direction':list(direction),'layer_offset_m':layer})
rig['wing_revision']='Continuous mount layout R4; no feather scaling'
bpy.ops.wm.save_as_mainfile(filepath=str(P/'Blender/SanctuaryR3/Paladin_R3_WingLayout.blend'))
(P/'Docs/SanctuaryR3Build/WingLayout.json').write_text(json.dumps(records,indent=2));print('WING_LAYOUT_REBUILT',len(records))
