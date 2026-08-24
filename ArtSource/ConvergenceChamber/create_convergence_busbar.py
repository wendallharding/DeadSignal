import math
import os

import bpy
from mathutils import Vector


SCRIPT_DIRECTORY = os.path.dirname(os.path.abspath(__file__))
PROJECT_DIRECTORY = os.path.abspath(os.path.join(SCRIPT_DIRECTORY, "..", ".."))
FBX_PATH = os.path.join(
    PROJECT_DIRECTORY, "Assets", "DeadSignal", "Resources", "Environment", "ConvergenceBusbarModel.fbx"
)
BLEND_PATH = os.path.join(SCRIPT_DIRECTORY, "ConvergenceBusbar.blend")
PREVIEW_PATH = os.path.join(SCRIPT_DIRECTORY, "ConvergenceBusbarPreview.png")


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


def add_box(name, location, dimensions, material, bevel_width=0.04):
    bpy.ops.mesh.primitive_cube_add(location=location)
    result = bpy.context.object
    result.name = name
    result.dimensions = dimensions
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    bevel = result.modifiers.new("Busbar edge bevel", "BEVEL")
    bevel.width = bevel_width
    bevel.segments = 2
    bevel.limit_method = "ANGLE"
    bpy.context.view_layer.objects.active = result
    bpy.ops.object.modifier_apply(modifier=bevel.name)
    result.data.materials.append(material)
    return result


def add_cylinder(name, location, radius, depth, material, vertices=16):
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices, radius=radius, depth=depth, location=location)
    result = bpy.context.object
    result.name = name
    bevel = result.modifiers.new("Busbar ring bevel", "BEVEL")
    bevel.width = 0.025
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
    world.node_tree.nodes["Background"].inputs["Strength"].default_value = 0.12
    bpy.ops.object.light_add(type="AREA", location=(-4.0, -4.8, 5.8))
    bpy.context.object.data.energy = 950.0
    bpy.context.object.data.size = 4.0
    bpy.context.object.data.color = (0.72, 0.78, 0.9)
    bpy.ops.object.light_add(type="AREA", location=(3.5, 1.5, 3.8))
    bpy.context.object.data.energy = 620.0
    bpy.context.object.data.size = 3.0
    bpy.context.object.data.color = (0.05, 0.8, 1.0)
    bpy.ops.object.camera_add(location=(5.4, -7.4, 5.4))
    camera = bpy.context.object
    camera.rotation_euler = (Vector((0.0, 0.0, 0.75)) - camera.location).to_track_quat("-Z", "Y").to_euler()
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
    armor = create_material("Convergence Armor", (0.08, 0.11, 0.16), metallic=0.58, roughness=0.34)
    ceramic = create_material("Convergence Ceramic", (0.72, 0.72, 0.68), metallic=0.05, roughness=0.46)
    copper = create_material("Convergence Bus", (0.56, 0.27, 0.06), metallic=0.82, roughness=0.28)
    cyan = create_material(
        "Convergence Signal", (0.01, 0.56, 0.75), metallic=0.08, roughness=0.16, emission=(0.0, 0.72, 1.0)
    )
    red = create_material(
        "Convergence Warning", (0.52, 0.015, 0.01), metallic=0.08, roughness=0.18, emission=(0.9, 0.02, 0.01)
    )
    objects = [
        add_box("Busbar armored plinth", (0.0, 0.0, 0.18), (3.2, 1.25, 0.34), armor, 0.07),
        add_box("Left ceramic shoulder", (-1.12, 0.0, 0.58), (0.5, 1.0, 0.5), ceramic, 0.06),
        add_box("Right ceramic shoulder", (1.12, 0.0, 0.58), (0.5, 1.0, 0.5), ceramic, 0.06),
        add_box("Crossfeed bus", (0.0, 0.0, 0.82), (2.25, 0.34, 0.18), copper, 0.035),
        add_cylinder("Left bus insulator", (-0.68, 0.0, 0.68), 0.22, 0.8, ceramic),
        add_cylinder("Right bus insulator", (0.68, 0.0, 0.68), 0.22, 0.8, ceramic),
        add_box("Cyan crossfeed window", (0.0, -0.64, 0.48), (1.45, 0.08, 0.19), cyan, 0.025),
        add_box("Red security node", (0.0, -0.65, 0.78), (0.38, 0.08, 0.38), red, 0.04),
    ]
    prepare_meshes(objects)
    export_model(objects)
    add_preview_stage()
    bpy.ops.wm.save_as_mainfile(filepath=BLEND_PATH)
    render_preview()


if __name__ == "__main__":
    main()
