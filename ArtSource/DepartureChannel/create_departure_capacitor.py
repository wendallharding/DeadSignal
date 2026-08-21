import math
import os

import bpy
from mathutils import Vector


SCRIPT_DIRECTORY = os.path.dirname(os.path.abspath(__file__))
PROJECT_DIRECTORY = os.path.abspath(os.path.join(SCRIPT_DIRECTORY, "..", ".."))
TEXTURE_PATH = os.path.join(
    PROJECT_DIRECTORY, "Assets", "DeadSignal", "Resources", "Environment", "DepartureCapacitorAlbedo.png"
)
FBX_PATH = os.path.join(
    PROJECT_DIRECTORY, "Assets", "DeadSignal", "Resources", "Environment", "DepartureCapacitorModel.fbx"
)
BLEND_PATH = os.path.join(SCRIPT_DIRECTORY, "DepartureCapacitor.blend")
PREVIEW_PATH = os.path.join(SCRIPT_DIRECTORY, "DepartureCapacitorPreview.png")


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
        shader.inputs["Emission Strength"].default_value = 2.4
    return material


def create_armor_material():
    material = create_material("Departure Capacitor Armor", (0.42, 0.45, 0.46), metallic=0.3, roughness=0.38)
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
    bevel = obj.modifiers.new("Capacitor bevel", "BEVEL")
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
        add_box("Capacitor chassis", (0.0, 0.0, 0.58), (4.6, 0.84, 0.92), 0.08),
        add_box("Ceramic shoulder left", (-1.72, -0.05, 1.06), (0.88, 0.72, 0.18), 0.045),
        add_box("Ceramic shoulder right", (1.72, -0.05, 1.06), (0.88, 0.72, 0.18), 0.045),
        add_box("Anchor left", (-1.86, 0.0, 0.16), (0.58, 1.04, 0.3), 0.05),
        add_box("Anchor right", (1.86, 0.0, 0.16), (0.58, 1.04, 0.3), 0.05),
    ]
    return join_parts(parts, "Departure Capacitor Armor", material)


def build_cells(material):
    parts = []
    for index, x_position in enumerate((-1.2, -0.4, 0.4, 1.2)):
        parts.append(add_box(f"Signal cell {index}", (x_position, -0.48, 0.62), (0.58, 0.14, 0.42), 0.055))
    parts.append(add_box("Signal spine", (0.0, -0.49, 0.92), (3.35, 0.12, 0.08), 0.025))
    return join_parts(parts, "Departure Capacitor Cells", material)


def build_beacons(material):
    parts = [
        add_box("Threshold beacon left", (-2.12, -0.48, 0.82), (0.18, 0.16, 0.58), 0.035),
        add_box("Threshold beacon right", (2.12, -0.48, 0.82), (0.18, 0.16, 0.58), 0.035),
    ]
    return join_parts(parts, "Departure Threshold Beacons", material)


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
    world.node_tree.nodes["Background"].inputs["Color"].default_value = (0.002, 0.005, 0.008, 1.0)
    world.node_tree.nodes["Background"].inputs["Strength"].default_value = 0.14
    bpy.ops.object.light_add(type="AREA", location=(-3.8, -4.4, 5.7))
    bpy.context.object.data.energy = 850.0
    bpy.context.object.data.size = 4.5
    bpy.context.object.data.color = (0.68, 0.78, 0.86)
    bpy.ops.object.light_add(type="AREA", location=(3.7, 1.7, 3.0))
    bpy.context.object.data.energy = 520.0
    bpy.context.object.data.size = 3.0
    bpy.context.object.data.color = (0.0, 0.78, 1.0)
    bpy.ops.object.camera_add(location=(6.0, -8.2, 5.8))
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
    cells = create_material(
        "Departure Capacitor Cells", (0.0, 0.62, 0.82), metallic=0.1, roughness=0.2, emission=(0.0, 0.7, 1.0)
    )
    beacon = create_material(
        "Departure Threshold Beacons", (0.42, 0.9, 1.0), metallic=0.08, roughness=0.18, emission=(0.1, 0.82, 1.0)
    )
    objects = [build_armor(armor), build_cells(cells), build_beacons(beacon)]
    export_model(objects)
    add_preview_stage()
    bpy.ops.wm.save_as_mainfile(filepath=BLEND_PATH)
    render_preview()


if __name__ == "__main__":
    main()
