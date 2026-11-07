import bpy,bmesh,math,json
from pathlib import Path
from mathutils import Vector
P=Path(__file__).resolve().parents[2]
bpy.ops.wm.open_mainfile(filepath=str(P/'Blender/SanctuaryR3/Sanctuary_R3_Surfaces.blend'))
s=bpy.context.scene;stone=bpy.data.materials['R3_Ivory_Marble_Foundation'];limestone=bpy.data.materials['R3_Warm_Limestone_Masonry'];gold=bpy.data.materials['R3_Antique_Gold_Foundation'];created=[]
def mesh(n,v,f,m,bevel=.003):
 me=bpy.data.meshes.new(n);me.from_pydata(v,[],f);me.update();bm=bmesh.new();bm.from_mesh(me);bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(me);bm.free();me.update();o=bpy.data.objects.new(n,me);s.collection.objects.link(o);o.data.materials.append(m)
 if bevel:b=o.modifiers.new('Dressed stone edge','BEVEL');b.width=bevel;b.segments=2
 created.append(o);return o
def cube(v,f,p,d):
 idx=len(v);v.extend([(p[0]+x*d[0]/2,p[1]+y*d[1]/2,p[2]+z*d[2]/2) for x,y,z in [(-1,-1,-1),(-1,-1,1),(-1,1,-1),(-1,1,1),(1,-1,-1),(1,-1,1),(1,1,-1),(1,1,1)]]);f.extend([tuple(idx+i for i in face) for face in [(0,4,6,2),(1,3,7,5),(0,1,5,4),(2,6,7,3),(0,2,3,1),(4,5,7,6)]])
def curve(u,w,r):return 7.6+r*math.sqrt(max(0,4-(1+abs(u)/w)**2))/math.sqrt(3)
def arch(side,c,w,r,x,width,depth,n,mat,blocks=40):
 v=[];f=[]
 for block in range(blocks):
  start=len(v);gap=.00022 if blocks>1 else 0
  for k in range(5):
   t=block/blocks+gap+(1/blocks-2*gap)*k/4;u=-w+2*w*t;z=curve(u,w,r);eps=.0001;deriv=(curve(min(w,u+eps),w,r)-curve(max(-w,u-eps),w,r))/(min(w,u+eps)-max(-w,u-eps));normal=Vector((-deriv,1)).normalized()
   for offset,dep in [(0,0),(width,0),(width,depth),(0,depth)]:v.append((side*(x+dep),c+u+normal.x*offset,z+normal.y*offset))
  for k in range(4):
   for q in range(4):a=start+k*4+q;b=start+k*4+(q+1)%4;f.append((a,b,b+4,a+4))
  f.append(tuple(start+i for i in reversed(range(4))));f.append(tuple(start+16+i for i in range(4)))
 return mesh(n,v,f,mat,.003 if mat==stone else .0008)
for o in list(s.objects):
 if o.name.startswith(('E03_Archivolt','E03_GoldInset','E03_ArcadeJamb')):bpy.data.objects.remove(o,do_unlink=True)
for side in [-1,1]:
 for c in [-24,-12,0,12,24]:
  for layer in range(4):
   x=31.22+layer*.18;w=4.9+layer*.16;r=4.8+layer*.10
   arch(side,c,w,r,x,.145,.18,'E03_R4_VoussoirBand',stone)
   arch(side,c,w+.04,r+.04,x-.006,.018,.006,'E03_R4_FlatGoldInlay',gold,80)
   v=[];f=[];gv=[];gf=[]
   for sign in [-1,1]:
    for k in range(19):cube(v,f,(side*(x+.09),c+sign*(w+.0725),.2+k*.4),(.18,.145,.395))
    cube(gv,gf,(side*(x-.003),c+sign*(w+.055),3.8),(.006,.018,7.56))
    # Stacked impost ledges join shaft courses to the arch spring.
    for z,width in [(7.48,.23),(7.55,.26),(7.62,.20)]:cube(v,f,(side*(x+.07),c+sign*(w+.0725),z),(.26,width,.065))
   mesh('E03_R4_JambStoneCourses',v,f,stone);mesh('E03_R4_JambGoldInlay',gv,gf,gold,.0008)
# Thin dressed masonry facing over original structural walls, actual5mm joints.
# Skip entire bounding regions of windows, rather than crossing stained glass with joints.
counts={}
for side in [-1,1]:
 v=[];f=[];count=0
 for row in range(30):
  z=.4+row*.8
  for col in range(37):
   a=-36+col*2-(row%2);b=min(36,a+2);a=max(-36,a)
   if b-a<.05:continue
   lowwin=any(a<c+3.25 and b>c-3.25 and z+.4>1 and z-.4<11.6 for c in [-24,0,24])
   highwin=any(a<c+5 and b>c-5 and z+.4>18 and z-.4<23.2 for c in [-24,-12,0,12,24])
   if lowwin or highwin:continue
   cube(v,f,(side*31.985,(a+b)/2,z),(.03,b-a-.005,.795));count+=1
 mesh('E03_R4_LimestoneSideFacing',v,f,limestone,.002);counts[str(side)]=count
for end,clear in [(1,11.5),(-1,7.5)]:
 v=[];f=[];count=0
 for row in range(30):
  for col in range(33):
   a=max(-32,-32+col*2-(row%2));b=min(32,-30+col*2-(row%2))
   if b-a<.05 or (a<clear and b>-clear):continue
   cube(v,f,((a+b)/2,end*35.985,.4+row*.8),(b-a-.005,.03,.795));count+=1
 mesh('E06_R4_LimestoneEndFacing' if end==1 else 'E07_R4_LimestoneEndFacing',v,f,limestone,.002);counts['end'+str(end)]=count
s['architecture_round']='R4 layered voussoirs, flat gold, actual masonry facing joints; not accepted'
bpy.ops.wm.save_as_mainfile(filepath=str(P/'Blender/SanctuaryR3/Sanctuary_R3_Arcades.blend'))
(P/'Docs/SanctuaryR3Build/ArcadeRound.json').write_text(json.dumps({'new_objects':len(created),'arcade_bays':10,'arch_layers_per_bay':4,'voussoirs_per_layer':40,'masonry_blocks':counts,'joint_width_m':.005,'facing_thickness_m':.03,'pending':'Precise window recess masonry, mortar/aging variation, close gold/stone fit, sculpted capitals, gate details, collision and performance'},indent=2))
s.cycles.samples=24;s.render.resolution_x=1400;s.render.resolution_y=1000;s.render.resolution_percentage=100
s.camera.location=(-23,-20,7);s.camera.rotation_euler=(Vector((-31.3,-12,8.8))-s.camera.location).to_track_quat('-Z','Y').to_euler();s.camera.data.type='PERSP';s.camera.data.lens=29;s.render.filepath=str(P/'Docs/SanctuaryR3Build/Arcade_Round4.png');bpy.ops.render.render(write_still=True)
print('ARCADE_ROUND_COMPLETE',len(created),counts)
