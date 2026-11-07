import bpy,math,json
from pathlib import Path
from mathutils import Vector
P=Path(__file__).resolve().parents[2]
bpy.ops.wm.open_mainfile(filepath=str(P/'Blender/SanctuaryR3/Sanctuary_R3_Windows.blend'))
s=bpy.context.scene;stone=bpy.data.materials['R3_Ivory_Marble_Foundation'];gold=bpy.data.materials['R3_Antique_Gold_Foundation'];iron=bpy.data.materials['R3_Gunmetal_Foundation']
for ob in s.objects:
 if ob.name.startswith('E05_EndTympanum') and min(v.co.y for v in ob.data.vertices)<0:
  for v in ob.data.vertices:v.co.y=-36
def mesh(name,v,f,mat):
 m=bpy.data.meshes.new(name);m.from_pydata(v,[],f);m.update();o=bpy.data.objects.new(name,m);s.collection.objects.link(o);o.data.materials.append(mat);return o
def line(name,pts,r,mat):
 c=bpy.data.curves.new(name,'CURVE');c.dimensions='3D';c.bevel_depth=r;c.bevel_resolution=2;sp=c.splines.new('POLY');sp.points.add(len(pts)-1)
 for p,v in zip(sp.points,pts):p.co=(*v,1)
 o=bpy.data.objects.new(name,c);s.collection.objects.link(o);o.data.materials.append(mat);return o
def box(name,loc,size,mat):
 bpy.ops.mesh.primitive_cube_add(size=1,location=loc);o=bpy.context.object;o.name=name;o.dimensions=size;bpy.ops.object.transform_apply(location=False,rotation=False,scale=True);o.data.materials.append(mat);return o
def profile(t):return math.sqrt(max(0,4-(1+abs(t))**2))/math.sqrt(3)
# Actual masonry above the south pointed opening, with outward thickness.
v=[]
for i in range(129):
 x=-5+10*i/128;v.extend([(x,-36,11+5*profile(x/5)),(x,-36,24)])
o=mesh('E07_PointArchSpandrel',v,[(2*i,2*i+1,2*i+3,2*i+2) for i in range(128)],stone);sol=o.modifiers.new('South masonry thickness','SOLIDIFY');sol.thickness=1.2;sol.offset=-1
for layer in range(5):
 w=5.2+.35*layer;rise=5.4+.65*layer;y=-35.0-.22*layer
 pts=[(-w,y,0),(-w,y,11)]+[(-w+2*w*k/96,y,11+rise*profile(-1+2*k/96)) for k in range(97)]+[(w,y,0)]
 line('E07_SouthPortalMoulding',pts,.13,stone)
 if layer in [1,3]:line('E07_SouthGoldReveal',[(x,y+.15,z) for x,y,z in pts],.023,gold)
# Small layered plinths keep the entrance at a continuous floor datum.
for side in [-1,1]:
 for layer in range(3):box('E07_JambPlinth',(side*5.9,-35.5,.2+layer*.25),(1.35-layer*.1,1.4-layer*.15,.25),stone)
# North gate rear straps and hinge barrels: complete physical assembly on both sides.
for side in [-1,1]:
 for z in [2.4,9.8,16.0]:
  bpy.ops.mesh.primitive_cylinder_add(vertices=24,radius=.13,depth=.7,location=(side*8,36,z));o=bpy.context.object;o.name='E06_HingeBarrel';o.data.materials.append(gold)
  for dz in [-.30,.30]:
   line('E06_HingeCollar',[(side*8+.145*math.cos(k*math.tau/48),36+.145*math.sin(k*math.tau/48),z+dz) for k in range(49)],.028,gold)
  box('E06_BackHingeStrap',(side*6.4,36.28,z),(3.1,.08,.22),iron)
 for x in [side*.35,side*7.6]:box('E06_RearVerticalBrace',(x,36.29,8.5),(.12,.08,16.5),iron)
 bpy.ops.object.empty_add(type='PLAIN_AXES',location=(side*8,36,0));bpy.context.object.name='E06_LeftHingePivot' if side<0 else 'E06_RightHingePivot';bpy.context.object['purpose']='Export pivot marker; door parenting and opening animation still pending'
s['design_version']='R3 entry and gate hardware pass - not accepted';s.cycles.samples=24
bpy.ops.wm.save_as_mainfile(filepath=str(P/'Blender/SanctuaryR3/Sanctuary_R3_Entry.blend'))
cam=s.camera;cam.location=(0,12,4);cam.rotation_euler=(Vector((0,-36,10))-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.lens=26
s.render.filepath=str(P/'Docs/SanctuaryR3Build/SouthEntry_Preview.png');bpy.ops.render.render(write_still=True)
print('R3_ENTRY_COMPLETE')
