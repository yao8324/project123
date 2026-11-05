"""Rebuild editable Blender sources from the V5 Unity authoring manifests.

Run with Blender's bundled Python, e.g. blender -b --python this_file -- <project>.
All input and output remains in the same Unity project. No reference media is copied.
"""
import json
import math
import sys
from pathlib import Path

import bpy
from mathutils import Matrix, Vector


PROJECT = Path(sys.argv[sys.argv.index("--") + 1]).resolve()
OUTPUT = PROJECT / "Blender" / "KnightProductionV5"
# Unity Y-up coordinates -> Blender Z-up; the same basis is used for mesh and rig.
BASIS = Matrix(((1, 0, 0, 0), (0, 0, -1, 0), (0, 1, 0, 0), (0, 0, 0, 1)))


def matrix(values):
    return Matrix(tuple(tuple(values[row * 4 + col] for col in range(4)) for row in range(4)))


def material(name, color):
    existing = bpy.data.materials.get(name)
    if existing:
        return existing
    result = bpy.data.materials.new(name)
    result.use_nodes = True
    shader = result.node_tree.nodes.get("Principled BSDF")
    shader.inputs["Base Color"].default_value = tuple(max(0, min(1, v)) for v in color[:3]) + (1,)
    metal = any(token in name.lower() for token in ("silver", "steel", "gold", "brass"))
    shader.inputs["Metallic"].default_value = 0.72 if metal else 0.04
    shader.inputs["Roughness"].default_value = 0.52 if metal else 0.72
    if "Light" in name or "Emission" in name:
        shader.inputs["Emission Color"].default_value = tuple(color[:3]) + (1,)
        shader.inputs["Emission Strength"].default_value = 0.3
    texture_name = "StoneGrain.png" if "Stone" in name or "Limestone" in name else "MetalGrain.png"
    texture_path = PROJECT / "Assets/Art/Bosses/Knight/ProductionV5/Textures" / texture_name
    stone_surface = "Stone" in name or "Limestone" in name
    if stone_surface:
        texture_path = PROJECT / "Assets/Art/MilanLibraryV6/Textures/IV4_Interior_AgedLimestone_BaseColor.png"
    if texture_path.exists():
        image = bpy.data.images.load(str(texture_path), check_existing=True)
        image.pack()
        node = result.node_tree.nodes.new("ShaderNodeTexImage")
        node.image = image
        multiply = result.node_tree.nodes.new("ShaderNodeMixRGB")
        multiply.blend_type = "MULTIPLY"
        multiply.inputs[0].default_value = 1
        multiply.inputs[1].default_value = tuple(max(0, min(1, v)) for v in color[:3]) + (1,)
        result.node_tree.links.new(node.outputs["Color"], multiply.inputs[2])
        result.node_tree.links.new(multiply.outputs["Color"], shader.inputs["Base Color"])
    if stone_surface:
        normal_path = PROJECT / "Assets/Art/MilanLibraryV6/Textures/IV4_Interior_AgedLimestone_Normal.png"
        if normal_path.exists():
            normal_image = bpy.data.images.load(str(normal_path), check_existing=True)
            normal_image.colorspace_settings.name = "Non-Color"
            normal_image.pack()
            texture_node = result.node_tree.nodes.new("ShaderNodeTexImage")
            texture_node.image = normal_image
            normal_node = result.node_tree.nodes.new("ShaderNodeNormalMap")
            normal_node.inputs["Strength"].default_value = 0.32
            result.node_tree.links.new(texture_node.outputs["Color"], normal_node.inputs["Color"])
            result.node_tree.links.new(normal_node.outputs["Normal"], shader.inputs["Normal"])
    return result


def clear():
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete(use_global=False)
    for datablock in list(bpy.data.meshes):
        if datablock.users == 0:
            bpy.data.meshes.remove(datablock)


def make_rig(data):
    records = {item["name"]: item for item in data["bones"]}
    paths = {mesh["bone"] for mesh in data["meshes"]}
    paths.update(path for path in records if "/Ribbon_" in path)
    for path in list(paths):
        parent = records.get(path, {}).get("parent")
        seen = {path}
        while parent is not None and parent in records and parent not in seen:
            seen.add(parent)
            paths.add(parent)
            parent = records[parent].get("parent")
    names = {path: f"b{i:03d}_{(path.rsplit('/', 1)[-1] or 'Root').replace(' ', '_')[:40]}" for i, path in enumerate(sorted(paths))}
    rig_data = bpy.data.armatures.new("SealedKnight_EditableSkeleton")
    rig = bpy.data.objects.new("SealedKnight_Rig", rig_data)
    bpy.context.collection.objects.link(rig)
    bpy.context.view_layer.objects.active = rig
    rig.select_set(True)
    bpy.ops.object.mode_set(mode="EDIT")
    positions = {path: (BASIS @ matrix(records[path]["matrix"])).translation for path in paths}
    for path in sorted(paths):
        bone = rig_data.edit_bones.new(names[path])
        bone.head = positions[path]
        children = [p for p in paths if records[p].get("parent") == path and (positions[p] - bone.head).length > 0.005]
        children.sort(key=lambda p: ("Ribbon_" not in p, "joint" not in p, len(p)))
        bone.tail = positions[children[0]] if children else bone.head + Vector((0, 0, 0.08))
        if (bone.tail - bone.head).length < 0.005:
            bone.tail = bone.head + Vector((0, 0, 0.08))
        bone.use_deform = True
    for path in sorted(paths):
        parent = records[path].get("parent")
        if parent in names and parent != path:
            rig_data.edit_bones[names[path]].parent = rig_data.edit_bones[names[parent]]
    bpy.ops.object.mode_set(mode="OBJECT")
    for path, name in names.items():
        rig_data.bones[name]["unity_path"] = path
    rig.show_in_front = True
    rig.select_set(False)
    return rig, names, positions


def build(kind):
    clear()
    with (OUTPUT / f"{kind}Source.json").open(encoding="utf-8") as stream:
        data = json.load(stream)
    rig, names, positions = make_rig(data) if kind == "Knight" else (None, {}, {})
    for number, item in enumerate(data["meshes"]):
        transform = BASIS @ matrix(item["matrix"])
        local_vertices = [Vector(item["vertices"][i:i + 3]) for i in range(0, len(item["vertices"]), 3)]
        vertices = [transform @ vertex for vertex in local_vertices]
        faces = [tuple(item["triangles"][i:i + 3]) for i in range(0, len(item["triangles"]), 3)]
        mesh = bpy.data.meshes.new(f"{kind}_{number:03d}_Mesh")
        mesh.from_pydata(vertices, [], faces)
        mesh.validate(clean_customdata=False)
        mesh.update()
        obj = bpy.data.objects.new(item["name"].rsplit("/", 1)[-1][:50] + f"_{number:03d}", mesh)
        bpy.context.collection.objects.link(obj)
        obj["unity_path"] = item["name"]
        mesh.materials.append(material(item["material"], item["color"]))
        if len(item["uv"]) == len(local_vertices) * 2:
            uv_layer = mesh.uv_layers.new(name="UVMap")
            for loop in mesh.loops:
                index = loop.vertex_index * 2
                uv_layer.data[loop.index].uv = item["uv"][index:index + 2]
        if rig:
            candidates = [path for path in names if path == item["bone"] or ("Flowing carved seal ribbon" in item["name"] and path.startswith(item["bone"] + "/Ribbon_"))]
            if not candidates:
                candidates = [""] if "" in names else [next(iter(names))]
            groups = {path: obj.vertex_groups.new(name=names[path]) for path in candidates}
            for vertex_index, vertex in enumerate(vertices):
                closest = sorted(candidates, key=lambda path: (positions[path] - vertex).length_squared)[:2]
                if len(closest) == 1:
                    groups[closest[0]].add([vertex_index], 1, "REPLACE")
                else:
                    a, b = ((positions[path] - vertex).length for path in closest)
                    weight = b / max(0.000001, a + b)
                    groups[closest[0]].add([vertex_index], weight, "REPLACE")
                    groups[closest[1]].add([vertex_index], 1 - weight, "REPLACE")
            modifier = obj.modifiers.new("Editable bone skin", "ARMATURE")
            modifier.object = rig
            obj.parent = rig
        if "Suspended oath blade" in item["name"] or "Folded wrist oath ward" in item["name"]:
            obj.hide_render = True
            obj.hide_set(True)
    scene = bpy.context.scene
    scene.unit_settings.system = "METRIC"
    scene.unit_settings.scale_length = 1
    scene["source"] = "Original V5 Unity authoring meshes and explicit skeleton; no reference-video geometry."
    scene["runtime"] = "Unity uses ProductionV5 prefab and mesh assets. This .blend is the editable modelling source, not an automatic FBX replacement."
    scene.render.engine = "CYCLES"
    scene.cycles.samples = 32
    scene.world.color = (0.045, 0.045, 0.055)
    used_materials = {slot.material for obj in scene.objects if obj.type == "MESH" for slot in obj.material_slots}
    for unused_material in list(bpy.data.materials):
        if unused_material not in used_materials:
            bpy.data.materials.remove(unused_material, do_unlink=True)
    bpy.context.preferences.filepaths.save_version = 0
    bpy.ops.wm.save_as_mainfile(filepath=str(OUTPUT / f"{kind}_Editable.blend"))
    return dict(kind=kind, objects=len(scene.objects), meshObjects=sum(obj.type == "MESH" for obj in scene.objects), bones=len(rig.data.bones) if rig else 0)


OUTPUT.mkdir(parents=True, exist_ok=True)
reports = [build("Knight"), build("Forecourt")]
with (OUTPUT / "SourceValidation.json").open("w", encoding="utf-8") as stream:
    json.dump(dict(blender=bpy.app.version_string, sources=reports), stream, ensure_ascii=False, indent=2)
print("KNIGHT_V5_EDITABLE_SOURCES_COMPLETE", json.dumps(reports))
