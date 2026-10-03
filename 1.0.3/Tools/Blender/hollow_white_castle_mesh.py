"""Cut a library hall in the castle core while preserving the imported source."""

import bmesh
import bpy
import json
from mathutils import Matrix, Vector
from pathlib import Path

project = Path(__file__).resolve().parents[2]
source = project / "Assets/_WhiteCastle/Meshes/Test2.fbx"
output = project / "Blender"
output.mkdir(exist_ok=True)

bpy.ops.object.select_all(action="SELECT")
bpy.ops.object.delete(use_global=False)
bpy.ops.import_scene.fbx(filepath=str(source))

# FBX stores paths from its original Windows authoring machine. Reconnect the
# images supplied by the Unity package using paths relative to the project.
textures = project / "Assets/_WhiteCastle/Textures"
image_aliases = {
    "TexturesCom_Rock_CliffCoastal5_1K_albedo.png": "Rock_CliffCoastal5_1K_albedo.png",
    "Map #41.001": "DarkRockDesert_Volume123_copy000_color.png",
    "Map #41.002": "DarkRockDesert_Volume123_copy000_color.png",
    "Map #41.003": "DarkRockDesert_Volume123_copy000_color.png",
    "Map #42.001": "Rocks_Volume123_copy000_gloss.png",
    "Map #43.001": "Rocks_Volume123_copy000_nmap.png",
}
relinked_images = []
unavailable_images = []
for image in bpy.data.images:
    if image.name in {"Render Result", "Viewer Node"}:
        continue
    candidate = textures / image_aliases.get(image.name, image.name)
    if not candidate.is_file():
        unavailable_images.append(image.name)
        continue
    image.filepath = str(candidate)
    image.reload()
    image.filepath = "//../Assets/_WhiteCastle/Textures/" + candidate.name
    relinked_images.append(image.name)

core = bpy.data.objects["Cube.032"]
original_faces = len(core.data.polygons)
source_collection = bpy.data.collections.new("SOURCE - original core")
bpy.context.scene.collection.children.link(source_collection)
source_collection.objects.link(core)
for collection in list(core.users_collection):
    if collection != source_collection:
        collection.objects.unlink(core)
core.hide_render = True
core.hide_set(True)

hall = core.copy()
hall.data = core.data.copy()
hall.name = "WhiteCastle_Hollow_Core"
bpy.context.scene.collection.objects.link(hall)
hall.data.transform(hall.matrix_world)
hall.matrix_world = Matrix.Identity(4)
hall.hide_render = False
hall.hide_set(False)

mesh = bmesh.new()
mesh.from_mesh(hall.data)

# Split at cavity edges so faces can be removed without tearing far into the
# outside facade. BMesh interpolates the original UVs on split faces.
planes = [
    (0, -38), (0, 38), (1, 165), (1, 190), (2, 78.5), (2, 136),
    (0, -8), (0, 8), (1, 139), (1, 169), (2, 105),
]
for axis, value in planes:
    normal = [0.0, 0.0, 0.0]
    normal[axis] = 1.0
    bmesh.ops.bisect_plane(
        mesh,
        geom=list(mesh.verts) + list(mesh.edges) + list(mesh.faces),
        dist=0.0001,
        plane_co=Vector(tuple(value if i == axis else 0.0 for i in range(3))),
        plane_no=Vector(normal),
        clear_inner=False,
        clear_outer=False,
    )


def within(point, bounds):
    return all(low + 0.0001 < point[i] < high - 0.0001 for i, (low, high) in enumerate(bounds))


room = ((-38, 38), (165, 190), (78.5, 136))
doorway = ((-8, 8), (139, 169), (78.5, 105))
remove = [face for face in mesh.faces if within(face.calc_center_median(), room) or within(face.calc_center_median(), doorway)]
removed_faces = len(remove)
bmesh.ops.delete(mesh, geom=remove, context="FACES")
mesh.to_mesh(hall.data)
mesh.free()
hall.data.update()

# The original package supplies ornate external detail but no finished
# traversable interior. These plain structural surfaces provide a clean volume
# for later shelves, stairs, lights, and Gothic interior detailing.
interior = bpy.data.collections.new("NEW - library room shell")
bpy.context.scene.collection.children.link(interior)


def slab(name, center, size):
    bpy.ops.mesh.primitive_cube_add(size=2, location=center)
    obj = bpy.context.object
    obj.name = name
    obj.dimensions = size
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    for collection in list(obj.users_collection):
        collection.objects.unlink(obj)
    interior.objects.link(obj)
    obj.color = (0.46, 0.56, 0.68, 1)
    return obj


slab("Library floor", (0, 177.5, 79.4), (76, 25, 0.3))
slab("Library ceiling", (0, 177.5, 136.2), (76, 25, 0.4))
slab("Library left wall", (-38, 177.5, 107.7), (0.4, 25, 57))
slab("Library right wall", (38, 177.5, 107.7), (0.4, 25, 57))
slab("Library back wall", (0, 190, 107.7), (76, 0.4, 57))
slab("Library front wall left", (-23, 165, 107.7), (30, 0.4, 57))
slab("Library front wall right", (23, 165, 107.7), (30, 0.4, 57))
slab("Library doorway lintel", (0, 165, 120.5), (16, 0.4, 31.4))


def hit_distance(obj, start, direction):
    to_local = obj.matrix_world.inverted()
    p = to_local @ Vector(start)
    d = (to_local.to_3x3() @ Vector(direction)).normalized()
    hit, position, _normal, _face = obj.ray_cast(p, d)
    return round(((obj.matrix_world @ position) - Vector(start)).length, 3) if hit else None


ray_checks = {
    "toward_front_before": hit_distance(core, (0, 170, 100), (0, -1, 0)),
    "toward_front_after": hit_distance(hall, (0, 170, 100), (0, -1, 0)),
    "toward_right_before": hit_distance(core, (0, 170, 100), (1, 0, 0)),
    "toward_right_after": hit_distance(hall, (0, 170, 100), (1, 0, 0)),
}

report = {
    "source": str(source.relative_to(project)),
    "core_original_polygons": original_faces,
    "core_hollow_polygons": len(hall.data.polygons),
    "faces_removed_inside_cavity": removed_faces,
    "hall_bounds": room,
    "doorway_bounds": doorway,
    "ray_checks": ray_checks,
    "relinked_images": relinked_images,
    "unavailable_imported_images": unavailable_images,
}
(output / "white_castle_hollow_report.json").write_text(json.dumps(report, indent=2))
print("REPORT", json.dumps(report))

scene = bpy.context.scene
scene.render.engine = "BLENDER_WORKBENCH"
scene.display.shading.light = "STUDIO"
scene.display.shading.color_type = "OBJECT"
scene.display.shading.show_cavity = True
scene.display.shading.cavity_type = "BOTH"
scene.display.shading.show_shadows = True
scene.render.resolution_x = 1500
scene.render.resolution_y = 1000
scene.render.resolution_percentage = 100
scene.render.image_settings.file_format = "PNG"

for obj in bpy.data.objects:
    if obj.type == "MESH" and obj.name not in interior.objects:
        obj.color = {
            "WhiteCastle_Hollow_Core": (0.65, 0.79, 0.92, 1),
            "15.003": (0.8, 0.8, 0.76, 1),
            "DarkRock_G2.033": (0.35, 0.32, 0.3, 1),
            "Plane": (0.38, 0.46, 0.38, 1),
        }.get(obj.name, (0.82, 0.73, 0.46, 1))

bpy.ops.object.camera_add()
camera = bpy.context.object
camera.name = "Interior preview camera"
scene.camera = camera
camera.data.lens = 16
camera.location = (24, 168, 88)
camera.rotation_euler = (Vector((-15, 184, 82)) - camera.location).to_track_quat("-Z", "Y").to_euler()

# The structural view hides detail to show the new opening clearly.
decoration = bpy.data.objects["15.003"]
decoration.hide_render = True
scene.render.filepath = str(output / "white_castle_hollow_interior.png")
bpy.ops.render.render(write_still=True)
decoration.hide_render = False

bpy.ops.object.select_all(action="DESELECT")
hall.select_set(True)
bpy.context.view_layer.objects.active = hall
for screen in bpy.data.screens:
    for area in screen.areas:
        if area.type != "VIEW_3D":
            continue
        space = area.spaces.active
        space.shading.type = "SOLID"
        space.shading.color_type = "OBJECT"
        space.shading.show_cavity = True
        space.region_3d.view_location = (0, 140, 95)
        space.region_3d.view_distance = 300
        space.region_3d.view_rotation = (
            Vector((0, 140, 95)) - Vector((220, -180, 210))
        ).to_track_quat("-Z", "Y")
bpy.ops.wm.save_as_mainfile(filepath=str(output / "WhiteCastle_Hollow_Working.blend"))
print("SAVED", output / "WhiteCastle_Hollow_Working.blend")
