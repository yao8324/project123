"""R6 gate construction layers and gallery blind tracery at fixed R3 dimensions."""
import bpy,bmesh,math,json
from pathlib import Path
from mathutils import Vector
P=Path(__file__).resolve().parents[2];bpy.ops.wm.open_mainfile(filepath=str(P/'Blender/SanctuaryR3/Sanctuary_R3_Materials.blend'))
s=bpy.context.scene;gold=bpy.data.materials['R3_Antique_Gold_Foundation'];stone=bpy.data.materials['R3_Ivory_Marble_Foundation'];iron=bpy.data.materials['R3_Gunmetal_Foundation'];groups={}
def poly(name,mat,verts,faces):
 if name.startswith("E04_"):name+="_"+str(side)
 v,f,m=groups.setdefault(name,([],[],mat));off=len(v);v.extend(verts);f.extend([tuple(off+i for i in face) for face in faces])
def slab(name,mat,polygon,frame,depth):
 n=len(polygon);vv=[frame(u,v,d) for d in [0,depth] for u,v in polygon];ff=[tuple(reversed(range(n))),tuple(n+i for i in range(n))]+[(i,(i+1)%n,(i+1)%n+n,i+n) for i in range(n)];poly(name,mat,vv,ff)
def ribbon(name,mat,pts,width,frame,depth=.012,closed=False):
 if closed:pts=pts+[pts[0]]
 vv=[]
 for i,(u,v) in enumerate(pts):
  prev=pts[max(0,i-1)];nxt=pts[min(len(pts)-1,i+1)];dx=nxt[0]-prev[0];dy=nxt[1]-prev[1];L=max(1e-9,math.hypot(dx,dy));nx=-dy/L*width/2;ny=dx/L*width/2
  vv.extend([frame(u+nx,v+ny,0),frame(u-nx,v-ny,0),frame(u-nx,v-ny,depth),frame(u+nx,v+ny,depth)])
 ff=[]
 for i in range(len(pts)-1):
  for j in range(4):a=i*4+j;b=i*4+(j+1)%4;ff.append((a,b,b+4,a+4))
 ff.extend([(3,2,1,0),tuple((len(pts)-1)*4+j for j in range(4))]);poly(name,mat,vv,ff)
def gate(u,v,d):return (u,35.735-d,v)
def top(x):return 17+7*math.sqrt(max(0,4-(1+abs(x)/8)**2))/math.sqrt(3)
# Separate forged plate faces;12mm joints retain the original overall leaf and center seam.
plate_count=0
for side in [-1,1]:
 for col in range(4):
  xa=.10+col*1.96;xb=xa+1.948
  for row in range(12):
   lo=.04+row*2;hi=lo+1.988
   if lo>=min(top(xa),top(xb))-.03:continue
   samples=[xa+(xb-xa)*i/8 for i in range(9)];polygon=[(side*xa,lo),(side*xb,lo)]+[(side*x,min(hi,top(x)-.03)) for x in reversed(samples)]
   slab('E06_R6_ForgedPlateFaces',iron,polygon,gate,.008);plate_count+=1
 # Four visible straps and pintle housings per leaf, all below the spring.
 for z in [2.2,6.2,13.8,16]:
  shape=[(side*7.90,z-.21),(side*6.1,z-.17),(side*5.68,z),(side*6.1,z+.17),(side*7.90,z+.21)]
  slab('E06_R6_HingeStraps',gold,shape,lambda u,v,d:(u,35.66-d,v),.035)
  slab('E06_R6_HingeHousings',iron,[(side*7.74,z-.42),(side*7.98,z-.42),(side*7.98,z+.42),(side*7.74,z+.42)],lambda u,v,d:(u,35.61-d,v),.14)
  for xx in [6.2,6.85,7.5]:
   pts=[(side*xx+.055*math.cos(a*math.tau/12),z+.055*math.sin(a*math.tau/12)) for a in range(12)]
   slab('E06_R6_HingeRivets',iron,pts,lambda u,v,d:(u,35.615-d,v),.022)
 # Lock plates frame the sealed seam, keeping both leaves separate.
 slab('E06_R6_CentralLockPlates',gold,[(side*.10,9.25),(side*.62,9.25),(side*.82,9.8),(side*.62,10.35),(side*.10,10.35)],lambda u,v,d:(u,35.32-d,v),.07)
 for z in [9.46,10.14]:
  slab('E06_R6_LockBolts',iron,[(side*.43+.055*math.cos(a*math.tau/12),z+.055*math.sin(a*math.tau/12)) for a in range(12)],lambda u,v,d:(u,35.24-d,v),.025)
# Gallery blind tracery: stepped coping, frames and quatrefoils above the existing solid parapet.
for side in [-1,1]:
 def gallery(u,v,d):return (side*(29.45-d),u,v)
 for center in [-24,-12,0,12,24]:
  for h,w in [(13.94,.10),(14.94,.12)]:ribbon('E04_R6_ParapetMoulding',stone,[(center-5.9,h),(center+5.9,h)],w,gallery,.10)
  ribbon('E04_R6_CopingGold',gold,[(center-5.9,15.015),(center+5.9,15.015)],.025,gallery,.012)
  for k in range(6):
   u=center-5+k*2
   ribbon('E04_R6_PanelFrames',stone,[(u-.85,14.06),(u+.85,14.06),(u+.85,14.83),(u-.85,14.83)],.055,gallery,.06,True)
   pts=[]
   for j in range(129):
    a=j*math.tau/128;r=.255+.075*math.cos(4*a);pts.append((u+r*math.cos(a),14.445+r*math.sin(a)))
   ribbon('E04_R6_QuatrefoilStone',stone,pts,.045,gallery,.07)
   ribbon('E04_R6_QuatrefoilGold',gold,pts,.012,lambda u,v,d:(side*(29.37-d),u,v),.006)
created=[]
for name,(vv,ff,mat) in groups.items():
 me=bpy.data.meshes.new(name);me.from_pydata(vv,[],ff);me.update();bm=bmesh.new();bm.from_mesh(me);bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces));bm.to_mesh(me);bm.free();o=bpy.data.objects.new(name,me);s.collection.objects.link(o);me.materials.append(mat);bevel=o.modifiers.new('Small cast edge','BEVEL');bevel.width=.002;bevel.segments=2;created.append(o)
bpy.context.view_layer.update();clear=[]
for o in created:
 pts=[o.matrix_world@Vector(p) for p in o.bound_box];lo=[min(p[i] for p in pts) for i in range(2)];hi=[max(p[i] for p in pts) for i in range(2)];dist=[0 if lo[i]<=0<=hi[i] else min(abs(lo[i]),abs(hi[i])) for i in range(2)];clear.append(math.hypot(*dist)-28)
assert min(clear)>0
bpy.ops.wm.save_as_mainfile(filepath=str(P/'Blender/SanctuaryR3/Sanctuary_R3_GateGallery.blend'))
(P/'Docs/SanctuaryR3Build/GateGalleryRound6.json').write_text(json.dumps({'new_mesh_groups':len(created),'gate_plate_faces':plate_count,'hinge_straps':8,'gallery_quatrefoils':60,'min_new_AABB_combat_gap_m':min(clear),'scope':'Surface construction and blind tracery; original gate and gallery dimensions preserved. No full collision/visual acceptance.'},indent=2))
print('GATE_GALLERY_R6_COMPLETE',len(created),plate_count,min(clear))
