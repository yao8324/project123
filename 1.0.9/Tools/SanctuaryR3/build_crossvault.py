import bpy, math, json
from pathlib import Path
from mathutils import Vector
P=Path(__file__).resolve().parents[2]
bpy.ops.wm.open_mainfile(filepath=str(P/'Blender/SanctuaryR3/Sanctuary_R3_Ornament.blend'))
s=bpy.context.scene;stone=bpy.data.materials['R3_Ivory_Marble_Foundation'];gold=bpy.data.materials['R3_Antique_Gold_Foundation']
def mesh(name,v,f,mat):
 m=bpy.data.meshes.new(name);m.from_pydata(v,[],f);m.update();o=bpy.data.objects.new(name,m);s.collection.objects.link(o);o.data.materials.append(mat);return o
def curve(name,pts,r,mat):
 c=bpy.data.curves.new(name,'CURVE');c.dimensions='3D';c.bevel_depth=r;c.bevel_resolution=2;sp=c.splines.new('POLY');sp.points.add(len(pts)-1)
 for p,v in zip(sp.points,pts):p.co=(*v,1)
 o=bpy.data.objects.new(name,c);s.collection.objects.link(o);o.data.materials.append(mat);return o
def profile(t):return math.sqrt(max(0,4-(1+abs(t))**2))/math.sqrt(3)
def roof(x,y):return 24+12*max(profile(x/30),profile(y/6))
# Replace the preliminary continuous barrel with the upper envelope of two intersecting pointed vaults.
for o in list(s.objects):
 if o.name.startswith(('E05_VaultWeb','E05_Diagonal','E05_Tierceron','E05_KeystoneEnvelope')):bpy.data.objects.remove(o,do_unlink=True)
for bay in range(5):
 cy=-24+12*bay;v=[];f=[];n=80
 for j in range(n+1):
  y=-6+12*j/n
  for i in range(n+1):
   x=-30+60*i/n;v.append((x,cy+y,roof(x,y)+.08))
 for j in range(n):
  for i in range(n):
   a=j*(n+1)+i;f.append((a,a+1,a+n+2,a+n+1))
 o=mesh('E05_TrueCrossVault',v,f,stone);o.modifiers.new('Stone web 350mm','SOLIDIFY').thickness=.35;o.modifiers[-1].offset=1
 # Four groins meet exactly at the crown; gold follows their lower face.
 for sx in [-1,1]:
  for sy in [-1,1]:
   pts=[(sx*30*(1-t/96),cy+sy*6*(1-t/96),24+12*profile(1-t/96)-.23) for t in range(97)]
   curve('E05_CrossGroinStone',pts,.225,stone);curve('E05_CrossGroinGold',[(x,y,z-.23) for x,y,z in pts],.026,gold)
   # Tierceron ribs join each pier to off-axis points on the central ridge.
   pts=[]
   for k in range(97):
    t=k/96;x=sx*30*(1-t);y=sy*(6*(1-t)+2*t);pts.append((x,cy+y,roof(x,y)-.16))
   curve('E05_TierceronStone',pts,.11,stone)
 # Longitudinal crown rib and decorated meeting boss.
 curve('E05_LongitudinalRidge',[(0,cy+y,35.8) for y in [-6,6]],.12,stone)
 for petal in range(12):
  a=petal*math.tau/12;pts=[]
  for k in range(25):
   t=k/24;r=.12+.55*math.sin(math.pi*t);ang=a+.19*math.cos(math.pi*t);pts.append((r*math.cos(ang),cy+r*math.sin(ang),35.55-.12*math.sin(math.pi*t)))
  curve('E05_RosetteBossPetal',pts,.075,gold)
 # Side lunettes continue over the narrow aisle to the enclosing wall.
 for side in [-1,1]:
  vv=[]
  for x in [side*30,side*32.05]:
   for j in range(81):y=-6+12*j/80;vv.append((x,cy+y,24+12*profile(y/6)+.08))
  oo=mesh('E05_SideLunetteRoof',vv,[(j,j+1,j+82,j+81) for j in range(80)],stone);oo.modifiers.new('Stone thickness','SOLIDIFY').thickness=.35;oo.modifiers[-1].offset=1
  # Masonry spandrel behind the arch, complete with layered blind tracery.
  vv=[]
  for j in range(81):
   y=-6+12*j/80;vv.extend([(side*32.05,cy+y,24),(side*32.05,cy+y,24+12*profile(y/6)+.08)])
  oo=mesh('E05_LunetteBacking',vv,[(2*j,2*j+1,2*j+3,2*j+2) for j in range(80)],stone);oo.modifiers.new('Wall thickness','SOLIDIFY').thickness=1.2;oo.modifiers[-1].offset=0
  for inset in [0,.22,.48]:
   w=6-inset;pts=[(side*31.35,cy-w+2*w*k/96,24+(12-inset)*profile(-1+2*k/96)-.18) for k in range(97)]
   curve('E05_LunetteArchMoulding',pts,.13 if inset==0 else .065,stone if inset!=.22 else gold)
  for dz in [-3,-1,1,3]:
   top=24+12*profile(dz/6)-.8
   curve('E05_BlindTraceryVertical',[(side*31.30,cy+dz,24.3),(side*31.30,cy+dz,top)],.055,stone)
# Main transverse ribs: layered underside mouldings establish visible depth.
for station in [-30,-18,-6,6,18,30]:
 for offset,dep,rad,mat in [(-.24,.35,.12,stone),(.24,.35,.12,stone),(0,.65,.055,gold)]:
  curve('E05_MainRibMoulding',[(-30+60*k/128,station+offset,24+12*profile(-1+2*k/128)-dep) for k in range(129)],rad,mat)
# Close narrow strips over end aisles, where the last half bay has no lunette.
for side in [-1,1]:
 for a,b in [(-36,-30),(30,36)]:
  o=mesh('E05_EndAisleRoof',[(side*30,a,24),(side*32.05,a,24),(side*32.05,b,24),(side*30,b,24)],[(0,1,2,3)],stone);o.modifiers.new('Thickness','SOLIDIFY').thickness=.35
s['design_version']='R3 intersecting vault construction - not accepted';s.cycles.samples=40
bpy.ops.wm.save_as_mainfile(filepath=str(P/'Blender/SanctuaryR3/Sanctuary_R3_CrossVault.blend'))
s.render.filepath=str(P/'Docs/SanctuaryR3Build/CrossVault_Preview.png');bpy.ops.render.render(write_still=True)
# Additional upward camera shows geometry unavailable in the front elevation.
cam=s.camera;cam.location=(17,-24,17);cam.rotation_euler=(Vector((-5,1,32))-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.lens=20
s.render.filepath=str(P/'Docs/SanctuaryR3Build/CrossVault_Upward.png');bpy.ops.render.render(write_still=True)
(P/'Docs/SanctuaryR3Build/CrossVault.json').write_text(json.dumps({'stage':'cross-vault geometry, not final acceptance','cross_vault_webs':5,'side_lunettes':10,'objects':len(s.objects),'crown_m':36,'spring_m':24,'pending':'visual review and full remaining scope'},indent=2))
print('R3_CROSSVAULT_COMPLETE')
