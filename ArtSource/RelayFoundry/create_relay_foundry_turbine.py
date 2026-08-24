import math
import os

import bpy
from mathutils import Vector


SCRIPT_DIRECTORY = os.path.dirname(os.path.abspath(__file__))
PROJECT_DIRECTORY = os.path.abspath(os.path.join(SCRIPT_DIRECTORY, "..", ".."))
TEXTURE_PATH = os.path.join(PROJECT_DIRECTORY, "Assets", "DeadSignal", "Resources", "Environment", "RelayFoundryTurbineAlbedo.png")
FBX_PATH = os.path.join(PROJECT_DIRECTORY, "Assets", "DeadSignal", "Resources", "Environment", "RelayFoundryTurbineModel.fbx")
BLEND_PATH = os.path.join(SCRIPT_DIRECTORY, "RelayFoundryTurbine.blend")
PREVIEW_PATH = os.path.join(SCRIPT_DIRECTORY, "RelayFoundryTurbinePreview.png")


def clear_scene():
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete(use_global=False)


def material(name, color, metallic, roughness, emission=None):
    result = bpy.data.materials.new(name)
    result.use_nodes = True
    shader = result.node_tree.nodes.get("Principled BSDF")
    shader.inputs["Base Color"].default_value = (*color, 1.0)
    shader.inputs["Metallic"].default_value = metallic
    shader.inputs["Roughness"].default_value = roughness
    if emission:
        shader.inputs["Emission Color"].default_value = (*emission, 1.0)
        shader.inputs["Emission Strength"].default_value = 3.0
    else:
        image = bpy.data.images.load(TEXTURE_PATH, check_existing=True)
        texture = result.node_tree.nodes.new("ShaderNodeTexImage")
        texture.image = image
        result.node_tree.links.new(texture.outputs["Color"], shader.inputs["Base Color"])
    return result


def cylinder(name, radius, depth, location, assigned, vertices=20):
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices, radius=radius, depth=depth, location=location)
    result = bpy.context.object
    result.name = name
    bevel = result.modifiers.new("Foundry bevel", "BEVEL")
    bevel.width = 0.08
    bevel.segments = 2
    bpy.context.view_layer.objects.active = result
    bpy.ops.object.modifier_apply(modifier=bevel.name)
    result.data.materials.append(assigned)
    bpy.ops.object.mode_set(mode="EDIT")
    bpy.ops.mesh.select_all(action="SELECT")
    bpy.ops.uv.smart_project(angle_limit=math.radians(58.0), island_margin=0.035)
    bpy.ops.object.mode_set(mode="OBJECT")
    return result


def box(name, dimensions, location, assigned):
    bpy.ops.mesh.primitive_cube_add(location=location)
    result = bpy.context.object
    result.name = name
    result.dimensions = dimensions
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    bevel = result.modifiers.new("Foundry bevel", "BEVEL")
    bevel.width = 0.08
    bevel.segments = 2
    bpy.context.view_layer.objects.active = result
    bpy.ops.object.modifier_apply(modifier=bevel.name)
    result.data.materials.append(assigned)
    bpy.ops.object.mode_set(mode="EDIT")
    bpy.ops.mesh.select_all(action="SELECT")
    bpy.ops.uv.smart_project(angle_limit=math.radians(58.0), island_margin=0.035)
    bpy.ops.object.mode_set(mode="OBJECT")
    return result


def main():
    clear_scene()
    armor = material("Foundry Armor", (0.16, 0.18, 0.2), 0.65, 0.34)
    ceramic = material("Foundry Ceramic", (0.72, 0.75, 0.74), 0.25, 0.4)
    cyan = material("Foundry Cyan", (0.0, 0.7, 0.85), 0.2, 0.22, (0.0, 0.85, 1.0))
    amber = material("Foundry Amber", (0.8, 0.32, 0.025), 0.3, 0.38, (0.8, 0.18, 0.0))
    objects = [
        cylinder("Turbine Plinth", 2.35, 0.45, (0.0, 0.0, 0.22), armor),
        cylinder("Turbine Ceramic Ring", 1.78, 0.55, (0.0, 0.0, 0.62), ceramic),
        cylinder("Turbine Signal Rotor", 1.22, 0.7, (0.0, 0.0, 1.05), cyan, 24),
        cylinder("Turbine Crown", 0.52, 1.45, (0.0, 0.0, 1.62), amber, 12),
        box("Turbine Brace North", (0.38, 4.6, 0.5), (0.0, 0.0, 0.38), armor),
        box("Turbine Brace East", (4.6, 0.38, 0.5), (0.0, 0.0, 0.38), armor),
    ]

    bpy.ops.object.select_all(action="DESELECT")
    for obj in objects:
        obj.select_set(True)
    bpy.context.view_layer.objects.active = objects[0]
    bpy.ops.export_scene.fbx(
        filepath=FBX_PATH, use_selection=True, apply_unit_scale=True,
        apply_scale_options="FBX_SCALE_UNITS", object_types={"MESH"},
        use_mesh_modifiers=True, mesh_smooth_type="FACE", axis_forward="-Z", axis_up="Y", path_mode="STRIP")

    world = bpy.context.scene.world
    world.color = (0.002, 0.005, 0.008)
    bpy.ops.object.light_add(type="AREA", location=(-4.5, -4.5, 7.0))
    bpy.context.object.data.energy = 1050.0
    bpy.context.object.data.size = 5.0
    bpy.ops.object.light_add(type="AREA", location=(4.0, 2.0, 4.0))
    bpy.context.object.data.energy = 650.0
    bpy.context.object.data.color = (0.0, 0.8, 1.0)
    bpy.ops.object.camera_add(location=(6.8, -8.2, 6.7))
    camera = bpy.context.object
    camera.rotation_euler = (Vector((0.0, 0.0, 0.8)) - camera.location).to_track_quat("-Z", "Y").to_euler()
    camera.data.lens = 55.0
    bpy.context.scene.camera = camera
    bpy.ops.wm.save_as_mainfile(filepath=BLEND_PATH)
    scene = bpy.context.scene
    scene.render.engine = "BLENDER_EEVEE"
    scene.render.resolution_x = 1280
    scene.render.resolution_y = 720
    scene.render.resolution_percentage = 100
    scene.render.image_settings.file_format = "PNG"
    scene.render.filepath = PREVIEW_PATH
    bpy.ops.render.render(write_still=True)


if __name__ == "__main__":
    main()
