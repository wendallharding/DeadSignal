import math
import os

import bpy
from mathutils import Vector


SCRIPT_DIRECTORY = os.path.dirname(os.path.abspath(__file__))
PROJECT_DIRECTORY = os.path.abspath(os.path.join(SCRIPT_DIRECTORY, "..", ".."))
TEXTURE_PATH = os.path.join(
    PROJECT_DIRECTORY, "Assets", "DeadSignal", "Resources", "Environment", "RelayForkAlbedo.png"
)
FBX_PATH = os.path.join(
    PROJECT_DIRECTORY, "Assets", "DeadSignal", "Resources", "Environment", "RelayBankModel.fbx"
)
BLEND_PATH = os.path.join(SCRIPT_DIRECTORY, "RelayBank.blend")
PREVIEW_PATH = os.path.join(SCRIPT_DIRECTORY, "RelayBankPreview.png")


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
        shader.inputs["Emission Strength"].default_value = 2.2
    return material


def create_armor_material():
    material = create_material("Relay Bank Armor", (0.12, 0.16, 0.23), metallic=0.46, roughness=0.36)
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
    bevel = obj.modifiers.new("Relay edge bevel", "BEVEL")
    bevel.width = bevel_width
    bevel.segments = 2
    bevel.limit_method = "ANGLE"
    bpy.context.view_layer.objects.active = obj
    bpy.ops.object.modifier_apply(modifier=bevel.name)
    return obj


def add_cylinder(name, location, radius, depth, rotation=(0.0, 0.0, 0.0), vertices=20):
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices, radius=radius, depth=depth, location=location, rotation=rotation)
    obj = bpy.context.object
    obj.name = name
    bevel = obj.modifiers.new("Relay rim bevel", "BEVEL")
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
        add_box("Relay chassis", (0.0, 0.0, 0.5), (2.8, 0.9, 0.82), 0.08),
        add_box("Relay plinth", (0.0, 0.0, 0.13), (3.0, 1.04, 0.22), 0.05),
        add_box("Left shoulder", (-1.08, 0.0, 0.92), (0.42, 0.78, 0.2), 0.04),
        add_box("Right shoulder", (1.08, 0.0, 0.92), (0.42, 0.78, 0.2), 0.04),
    ]
    return join_parts(parts, "Relay Bank Armor", material)


def build_insulators(material):
    parts = []
    for index, x_position in enumerate((-0.72, 0.0, 0.72)):
        parts.append(add_cylinder(f"Ceramic insulator {index}", (x_position, 0.0, 1.18), 0.22, 0.62, vertices=16))
        for ring_index, z_position in enumerate((0.98, 1.14, 1.3, 1.46)):
            parts.append(add_cylinder(
                f"Insulator flange {index}-{ring_index}", (x_position, 0.0, z_position), 0.28, 0.07, vertices=16
            ))
    return join_parts(parts, "Relay Bank Insulators", material)


def build_coils(material):
    parts = [
        add_cylinder("Left induction coil", (-0.72, 0.0, 1.18), 0.14, 0.76, vertices=20),
        add_cylinder("Center induction coil", (0.0, 0.0, 1.18), 0.14, 0.76, vertices=20),
        add_cylinder("Right induction coil", (0.72, 0.0, 1.18), 0.14, 0.76, vertices=20),
        add_box("Coil bus", (0.0, -0.46, 0.72), (2.2, 0.12, 0.14), 0.025),
    ]
    return join_parts(parts, "Relay Bank Coils", material)


def build_signals(material):
    parts = [
        add_box("Route window left", (-1.18, -0.49, 0.62), (0.22, 0.1, 0.42), 0.025),
        add_box("Route window center", (0.0, -0.49, 0.62), (0.5, 0.1, 0.16), 0.025),
        add_box("Route window right", (1.18, -0.49, 0.62), (0.22, 0.1, 0.42), 0.025),
    ]
    return join_parts(parts, "Relay Bank Signals", material)


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
    world.node_tree.nodes["Background"].inputs["Color"].default_value = (0.002, 0.004, 0.008, 1.0)
    world.node_tree.nodes["Background"].inputs["Strength"].default_value = 0.13
    bpy.ops.object.light_add(type="AREA", location=(-3.8, -4.6, 5.7))
    bpy.context.object.data.energy = 900.0
    bpy.context.object.data.size = 4.2
    bpy.context.object.data.color = (0.68, 0.76, 0.86)
    bpy.ops.object.light_add(type="AREA", location=(3.4, 1.4, 3.4))
    bpy.context.object.data.energy = 500.0
    bpy.context.object.data.size = 3.0
    bpy.context.object.data.color = (0.0, 0.74, 1.0)
    bpy.ops.object.camera_add(location=(5.2, -7.2, 5.2))
    camera = bpy.context.object
    camera.rotation_euler = (Vector((0.0, 0.0, 0.68)) - camera.location).to_track_quat("-Z", "Y").to_euler()
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
    insulators = create_material("Relay Bank Insulators", (0.74, 0.72, 0.63), metallic=0.08, roughness=0.48)
    coils = create_material("Relay Bank Coils", (0.54, 0.33, 0.09), metallic=0.76, roughness=0.32)
    signals = create_material(
        "Relay Bank Signals", (0.02, 0.62, 0.82), metallic=0.08, roughness=0.16, emission=(0.0, 0.72, 1.0)
    )
    objects = [build_armor(armor), build_insulators(insulators), build_coils(coils), build_signals(signals)]
    export_model(objects)
    add_preview_stage()
    bpy.ops.wm.save_as_mainfile(filepath=BLEND_PATH)
    render_preview()


if __name__ == "__main__":
    main()
