import os

import bpy


SCRIPT_DIRECTORY = os.path.dirname(os.path.abspath(__file__))
PROJECT_DIRECTORY = os.path.abspath(os.path.join(SCRIPT_DIRECTORY, "..", ".."))
FBX_PATH = os.path.join(
    PROJECT_DIRECTORY, "Assets", "DeadSignal", "Resources", "Environment", "SecurityBayRouteMarkerModel.fbx"
)
BLEND_PATH = os.path.join(SCRIPT_DIRECTORY, "SecurityBayRouteMarker.blend")
PREVIEW_PATH = os.path.join(SCRIPT_DIRECTORY, "SecurityBayRouteMarkerPreview.png")


def clear_scene():
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete(use_global=False)


def create_marker():
    outlines = [
        [(-1.15, -0.52), (-0.72, -0.52), (-0.05, 0.0), (-0.72, 0.52), (-1.15, 0.52), (-0.48, 0.0)],
        [(-0.15, -0.52), (0.28, -0.52), (0.95, 0.0), (0.28, 0.52), (-0.15, 0.52), (0.52, 0.0)],
    ]
    height = 0.07
    vertices = []
    faces = []
    for outline in outlines:
        start = len(vertices)
        count = len(outline)
        # The established FBX import keeps this source orientation, so author the broad face in XZ for Unity's floor.
        vertices.extend((x, 0.0, y) for x, y in outline)
        vertices.extend((x, height, y) for x, y in outline)
        faces.append(tuple(start + index for index in range(count - 1, -1, -1)))
        faces.append(tuple(start + count + index for index in range(count)))
        for index in range(count):
            next_index = (index + 1) % count
            faces.append((start + index, start + next_index, start + next_index + count, start + index + count))

    mesh = bpy.data.meshes.new("Security Bay Route Marker Mesh")
    mesh.from_pydata(vertices, [], faces)
    mesh.update()
    marker = bpy.data.objects.new("Security Bay Route Marker", mesh)
    bpy.context.collection.objects.link(marker)

    material = bpy.data.materials.new("Signal Route Preview")
    material.diffuse_color = (0.0, 0.78, 0.92, 1.0)
    material.use_nodes = True
    shader = material.node_tree.nodes.get("Principled BSDF")
    shader.inputs["Base Color"].default_value = (0.0, 0.78, 0.92, 1.0)
    shader.inputs["Emission Color"].default_value = (0.0, 0.78, 0.92, 1.0)
    shader.inputs["Emission Strength"].default_value = 1.8
    marker.data.materials.append(material)

    bpy.context.view_layer.objects.active = marker
    marker.select_set(True)
    bpy.ops.object.mode_set(mode="EDIT")
    bpy.ops.mesh.select_all(action="SELECT")
    bpy.ops.uv.smart_project(island_margin=0.04)
    bpy.ops.object.mode_set(mode="OBJECT")
    bevel = marker.modifiers.new("Route edge bevel", "BEVEL")
    bevel.width = 0.025
    bevel.segments = 2
    bpy.ops.object.modifier_apply(modifier=bevel.name)
    return marker


def export_model(marker):
    bpy.ops.object.select_all(action="DESELECT")
    marker.select_set(True)
    bpy.context.view_layer.objects.active = marker
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
    world = bpy.context.scene.world
    world.use_nodes = True
    world.node_tree.nodes["Background"].inputs["Color"].default_value = (0.002, 0.004, 0.006, 1.0)
    world.node_tree.nodes["Background"].inputs["Strength"].default_value = 0.2
    bpy.ops.object.light_add(type="AREA", location=(-2.0, -3.0, 4.0))
    bpy.context.object.data.energy = 700.0
    bpy.context.object.data.size = 4.0
    bpy.ops.object.camera_add(location=(0.0, -4.2, 4.8))
    camera = bpy.context.object
    camera.rotation_euler = (0.70, 0.0, 0.0)
    camera.data.lens = 62.0
    bpy.context.scene.camera = camera
    scene = bpy.context.scene
    scene.render.engine = "BLENDER_EEVEE"
    scene.render.resolution_x = 900
    scene.render.resolution_y = 520
    scene.render.resolution_percentage = 100
    scene.render.image_settings.file_format = "PNG"
    scene.render.filepath = PREVIEW_PATH
    bpy.ops.render.render(write_still=True)


def main():
    clear_scene()
    marker = create_marker()
    export_model(marker)
    bpy.ops.wm.save_as_mainfile(filepath=BLEND_PATH)
    render_preview()


if __name__ == "__main__":
    main()
