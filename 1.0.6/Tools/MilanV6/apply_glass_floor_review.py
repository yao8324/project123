"""Extend the accepted native copy; preserve the central atrium and stair openings."""
import bpy,json,hashlib,math
from pathlib import Path
from mathutils import Vector
R=Path(__file__).resolve().parents[2];src=R/'Blender/MilanLibrary_Magic_V6/MilanLibrary_Magic_V6_PlaytestReview.blend';h=hashlib.sha256(src.read_bytes()).hexdigest()
bpy.ops.wm.open_mainfile(filepath=str(src),load_ui=False,use_scripts=False)
S=bpy.data.scenes['MilanLibrary_Final'];bpy.context.window.scene=S;bpy.context.window.view_layer=S.view_layers['Full_Collection'];S.frame_set(35)
C=bpy.data.collections.new('GlassFloor_Review');S.collection.children.link(C)
report={'inputSHA256':h,'fillBoundsBlender':[[-12,0,5.65],[12,6,6]],'atriumPreserved':True,'stairOpeningsPreserved':True,'retiredRails':[],'woodFloors':[],'plankSizeMetres':[.24,1.6],'jointMetres':.004,'surfaceZ':6.004}
# Retire only the former front edge railing, now inside the new continuous floor.
for o in list(S.objects):
 if o.name.startswith(('WV1_SouthBridge_6_','WV1_SouthBridgeBarrier_6_')):
  report['retiredRails'].append(o.name);bpy.data.objects.remove(o,do_unlink=True)
vs=[(-12,0,5.65),(12,0,5.65),(12,6,5.65),(-12,6,5.65),(-12,0,6),(12,0,6),(12,6,6),(-12,6,6)]
me=bpy.data.meshes.new('GFReview_EntranceUpperFloor');me.from_pydata(vs,[],[(0,3,2,1),(4,5,6,7),(0,1,5,4),(1,2,6,5),(2,3,7,6),(3,0,4,7)]);me.update();o=bpy.data.objects.new(me.name,me);C.objects.link(o);o['collision_role']='walkable';me.materials.append(bpy.data.materials['V5_Border_Walnut'])
floors=[x for x in S.objects if x.name.startswith(('G01_Gallery','G02_ApseGallery','G07_SouthBridge','GFReview_EntranceUpperFloor'))]
wood=bpy.data.materials['V5_Border_Walnut']
def clip(poly,axis,value,greater):
 out=[]
 for a,b in zip(poly,poly[1:]+poly[:1]):
  ia=(a[axis]>=value)if greater else(a[axis]<=value);ib=(b[axis]>=value)if greater else(b[axis]<=value)
  if ia:out.append(a)
  if ia!=ib:
   t=(value-a[axis])/(b[axis]-a[axis]);out.append((a[0]+t*(b[0]-a[0]),a[1]+t*(b[1]-a[1])))
 return out
for f in floors:
 f.data=f.data.copy();f.data.materials.clear();f.data.materials.append(wood)
 uv=f.data.uv_layers.active or f.data.uv_layers.new(name='UVMap')
 for p in f.data.polygons:
  p.material_index=0
  for li in p.loop_indices:
   v=f.matrix_world@f.data.vertices[f.data.loops[li].vertex_index].co;uv.data[li].uv=(v.x/1.2,v.y/2)
 verts=[];faces=[];uvs=[];planks=0
 for p in f.data.polygons:
  w=[f.matrix_world@f.data.vertices[i].co for i in p.vertices]
  if not all(abs(v.z-6)<.001 for v in w):continue
  poly=[(v.x,v.y)for v in w]
  for ix in range(math.floor(min(v[0]for v in poly)/.24),math.ceil(max(v[0]for v in poly)/.24)):
   offset=(ix%2)*.8
   for iy in range(math.floor((min(v[1]for v in poly)-offset)/1.6),math.ceil((max(v[1]for v in poly)-offset)/1.6)):
    x=ix*.24;y=iy*1.6+offset;q=poly[:]
    for ax,val,gt in [(0,x+.002,True),(0,x+.238,False),(1,y+.002,True),(1,y+1.598,False)]:
     if q:q=clip(q,ax,val,gt)
    if len(q)<3:continue
    start=len(verts);verts.extend((a,b,6.004)for a,b in q);faces.append(tuple(range(start,start+len(q))));uvs.extend(((a-x)/1.2+(ix*37%13)/13,(b-y)/2+(iy*17%11)/11)for a,b in q);planks+=1
 pm=bpy.data.meshes.new('GFReview_Planks_'+f.name);pm.from_pydata(verts,[],faces);pm.update();po=bpy.data.objects.new(pm.name,pm);C.objects.link(po);pm.materials.append(wood);po['collision_role']='none'
 puv=pm.uv_layers.new(name='UVMap')
 for loop in pm.loops:puv.data[loop.index].uv=uvs[loop.vertex_index]
 report['woodFloors'].append({'source':f.name,'planks':planks})
S['glass_floor_revision']='glass_floor_20261007';out=src.with_name('MilanLibrary_Magic_V6_GlassFloorReview.blend');bpy.ops.wm.save_as_mainfile(filepath=str(out));assert hashlib.sha256(src.read_bytes()).hexdigest()==h
(R/'Docs/MilanV6Unity/glass_floor_geometry.json').write_text(json.dumps(report,indent=2));print('GLASS_FLOOR_NATIVE_READY',len(floors),sum(r['planks']for r in report['woodFloors']))
