import math
import os

import bpy
from mathutils import Vector


SCRIPT_DIRECTORY = os.path.dirname(os.path.abspath(__file__))
PROJECT_DIRECTORY = os.path.abspath(os.path.join(SCRIPT_DIRECTORY, "..", ".."))
FBX_PATH = os.path.join(
    PROJECT_DIRECTORY, "Assets", "DeadSignal", "Resources", "Environment", "ArcFurnaceModel.fbx"
)
BLEND_PATH = os.path.join(SCRIPT_DIRECTORY, "ArcFurnace.blend")
PREVIEW_PATH = os.path.join(SCRIPT_DIRECTORY, "ArcFurnacePreview.png")


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
        shader.inputs["Emission Strength"].default_value = 3.0
    return material


def add_box(name, location, dimensions, material, rotation=0.0, bevel_width=0.05):
    bpy.ops.mesh.primitive_cube_add(location=location, rotation=(0.0, 0.0, rotation))
    result = bpy.context.object
    result.name = name
    result.dimensions = dimensions
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    bevel = result.modifiers.new("Furnace edge bevel", "BEVEL")
    bevel.width = bevel_width
    bevel.segments = 2
    bevel.limit_method = "ANGLE"
    bpy.context.view_layer.objects.active = result
    bpy.ops.object.modifier_apply(modifier=bevel.name)
    result.data.materials.append(material)
    return result


def add_cylinder(name, location, radius, depth, material, vertices=24):
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices, radius=radius, depth=depth, location=location)
    result = bpy.context.object
    result.name = name
    bevel = result.modifiers.new("Furnace ring bevel", "BEVEL")
    bevel.width = 0.04
    bevel.segments = 2
    bpy.context.view_layer.objects.active = result
    bpy.ops.object.modifier_apply(modifier=bevel.name)
    result.data.materials.append(material)
    return result


def prepare_meshes(objects):
    for obj in objects:
        bpy.context.view_layer.objects.active = obj
        obj.select_set(True)
        bpy.ops.object.mode_set(mode="EDIT")
        bpy.ops.mesh.select_all(action="SELECT")
        bpy.ops.uv.smart_project(angle_limit=math.radians(58.0), island_margin=0.035)
        bpy.ops.object.mode_set(mode="OBJECT")
        obj.select_set(False)


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
    world.node_tree.nodes["Background"].inputs["Strength"].default_value = 0.11
    bpy.ops.object.light_add(type="AREA", location=(-4.5, -5.2, 6.0))
    bpy.context.object.data.energy = 1000.0
    bpy.context.object.data.size = 4.0
    bpy.context.object.data.color = (0.7, 0.78, 0.92)
    bpy.ops.object.light_add(type="AREA", location=(3.8, 1.0, 4.0))
    bpy.context.object.data.energy = 760.0
    bpy.context.object.data.size = 3.0
    bpy.context.object.data.color = (1.0, 0.08, 0.015)
    bpy.ops.object.camera_add(location=(5.8, -7.8, 5.6))
    camera = bpy.context.object
    camera.rotation_euler = (Vector((0.0, 0.0, 0.8)) - camera.location).to_track_quat("-Z", "Y").to_euler()
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
    armor = create_material("Arc Furnace Armor", (0.055, 0.075, 0.11), metallic=0.7, roughness=0.3)
    ceramic = create_material("Arc Furnace Ceramic", (0.72, 0.74, 0.72), metallic=0.03, roughness=0.45)
    amber = create_material("Arc Furnace Bus", (0.62, 0.28, 0.025), metallic=0.75, roughness=0.25)
    red = create_material(
        "Arc Furnace Core", (0.62, 0.012, 0.006), metallic=0.1, roughness=0.16, emission=(1.0, 0.015, 0.005)
    )
    cyan = create_material(
        "Arc Furnace Return", (0.005, 0.56, 0.75), metallic=0.08, roughness=0.16, emission=(0.0, 0.75, 1.0)
    )
    objects = [
        add_cylinder("Furnace armored plinth", (0.0, 0.0, 0.22), 1.72, 0.44, armor),
        add_cylinder("Furnace amber induction ring", (0.0, 0.0, 0.48), 1.32, 0.2, amber),
        add_cylinder("Furnace red arc core", (0.0, 0.0, 0.64), 0.82, 0.34, red),
        add_box("West ceramic shield", (-1.5, 0.0, 0.76), (0.44, 1.15, 0.72), ceramic, math.radians(-18.0)),
        add_box("East ceramic shield", (1.5, 0.0, 0.76), (0.44, 1.15, 0.72), ceramic, math.radians(18.0)),
        add_box("North ceramic shield", (0.0, 1.5, 0.76), (1.15, 0.44, 0.72), ceramic),
        add_box("South return manifold", (0.0, -1.48, 0.58), (1.05, 0.36, 0.42), cyan),
    ]
    prepare_meshes(objects)
    export_model(objects)
    add_preview_stage()
    bpy.ops.wm.save_as_mainfile(filepath=BLEND_PATH)
    render_preview()


if __name__ == "__main__":
    main()
