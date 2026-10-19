"""Render the carved entrance and a cutaway of the new library space."""

import bpy
from mathutils import Vector
from pathlib import Path

project = Path(__file__).resolve().parents[2]
output = project / "Blender"
bpy.ops.wm.open_mainfile(filepath=str(output / "WhiteCastle_Hollow_Working.blend"))
scene = bpy.context.scene
camera = scene.camera

scene.render.engine = "BLENDER_WORKBENCH"
scene.display.shading.color_type = "OBJECT"
scene.display.shading.show_cavity = True
scene.render.resolution_x = 1500
scene.render.resolution_y = 1000
scene.render.resolution_percentage = 100

# Eye-level diagonal view toward the new hall. Hide the outside landscape and
# ornament only for this structural preview; the saved .blend keeps them.
for name in ("15.003", "Details.001", "DarkRock_G2.033", "Plane"):
    bpy.data.objects[name].hide_render = True
camera.data.type = "PERSP"
camera.data.lens = 16
camera.location = (24, 168, 88)
camera.rotation_euler = (Vector((-15, 184, 82)) - camera.location).to_track_quat("-Z", "Y").to_euler()
scene.render.filepath = str(output / "white_castle_hollow_from_door.png")
bpy.ops.render.render(write_still=True)

# Lift the room lid for a structural overview without altering the saved model.
for name in ("WhiteCastle_Hollow_Core", "15.003", "Details.001", "DarkRock_G2.033", "Plane"):
    bpy.data.objects[name].hide_render = True
for name in ("Library ceiling", "Library front wall left", "Library front wall right", "Library doorway lintel"):
    bpy.data.objects[name].hide_render = True
camera.data.type = "ORTHO"
camera.data.ortho_scale = 140
camera.location = (110, 50, 210)
camera.rotation_euler = (Vector((0, 168, 100)) - camera.location).to_track_quat("-Z", "Y").to_euler()
scene.render.filepath = str(output / "white_castle_hollow_cutaway.png")
bpy.ops.render.render(write_still=True)

# Exterior check: the distinctive facade should remain recognizable.
for obj in bpy.data.objects:
    if obj.type == "MESH" and obj.name != "Cube.032":
        obj.hide_render = False
camera.data.type = "ORTHO"
camera.data.ortho_scale = 390
camera.location = (0, -310, 145)
camera.rotation_euler = (Vector((0, 80, 95)) - camera.location).to_track_quat("-Z", "Y").to_euler()
scene.render.filepath = str(output / "white_castle_hollow_exterior.png")
bpy.ops.render.render(write_still=True)
