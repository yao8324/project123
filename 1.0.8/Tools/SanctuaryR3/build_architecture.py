import runpy,bpy,math,json
from pathlib import Path
from mathutils import Vector
P=Path(__file__).resolve().parents[2]
g=runpy.run_path(str(P/'Tools/SanctuaryR3/build_foundation.py'))
box,tube,arch,shaft,stone,gold,iron,floor=[g[k] for k in ['box','tube','arch','shaft','stone','gold','iron','floor']]
scene=bpy.context.scene
# All geometry remains at real meter scale, Blender Z is height.
def mesh(name,verts,faces,material):
 m=bpy.data.meshes.new(name);m.from_pydata(verts,[],faces);m.update();o=bpy.data.objects.new(name,m);scene.collection.objects.link(o);o.data.materials.append(material);return o
def rise(x):return 24+12*math.sqrt(max(0,4-(1+abs(x)/30)**2))/math.sqrt(3)
# Five vault webs with transverse joints and thick, intersecting diagonal ribs.
for bay in range(5):
 z0=-30+bay*12;verts=[];faces=[];nx=64;nz=16
 for j in range(nz+1):
  z=z0+12*j/nz
  for i in range(nx+1):
   x=-30+60*i/nx;verts.append((x,z,rise(x)+.12))
 for j in range(nz):
  for i in range(nx):
   a=j*(nx+1)+i;faces.append((a,a+1,a+nx+2,a+nx+1))
 o=mesh(f'E05_VaultWeb_{bay}',verts,faces,stone);sol=o.modifiers.new('Masonry thickness','SOLIDIFY');sol.thickness=.35
 for side in [-1,1]:
  pts=[]
  for k in range(129):
   t=k/128;x=side*(-30+60*t);z=z0+12*t;pts.append((x,z,rise(x)-.2))
  tube('E05_DiagonalGroin',pts,.225,stone)
  tube('E05_DiagonalGold',[(x,y,z-.24) for x,y,z in pts],.025,gold)
  for end in [.33,.67]:
   pts=[]
   for k in range(65):
    t=k/64;x=side*30*(1-t);z=z0+12*end*t;pts.append((x,z,rise(x)-.17))
   tube('E05_Tierceron',pts,.11,stone)
 # Floral bosses will replace these correctly located envelopes in next detail pass.
 bpy.ops.mesh.primitive_uv_sphere_add(segments=24,ring_count=12,radius=.65,location=(0,z0+6,35.65));bpy.context.object.name='E05_KeystoneEnvelope';bpy.context.object.data.materials.append(gold)
# End transitions use same vault section, avoiding open sky at the entry and gate.
for za,zb in [(-36,-30),(30,36)]:
 verts=[(x,z,rise(x)+.12) for z in [za,zb] for x in [-30+60*i/64 for i in range(65)]]
 o=mesh('E05_EndVault',verts,[(i,i+1,i+66,i+65) for i in range(64)],stone);o.modifiers.new('Thickness','SOLIDIFY').thickness=.35
# Build side arches in a local plane; map local horizontal to hall longitudinal axis.
def sidearch(name,side,center,half,spring,delta,depth=0,radius=.12,material=None):
 pts=[]
 for i in range(97):
  a=-half+2*half*i/96;h=spring+delta*math.sqrt(max(0,4-(1+abs(a)/half)**2))/math.sqrt(3)
  pts.append((side*(32-depth),center+a,h))
 return tube(name,pts,radius,material or stone)
for side in [-1,1]:
 for idx,center in enumerate([-24,-12,0,12,24]):
  for layer in range(4):sidearch('E03_Archivolt',side,center,4.9+layer*.16,7.6,4.8+layer*.10,.7-layer*.18,.13,stone)
  sidearch('E03_GoldInset',side,center,4.75,7.6,4.65,.85,.035,gold)
  for dz in [-4.9,4.9]:shaft('E03_ArcadeJamb',side*31.3,center+dz,.20,7.6)
  box('E04_GalleryFloor',(side*30.8,center,13.65),(2.4,12,.3),stone)
  box('E04_GalleryParapet',(side*29.55,center,14.4),(.18,12,1.2),stone)
  for dz in [-4,0,4]:
   sidearch('E04_Triforium',side,center+dz,1.4,15.4,2.0,2.45,.09,stone)
   for dx in [-1.4,1.4]:shaft('E04_Colonette',side*29.55,center+dz+dx,.10,1.6,13.8)
  # Actual window recess surface and leadwork; outer wall remains architectural backing.
  if idx%2==0:
   glass=g['mat']('E03_WindowGlass',(.29,.40,.45),.1,.3)
   box('E03_LancetGlass',(side*31.93,center,5.5),(.02,6.4,9),glass)
   for n in range(-3,4):box('E03_WindowMullion',(side*31.88,center+n*.9,5.5),(.08,.06,9),gold if n%2 else stone)
  for dz in [-2,0,2]:box('E04_HighWindow',(side*31.92,center+dz,20.6),(.02,1.65,5.2),g['mat']('HighWindow',(.27,.37,.42),0,.35))
# Pointed upper leaves close the previously rectangular door foundation.
for sign in [-1,1]:
 xs=[.08+(8-.08)*i/64 for i in range(65)];verts=[]
 for x in xs:
  y=17+7*math.sqrt(max(0,4-(1+x/8)**2))/math.sqrt(3);verts.extend([(sign*x,35.74,17),(sign*x,35.74,y)])
 o=mesh('E06_PointUpperLeaf',verts,[(2*i,2*i+1,2*i+3,2*i+2) for i in range(64)],iron);o.modifiers.new('Door thickness','SOLIDIFY').thickness=.5
for radius,width in [(5.4,.18),(4.8,.07)]:
 for start,end in [(-70,70),(110,250)]:
  verts=[];faces=[]
  for k in range(97):
   a=math.radians(start+(end-start)*k/96)
   for r in [radius-width/2,radius+width/2]:verts.append((r*math.cos(a),35.68,10.8+r*math.sin(a)))
  for k in range(96):faces.append((2*k,2*k+1,2*k+3,2*k+2))
  o=mesh('E06_FlatGoldSeal',verts,faces,gold);o.modifiers.new('Solid gold inlay','SOLIDIFY').thickness=.025
box('E06_Crossbar',(0,35.5,9.8),(15.4,.3,.5),gold)
# Surface materials use the authored marble maps as a first material pass, physical object coordinates.
tex=P/'Assets/Art/Bosses/Knight/Sanctuary20261107/Textures'
for material,stem in [(stone,'Marble'),(floor,'Slate'),(gold,'Brass'),(iron,'Steel')]:
 nodes=material.node_tree.nodes;links=material.node_tree.links;bsdf=next(n for n in nodes if n.type=='BSDF_PRINCIPLED');coord=nodes.new('ShaderNodeTexCoord');mapping=nodes.new('ShaderNodeVectorMath');mapping.operation='SCALE';mapping.inputs[3].default_value=.34;links.new(coord.outputs['Object'],mapping.inputs[0]);im=nodes.new('ShaderNodeTexImage');im.image=bpy.data.images.load(str(tex/(stem+'_Albedo.png')),check_existing=True);im.projection='BOX';im.projection_blend=.2;links.new(mapping.outputs[0],im.inputs['Vector']);links.new(im.outputs['Color'],bsdf.inputs['Base Color'])
# Broad daylight sources, both sides, aimed into the room.
for side in [-1,1]:
 for z in [-20,0,20]:
  bpy.ops.object.light_add(type='AREA',location=(side*29,z,22));l=bpy.context.object;l.name='R3_Clerestory_Daylight';l.data.energy=18000;l.data.shape='DISK';l.data.size=12;l.rotation_euler=(Vector((0,z,3))-l.location).to_track_quat('-Z','Y').to_euler()
for o in scene.objects:
 if o.name.startswith('GUIDE_'):o.hide_render=True
scene.render.engine='CYCLES';scene.cycles.samples=24;scene.cycles.use_denoising=True;scene.render.resolution_x=1400;scene.render.resolution_y=900;scene.render.resolution_percentage=100
out=P/'Blender/SanctuaryR3';bpy.ops.wm.save_as_mainfile(filepath=str(out/'Sanctuary_R3_Architecture.blend'))
scene.render.filepath=str(P/'Docs/SanctuaryR3Build/Architecture_Preview.png');bpy.ops.render.render(write_still=True)
print('R3_ARCHITECTURE_RENDER_COMPLETE',len(scene.objects))
