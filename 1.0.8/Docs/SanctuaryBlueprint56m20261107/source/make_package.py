from pathlib import Path
import json,csv,math,re,hashlib,zipfile,html
import numpy as np
from PIL import Image
from reportlab.pdfgen import canvas
from reportlab.lib import colors
from reportlab.lib.pagesizes import A3,A4,landscape
from reportlab.pdfbase import pdfmetrics
from reportlab.pdfbase.ttfonts import TTFont
from reportlab.graphics.shapes import Drawing,Line,Rect,Circle,Polygon,PolyLine,String
from reportlab.graphics import renderPDF,renderSVG
from reportlab.platypus import SimpleDocTemplate,Paragraph,Spacer,Table,TableStyle,PageBreak
from reportlab.lib.styles import ParagraphStyle
import fitz

ROOT=Path(__file__).resolve().parents[1];IMG=ROOT/'images';DATA=ROOT/'data';DRAW=ROOT/'drawings';QA=ROOT/'qa'
for p in (DATA,DRAW,QA):p.mkdir(exist_ok=True)
pdfmetrics.registerFont(TTFont('CN','/System/Library/Fonts/Supplemental/Arial Unicode.ttf'))
W,H=landscape(A3);INK='#242B32';GOLD='#AD874B';BLUE='#35697C';PALE='#F4F1E9';GRAY='#7B8187'
def js(name,data): (DATA/name).write_text(json.dumps(data,ensure_ascii=False,indent=2)+'\n')
def csvout(name,rows):
 with (DATA/name).open('w',newline='',encoding='utf-8-sig') as f:
  w=csv.DictWriter(f,fieldnames=list(rows[0]));w.writeheader();w.writerows(rows)

dims={'version':'R3-56m-winged','units':'m','coordinate_system':'Unity: +Y up, +Z north/gate; Blender Xb=Xu,Yb=Zu,Zb=Yu',
 'authority':'Numeric data and vector dimensions override generated image pixel proportions.',
 'scope':'Design specification only; no new 56m Unity build or character mesh is delivered by this package.',
 'hall':{'clear_width_x':64,'clear_length_z':72,'inner_vault_height_y':36,'wall_thickness':1.2,'outer_width_x':66.4,'outer_length_z':74.4,'roof_outer_y':37.2,'floor_thickness':.35,'vault_spring_y':24},
 'arena':{'diameter':56,'radius':28,'previous_diameter':28,'horizontal_scale_factor':2,'area_factor':4,'clear_area_m2':math.pi*28**2,'spawn':[0,0,-22],'boss_start':[0,0,25]},
 'column':{'count':12,'x_rows':[-30,30],'z_stations':[-30,-18,-6,6,18,30],'base_width':2.8,'base_depth':2.8,'base_height':1.15,'core_diameter':1.2,'satellite_count':8,'satellite_diameter':.4,'satellite_center_radius':.8,'shaft_top':22.75,'capital_top':24,'capital_outer_diameter':2.7,'leaf_count_per_row':12,'leaf_rows':2,'middle_collar_y':[12.2,13.1]},
 'vault':{'transverse_arches':6,'central_bays':5,'bay_length':12,'span':60,'spring_y':24,'rise':12,'main_rib_width':.65,'main_rib_depth':.90,'diagonal_rib_width':.45,'diagonal_rib_depth':.55,'secondary_rib_width':.22,'secondary_rib_depth':.28,'web_thickness':.35},
 'arcade':{'bays_each_side':5,'bay_length':12,'depth':3,'clear_opening':9.8,'spring_y':7.6,'apex_y':12.4,'window_width':6.4,'window_bottom':1,'window_apex':11.6,'gallery_floor_y':13.8,'gallery_clear_width':2.4,'gallery_railing_top':15,'gallery_apex':17.4,'gallery_arches_per_bay':3,'high_window_bottom':18,'high_window_top':23.2},
 'north_gate':{'width':16,'height':24,'spring_y':17,'leaf_width':7.92,'leaf_depth':.50,'center_gap':.16,'frame_width':20.8,'frame_height':28.2,'frame_depth':1.8,'seal_radius':5.4,'seal_y':10.8,'seal_strip_width':.18,'seal_strip_depth':.025,'secondary_seal_radius':4.8,'crossbar':[15.4,.5,.3],'crossbar_y':9.8,'hinges':[[-8,0,36],[8,0,36]]},
 'south_entry':{'width':10,'height':16,'spring_y':11,'frame_width':13.2,'frame_height':19,'frame_depth':1.4},
 'floor':{'tile':[1.6,1.6],'secondary_tile':[.8,1.6],'grout_width':[.004,.006],'inlay_radii':[6,6.15,17.6,25.8,26.05],'inlay_surface_step_max':.001},
 'character':{'body_height':2.15,'heads':8.25,'helmet_top':2.28,'halo_top':2.54,'armored_shoulder_width':1.08,'torso_depth':.42,'pelvis_width':.50,'boot_length':.34,'helmet_box':[.28,.36,.32],'joint_y':{'ankle':.13,'knee':.62,'hip':1.13,'waist':1.30,'shoulder':1.78,'neck':1.94,'eyes':2.04,'head_top':2.15}},
 'wings':{'span_open':5.8,'tip_top_open_y':3.10,'bottom_open_y':.85,'depth_open':.75,'width_folded':1.65,'top_folded_y':2.65,'bottom_folded_y':.30,'depth_folded':.85,'feathers_each_side':{'primary':8,'secondary':6,'coverts':5},'feather_thickness':[.004,.008],'root_reinforcement_thickness':.012,'layer_spacing':[.020,.035],'backplate':[.40,.58,.06]},
 'sword':{'total_length':1.70,'blade_length':1.26,'grip_length':.32,'pommel_length':.12,'guard_width':.54,'blade_root_width':.085,'spine_thickness':.012,'grip_pivot_from_pommel':.28,'guard_station_from_pommel':.44},
 'shield':{'width':.78,'height':1.20,'depth':.12,'plate_thickness':.004,'lining_thickness':.008,'gold_border_width':.030,'pivot':[0,0,-.08]}}
js('dimensions.json',dims)
rows=[]
def place(id,kind,x,y,z,yaw=0,w=0,h=0,d=0,note='bottom-center pivot'):
 rows.append(dict(id=id,module=kind,x_m=x,y_m=y,z_m=z,yaw_deg=yaw,width_m=w,height_m=h,depth_m=d,scale_x=1,scale_y=1,scale_z=1,note=note))
for side,x in [('W',-30),('E',30)]:
 for n,z in enumerate(dims['column']['z_stations'],1):place(f'E02_{side}{n}','bundled_pier',x,0,z,0,2.8,24,2.8)
for n,z in enumerate(dims['column']['z_stations'],1):place(f'E05_A{n}','transverse_arch',0,24,z,0,60,12,.90,'spring-center pivot; envelope follows arch')
for side,x,yaw in [('W',-32,90),('E',32,-90)]:
 for n,z in enumerate([-24,-12,0,12,24],1):
  place(f'E03_{side}{n}','lancet_window_bay' if n%2 else 'service_bay',x,0,z,yaw,12,12.4,3,'wall origin; local +Z faces room; bay extends into room')
  place(f'E04_{side}{n}','upper_gallery_bay',x,13.8,z,yaw,12,10.2,2.4,'wall origin; local +Z faces room')
place('E06_N','sealed_gate_frame',0,0,36,180,20.8,28.2,1.8)
place('E07_S','entry_frame',0,0,-36,0,13.2,19,1.4)
for id,x,y,z,yaw in [('A',-25,0,-29,8),('B',-25,.78,-29,8),('C',-23.5,0,-30,-12),('D',25,0,-30,-10)]:place('P01_'+id,'transit_chest',x,y,z,yaw,1.25,.78,.72)
place('P02_A','repair_workbench',-27,0,-27,15,1.8,.9,.75)
place('P03_A','inventory_stand',-24,0,-26.5,10,.55,1.45,.75)
place('P04_A','empty_cradle',-26,0,-31,0,.95,.35,.75)
for side,x,yaw in [('W',-31.82,90),('E',31.82,-90)]:
 for n,z in enumerate([-24,-12,0,12,24],1):place(f'P06_{side}{n}','wall_lantern',x,2.8,z+4,yaw,.35,.70,.45,'lamp bottom pivot; bracket points into room')
csvout('module_placements.csv',rows)
materials=[
 ('M01','墙体石灰岩','#B9AFA0',0,.52,.68,'细孔/分块接缝；避免全墙镜面'),('M02','象牙大理石','#CEC8B9',0,.28,.42,'云斑0.6-1.8m；灰脉2-10mm'),('M03','蓝灰大理石地面','#414650',0,.30,.45,'低对比灰白纹脉；金条齐平'),('M04','古金镶边','#B28A46',1,.28,.45,'凹处粗糙；倒角0.18-0.25'),('M05','锻铁封印门','#282E36',1,.48,.64,'细锤痕和轻边缘磨损'),('M06','胡桃木','#503322',0,.45,.65,'顺纹木理；断面单独UV'),('M07','深棕皮革','#40291F',0,.50,.72,'皮纹/缝线/折边'),('M08','蓝绳','#25496B',0,.65,.82,'18mm绳径；三股扭绳'),('M09','象牙布料','#D9D1BB',0,.70,.90,'细织纹；金线刺绣单独材质'),('M10','盔甲象牙釉','#DDD6C4',0,.24,.36,'磨损只在暴露边缘'),('M11','冷钢/链甲','#737B82',1,.32,.52,'链环有厚度；铰接处偏暗'),('M12','黄金羽片','#C39A50',1,.24,.38,'羽脉浮雕；羽缘倒角略亮')]
mrows=[dict(id=a,name=b,srgb_hex=c,metallic=d,roughness_min=e,roughness_max=f,note=g) for a,b,c,d,e,f,g in materials];csvout('materials.csv',mrows)
anims=[
 ('A01','slow_walk_first_half',0,.55,.08,.12,'smoothstep','2.0 m/s; left foot lead; no flap'),
 ('A01','slow_walk_second_half',.55,1.10,.12,.08,'smoothstep','right foot lead; seamless cycle endpoint'),
 ('A02','anticipation',0,.18,.10,.65,'smoothstep','root stationary; torso forward18deg'),('A02','accelerate_open',.18,.26,.65,.90,'smoothstep','16 m/s root translation'),('A02','dash',.26,.68,.90,.90,'hold','16 m/s; swept-back35deg'),('A02','brake',.68,.82,.90,.70,'smoothstep','root stops after8m; wings lateral'),('A02','recover',.82,.95,.70,.12,'smoothstep','return to locomotion'),
 ('A03','anticipation',0,.20,.12,.65,'smoothstep','crouch; right sword/left shield persist'),('A03','takeoff',.20,.45,.65,1,'smoothstep','FootOff at0.20'),('A03','apex',.45,.70,1,1,'hold','Apex at0.65; foot height4m'),('A03','descending_swing',.70,1.10,1,.75,'smoothstep','horizontal reach10m at1.10'),('A03','landing',1.10,1.25,.75,.55,'smoothstep','FootContact at1.10'),('A03','recover',1.25,1.60,.55,.12,'smoothstep','WingFoldComplete at1.60')]
csvout('animation_states.csv',[dict(action=a,phase=b,start_s=c,end_s=d,wing_start=e,wing_end=f,interpolation=g,note=h) for a,b,c,d,e,f,g,h in anims])
op=np.array([[.28,1.78,-.24],[1.30,2.18,-.28],[2.10,2.75,-.20],[2.90,3.10,-.15]])
lens=np.linalg.norm(np.diff(op,axis=0),axis=1)
cl=np.zeros((4,3));cl[0]=op[0];cl[1]=[.65,2.56,op[0,2]-math.sqrt(lens[0]**2-.37**2-.78**2)];cl[2]=[.50,cl[1,1]-math.sqrt(lens[1]**2-.15**2-(-.62-cl[1,2])**2),-.62];cl[3]=[.60,cl[2,1]-math.sqrt(lens[2]**2-.10**2-.12**2),-.74]
def basis(a,b):
 y=(b-a)/np.linalg.norm(b-a);z=np.array([0.,0.,1.]);z-=np.dot(z,y)*y;z/=np.linalg.norm(z);x=np.cross(y,z);return np.column_stack([x,y,z])
def quat(m):
 # Stable eigen-decomposition; output xyzw, normalized, positive w.
 K=np.array([[m[0,0]-m[1,1]-m[2,2],m[1,0]+m[0,1],m[2,0]+m[0,2],m[2,1]-m[1,2]],[m[1,0]+m[0,1],m[1,1]-m[0,0]-m[2,2],m[2,1]+m[1,2],m[0,2]-m[2,0]],[m[2,0]+m[0,2],m[2,1]+m[1,2],m[2,2]-m[0,0]-m[1,1],m[1,0]-m[0,1]],[m[2,1]-m[1,2],m[0,2]-m[2,0],m[1,0]-m[0,1],np.trace(m)]])/3
 values,vectors=np.linalg.eigh(K);q=vectors[:,np.argmax(values)];return (q if q[3]>=0 else -q).tolist()
rig={'axis':'bone local +Y toward child; local +Z projected world +Z; +X=Y cross Z; quaternion xyzw','segment_lengths_m':lens.tolist(),'interpolation':'local quaternion slerp only; fixed bone offsets; smoothstep windows','windows':{'root':[0,.4],'elbow':[.15,.7],'wrist':[.35,.9],'fan':[.45,1]},'states':{}}
for label,pts in [('open',op),('folded',cl)]:
 rig['states'][label]={}
 for side,sign in [('R',1),('L',-1)]:
  q=pts.copy();q[:,0]*=sign;prev=np.eye(3);rot=[]
  for i in range(3):
   mat=basis(q[i],q[i+1]);rot.append(quat(prev.T@mat));prev=mat
  rig['states'][label][side]={'joint_positions_m':q.tolist(),'local_quaternions_xyzw':rot}
js('wing_rig.json',rig)
# Keep brief rounded control points synchronized with the authoritative rig.
brief=ROOT/'BUILD_BRIEF.md';s=brief.read_text();s=re.sub(r'elbow=\(0\.65,2\.56,-[0-9.]+\)，wrist=\(0\.50,[0-9.]+,-0\.62\)，tip=\(0\.60,[0-9.]+,-0\.74\)',f'elbow=(0.65,2.56,{cl[1,2]:.6f})，wrist=(0.50,{cl[2,1]:.6f},-0.62)，tip=(0.60,{cl[3,1]:.6f},-0.74)',s);brief.write_text(s)
parts=[('E01','地面'),('E02','束柱'),('E03','下层拱廊'),('E04','回廊高窗'),('E05','穹顶'),('E06','北封印门'),('E07','南入口'),('P01','箱'),('P02','工具台'),('P03','清单架'),('P04','空托位'),('P05','导流管'),('P06','灯'),('R01','头盔背饰'),('R02','躯干'),('R03','上肢'),('R04','手'),('R05','下肢裙甲'),('R06','靴'),('W01','双翼'),('C01','布料'),('S01','剑'),('S02','盾')]+[(a,b) for a,b,*_ in materials]+[('L01','照明'),('A01','慢走'),('A02','冲刺'),('A03','跳跃攻击')]
rubrics={'static':{'structure':25,'dimensions':25,'detail':20,'material':20,'placement':10},'animation':{'key_pose':25,'timing_motion':25,'wing_state':25,'contact_equipment':25},'material':{'basecolor':20,'metallic_roughness':30,'texture_scale':25,'seams_repetition':15,'wear':10},'lighting':{'hierarchy':25,'temperature':20,'material_readability':25,'telegraph_readability':20,'exposure':10}}
js('acceptance_rubrics.json',rubrics)
csvout('acceptance_scorecard.csv',[dict(id=a,part=b,rubric='animation' if a.startswith('A') else 'material' if a.startswith('M') else 'lighting' if a.startswith('L') else 'static',hard_gates='PENDING',criterion1='',criterion2='',criterion3='',criterion4='',criterion5_or_NA='',total100='',minimum=90,evidence='',status='NOT_EVALUATED') for a,b in parts])
pipepaths=[]
for sign in [-1,1]:
 for level in [.65,.90]:
  pipepaths.append({'id':f'P05_{sign}_{level}','outer_diameter':.10,'clamp_spacing':1.8,'centerline_m':[[sign*31.87,level,-32],[sign*31.87,level,32],[sign*31.87,level,35.65],[sign*9.0,level,35.65],[sign*9.0,8.0,35.65]],'cap_start_end':True})
js('pipe_paths.json',pipepaths)


# Shared vector drawing vocabulary: exact same geometry rendered to SVG and PDF.
def c(v):return colors.HexColor(v)
def tx(d,x,y,t,size=11,col=INK,anchor='start'):d.add(String(x,y,str(t),fontName='CN',fontSize=size,fillColor=c(col),textAnchor=anchor))
def ln(d,x1,y1,x2,y2,col=INK,width=1,dash=None):d.add(Line(x1,y1,x2,y2,strokeColor=c(col),strokeWidth=width,strokeDashArray=dash))
def rect(d,x,y,w,h,fill=None,stroke=INK,width=1):d.add(Rect(x,y,w,h,fillColor=c(fill) if fill else None,strokeColor=c(stroke) if stroke else None,strokeWidth=width))
def circ(d,x,y,r,fill=None,stroke=INK,width=1):d.add(Circle(x,y,r,fillColor=c(fill) if fill else None,strokeColor=c(stroke) if stroke else None,strokeWidth=width))
def poly(d,pts,fill=None,stroke=INK,width=1,closed=False):d.add((Polygon if closed else PolyLine)([q for p in pts for q in p],fillColor=c(fill) if fill else None,strokeColor=c(stroke),strokeWidth=width))
def dimh(d,x1,x2,y,text,origin_y=None):
 ln(d,x1,y,x2,y,BLUE,.7)
 for x in [x1,x2]:
  ln(d,x-3,y-3,x+3,y+3,BLUE,.8)
  if origin_y is not None:ln(d,x,origin_y,x,y-5,BLUE,.5)
 tx(d,(x1+x2)/2,y+7,text,10,BLUE,'middle')
def dimv(d,x,y1,y2,text,origin_x=None):
 ln(d,x,y1,x,y2,BLUE,.7)
 for y in [y1,y2]:
  ln(d,x-3,y-3,x+3,y+3,BLUE,.8)
  if origin_x is not None:ln(d,origin_x,y,x-5,y,BLUE,.5)
 tx(d,x+7,(y1+y2)/2,text,10,BLUE)
def sheet(code,title,subtitle):
 d=Drawing(W,H);rect(d,0,0,W,H,PALE,None);tx(d,42,H-46,code,11,GOLD);tx(d,42,H-80,title,27);tx(d,42,H-104,subtitle,11,GRAY);ln(d,42,H-120,W-42,H-120,GOLD,1)
 tx(d,42,28,'终扉圣堂 / R3 / 2026-11-07 / 单位 m / 数值优先于生成图像素比例',9,GRAY);tx(d,W-42,28,code,9,GRAY,'end');return d
def note(d,x,y,lines,size=11,step=23):
 for i,t in enumerate(lines):tx(d,x,y-i*step,t,size)
def archpts(w,h,r,n=80):return [(x,h+r*math.sqrt(max(0,4-(1+abs(x)/w)**2))/math.sqrt(3)) for x in np.linspace(-w,w,n+1)]
def arch(d,ox,oy,s,w,h,r,col=INK,width=1):poly(d,[(ox+x*s,oy+y*s) for x,y in archpts(w,h,r)],stroke=col,width=width)
drawings=[]

d=sheet('D01','总平面 / 56 m 战斗圆','A3 横向，按 100% 打印为 1:400；中心 (0,0,0)，北侧封印门为 +Z。')
s=1000/400*72/25.4;ox=370;oy=405
rect(d,ox-33.2*s,oy-37.2*s,66.4*s,74.4*s,'#D8D1C4',INK)
rect(d,ox-32*s,oy-36*s,64*s,72*s,'#FDFBF7',INK)
circ(d,ox,oy,28*s,'#E6E9E9',BLUE,1.7);circ(d,ox,oy,14*s,None,GRAY,.7)
for r in dims['floor']['inlay_radii']:circ(d,ox,oy,r*s,None,GOLD,.45)
ln(d,ox,oy-36*s,ox,oy+36*s,GRAY,.5,[4,4]);ln(d,ox-32*s,oy,ox+32*s,oy,GRAY,.5,[4,4])
for row in rows:
 if row['module']=='bundled_pier':rect(d,ox+(row['x_m']-1.4)*s,oy+(row['z_m']-1.4)*s,2.8*s,2.8*s,'#CBBE9E')
 if row['id'].startswith('P0') and row['module']!='wall_lantern':rect(d,ox+(row['x_m']-.6)*s,oy+(row['z_m']-.4)*s,1.2*s,.8*s,GOLD)
rect(d,ox-8*s,oy+35.5*s,16*s,.5*s,'#323B42');rect(d,ox-5*s,oy-36*s,10*s,.5*s,'#323B42')
for x,z,t in [(0,-22,'玩家'),(0,25,'骑士')]:circ(d,ox+x*s,oy+z*s,4,BLUE,None);tx(d,ox+9,oy+z*s,t,10)
dimh(d,ox-32*s,ox+32*s,oy-36*s-28,'64.00 净宽',oy-36*s)
dimv(d,ox-32*s-35,oy-36*s,oy+36*s,'72.00 净长',ox-32*s)
dimh(d,ox-28*s,ox+28*s,oy-8,'Ø56.00 中央净空')
note(d,690,650,['01  战斗半径 28 m；旧半径 14 m 以细灰圈对照。','02  大厅净空 64 × 72 m；墙厚 1.2 m 向外。','03  主柱 X=±30，柱基内缘 X=±28.6。','04  每列 6 柱，纵向柱距 12 m，共 5 个中央跨。','05  外围陈设均在中央圆外；蓝绳箱集中西南角。','06  北门宽 16 m；南入口宽 10 m。','07  人体与工具保持原尺寸，不随场地放大。',f'08  CSV 提供 {len(rows)} 个模块/道具实例的坐标。'],11,30)
dimh(d,720,720+10*s,275,'10 m 比例尺');tx(d,720,235,'图纸尺寸不可由浏览器缩放后的屏幕长度换算。',10,GRAY)
drawings.append(('D01_plan',d))

d=sheet('D02','北门与横向剖面 / 立面标高','A3 100% 打印 1:250。立面展示主要比例；雕花见建筑详图。')
s=1000/250*72/25.4;ox=420;oy=180
ln(d,ox-32*s,oy,ox+32*s,oy,INK,2)
for x in [-30,30]:rect(d,ox+(x-1)*s,oy,2*s,24*s,'#D5CAB8');rect(d,ox+(x-1.4)*s,oy,2.8*s,1.15*s,'#C5B89D')
arch(d,ox,oy,s,30,24,12,INK,2);ln(d,ox-30*s,oy+24*s,ox+30*s,oy+24*s,GRAY,.5,[3,3])
rect(d,ox-8*s,oy,16*s,17*s,'#333B42');poly(d,[(ox-8*s,oy+17*s)]+[(ox+x*s,oy+y*s) for x,y in archpts(8,17,7)]+[(ox+8*s,oy+17*s)],'#333B42',closed=True)
for i in range(5):arch(d,ox,oy,s,8.4+i*.5,17.2,7.6+i*.85,GOLD if i%2 else INK,1)
for r in [5.4,4.8]:circ(d,ox,oy+10.8*s,r*s,None,GOLD,2 if r==5.4 else 1)
rect(d,ox-.08*s,oy,.16*s,23.9*s,'#AFC6CD',None);rect(d,ox-7.7*s,oy+9.55*s,15.4*s,.5*s,GOLD)
dimh(d,ox-8*s,ox+8*s,oy-28,'16.00 门净宽',oy)
dimv(d,ox+11.3*s,oy,oy+24*s,'24.00 门顶')
dimv(d,ox-33*s,oy,oy+36*s,'36.00 冠顶')
for y,t in [(1.15,'柱基 1.15'),(12.4,'下拱 12.40'),(13.8,'回廊地 13.80'),(18,'高窗底 18.00'),(24,'起拱 24.00'),(28.2,'门框 28.20'),(36,'冠顶 36.00')]:ln(d,790,oy+y*s,830,oy+y*s,GRAY,.7);tx(d,840,oy+y*s-3,t,10)
note(d,850,180,['门框外宽 20.80 m','门叶每扇 7.92 m','中央门缝 0.16 m','主门扇厚 0.50 m','主柱轴间 60.00 m'],10,22)
drawings.append(('D02_gate_elevation',d))

d=sheet('D03','纵剖面 / 柱网与空间层次','A3 100% 打印 1:250。沿中轴看向侧墙；中央五跨 + 两端各 6 m 过渡。')
s=1000/250*72/25.4;ox=480;oy=185
rect(d,ox-36*s,oy,72*s,36*s,None,INK);ln(d,ox-36*s,oy+24*s,ox+36*s,oy+24*s,GOLD,1.4)
for z in [-30,-18,-6,6,18,30]:rect(d,ox+(z-.8)*s,oy,1.6*s,24*s,'#D5CAB8');ln(d,ox+z*s,oy+24*s,ox+z*s,oy+36*s,GRAY,.6)
for z in [-24,-12,0,12,24]:
 arch(d,ox+z*s,oy,s,4.9,7.6,4.8,INK,1.5)
 for q in [-4,0,4]:arch(d,ox+(z+q)*s,oy,s,1.4,15.4,2,GRAY,.8)
 rect(d,ox+(z-2.8)*s,oy+18*s,5.6*s,5.2*s,None,BLUE,.8)
ln(d,ox-36*s,oy+13.8*s,ox+36*s,oy+13.8*s,GOLD,1)
for z in [-30,-18,-6,6,18]:dimh(d,ox+z*s,ox+(z+12)*s,oy-27,'12.00',oy)
dimh(d,ox-36*s,ox+36*s,oy-60,'72.00 净长')
dimv(d,ox+37*s,oy,oy+36*s,'36.00')
note(d,80,104,['主柱 12 座 / 主横拱 6 道 / 中央拱顶跨 5 个 / 两侧下廊共 10 跨 / 上廊小拱 30 个'],11)
drawings.append(('D03_longitudinal_section',d))

d=sheet('D04','束柱 / 拱廊模块 / 表面做法','主体 1:150；局部剖面为示意并单独标尺寸，不按主比例量取。')
s=1000/150*72/25.4;ox=185;oy=170
rect(d,ox-1.4*s,oy,2.8*s,1.15*s,'#CDBE9E')
for x in [-.8,-.4,0,.4,.8]:rect(d,ox+(x-.18)*s,oy+1.15*s,.36*s,21.6*s,'#DED6C8')
for y,h in [(1.20,.18),(12.2,.9),(22.75,1.25)]:rect(d,ox-1.35*s,oy+y*s,2.7*s,h*s,GOLD)
dimv(d,ox-55,oy,oy+24*s,'24.00 主柱高');dimh(d,ox-1.4*s,ox+1.4*s,oy-25,'2.80 柱基宽')
bx=480;arch(d,bx,oy,s,4.9,7.6,4.8,INK,2);arch(d,bx,oy,s,4.7,7.6,4.6,GOLD,1)
for x in [-4.9,4.9]:rect(d,bx+(x-.15)*s,oy,.3*s,7.6*s,'#D5CAB8')
dimh(d,bx-4.9*s,bx+4.9*s,oy-25,'9.80 拱净宽');dimv(d,bx+5.8*s,oy,oy+12.4*s,'12.40')
note(d,735,650,['石柱轴身：主芯 Ø1.20 m + 8 × Ø0.40 m 附柱','附柱中心圆半径 0.80 m；整体轴身 Ø2.00 m','主柱头 Y=22.75-24.00；中承托 Y=12.20-13.10','主柱头最大 Ø2.70 m；每层 12 叶，错位 15°','每根柱的石纹方向连续，相邻柱 UV 随机偏移','黄金边宽 40-70 mm，厚 8-15 mm','叶片有卷曲、凹槽、厚度，不是贴片或尖刺'],11,27)
rect(d,740,255,330,80,'#D6CDBB');rect(d,820,326,155,9,GOLD);tx(d,745,229,'金条表面与石面齐平；地面高差 ≤1 mm',11)
ln(d,900,335,900,378,BLUE);tx(d,790,390,'地面金条厚 8 mm / 倒角 1 mm',11,BLUE)
drawings.append(('D04_column_module',d))

def human(d,x,y,s):
 # Dimension silhouette only; armor contour and styling come from master image.
 poly(d,[(x-.25*s,y+1.13*s),(x-.40*s,y+1.65*s),(x-.54*s,y+1.78*s),(x-.20*s,y+1.94*s),(x+.20*s,y+1.94*s),(x+.54*s,y+1.78*s),(x+.4*s,y+1.65*s),(x+.25*s,y+1.13*s)],'#D9D4C7',closed=True)
 rect(d,x-.14*s,y+1.92*s,.28*s,.36*s,'#D9D4C7')
 for side in [-1,1]:
  poly(d,[(x+side*.18*s,y+1.13*s),(x+side*.2*s,y+.62*s),(x+side*.20*s,y+.13*s)],stroke=INK,width=9)
  poly(d,[(x+side*.50*s,y+1.78*s),(x+side*.66*s,y+1.38*s),(x+side*.65*s,y+1.04*s)],stroke=INK,width=6)
 circ(d,x,y+2.20*s,.34*s,None,GOLD,2)
d=sheet('D05','角色 / 展翼包络 / 人体标尺','精确比例示意；羽片、甲片外观以最终黄金翼设定图为准。主体不随场地放大。')
s=150;ox=495;oy=195;human(d,ox,oy,s)
for sign in [-1,1]:
 pts=op.copy();pts[:,0]*=sign;poly(d,[(ox+x*s,oy+y*s) for x,y,z in pts],stroke=GOLD,width=5)
 outline=[(.28,1.78),(1.30,2.18),(2.10,2.75),(2.90,3.10),(2.65,1.0),(1.7,.85),(.55,1.20)]
 poly(d,[(ox+sign*x*s,oy+y*s) for x,y in outline],stroke=GOLD,width=1.3,closed=True)
 for i in range(8):
  u=i/7;ln(d,ox+sign*(.65+2.0*u)*s,oy+(1.95+.97*u)*s,ox+sign*(.65+2.0*u)*s,oy+(.95+.10*u)*s,GOLD,.7)
dimh(d,ox-2.9*s,ox+2.9*s,oy-35,'5.80 完全展翼')
dimh(d,ox-.54*s,ox+.54*s,oy+1.78*s+28,'1.08 装甲肩宽')
dimv(d,1005,oy,oy+3.10*s,'3.10 翼尖最高')
dimv(d,1035,oy,oy+2.28*s,'2.28 盔顶')
tx(d,60,100,'裸身 2.15 / 8.25 头身；盔顶 2.28；破环最高 2.54；每侧 8+6+5=19 枚主羽。',12)
drawings.append(('D05_character_proportions',d))

d=sheet('D06','黄金翼机械绑定 / 收展姿态','骨段长度不变；右侧控制点，左侧镜像。精确局部四元数见 wing_rig.json。')
s=145;oy=205
for ox,pts,title in [(120,op,'展开 / WingOpen = 1'),(720,cl,'收拢 / WingOpen = 0')]:
 poly(d,[(ox+x*s,oy+y*s) for x,y,z in pts],stroke=GOLD,width=6)
 for i,(x,y,z) in enumerate(pts):circ(d,ox+x*s,oy+y*s,5,BLUE,None);tx(d,ox+x*s+9,oy+y*s-4,['root','elbow','wrist','tip'][i],10)
 tx(d,ox,oy-40,title,15)
note(d,80,658,[f'固定骨长：{lens[0]:.4f} / {lens[1]:.4f} / {lens[2]:.4f} m','骨本地 +Y 指向子骨；Z 为世界 +Z 的正交投影。','局部四元数 slerp；不做骨长缩放或平移插值。'],11,24)
note(d,680,658,['收翼：宽 1.65 / 高区间 0.30-2.65 / 深 0.85 m','翼根与肩甲 ≥40 mm；手肘 ≥50 mm；背饰 ≥30 mm','先翼根，再肘，再腕，最后扇开羽组。'],11,24)
note(d,80,105,['root [0,.40]  →  elbow [.15,.70]  →  wrist [.35,.90]  →  fan [.45,1.00]','这些区间相对于 WingOpen；收拢沿同曲线反向，需验证全部中间姿态。'],11,24)
drawings.append(('D06_wing_rig',d))

d=sheet('D07','封印剑 / 鸢盾 / 握持与结构','A3 100% 打印 1:10。正面尺寸为权威基准；侧面厚度另用局部放大示意。')
s=1000/10*72/25.4;ox=220;oy=180
poly(d,[(ox-.0425*s,oy+.44*s),(ox+.0425*s,oy+.44*s),(ox+.032*s,oy+1.40*s),(ox,oy+1.70*s),(ox-.032*s,oy+1.40*s)],'#A6B1B9',closed=True)
rect(d,ox-.27*s,oy+.41*s,.54*s,.06*s,GOLD);rect(d,ox-.022*s,oy+.12*s,.044*s,.32*s,'#5C4433');rect(d,ox-.045*s,oy,.09*s,.12*s,GOLD)
circ(d,ox,oy+.28*s,5,BLUE,None);tx(d,ox+15,oy+.28*s,'右手枢轴：距剑首底 0.28',10,BLUE)
dimv(d,95,oy,oy+1.70*s,'1.70 总长');dimh(d,ox-.27*s,ox+.27*s,oy+.44*s+25,'0.54 护手')
tx(d,350,oy+1.05*s,'刃长 1.26',12);tx(d,350,oy+.88*s,'刃根宽 0.085',11);tx(d,350,oy+.73*s,'中脊厚 0.012',11)
sx=700;sy=210
poly(d,[(sx-.39*s,sy+1.2*s),(sx+.39*s,sy+1.2*s),(sx+.35*s,sy+.72*s),(sx,sy),(sx-.35*s,sy+.72*s)],'#DBD4C1',GOLD,5,True)
for start,end in [(-70,70),(110,250)]:
 poly(d,[(sx+math.cos(a)*.18*s,sy+.78*s+math.sin(a)*.18*s) for a in np.linspace(math.radians(start),math.radians(end),40)],stroke=GOLD,width=3)
for offset in [-.20,0,.20]:ln(d,sx+offset*s,sy+(.15+abs(offset))*s,sx+offset*s,sy+1.12*s,GOLD,1.3)
for u in np.linspace(.14,.40,8):ln(d,ox-.021*s,oy+u*s,ox+.021*s,oy+(u+.015)*s,GOLD,.6)
poly(d,[(ox-.045*s,oy+.025*s),(ox-.025*s,oy),(ox+.025*s,oy),(ox+.045*s,oy+.025*s),(ox+.045*s,oy+.095*s),(ox+.025*s,oy+.12*s),(ox-.025*s,oy+.12*s),(ox-.045*s,oy+.095*s)],GOLD,INK,.6,True)
for side in [-1,1]:
 for k in range(4):
  x=ox+side*(.06+k*.045)*s;y=oy+.44*s;poly(d,[(x,y),(x+side*.02*s,y+.014*s),(x+side*.025*s,y),(x+side*.02*s,y-.014*s),(x,y)],stroke=INK,width=.45)

dimh(d,sx-.39*s,sx+.39*s,sy+1.2*s+30,'0.78 盾宽');dimv(d,850,sy,sy+1.2*s,'1.20 盾高')
rect(d,970,200,25,210,'#D7D0BF');ln(d,1005,220,1030,270,INK,4);ln(d,1005,380,1030,330,INK,4)
note(d,920,610,['盾背放大示意','整体弧深 0.12 m','钢板 4 mm','皮衬 8 mm','独立手握把 + 前臂绑带'],11,25)
tx(d,70,98,'剑局部 +Y 指向剑尖，+Z 为宽面法线；盾局部 +Y 朝盾顶，+Z 朝敌人。',11)
drawings.append(('D07_weapons',d))

d=sheet('D08','羽翼动作曲线 / 冲刺与跳跃攻击','定量动画设计。场景中仍须制作、绑定、播放验收；概念分镜不等于已实现动画。')
def graph(x,y,w,h,duration,action,title):
 rect(d,x,y,w,h,None,GRAY,.7);tx(d,x,y+h+20,title,16)
 for v in [0,.25,.5,.75,1]:ln(d,x,y+v*h,x+w,y+v*h,GRAY,.3);tx(d,x-8,y+v*h-4,str(v),9,GRAY,'end')
 for row in anims:
  a,b,t0,t1,v0,v1,inter,n=row
  if a!=action:continue
  pts=[]
  for t in np.linspace(t0,t1,25):
   q=(t-t0)/(t1-t0);q=q*q*(3-2*q);v=v0+(v1-v0)*q;pts.append((x+t/duration*w,y+v*h))
  poly(d,pts,stroke=GOLD,width=2)
  tx(d,x+t0/duration*w,y-18,f'{t0:g}',9,GRAY)
 tx(d,x+w,y-18,f'{duration:g} s',9,GRAY,'end')
graph(100,465,930,150,.95,'A02','A02 冲刺：0.95 s / 位移 8 m / WingOpen 曲线')
graph(100,195,930,150,1.60,'A03','A03 跳攻：1.60 s / 位移 10 m / 顶点高 4 m')
note(d,100,110,['跳跃脚底：u=(t-0.20)/0.90；Y=16u(1-u)；水平位移=10(3u²-2u³)，仅在离地段生效。','慢走：1.10 s 循环，2.0 m/s，WingOpen=0.08-0.12；剑盾全程保持绑定，不随关键帧消失。'],11,24)
drawings.append(('D08_animation_curves',d))

d=sheet('D09','材质矩阵 / 金属与石材分开控制','色板为 sRGB 底色方向；粗糙度和金属度是 PBR 参数，需在同曝光下比较。')
for i,m in enumerate(mrows):
 col=i//6;row=i%6;x=60+col*555;y=650-row*92
 rect(d,x,y-40,65,55,m['srgb_hex'],None);tx(d,x+80,y,m['id']+'  '+m['name'],15)
 tx(d,x+80,y-22,f"{m['srgb_hex']}  Metallic {m['metallic']}  Roughness {m['roughness_min']:.2f}-{m['roughness_max']:.2f}",10,GRAY)
 tx(d,x+80,y-43,m['note'],10)
drawings.append(('D09_materials',d))

for name,draw in drawings:
 renderSVG.drawToFile(draw,str(DRAW/(name+'.svg')))
 f=DRAW/(name+'.svg');s=f.read_text();s=s.replace("font-family: CN", "font-family: 'Arial Unicode MS', sans-serif");f.write_text(s)

# Illustrated atlas. Crops are clipped views of the unchanged source image, not invented pixels.
atlas=ROOT/'source'/'atlas.pdf';cv=canvas.Canvas(str(atlas),pagesize=(W,H));cv.setTitle('终扉圣堂与黄金翼圣骑士 R3 建模设计图集');cv.setAuthor('IM MONSTER design handoff')
pages=[]
def pagehead(title,sub=''):
 cv.setFillColor(c(PALE));cv.rect(0,0,W,H,fill=1,stroke=0);cv.setFillColor(c(INK));cv.setFont('CN',26);cv.drawString(42,H-58,title);cv.setFont('CN',11);cv.setFillColor(c(GRAY));cv.drawString(42,H-84,sub);cv.setStrokeColor(c(GOLD));cv.line(42,H-102,W-42,H-102)
def finish(title):
 pages.append(title);cv.setFont('CN',9);cv.setFillColor(c(GRAY));cv.drawString(42,25,'R3 / 56 m / 2026-11-07 / 精确尺寸见矢量图和数据文件');cv.drawRightString(W-42,25,f'{len(pages):02d}');cv.showPage()
def imgfit(path,x,y,w,h):
 iw,ih=Image.open(path).size;s=min(w/iw,h/ih);cv.drawImage(str(path),x+(w-iw*s)/2,y+(h-ih*s)/2,iw*s,ih*s,mask='auto')
def para(t,x,y,width,size=12,leading=20):
 st=ParagraphStyle('note',fontName='CN',fontSize=size,leading=leading,textColor=c(INK));p=Paragraph(html.escape(t),st);_,h=p.wrap(width,500);p.drawOn(cv,x,y-h);return h
pagehead('终扉圣堂 · 黄金翼守门骑士','统一建模设计图集 + 完整执行规范 / 场地扩大：长宽各 2 倍 / 逐部分 ≥90 分验收')
imgfit(IMG/'00_reference.png',42,200,1106,520)
para('56 m 战斗直径 · 64 × 72 m 大厅净空 · 36 m 冠顶 · 2.28 m 装甲身高 · 5.80 m 展翼',60,165,1070,17,26)
para('本册将原始概念、设计补全、精确图纸分开呈现。先按数据锁定体量，再按图完成石纹、金属、盔甲与羽翼细节。当前是设计交接包，尚未据此交付新的 56 m Unity 场景或角色网格。',60,118,1070,12,20);finish('封面与执行范围')
pagehead('原图分区 / 建模观察顺序','原始概念图。编号对应下方文字；透视图不承担尺寸测量。')
imgfit(IMG/'00_reference.png',42,215,1106,500)
for n,(x,y) in enumerate([(180,570),(570,660),(770,510),(590,300),(160,310),(870,330)],1):
 cv.setFillColor(c(GOLD));cv.circle(x,y,13,fill=1,stroke=0);cv.setFillColor(colors.white);cv.setFont('CN',12);cv.drawCentredString(x,y-4,str(n))
para('1 束柱与古金柱头  ·  2 多层尖拱与肋拱顶  ·  3 厚重封印门  ·  4 深灰大理石与金属镶圈  ·  5 撤离箱与维修台  ·  6 门侧导流设施',60,165,1070,14,24)
para('生成详图会补出叶脉、背面、铰链和隐藏支架；这些属于本次设计，并不声称能从原图中读取。',60,98,1070,11,20);finish('原图分区')
def crop(path,box,x,y,w,h):
 iw,ih=Image.open(path).size;l,t,r,b=box;cw=(r-l)*iw;ch=(b-t)*ih;s=min(w/cw,h/ch);rw=cw*s;rh=ch*s;px=x+(w-rw)/2;py=y+(h-rh)/2
 cv.saveState();p=cv.beginPath();p.rect(px,py,rw,rh);cv.clipPath(p,stroke=0);cv.drawImage(str(path),px-l*iw*s,py-(1-b)*ih*s,iw*s,ih*s);cv.restoreState()
crops=[('柱头与石纹',(.12,.03,.34,.60),'原图：细长柱束、金属承托带、石材云纹。要求：叶饰有体积，金条镶入线脚。'),('尖拱与穹顶',(.30,0,.72,.31),'原图：不同粗细肋条交叉、尖拱层层后退。要求：主肋、次肋和拱面分件。'),('门与封印',(.44,.14,.64,.61),'原图：暗铁门、破环、横栓与铆钉。要求：扁平金属镶板、门厚与背面完整。'),('地面与金圈',(.28,.62,.78,.96),'原图：深灰石板、轻纹脉和薄金圈。要求：金圈齐平，纹脉不做凸起。'),('撤离角落',(.01,.48,.28,.99),'原图：蓝绳箱、清单、工具台、空托位。要求：保持人用尺度与正在撤离的状态。'),('骑士原始尺度',(.54,.48,.60,.63),'原图中的角色像素很少。最终盔甲背面、羽翼与动作均为本次补全设计。')]
for k in range(0,6,2):
 pagehead('原图局部放大 / '+str(k//2+1),'直接显示未改动原图的局部；放大不会产生新的真实细节。')
 for j,(title,box,desc) in enumerate(crops[k:k+2]):
  x=48+j*565;crop(IMG/'00_reference.png',box,x,225,535,475);para(title,x,195,535,18,25);para(desc,x,153,535,12,21)
 finish('原图局部 '+str(k//2+1))
js('source_crops.json',[dict(title=a,normalized_box_left_top_right_bottom=b,interpretation=d) for a,b,d in crops])
for name,draw in drawings[:4]:renderPDF.draw(draw,cv,0,0);pages.append(name);cv.showPage()
def imagepage(file,title,sub,notes):
 pagehead(title,sub);imgfit(IMG/file,42,165,1106,560);para(notes,55,133,1080,12,21);finish(title)
imagepage('01_architecture_details.png','建筑精细设计 / 石、金属、门与地面','AI 设计展开图；与原始局部放大分开使用。','柱头要有连续承托带与双层叶饰；大理石纹脉进入石料内部；黄金有实际厚度、倒角和粗糙度差异。门、柱、拱具体数值以 D02-D04 为准。')
imagepage('03_evacuation_props.png','外围道具 / 转运、维修与导流','道具保持人的使用尺寸；不得随大厅尺度翻倍。','主箱 1.25 × 0.72 × 0.78 m；工作台 1.80 × 0.75 × 0.90 m；蓝绳 Ø18 mm；双导管 Ø100 mm。原点与实例位置见 module_placements.csv。')
renderPDF.draw(drawings[8][1],cv,0,0);pages.append('D09_materials');cv.showPage()
imagepage('07_winged_knight_master.png','最终角色 / 黄金双翼圣骑士','唯一最终角色外观基准。正面/背面展开；侧面/正面收拢。','保持象牙甲、旧金包边、封闭面甲与破环背饰。躯干尺寸以 D05 为准；展开 5.80 m，收拢 1.65 m。生成图各视图的缩放不同，不能从图中量取翼展比例。')
renderPDF.draw(drawings[4][1],cv,0,0);pages.append('D05_character_proportions');cv.showPage()
imagepage('08_wings_armor_detail.png','翼根 / 甲片 / 黄金羽片分层','主架、铰链、背部承重板与羽片分别建模。','每侧固定 8 枚初级 + 6 枚次级 + 5 枚覆羽；插画中的片数不作计数依据。改用腰下两条后摆，保留翼根工作空间；背饰与羽翼独立安装。')
for idx in [5,6]:renderPDF.draw(drawings[idx][1],cv,0,0);pages.append(drawings[idx][0]);cv.showPage()
imagepage('09_walk_dash_storyboard.png','动作分镜 / 慢走收翼，冲刺展开','上排：慢走接触、支撑、离地；下排：冲刺蓄力、位移、制动。','图中空手用于看清身体与翼根动作；实际交付必须全程持右剑左盾。慢走 WingOpen 0.08-0.12；冲刺总长 0.95 s，有效位移 8 m，峰值展开 0.90。')
imagepage('10_jump_attack_storyboard.png','动作分镜 / 起跳、展翼、下劈与回收','六阶段：蓄力 → 起跳 → 顶点 → 下劈 → 落地 → 收翼。','总长 1.60 s、水平跨越 10 m、脚底顶点 4 m；落地时刻 1.10 s。前两格剑未完整画出，实际剑盾全程绑定；盾徽以 D07 为准。禁止据插画省略装备或做瞬间切换。')
renderPDF.draw(drawings[7][1],cv,0,0);pages.append('D08_animation_curves');cv.showPage()
pagehead('逐部分 ≥90 / 交付验收','不能用整体平均分补偿某个未完成部件。先过硬条件，再评外观。')
for x,title,body in [(65,'静态部件 / 100 分','轮廓结构 25\n尺寸比例 25\n关键细节 20\n材质 20\n位置连接 10'),(440,'动作 / 100 分','关键姿态 25\n时间位移 25\n羽翼状态 25\n装备/布料/接触 25'),(800,'硬条件 / 任一失败返工','直径 56.0±0.1 m\n角色与翼展误差 ≤1%\n主要模块数与羽片数正确\n中央无障碍 / 无持续穿插\n慢走、冲刺、跳攻实播通过')]:
 para(title,x,650,310,21,28)
 for i,line in enumerate(body.split('\n')):para(line,x,585-i*47,310,15,24)
para('提交模型后，由评审按统一光照和相机对照，每项附证据及扣分原因。验收 CSV 当前留空并标记 NOT_EVALUATED；本次设计包没有替尚未制作的模型宣称达到 90 分。',65,240,1060,14,24)
para('后续搭建顺序：数据白模 → 比例验收 → 建筑模块 → 材质/照明 → 角色/羽翼 → 绑定动作 → 引擎接入 → 逐项验收。',65,138,1060,13,23);finish('验收规则')
imagepage('04_knight_turnaround.png','设计历史 / 无翼初版（废止）','仅归档。不得用本页覆盖最终黄金翼角色、背部支架或布料方案。','保留此图以完整记录本轮概念过程。最终角色使用第 07 图及 W01 数据；初版的宽肩披风不再执行。')
cv.save();js('atlas_pages.json',[{'page':i+1,'title':p} for i,p in enumerate(pages)])

# Full unabridged specification appended as searchable A4 pages.
styles={'body':ParagraphStyle('body',fontName='CN',fontSize=10,leading=16,spaceAfter=8,wordWrap='CJK'),
 'h1':ParagraphStyle('h1',fontName='CN',fontSize=20,leading=28,spaceBefore=12,spaceAfter=12),
 'h2':ParagraphStyle('h2',fontName='CN',fontSize=15,leading=23,spaceBefore=15,spaceAfter=9,keepWithNext=True),
 'h3':ParagraphStyle('h3',fontName='CN',fontSize=12,leading=19,spaceBefore=12,spaceAfter=7,keepWithNext=True)}
story=[];lines=brief.read_text().splitlines();i=0
while i<len(lines):
 line=lines[i].strip();i+=1
 if not line:continue
 if line.startswith('|'):
  tab=[]
  while True:
   if not re.match(r'^\|[-|: ]+\|$',line):tab.append([Paragraph(html.escape(v.strip()),styles['body']) for v in line.strip('|').split('|')])
   if i>=len(lines) or not lines[i].strip().startswith('|'):break
   line=lines[i].strip();i+=1
  widths=[75,75,70,287] if len(tab[0])==4 else None;t=Table(tab,colWidths=widths,repeatRows=1,hAlign='LEFT');t.setStyle(TableStyle([('BACKGROUND',(0,0),(-1,0),c('#EEE9DE')),('GRID',(0,0),(-1,-1),.4,c('#BFB8A8')),('VALIGN',(0,0),(-1,-1),'TOP'),('LEFTPADDING',(0,0),(-1,-1),6),('RIGHTPADDING',(0,0),(-1,-1),6)]));story.append(t);story.append(Spacer(1,10));continue
 level=len(line)-len(line.lstrip('#'))
 style=styles['h'+str(min(level,3))] if level else styles['body']
 text=line[level:].strip() if level else line;text=re.sub(r'\*\*(.*?)\*\*',r'\1',text);text=text.replace('`','')
 story.append(Paragraph(html.escape(text),style))
def specfooter(can,doc):
 can.setFont('CN',8);can.setFillColor(c(GRAY));can.drawString(44,23,'R3 完整执行规范 / 与 A3 图集共同使用');can.drawRightString(A4[0]-44,23,f'规范 {doc.page}')
SimpleDocTemplate(str(ROOT/'source/spec.pdf'),pagesize=A4,leftMargin=44,rightMargin=44,topMargin=40,bottomMargin=42).build(story,onFirstPage=specfooter,onLaterPages=specfooter)
merged=fitz.open();merged.insert_pdf(fitz.open(atlas));merged.insert_pdf(fitz.open(ROOT/'source/spec.pdf'));merged.set_metadata({'title':'终扉圣堂与黄金翼圣骑士_R3_56m建模交接','author':'IM MONSTER design handoff'});merged.set_toc([[1,'设计图集',1],[1,'完整执行规范',len(pages)+1]])
out=ROOT/'终扉圣堂_黄金翼圣骑士_56m_完整建模交接.pdf';merged.save(out,garbage=4,deflate=True);merged.close()
pdf=fitz.open(out)
for i in range(len(pages)):
 page=pdf[i];page.get_pixmap(matrix=fitz.Matrix(.70,.70),alpha=False).save(QA/f'page_{i+1:02d}.png')
for i in range(len(pages),len(pdf)):
 pdf[i].get_pixmap(matrix=fitz.Matrix(.65,.65),alpha=False).save(QA/f'page_{i+1:02d}.png')
for name,draw in drawings:
 idx=pages.index(name);pdf[idx].get_pixmap(matrix=fitz.Matrix(1.6,1.6),alpha=False).save(DRAW/(name+'.png'))
manifest=[]
for f in sorted(IMG.glob('*.png')):
 status='superseded_history' if f.name.startswith('04') else 'original_source' if f.name.startswith('00') else 'current_design_reference'
 manifest.append({'file':'images/'+f.name,'status':status,'sha256':hashlib.sha256(f.read_bytes()).hexdigest(),'size_px':list(Image.open(f).size)})
js('image_manifest.json',manifest)
checks={'hall_ratio':[16,18,9],'arena_area_factor':4,'column_instances':sum(r['module']=='bundled_pier' for r in rows),'transverse_arches':sum(r['module']=='transverse_arch' for r in rows),'placement_count':len(rows),'rig_length_error':float(np.max(np.abs(np.linalg.norm(np.diff(cl,axis=0),axis=1)-lens))),'gate_leaf_sum':2*7.92+.16,'sword_component_sum':1.26+.32+.12,'jump_apex_t':.65,'jump_apex_y':4,'jump_distance':10,'pdf_pages':len(pdf),'atlas_pages':len(pages),'downstream_model_acceptance':'NOT_EVALUATED'}
assert checks['column_instances']==12 and checks['transverse_arches']==6 and checks['rig_length_error']<1e-8
assert abs(checks['gate_leaf_sum']-16)<1e-8 and abs(checks['sword_component_sum']-1.7)<1e-8
js('design_validation.json',checks)
print(json.dumps(checks,ensure_ascii=False));print(out)
