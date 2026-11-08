import bpy
from pathlib import Path
from mathutils import Vector
P=Path(__file__).resolve().parents[2]
bpy.ops.wm.open_mainfile(filepath=str(P/'Blender/SanctuaryR3/Sanctuary_R3_Entry.blend'))
s=bpy.context.scene
for o in s.objects:
 if o.name.startswith('E07_PointArchSpandrel'):o.modifiers[0].offset=-1
 if o.name.startswith('E05_EndTympanum') and min(v.co.y for v in o.data.vertices)<0:
  for v in o.data.vertices:v.co.y=-36
# Save before modifying the evidence camera so the native file still opens to the north gate.
bpy.ops.wm.save_as_mainfile(filepath=str(P/'Blender/SanctuaryR3/Sanctuary_R3_Entry.blend'))
cam=s.camera;cam.location=(0,12,4);cam.rotation_euler=(Vector((0,-36,10))-cam.location).to_track_quat('-Z','Y').to_euler();cam.data.lens=26
s.render.filepath=str(P/'Docs/SanctuaryR3Build/SouthEntry_Preview.png');bpy.ops.render.render(write_still=True)
print('SOUTH_CLOSURE_CORRECTED')
