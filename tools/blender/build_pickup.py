"""
Builds the hero storm-chaser pickup (S7-07) from code, renders previews and exports FBX for Unity.

    blender --background --factory-startup --python tools/blender/build_pickup.py -- <fbx_out> <render_dir>

Brief: docs/visual-targets/vehicles/pickup/README.md. Space: Blender X right, Y forward, Z up; origin at the
truck's physics origin (0.5 m above ground). Must match WheelLayout.Pickup: axles at Y +0.62 / -0.60, track
X +/-0.52, wheel radius 0.22 (visual 0.23), body collider 1.0 x 1.85 x 0.5.
Five materials, mapped to toon materials in Unity: Body, Trim, Dark, Glass, Light. Detail is shape, not texture.
"""
import math
import os
import sys

import bpy
import bmesh
from mathutils import Vector

ARGS = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
FBX_OUT = ARGS[0] if len(ARGS) > 0 else os.path.abspath("Pickup.fbx")
RENDER_DIR = ARGS[1] if len(ARGS) > 1 else os.path.dirname(FBX_OUT)

GROUND_Z = -0.5
WHEEL_R = 0.23
WHEEL_W = 0.20
WHEEL_Z = GROUND_Z + 0.21  # sits on the springs at rest, matching the cube truck's -0.29
AXLE_F, AXLE_R, TRACK = 0.62, -0.60, 0.52

COLORS = {
    "Body": (0.80, 0.14, 0.10, 1.0),
    "Trim": (0.93, 0.89, 0.80, 1.0),
    "Dark": (0.10, 0.10, 0.11, 1.0),
    "Glass": (0.16, 0.24, 0.34, 1.0),
    "Light": (1.00, 0.82, 0.30, 1.0),
}


def reset():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.context.scene.unit_settings.system = "METRIC"
    bpy.context.scene.unit_settings.scale_length = 1.0


def materials():
    mats = {}
    for name, rgba in COLORS.items():
        m = bpy.data.materials.new(name)
        m.diffuse_color = rgba
        m.use_nodes = True
        bsdf = m.node_tree.nodes.get("Principled BSDF")
        if bsdf is not None:
            bsdf.inputs["Base Color"].default_value = rgba
            bsdf.inputs["Roughness"].default_value = 0.8
        mats[name] = m
    return mats


def link(obj, parent):
    bpy.context.scene.collection.objects.link(obj)
    if parent is not None:
        obj.parent = parent
    return obj


def mesh_object(name, bm, mat, parent, bevel=0.0):
    me = bpy.data.meshes.new(name)
    bm.to_mesh(me)
    bm.free()
    for p in me.polygons:
        p.use_smooth = False
    me.materials.append(mat)
    obj = link(bpy.data.objects.new(name, me), parent)
    if bevel > 0.0:
        mod = obj.modifiers.new("Chamfer", "BEVEL")
        mod.width = bevel
        mod.segments = 1
        mod.limit_method = "ANGLE"
    return obj


def box(name, center, size, mat, parent, bevel=0.012, rake_front=0.0, rake_back=0.0, taper_x=0.0):
    """Axis box. rake_front/back pull the top front/back edge in along Y (windshield, tailgate slope);
    taper_x narrows the top face on each side (greenhouse tumblehome)."""
    bm = bmesh.new()
    sx, sy, sz = (s * 0.5 for s in size)
    for v in bmesh.ops.create_cube(bm, size=1.0)["verts"]:
        x, y, z = v.co.x * 2 * sx, v.co.y * 2 * sy, v.co.z * 2 * sz
        if z > 0:
            if y > 0:
                y -= rake_front
            else:
                y += rake_back
            x -= math.copysign(taper_x, x)
        v.co = Vector((x + center[0], y + center[1], z + center[2]))
    return mesh_object(name, bm, mat, parent, bevel)


def cylinder(name, center, radius, depth, mat, parent, axis="X", segments=16, bevel=0.0):
    bm = bmesh.new()
    bmesh.ops.create_cone(bm, cap_ends=True, segments=segments, radius1=radius, radius2=radius, depth=depth)
    rot = {"X": (0.0, math.pi / 2, 0.0), "Y": (math.pi / 2, 0.0, 0.0), "Z": (0.0, 0.0, 0.0)}[axis]
    from mathutils import Euler
    bmesh.ops.rotate(bm, verts=bm.verts, cent=(0, 0, 0), matrix=Euler(rot).to_matrix())
    bmesh.ops.translate(bm, verts=bm.verts, vec=Vector(center))
    return mesh_object(name, bm, mat, parent, bevel)


def bar(name, a, b, thickness, mat, parent, width=None):
    """Square tube from point a to point b (rack rails, bull bar, mast); with width, a flat plate whose
    width runs along X (windshield)."""
    a, b = Vector(a), Vector(b)
    d = b - a
    bm = bmesh.new()
    bmesh.ops.create_cube(bm, size=1.0)
    bmesh.ops.scale(bm, verts=bm.verts, vec=Vector((width or thickness, thickness, d.length)))
    rot = Vector((0, 0, 1)).rotation_difference(d.normalized()).to_matrix()
    bmesh.ops.rotate(bm, verts=bm.verts, cent=(0, 0, 0), matrix=rot)
    bmesh.ops.translate(bm, verts=bm.verts, vec=(a + b) * 0.5)
    return mesh_object(name, bm, mat, parent)


def wheel(name, x, y, m, root):
    """One mesh per wheel (tyre + hub cap), pivot at the hub, so Unity can spin and steer it. A single object
    on purpose: the FBX exporter's baked axis conversion misplaced mesh children of offset empties."""
    side = 1.0 if x > 0 else -1.0
    tyre = cylinder(name, (0, 0, 0), WHEEL_R, WHEEL_W, m["Dark"], None, segments=14, bevel=0.02)
    cap = cylinder(name + "_Hub", (side * (WHEEL_W * 0.5 + 0.004), 0, 0), WHEEL_R * 0.55, 0.012, m["Trim"], None,
                   segments=7)
    bpy.ops.object.select_all(action="DESELECT")
    tyre.select_set(True)
    cap.select_set(True)
    bpy.context.view_layer.objects.active = tyre
    bpy.ops.object.join()
    tyre.location = (x, y, WHEEL_Z)
    tyre.parent = root
    return tyre


def build(m):
    root = link(bpy.data.objects.new("Pickup", None), None)

    # ---- Body ----
    box("LowerBody", (0, 0.02, -0.14), (0.98, 1.86, 0.28), m["Body"], root, bevel=0.03)
    box("Hood", (0, 0.76, 0.03), (0.94, 0.42, 0.10), m["Body"], root, rake_front=0.04, bevel=0.02)
    box("Cab", (0, 0.30, 0.20), (0.95, 0.66, 0.36), m["Body"], root, rake_front=0.20, taper_x=0.05, bevel=0.025)
    box("Roof", (0, 0.24, 0.395), (0.84, 0.44, 0.04), m["Trim"], root, bevel=0.01)
    bar("Windshield", (0, 0.638, 0.05), (0, 0.448, 0.37), 0.02, m["Glass"], root, width=0.80)
    for s in (-1, 1):
        box("SideWindow" + ("L" if s < 0 else "R"), (s * 0.455, 0.30, 0.25), (0.02, 0.48, 0.20), m["Glass"], root,
            rake_front=0.12, bevel=0.0)
    box("RearWindow", (0, -0.035, 0.25), (0.78, 0.02, 0.18), m["Glass"], root, bevel=0.0)

    # Bed: sides, floor, tailgate
    for s in (-1, 1):
        box("BedSide" + ("L" if s < 0 else "R"), (s * 0.465, -0.52, 0.06), (0.06, 0.82, 0.16), m["Body"], root)
    box("BedFloor", (0, -0.52, -0.01), (0.88, 0.82, 0.03), m["Dark"], root, bevel=0.0)
    box("Tailgate", (0, -0.93, 0.05), (0.98, 0.05, 0.18), m["Trim"], root)

    # ---- Arches: chunky dark flares that stick out past the body (Rocket League stance) ----
    for y in (AXLE_F, AXLE_R):
        for s in (-1, 1):
            box("Flare", (s * 0.52, y, -0.07), (0.16, 0.62, 0.10), m["Dark"], root, rake_front=0.06,
                rake_back=0.06, bevel=0.02)

    # ---- Front: grille block, headlights, bumper, skid plate, bull bar ----
    box("Grille", (0, 0.965, -0.06), (0.62, 0.06, 0.20), m["Dark"], root)
    for s in (-1, 1):
        box("Headlight", (s * 0.36, 0.975, -0.02), (0.16, 0.04, 0.09), m["Light"], root, bevel=0.0)
    box("FrontBumper", (0, 1.0, -0.21), (1.04, 0.10, 0.11), m["Dark"], root, bevel=0.02)
    box("SkidPlate", (0, 0.98, -0.31), (0.70, 0.12, 0.05), m["Trim"], root, bevel=0.01)
    for s in (-1, 1):
        bar("BullBarPost", (s * 0.30, 1.07, -0.20), (s * 0.27, 1.09, 0.06), 0.06, m["Dark"], root)
    bar("BullBarTop", (-0.30, 1.09, 0.06), (0.30, 1.09, 0.06), 0.06, m["Dark"], root)
    for s in (-1, 1):
        box("Mirror", (s * 0.53, 0.50, 0.17), (0.08, 0.05, 0.08), m["Dark"], root, bevel=0.0)

    # ---- Rear ----
    box("RearBumper", (0, -0.97, -0.20), (1.02, 0.08, 0.10), m["Dark"], root, bevel=0.02)
    for s in (-1, 1):
        box("TailLight", (s * 0.44, -0.955, 0.07), (0.06, 0.03, 0.12), m["Body"], root, bevel=0.0)

    # ---- Chaser kit: roof rack, light bar, beacon, antennas ----
    rack_z = 0.46
    t = 0.05  # bar thickness: thin rails alias and turn into outline noise at chase distance
    for s in (-1, 1):
        bar("RackRail", (s * 0.40, 0.48, rack_z), (s * 0.40, -0.02, rack_z), t, m["Dark"], root)
        for y in (0.44, 0.02):
            bar("RackFoot", (s * 0.40, y, 0.40), (s * 0.40, y, rack_z), t, m["Dark"], root)
    for y in (0.24, 0.02):
        bar("RackCross", (-0.40, y, rack_z), (0.40, y, rack_z), t, m["Dark"], root)
    box("LightBarHousing", (0, 0.50, rack_z + 0.04), (0.74, 0.08, 0.07), m["Dark"], root, bevel=0.0)
    box("LightBar", (0, 0.545, rack_z + 0.04), (0.68, 0.02, 0.045), m["Light"], root, bevel=0.0)
    cylinder("Beacon", (-0.30, 0.06, rack_z + 0.07), 0.06, 0.10, m["Light"], root, axis="Z", segments=8)
    bar("Antenna", (0.44, -0.08, 0.40), (0.47, -0.12, 0.88), 0.025, m["Dark"], root)

    # Roof camcorder on a stubby mount: CamcorderMount sits at its lens (Unity 0, 0.66, 0.41).
    bar("CamPole", (0, 0.25, rack_z), (0, 0.25, 0.60), t, m["Dark"], root)
    box("Camcorder", (0, 0.27, 0.66), (0.13, 0.20, 0.11), m["Dark"], root, bevel=0.015)
    cylinder("CamLens", (0, 0.385, 0.66), 0.04, 0.04, m["Glass"], root, axis="Y", segments=8)

    # ---- Signature: mesonet instrument mast boomed forward over the hood (NSSL reference) ----
    # Side-mounted on the passenger side (MX), with the instruments outboard, so nothing sits inside the
    # roof camcorder's 30-degree view (the viewfinder renders from the camcorder).
    mx = 0.32
    bar("MastBoom", (mx, 0.48, rack_z + 0.02), (mx, 1.12, rack_z + 0.02), t, m["Dark"], root)
    bar("MastBrace", (mx, 0.82, rack_z + 0.02), (mx - 0.02, 1.08, 0.08), 0.04, m["Dark"], root)
    bar("MastPost", (mx, 1.12, rack_z + 0.02), (mx, 1.12, 0.80), t, m["Dark"], root)
    bar("MastArm", (mx, 1.12, 0.76), (mx + 0.24, 1.12, 0.76), 0.04, m["Dark"], root)
    # anemometer at the arm tip: hub + three cups; vane trailing behind the post
    ax = mx + 0.24
    cylinder("AnemometerHub", (ax, 1.12, 0.80), 0.03, 0.08, m["Trim"], root, axis="Z", segments=6)
    for k in range(3):
        a = k * 2 * math.pi / 3
        cx, cy = ax + 0.09 * math.cos(a), 1.12 + 0.09 * math.sin(a)
        box("AnemometerCup", (cx, cy, 0.83), (0.06, 0.06, 0.05), m["Trim"], root, bevel=0.0)
    bar("VaneRod", (mx + 0.10, 1.12, 0.81), (mx + 0.10, 0.92, 0.81), 0.03, m["Trim"], root)
    box("VaneFin", (mx + 0.10, 0.90, 0.81), (0.015, 0.10, 0.10), m["Trim"], root, bevel=0.0)

    # Bed gear: instrument case + rack frame over the bed
    box("GearCase", (0.12, -0.44, 0.10), (0.42, 0.30, 0.18), m["Trim"], root, bevel=0.015)
    box("GearCrate", (-0.22, -0.66, 0.08), (0.30, 0.26, 0.14), m["Dark"], root, bevel=0.01)

    # ---- Wheels ----
    wheel("Wheel_FL", -TRACK, AXLE_F, m, root)
    wheel("Wheel_FR", TRACK, AXLE_F, m, root)
    wheel("Wheel_RL", -TRACK, AXLE_R, m, root)
    wheel("Wheel_RR", TRACK, AXLE_R, m, root)
    return root


def triangle_count():
    deps = bpy.context.evaluated_depsgraph_get()
    total = 0
    for obj in bpy.context.scene.objects:
        if obj.type != "MESH":
            continue
        me = obj.evaluated_get(deps).to_mesh()
        me.calc_loop_triangles()
        total += len(me.loop_triangles)
        obj.evaluated_get(deps).to_mesh_clear()
    return total


def render_previews():
    scene = bpy.context.scene
    scene.render.engine = "BLENDER_WORKBENCH"
    shading = scene.display.shading
    shading.light = "STUDIO"
    shading.color_type = "MATERIAL"
    shading.show_object_outline = True
    shading.show_cavity = True
    scene.render.resolution_x, scene.render.resolution_y = 1280, 800
    scene.render.film_transparent = False
    world = bpy.data.worlds.new("World")
    scene.world = world
    world.color = (0.55, 0.62, 0.70)

    # ground plane for contact
    bm = bmesh.new()
    bmesh.ops.create_grid(bm, x_segments=1, y_segments=1, size=4.0)
    bmesh.ops.translate(bm, verts=bm.verts, vec=Vector((0, 0, GROUND_Z)))
    ground_mat = bpy.data.materials.new("Ground")
    ground_mat.diffuse_color = (0.35, 0.55, 0.25, 1.0)
    ground = mesh_object("PreviewGround", bm, ground_mat, None)

    cam_data = bpy.data.cameras.new("Cam")
    cam_data.lens = 50
    cam = link(bpy.data.objects.new("Cam", cam_data), None)
    scene.camera = cam
    target = Vector((0, 0.05, 0.05))
    views = {
        "front34": Vector((3.2, 4.2, 1.9)),
        "side": Vector((5.6, 0.05, 0.6)),
        "rear34": Vector((-3.4, -4.2, 2.1)),
        "chase": Vector((0, -8.0, 4.0)),  # the game's chase distance
    }
    for name, pos in views.items():
        cam.location = pos
        cam.rotation_euler = (target - pos).to_track_quat("-Z", "Y").to_euler()
        scene.render.filepath = os.path.join(RENDER_DIR, "pickup_" + name + ".png")
        bpy.ops.render.render(write_still=True)
    bpy.data.objects.remove(ground)
    bpy.data.objects.remove(cam)


def export_fbx():
    os.makedirs(os.path.dirname(FBX_OUT), exist_ok=True)
    bpy.ops.export_scene.fbx(
        filepath=FBX_OUT,
        use_selection=False,
        object_types={"MESH", "EMPTY"},
        use_mesh_modifiers=True,
        mesh_smooth_type="FACE",
        apply_unit_scale=True,
        apply_scale_options="FBX_SCALE_ALL",
        axis_forward="Z",
        axis_up="Y",
        bake_space_transform=True,
        add_leaf_bones=False,
        bake_anim=False,
    )


if __name__ == "__main__":
    reset()
    mats = materials()
    build(mats)
    print(f"[Pickup] triangles={triangle_count()}")
    os.makedirs(RENDER_DIR, exist_ok=True)
    render_previews()
    export_fbx()
    print(f"[Pickup] exported {FBX_OUT}")
