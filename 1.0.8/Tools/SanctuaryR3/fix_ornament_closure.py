import bpy,math
from pathlib import Path
P=Path(__file__).resolve().parents[2]
bpy.ops.wm.open_mainfile(filepath=str(P/'Blender/SanctuaryR3/Sanctuary_R3_Ornament.blend'))
for o in bpy.context.scene.objects:
 if o.name.startswith('E05_EndTympanum'):
  for v in o.data.vertices:v.co.y=37.2 if v.co.y>0 else -36
# Update native scene and matching wide evidence after the occlusion correction.
bpy.ops.wm.save_as_mainfile(filepath=str(P/'Blender/SanctuaryR3/Sanctuary_R3_Ornament.blend'))
s=bpy.context.scene;s.render.filepath=str(P/'Docs/SanctuaryR3Build/Ornament_Preview.png');bpy.ops.render.render(write_still=True)
