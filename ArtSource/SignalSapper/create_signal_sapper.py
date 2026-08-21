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
    "Actors",
    "SignalSapperArmorAlbedo.png",
)
FBX_PATH = os.path.join(
    PROJECT_DIRECTORY,
    "Assets",
    "DeadSignal",
    "Resources",
    "Actors",
    "SignalSapperModel.fbx",
)
BLEND_PATH = os.path.join(SCRIPT_DIRECTORY, "SignalSapper.blend")
PREVIEW_PATH = os.path.join(SCRIPT_DIRECTORY, "SignalSapperPreview.png")


def clear_scene():
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete(use_global=False)
    for datablocks in (bpy.data.meshes, bpy.data.curves, bpy.data.materials, bpy.data.cameras, bpy.data.lights):
        for datablock in list(datablocks):
            if datablock.users == 0:
                datablocks.remove(datablock)


def create_material(name, color, metallic=0.0, roughness=0.5, emission=None, emission_strength=3.5):
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


def create_armor_material():
    material = create_material("Sapper Armor Albedo", (0.10, 0.08, 0.13), metallic=0.38, roughness=0.40)
    image = bpy.data.images.load(TEXTURE_PATH, check_existing=True)
    image.colorspace_settings.name = "sRGB"
    texture = material.node_tree.nodes.new("ShaderNodeTexImage")
    texture.image = image
    texture.interpolation = "Linear"
    shader = material.node_tree.nodes.get("Principled BSDF")
    material.node_tree.links.new(texture.outputs["Color"], shader.inputs["Base Color"])
    return material


def add_beveled_box(name, location, dimensions, bevel=0.04, rotation=(0.0, 0.0, 0.0)):
    bpy.ops.mesh.primitive_cube_add(location=location, rotation=rotation)
    obj = bpy.context.object
    obj.name = name
    obj.dimensions = dimensions
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    modifier = obj.modifiers.new("Sapper chamfers", "BEVEL")
    modifier.width = bevel
    modifier.segments = 2
    modifier.limit_method = "ANGLE"
    bpy.context.view_layer.objects.active = obj
    bpy.ops.object.modifier_apply(modifier=modifier.name)
    return obj


def add_cylinder(name, location, radius, depth, vertices=12, bevel=0.015):
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices, radius=radius, depth=depth, location=location)
    obj = bpy.context.object
    obj.name = name
    modifier = obj.modifiers.new("Sapper chamfers", "BEVEL")
    modifier.width = bevel
    modifier.segments = 2
    modifier.limit_method = "ANGLE"
    bpy.context.view_layer.objects.active = obj
    bpy.ops.object.modifier_apply(modifier=modifier.name)
    return obj


def join_parts(parts, name, origin, material, smooth=False):
    bpy.ops.object.select_all(action="DESELECT")
    for part in parts:
        part.select_set(True)
    bpy.context.view_layer.objects.active = parts[0]
    bpy.ops.object.join()
    obj = bpy.context.object
    obj.name = name
    bpy.context.scene.cursor.location = origin
    bpy.ops.object.origin_set(type="ORIGIN_CURSOR")
    bpy.ops.object.mode_set(mode="EDIT")
    bpy.ops.mesh.select_all(action="SELECT")
    bpy.ops.uv.smart_project(angle_limit=math.radians(62.0), island_margin=0.035)
    bpy.ops.object.mode_set(mode="OBJECT")
    obj.data.materials.clear()
    obj.data.materials.append(material)
    for polygon in obj.data.polygons:
        polygon.use_smooth = smooth
    return obj


def build_chassis(material):
    parts = [
        add_beveled_box("Chassis spine", (0.0, 0.02, 0.33), (0.65, 0.90, 0.28), 0.075),
        add_beveled_box("Chassis prow", (0.0, -0.47, 0.31), (0.53, 0.24, 0.22), 0.055),
        add_beveled_box("Left shell", (-0.34, 0.03, 0.31), (0.22, 0.65, 0.23), 0.05,
                         rotation=(0.0, 0.0, math.radians(-8.0))),
        add_beveled_box("Right shell", (0.34, 0.03, 0.31), (0.22, 0.65, 0.23), 0.05,
                         rotation=(0.0, 0.0, math.radians(8.0))),
        add_beveled_box("Rear ballast", (0.0, 0.48, 0.29), (0.46, 0.22, 0.18), 0.045),
        add_beveled_box("Raised siphon bed", (0.0, 0.09, 0.49), (0.42, 0.50, 0.10), 0.03),
    ]
    return join_parts(parts, "Sapper Chassis", (0.0, 0.0, 0.32), material)


def build_fork(name, side, material):
    x = 0.43 * side
    parts = [
        add_beveled_box("Fork beam", (x, -0.29, 0.28), (0.17, 0.72, 0.16), 0.035,
                         rotation=(0.0, 0.0, math.radians(4.0 * side))),
        add_beveled_box("Fork shoulder", (0.34 * side, 0.04, 0.30), (0.20, 0.30, 0.20), 0.04,
                         rotation=(0.0, 0.0, math.radians(-10.0 * side))),
        add_beveled_box("Fork fang", (0.48 * side, -0.68, 0.30), (0.18, 0.23, 0.22), 0.035,
                         rotation=(0.0, 0.0, math.radians(-13.0 * side))),
        add_cylinder("Fork node", (x, -0.45, 0.38), 0.09, 0.07, vertices=10, bevel=0.012),
    ]
    return join_parts(parts, name, (x, -0.28, 0.28), material)


def build_core(material):
    bpy.ops.mesh.primitive_torus_add(
        align="WORLD",
        major_segments=16,
        minor_segments=6,
        location=(0.0, 0.12, 0.56),
        major_radius=0.25,
        minor_radius=0.045,
    )
    ring = bpy.context.object
    parts = [
        ring,
        add_cylinder("Drain rotor", (0.0, 0.12, 0.55), 0.18, 0.08, vertices=12, bevel=0.015),
        add_beveled_box("Rotor needle north", (0.0, -0.12, 0.57), (0.08, 0.20, 0.07), 0.015),
        add_beveled_box("Rotor needle south", (0.0, 0.36, 0.57), (0.08, 0.20, 0.07), 0.015),
        add_beveled_box("Rotor needle west", (-0.24, 0.12, 0.57), (0.20, 0.08, 0.07), 0.015),
        add_beveled_box("Rotor needle east", (0.24, 0.12, 0.57), (0.20, 0.08, 0.07), 0.015),
    ]
    return join_parts(parts, "Sapper Drain Core", (0.0, 0.12, 0.55), material, smooth=True)


def add_preview_stage():
    bpy.ops.mesh.primitive_plane_add(size=14.0, location=(0.0, 0.0, -0.02))
    stage = bpy.context.object
    stage.name = "Preview Deck"
    stage.data.materials.append(create_material("Preview Deck Material", (0.01, 0.015, 0.025), metallic=0.3, roughness=0.72))

    world = bpy.context.scene.world
    world.color = (0.004, 0.002, 0.008)
    world.use_nodes = True
    world.node_tree.nodes["Background"].inputs["Color"].default_value = (0.004, 0.002, 0.009, 1.0)
    world.node_tree.nodes["Background"].inputs["Strength"].default_value = 0.16

    bpy.ops.object.light_add(type="AREA", location=(-2.8, -3.2, 5.2))
    key = bpy.context.object
    key.name = "Preview Key"
    key.data.energy = 760.0
    key.data.shape = "DISK"
    key.data.size = 3.8
    key.data.color = (0.48, 0.56, 0.78)

    bpy.ops.object.light_add(type="AREA", location=(3.2, -0.6, 2.8))
    rim = bpy.context.object
    rim.name = "Preview Sapper Rim"
    rim.data.energy = 620.0
    rim.data.size = 2.8
    rim.data.color = (1.0, 0.015, 0.48)

    bpy.ops.object.camera_add(location=(2.8, -3.8, 3.1))
    camera = bpy.context.object
    camera.name = "Preview Camera"
    camera.data.lens = 58.0
    direction = Vector((0.0, -0.10, 0.32)) - camera.location
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
    armor_material = create_armor_material()
    fork_material = create_material(
        "Sapper Fork Magenta", (0.18, 0.006, 0.11), metallic=0.4, roughness=0.3,
        emission=(0.12, 0.001, 0.065), emission_strength=1.0
    )
    core_material = create_material(
        "Sapper Drain Core Magenta", (0.32, 0.006, 0.19), metallic=0.18, roughness=0.2,
        emission=(0.28, 0.002, 0.15), emission_strength=1.5
    )

    model_objects = [
        build_chassis(armor_material),
        # FBX conversion mirrors Blender X into Unity, so source sides are intentionally exchanged.
        build_fork("Sapper Fork Left", 1, fork_material),
        build_fork("Sapper Fork Right", -1, fork_material),
        build_core(core_material),
    ]
    export_model(model_objects)
    add_preview_stage()
    bpy.ops.wm.save_as_mainfile(filepath=BLEND_PATH)
    render_preview()


if __name__ == "__main__":
    main()
