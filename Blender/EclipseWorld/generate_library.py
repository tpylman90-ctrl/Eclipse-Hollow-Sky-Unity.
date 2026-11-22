"""Generate Eclipse's short-run Blender environment kit.

Blender coordinates follow the game convention: X sideways, Y up, +Z forward.
Exports are Unity-friendly OBJ/MTL with one shared, baked-looking facade atlas.
Original procedural art; no third-party meshes or textures.
"""
import bpy
import base64
import math
import os
import random
from array import array
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / "Assets/Resources/EclipseWorld"
OUT.mkdir(parents=True, exist_ok=True)
random.seed(2407)

def clear_scene():
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete(use_global=False)

def mat(name, color, metallic=0.25, roughness=0.45, emission=None):
    m = bpy.data.materials.new(name)
    m.diffuse_color = (*color, 1)
    m.use_nodes = True
    bs = m.node_tree.nodes.get("Principled BSDF")
    bs.inputs["Base Color"].default_value = (*color, 1)
    bs.inputs["Metallic"].default_value = metallic
    bs.inputs["Roughness"].default_value = roughness
    if emission:
        bs.inputs["Emission Color"].default_value = (*emission, 1)
        bs.inputs["Emission Strength"].default_value = 2.0
    return m

def make_facade_atlas():
    """A seamless 256px facade tile: lit windows, seams, and color variation."""
    size = 256
    image = bpy.data.images.new("Eclipse_FacadeAtlas", width=size, height=size, alpha=False)
    image.colorspace_settings.name = "sRGB"
    pixels = array("f")
    for y in range(size):
        row = (y // 32) % 8
        for x in range(size):
            col = (x // 32) % 8
            ix, iy = x % 32, y % 32
            seed = ((x * 73856093) ^ (y * 19349663) ^ 2407) & 255
            noise = (seed / 255.0 - 0.5) * 0.035
            # Deep navy composite panels with metal reveals and cyan glazing.
            if ix in (0, 1) or iy in (0, 1):
                c = (0.035, 0.075, 0.12)
            elif 5 <= ix <= 26 and 5 <= iy <= 24:
                warm = ((col + row * 3) % 13 == 0)
                if warm:
                    c = (0.95, 0.31, 0.075)
                else:
                    c = (0.045, 0.53 + (row % 3) * 0.055, 0.88)
            elif iy in (27, 28):
                c = (0.12, 0.23, 0.31)
            else:
                c = (0.065 + noise, 0.115 + noise, 0.17 + noise)
            pixels.extend((max(0, min(1, c[0] + noise)),
                           max(0, min(1, c[1] + noise)),
                           max(0, min(1, c[2] + noise)), 1.0))
    image.pixels.foreach_set(pixels)
    image.filepath_raw = str(OUT / "Eclipse_FacadeAtlas.png")
    image.file_format = "PNG"
    image.save()
    source_copy = Path(__file__).resolve().parent / "Eclipse_FacadeAtlas.png.base64"
    source_copy.write_text(base64.b64encode((OUT / "Eclipse_FacadeAtlas.png").read_bytes()).decode("ascii"))
    return image

ATLAS = make_facade_atlas()

def facade_material():
    m = mat("Eclipse_Facade_BakedColor", (0.07, 0.14, 0.22), 0.42, 0.34)
    nodes, links = m.node_tree.nodes, m.node_tree.links
    tex = nodes.new("ShaderNodeTexImage")
    tex.image = ATLAS
    tex.interpolation = "Linear"
    tex.extension = "REPEAT"
    tex.location = (-350, 100)
    uv = nodes.new("ShaderNodeTexCoord")
    uv.location = (-560, 100)
    links.new(uv.outputs["UV"], tex.inputs["Vector"])
    links.new(tex.outputs["Color"], nodes.get("Principled BSDF").inputs["Base Color"])
    return m

FACADE = facade_material()
CERAMIC = mat("Eclipse_Ceramic_Blue", (0.035, 0.075, 0.12), 0.52, 0.32)
METAL = mat("Eclipse_Brushed_Titanium", (0.30, 0.42, 0.52), 0.78, 0.25)
DARK = mat("Eclipse_Road_Carbon", (0.018, 0.029, 0.043), 0.22, 0.58)
CYAN = mat("Eclipse_Neon_Cyan", (0.01, 0.45, 0.75), 0.12, 0.23, (0.01, 0.65, 1.0))
AMBER = mat("Eclipse_Neon_Amber", (0.72, 0.18, 0.035), 0.12, 0.28, (1.0, 0.16, 0.025))
GLASS = mat("Eclipse_Dark_Glass", (0.012, 0.055, 0.09), 0.66, 0.16)

def finish(obj, material, name=None):
    obj.name = name or obj.name
    obj.data.materials.append(material)
    for p in obj.data.polygons:
        p.use_smooth = False
    return obj

def cube(name, loc, size, material, bevel=0.0):
    bpy.ops.mesh.primitive_cube_add(size=1, location=loc)
    o = bpy.context.object
    o.name = name
    o.dimensions = size
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    finish(o, material)
    if bevel:
        b = o.modifiers.new("Machined edge chamfers", "BEVEL")
        b.width = bevel
        b.segments = 2
        bpy.context.view_layer.objects.active = o
        bpy.ops.object.modifier_apply(modifier=b.name)
        n = o.modifiers.new("Weighted corner normals", "WEIGHTED_NORMAL")
        bpy.ops.object.modifier_apply(modifier=n.name)
    return o

def uv_repeat(obj, scale_u=1.0, scale_v=1.0):
    bpy.context.view_layer.objects.active = obj
    obj.select_set(True)
    bpy.ops.object.mode_set(mode="EDIT")
    bpy.ops.mesh.select_all(action="SELECT")
    bpy.ops.uv.cube_project(cube_size=1.0)
    bpy.ops.object.mode_set(mode="OBJECT")
    uv = obj.data.uv_layers.active
    for d in uv.data:
        d.uv.x *= scale_u
        d.uv.y *= scale_v
    obj.select_set(False)

def export_model(name, objects):
    # Bake all transforms, then merge static geometry by material. Unity gets
    # a handful of renderer groups per asset instead of dozens of tiny draws.
    for o in objects:
        bpy.ops.object.select_all(action="DESELECT")
        o.select_set(True)
        bpy.context.view_layer.objects.active = o
        bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
    groups = []
    materials = []
    for o in objects:
        if o.type == "MESH" and o.data.materials:
            material = o.data.materials[0]
            if material not in materials:
                materials.append(material)
    for material in materials:
        group = [o for o in bpy.context.scene.objects
                 if o.type == "MESH" and o.data.materials and o.data.materials[0] == material]
        if len(group) > 1:
            bpy.ops.object.select_all(action="DESELECT")
            for o in group:
                o.select_set(True)
            bpy.context.view_layer.objects.active = group[0]
            bpy.ops.object.join()
            group = [bpy.context.object]
        groups.extend(group)
    bpy.ops.object.select_all(action="DESELECT")
    for o in groups:
        o.select_set(True)
    bpy.context.view_layer.objects.active = groups[0]
    path = OUT / (name + ".obj")
    bpy.ops.wm.obj_export(filepath=str(path), export_selected_objects=True,
        export_materials=True, export_uv=True, export_normals=True,
        export_triangulated_mesh=True, forward_axis="Y", up_axis="Z")
    for o in groups:
        o.select_set(False)
    print("ECLIPSE_ASSET_OK", name, "material_meshes", len(groups), "obj_bytes", path.stat().st_size)

def building_base(name, width, depth, height, detailed=False, variant=0):
    objects = []
    # Facade body receives the tiled atlas: its windows, panel seams and color
    # variation remain readable at distance without thousands of window meshes.
    body = cube(name + "_Facade", (0, height * 0.5, 0),
                (width, height, depth), FACADE, 0.22 if detailed else 0.10)
    uv_repeat(body, max(1, width / 7.0), max(1, height / 8.0))
    objects.append(body)
    # Setback crown gives the repeated archetype a recognizable engineered top.
    objects += [
        cube(name + "_RoofArmor", (0, height + 0.35, 0),
             (width * 0.80, 0.70, depth * 0.82), CERAMIC, 0.12),
        cube(name + "_RoofLight", (0, height + 0.74, 0),
             (width * 0.48, 0.11, depth * 0.50), CYAN if variant % 2 == 0 else AMBER, 0.04),
    ]
    if detailed:
        # Full-height corner spars, floor ledges, and recessed service bays.
        for sx in (-1, 1):
            for sz in (-1, 1):
                objects.append(cube(name + "_CornerSpar", (sx * width * 0.46, height * 0.5, sz * depth * 0.46),
                    (0.58, height * 0.96, 0.58), METAL, 0.10))
        floor_count = max(6, int(height / 5.0))
        for floor in range(1, floor_count):
            y = height * floor / floor_count
            objects.append(cube(name + "_FloorBand", (0, y, 0), (width * 1.025, 0.22, depth * 1.025), CERAMIC, 0.045))
            # Alternating cyan and amber edge strips create visual rhythm.
            edge_mat = CYAN if (floor + variant) % 4 else AMBER
            objects.append(cube(name + "_BandGlow", (0, y + 0.12, -depth * 0.516), (width * 0.74, 0.065, 0.055), edge_mat, 0.02))
        # Distinct angled crown fins and a center light blade.
        for x in (-width * 0.31, 0, width * 0.31):
            fin = cube(name + "_CrownFin", (x, height + 1.25, depth * 0.12),
                       (0.34, 2.4 + variant * 0.35, depth * 0.18), METAL, 0.06)
            fin.rotation_euler[2] = (-0.12 if x < 0 else 0.12)
            objects.append(fin)
        objects.append(cube(name + "_VerticalSignal", (width * 0.31, height * 0.53, -depth * 0.51),
            (0.16, height * 0.63, 0.08), CYAN if variant % 2 == 0 else AMBER, 0.035))
        # Layered entrance bay visible from the low flight camera.
        objects.append(cube(name + "_EntryRecess", (0, 2.4, -depth * 0.515),
            (width * 0.32, 4.2, 0.12), DARK, 0.08))
        objects.append(cube(name + "_EntryGlass", (0, 2.45, -depth * 0.525),
            (width * 0.23, 3.2, 0.08), GLASS, 0.06))
    return objects

def build_standard():
    clear_scene()
    objs = building_base("StandardTower", 14, 12, 22, detailed=False)
    export_model("StandardTower", objs)

def build_hero_a():
    clear_scene()
    objs = building_base("HeroTowerA", 16, 14, 54, detailed=True, variant=0)
    # Cantilevered, stacked observation decks on the camera-facing side.
    for y in (13, 28, 43):
        objs.append(cube("HeroA_ObservationDeck", (0, y, -8.8), (19, 0.48, 5.2), METAL, 0.12))
        objs.append(cube("HeroA_DeckLight", (0, y + 0.28, -11.15), (17.5, 0.10, 0.08), AMBER, 0.025))
    export_model("HeroTowerA", objs)

def build_hero_b():
    clear_scene()
    objs = building_base("HeroTowerB", 13, 15, 46, detailed=True, variant=1)
    # Offset crown makes this silhouette read as a different authored landmark.
    objs += [
        cube("HeroB_CrownWingLeft", (-6.2, 48, -0.2), (0.55, 6.8, 16), CYAN, 0.09),
        cube("HeroB_CrownWingRight", (6.2, 48, -0.2), (0.55, 4.2, 16), AMBER, 0.09),
        cube("HeroB_RoofBridge", (0, 50.2, 0), (12.5, 0.62, 2.2), METAL, 0.12),
    ]
    for y in (10, 22, 34):
        objs.append(cube("HeroB_FacadeBridge", (0, y, -8.25), (16.8, 0.42, 0.46), CYAN, 0.08))
    export_model("HeroTowerB", objs)

def build_road():
    clear_scene()
    objs = []
    # 39m authored module. The top lane is a continuous beveled multi-layer deck.
    objs.append(cube("Road_MainDeck", (0, 3.45, 19.5), (15.2, 0.9, 39), DARK, 0.22))
    objs.append(cube("Road_UnderslungArmor", (0, 2.75, 19.5), (13.6, 0.62, 38.2), CERAMIC, 0.16))
    objs.append(cube("Road_CenterRaceSurface", (0, 3.93, 19.5), (12.3, 0.07, 38.2), DARK, 0.08))
    for side in (-1, 1):
        x = side * 7.22
        objs.append(cube("Road_TitaniumShoulder", (x, 4.02, 19.5), (0.38, 0.28, 38.4), METAL, 0.07))
        objs.append(cube("Road_CyanEdge", (side * 7.08, 4.19, 19.5), (0.11, 0.09, 38.1), CYAN, 0.035))
        # Repeated underside ribs provide a visible engineered structure.
        for z in range(1, 39, 6):
            objs.append(cube("Road_CrossRib", (0, 2.34, float(z)), (14.0, 0.34, 0.62), METAL, 0.09))
            for sx in (-1, 1):
                objs.append(cube("Road_SupportWeb", (sx * 5.4, 1.63, float(z)), (0.24, 1.0, 0.36), CERAMIC, 0.07))
    # Dashed, slightly raised centerline and lane guidance marks.
    for z in range(2, 39, 7):
        objs.append(cube("Road_CenterDash", (0, 4.03, float(z)), (0.20, 0.035, 3.3), METAL, 0.02))
    export_model("RoadSegment", objs)

build_standard()
build_hero_a()
build_hero_b()
build_road()
print("ECLIPSE_BUILDING_LIBRARY_READY", str(OUT))
