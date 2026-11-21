import bpy
import math
import os
from mathutils import Vector

PROJECT_ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "../.."))
OUT = os.path.join(PROJECT_ROOT, "Assets", "Resources", "EclipseRacer.obj")
os.makedirs(os.path.dirname(OUT), exist_ok=True)

bpy.ops.object.select_all(action="SELECT")
bpy.ops.object.delete(use_global=False)

def material(name, color, metallic=0.3, roughness=0.3, emission=None):
    m = bpy.data.materials.new(name)
    m.diffuse_color = (*color, 1.0)
    m.use_nodes = True
    bs = m.node_tree.nodes.get("Principled BSDF")
    bs.inputs["Base Color"].default_value = (*color, 1.0)
    bs.inputs["Metallic"].default_value = metallic
    bs.inputs["Roughness"].default_value = roughness
    if emission:
        bs.inputs["Emission Color"].default_value = (*emission, 1.0)
        bs.inputs["Emission Strength"].default_value = 2.5
    return m

armor = material("Eclipse_Titanium", (0.38, 0.53, 0.66), 0.72, 0.24)
dark = material("Eclipse_Ceramic", (0.022, 0.045, 0.075), 0.58, 0.28)
glass = material("Eclipse_Canopy", (0.015, 0.18, 0.27), 0.72, 0.12)
orange = material("Eclipse_Thruster_Orange", (1.0, 0.16, 0.035), 0.45, 0.22, (1.0, 0.08, 0.01))
cyan = material("Eclipse_Emitter_Cyan", (0.02, 0.65, 1.0), 0.25, 0.2, (0.01, 0.75, 1.0))
black = material("Eclipse_Intake_Black", (0.004, 0.009, 0.018), 0.2, 0.35)

parts = []

def finish(obj, mat):
    obj.data.materials.append(mat)
    parts.append(obj)
    return obj

def box(name, loc, dims, mat, bevel=0.08):
    bpy.ops.mesh.primitive_cube_add(size=1, location=loc)
    o = bpy.context.object
    o.name = name
    o.dimensions = dims
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    if bevel:
        mod = o.modifiers.new("Machined chamfers", "BEVEL")
        mod.width = bevel
        mod.segments = 2
        mod.profile = 0.5
        bpy.context.view_layer.objects.active = o
        bpy.ops.object.modifier_apply(modifier=mod.name)
        norm = o.modifiers.new("Corner normals", "WEIGHTED_NORMAL")
        bpy.ops.object.modifier_apply(modifier=norm.name)
    return finish(o, mat)

def sphere(name, loc, scale, mat):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=20, ring_count=12, location=loc)
    o = bpy.context.object
    o.name = name
    o.scale = scale
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    for p in o.data.polygons:
        p.use_smooth = True
    return finish(o, mat)

def cylinder(name, loc, radius, depth, mat, vertices=32, rotation=(0, 0, 0)):
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices, radius=radius, depth=depth,
                                        location=loc, rotation=rotation)
    o = bpy.context.object
    o.name = name
    bevel = o.modifiers.new("Machined rim", "BEVEL")
    bevel.width = 0.045
    bevel.segments = 2
    bpy.context.view_layer.objects.active = o
    bpy.ops.object.modifier_apply(modifier=bevel.name)
    return finish(o, mat)

def wedge(name, mat):
    # Forward is +Z; Y is vertical. The tapered snout is a true faceted mesh.
    verts = [
        (-1.62, -0.42, 1.65), (1.62, -0.42, 1.65),
        (1.62, 0.40, 1.65), (-1.62, 0.40, 1.65),
        (-0.92, -0.12, 4.50), (0.92, -0.12, 4.50),
        (0.92, 0.25, 4.50), (-0.92, 0.25, 4.50),
    ]
    faces = [(0,1,2,3), (4,7,6,5), (0,4,5,1), (1,5,6,2),
             (2,6,7,3), (3,7,4,0)]
    mesh = bpy.data.meshes.new(name + "Mesh")
    mesh.from_pydata(verts, [], faces)
    mesh.materials.append(mat)
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    bevel = obj.modifiers.new("Nose edge chamfers", "BEVEL")
    bevel.width = 0.09
    bevel.segments = 2
    bpy.context.view_layer.objects.active = obj
    bpy.ops.object.select_all(action="DESELECT")
    obj.select_set(True)
    bpy.ops.object.modifier_apply(modifier=bevel.name)
    parts.append(obj)
    return obj

# Main monocoque, tapered nose and layered armor spine.
box("Primary armored hull", (0, 0, -0.15), (3.5, 0.82, 6.9), armor, 0.24)
wedge("Tapered armored nose", armor)
box("Raised center armor", (0, 0.49, -0.05), (1.55, 0.22, 4.2), dark, 0.10)
box("Forward hood spine", (0, 0.57, 2.0), (0.55, 0.13, 2.0), armor, 0.055)

# Open, low canopy set into the chassis.
sphere("Cockpit canopy", (0, 0.55, -0.72), (0.89, 0.46, 1.72), glass)
box("Canopy forward frame", (0, 0.78, 0.83), (1.92, 0.12, 0.14), armor, 0.055)
box("Canopy rear frame", (0, 0.78, -2.23), (1.92, 0.12, 0.14), armor, 0.055)

# Broad side sponsons, clipped wings, underslung hover pads and armored fins.
for side in (-1, 1):
    x = side
    box("Side sponson", (x * 2.48, -0.08, -0.24), (1.42, 0.72, 5.8), armor, 0.2)
    box("Wing root", (x * 2.75, -0.04, -1.58), (2.15, 0.18, 2.75), dark, 0.09)
    box("Wing armor cap", (x * 2.72, 0.08, -1.45), (1.92, 0.14, 2.42), armor, 0.08)
    box("Wing edge light", (x * 3.53, 0.12, -1.40), (0.12, 0.09, 2.45), cyan, 0.035)
    box("Hover pad", (x * 2.44, -0.63, -0.7), (0.84, 0.23, 2.5), dark, 0.1)
    box("Hover pad emitter", (x * 2.44, -0.78, -0.7), (0.56, 0.08, 1.6), cyan, 0.04)
    box("Tail fin", (x * 1.18, 0.72, -2.73), (0.22, 1.45, 1.2), armor, 0.08)
    box("Fin accent", (x * 1.18, 1.43, -2.72), (0.25, 0.12, 0.9), orange, 0.04)

    # Twin rear turbine housings with visible nested nozzle rings.
    engine_x = x * 2.46
    cylinder("Turbine outer shell", (engine_x, -0.11, -3.55), 0.51, 1.12, dark)
    cylinder("Turbine armor ring", (engine_x, -0.11, -4.12), 0.53, 0.13, orange)
    cylinder("Turbine inner throat", (engine_x, -0.11, -4.205), 0.35, 0.12, black)
    cylinder("Turbine plasma core", (engine_x, -0.11, -4.28), 0.21, 0.08, cyan)
    box("Rear engine bridge", (x * 1.88, 0.42, -3.37), (0.72, 0.15, 0.68), armor, 0.08)

    # Faceted intake slots and cyan locator lamps.
    box("Nose intake recess", (x * 0.91, 0.47, 2.80), (0.68, 0.09, 0.92), black, 0.08)
    for slat in range(3):
        box("Intake vane", (x * 0.91, 0.535, 2.50 + slat * 0.28),
            (0.56, 0.045, 0.075), cyan if slat == 0 else armor, 0.022)
    box("Nose locator", (x * 1.13, 0.34, 3.83), (0.15, 0.11, 0.68), orange, 0.035)
    box("Side armor seam", (x * 3.205, 0.10, -0.35), (0.08, 0.12, 3.9), dark, 0.025)
    for panel in range(5):
        box("Sponson gill", (x * 3.22, 0.20, -1.48 + panel * 0.44),
            (0.07, 0.06, 0.22), cyan if panel == 0 else armor, 0.025)

# Twin nose rails and central sensor slit.
for side in (-1, 1):
    box("Nose blade", (side * 0.42, 0.31, 3.62), (0.11, 0.09, 1.28), cyan, 0.035)
box("Central sensor", (0, 0.36, 4.02), (0.43, 0.08, 0.13), orange, 0.035)

# Join to a single mesh with material submeshes; this stays compact in Unity.
bpy.ops.object.select_all(action="DESELECT")
for obj in parts:
    obj.select_set(True)
bpy.context.view_layer.objects.active = parts[0]
bpy.ops.object.join()
craft = bpy.context.object
craft.name = "EclipseRacer"
craft.data.name = "EclipseRacerMesh"
craft.location = (0, 0, 0)
craft.rotation_euler = (0, 0, 0)
craft.scale = (1, 1, 1)

bpy.ops.object.mode_set(mode="EDIT")
bpy.ops.mesh.select_all(action="SELECT")
bpy.ops.mesh.normals_make_consistent(inside=False)
bpy.ops.object.mode_set(mode="OBJECT")

bpy.ops.object.select_all(action="DESELECT")
craft.select_set(True)
bpy.context.view_layer.objects.active = craft
bpy.ops.wm.obj_export(
    filepath=OUT,
    export_selected_objects=True,
    export_materials=True,
    export_uv=False,
    export_normals=False,
    export_triangulated_mesh=True,
    export_pbr_extensions=True,
    forward_axis="Y",
    up_axis="Z",
)
print("ECLIPSE RACER OBJ EXPORTED", OUT, "vertices", len(craft.data.vertices), "faces", len(craft.data.polygons))
