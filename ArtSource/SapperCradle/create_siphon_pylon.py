import math
import os

import bpy
from mathutils import Vector


SCRIPT_DIRECTORY = os.path.dirname(os.path.abspath(__file__))
PROJECT_DIRECTORY = os.path.abspath(os.path.join(SCRIPT_DIRECTORY, "..", ".."))
TEXTURE_PATH = os.path.join(
    PROJECT_DIRECTORY, "Assets", "DeadSignal", "Resources", "Environment", "SapperCradleAlbedo.png"
)
FBX_PATH = os.path.join(
    PROJECT_DIRECTORY, "Assets", "DeadSignal", "Resources", "Environment", "SapperSiphonPylonModel.fbx"
)
BLEND_PATH = os.path.join(SCRIPT_DIRECTORY, "SapperSiphonPylon.blend")
PREVIEW_PATH = os.path.join(SCRIPT_DIRECTORY, "SapperSiphonPylonPreview.png")


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
    material = create_material("Sapper Cradle Armor", (0.09, 0.06, 0.13), metallic=0.42, roughness=0.4)
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
    bevel = obj.modifiers.new("Pylon edge bevel", "BEVEL")
    bevel.width = bevel_width
    bevel.segments = 2
    bevel.limit_method = "ANGLE"
    bpy.context.view_layer.objects.active = obj
    bpy.ops.object.modifier_apply(modifier=bevel.name)
    return obj


def add_cylinder(name, location, radius, depth, rotation=(0.0, 0.0, 0.0), vertices=12):
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices, radius=radius, depth=depth, location=location, rotation=rotation)
    obj = bpy.context.object
    obj.name = name
    bevel = obj.modifiers.new("Coil edge bevel", "BEVEL")
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
        add_box("Pylon core", (0.0, 0.0, 0.46), (2.5, 0.78, 0.72), 0.09),
        add_box("Pylon plinth", (0.0, 0.0, 0.12), (2.65, 0.9, 0.22), 0.055),
        add_box("Left anchor", (-1.08, 0.0, 0.65), (0.38, 0.86, 0.82), 0.06),
        add_box("Right anchor", (1.08, 0.0, 0.65), (0.38, 0.86, 0.82), 0.06),
    ]
    return join_parts(parts, "Sapper Cradle Armor", material)


def build_ceramic(material):
    parts = [
        add_box("Upper yoke", (0.0, -0.43, 0.78), (1.65, 0.11, 0.16), 0.035),
        add_box("Lower yoke", (0.0, -0.43, 0.36), (1.65, 0.11, 0.16), 0.035),
        add_box("Left fork", (-0.92, -0.43, 0.57), (0.22, 0.11, 0.58), 0.035),
        add_box("Right fork", (0.92, -0.43, 0.57), (0.22, 0.11, 0.58), 0.035),
    ]
    return join_parts(parts, "Sapper Cradle Ceramic", material)


def build_energy(material):
    parts = [
        add_cylinder("Siphon coil left", (-0.5, -0.51, 0.57), 0.15, 0.1, (math.radians(90), 0.0, 0.0)),
        add_cylinder("Siphon coil center", (0.0, -0.51, 0.57), 0.15, 0.1, (math.radians(90), 0.0, 0.0)),
        add_cylinder("Siphon coil right", (0.5, -0.51, 0.57), 0.15, 0.1, (math.radians(90), 0.0, 0.0)),
        add_box("Energy rail", (0.0, 0.44, 0.54), (1.8, 0.09, 0.12), 0.025),
    ]
    return join_parts(parts, "Sapper Cradle Energy", material)


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
    world.node_tree.nodes["Background"].inputs["Color"].default_value = (0.002, 0.001, 0.005, 1.0)
    world.node_tree.nodes["Background"].inputs["Strength"].default_value = 0.12
    bpy.ops.object.light_add(type="AREA", location=(-3.4, -4.2, 5.0))
    bpy.context.object.data.energy = 850.0
    bpy.context.object.data.size = 4.0
    bpy.context.object.data.color = (0.74, 0.72, 0.78)
    bpy.ops.object.light_add(type="AREA", location=(3.0, 1.0, 2.8))
    bpy.context.object.data.energy = 520.0
    bpy.context.object.data.size = 2.8
    bpy.context.object.data.color = (1.0, 0.02, 0.55)
    bpy.ops.object.camera_add(location=(4.5, -6.8, 4.5))
    camera = bpy.context.object
    camera.rotation_euler = (Vector((0.0, 0.0, 0.55)) - camera.location).to_track_quat("-Z", "Y").to_euler()
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
    ceramic = create_material("Sapper Cradle Ceramic", (0.7, 0.68, 0.66), metallic=0.08, roughness=0.42)
    energy = create_material(
        "Sapper Cradle Energy", (0.72, 0.0, 0.32), metallic=0.05, roughness=0.16, emission=(0.95, 0.0, 0.52)
    )
    objects = [build_armor(armor), build_ceramic(ceramic), build_energy(energy)]
    export_model(objects)
    add_preview_stage()
    bpy.ops.wm.save_as_mainfile(filepath=BLEND_PATH)
    render_preview()


if __name__ == "__main__":
    main()
