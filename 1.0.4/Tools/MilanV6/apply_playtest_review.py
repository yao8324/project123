"""Deterministic post-repair playtest revision. Keeps Assembly and CloudRepair untouched.
Run in background Blender with --python-exit-code 1. Exports a separate Review.blend.
"""
import bpy,json,math,hashlib,sys
from pathlib import Path
from mathutils import Vector
ROOT=Path(__file__).resolve().parents[2];D=ROOT/'Docs/MilanV6Unity';SRC=ROOT/'Blender/MilanLibrary_Magic_V6/MilanLibrary_Magic_V6_CloudRepair.blend';ORIGINAL=SRC.with_name('MilanLibrary_Magic_V6_Assembly.blend');OUT=SRC.with_name('MilanLibrary_Magic_V6_PlaytestReview.blend')
original_hash=hashlib.sha256(ORIGINAL.read_bytes()).hexdigest();input_hash=hashlib.sha256(SRC.read_bytes()).hexdigest()
bpy.ops.wm.open_mainfile(filepath=str(SRC),load_ui=False,use_scripts=False)
S=bpy.data.scenes['MilanLibrary_Final'];bpy.context.window.scene=S;bpy.context.window.view_layer=S.view_layers['Full_Collection'];S.frame_set(35)
V=bpy.data.collections.new('Playtest_Review_Visual');S.collection.children.link(V)
C=bpy.data.collections.new('Playtest_Review_Collision');S.collection.children.link(C);C.hide_render=True
report={'inputSHA256':input_hash,'originalSHA256':original_hash,'windows':[],'tables':[],'closedDoors':[],'unchangedFrontWindows':[]}
def bounds(o):
 vs=[o.matrix_world@v.co for v in o.data.vertices];return [Vector([min(v[i]for v in vs)for i in range(3)]),Vector([max(v[i]for v in vs)for i in range(3)])]
def local_bounds(o):return [Vector([min(v.co[i]for v in o.data.vertices)for i in range(3)]),Vector([max(v.co[i]for v in o.data.vertices)for i in range(3)])]
def coords(b):return [list(x)for x in b]
def edit(o,fn):
 o.data=o.data.copy()
 for v in o.data.vertices:v.co=fn(v.co.copy())
 o.data.update()
# Restore the two distorted stair-adjacent window assemblies before one coherent refit.
restore=['GF08_Side_frame.002','GF08_Side_frame.008','GF19_SolidWindowReveal.002','GF19_SolidWindowReveal.008']
targets={n:bpy.data.objects[n]for n in restore}
with bpy.data.libraries.load(str(ORIGINAL),link=False)as(a,b):b.objects=list(restore)
for name,loaded in zip(restore,b.objects):targets[name].data=loaded.data.copy();bpy.data.objects.remove(loaded,do_unlink=True)
frames=[o for o in S.objects if o.name.startswith(('GF08_Side_frame','GF09_TallApseTracery'))]
walls=[o for o in S.objects if o.name.startswith(('GF08_Side_wall','GF09_ApseWindowWall'))]
reveals=[o for o in S.objects if o.name.startswith('GF19_SolidWindowReveal')]
glass=sorted([o for o in S.objects if o.name.startswith('GF21_FiguralGlazing')],key=lambda o:o.name)
assert len(glass)==27 and len(frames)==22
for index,g in enumerate(glass):
 if index>=22:
  report['unchangedFrontWindows'].append({'glass':g.name,'bounds':coords(bounds(g)),'reason':'Front risalit uses its separate GF27 reveal and A04 host wall; all five retained after native aperture scan and Unity close-up review.'});continue
 nearest=lambda objects:min(objects,key=lambda o:(o.matrix_world.translation-g.matrix_world.translation).length)
 f,w,r=nearest(frames),nearest(walls),nearest(reveals)
 for item in [f,w,r]:assert (item.matrix_world.translation-g.matrix_world.translation).length<.03,(g.name,item.name)
 before={'glass':coords(bounds(g)),'frame':coords(bounds(f)),'reveal':coords(bounds(r))}
 # Preserve full carved depth on ordinary bays; only stair-adjacent bays need shallower relief.
 flo,fhi=local_bounds(f);y0,y1=flo.y,fhi.y
 depth=.24 if f.name in ('GF08_Side_frame.002','GF08_Side_frame.008') else y1-y0
 front=.04-depth
 edit(f,lambda p:Vector((p.x,front+(p.y-y0)/(y1-y0)*depth,p.z)))
 # Continuous reveal bridges the decorated exterior to the inner wall face, not a filled wall.
 rlo,rhi=local_bounds(r);y0,y1=rlo.y,rhi.y
 edit(r,lambda p:Vector((p.x,front+(p.y-y0)/(y1-y0)*(.41-front),p.z)))
 # Seat closed painted pane just inside the inner wall face: no deep walk-in pocket.
 glo,ghi=local_bounds(g);y0,y1=glo.y,ghi.y;z0,z1=glo.z,ghi.z
 edit(g,lambda p:Vector((p.x*1.055,.26+(p.y-y0)/(y1-y0)*.12,z0-.025+(p.z-z0)/(z1-z0)*(z1-z0+.05))))
 for item in [f,r,g]:item['collision_role']='barrier';item['playtest_revision']='review_20261007'
 report['windows'].append({'glass':g.name,'frame':f.name,'wall':w.name,'reveal':r.name,'before':before,'after':{'glass':coords(bounds(g)),'frame':coords(bounds(f)),'reveal':coords(bounds(r))},'paneInnerLocalY':.38,'hostInnerLocalY':local_bounds(w)[1].y,'originalUVsRetained':True,'frameDepth':depth,'stairClearanceVariant':depth==.24})
FACES=[(0,3,2,1),(4,5,6,7),(0,1,5,4),(1,2,6,5),(2,3,7,6),(3,0,4,7)]
def box(n,lo,hi,matrix=None):
 x,y,z=lo;X,Y,Z=hi;vs=[(x,y,z),(X,y,z),(X,Y,z),(x,Y,z),(x,y,Z),(X,y,Z),(X,Y,Z),(x,Y,Z)]
 if matrix:vs=[matrix@Vector(v)for v in vs]
 me=bpy.data.meshes.new(n);me.from_pydata(vs,[],FACES);me.update();o=bpy.data.objects.new(n,me);C.objects.link(o);o.hide_render=True;o['collision_role']='barrier';o['playtest_revision']='review_20261007';return o
# Table collision follows major disconnected joinery parts, retaining open space below tops.
for o in list(S.objects):
 if not o.name.startswith(('IV4_W04_Table_WalnutJoinery.','V5_DeskConsole_','V5_ConsoleBrackets_')) or o.type!='MESH':continue
 me=o.data;parents=list(range(len(me.vertices)))
 def root(i):
  while parents[i]!=i:parents[i]=parents[parents[i]];i=parents[i]
  return i
 for e in me.edges:a,b=e.vertices;parents[root(a)]=root(b)
 components={}
 for i in range(len(me.vertices)):components.setdefault(root(i),[]).append(me.vertices[i].co)
 proxies=[]
 for points in components.values():
  lo=Vector([min(v[i]for v in points)for i in range(3)]);hi=Vector([max(v[i]for v in points)for i in range(3)]);size=hi-lo
  if o.name.startswith('IV4_') and (min(size)<.045 or max(size)<.25):continue
  proxy=box('RV_Table_'+o.name+'_'+str(len(proxies)),lo,hi,o.matrix_world);proxy['source_object']=o.name
  proxies.append({'name':proxy.name,'localBounds':[list(lo),list(hi)]})
 report['tables'].append({'source':o.name,'worldBounds':coords(bounds(o)),'proxies':proxies,'openUnderTableSpacePreserved':True})
# Four secondary facade entrances close visibly; central pair stays open. No interaction code.
for leaf in [o for o in S.objects if o.name.startswith('E02_DoorLeaf')]:
 x=leaf.parent.matrix_world.translation.x
 if abs(x)<3:continue
 hinge=leaf.parent;hinge.rotation_euler.z=0;report['closedDoors'].append({'leaf':leaf.name,'hinge':hinge.name,'angleRadians':0})
assert len(report['closedDoors'])==8
# One connected apron, with continuous slope along the full front and both outer ends.
# Retire only the five old ramp visuals/proxies; keep step and facade source geometry.
for o in list(S.objects):
 if o.name.startswith(('WV1_EntranceRamp_','WV1_EntranceRampProxy_')):bpy.data.objects.remove(o,do_unlink=True)
xs=[-19.4,-17.6,17.6,19.4];ys=[-9.8,-5.6,0.0];vs=[]
for y in ys:
 for x in xs:
  f=min(1,max(0,(y+9.8)/4.2));side=min(1,max(0,(19.4-abs(x))/1.8));vs.append((x,y,-.60+.615*min(f,side)))
faces=[]
for row in range(2):
 for col in range(3):i=row*4+col;faces.append((i,i+1,i+5,i+4))
# Closed underside and perimeter give a proper watertight collider, with no invisible tall wall.
perimeter=[0,1,2,3,7,11,10,9,8,4];bottom=[]
for i in perimeter:bottom.append(len(vs));x,y,z=vs[i];vs.append((x,y,-.75))
for j,i in enumerate(perimeter):k=(j+1)%len(perimeter);faces.append((i,bottom[j],bottom[k],perimeter[k]))
faces.append(tuple(reversed(bottom)))
me=bpy.data.meshes.new('RV_ContinuousFrontApron');me.from_pydata(vs,[],faces);me.update();apron=bpy.data.objects.new('RV_ContinuousFrontApron',me);V.objects.link(apron);me.materials.append(bpy.data.materials['MAT_V2_Ivory_Carving']);apron['collision_role']='walkable'
uv=me.uv_layers.new(name='UVMap')
for poly in me.polygons:
 axis=max(range(3),key=lambda i:abs(poly.normal[i]))
 for li in poly.loop_indices:
  p=me.vertices[me.loops[li].vertex_index].co;uv.data[li].uv=((p.y if axis==0 else p.x)*.5,(p.y if axis==2 else p.z)*.5)
report['frontApron']={'bounds':coords(bounds(apron)),'plateauHeight':.015,'groundHeight':-.6,'mainRampRun':4.2,'frontWidth':38.8,'sideTaperWidth':1.8,'material':'MAT_V2_Ivory_Carving','uvUnitsPerMetre':.5,'oldFiveRampsRemoved':True,'expectedCentralDoorPassable':True,'sideDoorsAuthoredClosed':4}
S['playtest_revision']='review_20261007'
bpy.context.view_layer.update();D.mkdir(parents=True,exist_ok=True)
(D/'playtest_review_geometry.json').write_text(json.dumps(report,indent=2))
bpy.ops.wm.save_as_mainfile(filepath=str(OUT));assert hashlib.sha256(ORIGINAL.read_bytes()).hexdigest()==original_hash;assert hashlib.sha256(SRC.read_bytes()).hexdigest()==input_hash
print('PLAYTEST_REVIEW_NATIVE_READY',len(report['windows']),len(report['tables']),len(report['closedDoors']),flush=True)
