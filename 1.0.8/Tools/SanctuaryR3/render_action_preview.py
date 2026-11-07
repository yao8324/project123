"""Continuous 30 fps silhouette review, not a final material render or Unity recording."""
import bpy,math,json,subprocess
from pathlib import Path
from mathutils import Vector
P=Path(__file__).resolve().parents[2];bpy.ops.wm.open_mainfile(filepath=str(P/'Blender/SanctuaryR3/Paladin_R3_Animated.blend'))
s=bpy.context.scene;rig=bpy.data.objects['Paladin_R3_Rig'];out=P/'Docs/SanctuaryR3Build/ActionPreviewR6';out.mkdir(exist_ok=True)
s.render.engine='BLENDER_WORKBENCH';s.render.resolution_x=800;s.render.resolution_y=800;s.render.resolution_percentage=100;s.render.image_settings.file_format='PNG';s.render.film_transparent=False
sh=s.display.shading;sh.light='STUDIO';sh.color_type='MATERIAL';sh.show_shadows=True;sh.show_cavity=True;sh.cavity_type='BOTH';sh.background_type='WORLD';s.world.color=(.12,.14,.18)
for m in bpy.data.materials:
 if 'Gold' in m.name:m.diffuse_color=(.65,.40,.13,1)
 elif 'Ivory' in m.name:m.diffuse_color=(.72,.70,.62,1)
 elif 'Steel' in m.name:m.diffuse_color=(.13,.15,.18,1)
report=[]
for name,duration in [('A01_SlowWalk',1.1),('A02_Dash',.95),('A03_JumpAttack',1.6)]:
 rig.animation_data.action=bpy.data.actions[name];folder=out/name;folder.mkdir(exist_ok=True);count=math.ceil(duration*30)+1
 for k in range(count):
  t=min(duration,k/30);frame=1+t*60;s.frame_set(int(frame),subframe=frame-int(frame));bpy.context.view_layer.update();pos=rig.pose.bones['Root'].matrix.translation;focus=Vector((0,pos.y,1.5+pos.z));cam=s.camera;cam.data.type='ORTHO';cam.data.ortho_scale=6.7;cam.location=focus+Vector((4,8,2));cam.rotation_euler=(focus-cam.location).to_track_quat('-Z','Y').to_euler();s.render.filepath=str(folder/f'{k:04}.png');bpy.ops.render.render(write_still=True)
 subprocess.run(['/opt/homebrew/bin/ffmpeg','-hide_banner','-loglevel','error','-y','-framerate','30','-i',str(folder/'%04d.png'),'-c:v','libx264','-pix_fmt','yuv420p','-crf','18',str(out/(name+'.mp4'))],check=True)
 report.append({'clip':name,'frames':count,'fps':30,'duration_authoring_s':duration,'camera':'actor-tracking; root movement must be reviewed separately','render':'Workbench geometry preview, not final materials'})
(out/'manifest.json').write_text(json.dumps(report,indent=2));print('ACTION_PREVIEW_COMPLETE')
