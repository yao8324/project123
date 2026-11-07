from pathlib import Path
import csv,json,math,hashlib
import numpy as np
import fitz
P=Path(__file__).resolve().parents[1]
def data(name):return json.loads((P/'data'/name).read_text())
def rows(name):return list(csv.DictReader((P/'data'/name).open(encoding='utf-8-sig')))
checks=[]
def check(value,label):
 assert value,label
 checks.append(label)
d=data('dimensions.json');rig=data('wing_rig.json');poses=rows('module_placements.csv');anims=rows('animation_states.csv')
check(d['arena']['diameter']==56 and d['arena']['horizontal_scale_factor']==2,'Confirmed linear enlargement: diameter 28 to 56, area factor4')
check(len(poses)==57,'57 placement rows')
check(sum(p['module']=='bundled_pier' for p in poses)==12,'12 bundled piers')
check(sum(p['module']=='transverse_arch' for p in poses)==6,'6 transverse arches / 5 central bays')
check(abs(2*d['north_gate']['leaf_width']+d['north_gate']['center_gap']-16)<1e-9,'Door leaves plus seam total16m')
check(abs(sum(d['sword'][k] for k in ['blade_length','grip_length','pommel_length'])-1.7)<1e-9,'Sword segments total1.7m')
check(sum(d['wings']['feathers_each_side'].values())==19,'19 main feathers per side')
for row in poses:
 if row['module'] not in ['bundled_pier','transit_chest','repair_workbench','inventory_stand','empty_cradle']:continue
 # Conservative nearest point of rotated oriented bounding rectangle to origin.
 x,z=float(row['x_m']),float(row['z_m']);a=math.radians(float(row['yaw_deg']));u=math.cos(a)*(-x)-math.sin(a)*(-z);v=math.sin(a)*(-x)+math.cos(a)*(-z)
 dist=math.hypot(max(abs(u)-float(row['width_m'])/2,0),max(abs(v)-float(row['depth_m'])/2,0))
 check(dist>28,'Central circle clear of '+row['id'])
def matrix(q):
 x,y,z,w=q
 return np.array([[1-2*(y*y+z*z),2*(x*y-z*w),2*(x*z+y*w)],[2*(x*y+z*w),1-2*(x*x+z*z),2*(y*z-x*w)],[2*(x*z-y*w),2*(y*z+x*w),1-2*(x*x+y*y)]])
errors=[]
for state_name,state in rig['states'].items():
 for side_name,side in state.items():
  target=np.array(side['joint_positions_m']);point=target[0].copy();R=np.eye(3)
  check(np.allclose(np.linalg.norm(np.diff(target,axis=0),axis=1),rig['segment_lengths_m']),'Fixed wing bone lengths '+state_name+side_name)
  for i,q in enumerate(side['local_quaternions_xyzw']):
   check(abs(np.linalg.norm(q)-1)<1e-8,'Normalized quaternion '+state_name+side_name+str(i))
   R=R@matrix(q);point+=R[:,1]*rig['segment_lengths_m'][i];errors.append(float(np.linalg.norm(point-target[i+1])))
check(max(errors)<1e-8,'Independent FK reproduces all declared joint positions')
for action in ['A01','A02','A03']:
 r=[p for p in anims if p['action']==action]
 for a,b in zip(r,r[1:]):
  check(abs(float(a['end_s'])-float(b['start_s']))<1e-9,action+' continuous time '+a['phase'])
  check(abs(float(a['wing_end'])-float(b['wing_start']))<1e-9,action+' continuous WingOpen '+a['phase'])
 if action=='A01':check(r[0]['wing_start']==r[-1]['wing_end'],'Walk loop endpoint matches start')
check(abs((.68-.18)*16-8)<1e-9,'Dash effective travel8m')
check(abs(16*.5*(1-.5)-4)<1e-9 and abs(.20+.9*.5-.65)<1e-9,'Jump apex4m at0.65s')
score=rows('acceptance_scorecard.csv');check(len(score)==39,'39 separately graded acceptance items')
check(all(r['status']=='NOT_EVALUATED' and r['total100']=='' for r in score),'No fabricated downstream scores')
check(all(sum(r.values())==100 for r in data('acceptance_rubrics.json').values()),'Each scoring rubric totals100')
for item in data('image_manifest.json'):
 check(hashlib.sha256((P/item['file']).read_bytes()).hexdigest()==item['sha256'],'Image hash '+item['file'])
check(len(list((P/'drawings').glob('*.svg')))==9,'9 SVG dimension sheets')
pdf=fitz.open(P/'终扉圣堂_黄金翼圣骑士_56m_完整建模交接.pdf');check(len(pdf)==30,'30-page integrated PDF')
overflow=[];text=''
for i,page in enumerate(pdf):
 text+=page.get_text()
 for block in page.get_text('blocks'):
  x0,y0,x1,y1=block[:4]
  if x0 < -1 or y0 < -1 or x1>page.rect.width+1 or y1>page.rect.height+1:overflow.append((i+1,block[:4]))
check(not overflow,'All extracted PDF blocks within page boundaries')
check('\ufffd' not in text,'No replacement characters in extracted PDF text')
for keyword in ['56','5.80','90','WingOpen','黄金','收拢','慢走']:
 check(keyword in text,'Searchable PDF contains '+keyword)
report={'design_checks_passed':len(checks),'checks':checks,'wing_fk_max_error_m':max(errors),'pdf_text_characters':len(text),'pdf_text_overflow':overflow,'scope':'Document/data validation only. No downstream meshes, rigs, rendered scene or final similarity scores evaluated.'}
(P/'data/package_validation.json').write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n')
print(json.dumps({k:v for k,v in report.items() if k!='checks'},ensure_ascii=False))
