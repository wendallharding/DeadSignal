import math
import os

import bpy
from mathutils import Vector


SCRIPT_DIRECTORY = os.path.dirname(os.path.abspath(__file__))
PROJECT_DIRECTORY = os.path.abspath(os.path.join(SCRIPT_DIRECTORY, "..", ".."))
TEXTURE_PATH = os.path.join(
    PROJECT_DIRECTORY, "Assets", "DeadSignal", "Resources", "Environment", "SalvageAnnexAlbedo.png"
)
FBX_PATH = os.path.join(
    PROJECT_DIRECTORY, "Assets", "DeadSignal", "Resources", "Environment", "SalvageAnnexBarrierModel.fbx"
)
BLEND_PATH = os.path.join(SCRIPT_DIRECTORY, "SalvageAnnexBarrier.blend")
PREVIEW_PATH = os.path.join(SCRIPT_DIRECTORY, "SalvageAnnexBarrierPreview.png")


def clear_scene():
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete(use_global=False)
    for datablocks in (bpy.data.meshes, bpy.data.materials, bpy.data.cameras, bpy.data.lights):
        for datablock in list(datablocks):
            if datablock.users == 0:
                datablocks.remove(datablock)


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


def create_surface_material():
    material = create_material("Salvage Annex Armor", (0.21, 0.18, 0.12), metallic=0.38, roughness=0.46)
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
    bevel = obj.modifiers.new("Annex edge bevel", "BEVEL")
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
        add_box("Barrier slab", (0.0, 0.0, 0.62), (4.3, 0.78, 1.05), 0.08),
        add_box("Barrier crown", (0.0, 0.0, 1.18), (3.72, 0.66, 0.18), 0.045),
        add_box("Left anchor", (-1.78, 0.0, 0.2), (0.6, 1.0, 0.34), 0.05),
        add_box("Right anchor", (1.78, 0.0, 0.2), (0.6, 1.0, 0.34), 0.05),
        add_box("Front armor left", (-1.15, -0.43, 0.72), (0.82, 0.08, 0.48), 0.025),
        add_box("Front armor right", (1.15, -0.43, 0.72), (0.82, 0.08, 0.48), 0.025),
    ]
    return join_parts(parts, "Salvage Annex Armor", material)


def build_hazard_rail(material):
    parts = [
        add_box("Hazard rail", (0.0, -0.47, 1.02), (2.65, 0.12, 0.16), 0.025),
        add_box("Hazard rail left cap", (-1.42, -0.47, 1.02), (0.18, 0.17, 0.32), 0.025),
        add_box("Hazard rail right cap", (1.42, -0.47, 1.02), (0.18, 0.17, 0.32), 0.025),
    ]
    return join_parts(parts, "Salvage Annex Hazard Rail", material)


def build_conduit(material):
    parts = [
        add_box("Signal channel", (0.0, -0.455, 0.36), (1.48, 0.1, 0.08), 0.02),
        add_box("Signal node left", (-0.86, -0.455, 0.36), (0.18, 0.12, 0.18), 0.025),
        add_box("Signal node right", (0.86, -0.455, 0.36), (0.18, 0.12, 0.18), 0.025),
    ]
    return join_parts(parts, "Salvage Annex Conduit", material)


def add_preview_stage():
    world = bpy.context.scene.world
    world.use_nodes = True
    world.node_tree.nodes["Background"].inputs["Color"].default_value = (0.003, 0.005, 0.007, 1.0)
    world.node_tree.nodes["Background"].inputs["Strength"].default_value = 0.15
    bpy.ops.object.light_add(type="AREA", location=(-3.8, -4.5, 5.8))
    key = bpy.context.object
    key.data.energy = 820.0
    key.data.size = 4.5
    key.data.color = (0.72, 0.66, 0.52)
    bpy.ops.object.light_add(type="AREA", location=(3.6, 1.8, 3.0))
    rim = bpy.context.object
    rim.data.energy = 520.0
    rim.data.size = 3.0
    rim.data.color = (0.0, 0.76, 1.0)
    bpy.ops.object.camera_add(location=(5.9, -8.0, 5.7))
    camera = bpy.context.object
    direction = Vector((0.0, 0.0, 0.62)) - camera.location
    camera.rotation_euler = direction.to_track_quat("-Z", "Y").to_euler()
    camera.data.lens = 58.0
    bpy.context.scene.camera = camera


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
        object_types={"MESH"},
        use_mesh_modifiers=True,
        mesh_smooth_type="FACE",
        add_leaf_bones=False,
        axis_forward="-Z",
        axis_up="Y",
        path_mode="STRIP",
        embed_textures=False,
    )


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
    armor = create_surface_material()
    hazard = create_material("Salvage Annex Hazard", (0.92, 0.48, 0.04), metallic=0.28, roughness=0.38)
    conduit = create_material(
        "Salvage Annex Conduit", (0.0, 0.55, 0.72), metallic=0.12, roughness=0.24, emission=(0.0, 0.62, 0.86)
    )
    model_objects = [build_armor(armor), build_hazard_rail(hazard), build_conduit(conduit)]
    export_model(model_objects)
    add_preview_stage()
    bpy.ops.wm.save_as_mainfile(filepath=BLEND_PATH)
    render_preview()


if __name__ == "__main__":
    main()
