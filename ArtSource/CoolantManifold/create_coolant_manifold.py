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
    "Environment",
    "CoolantManifoldAlbedo.png",
)
FBX_PATH = os.path.join(
    PROJECT_DIRECTORY,
    "Assets",
    "DeadSignal",
    "Resources",
    "Environment",
    "CoolantManifoldModel.fbx",
)
BLEND_PATH = os.path.join(SCRIPT_DIRECTORY, "CoolantManifold.blend")
PREVIEW_PATH = os.path.join(SCRIPT_DIRECTORY, "CoolantManifoldPreview.png")


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


def create_armor_material():
    material = create_material("Coolant Manifold Armor", (0.18, 0.22, 0.25), metallic=0.42, roughness=0.38)
    image = bpy.data.images.load(TEXTURE_PATH, check_existing=True)
    image.colorspace_settings.name = "sRGB"
    texture = material.node_tree.nodes.new("ShaderNodeTexImage")
    texture.image = image
    texture.interpolation = "Linear"
    shader = material.node_tree.nodes.get("Principled BSDF")
    material.node_tree.links.new(texture.outputs["Color"], shader.inputs["Base Color"])
    return material


def add_box(name, location, dimensions, bevel_width=0.04, rotation=(0.0, 0.0, 0.0)):
    bpy.ops.mesh.primitive_cube_add(location=location, rotation=rotation)
    obj = bpy.context.object
    obj.name = name
    obj.dimensions = dimensions
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    bevel = obj.modifiers.new("Manifold edge bevel", "BEVEL")
    bevel.width = bevel_width
    bevel.segments = 2
    bevel.limit_method = "ANGLE"
    bpy.context.view_layer.objects.active = obj
    bpy.ops.object.modifier_apply(modifier=bevel.name)
    return obj


def add_cylinder(name, location, radius, depth, rotation=(0.0, 0.0, 0.0), vertices=12):
    bpy.ops.mesh.primitive_cylinder_add(
        vertices=vertices,
        radius=radius,
        depth=depth,
        location=location,
        rotation=rotation,
    )
    obj = bpy.context.object
    obj.name = name
    bpy.ops.object.transform_apply(location=False, rotation=True, scale=True)
    bevel = obj.modifiers.new("Manifold pipe bevel", "BEVEL")
    bevel.width = 0.018
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
    bpy.ops.uv.smart_project(angle_limit=math.radians(58.0), island_margin=0.035)
    bpy.ops.object.mode_set(mode="OBJECT")
    obj.data.materials.clear()
    obj.data.materials.append(material)
    for polygon in obj.data.polygons:
        polygon.use_smooth = smooth
    return obj


def build_body(material):
    parts = [
        add_box("Main armored reservoir", (0.0, 0.0, 0.55), (4.4, 1.08, 0.82), 0.09),
        add_box("Upper service plate", (0.0, 0.0, 1.0), (3.55, 0.82, 0.18), 0.05),
        add_box("Left armored shoulder", (-1.82, 0.0, 0.98), (0.7, 0.92, 0.28), 0.05),
        add_box("Right armored shoulder", (1.82, 0.0, 0.98), (0.7, 0.92, 0.28), 0.05),
        add_box("Left support foot", (-1.55, 0.0, 0.14), (0.72, 1.22, 0.28), 0.04),
        add_box("Right support foot", (1.55, 0.0, 0.14), (0.72, 1.22, 0.28), 0.04),
        add_box("Front vent left", (-0.92, -0.56, 0.62), (0.75, 0.08, 0.32), 0.025),
        add_box("Front vent right", (0.92, -0.56, 0.62), (0.75, 0.08, 0.32), 0.025),
    ]
    return join_parts(parts, "Coolant Manifold Body", material)


def build_conduit(material):
    parts = [
        add_cylinder(
            "Coolant conduit spine",
            (0.0, -0.62, 0.88),
            0.09,
            2.75,
            rotation=(0.0, math.radians(90.0), 0.0),
        ),
        add_cylinder("Coolant conduit left riser", (-1.36, -0.62, 0.68), 0.09, 0.42),
        add_cylinder("Coolant conduit right riser", (1.36, -0.62, 0.68), 0.09, 0.42),
        add_box("Coolant node left", (-1.36, -0.62, 1.0), (0.25, 0.18, 0.25), 0.03),
        add_box("Coolant node center", (0.0, -0.62, 0.88), (0.28, 0.18, 0.28), 0.03),
        add_box("Coolant node right", (1.36, -0.62, 1.0), (0.25, 0.18, 0.25), 0.03),
    ]
    return join_parts(parts, "Coolant Manifold Conduit", material, smooth=True)


def add_preview_stage():
    world = bpy.context.scene.world
    world.color = (0.002, 0.006, 0.009)
    world.use_nodes = True
    world.node_tree.nodes["Background"].inputs["Color"].default_value = (0.002, 0.006, 0.009, 1.0)
    world.node_tree.nodes["Background"].inputs["Strength"].default_value = 0.14

    bpy.ops.object.light_add(type="AREA", location=(-3.6, -4.2, 5.6))
    key = bpy.context.object
    key.name = "Preview Key"
    key.data.energy = 780.0
    key.data.size = 4.0
    key.data.color = (0.55, 0.72, 0.84)

    bpy.ops.object.light_add(type="AREA", location=(3.8, 1.6, 2.8))
    rim = bpy.context.object
    rim.name = "Preview Cyan Rim"
    rim.data.energy = 560.0
    rim.data.size = 3.0
    rim.data.color = (0.0, 0.82, 1.0)

    bpy.ops.object.camera_add(location=(5.8, -7.8, 5.6))
    camera = bpy.context.object
    camera.name = "Preview Camera"
    camera.data.lens = 58.0
    direction = Vector((0.0, 0.0, 0.55)) - camera.location
    camera.rotation_euler = direction.to_track_quat("-Z", "Y").to_euler()
    bpy.context.scene.camera = camera


def render_preview():
    scene = bpy.context.scene
    scene.render.engine = "BLENDER_EEVEE"
    scene.render.resolution_x = 1280
    scene.render.resolution_y = 720
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
    conduit_material = create_material(
        "Coolant Manifold Conduit",
        (0.0, 0.56, 0.72),
        metallic=0.12,
        roughness=0.22,
        emission=(0.0, 0.62, 0.88),
        emission_strength=2.6,
    )
    model_objects = [build_body(armor_material), build_conduit(conduit_material)]
    export_model(model_objects)
    add_preview_stage()
    bpy.ops.wm.save_as_mainfile(filepath=BLEND_PATH)
    render_preview()


if __name__ == "__main__":
    main()
