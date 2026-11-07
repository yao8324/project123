"""Render current delivered armor without saving changes to the model."""
import bpy
from pathlib import Path
from mathutils import Vector
P=Path(__file__).resolve().parents[2]
bpy.ops.wm.open_mainfile(filepath=str(P/'Blender/SanctuaryR3/Paladin_R3_Animated.blend'))
s=bpy.context.scene;r=bpy.data.objects['Paladin_R3_Rig'];r.data.pose_position='REST'
for o in s.objects:
 if o.name.startswith(('W01_','W02_','S01_','S02_','R01_R8_')):o.hide_render=True
s.cycles.samples=24;s.render.resolution_x=1000;s.render.resolution_y=1400;s.render.resolution_percentage=100
s.camera.data.type='ORTHO';s.camera.data.ortho_scale=2.55
s.camera.location=(3,6,1.6);s.camera.rotation_euler=(Vector((0,0,1.18))-s.camera.location).to_track_quat('-Z','Y').to_euler()
s.render.filepath=str(P/'Docs/SanctuaryR3Build/Knight_Body_Current_R12.png');bpy.ops.render.render(write_still=True)
