import bpy,json
from pathlib import Path
from mathutils import Vector
from mathutils.bvhtree import BVHTree
P=Path(__file__).resolve().parents[2];bpy.ops.wm.open_mainfile(filepath=str(P/'Blender/SanctuaryR3/Paladin_R3_Animated.blend'));s=bpy.context.scene;r=bpy.data.objects['Paladin_R3_Rig'];r.animation_data.action=bpy.data.actions['A02_Dash'];s.frame_set(25);bpy.context.view_layer.update()
for o in s.objects:
 if o.name.startswith(('W01_','W02_','S01_','S02_','R01_R8_')):o.hide_render=True
root=r.pose.bones['Root'].matrix.translation
s.cycles.samples=20;s.render.resolution_x=1000;s.render.resolution_y=1200;s.render.resolution_percentage=100;s.camera.data.type='ORTHO';s.camera.data.ortho_scale=2.6;s.camera.location=root+Vector((3,6,1.6));s.camera.rotation_euler=(root+Vector((0,0,1.18))-s.camera.location).to_track_quat('-Z','Y').to_euler();s.render.filepath=str(P/'Docs/SanctuaryR3Build/Knight_DashCloth_R17.png');bpy.ops.render.render(write_still=True)
