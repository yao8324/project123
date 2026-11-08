import bpy,math,csv,json,random
from pathlib import Path
from mathutils import Vector
P=Path(__file__).resolve().parents[2]
bpy.ops.wm.open_mainfile(filepath=str(P/'Blender/SanctuaryR3/Sanctuary_R3_Entry.blend'))
s=bpy.context.scene;gold=bpy.data.materials['R3_Antique_Gold_Foundation'];steel=bpy.data.materials['R3_Gunmetal_Foundation']
def mat(name,c,rough=.6,metal=0):
 m=bpy.data.materials.new(name);m.diffuse_color=(*c,1);m.use_nodes=True;n=next(n for n in m.node_tree.nodes if n.type=='BSDF_PRINCIPLED');n.inputs['Base Color'].default_value=(*c,1);n.inputs['Roughness'].default_value=rough;n.inputs['Metallic'].default_value=metal;return m
wood=mat('R3_Prop_Walnut',(.075,.032,.014));leather=mat('R3_Prop_DarkLeather',(.052,.021,.012),.55);rope=mat('R3_Prop_BlueCord',(.018,.09,.23),.8);cloth=mat('R3_Prop_IvoryLinen',(.65,.57,.40),.9);paper=mat('R3_Prop_InventoryPaper',(.66,.54,.33),.95)
# Authored wood/leather maps; object coordinates preserve physical scale.
for material,stem in [(wood,'Wood'),(leather,'Leather')]:
 ns=material.node_tree.nodes;ls=material.node_tree.links;bs=next(n for n in ns if n.type=='BSDF_PRINCIPLED');tex=ns.new('ShaderNodeTexImage');tex.image=bpy.data.images.load(str(P/f'Assets/Art/Bosses/Knight/Sanctuary20261107/Textures/{stem}_Albedo.png'),check_existing=True);tex.projection='BOX';tex.projection_blend=.2;co=ns.new('ShaderNodeTexCoord');ls.new(co.outputs['Object'],tex.inputs['Vector']);ls.new(tex.outputs['Color'],bs.inputs['Base Color']);b=ns.new('ShaderNodeBump');b.inputs['Strength'].default_value=.16;b.inputs['Distance'].default_value=.002;ls.new(tex.outputs['Color'],b.inputs['Height']);ls.new(b.outputs['Normal'],bs.inputs['Normal'])
root=None
created=[]
def attach(o):
 if root:o.parent=root
 created.append(o);return o
def box(name,p,d,m,bevel=.006):
 bpy.ops.mesh.primitive_cube_add(size=1,location=p);o=bpy.context.object;o.name=name;o.dimensions=d;bpy.ops.object.transform_apply(location=False,rotation=False,scale=True);o.data.materials.append(m)
 if bevel:b=o.modifiers.new('Crafted edge','BEVEL');b.width=bevel;b.segments=2
 return attach(o)
def line(name,pts,r,m):
 c=bpy.data.curves.new(name,'CURVE');c.dimensions='3D';c.bevel_depth=r;c.bevel_resolution=2;sp=c.splines.new('POLY');sp.points.add(len(pts)-1)
 for p,v in zip(sp.points,pts):p.co=(*v,1)
 o=bpy.data.objects.new(name,c);s.collection.objects.link(o);o.data.materials.append(m);return attach(o)
def sphere(name,p,r,m):
 bpy.ops.mesh.primitive_uv_sphere_add(segments=12,ring_count=8,radius=r,location=p);o=bpy.context.object;o.name=name;o.data.materials.append(m);return attach(o)
def cyl(name,p,r,h,m):
 bpy.ops.mesh.primitive_cylinder_add(vertices=24,radius=r,depth=h,location=p);o=bpy.context.object;o.name=name;o.data.materials.append(m);return attach(o)
def ring(name,p,r,m,axis='Y',thick=.008,start=0,end=360):
 pts=[]
 for i in range(65):
  a=math.radians(start+(end-start)*i/64);x=r*math.cos(a);y=r*math.sin(a);pts.append((p[0]+x,p[1]+(y if axis=='Z' else 0),p[2]+(y if axis=='Y' else 0)))
 return line(name,pts,thick,m)
def chest():
 box('P01_WoodCarcass',(0,0,.34),(1.22,.69,.66),wood)
 for j in range(6):box('P01_LeatherPanel',(-.5+j*.2,-.349,.34),(.195,.012,.57),leather,.003)
 box('P01_SeparateLid',(0,0,.705),(1.23,.70,.11),leather)
 for x in [-.59,.59]:
  for y in [-.329,.329]:
   box('P01_GoldCorner',(x,y,.38),(.045,.045,.72),gold,.004)
   for z in [.08,.22,.44,.60,.73]:sphere('P01_CornerRivet',(x,y-.025 if y<0 else y+.025,z),.009,gold)
 for z in [.04,.64,.75]:
  for y in [-.351,.351]:box('P01_HorizontalBand',(0,y,z),(1.23,.006,.045),gold,.002)
 for x in [-.4,.4]:
  box('P01_HingeLeaf',(x,.355,.65),(.10,.012,.14),gold)
  h=cyl('P01_HingeBarrel',(x,.365,.67),.018,.12,gold);h.rotation_euler.y=math.pi/2
  # Bound blue cord, physically follows lid, front and underside.
  pts=[(x,-.36,.025),(x,-.36,.765),(x,.36,.765),(x,.36,.025),(x,-.36,.025)]
  line('P01_BlueBindingCord',pts,.009,rope)
  line('P01_BindingLooseTail',[(x,-.37,.58),(x+.02,-.375,.42),(x-.01,-.38,.31)],.009,rope)
 box('P01_Latch',(0,-.363,.62),(.08,.014,.14),gold)
 ring('P01_BrokenRingSeal',(0,-.375,.42),.105,gold,start=20,end=340)
 line('P01_SealCord',[(0,-.37,.66),(0,-.378,.52)],.009,rope)
 for x in [-.62,.62]:line('P01_SideHandle',[(x,-.12,.48),(x*1.04,-.12,.38),(x*1.04,.12,.38),(x,.12,.48)],.016,steel)
def bench():
 for i in range(5):box('P02_TopPlank',(0,-.3+i*.15,.865),(1.8,.145,.07),wood)
 for x in [-.77,.77]:
  for y in [-.26,.26]:box('P02_Leg',(x,y,.415),(.09,.09,.83),wood)
 box('P02_LowerStretcher',(0,0,.25),(1.62,.055,.07),wood)
 for x in [-.77,.77]:box('P02_EndBrace',(x,0,.27),(.07,.63,.10),wood)
 # Cloth drapes from tabletop to front; folds are actual geometry.
 v=[];f=[]
 for j in range(26):
  t=j/25;yy=.35-1.0*min(t,.75);zz=.925 if t<=.75 else .925-(t-.75)
  for i in range(21):xx=-.75+.75*i/20;v.append((xx,yy,zz+.008*math.sin(i*.9)*(1+t)))
 for j in range(25):
  for i in range(20):a=j*21+i;f.append((a,a+1,a+22,a+21))
 m=bpy.data.meshes.new('Linen');m.from_pydata(v,[],f);m.update();o=bpy.data.objects.new('P02_DrapedLinen',m);s.collection.objects.link(o);o.data.materials.append(cloth);attach(o);o.modifiers.new('Woven thickness','SOLIDIFY').thickness=.001
 # Human-scale tool silhouettes.
 box('P02_WrenchStem',(.25,-.05,.92),(.028,.15,.018),steel)
 ring('P02_WrenchOpenJaw',(.25,.055,.92),.035,steel,axis='Z',thick=.009,start=35,end=325)
 ring('P02_WrenchRingEnd',(.25,-.14,.92),.023,steel,axis='Z',thick=.009)
 box('P02_ChiselHandle',(.45,-.06,.93),(.027,.10,.028),wood)
 box('P02_ChiselBlade',(.45,.03,.928),(.018,.08,.009),steel,.002)
 for sign in [-1,1]:line('P02_PliersArm',[(.65+sign*.035,-.15,.925),(.65,-.02,.925),(.65+sign*.025,.09,.925)],.009,steel)
 cyl('P02_OilCan',(-.1,.1,.97),.045,.12,gold)
 line('P02_OilSpout',[(-.07,.1,1.01),(.03,.1,1.05)],.007,gold)
def stand():
 box('P03_Base',(0,0,.04),(.52,.46,.08),wood);box('P03_Post',(0,.10,.60),(.065,.065,1.12),wood)
 board=box('P03_InventoryBoard',(0,0,1.08),(.55,.025,.75),wood);board.rotation_euler.x=-math.radians(18)
 sheet=box('P03_ReplaceablePaper',(0,-.022,1.08),(.48,.003,.66),paper,.001);sheet.rotation_euler.x=-math.radians(18)
 # Abstract engraved inventory strokes rather than fabricated readable narrative text.
 for row in range(10):
  z=.83+row*.045;y=-.032+(z-1.08)*math.sin(math.radians(18));line('P03_ListStroke',[(-.18,y,z),(.10+(row%3)*.025,y,z)],.0018,steel)
def cradle():
 for x in [-.38,.38]:
  box('P04_CradleFoot',(x,0,.065),(.12,.75,.13),wood)
  for y in [-.30,.30]:box('P04_EmptySupport',(x,y,.23),(.12,.10,.24),wood)
 box('P04_EmptyCrossBrace',(0,0,.12),(.95,.08,.08),wood)
def lamp():
 # Local +Y points into the hall; CSV yaw is converted on the parent.
 line('P06_WallBracket',[(0,0,.60),(0,.43,.60),(0,.43,.52)],.018,gold)
 box('P06_Backplate',(0,.015,.4),(.14,.04,.45),gold)
 y=.27
 for z in [.035,.585]:box('P06_LanternFrame',(0,y,z),(.35,.32,.035),gold)
 for x in [-.16,.16]:
  for yy in [y-.145,y+.145]:line('P06_LanternUpright',[(x,yy,.04),(x,yy,.58)],.012,gold)
 roof=cyl('P06_LanternCrown',(0,y,.63),.20,.12,gold);roof.scale=(1,.8,1)
 glow=mat('R3_LanternWarmGlass',(.45,.16,.025),.32);b=next(n for n in glow.node_tree.nodes if n.type=='BSDF_PRINCIPLED');b.inputs['Emission Color'].default_value=(1,.29,.045,1);b.inputs['Emission Strength'].default_value=2
 box('P06_WarmGlass',(0,y,.31),(.29,.26,.48),glow,.01)
 bpy.ops.object.light_add(type='POINT',location=(0,y+.22,.35));l=bpy.context.object;l.name='P06_2600K_MaintenanceLight';l.data.color=(1,.43,.15);l.data.energy=65;l.data.shadow_soft_size=.12;attach(l)
rows=list(csv.DictReader((P/'Docs/SanctuaryBlueprint56m20261107/data/module_placements.csv').open(encoding='utf-8-sig')))
funcs={'transit_chest':chest,'repair_workbench':bench,'inventory_stand':stand,'empty_cradle':cradle,'wall_lantern':lamp}
roots=[]
for row in rows:
 if row['module'] not in funcs:continue
 root=bpy.data.objects.new(row['id']+'_'+row['module'],None);s.collection.objects.link(root);roots.append(root);funcs[row['module']]();root.location=(float(row['x_m']),float(row['z_m']),float(row['y_m']));root.rotation_euler.z=-math.radians(float(row['yaw_deg']))
 root['front_facing_hall']=True
 if row['module']!='wall_lantern':
  for child in root.children:
   child.location.x*=-1;child.location.y*=-1;child.rotation_euler.z+=math.pi
s['design_version']='R3 evacuation props first craft pass - not accepted';s.cycles.samples=32
bpy.ops.wm.save_as_mainfile(filepath=str(P/'Blender/SanctuaryR3/Sanctuary_R3_Props.blend'))
cam=s.camera;cam.location=(-21,-24,3.1);cam.rotation_euler=(Vector((-25,-28.6,.8))-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.lens=42
s.render.filepath=str(P/'Docs/SanctuaryR3Build/Props_Detail.png');bpy.ops.render.render(write_still=True)
(P/'Docs/SanctuaryR3Build/Props.json').write_text(json.dumps({'stage':'first craft pass, not final acceptance','placed_prop_roots':len(roots),'created_objects':len(created),'pending':'clearance evaluated bounds, cloth fix if needed, tool refinement, pipes, shelves, weathering and all remaining final scope'},indent=2))
print('R3_PROPS_COMPLETE',len(roots),len(created))
