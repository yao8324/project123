import bpy,math,json
from pathlib import Path
from mathutils import Vector
P=Path(__file__).resolve().parents[2]
bpy.ops.wm.open_mainfile(filepath=str(P/'Blender/SanctuaryR3/Sanctuary_R3_Floor.blend'))
s=bpy.context.scene;stone=bpy.data.materials['R3_Ivory_Marble_Foundation'];gold=bpy.data.materials['R3_Antique_Gold_Foundation']
def mat(name,color,metal,rough):
 m=bpy.data.materials.new(name);m.diffuse_color=(*color,1);m.use_nodes=True;b=next(n for n in m.node_tree.nodes if n.type=='BSDF_PRINCIPLED');b.inputs['Base Color'].default_value=(*color,1);b.inputs['Metallic'].default_value=metal;b.inputs['Roughness'].default_value=rough;return m
lead=mat('R3_Window_WeatheredLead',(.095,.105,.12),.75,.48)
glass=mat('R3_Window_ColdGlass',(.23,.32,.40),.12,.24);bs=next(n for n in glass.node_tree.nodes if n.type=='BSDF_PRINCIPLED');bs.inputs['Emission Color'].default_value=(.31,.44,.59,1);bs.inputs['Emission Strength'].default_value=.35
# Side-facing geometry uses u along hall length and v as elevation.
def line(name,paths,r,material):
 c=bpy.data.curves.new(name,'CURVE');c.dimensions='3D';c.bevel_depth=r;c.bevel_resolution=2
 for path in paths:
  sp=c.splines.new('POLY');sp.points.add(len(path)-1)
  for p,v in zip(sp.points,path):p.co=(*v,1)
 o=bpy.data.objects.new(name,c);s.collection.objects.link(o);o.data.materials.append(material);return o
def mesh(name,v,f,material):
 m=bpy.data.meshes.new(name);m.from_pydata(v,[],f);m.update();o=bpy.data.objects.new(name,m);s.collection.objects.link(o);o.data.materials.append(material);return o
def prof(t):return math.sqrt(max(0,4-(1+abs(t))**2))/math.sqrt(3)
def window(side,center,width,bottom,spring,top,label):
 half=width/2
 def pos(u,v,depth=31.82):return(side*depth,center+u,v)
 def crown(u):return spring+(top-spring)*prof(u/half)
 border=[(-half,bottom),(-half,spring)]+[(-half+width*i/64,crown(-half+width*i/64)) for i in range(65)]+[(half,bottom),(-half,bottom)]
 ob=mesh(label+'_Glass',[pos(u,v,31.96) for u,v in border[:-1]],[tuple(range(len(border)-1))],glass)
 line(label+'_StoneSurround',[[pos(u,v,31.80) for u,v in border]],.085,stone)
 line(label+'_GoldReveal',[[pos(u*.985,bottom+.025+(v-bottom)*.993,31.74) for u,v in border]],.018,gold)
 # Diamond glazing: clipped to actual pointed silhouette, 0.42m pitch; all leadwork in one curve object.
 paths=[]
 for slope in [-1,1]:
  for n in range(-40,70):
   intercept=n*.42;pts=[]
   for k in range(161):
    u=-half+width*k/160;v=slope*u+intercept
    if bottom+.04<=v<=crown(u)-.06:pts.append(pos(u,v,31.89))
    elif len(pts)>1:paths.append(pts);pts=[]
   if len(pts)>1:paths.append(pts)
 line(label+'_DiamondLead',paths,.010,lead)
 return pos
for o in list(s.objects):
 if o.name.startswith(('E03_LancetGlass','E03_WindowMullion','E04_HighWindow')):bpy.data.objects.remove(o,do_unlink=True)
for side in [-1,1]:
 for idx,center in enumerate([-24,-12,0,12,24]):
  if idx%2==0:
   pos=window(side,center,6.4,1,7.7,11.6,'E03_GothicWindow')
   # Three stone lancets inside the larger window, plus a rose with six lobes.
   for u0 in [-2.1,0,2.1]:
    w=.94;pts=[pos(u0-w,1),pos(u0-w,7.0)]+[pos(u0-w+2*w*k/48,7+1.7*prof(-1+2*k/48)) for k in range(49)]+[pos(u0+w,1)]
    line('E03_TripleLancet',[pts],.040,stone)
   rose=[]
   for k in range(129):
    a=k*math.tau/128;r=.67+.12*math.cos(6*a);rose.append(pos(r*math.cos(a),9.75+r*math.sin(a),31.76))
   line('E03_SixfoilRose',[rose],.045,stone)
   line('E03_RoseGold',[[pos(.33*math.cos(k*math.tau/64),9.75+.33*math.sin(k*math.tau/64),31.70) for k in range(65)]],.018,gold)
  for dz in [-2,0,2]:
   pos=window(side,center+dz,1.65,18,21.3,23.2,'E04_ClerestoryLancet')
   line('E04_ClerestoryCentralMullion',[[pos(0,18),pos(0,22.3)]],.034,stone)
  # Broken-ring wall insignia between gallery and upper windows.
  for dz in [-4.6,4.6]:
   paths=[]
   for start,end in [(20,160),(200,340)]:
    paths.append([(side*31.83,center+dz+.36*math.cos(math.radians(start+(end-start)*k/32)),20.5+.5*math.sin(math.radians(start+(end-start)*k/32))) for k in range(33)])
   line('E04_GoldWallInsignia',paths,.022,gold)
# Gate rivets: 60mm heads, spaced ~350mm, instanced mesh preserves editable individual fittings.
for o in list(s.objects):
 if o.name.startswith('E06_ForgedRivet'):bpy.data.objects.remove(o,do_unlink=True)
bpy.ops.mesh.primitive_uv_sphere_add(segments=12,ring_count=8,radius=.03);template=bpy.context.object;data=template.data;data.materials.append(gold);bpy.data.objects.remove(template,do_unlink=True)
positions=[]
for sign in [-1,1]:
 for xx in [.24,7.73]:
  for i in range(48):positions.append((sign*xx,35.62,.3+i*.35))
 for i in range(22):positions.append((sign*(.3+i*.35),35.31,9.8))
for p in positions:
 o=bpy.data.objects.new('E06_60mmRivet',data);s.collection.objects.link(o);o.location=p
# Five complete gate-jamb mouldings give the upper archivolts visible support down to floor level.
for side in [-1,1]:
 for layer in range(5):
  x=side*(8.4+layer*.5);y=35.1+layer*.22
  line('E06_GateJambMoulding',[[(x,y,.2),(x,y,17.2)]],.18,stone)
  line('E06_GateJambGold',[[(x,y-.19,.4),(x,y-.19,17.2)]],.026,gold)
s['design_version']='R3 window and gate detail pass, not accepted';s.cycles.samples=32
bpy.ops.wm.save_as_mainfile(filepath=str(P/'Blender/SanctuaryR3/Sanctuary_R3_Windows.blend'))
s.render.filepath=str(P/'Docs/SanctuaryR3Build/Windows_Wide.png');bpy.ops.render.render(write_still=True)
cam=s.camera;cam.location=(19,-10,9);cam.rotation_euler=(Vector((31.8,0,11))-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.lens=26
s.render.filepath=str(P/'Docs/SanctuaryR3Build/Windows_Detail.png');bpy.ops.render.render(write_still=True)
report={'stage':'windows and gate detail, NOT final acceptance','lower_pointed_windows':6,'upper_lancets':30,'rivet_instances':len(positions),'rivet_diameter_m':.06,'remaining':'recessed masonry, bookshelf niches, weathering, south entry, props, character/animations, Unity and full acceptance'}
(P/'Docs/SanctuaryR3Build/Windows.json').write_text(json.dumps(report,indent=2));print('R3_WINDOWS_COMPLETE',report)
