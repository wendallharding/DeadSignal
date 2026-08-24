import math
import os

import bpy
from mathutils import Vector


SCRIPT_DIRECTORY = os.path.dirname(os.path.abspath(__file__))
PROJECT_DIRECTORY = os.path.abspath(os.path.join(SCRIPT_DIRECTORY, "..", ".."))
FBX_PATH = os.path.join(
    PROJECT_DIRECTORY, "Assets", "DeadSignal", "Resources", "Environment", "QuenchCondenserModel.fbx"
)
BLEND_PATH = os.path.join(SCRIPT_DIRECTORY, "QuenchCondenser.blend")
PREVIEW_PATH = os.path.join(SCRIPT_DIRECTORY, "QuenchCondenserPreview.png")


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
        shader.inputs["Emission Strength"].default_value = 2.6
    return material


def add_box(name, location, dimensions, material, rotation=0.0, bevel_width=0.05):
    bpy.ops.mesh.primitive_cube_add(location=location, rotation=(0.0, 0.0, rotation))
    result = bpy.context.object
    result.name = name
    result.dimensions = dimensions
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    bevel = result.modifiers.new("Condenser edge bevel", "BEVEL")
    bevel.width = bevel_width
    bevel.segments = 2
    bevel.limit_method = "ANGLE"
    bpy.context.view_layer.objects.active = result
    bpy.ops.object.modifier_apply(modifier=bevel.name)
    result.data.materials.append(material)
    return result


def add_cylinder(name, location, radius, depth, material, rotation=(0.0, 0.0, 0.0), vertices=20):
    bpy.ops.mesh.primitive_cylinder_add(
        vertices=vertices, radius=radius, depth=depth, location=location, rotation=rotation
    )
    result = bpy.context.object
    result.name = name
    bevel = result.modifiers.new("Condenser coil bevel", "BEVEL")
    bevel.width = 0.035
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
    bpy.ops.object.light_add(type="AREA", location=(-3.8, -4.5, 5.5))
    bpy.context.object.data.energy = 900.0
    bpy.context.object.data.size = 3.5
    bpy.context.object.data.color = (0.72, 0.82, 0.94)
    bpy.ops.object.light_add(type="AREA", location=(3.4, 1.2, 3.8))
    bpy.context.object.data.energy = 720.0
    bpy.context.object.data.size = 2.8
    bpy.context.object.data.color = (0.0, 0.8, 1.0)
    bpy.ops.object.camera_add(location=(5.2, -7.2, 4.8))
    camera = bpy.context.object
    camera.rotation_euler = (Vector((0.0, 0.0, 0.65)) - camera.location).to_track_quat("-Z", "Y").to_euler()
    camera.data.lens = 60.0
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
    armor = create_material("Quench Condenser Armor", (0.045, 0.065, 0.095), metallic=0.72, roughness=0.3)
    ceramic = create_material("Quench Condenser Ceramic", (0.76, 0.78, 0.76), metallic=0.02, roughness=0.43)
    amber = create_material("Quench Condenser Warning", (0.62, 0.29, 0.02), metallic=0.65, roughness=0.26)
    cyan = create_material(
        "Quench Condenser Coolant", (0.005, 0.58, 0.78), metallic=0.08, roughness=0.16,
        emission=(0.0, 0.78, 1.0)
    )
    objects = [
        add_box("Condenser armored plinth", (0.0, 0.0, 0.24), (2.2, 1.5, 0.48), armor),
        add_box("Condenser ceramic spine", (0.0, 0.0, 0.72), (0.42, 1.28, 0.92), ceramic),
        add_cylinder("West coolant coil", (-0.68, 0.0, 0.78), 0.38, 1.08, cyan,
                     rotation=(math.radians(90.0), 0.0, 0.0)),
        add_cylinder("East coolant coil", (0.68, 0.0, 0.78), 0.38, 1.08, cyan,
                     rotation=(math.radians(90.0), 0.0, 0.0)),
        add_box("South warning manifold", (0.0, -0.7, 0.58), (1.45, 0.22, 0.38), amber),
        add_box("North return manifold", (0.0, 0.7, 0.58), (1.45, 0.22, 0.38), cyan),
    ]
    prepare_meshes(objects)
    export_model(objects)
    add_preview_stage()
    bpy.ops.wm.save_as_mainfile(filepath=BLEND_PATH)
    render_preview()


if __name__ == "__main__":
    main()
