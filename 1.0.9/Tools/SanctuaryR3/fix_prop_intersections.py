import bpy
from pathlib import Path
from mathutils import Vector
P=Path(__file__).resolve().parents[2];bpy.ops.wm.open_mainfile(filepath=str(P/'Blender/SanctuaryR3/Sanctuary_R3_Props.blend'));s=bpy.context.scene
for o in s.objects:
 if o.name.startswith('P03_Post'):o.location.y=-.10
 if o.name.startswith('P02_DrapedLinen'):
  for v in o.data.vertices:v.co.z+=.019
 if o.type=='EMPTY' and o.name.startswith(('P01_','P02_','P03_','P04_','P06_')):o['front_facing_hall']=True
bpy.ops.wm.save_as_mainfile(filepath=str(P/'Blender/SanctuaryR3/Sanctuary_R3_Props.blend'))
cam=s.camera;cam.location=(-21,-24,3.1);cam.rotation_euler=(Vector((-25,-28.6,.8))-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.lens=42;s.render.filepath=str(P/'Docs/SanctuaryR3Build/Props_Detail.png');bpy.ops.render.render(write_still=True)
print('PROP_INTERSECTIONS_CORRECTED')
