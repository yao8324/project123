import bpy, math, json
from pathlib import Path
from mathutils import Vector
P=Path(__file__).resolve().parents[2]
bpy.ops.wm.open_mainfile(filepath=str(P/'Blender/SanctuaryR3/Sanctuary_R3_Architecture.blend'))
s=bpy.context.scene
stone=bpy.data.materials['R3_Ivory_Marble_Foundation'];gold=bpy.data.materials['R3_Antique_Gold_Foundation']
def mesh(name,v,f,mat):
 m=bpy.data.meshes.new(name);m.from_pydata(v,[],f);m.update();o=bpy.data.objects.new(name,m);s.collection.objects.link(o);o.data.materials.append(mat);return o
def line(name,pts,r,mat):
 c=bpy.data.curves.new(name,'CURVE');c.dimensions='3D';c.bevel_depth=r;c.bevel_resolution=2;sp=c.splines.new('POLY');sp.points.add(len(pts)-1)
 for p,v in zip(sp.points,pts):p.co=(*v,1)
 o=bpy.data.objects.new(name,c);s.collection.objects.link(o);o.data.materials.append(mat);return o
def ring(name,x,y,z,r,thick,mat):return line(name,[(x+r*math.cos(i*math.tau/64),y+r*math.sin(i*math.tau/64),z) for i in range(65)],thick,mat)
def height(x):return 24+12*math.sqrt(max(0,4-(1+abs(x)/30)**2))/math.sqrt(3)
# Close both tympana with actual masonry rather than a dark open end.
for end in [-1,1]:
 v=[]
 for i in range(129):
  x=-30+60*i/128;v.extend([(x,(37.2 if end==1 else -36),24),(x,(37.2 if end==1 else -36),height(x)+.2)])
 o=mesh('E05_EndTympanum',v,[(2*i,2*i+1,2*i+3,2*i+2) for i in range(128)],stone);o.modifiers.new('Wall thickness','SOLIDIFY').thickness=1.2
# Ribbed acanthus leaves: tapered lobed surfaces with raised center veins and curled tips.
for o in list(s.objects):
 if 'CapitalEnvelope' in o.name:bpy.data.objects.remove(o,do_unlink=True)
for x in [-30,30]:
 for y in [-30,-18,-6,6,18,30]:
  for base,total in [(12.2,.9),(22.75,1.25)]:
   for h,r,t in [(0,1.04,.075),(.12,1.10,.05),(.92,1.25,.065),(1,1.28,.07)]:ring('E02_CapitalMoulding',x,y,base+h*total,r,t,gold)
   for row in range(2):
    for k in range(12):
     a=(k+row*.5)*math.tau/12;v=[];f=[];vein=[]
     for j in range(17):
      t=j/16;z=base+total*(row*.32+t*.62);rad=1.01+.23*t*t;w=.24*math.sin(math.pi*t)**.65*(.82+.18*math.cos(t*8*math.pi))
      for u in [-1,-.5,0,.5,1]:
       rr=rad+.075*(1-abs(u))*math.sin(math.pi*t);aa=a+w*u/rad;v.append((x+rr*math.cos(aa),y+rr*math.sin(aa),z))
      vein.append((x+(rad+.08)*math.cos(a),y+(rad+.08)*math.sin(a),z))
     for j in range(16):
      for q in range(4):n=j*5+q;f.append((n,n+1,n+6,n+5))
     o=mesh('E02_AcanthusLeaf',v,f,gold);o.modifiers.new('Cast gold thickness','SOLIDIFY').thickness=.025
     for p in o.data.polygons:p.use_smooth=True
     line('E02_LeafVein',vein,.012,gold)
  # Layered rings on each individual bundled shaft, plus stone base moulding.
  for k in range(8):
   a=k*math.pi/4;xx=x+.8*math.cos(a);yy=y+.8*math.sin(a)
   for h,r in [(1.18,.25),(1.30,.23),(1.43,.21),(12.10,.22),(22.65,.23)]:ring('E02_ShaftCollar',xx,yy,h,r,.035,gold if h>12 else stone)
# Gate frame interlaced gold tracery following the pointed arch plane.
def gh(x,w,r):return 17.2+r*math.sqrt(max(0,4-(1+abs(x)/w)**2))/math.sqrt(3)
for band in range(4):
 w=8.65+band*.5;r=8.025+band*.85;dep=35.12+band*.22
 for flip in [-1,1]:
  pts=[]
  for i in range(257):
   t=i/256;x=-w+2*w*t;pts.append((x,dep-.24,gh(x,w,r)+flip*.15*math.sin(t*math.pi*24)))
  line('E06_InterlaceGold',pts,.032,gold)
# Door border straps and regularly spaced forged rivets.
for sign in [-1,1]:
 for xx in [.23,7.75]:
  line('E06_VerticalGoldStrap',[(sign*xx,35.70,.3),(sign*xx,35.70,17)],.045,gold)
 for zz in [1,3,5,7,9,11,13,15,16.7]:
  for xx in [.24,7.73]:
   bpy.ops.mesh.primitive_uv_sphere_add(segments=12,ring_count=8,radius=.085,location=(sign*xx,35.62,zz));o=bpy.context.object;o.name='E06_ForgedRivet';o.scale=(1,.5,1);o.data.materials.append(gold)
# Marble material relief is visible at grazing angles; use world-coordinate mapped texture.
for mat,stem,rough,strength in [(stone,'Marble',.38,.12),(gold,'Brass',.29,.055),(bpy.data.materials['R3_Bluegrey_Marble_Foundation'],'Slate',.30,.18)]:
 ns=mat.node_tree.nodes;ls=mat.node_tree.links;bs=next(n for n in ns if n.type=='BSDF_PRINCIPLED');bs.inputs['Roughness'].default_value=rough
 al=next(n for n in ns if n.type=='TEX_IMAGE');bump=ns.new('ShaderNodeBump');bump.inputs['Strength'].default_value=strength;bump.inputs['Distance'].default_value=.035;ls.new(al.outputs['Color'],bump.inputs['Height']);ls.new(bump.outputs['Normal'],bs.inputs['Normal'])
 if stem=='Slate':
  mix=ns.new('ShaderNodeMixRGB');mix.blend_type='MULTIPLY';mix.inputs[0].default_value=1;mix.inputs[2].default_value=(.24,.29,.36,1);ls.new(al.outputs['Color'],mix.inputs[1]);ls.new(mix.outputs[0],bs.inputs['Base Color'])
# Lower the excessive fill from the early blockout render.
for o in s.objects:
 if o.type=='LIGHT':o.data.energy*=.48
s.view_settings.exposure=-.35;s.cycles.samples=32
s['design_version']='R3 ornament pass 1 - not accepted'
bpy.ops.wm.save_as_mainfile(filepath=str(P/'Blender/SanctuaryR3/Sanctuary_R3_Ornament.blend'))
s.render.filepath=str(P/'Docs/SanctuaryR3Build/Ornament_Preview.png');bpy.ops.render.render(write_still=True)
# A dedicated close-up makes actual leaf geometry reviewable.
cam=s.camera;cam.location=(25,-26,23.5);cam.rotation_euler=(Vector((30,-18,23.35))-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.lens=62
s.render.filepath=str(P/'Docs/SanctuaryR3Build/Capital_Detail.png');bpy.ops.render.render(write_still=True)
report={'stage':'ornament pass 1, NOT final acceptance','objects':len(s.objects),'capital_leaf_meshes':sum(o.name.startswith('E02_AcanthusLeaf') for o in s.objects),'remaining':'true groin vault/lunettes, window tracery, floor masonry and flush inlays, props, character and actions, export and Unity acceptance'}
(P/'Docs/SanctuaryR3Build/Ornament.json').write_text(json.dumps(report,indent=2));print('R3_ORNAMENT_COMPLETE',report)
