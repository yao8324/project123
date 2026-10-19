"""Reusable crown wards, luminous spine tooling, and editable floating archive books."""
import bpy,runpy,json,math,random,hashlib
from pathlib import Path
from mathutils import Matrix,Vector
from math import sin,cos,pi
P=Path(__file__).resolve().parents[1];N=runpy.run_path(str(P/'Scripts/common.py'));G=N['Geo'];s=bpy.data.scenes['MilanLibrary_Final'];bpy.context.window.scene=s
assert not s.get('V6_shelf_magic'), 'Already applied.'
C=N['col']('V6_Enchanted_Shelves',s.collection);D=N['col']('V6_Enchanted_BookSpines',s.collection);F=N['col']('V6_Floating_ArchiveBooks',s.collection)
random.seed(624)
def mat(name,color,emit):
 m=bpy.data.materials['V5_Magic_Gold'].copy();m.name='V6_'+name;m.diffuse_color=(*color,1)
 p=next(n for n in m.node_tree.nodes if n.type=='BSDF_PRINCIPLED');p.inputs['Base Color'].default_value=(*color,1);p.inputs['Emission Color'].default_value=(*color,1);p.inputs['Emission Strength'].default_value=emit;p.inputs['Metallic'].default_value=.35;p.inputs['Roughness'].default_value=.36
 return m
amber=mat('ShelfWard_Amber',(1,.46,.105),2.4);blue=mat('ShelfWard_Blue',(.18,.55,1),2.1);spinemats=[mat('SpineGlow_'+str(i),col,1.55) for i,col in enumerate([(1,.49,.10),(.13,.55,.72),(.48,.30,.86),(1,.62,.20)])]
for i,m in enumerate(spinemats):
 p=next(n for n in m.node_tree.nodes if n.type=='BSDF_PRINCIPLED');d=p.inputs['Emission Strength'].driver_add('default_value');d.driver.expression=f'1.55+0.22*sin(2*pi*(frame-1)/240+{i*pi/2})'
brass=bpy.data.materials['V5_Magic_AntiqueBrass'];paper=bpy.data.materials['MAT_Old_Paper'];leather=bpy.data.materials['V5_Magic_BookLeather']
def tag(o,aid,zh,source='',floor=0):
 N['meta'](o,aid,'R02',floor);o['label_zh']=zh;o['effect_only']=True
 if source:o['source_shelf_or_book']=source
 return o
def spark(g,c,r):
 x,y,z=c;g.add([(x-r,y,z),(x+r,y,z),(x,y-r,z),(x,y+r,z),(x,y,z-r),(x,y,z+r)],[(0,2,5),(2,1,5),(1,3,5),(3,0,5),(2,0,4),(1,2,4),(3,1,4),(0,3,4)])
def driver(o,path,i,expression):o.driver_add(path,i).driver.expression=expression
# Each original shelf group receives a crown ward. Double-sided cases receive two.
roots=sorted([o for o in s.objects if o.get('component')=='shelf_root'],key=lambda o:o.name);crown_count=0
for index,r in enumerate(roots):
 aid=r.get('asset_id');h=4.5 if aid=='B10' else 5.6;depth=1.2 if aid=='B10' else .6;xf=r.matrix_world.copy()
 if aid not in ['B01','B09','B10']:
  rr=bpy.data.objects.get(r.name+'_Carved_1')
  if not rr:continue
  xf=rr.matrix_world.copy()
 for face in range(2 if aid=='B10' else 1):
  m=xf@Matrix.Rotation(face*pi,4,'Z');g=G();y=-depth/2-.19;z=h+.24
  g.ring((0,y,z),.105,.004,'XZ',48)
  g.tube([(0,y,z+.081),(.053,y,z),(0,y,z-.081),(-.053,y,z),(0,y,z+.081)],.003,n=6)
  g.tube([(-.077,y,z),(+.077,y,z)],.0027,n=6)
  for k in range(6):a=k*pi/3;spark(g,(.155*cos(a),y,z+.155*sin(a)),.010)
  o=g.transformed(m).object('V6_CrownWard_'+r.name+f'_{face}',C,amber if index%3 else blue,'SH01',smooth=True);tag(o,'SH01','书架顶冠守护纹章',r.name,int(r.get('floor',0)));crown_count+=1
# Raised gilded runes only on selected existing spines, merged into four meshes for a bounded object count.
acc=[G() for i in range(4)];selected=[[] for i in range(4)];tints=[]
for book in bpy.data.collections['Preview_FullCollection'].objects:
 digest=int.from_bytes(hashlib.sha256(book.name.encode()).digest()[:4],'little')
 if digest%19!=0:continue
 bb=book.bound_box;w=max(v[0] for v in bb)-min(v[0] for v in bb);h=max(v[2] for v in bb)-min(v[2] for v in bb);y=min(v[1] for v in bb)-.007
 g=G();x=w*.28;z=h*.53
 for zz in [h*.16,h*.82]:g.box((0,y,zz),(w*.78,.0025,.006))
 g.tube([(0,y,z+h*.12),(x,y,z),(0,y,z-h*.12),(-x,y,z),(0,y,z+h*.12)],.0018,n=4)
 g.tube([(0,y,z-h*.20),(0,y,z+h*.20)],.0013,n=4)
 for sign in [-1,1]:g.box((sign*w*.38,y,h*.49),(.0025,.0025,h*.50))
 kind=digest%4;acc[kind].merge(g.transformed(book.matrix_world));selected[kind].append(book.name)
for i,g in enumerate(acc):
 ob=g.object('V6_EnchantedSpineTooling_'+str(i),D,spinemats[i],'BK01');tag(ob,'BK01','藏书书脊烫金符文与呼吸微光');ob['decorated_book_count']=len(selected[i])
# Floating display copies are atmospheric props, not additional book slots or user books.
float_rigs=[];open_count=0;closed_count=0
for j,r in enumerate([o for o in roots if o.get('asset_id')=='B10']):
 p=r.matrix_world.translation;loc=Vector((p.x,p.y,5.18));rig=bpy.data.objects.new('V6_ArchiveFloat_'+r.name,None);F.objects.link(rig);tag(rig,'BK02','书架上方悬浮藏书',r.name);rig.location=loc;rig.rotation_euler=(.16 if j%2 else -.12,0,(j%3-1)*.22)
 driver(rig,'location',2,f'5.18+0.055*sin(2*pi*(frame-1)/240+{j*.65})');driver(rig,'rotation_euler',2,f'{(j%3-1)*.22}+0.085*sin(2*pi*(frame-1)/240+{j*.65})');float_rigs.append(rig.name)
 if abs(p.y-25.5)<.01:
  rig.scale=(.62,)*3
  for source in bpy.data.objects['V5_FloatingBook'].children:
   ob=source.copy();ob.data=source.data;F.objects.link(ob);ob.name='V6_ArchiveOpen_'+r.name+'_'+source.name;ob.parent=rig;ob.matrix_parent_inverse=Matrix.Identity(4);ob.matrix_basis=source.matrix_basis.copy();tag(ob,'BK02','可翻页的悬浮典藏书',r.name)
  open_count+=1
 else:
  geoms=[(G().box((0,0,0),(.33,.47,.10)),paper,'Pages'),(G().box((0,0,-.059),(.37,.51,.019)).box((0,0,.059),(.37,.51,.019)).box((-.179,0,0),(.018,.51,.12)),leather,'Binding')]
  gold=G()
  for zz in [-.066,.066]:gold.tube([(-.17,-.24,zz),(.17,-.24,zz),(.17,.24,zz),(-.17,.24,zz),(-.17,-.24,zz)],.0035,n=6)
  for x in [-.10,.10]:gold.box((x,0,.071),(.012,.47,.007))
  gold.ring((0,0,.074),.075,.003,n=48);geoms.append((gold,brass,'Gilt'))
  for g,mat_,part in geoms:
   ob=g.object('V6_ArchiveClosed_'+r.name+'_'+part,F,mat_,'BK02',bevel=.002 if part!='Gilt' else 0);ob.parent=rig;ob.matrix_parent_inverse=Matrix.Identity(4);tag(ob,'BK02','旧皮革金属包角悬浮典藏书',r.name)
  closed_count+=1
 # Broken orbit and tiny dust are separate from physical books, above crown level.
 g=G();z=4.92
 pts=[(p.x+.35*cos(a),p.y+.35*sin(a),z+.035*sin(2*a)) for a in [i*pi*1.60/72 for i in range(73)]];g.tube(pts,.003,n=6)
 for k in range(22):a=random.uniform(0,2*pi);rr=random.uniform(.22,.40);spark(g,(p.x+rr*cos(a),p.y+rr*sin(a),random.uniform(4.89,5.65)),random.uniform(.004,.009))
 ob=g.object('V6_ArchiveWisps_'+r.name,F,amber if j%2 else blue,'SH02',smooth=True);tag(ob,'SH02','书架上方细碎星尘与导引光弧',r.name)
 dtype='POINT';assert dtype in [x.identifier for x in bpy.types.Light.bl_rna.properties['type'].enum_items]
 d=bpy.data.lights.new('V6_ArchiveBounce_'+r.name,dtype);d.energy=7;d.color=(1,.57,.19) if j%2 else (.18,.56,1);d.shadow_soft_size=.28;ob=bpy.data.objects.new(d.name,d);F.objects.link(ob);ob.location=(p.x,p.y,5.18);tag(ob,'SH02','悬浮藏书微光回照',r.name)
(P/'Audit/shelf_magic.json').write_text(json.dumps({'shelf_groups_covered':len(roots),'crown_wards':crown_count,'decorated_existing_spines':sum(map(len,selected)),'selected_spines':selected,'floating_open_books':open_count,'floating_closed_books':closed_count,'float_rigs':float_rigs,'extra_user_book_capacity':0,'original_preview_book_transforms_changed':False},ensure_ascii=False,indent=2))
s['V6_shelf_magic']=True;s.frame_set(35);N['save']('enchanted_shelves_and_archive_books')
print('Shelf wards',crown_count,'enchanted spines',sum(map(len,selected)),'floating books',open_count+closed_count)
