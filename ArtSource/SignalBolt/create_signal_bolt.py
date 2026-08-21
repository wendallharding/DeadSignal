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
    "Projectiles",
    "SignalBoltAlbedo.png",
)
FBX_PATH = os.path.join(
    PROJECT_DIRECTORY,
    "Assets",
    "DeadSignal",
    "Resources",
    "Projectiles",
    "SignalBoltModel.fbx",
)
BLEND_PATH = os.path.join(SCRIPT_DIRECTORY, "SignalBolt.blend")
PREVIEW_PATH = os.path.join(SCRIPT_DIRECTORY, "SignalBoltPreview.png")


def clear_scene():
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete(use_global=False)
    for datablocks in (bpy.data.meshes, bpy.data.materials, bpy.data.cameras, bpy.data.lights):
        for datablock in list(datablocks):
            if datablock.users == 0:
                datablocks.remove(datablock)


def create_material(name, color, metallic=0.0, roughness=0.5, emission=None, emission_strength=2.5):
    material = bpy.data.materials.new(name)
    material.diffuse_color = (*color, 1.0)
    material.use_nodes = True
    shader = material.node_tree.nodes.get("Principled BSDF")
    shader.inputs["Base Color"].default_value = (*color, 1.0)
    shader.inputs["Metallic"].default_value = metallic
    shader.inputs["Roughness"].default_value = roughness
    if emission is not None:
        shader.inputs["Emission Color"].default_value = (*emission, 1.0)
        shader.inputs["Emission Strength"].default_value = emission_strength
    return material


def create_shell_material():
    material = create_material("Signal Bolt Ceramic", (0.70, 0.76, 0.76), metallic=0.28, roughness=0.34)
    image = bpy.data.images.load(TEXTURE_PATH, check_existing=True)
    image.colorspace_settings.name = "sRGB"
    texture = material.node_tree.nodes.new("ShaderNodeTexImage")
    texture.image = image
    texture.interpolation = "Linear"
    shader = material.node_tree.nodes.get("Principled BSDF")
    material.node_tree.links.new(texture.outputs["Color"], shader.inputs["Base Color"])
    return material


def add_cylinder(name, location, radius, depth, vertices=12):
    bpy.ops.mesh.primitive_cylinder_add(
        vertices=vertices,
        radius=radius,
        depth=depth,
        location=location,
        rotation=(math.radians(90.0), 0.0, 0.0),
    )
    obj = bpy.context.object
    obj.name = name
    bpy.ops.object.transform_apply(location=False, rotation=True, scale=True)
    bevel = obj.modifiers.new("Bolt edge bevel", "BEVEL")
    bevel.width = 0.018
    bevel.segments = 2
    bevel.limit_method = "ANGLE"
    bpy.context.view_layer.objects.active = obj
    bpy.ops.object.modifier_apply(modifier=bevel.name)
    return obj


def add_cone(name, location, radius_one, radius_two, depth, vertices=12):
    bpy.ops.mesh.primitive_cone_add(
        vertices=vertices,
        radius1=radius_one,
        radius2=radius_two,
        depth=depth,
        location=location,
        rotation=(math.radians(90.0), 0.0, 0.0),
    )
    obj = bpy.context.object
    obj.name = name
    bpy.ops.object.transform_apply(location=False, rotation=True, scale=True)
    bevel = obj.modifiers.new("Bolt edge bevel", "BEVEL")
    bevel.width = 0.012
    bevel.segments = 2
    bevel.limit_method = "ANGLE"
    bpy.context.view_layer.objects.active = obj
    bpy.ops.object.modifier_apply(modifier=bevel.name)
    return obj


def add_fin(name, location, rotation_z):
    bpy.ops.mesh.primitive_cube_add(location=location, rotation=(0.0, 0.0, rotation_z))
    obj = bpy.context.object
    obj.name = name
    obj.dimensions = (0.055, 0.22, 0.045)
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    bevel = obj.modifiers.new("Bolt fin bevel", "BEVEL")
    bevel.width = 0.012
    bevel.segments = 2
    bevel.limit_method = "ANGLE"
    bpy.context.view_layer.objects.active = obj
    bpy.ops.object.modifier_apply(modifier=bevel.name)
    return obj


def join_parts(parts, name, material, smooth=False):
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
    bpy.ops.uv.smart_project(angle_limit=math.radians(58.0), island_margin=0.04)
    bpy.ops.object.mode_set(mode="OBJECT")
    obj.data.materials.clear()
    obj.data.materials.append(material)
    for polygon in obj.data.polygons:
        polygon.use_smooth = smooth
    return obj


def build_shell(material):
    parts = [
        add_cylinder("Bolt armored body", (0.0, 0.0, 0.0), 0.13, 0.38),
        add_cone("Bolt forward collar", (0.0, -0.235, 0.0), 0.07, 0.13, 0.09),
        add_cone("Bolt rear collar", (0.0, 0.235, 0.0), 0.13, 0.065, 0.09),
        add_fin("Bolt fin left", (-0.135, 0.09, 0.0), math.radians(-8.0)),
        add_fin("Bolt fin right", (0.135, 0.09, 0.0), math.radians(8.0)),
    ]
    return join_parts(parts, "Bolt Shell", material)


def build_energy(material):
    bpy.ops.mesh.primitive_torus_add(
        align="WORLD",
        major_segments=12,
        minor_segments=5,
        location=(0.0, -0.13, 0.0),
        rotation=(math.radians(90.0), 0.0, 0.0),
        major_radius=0.14,
        minor_radius=0.025,
    )
    ring = bpy.context.object
    parts = [
        ring,
        add_cone("Bolt energy nose", (0.0, -0.315, 0.0), 0.012, 0.075, 0.085, vertices=10),
        add_cylinder("Bolt energy spine", (0.0, 0.0, 0.0), 0.045, 0.52, vertices=10),
    ]
    return join_parts(parts, "Bolt Energy", material, smooth=True)


def add_preview_stage():
    world = bpy.context.scene.world
    world.color = (0.002, 0.006, 0.009)
    world.use_nodes = True
    world.node_tree.nodes["Background"].inputs["Color"].default_value = (0.002, 0.006, 0.009, 1.0)
    world.node_tree.nodes["Background"].inputs["Strength"].default_value = 0.12

    bpy.ops.object.light_add(type="AREA", location=(-1.6, -2.2, 2.4))
    key = bpy.context.object
    key.name = "Preview Key"
    key.data.energy = 520.0
    key.data.size = 2.4
    key.data.color = (0.56, 0.72, 0.82)

    bpy.ops.object.light_add(type="AREA", location=(1.4, 0.8, 1.2))
    rim = bpy.context.object
    rim.name = "Preview Cyan Rim"
    rim.data.energy = 420.0
    rim.data.size = 1.8
    rim.data.color = (0.0, 0.85, 1.0)

    bpy.ops.object.camera_add(location=(1.05, -1.45, 0.92))
    camera = bpy.context.object
    camera.name = "Preview Camera"
    camera.data.lens = 62.0
    direction = Vector((0.0, 0.0, 0.0)) - camera.location
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
    shell_material = create_shell_material()
    energy_material = create_material(
        "Signal Bolt Energy",
        (0.01, 0.66, 0.82),
        metallic=0.08,
        roughness=0.18,
        emission=(0.0, 0.68, 0.95),
        emission_strength=3.2,
    )
    model_objects = [build_shell(shell_material), build_energy(energy_material)]
    export_model(model_objects)
    add_preview_stage()
    bpy.ops.wm.save_as_mainfile(filepath=BLEND_PATH)
    render_preview()


if __name__ == "__main__":
    main()
