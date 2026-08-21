import math
import os

import bpy
from mathutils import Vector


SCRIPT_DIRECTORY = os.path.dirname(os.path.abspath(__file__))
PROJECT_DIRECTORY = os.path.abspath(os.path.join(SCRIPT_DIRECTORY, "..", ".."))
TEXTURE_PATH = os.path.join(
    PROJECT_DIRECTORY, "Assets", "DeadSignal", "Resources", "Environment", "EastSalvageVaultAlbedo.png"
)
FBX_PATH = os.path.join(
    PROJECT_DIRECTORY, "Assets", "DeadSignal", "Resources", "Environment", "EastSalvageVaultModel.fbx"
)
BLEND_PATH = os.path.join(SCRIPT_DIRECTORY, "EastSalvageVault.blend")
PREVIEW_PATH = os.path.join(SCRIPT_DIRECTORY, "EastSalvageVaultPreview.png")


def clear_scene():
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete(use_global=False)


def create_material(name, color, metallic=0.0, roughness=0.5, emission=None, use_texture=False):
    material = bpy.data.materials.new(name)
    material.diffuse_color = (*color, 1.0)
    material.use_nodes = True
    shader = material.node_tree.nodes.get("Principled BSDF")
    shader.inputs["Base Color"].default_value = (*color, 1.0)
    shader.inputs["Metallic"].default_value = metallic
    shader.inputs["Roughness"].default_value = roughness
    if use_texture:
        image = bpy.data.images.load(TEXTURE_PATH, check_existing=True)
        image.colorspace_settings.name = "sRGB"
        texture = material.node_tree.nodes.new("ShaderNodeTexImage")
        texture.image = image
        material.node_tree.links.new(texture.outputs["Color"], shader.inputs["Base Color"])
    if emission is not None:
        shader.inputs["Emission Color"].default_value = (*emission, 1.0)
        shader.inputs["Emission Strength"].default_value = 2.0
    return material


def add_box(name, location, dimensions, material, bevel_width=0.04):
    bpy.ops.mesh.primitive_cube_add(location=location)
    obj = bpy.context.object
    obj.name = name
    obj.dimensions = dimensions
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    bevel = obj.modifiers.new("Vault edge bevel", "BEVEL")
    bevel.width = bevel_width
    bevel.segments = 2
    bevel.limit_method = "ANGLE"
    bpy.context.view_layer.objects.active = obj
    bpy.ops.object.modifier_apply(modifier=bevel.name)
    bpy.ops.object.mode_set(mode="EDIT")
    bpy.ops.mesh.select_all(action="SELECT")
    bpy.ops.uv.smart_project(angle_limit=math.radians(58.0), island_margin=0.035)
    bpy.ops.object.mode_set(mode="OBJECT")
    obj.data.materials.append(material)
    return obj


def join_parts(parts, name, material):
    bpy.ops.object.select_all(action="DESELECT")
    for part in parts:
        part.select_set(True)
    bpy.context.view_layer.objects.active = parts[0]
    bpy.ops.object.join()
    obj = bpy.context.object
    obj.name = name
    bpy.ops.object.mode_set(mode="EDIT")
    bpy.ops.mesh.select_all(action="SELECT")
    bpy.ops.uv.smart_project(angle_limit=math.radians(58.0), island_margin=0.035)
    bpy.ops.object.mode_set(mode="OBJECT")
    obj.data.materials.clear()
    obj.data.materials.append(material)
    return obj


def build_vault():
    deck = create_material("Vault Deck", (0.035, 0.05, 0.065), metallic=0.42, roughness=0.46, use_texture=True)
    armor = create_material("Vault Armor", (0.58, 0.57, 0.52), metallic=0.12, roughness=0.48, use_texture=True)
    copper = create_material("Vault Copper", (0.32, 0.12, 0.055), metallic=0.6, roughness=0.38, use_texture=True)
    energy = create_material("Vault Energy", (1.0, 0.32, 0.02), metallic=0.05, roughness=0.16,
                             emission=(1.0, 0.2, 0.01))

    objects = [
        add_box("Vault Floor", (0.0, 0.0, -0.18), (6.6, 6.6, 0.3), deck, 0.06),
        add_box("Vault North Wall", (0.0, 3.15, 0.52), (6.6, 0.32, 1.25), armor, 0.08),
        add_box("Vault South Wall", (0.0, -3.15, 0.52), (6.6, 0.32, 1.25), armor, 0.08),
        add_box("Vault East Wall", (3.15, 0.0, 0.52), (0.32, 6.6, 1.25), armor, 0.08),
        add_box("Vault West North Gate", (-3.15, 2.15, 0.52), (0.32, 2.0, 1.25), armor, 0.08),
        add_box("Vault West South Gate", (-3.15, -2.15, 0.52), (0.32, 2.0, 1.25), armor, 0.08),
        add_box("Vault Route Splitter", (0.45, 0.0, 0.48), (1.1, 2.5, 1.0), copper, 0.1),
    ]

    light_parts = []
    for y in (-2.78, 2.78):
        for x in (-2.2, 0.0, 2.2):
            light_parts.append(add_box("Amber guide", (x, y, 0.12), (0.82, 0.12, 0.06), energy, 0.02))
    light_parts.extend([
        add_box("Vault lock north", (2.95, 0.72, 0.7), (0.08, 0.65, 0.18), energy, 0.02),
        add_box("Vault lock south", (2.95, -0.72, 0.7), (0.08, 0.65, 0.18), energy, 0.02),
    ])
    objects.append(join_parts(light_parts, "Vault Energy Guides", energy))
    return objects


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
    world.node_tree.nodes["Background"].inputs["Color"].default_value = (0.003, 0.006, 0.012, 1.0)
    world.node_tree.nodes["Background"].inputs["Strength"].default_value = 0.16
    bpy.ops.object.light_add(type="AREA", location=(-4.5, -4.8, 7.0))
    bpy.context.object.data.energy = 1100.0
    bpy.context.object.data.size = 5.0
    bpy.context.object.data.color = (0.55, 0.7, 0.82)
    bpy.ops.object.light_add(type="AREA", location=(4.0, 1.5, 4.0))
    bpy.context.object.data.energy = 720.0
    bpy.context.object.data.size = 3.2
    bpy.context.object.data.color = (1.0, 0.28, 0.04)
    bpy.ops.object.camera_add(location=(8.3, -10.0, 10.2))
    camera = bpy.context.object
    camera.rotation_euler = (Vector((0.0, 0.0, 0.25)) - camera.location).to_track_quat("-Z", "Y").to_euler()
    camera.data.lens = 53.0
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
    objects = build_vault()
    export_model(objects)
    add_preview_stage()
    bpy.ops.wm.save_as_mainfile(filepath=BLEND_PATH)
    render_preview()


if __name__ == "__main__":
    main()
