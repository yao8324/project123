import bpy,math,json
from pathlib import Path
from mathutils import Vector
P=Path(__file__).resolve().parents[2]
D=json.loads((P/'Docs/SanctuaryBlueprint56m20261107/data/dimensions.json').read_text())
# Dedicated background Blender process: does not touch the user's live document.
bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
scene=bpy.context.scene;scene.unit_settings.system='METRIC';scene.unit_settings.scale_length=1
scene['design_version']='R3 dimensional foundation - NOT final art'
def mat(name,col,metal=0,rough=.5):
 m=bpy.data.materials.new(name);m.diffuse_color=(*col,1);m.use_nodes=True
 n=next(n for n in m.node_tree.nodes if n.type=='BSDF_PRINCIPLED');n.inputs['Base Color'].default_value=(*col,1);n.inputs['Metallic'].default_value=metal;n.inputs['Roughness'].default_value=rough;return m
stone=mat('R3_Ivory_Marble_Foundation',(.72,.69,.62));gold=mat('R3_Antique_Gold_Foundation',(.61,.40,.15),1,.34);floor=mat('R3_Bluegrey_Marble_Foundation',(.13,.15,.18));iron=mat('R3_Gunmetal_Foundation',(.06,.07,.085),1,.5)
def box(name,loc,size,material):
 bpy.ops.mesh.primitive_cube_add(size=1,location=loc);o=bpy.context.object;o.name=name;o.dimensions=size;bpy.ops.object.transform_apply(location=False,rotation=False,scale=True);o.data.materials.append(material);return o
def shaft(name,x,z,r,h,bottom=0):
 bpy.ops.mesh.primitive_cylinder_add(vertices=48,radius=r,depth=h,location=(x,z,bottom+h/2));o=bpy.context.object;o.name=name;o.data.materials.append(stone)
 for p in o.data.polygons:p.use_smooth=True
 return o
def tube(name,pts,r,material):
 c=bpy.data.curves.new(name,'CURVE');c.dimensions='3D';c.resolution_u=1;c.bevel_depth=r;c.bevel_resolution=3;s=c.splines.new('POLY');s.points.add(len(pts)-1)
 for p,v in zip(s.points,pts):p.co=(*v,1)
 o=bpy.data.objects.new(name,c);scene.collection.objects.link(o);o.data.materials.append(material);return o
def arch(name,z,w,h,r,thick,material):
 pts=[]
 for i in range(129):
  x=-w+2*w*i/128;y=h+r*math.sqrt(max(0,4-(1+abs(x)/w)**2))/math.sqrt(3);pts.append((x,z,y))
 return tube(name,pts,thick,material)
box('E01_Floor_64x72',(0,0,-.175),(64,72,.35),floor)
for sign in [-1,1]:
 box('Side_wall_'+str(sign),(sign*32.6,0,12),(1.2,72,24),stone)
for sign in [-1,1]:
 # End wall split around actual gate openings.
 opening=16 if sign==1 else 10
 for side in [-1,1]:box('End_wall_pier',(side*(32+opening/2)/2,sign*36.6,12),((64-opening)/2,1.2,24),stone)
for x in D['column']['x_rows']:
 for z in D['column']['z_stations']:
  prefix=f'E02_Pier_{x}_{z}';box(prefix+'_Base',(x,z,.575),(2.8,2.8,1.15),stone)
  shaft(prefix+'_Core',x,z,.6,21.6,1.15)
  for k in range(8):
   a=k*math.pi/4;shaft(prefix+f'_Shaft{k}',x+.8*math.cos(a),z+.8*math.sin(a),.2,21.6,1.15)
  for y,h in [(12.2,.9),(22.75,1.25)]:
   o=shaft(prefix+'_CapitalEnvelope',x,z,1.35,h,y);o.data.materials.clear();o.data.materials.append(gold)
for z in D['column']['z_stations']:arch('E05_MainArch_'+str(z),z,30,24,12,.325,stone)
for r in D['floor']['inlay_radii']:tube('E01_Inlay_'+str(r),[(r*math.cos(i*math.tau/256),r*math.sin(i*math.tau/256),.001) for i in range(257)],.015,gold)
# Clearly named non-rendering combat guide.
guide=tube('GUIDE_Combat_Diameter_56m',[(28*math.cos(i*math.tau/128),28*math.sin(i*math.tau/128),.03) for i in range(129)],.025,gold);guide.hide_render=True
for sign in [-1,1]:box('E06_DoorLeaf_Foundation',(sign*4.04,36,8.5),(7.92,.50,17),iron)
for i in range(5):arch('E06_GateArch_'+str(i),35.1+i*.22,8.4+i*.5,17.2,7.6+i*.85,.18,stone)
# Reference silhouettes make human vs hall scale reviewable.
shaft('GUIDE_Knight_2p28m',0,25,.24,2.28)
box('GUIDE_Wingspan_5p8m',(0,25,1.78),(5.8,.06,.06),gold)
# Save the editable native foundation, then a portable geometry export.
scene.world.color=(.22,.22,.22)
bpy.ops.object.light_add(type='AREA',location=(0,-8,30));bpy.context.object.data.energy=7000;bpy.context.object.data.shape='DISK';bpy.context.object.data.size=28
bpy.ops.object.camera_add(location=(0,-32,6));cam=bpy.context.object;cam.rotation_euler=(Vector((0,15,15))-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.lens=22;scene.camera=cam
out=P/'Blender/SanctuaryR3';out.mkdir(parents=True,exist_ok=True)
bpy.ops.wm.save_as_mainfile(filepath=str(out/'Sanctuary_R3_Foundation.blend'))
report={'stage':'dimension foundation, not final art','floor_dimensions':list(bpy.data.objects['E01_Floor_64x72'].dimensions),'column_count':12,'main_arches':6,'objects':len(scene.objects),'combat_diameter':56,'required_remaining':'all detailed surfaces/props/character/rig/animation/Unity/acceptance'}
(P/'Docs/SanctuaryR3Build/Foundation.json').write_text(json.dumps(report,indent=2))
print('R3_FOUNDATION_SAVED',report)
