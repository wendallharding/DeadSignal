import math
import os

import bpy
from mathutils import Vector


SCRIPT_DIRECTORY = os.path.dirname(os.path.abspath(__file__))
PROJECT_DIRECTORY = os.path.abspath(os.path.join(SCRIPT_DIRECTORY, "..", ".."))
TEXTURE_PATH = os.path.join(
    PROJECT_DIRECTORY, "Assets", "DeadSignal", "Resources", "Environment", "CoolantGauntletAlbedo.png"
)
FBX_PATH = os.path.join(
    PROJECT_DIRECTORY, "Assets", "DeadSignal", "Resources", "Environment", "CoolantBaffleModel.fbx"
)
BLEND_PATH = os.path.join(SCRIPT_DIRECTORY, "CoolantBaffle.blend")
PREVIEW_PATH = os.path.join(SCRIPT_DIRECTORY, "CoolantBafflePreview.png")


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
        shader.inputs["Emission Strength"].default_value = 2.0
    return material


def create_armor_material():
    material = create_material("Coolant Baffle Armor", (0.28, 0.3, 0.31), metallic=0.42, roughness=0.38)
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
    bevel = obj.modifiers.new("Industrial edge bevel", "BEVEL")
    bevel.width = bevel_width
    bevel.segments = 2
    bevel.limit_method = "ANGLE"
    bpy.context.view_layer.objects.active = obj
    bpy.ops.object.modifier_apply(modifier=bevel.name)
    return obj


def add_pipe(name, location, radius, depth):
    bpy.ops.mesh.primitive_cylinder_add(vertices=16, radius=radius, depth=depth, location=location, rotation=(0.0, math.pi / 2.0, 0.0))
    obj = bpy.context.object
    obj.name = name
    bevel = obj.modifiers.new("Pipe rim bevel", "BEVEL")
    bevel.width = 0.025
    bevel.segments = 2
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
        add_box("Baffle chassis", (0.0, 0.0, 0.55), (4.0, 0.86, 0.86), 0.08),
        add_box("Baffle plinth", (0.0, 0.0, 0.16), (4.25, 1.02, 0.24), 0.055),
        add_box("Left anchor", (-1.68, 0.0, 0.1), (0.46, 1.16, 0.2), 0.045),
        add_box("Right anchor", (1.68, 0.0, 0.1), (0.46, 1.16, 0.2), 0.045),
    ]
    return join_parts(parts, "Coolant Baffle Armor", material)


def build_fins(material):
    parts = []
    for index, x_position in enumerate((-1.45, -0.95, -0.45, 0.05, 0.55, 1.05, 1.55)):
        parts.append(add_box(f"Cooling fin {index}", (x_position, -0.05, 1.04), (0.16, 0.72, 0.52), 0.025))
    return join_parts(parts, "Coolant Baffle Fins", material)


def build_pipes(material):
    parts = [
        add_pipe("Upper reclaim pipe", (0.0, -0.5, 0.72), 0.1, 3.45),
        add_pipe("Lower reclaim pipe", (0.0, -0.5, 0.42), 0.1, 3.45),
        add_box("Left pipe clamp", (-1.45, -0.51, 0.57), (0.15, 0.12, 0.55), 0.025),
        add_box("Right pipe clamp", (1.45, -0.51, 0.57), (0.15, 0.12, 0.55), 0.025),
    ]
    return join_parts(parts, "Coolant Baffle Pipes", material)


def build_lights(material):
    parts = [
        add_box("Status light left", (-1.83, -0.51, 0.88), (0.22, 0.1, 0.2), 0.025),
        add_box("Status light right", (1.83, -0.51, 0.88), (0.22, 0.1, 0.2), 0.025),
    ]
    return join_parts(parts, "Coolant Baffle Lights", material)


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
    world.node_tree.nodes["Background"].inputs["Color"].default_value = (0.002, 0.004, 0.006, 1.0)
    world.node_tree.nodes["Background"].inputs["Strength"].default_value = 0.12
    bpy.ops.object.light_add(type="AREA", location=(-3.8, -4.8, 5.8))
    bpy.context.object.data.energy = 900.0
    bpy.context.object.data.size = 4.5
    bpy.context.object.data.color = (0.72, 0.79, 0.82)
    bpy.ops.object.light_add(type="AREA", location=(3.8, 0.8, 3.2))
    bpy.context.object.data.energy = 480.0
    bpy.context.object.data.size = 3.0
    bpy.context.object.data.color = (0.0, 0.72, 1.0)
    bpy.ops.object.camera_add(location=(6.2, -8.4, 5.9))
    camera = bpy.context.object
    camera.rotation_euler = (Vector((0.0, 0.0, 0.58)) - camera.location).to_track_quat("-Z", "Y").to_euler()
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
    fins = create_material("Coolant Baffle Fins", (0.72, 0.7, 0.62), metallic=0.12, roughness=0.46)
    pipes = create_material("Coolant Baffle Pipes", (0.42, 0.18, 0.07), metallic=0.72, roughness=0.34)
    lights = create_material(
        "Coolant Baffle Lights", (0.02, 0.62, 0.8), metallic=0.08, roughness=0.18, emission=(0.0, 0.72, 1.0)
    )
    objects = [build_armor(armor), build_fins(fins), build_pipes(pipes), build_lights(lights)]
    export_model(objects)
    add_preview_stage()
    bpy.ops.wm.save_as_mainfile(filepath=BLEND_PATH)
    render_preview()


if __name__ == "__main__":
    main()
