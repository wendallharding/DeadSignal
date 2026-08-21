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
    "SecurityWardenArmorAlbedo.png",
)
FBX_PATH = os.path.join(
    PROJECT_DIRECTORY,
    "Assets",
    "DeadSignal",
    "Resources",
    "Actors",
    "SecurityWardenModel.fbx",
)
BLEND_PATH = os.path.join(SCRIPT_DIRECTORY, "SecurityWarden.blend")
PREVIEW_PATH = os.path.join(SCRIPT_DIRECTORY, "SecurityWardenPreview.png")


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
        shader.inputs["Emission Strength"].default_value = 4.5
    return material


def create_armor_material():
    material = create_material("Warden Armor Albedo", (0.13, 0.15, 0.18), metallic=0.48, roughness=0.36)
    image = bpy.data.images.load(TEXTURE_PATH, check_existing=True)
    image.colorspace_settings.name = "sRGB"
    texture = material.node_tree.nodes.new("ShaderNodeTexImage")
    texture.image = image
    texture.interpolation = "Linear"
    shader = material.node_tree.nodes.get("Principled BSDF")
    material.node_tree.links.new(texture.outputs["Color"], shader.inputs["Base Color"])
    return material


def add_beveled_box(name, location, dimensions, bevel=0.06, rotation=(0.0, 0.0, 0.0)):
    bpy.ops.mesh.primitive_cube_add(location=location, rotation=rotation)
    obj = bpy.context.object
    obj.name = name
    obj.dimensions = dimensions
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    modifier = obj.modifiers.new("Armor chamfers", "BEVEL")
    modifier.width = bevel
    modifier.segments = 2
    modifier.limit_method = "ANGLE"
    bpy.context.view_layer.objects.active = obj
    bpy.ops.object.modifier_apply(modifier=modifier.name)
    return obj


def add_cylinder(name, location, radius, depth, vertices=12, bevel=0.02):
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices, radius=radius, depth=depth, location=location)
    obj = bpy.context.object
    obj.name = name
    if bevel > 0.0:
        modifier = obj.modifiers.new("Armor chamfers", "BEVEL")
        modifier.width = bevel
        modifier.segments = 2
        modifier.limit_method = "ANGLE"
        bpy.context.view_layer.objects.active = obj
        bpy.ops.object.modifier_apply(modifier=modifier.name)
    return obj


def join_parts(parts, name, origin, material, smart_uv=True, smooth=False):
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
        bpy.ops.uv.smart_project(angle_limit=math.radians(64.0), island_margin=0.035)
        bpy.ops.object.mode_set(mode="OBJECT")
    obj.data.materials.clear()
    obj.data.materials.append(material)
    for polygon in obj.data.polygons:
        polygon.use_smooth = smooth
    return obj


def build_chassis(material):
    parts = [
        add_beveled_box("Armor citadel", (0.0, 0.0, 0.39), (0.82, 0.86, 0.40), 0.095),
        add_beveled_box("Left ram plate", (-0.52, 0.02, 0.38), (0.30, 0.76, 0.34), 0.07,
                         rotation=(0.0, 0.0, math.radians(-5.0))),
        add_beveled_box("Right ram plate", (0.52, 0.02, 0.38), (0.30, 0.76, 0.34), 0.07,
                         rotation=(0.0, 0.0, math.radians(5.0))),
        add_beveled_box("Front glacis", (0.0, 0.43, 0.36), (0.70, 0.28, 0.27), 0.06),
        add_beveled_box("Rear left drive", (-0.31, -0.45, 0.34), (0.28, 0.30, 0.25), 0.055,
                         rotation=(0.0, 0.0, math.radians(-13.0))),
        add_beveled_box("Rear right drive", (0.31, -0.45, 0.34), (0.28, 0.30, 0.25), 0.055,
                         rotation=(0.0, 0.0, math.radians(13.0))),
        add_beveled_box("Upper armor", (0.0, -0.02, 0.61), (0.62, 0.64, 0.10), 0.035),
    ]
    return join_parts(parts, "Warden Chassis", (0.0, 0.0, 0.38), material)


def build_eye(material):
    parts = [
        add_beveled_box("Sensor aperture", (0.0, 0.585, 0.49), (0.68, 0.075, 0.13), 0.025),
        add_beveled_box("Sensor left cap", (-0.37, 0.57, 0.49), (0.10, 0.12, 0.17), 0.025),
        add_beveled_box("Sensor right cap", (0.37, 0.57, 0.49), (0.10, 0.12, 0.17), 0.025),
    ]
    return join_parts(parts, "Warden Eye", (0.0, 0.59, 0.48), material)


def build_crown(material):
    parts = [
        add_cylinder("Crown base", (0.0, -0.02, 0.755), 0.34, 0.12, vertices=10, bevel=0.025),
        add_cylinder("Crown cap", (0.0, -0.02, 0.835), 0.22, 0.055, vertices=8, bevel=0.015),
        add_beveled_box("Crown fin north", (0.0, 0.28, 0.79), (0.13, 0.22, 0.09), 0.02),
        add_beveled_box("Crown fin south", (0.0, -0.32, 0.79), (0.13, 0.20, 0.09), 0.02),
        add_beveled_box("Crown fin west", (-0.30, -0.02, 0.79), (0.20, 0.13, 0.09), 0.02),
        add_beveled_box("Crown fin east", (0.30, -0.02, 0.79), (0.20, 0.13, 0.09), 0.02),
    ]
    return join_parts(parts, "Warden Crown", (0.0, 0.0, 0.76), material)


def add_preview_stage():
    bpy.ops.mesh.primitive_plane_add(size=14.0, location=(0.0, 0.0, -0.02))
    stage = bpy.context.object
    stage.name = "Preview Deck"
    stage.data.materials.append(create_material("Preview Deck Material", (0.012, 0.02, 0.03), metallic=0.35, roughness=0.7))

    world = bpy.context.scene.world
    world.color = (0.003, 0.004, 0.007)
    world.use_nodes = True
    world.node_tree.nodes["Background"].inputs["Color"].default_value = (0.003, 0.004, 0.008, 1.0)
    world.node_tree.nodes["Background"].inputs["Strength"].default_value = 0.15

    bpy.ops.object.light_add(type="AREA", location=(-2.5, 3.4, 5.3))
    key = bpy.context.object
    key.name = "Preview Key"
    key.data.energy = 780.0
    key.data.shape = "DISK"
    key.data.size = 3.5
    key.data.color = (0.48, 0.58, 0.72)

    bpy.ops.object.light_add(type="AREA", location=(3.1, 1.2, 2.7))
    rim = bpy.context.object
    rim.name = "Preview Threat Rim"
    rim.data.energy = 650.0
    rim.data.size = 2.8
    rim.data.color = (1.0, 0.02, 0.01)

    bpy.ops.object.camera_add(location=(2.7, 4.0, 3.1))
    camera = bpy.context.object
    camera.name = "Preview Camera"
    camera.data.lens = 58.0
    direction = Vector((0.0, 0.08, 0.4)) - camera.location
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
    armor_material = create_armor_material()
    eye_material = create_material(
        "Warden Eye Crimson", (0.55, 0.005, 0.008), metallic=0.2, roughness=0.2, emission=(1.0, 0.005, 0.01)
    )
    crown_material = create_material(
        "Warden Crown Crimson", (0.12, 0.004, 0.006), metallic=0.55, roughness=0.3, emission=(0.055, 0.0, 0.002)
    )

    model_objects = [
        build_chassis(armor_material),
        build_eye(eye_material),
        build_crown(crown_material),
    ]
    export_model(model_objects)
    add_preview_stage()
    bpy.ops.wm.save_as_mainfile(filepath=BLEND_PATH)
    render_preview()


if __name__ == "__main__":
    main()
