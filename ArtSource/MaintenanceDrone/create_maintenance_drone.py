import math
import os

import bpy
from mathutils import Vector


SCRIPT_DIRECTORY = os.path.dirname(os.path.abspath(__file__))
PROJECT_DIRECTORY = os.path.abspath(os.path.join(SCRIPT_DIRECTORY, "..", ".."))
TEXTURE_PATH = os.path.join(
    PROJECT_DIRECTORY,
    "Assets",
    "DeadSignal",
    "Resources",
    "Actors",
    "MaintenanceDroneHullAlbedo.png",
)
FBX_PATH = os.path.join(
    PROJECT_DIRECTORY,
    "Assets",
    "DeadSignal",
    "Resources",
    "Actors",
    "MaintenanceDroneModel.fbx",
)
BLEND_PATH = os.path.join(SCRIPT_DIRECTORY, "MaintenanceDrone.blend")
PREVIEW_PATH = os.path.join(SCRIPT_DIRECTORY, "MaintenanceDronePreview.png")


def clear_scene():
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete(use_global=False)
    for datablocks in (bpy.data.meshes, bpy.data.curves, bpy.data.materials, bpy.data.cameras, bpy.data.lights):
        for datablock in list(datablocks):
            if datablock.users == 0:
                datablocks.remove(datablock)


def create_material(name, color, metallic=0.0, roughness=0.5, emission=None):
    material = bpy.data.materials.new(name)
    material.diffuse_color = (*color, 1.0)
    material.use_nodes = True
    shader = material.node_tree.nodes.get("Principled BSDF")
    shader.inputs["Base Color"].default_value = (*color, 1.0)
    shader.inputs["Metallic"].default_value = metallic
    shader.inputs["Roughness"].default_value = roughness
    if emission is not None:
        shader.inputs["Emission Color"].default_value = (*emission, 1.0)
        shader.inputs["Emission Strength"].default_value = 4.0
    return material


def create_hull_material():
    material = create_material("Drone Hull Albedo", (0.84, 0.82, 0.73), metallic=0.12, roughness=0.42)
    image = bpy.data.images.load(TEXTURE_PATH, check_existing=True)
    image.colorspace_settings.name = "sRGB"
    nodes = material.node_tree.nodes
    texture = nodes.new("ShaderNodeTexImage")
    texture.image = image
    texture.interpolation = "Linear"
    shader = nodes.get("Principled BSDF")
    material.node_tree.links.new(texture.outputs["Color"], shader.inputs["Base Color"])
    return material


def add_beveled_box(name, location, dimensions, bevel=0.06, rotation=(0.0, 0.0, 0.0)):
    bpy.ops.mesh.primitive_cube_add(location=location, rotation=rotation)
    obj = bpy.context.object
    obj.name = name
    obj.dimensions = dimensions
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    modifier = obj.modifiers.new("Edge chamfers", "BEVEL")
    modifier.width = bevel
    modifier.segments = 2
    modifier.limit_method = "ANGLE"
    bpy.context.view_layer.objects.active = obj
    bpy.ops.object.modifier_apply(modifier=modifier.name)
    return obj


def add_cylinder(name, location, radius, depth, vertices=12, rotation=(0.0, 0.0, 0.0), bevel=0.02):
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices, radius=radius, depth=depth, location=location, rotation=rotation)
    obj = bpy.context.object
    obj.name = name
    if bevel > 0.0:
        modifier = obj.modifiers.new("Edge chamfers", "BEVEL")
        modifier.width = bevel
        modifier.segments = 2
        modifier.limit_method = "ANGLE"
        bpy.context.view_layer.objects.active = obj
        bpy.ops.object.modifier_apply(modifier=modifier.name)
    return obj


def join_parts(parts, name, origin, material, smart_uv=True):
    bpy.ops.object.select_all(action="DESELECT")
    for part in parts:
        part.select_set(True)
    bpy.context.view_layer.objects.active = parts[0]
    bpy.ops.object.join()
    obj = bpy.context.object
    obj.name = name
    bpy.context.scene.cursor.location = origin
    bpy.ops.object.origin_set(type="ORIGIN_CURSOR")
    if smart_uv:
        bpy.ops.object.mode_set(mode="EDIT")
        bpy.ops.mesh.select_all(action="SELECT")
        bpy.ops.uv.smart_project(angle_limit=math.radians(66.0), island_margin=0.035)
        bpy.ops.object.mode_set(mode="OBJECT")
    obj.data.materials.clear()
    obj.data.materials.append(material)
    for polygon in obj.data.polygons:
        polygon.use_smooth = False
    return obj


def build_chassis(material):
    parts = [
        add_beveled_box("Hull center", (0.0, 0.02, 0.29), (0.86, 0.78, 0.25), 0.09),
        add_beveled_box("Hull left pod", (-0.51, 0.04, 0.28), (0.27, 0.62, 0.19), 0.055,
                         rotation=(0.0, 0.0, math.radians(-7.0))),
        add_beveled_box("Hull right pod", (0.51, 0.04, 0.28), (0.27, 0.62, 0.19), 0.055,
                         rotation=(0.0, 0.0, math.radians(7.0))),
        add_beveled_box("Hull rear left", (-0.33, 0.42, 0.255), (0.24, 0.30, 0.13), 0.045,
                         rotation=(0.0, 0.0, math.radians(-18.0))),
        add_beveled_box("Hull rear right", (0.33, 0.42, 0.255), (0.24, 0.30, 0.13), 0.045,
                         rotation=(0.0, 0.0, math.radians(18.0))),
        add_beveled_box("Tool collar", (0.0, -0.39, 0.29), (0.38, 0.22, 0.20), 0.045),
    ]
    return join_parts(parts, "Drone Chassis", (0.0, 0.0, 0.28), material)


def build_signal_ring(material):
    bpy.ops.mesh.primitive_torus_add(
        align="WORLD",
        major_segments=16,
        minor_segments=6,
        location=(0.0, 0.02, 0.445),
        major_radius=0.345,
        minor_radius=0.042,
    )
    ring = bpy.context.object
    ring.name = "Drone Signal Ring"
    bpy.context.scene.cursor.location = (0.0, 0.0, 0.42)
    bpy.ops.object.origin_set(type="ORIGIN_CURSOR")
    ring.data.materials.append(material)
    for polygon in ring.data.polygons:
        polygon.use_smooth = True
    return ring


def build_core(material):
    parts = [
        add_cylinder("Core housing", (0.0, 0.02, 0.515), 0.205, 0.13, vertices=10, bevel=0.025),
        add_cylinder("Core cap", (0.0, 0.02, 0.59), 0.12, 0.045, vertices=8, bevel=0.012),
        add_beveled_box("Core status slot", (0.0, -0.155, 0.545), (0.18, 0.045, 0.055), 0.012),
    ]
    return join_parts(parts, "Drone Core", (0.0, 0.0, 0.50), material)


def build_tool(material):
    parts = [
        add_beveled_box("Emitter body", (0.0, -0.67, 0.31), (0.22, 0.50, 0.17), 0.045),
        add_beveled_box("Emitter left brace", (-0.15, -0.56, 0.295), (0.09, 0.28, 0.11), 0.025,
                         rotation=(0.0, 0.0, math.radians(-12.0))),
        add_beveled_box("Emitter right brace", (0.15, -0.56, 0.295), (0.09, 0.28, 0.11), 0.025,
                         rotation=(0.0, 0.0, math.radians(12.0))),
        add_cylinder("Emitter muzzle", (0.0, -0.93, 0.31), 0.115, 0.10, vertices=10,
                     rotation=(math.radians(90.0), 0.0, 0.0), bevel=0.012),
    ]
    return join_parts(parts, "Drone Tool", (0.0, -0.68, 0.30), material)


def add_preview_stage():
    bpy.ops.mesh.primitive_plane_add(size=14.0, location=(0.0, 0.0, -0.02))
    stage = bpy.context.object
    stage.name = "Preview Deck"
    stage.data.materials.append(create_material("Preview Deck Material", (0.012, 0.02, 0.03), metallic=0.35, roughness=0.7))

    world = bpy.context.scene.world
    world.color = (0.003, 0.006, 0.012)
    world.use_nodes = True
    world.node_tree.nodes["Background"].inputs["Color"].default_value = (0.002, 0.006, 0.012, 1.0)
    world.node_tree.nodes["Background"].inputs["Strength"].default_value = 0.2

    bpy.ops.object.light_add(type="AREA", location=(-2.6, -3.2, 5.5))
    key = bpy.context.object
    key.name = "Preview Key"
    key.data.energy = 850.0
    key.data.shape = "DISK"
    key.data.size = 4.0
    key.data.color = (0.55, 0.75, 1.0)

    bpy.ops.object.light_add(type="AREA", location=(3.0, 1.5, 3.0))
    fill = bpy.context.object
    fill.name = "Preview Rim"
    fill.data.energy = 550.0
    fill.data.size = 3.0
    fill.data.color = (0.1, 1.0, 1.0)

    bpy.ops.object.camera_add(location=(2.65, -3.7, 3.2))
    camera = bpy.context.object
    camera.name = "Preview Camera"
    camera.data.lens = 58.0
    direction = Vector((0.0, -0.12, 0.3)) - camera.location
    camera.rotation_euler = direction.to_track_quat("-Z", "Y").to_euler()
    bpy.context.scene.camera = camera


def render_preview():
    scene = bpy.context.scene
    scene.render.engine = "BLENDER_EEVEE"
    scene.render.resolution_x = 1024
    scene.render.resolution_y = 1024
    scene.render.resolution_percentage = 100
    scene.render.image_settings.file_format = "PNG"
    scene.render.filepath = PREVIEW_PATH
    scene.render.film_transparent = False
    scene.render.image_settings.color_mode = "RGBA"
    bpy.ops.render.render(write_still=True)


def export_model(model_objects):
    bpy.ops.object.select_all(action="DESELECT")
    for obj in model_objects:
        obj.select_set(True)
    bpy.context.view_layer.objects.active = model_objects[0]
    bpy.ops.export_scene.fbx(
        filepath=FBX_PATH,
        use_selection=True,
        apply_unit_scale=True,
        apply_scale_options="FBX_SCALE_UNITS",
        bake_space_transform=False,
        object_types={"MESH"},
        use_mesh_modifiers=True,
        mesh_smooth_type="FACE",
        add_leaf_bones=False,
        axis_forward="-Z",
        axis_up="Y",
        path_mode="STRIP",
        embed_textures=False,
    )


def main():
    clear_scene()
    hull_material = create_hull_material()
    signal_material = create_material(
        "Signal Cyan", (0.01, 0.55, 0.65), metallic=0.1, roughness=0.22, emission=(0.0, 0.8, 1.0)
    )
    core_material = create_material("Core Graphite", (0.025, 0.035, 0.045), metallic=0.65, roughness=0.28)
    tool_material = create_material(
        "Tool Cyan", (0.02, 0.32, 0.38), metallic=0.35, roughness=0.24, emission=(0.0, 0.5, 0.65)
    )

    model_objects = [
        build_chassis(hull_material),
        build_signal_ring(signal_material),
        build_core(core_material),
        build_tool(tool_material),
    ]
    export_model(model_objects)
    add_preview_stage()
    bpy.ops.wm.save_as_mainfile(filepath=BLEND_PATH)
    render_preview()


if __name__ == "__main__":
    main()
