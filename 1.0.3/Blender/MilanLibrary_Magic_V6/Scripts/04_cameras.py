import bpy,runpy,json
from pathlib import Path
from mathutils import Vector
P=Path(__file__).resolve().parents[1];N=runpy.run_path(str(P/'Scripts/common.py'));s=bpy.data.scenes['MilanLibrary_Final'];bpy.context.window.scene=s
C=N['col']('V6_Inspection_Cameras',s.collection)
def camera(name,location,target,lens):
 o=bpy.data.objects.get(name)
 if not o:d=bpy.data.cameras.new(name);o=bpy.data.objects.new(name,d);C.objects.link(o)
 o.location=location;o.rotation_euler=(Vector(target)-o.location).to_track_quat('-Z','Y').to_euler();o.data.lens=lens;o.data.clip_end=200
camera('CAM_V6_Overview',(0,7.8,2.7),(0,28,6.8),12)
camera('CAM_V6_Workzones',(0,8.6,5.1),(0,14.7,1.6),12)
camera('CAM_V6_Upload',(-1.8,10.2,3.65),(-5.8,14.13,2.4),40)
camera('CAM_V6_Search',(1.8,10.2,3.65),(5.8,14.13,2.35),40)
camera('CAM_V6_ShelfMagic',(0,21.6,4.25),(-5,25.5,4.98),38)
selected=json.loads((P/'Audit/shelf_magic.json').read_text())['selected_spines'];names=[n for a in selected for n in a]
bs=[bpy.data.objects[n] for n in names if bpy.data.objects[n].location.x<-10 and 14.6<bpy.data.objects[n].location.y<17.1 and 1.5<bpy.data.objects[n].location.z<2.7]
b=bs[len(bs)//2];p=b.matrix_world.translation;camera('CAM_V6_BookSpines',(-8.8,p.y-.65,p.z+.60),(-11.18,p.y,p.z+.22),48)
s.frame_set(35);s.camera=bpy.data.objects['CAM_V6_Overview'];s.render.resolution_x=1600;s.render.resolution_y=1000;s.render.resolution_percentage=100;s.cycles.samples=64
N['save']('V6_visual_review_cameras')
print('Focus book',b.name)
