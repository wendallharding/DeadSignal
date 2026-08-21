import math
import os

import bpy
from mathutils import Vector


SCRIPT_DIRECTORY = os.path.dirname(os.path.abspath(__file__))
PROJECT_DIRECTORY = os.path.abspath(os.path.join(SCRIPT_DIRECTORY, "..", ".."))
TEXTURE_PATH = os.path.join(
    PROJECT_DIRECTORY, "Assets", "DeadSignal", "Resources", "Environment", "WardenBayAlbedo.png"
)
FBX_PATH = os.path.join(
    PROJECT_DIRECTORY, "Assets", "DeadSignal", "Resources", "Environment", "SecurityBlastShieldModel.fbx"
)
BLEND_PATH = os.path.join(SCRIPT_DIRECTORY, "SecurityBlastShield.blend")
PREVIEW_PATH = os.path.join(SCRIPT_DIRECTORY, "SecurityBlastShieldPreview.png")


def clear_scene():
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete(use_global=False)


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
        shader.inputs["Emission Strength"].default_value = 2.3
    return material


def create_armor_material():
    material = create_material("Security Shield Armor", (0.12, 0.12, 0.13), metallic=0.5, roughness=0.38)
    image = bpy.data.images.load(TEXTURE_PATH, check_existing=True)
    image.colorspace_settings.name = "sRGB"
    texture = material.node_tree.nodes.new("ShaderNodeTexImage")
    texture.image = image
    shader = material.node_tree.nodes.get("Principled BSDF")
    material.node_tree.links.new(texture.outputs["Color"], shader.inputs["Base Color"])
    return material


def add_box(name, location, dimensions, bevel_width=0.04):
    bpy.ops.mesh.primitive_cube_add(location=location)
    obj = bpy.context.object
    obj.name = name
    obj.dimensions = dimensions
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    bevel = obj.modifiers.new("Security edge bevel", "BEVEL")
    bevel.width = bevel_width
    bevel.segments = 2
    bevel.limit_method = "ANGLE"
    bpy.context.view_layer.objects.active = obj
    bpy.ops.object.modifier_apply(modifier=bevel.name)
    return obj


def join_parts(parts, name, material):
    bpy.ops.object.select_all(action="DESELECT")
    for part in parts:
        part.select_set(True)
    bpy.context.view_layer.objects.active = parts[0]
    bpy.ops.object.join()
    obj = bpy.context.object
    obj.name = name
    bpy.context.scene.cursor.location = (0.0, 0.0, 0.0)
    bpy.ops.object.origin_set(type="ORIGIN_CURSOR")
    bpy.ops.object.mode_set(mode="EDIT")
    bpy.ops.mesh.select_all(action="SELECT")
    bpy.ops.uv.smart_project(angle_limit=math.radians(58.0), island_margin=0.035)
    bpy.ops.object.mode_set(mode="OBJECT")
    obj.data.materials.clear()
    obj.data.materials.append(material)
    return obj


def build_armor(material):
    parts = [
        add_box("Shield core", (0.0, 0.0, 0.7), (3.0, 0.72, 1.28), 0.09),
        add_box("Shield plinth", (0.0, 0.0, 0.14), (3.2, 0.9, 0.25), 0.055),
        add_box("Left buttress", (-1.28, 0.0, 0.45), (0.45, 0.92, 0.8), 0.06),
        add_box("Right buttress", (1.28, 0.0, 0.45), (0.45, 0.92, 0.8), 0.06),
    ]
    return join_parts(parts, "Security Shield Armor", material)


def build_braces(material):
    parts = [
        add_box("Upper ceramic brace", (0.0, -0.42, 1.12), (2.45, 0.12, 0.18), 0.035),
        add_box("Lower ceramic brace", (0.0, -0.42, 0.38), (2.45, 0.12, 0.18), 0.035),
        add_box("Left ceramic plate", (-1.2, -0.42, 0.74), (0.28, 0.12, 0.62), 0.04),
        add_box("Right ceramic plate", (1.2, -0.42, 0.74), (0.28, 0.12, 0.62), 0.04),
    ]
    return join_parts(parts, "Security Shield Braces", material)


def build_warning_lenses(material):
    parts = [
        add_box("Warning lens left", (-0.78, -0.5, 0.74), (0.34, 0.1, 0.46), 0.035),
        add_box("Warning lens center", (0.0, -0.5, 0.74), (0.34, 0.1, 0.46), 0.035),
        add_box("Warning lens right", (0.78, -0.5, 0.74), (0.34, 0.1, 0.46), 0.035),
    ]
    return join_parts(parts, "Security Shield Warning Lenses", material)


def export_model(objects):
    bpy.ops.object.select_all(action="DESELECT")
    for obj in objects:
        obj.select_set(True)
    bpy.context.view_layer.objects.active = objects[0]
    bpy.ops.export_scene.fbx(
        filepath=FBX_PATH,
        use_selection=True,
        apply_unit_scale=True,
        apply_scale_options="FBX_SCALE_UNITS",
        object_types={"MESH"},
        use_mesh_modifiers=True,
        mesh_smooth_type="FACE",
        add_leaf_bones=False,
        axis_forward="-Z",
        axis_up="Y",
        path_mode="STRIP",
        embed_textures=False,
    )


def add_preview_stage():
    world = bpy.context.scene.world
    world.use_nodes = True
    world.node_tree.nodes["Background"].inputs["Color"].default_value = (0.002, 0.002, 0.003, 1.0)
    world.node_tree.nodes["Background"].inputs["Strength"].default_value = 0.12
    bpy.ops.object.light_add(type="AREA", location=(-3.7, -4.5, 5.5))
    bpy.context.object.data.energy = 880.0
    bpy.context.object.data.size = 4.2
    bpy.context.object.data.color = (0.76, 0.75, 0.72)
    bpy.ops.object.light_add(type="AREA", location=(3.3, 1.2, 3.2))
    bpy.context.object.data.energy = 500.0
    bpy.context.object.data.size = 3.0
    bpy.context.object.data.color = (1.0, 0.02, 0.03)
    bpy.ops.object.camera_add(location=(5.2, -7.4, 5.3))
    camera = bpy.context.object
    camera.rotation_euler = (Vector((0.0, 0.0, 0.65)) - camera.location).to_track_quat("-Z", "Y").to_euler()
    camera.data.lens = 58.0
    bpy.context.scene.camera = camera


def render_preview():
    scene = bpy.context.scene
    scene.render.engine = "BLENDER_EEVEE"
    scene.render.resolution_x = 1280
    scene.render.resolution_y = 720
    scene.render.resolution_percentage = 100
    scene.render.image_settings.file_format = "PNG"
    scene.render.filepath = PREVIEW_PATH
    bpy.ops.render.render(write_still=True)


def main():
    clear_scene()
    armor = create_armor_material()
    braces = create_material("Security Shield Braces", (0.76, 0.74, 0.68), metallic=0.1, roughness=0.44)
    warnings = create_material(
        "Security Shield Warning Lenses", (0.68, 0.01, 0.02), metallic=0.08, roughness=0.18, emission=(0.86, 0.01, 0.02)
    )
    objects = [build_armor(armor), build_braces(braces), build_warning_lenses(warnings)]
    export_model(objects)
    add_preview_stage()
    bpy.ops.wm.save_as_mainfile(filepath=BLEND_PATH)
    render_preview()


if __name__ == "__main__":
    main()
