"""Remove superseded installations, enlarge working stations, uniformly enlarge magic assemblies."""
import bpy,runpy,json,math
from pathlib import Path
from mathutils import Matrix,Vector
P=Path(__file__).resolve().parents[1];N=runpy.run_path(str(P/'Scripts/common.py'));G=N['Geo'];s=bpy.data.scenes['MilanLibrary_Final'];bpy.context.window.scene=s
assert not s.get('V6_station_revision'), 'Already applied; rebuild from Before snapshot.'
C=N['col']('V6_Station_Enrichment',s.collection);removed=[];changes=[]
old=[]
for name in ['V2_R04_Station','V2_R03_Station','IV4_W02_Folio_Installed','IV4_W03_Armillary_Installed']:
 r=bpy.data.objects.get(name)
 if r:old.extend([r]+list(r.children_recursive))
old += [o for o in s.objects if o.name in ['V2_StationGlow_R04','V2_StationGlow_R03']]
for o in set(old):removed.append({'name':o.name,'asset_id':o.get('asset_id'),'type':o.type,'render_hidden':o.hide_render,'collections':[c.name for c in o.users_collection]})
bpy.data.batch_remove(ids=set(old))
(P/'Audit/removed_obsolete_objects.json').write_text(json.dumps(removed,ensure_ascii=False,indent=2))

def move(o,mat):
 before=[list(r) for r in o.matrix_world];o.matrix_world=mat@o.matrix_world;changes.append({'object':o.name,'before':before,'after':[list(r) for r in o.matrix_world]})
# Counter carcass, top, separate carving and collision proxies share the exact same affine transform.
for side,zone in [(-1,'R04'),(1,'R03')]:
 oldcenter=Vector((side*5.5,14.2,0));newcenter=Vector((side*5.8,15.2,0));A=Matrix.Translation(newcenter)@Matrix.Diagonal((1.22,1.60,1,1))@Matrix.Translation(-oldcenter)
 r=bpy.data.objects[zone+'_WalnutCounter'];move(r,A);r['staff_opening_width_m']=2.68*1.22;r['V6_footprint_scale']=[1.22,1.6];r['station_role']='knowledge_upload' if side<0 else 'knowledge_search'
 for o in list(s.objects):
  if o.name.startswith('COL_IV4_'+zone+'_WalnutCounter') or o.name==('IV4_CounterLabel_UPLOAD' if side<0 else 'IV4_CounterLabel_SEARCH') or (o.get('asset_id')=='V5_W06' and o.get('zone_id')==zone):move(o,A)
 # Uniform assembly parent preserves each native frame driver and relative component placement.
 pivot=Vector((side*5.5,13.50 if side<0 else 13.52,1.066));target=Vector((side*5.8,14.13,1.066));rig=bpy.data.objects.new('V6_'+zone+'_MagicScaleRig',None);C.objects.link(rig);N['meta'](rig,'FX_SCALE',zone);rig['label_zh']='魔法主装置整体等比例放大';rig.location=target;rig.scale=(1.7,)*3;rig['uniform_scale_factor']=1.7
 aid='V5_FX01' if side<0 else 'V5_FX02'
 for o in list(bpy.data.collections['V5_Magic'].objects):
  if o.get('asset_id')==aid and not o.parent:
   basis=o.matrix_world.copy();o.parent=rig;o.matrix_parent_inverse=Matrix.Translation(-pivot);o.matrix_basis=basis
 for marker in ['UI','BookSpawn','Interaction']:
  o=bpy.data.objects[('F03_' if side<0 else 'F04_')+marker];before=list(o.location)
  o.location=(side*5.8,14.13,2.65) if marker!='Interaction' else (side*5.8,11.8,0)
  changes.append({'object':o.name,'position_before':before,'position_after':list(o.location)})
 bpy.data.objects['ZONE_'+zone].location=(side*5.8,15.2,0)
 light=bpy.data.objects['V5_Folio_AmberBounce' if side<0 else 'V5_Orb_BlueBounce'];light.location=(side*5.8,14.13,2.55);light.data.energy=56 if side<0 else 40
 # The original desk lamp is retained on the outer return; a linked matching lamp fills the inner return.
 lamp=bpy.data.objects[zone+'_DeskLamp'];lamp.location=(side*7.90,16.35,1.064)
 nr=lamp.copy();nr.name='V6_'+zone+'_InnerDeskLamp';C.objects.link(nr);nr.location=(side*3.70,16.35,1.064);N['meta'](nr,'L01',zone);nr['label_zh']='扩展柜台绿玻璃阅读灯'
 for p in lamp.children:
  ob=p.copy();ob.data=p.data;C.objects.link(ob);ob.parent=nr;ob.matrix_parent_inverse=p.matrix_parent_inverse.copy();ob.matrix_basis=p.matrix_basis.copy();N['meta'](ob,'L01',zone);ob['label_zh']='扩展柜台阅读灯构件'
 # Move existing practical desk illumination to its new fixture.
 for o in s.objects:
  if o.type=='LIGHT' and o.name.startswith('IV4_CounterReadingGlow') and o.location.x*side>0:o.location=(side*7.90,16.19,1.56)
 dtype='POINT';assert dtype in [x.identifier for x in bpy.types.Light.bl_rna.properties['type'].enum_items]
 d=bpy.data.lights.new('V6_InnerLampGlow_'+zone,dtype);d.energy=18;d.color=(1,.66,.31);d.shadow_soft_size=.12;o=bpy.data.objects.new(d.name,d);C.objects.link(o);o.location=(side*3.70,16.18,1.56);N['meta'](o,'L01',zone);o['label_zh']='新增阅读灯实光源'
# Retain all reading furniture and its props; relocate the second pair into the empty former station footprint.
for o in list(s.objects):
 if o.parent or not o.name.startswith(('IV4_','COL_IV4_')):continue
 if '16.2' in o.name:move(o,Matrix.Translation(Vector((0,1.9,0))))
 elif '21.7' in o.name:
  side=-1 if o.matrix_world.translation.x<0 else 1
  move(o,Matrix.Translation(Vector((side*.6,-13.3,0))));o['V6_role_zh']='入口资料整理与等待阅读区'
# Larger, readable labels, using a local font without adding an external dependency to the scene.
fontpath=Path('/System/Library/Fonts/PingFang.ttc');font=bpy.data.fonts.load(str(fontpath)) if fontpath.exists() else None
for name,text,english in [('IV4_CounterLabel_UPLOAD','知识上传','KNOWLEDGE UPLOAD'),('IV4_CounterLabel_SEARCH','知识检索','KNOWLEDGE SEARCH')]:
 o=bpy.data.objects[name];o.data.body=text if font else english;o.data.size=.20 if font else .105;o.data.align_x='CENTER';o.data.extrude=.001;o.location.z=.82
 if font:o.data.font=font
# Per-counter antique accessories: leather reference folios, scrolls and writing tools.
wood=bpy.data.materials['V5_Interior_Joinery'];brass=bpy.data.materials['V5_Magic_AntiqueBrass'];paper=bpy.data.materials['MAT_Old_Paper'];leather=bpy.data.materials['V5_Magic_BookLeather']
for side,zone in [(-1,'R04'),(1,'R03')]:
 for j in range(3):
  x=side*7.35+(j-1)*.19;y=14.10;h=.39+j*.055;z=1.066
  g=G().box((x,y+.02,z+h/2),(.125,.29,h-.035));o=g.object(f'V6_{zone}_DeskFolioPages_{j}',C,paper,'W02',bevel=.003);o['zone_id']=zone;o['label_zh']='柜台参考古籍纸页'
  g=G().box((x-.075,y,z+h/2),(.016,.34,h)).box((x+.075,y,z+h/2),(.016,.34,h)).box((x,y-.164,z+h/2),(.15,.021,h));o=g.object(f'V6_{zone}_DeskFolioLeather_{j}',C,leather,'W02',bevel=.004);o['zone_id']=zone;o['label_zh']='旧皮革柜台参考古籍'
  g=G()
  for zz in (.065,h-.065):g.box((x,y-.178,z+zz),(.143,.008,.012))
  g.quatre((x,y-.18,z+h*.51),.042,.003);o=g.object(f'V6_{zone}_DeskFolioGilt_{j}',C,brass,'W02',smooth=True);o['zone_id']=zone;o['label_zh']='参考古籍烫金纹章'
 # A low brass ink pot and sculpted feather at the other front flank.
 x=side*4.23;y=14.10
 g=G().lathe((x,y,1.066),[(.055,0),(.065,.025),(.060,.09),(.037,.11),(.030,.14)],32);o=g.object(f'V6_{zone}_Inkwell',C,brass,'W02',smooth=True);o['zone_id']=zone;o['label_zh']='复古黄铜墨水座'
 g=G().tube([(x,y,1.18),(x+.11,y,1.43)],.004,n=6).leaf((x+.06,y,1.30),.075,.25,-.38,.01);o=g.object(f'V6_{zone}_Quill',C,paper,'W02',smooth=True);o['zone_id']=zone;o['label_zh']='书写羽毛笔'
(P/'Audit/station_changes.json').write_text(json.dumps({'removed_objects':len(removed),'counter_xy_scale':[1.22,1.60],'counter_area_multiplier':1.22*1.60,'magic_uniform_scale':1.7,'station_centers':[[-5.8,15.2,0],[5.8,15.2,0]],'reading_first_delta_y':1.9,'reading_second_delta_y':-13.3,'transforms':changes},ensure_ascii=False,indent=2))
s['V6_station_revision']=True;s.frame_set(35);bpy.ops.file.pack_all();N['save']('expanded_stations_and_real_duplicate_removal')
print('V6 stations, removed',len(removed),'objects')
