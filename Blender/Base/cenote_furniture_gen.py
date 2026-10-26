# 拠点「セノーテの洞窟」の家具・器具を作るスクリプト（Blender 5.2）
# docs/modeling/base_furniture.md 準拠
# Step 3 細密化版: 岩彫り・流木の木目・編み籠の段・道具1つずつの造形・WorldLook 準拠の色むら/凹凸シェーダー・FBX書き出し
# Oct 18 Claude 細かさの版: add_box・add_cyl・add_tube_along_curve・add_lathe を cenote_detail.py の細かい部品に通した
#   （木＝板か角材か丸太、石＝角の丸い網目の塊、つる・ヤシ縄＝3本撚りの筋と巻いた縛り、回す形＝周りと輪郭を細かく）。DETAIL=False で元の形。
#   部品ごと 2 万三角形まで（いちばん重いのは BirdBasket 1.5 万）。
#   入れ子の書き出しで家具が 90° 倒れていたのを直した（Z が上のまま書き出し、Unity の Bake Axis Conversion）。
# 1 単位 ＝ 1 m、部屋のローカル座標は目印（Anchor）が原点、+X＝右、+Y＝横穴の奥（独立した部屋は北）、+Z＝上。
# 薄い部品も厚みを持たせた閉じたメッシュにし、滑らかな法線で書き出す。

import bpy, bmesh, math, random, os, sys, importlib
from mathutils import Vector, Matrix, Euler
from mathutils import noise as mnoise

# 細かい形の部品（板・角材・丸太・石・縛り・撚った縄）は cenote_detail.py にまとめた（Oct 18 Claude）
sys.path.insert(0, "C:/UnityProjects/ExtractionGame/Blender/Base")
import cenote_detail as D
importlib.reload(D)
DETAIL = True   # False で Argon の Step 3 の形（細かい部品を使わない）
FURN_ROCK_MATS = ("CenoteBase_Limestone", "CenoteBase_LimestoneWet", "CenoteBase_Coral", "CenoteBase_LavaStone",
                  "CenoteBase_Moss", "CenoteBase_Soil", "CenoteBase_Compost", "CenoteBase_Salt", "CenoteBase_BlackSalt")
ROPE_MATS = ("CenoteBase_Vine", "CenoteBase_Coir")

EXPORT_DIR = "C:/UnityProjects/ExtractionGame/Assets/Art/Models/Base/CenoteFurniture/"
BLEND_PATH = "C:/UnityProjects/ExtractionGame/Blender/Base/CenoteFurniture.blend"
CAVE_BLEND_PATH = "C:/UnityProjects/ExtractionGame/Blender/Base/CenoteBase.blend"
EXPORT = True
ROUGH_MODE = False


def nz(p):
    return mnoise.noise(p)


def fbm(p, octaves=3):
    val = 0.0
    amp = 0.5
    freq = 1.0
    for _ in range(octaves):
        val += amp * mnoise.noise(p * freq)
        freq *= 2.05
        amp *= 0.48
    return val


# ---------------------------------------------------------------- 全 42 マテリアル定義（docs/modeling/base_furniture.md 準拠）
PALETTE_HEX = {
    "CenoteBase_Limestone":     "#D9D2C0",
    "CenoteBase_LimestoneWet":  "#A9A390",
    "CenoteBase_Driftwood":     "#B9AE98",
    "CenoteBase_DriftwoodDark": "#8E7F68",
    "CenoteBase_Vine":          "#6B6A45",
    "CenoteBase_Coir":          "#8A6E4E",
    "CenoteBase_Rug":           "#8C7F5E",
    "CenoteBase_Leaf":          "#4E5F3A",
    "CenoteBase_Thatch":        "#A39466",
    "CenoteBase_Moss":          "#6E7C4A",
    "CenoteBase_BarkCloth":     "#D8CBAE",
    "CenoteBase_Noren":         "#D8CBAE",
    "CenoteBase_ClothRed":      "#A8655A",
    "CenoteBase_ClothIndigo":   "#5E6E8A",
    "CenoteBase_ClothOchre":    "#C2A55E",
    "CenoteBase_ClamShell":     "#E3DCCB",
    "CenoteBase_ClamInner":     "#EEE8DD",
    "CenoteBase_Conch":         "#D9B79A",
    "CenoteBase_WhaleBone":     "#E4DCC8",
    "CenoteBase_Coconut":       "#6E5240",
    "CenoteBase_Clay":          "#B5876A",
    "CenoteBase_LavaStone":     "#4A4744",
    "CenoteBase_Obsidian":      "#2A2A30",
    "CenoteBase_Coral":         "#CFC4B4",
    "CenoteBase_Soil":          "#5A4A3C",
    "CenoteBase_Compost":       "#463A30",
    "CenoteBase_Ash":           "#9A968F",
    "CenoteBase_Salt":          "#EEEBE4",
    "CenoteBase_BlackSalt":     "#4F4B4E",
    "RockSalt_Crystal":         "#F4BECB",  # 採取物の岩塩マテリアルを再利用
    "CenoteBase_Pile":          "#C9C2B0",
    "CenoteBase_Water":         "#3F7F86",
    "CenoteBase_DyeRed":        "#8E5550",
    "CenoteBase_DyeIndigo":     "#4F6A80",
    "CenoteBase_DyeOchre":      "#A99350",
    "CenoteBase_Copper":        "#B07A4E",
    "CenoteBase_Brass":         "#A8873E",
    "CenoteBase_LampFlame":     "#FFB860",
    "CenoteBase_Feather":       "#EEE8DC",
    "CenoteBase_Charcoal":      "#3A3634",
    "CenoteBase_Bone":          "#E6DFCF",
    "CenoteBase_Map":           "#D8CBAE",
}

MOTTLE_HEX = {
    "CenoteBase_Conch":     "#A5785A",
    "CenoteBase_WhaleBone": "#B9B09C",
    "CenoteBase_LavaStone": "#3A3836",
    "CenoteBase_Copper":    "#6F8A72",
    "CenoteBase_Brass":     "#6F8A72",
}

# マテリアルごとの表面凹凸（頂点ノイズ粒度 m）の基準値
MAT_GRAIN = {
    "CenoteBase_Limestone":     0.014,
    "CenoteBase_LimestoneWet":  0.015,
    "CenoteBase_Coral":         0.020,
    "CenoteBase_LavaStone":     0.012,
    "CenoteBase_Soil":          0.016,
    "CenoteBase_Compost":       0.018,
    "CenoteBase_Moss":          0.012,
    "CenoteBase_Driftwood":     0.006,
    "CenoteBase_DriftwoodDark": 0.006,
    "CenoteBase_Vine":          0.004,
    "CenoteBase_Coir":          0.005,
    "CenoteBase_Rug":           0.004,
    "CenoteBase_Thatch":        0.007,
    "CenoteBase_BarkCloth":     0.003,
    "CenoteBase_Noren":         0.004,
    "CenoteBase_ClothRed":      0.003,
    "CenoteBase_ClothIndigo":   0.003,
    "CenoteBase_ClothOchre":    0.003,
    "CenoteBase_Clay":          0.004,
    "CenoteBase_WhaleBone":     0.008,
    "CenoteBase_Pile":          0.012,
}

MAT_LIST = sorted(PALETTE_HEX.keys())
MAT_ID = {name: i + 1 for i, name in enumerate(MAT_LIST)}
ID_TO_MAT = {i + 1: name for i, name in enumerate(MAT_LIST)}


def srgb_to_linear(c):
    return c / 12.92 if c <= 0.04045 else ((c + 0.055) / 1.055) ** 2.4


def hex_to_LinearRGB(hex_str):
    h = hex_str.lstrip('#')
    r = srgb_to_linear(int(h[0:2], 16) / 255.0)
    g = srgb_to_linear(int(h[2:4], 16) / 255.0)
    b = srgb_to_linear(int(h[4:6], 16) / 255.0)
    return (r, g, b)


def clear_scene():
    for o in list(bpy.data.objects):
        bpy.data.objects.remove(o, do_unlink=True)
    for m in list(bpy.data.meshes):
        bpy.data.meshes.remove(m)
    for mat in list(bpy.data.materials):
        bpy.data.materials.remove(mat)
    for lib in list(bpy.data.libraries):
        bpy.data.libraries.remove(lib)


def get_rough_material(name="CenoteFurniture_RoughGrey", color=(0.80, 0.78, 0.74)):
    m = bpy.data.materials.get(name) or bpy.data.materials.new(name)
    m.use_nodes = True
    nt = m.node_tree
    nt.nodes.clear()
    out = nt.nodes.new("ShaderNodeOutputMaterial")
    bsdf = nt.nodes.new("ShaderNodeBsdfPrincipled")
    bsdf.inputs["Base Color"].default_value = (*color, 1.0)
    bsdf.inputs["Roughness"].default_value = 0.78
    nt.links.new(bsdf.outputs["BSDF"], out.inputs["Surface"])
    return m


def get_palette_material(name):
    if ROUGH_MODE:
        return get_rough_material()
    m = bpy.data.materials.get(name)
    if m is not None:
        return m
    m = bpy.data.materials.new(name)
    m.use_nodes = True
    nt = m.node_tree
    nt.nodes.clear()
    out = nt.nodes.new("ShaderNodeOutputMaterial")
    bsdf = nt.nodes.new("ShaderNodeBsdfPrincipled")
    col = hex_to_LinearRGB(PALETTE_HEX.get(name, "#B9AE98"))
    m.diffuse_color = (*col, 1.0)
    bsdf.inputs["Base Color"].default_value = (*col, 1.0)

    if name == "CenoteBase_Obsidian":
        bsdf.inputs["Roughness"].default_value = 0.12
    elif name in ("CenoteBase_Water", "CenoteBase_DyeRed", "CenoteBase_DyeIndigo", "CenoteBase_DyeOchre", "CenoteBase_ClamInner"):
        bsdf.inputs["Roughness"].default_value = 0.22
    elif name in ("CenoteBase_Copper", "CenoteBase_Brass", "CenoteBase_Conch"):
        bsdf.inputs["Roughness"].default_value = 0.52
    else:
        bsdf.inputs["Roughness"].default_value = 0.80

    if name == "CenoteBase_LampFlame":
        if "Emission Color" in bsdf.inputs:
            bsdf.inputs["Emission Color"].default_value = (*col, 1.0)
            bsdf.inputs["Emission Strength"].default_value = 4.0
    elif name not in ("CenoteBase_Obsidian", "CenoteBase_Water", "CenoteBase_DyeRed", "CenoteBase_DyeIndigo", "CenoteBase_DyeOchre"):
        if name in MOTTLE_HEX:
            mcol = hex_to_LinearRGB(MOTTLE_HEX[name])
        else:
            mcol = (col[0] * 0.76, col[1] * 0.75, col[2] * 0.73)
        tc = nt.nodes.new("ShaderNodeTexCoord")
        noi = nt.nodes.new("ShaderNodeTexNoise")
        noi.inputs["Scale"].default_value = 10.0 if "Limestone" in name else 16.0
        noi.inputs["Detail"].default_value = 4.5
        noi.inputs["Roughness"].default_value = 0.65
        cr = nt.nodes.new("ShaderNodeValToRGB")
        cr.color_ramp.elements[0].position = 0.34
        cr.color_ramp.elements[0].color = (*col, 1.0)
        cr.color_ramp.elements[1].position = 0.66
        cr.color_ramp.elements[1].color = (*mcol, 1.0)
        nt.links.new(tc.outputs["Object"], noi.inputs["Vector"])
        nt.links.new(noi.outputs["Fac"], cr.inputs["Fac"])
        nt.links.new(cr.outputs["Color"], bsdf.inputs["Base Color"])

        noi_b = nt.nodes.new("ShaderNodeTexNoise")
        noi_b.inputs["Scale"].default_value = 38.0 if "Limestone" in name else 55.0
        noi_b.inputs["Detail"].default_value = 5.0
        bump = nt.nodes.new("ShaderNodeBump")
        bump.inputs["Strength"].default_value = 0.32 if ("Limestone" in name or name in ("CenoteBase_LavaStone", "CenoteBase_Coral")) else 0.20
        bump.inputs["Distance"].default_value = 0.025
        nt.links.new(tc.outputs["Object"], noi_b.inputs["Vector"])
        nt.links.new(noi_b.outputs["Fac"], bump.inputs["Height"])
        nt.links.new(bump.outputs["Normal"], bsdf.inputs["Normal"])

    nt.links.new(bsdf.outputs["BSDF"], out.inputs["Surface"])
    return m


def smart_uv(me):
    bm = bmesh.new()
    bm.from_mesh(me)
    uv = bm.loops.layers.uv.verify()
    for f in bm.faces:
        n = f.normal
        ax = max(range(3), key=lambda i: abs(n[i]))
        for l in f.loops:
            c = l.vert.co
            l[uv].uv = (c.y, c.z) if ax == 0 else ((c.x, c.z) if ax == 1 else (c.x, c.y))
    bm.to_mesh(me)
    bm.free()


def make_empty(name, loc=(0, 0, 0), rot_euler=(0, 0, 0), parent=None, size=0.25):
    emp = bpy.data.objects.new(name, None)
    emp.empty_display_type = 'PLAIN_AXES'
    emp.empty_display_size = size
    emp.location = loc
    emp.rotation_euler = Euler(rot_euler, 'XYZ')
    bpy.context.scene.collection.objects.link(emp)
    if parent is not None:
        emp.parent = parent
    return emp


def make_obj(name, bm, location=(0, 0, 0), rot_euler=(0, 0, 0), parent=None, mat=None, default_mat="CenoteBase_Driftwood"):
    me = bpy.data.meshes.new(name)
    bm.normal_update()
    if mat is not None:
        me.materials.append(mat)
        for f in bm.faces:
            f.material_index = 0
            f.smooth = True
    elif ROUGH_MODE:
        me.materials.append(get_rough_material())
        for f in bm.faces:
            f.material_index = 0
            f.smooth = True
    else:
        default_id = MAT_ID.get(default_mat, MAT_ID["CenoteBase_Driftwood"])
        used_ids = set()
        for f in bm.faces:
            if f.material_index <= 0 or f.material_index not in ID_TO_MAT:
                f.material_index = default_id
            used_ids.add(f.material_index)
        sorted_ids = sorted(used_ids)
        id_to_slot = {}
        for slot, mid in enumerate(sorted_ids):
            me.materials.append(get_palette_material(ID_TO_MAT[mid]))
            id_to_slot[mid] = slot
        for f in bm.faces:
            f.material_index = id_to_slot[f.material_index]
            f.smooth = True

    bm.to_mesh(me)
    bm.free()
    smart_uv(me)
    ob = bpy.data.objects.new(name, me)
    ob.location = location
    ob.rotation_euler = Euler(rot_euler, 'XYZ')
    bpy.context.scene.collection.objects.link(ob)
    if parent is not None:
        ob.parent = parent
    return ob


# ---------------------------------------------------------------- 基本ジオメトリ追加ヘルパー（細密化・表面凹凸つき）
def merge_bm(target_bm, src_bm, mat=None, mat_name=None):
    if mat is not None:
        bmesh.ops.transform(src_bm, matrix=mat, verts=list(src_bm.verts))
    if mat_name is not None and mat_name in MAT_ID:
        mid = MAT_ID[mat_name]
        for f in src_bm.faces:
            if f.material_index <= 0:
                f.material_index = mid
    tmp = bpy.data.meshes.new("_tmp")
    src_bm.to_mesh(tmp)
    src_bm.free()
    target_bm.from_mesh(tmp)
    bpy.data.meshes.remove(tmp)


def add_box(bm, size, center=(0, 0, 0), rot_euler=(0, 0, 0), bevel_ratio=0.12, warp=0.0, bevel_segs=1, mat_name=None, grain=None, cuts=None):
    w, d, h = size
    if DETAIL and not ROUGH_MODE and min(w, d, h) > 0.012:
        xform = Matrix.Translation(center) @ Euler(rot_euler, 'XYZ').to_matrix().to_4x4()
        seed = D.seed_vec(center)
        if mat_name in D.WOOD_MATS:
            if D.is_plank_shape(size) and max(w, d, h) > 0.14:
                L, W, T, rot = D.plank_axes(size)
                cb = D.plank_cb(L, W, T, seed, step=0.12 if L > 1.2 else (0.07 if L > 0.4 else 0.05),
                                bend=0.010 + abs(warp) * 0.5, groove=0.010 if T > 0.03 else 0.006,
                                chip=min(0.022, L * 0.04))
                merge_bm(bm, cb, xform @ rot, mat_name=mat_name)
            else:
                merge_bm(bm, D.beam_cb(size, seed, step=0.06 if max(w, d, h) < 0.6 else 0.09), xform, mat_name=mat_name)
            return
        if mat_name in FURN_ROCK_MATS:
            m = min(w, d, h)
            st = max(0.035, min(0.12, max(w, d, h) / 12.0))
            cb = D.rock_cb(size, seed, step=st, round_k=max(0.12, min(0.45, bevel_ratio * 1.3)),
                           rough=0.09 if bevel_ratio < 0.2 else 0.13, fine=min(0.016, m * 0.08))
            merge_bm(bm, cb, xform, mat_name=mat_name)
            return
    cb = bmesh.new()
    bmesh.ops.create_cube(cb, size=1.0)
    for v in cb.verts:
        v.co = Vector((v.co.x * w, v.co.y * d, v.co.z * h))
    max_dim = max(w, d, h)
    if bevel_ratio > 0 and bevel_segs > 0:
        bmesh.ops.bevel(cb, geom=list(cb.edges), offset=min(w, d, h) * bevel_ratio,
                        segments=1, affect="EDGES", profile=0.5, clamp_overlap=True)
    c_num = cuts if cuts is not None else (2 if max_dim > 0.45 else (1 if max_dim > 0.22 else 0))
    if c_num > 0:
        long_edges = [e for e in cb.edges if (e.verts[0].co - e.verts[1].co).length > max_dim * 0.42]
        if long_edges:
            bmesh.ops.subdivide_edges(cb, edges=long_edges, cuts=c_num, use_grid_fill=True)
    if warp != 0.0:
        for v in cb.verts:
            v.co.z += warp * (v.co.x / max(0.1, w * 0.5)) ** 2
            v.co.y += warp * 0.5 * math.sin(v.co.x * 3.0)
    g_amp = grain if grain is not None else min(min(w, d, h) * 0.16, MAT_GRAIN.get(mat_name, 0.004))
    if g_amp > 1e-4:
        seed_v = Vector(center) * 3.1
        for v in cb.verts:
            p = v.co * 6.5 + seed_v
            v.co += Vector((nz(p), nz(p + Vector((4.1, 1.7, 8.3))), nz(p + Vector((9.2, 5.4, 2.6))))) * g_amp
    mat = Matrix.Translation(center) @ Euler(rot_euler, 'XYZ').to_matrix().to_4x4()
    merge_bm(bm, cb, mat, mat_name=mat_name)


def add_cyl(bm, r_bot, r_top, height, segs=18, center=(0, 0, 0), rot_euler=(0, 0, 0), bevel_ends=True, bevel_segs=1, mat_name=None, grain=None):
    if DETAIL and not ROUGH_MODE:
        xform = Matrix.Translation(center) @ Euler(rot_euler, 'XYZ').to_matrix().to_4x4()
        seed = D.seed_vec(center)
        r_max = max(r_bot, r_top)
        if mat_name in D.WOOD_MATS and height > 0.12 and r_max > 0.012:
            merge_bm(bm, D.log_cb(r_bot, r_top, height, seed, segs=max(segs, min(18, int(round(math.tau * r_max / 0.03)))),
                                  ring_step=0.06), xform, mat_name=mat_name)
            return
        if mat_name in ROPE_MATS and height <= 0.12 and r_max > height * 0.4 and r_max > 0.015:
            cord = max(0.006, min(0.014, height * 0.25))
            cb = D.lashing_cb((0, 0, 0), (0, 0, 1), max(0.004, r_max - cord), width=height,
                              turns=max(2.0, min(5.0, height / (cord * 1.6))), cord=cord)
            merge_bm(bm, cb, xform, mat_name=mat_name)
            return
        segs = max(segs, min(32, int(round(math.tau * r_max / 0.02))))
    cb = bmesh.new()
    bmesh.ops.create_cone(cb, cap_ends=True, cap_tris=False, segments=segs,
                          radius1=r_bot, radius2=r_top, depth=height)
    if bevel_ends and min(r_bot, r_top, height) > 0.015 and bevel_segs > 0:
        cap_edges = [e for e in cb.edges if abs(e.verts[0].co.z - e.verts[1].co.z) < 1e-4]
        if cap_edges:
            bmesh.ops.bevel(cb, geom=cap_edges, offset=min(r_bot, r_top, height) * 0.10,
                            segments=1, affect="EDGES", profile=0.5, clamp_overlap=True)
    if height > 0.28:
        v_edges = [e for e in cb.edges if abs(e.verts[0].co.z - e.verts[1].co.z) > height * 0.45]
        if v_edges:
            bmesh.ops.subdivide_edges(cb, edges=v_edges, cuts=2 if height > 0.65 else 1)
    g_amp = grain if grain is not None else min(min(r_bot, r_top) * 0.14, MAT_GRAIN.get(mat_name, 0.003))
    if g_amp > 1e-4:
        seed_v = Vector(center) * 2.7
        for v in cb.verts:
            p = v.co * 6.0 + seed_v
            v.co.x += nz(p) * g_amp
            v.co.y += nz(p + Vector((5.2, 3.1, 1.4))) * g_amp
    mat = Matrix.Translation(center) @ Euler(rot_euler, 'XYZ').to_matrix().to_4x4()
    merge_bm(bm, cb, mat, mat_name=mat_name)


def _catmull(points, radii, step):
    """点列をなめらかに細かく打ち直す（Catmull-Rom）"""
    n = len(points)
    if n < 2:
        return points, radii
    out_p, out_r = [], []
    for i in range(n - 1):
        p0 = points[max(0, i - 1)]
        p1 = points[i]
        p2 = points[i + 1]
        p3 = points[min(n - 1, i + 2)]
        L = (p2 - p1).length
        k = max(1, int(math.ceil(L / step)))
        for j in range(k):
            t = j / k
            t2, t3 = t * t, t * t * t
            p = 0.5 * ((2 * p1) + (-p0 + p2) * t + (2 * p0 - 5 * p1 + 4 * p2 - p3) * t2 + (-p0 + 3 * p1 - 3 * p2 + p3) * t3)
            out_p.append(p)
            out_r.append(radii[i] + (radii[i + 1] - radii[i]) * t)
    out_p.append(points[-1])
    out_r.append(radii[-1])
    return out_p, out_r


def add_tube_along_curve(bm, points, radii, segs=12, cap=True, mat_name=None, twist_rib=0.0):
    if DETAIL and not ROUGH_MODE and len(points) >= 2:
        r_avg = sum(radii) / len(radii)
        if mat_name in ROPE_MATS:
            # 撚った縄・つる：3本撚りのらせんの筋、0.4×太さごとに打ち直す
            pts, rads = _catmull(points, radii, max(0.010, min(0.05, r_avg * 0.8)))
            sg = max(6, min(10, segs))
            cb = D.tube_cb(pts, rads, segs=sg, cap=cap, rib=0.16, rib_freq=3,
                           rib_twist=math.tau / max(0.1, r_avg * 10.0) * 3.0, lumpy=0.04, seed=points[0])
        else:
            pts, rads = _catmull(points, radii, max(0.02, min(0.08, r_avg * 1.3)))
            sg = max(segs, min(18, int(round(math.tau * r_avg / 0.025))))
            lumpy = 0.07 if mat_name in D.WOOD_MATS else 0.03
            cb = D.tube_cb(pts, rads, segs=sg, cap=cap, rib=twist_rib * 0.6, rib_freq=3, rib_twist=4.0,
                           lumpy=lumpy, seed=points[0])
        merge_bm(bm, cb, mat_name=mat_name)
        return
    cb = bmesh.new()
    n = len(points)
    loops = []
    for i, (pt, r) in enumerate(zip(points, radii)):
        if i == 0:
            tang = (points[1] - points[0]).normalized()
        elif i == n - 1:
            tang = (points[-1] - points[-2]).normalized()
        else:
            tang = (points[i + 1] - points[i - 1]).normalized()
        up = Vector((0, 0, 1)) if abs(tang.z) < 0.9 else Vector((1, 0, 0))
        bx = tang.cross(up).normalized()
        by = bx.cross(tang).normalized()
        ring = []
        for s in range(segs):
            ang = (s / segs) * math.tau
            r_mod = r * (1.0 + twist_rib * math.sin(ang * 3 + i * 1.4) + 0.06 * nz(pt * 5.0 + Vector((s, i, 0))))
            p = pt + (bx * math.cos(ang) + by * math.sin(ang)) * r_mod
            ring.append(cb.verts.new(p))
        loops.append(ring)

    for i in range(n - 1):
        la, lb = loops[i], loops[i + 1]
        for s in range(segs):
            sn = (s + 1) % segs
            cb.faces.new((la[s], la[sn], lb[sn], lb[s]))

    if cap and len(loops) >= 2:
        cb.faces.new(reversed(loops[0]))
        cb.faces.new(loops[-1])
    merge_bm(bm, cb, mat_name=mat_name)


def add_lathe(bm, profile_rz, segs=24, center=(0, 0, 0), rot_euler=(0, 0, 0), wave_amp=0.0, wave_freq=6, mat_name=None, grain=None):
    if DETAIL and not ROUGH_MODE and len(profile_rz) >= 3:
        # 回す形：周りを 1.5 倍に分け、輪郭の点の間に中点を足す（なめらかな丸みと、ろくろ目の段）
        r_max = max(r for r, _ in profile_rz)
        segs = max(segs, min(40, int(round(math.tau * r_max / 0.018))))
        dense = [profile_rz[0]]
        for i in range(1, len(profile_rz)):
            (r0, z0), (r1, z1) = profile_rz[i - 1], profile_rz[i]
            if r0 > 1e-4 and r1 > 1e-4 and math.hypot(r1 - r0, z1 - z0) > 0.03:
                rm = (r0 + r1) * 0.5
                if mat_name == "CenoteBase_Clay":
                    rm += 0.004 * math.sin(i * 2.1)   # ろくろ目
                dense.append((rm, (z0 + z1) * 0.5))
            dense.append((r1, z1))
        profile_rz = dense
    cb = bmesh.new()
    loops = []
    g_amp = grain if grain is not None else MAT_GRAIN.get(mat_name, 0.003) * 0.5
    seed_v = Vector(center) * 2.3
    for ir, (r, z) in enumerate(profile_rz):
        if r <= 1e-4:
            v_pole = cb.verts.new(Vector((0, 0, z)))
            loops.append([v_pole])
        else:
            ring = []
            for s in range(segs):
                ang = (s / segs) * math.tau
                p_n = Vector((math.cos(ang) * r, math.sin(ang) * r, z)) * 5.0 + seed_v
                rw = r + wave_amp * math.sin(ang * wave_freq + ir * 0.7) + g_amp * nz(p_n)
                zw = z + g_amp * 0.4 * nz(p_n + Vector((3, 7, 2)))
                ring.append(cb.verts.new(Vector((math.cos(ang) * rw, math.sin(ang) * rw, zw))))
            loops.append(ring)

    for i in range(len(loops) - 1):
        la, lb = loops[i], loops[i + 1]
        if len(la) == 1 and len(lb) > 1:
            vp = la[0]
            for s in range(segs):
                sn = (s + 1) % segs
                cb.faces.new((vp, lb[sn], lb[s]))
        elif len(lb) == 1 and len(la) > 1:
            vp = lb[0]
            for s in range(segs):
                sn = (s + 1) % segs
                cb.faces.new((la[s], la[sn], vp))
        elif len(la) > 1 and len(lb) > 1:
            for s in range(segs):
                sn = (s + 1) % segs
                cb.faces.new((la[s], la[sn], lb[sn], lb[s]))

    mat = Matrix.Translation(center) @ Euler(rot_euler, 'XYZ').to_matrix().to_4x4()
    merge_bm(bm, cb, mat, mat_name=mat_name)


# 編み籠（丸籠・長籠）の細密ヘルパー：横の編み段＋縦の骨組み＋底の敷物
def add_woven_round_basket_bm(bm, r_bot, r_top, height, center=(0, 0, 0), n_rings=4, n_ribs=8):
    cx, cy, cz = center
    add_cyl(bm, r_bot * 0.92, r_bot * 0.92, 0.03, segs=10, center=(cx, cy, cz + 0.015), bevel_ends=False, mat_name="CenoteBase_Rug")
    ring_h = height / n_rings
    for ir in range(n_rings):
        t = (ir + 0.5) / n_rings
        r_here = r_bot + (r_top - r_bot) * t
        z_here = cz + (ir + 0.5) * ring_h
        add_lathe(bm, [
            (r_here * 0.88, -ring_h * 0.48),
            (r_here * 1.03, 0.0),
            (r_here * 0.88,  ring_h * 0.48),
        ], segs=12, center=(cx, cy, z_here), wave_amp=0.008, wave_freq=n_ribs, mat_name="CenoteBase_Vine")
    # 縁の太い三つ編みリング
    add_lathe(bm, [
        (r_top * 0.86, height - 0.025),
        (r_top * 1.06, height),
        (r_top * 0.86, height + 0.022),
    ], segs=12, center=(cx, cy, cz), wave_amp=0.006, wave_freq=n_ribs, mat_name="CenoteBase_Vine")
    # 縦の骨（つるの縦芯）
    for k in range(n_ribs):
        ang = (k / n_ribs) * math.tau
        p0 = Vector((cx + math.cos(ang) * r_bot * 0.96, cy + math.sin(ang) * r_bot * 0.96, cz + 0.02))
        p1 = Vector((cx + math.cos(ang) * r_top * 0.99, cy + math.sin(ang) * r_top * 0.99, cz + height))
        add_tube_along_curve(bm, [p0, p1], [0.012, 0.014], segs=4, cap=False, mat_name="CenoteBase_Vine")


def add_woven_long_basket_bm(bm, width=0.66, depth=0.32, height=0.19, center=(0, 0, 0), rot_z=0.0):
    cb = bmesh.new()
    # 底のパンダナス敷物＋内部の2枚の仕切り板
    add_box(cb, (width - 0.02, depth - 0.02, 0.028), center=(0, 0, 0.014), bevel_ratio=0.0, bevel_segs=0, cuts=0, mat_name="CenoteBase_Rug")
    for sx_div in (-width * 0.16, width * 0.16):
        add_box(cb, (0.026, depth - 0.03, height - 0.02), center=(sx_div, 0, height * 0.5), bevel_ratio=0.0, bevel_segs=0, cuts=0, mat_name="CenoteBase_Vine")
    # 3段の横編みつるストリップ（外周の前後・左右）
    for iz in range(3):
        zh = 0.036 + iz * 0.056
        for sy in (-depth * 0.47, depth * 0.47):
            add_box(cb, (width, 0.032, 0.044), center=(0, sy, zh), bevel_ratio=0.0, warp=0.005 * (1 if iz % 2 == 0 else -1), bevel_segs=0, cuts=0, mat_name="CenoteBase_Vine")
        for sx in (-width * 0.48, width * 0.48):
            add_box(cb, (0.030, depth, 0.044), center=(sx, 0, zh), bevel_ratio=0.0, bevel_segs=0, cuts=0, mat_name="CenoteBase_Vine")
    # 四隅＋仕切り位置の縦支柱 8本
    for sx in (-width * 0.48, -width * 0.16, width * 0.16, width * 0.48):
        for sy in (-depth * 0.47, depth * 0.47):
            add_cyl(cb, 0.018, 0.016, height + 0.02, segs=4, center=(sx, sy, height * 0.5), bevel_ends=False, mat_name="CenoteBase_DriftwoodDark")
    # 前縁の I / II / III の刻み目
    for comp_idx, cx in enumerate((-width * 0.32, 0.0, width * 0.32)):
        for k in range(comp_idx + 1):
            nx = cx + (k - comp_idx * 0.5) * 0.036
            add_box(cb, (0.016, 0.042, 0.035), center=(nx, -depth * 0.50, height + 0.005), bevel_ratio=0.0, bevel_segs=0, cuts=0, mat_name="CenoteBase_Driftwood")
    mat = Matrix.Translation(center) @ Euler((0, 0, rot_z), 'XYZ').to_matrix().to_4x4()
    merge_bm(bm, cb, mat)


# 共通部品を bmesh に追加するサブ関数（部屋の中でも再利用）
def add_conch_lamp_bm(bm, center=(0, 0, 0), rot_z=0.0):
    prof_conch = [(0.0, 0.0), (0.04, 0.03), (0.08, 0.09), (0.11, 0.16), (0.125, 0.22), (0.09, 0.28), (0.05, 0.33), (0.02, 0.36), (0.0, 0.38)]
    add_lathe(bm, prof_conch, segs=18, center=(center[0], center[1] + 0.12, center[2] + 0.10),
              rot_euler=(math.pi / 2, 0.2, rot_z + 0.4), wave_amp=0.011, wave_freq=6, mat_name="CenoteBase_Conch")
    # 巻貝を受ける小石・つるの座
    add_cyl(bm, 0.09, 0.08, 0.025, segs=10, center=(center[0], center[1] + 0.06, center[2] + 0.012), bevel_ends=False, mat_name="CenoteBase_Vine")
    prof_flame = [(0.0, 0.0), (0.04, 0.03), (0.045, 0.08), (0.02, 0.13), (0.0, 0.16)]
    add_lathe(bm, prof_flame, segs=12, center=(center[0] + 0.03, center[1] - 0.02, center[2] + 0.11), mat_name="CenoteBase_LampFlame")


def add_brass_wall_lamp_bm(bm, center=(0, 0, 0), rot_z=0.0):
    cb = bmesh.new()
    # 壁当て板＋上下のリベット鋲
    add_box(cb, (0.085, 0.032, 0.23), center=(0, 0.12, 0.0), bevel_ratio=0.22, bevel_segs=2, mat_name="CenoteBase_Brass")
    for rz_riv in (-0.08, 0.08):
        add_cyl(cb, 0.014, 0.010, 0.02, segs=8, center=(0, 0.10, rz_riv), rot_euler=(math.pi / 2, 0, 0), bevel_ends=False, mat_name="CenoteBase_Copper")
    pts_arm = [Vector((0, 0.11, -0.04)), Vector((0, 0.04, -0.07)), Vector((0, -0.04, -0.05)), Vector((0, -0.10, 0.0))]
    add_tube_along_curve(cb, pts_arm, [0.024, 0.020, 0.020, 0.022], segs=10, mat_name="CenoteBase_Brass")
    for i in range(4):
        add_cyl(cb, 0.028, 0.028, 0.014, segs=8, center=(0, 0.03 - i * 0.018, -0.058), rot_euler=(math.pi / 2, 0, 0), bevel_ends=False, mat_name="CenoteBase_Vine")
    add_lathe(cb, [(0.0, 0.0), (0.06, 0.0), (0.095, 0.04), (0.085, 0.075), (0.06, 0.08), (0.0, 0.05)], segs=14, center=(0, -0.10, -0.01), wave_amp=0.004, wave_freq=8, mat_name="CenoteBase_Brass")
    add_lathe(cb, [(0.0, 0.0), (0.04, 0.03), (0.045, 0.08), (0.02, 0.14), (0.0, 0.17)], segs=12, center=(0, -0.10, 0.07), mat_name="CenoteBase_LampFlame")
    mat = Matrix.Translation(center) @ Euler((0, 0, rot_z), 'XYZ').to_matrix().to_4x4()
    merge_bm(bm, cb, mat)


def add_clam_shell_bm(bm, width=1.30, depth=0.80, height=0.30, center=(0, 0, 0), rot_z=0.0, wear_front=False, n_u=32, n_v=14):
    cb = bmesh.new()
    outer_grid = []
    inner_grid = []
    rx0 = width * 0.5
    ry0 = depth * 0.52
    for iv in range(n_v):
        tv = iv / (n_v - 1)
        row_out = []
        row_in = []
        for iu in range(n_u):
            tu = (iu / (n_u - 1)) * 2.0 - 1.0
            ang = tu * (math.pi * 0.48)
            rad_x = rx0 * (0.25 + 0.75 * tv)
            rad_y = ry0 * (0.25 + 0.75 * tv)
            # 放射状の深いヒダ＋成長線の段差
            rib = math.cos(tu * 6.0 * math.pi) * (0.042 * width * tv) + 0.008 * math.sin(tv * 18.0)
            wear = (-0.048 * max(0.0, 1.0 - (tu * 2.5) ** 2) * (tv ** 2)) if wear_front else 0.0
            x = math.sin(ang) * rad_x
            y = depth * 0.42 - math.cos(ang) * rad_y * 1.8
            z_bowl = 0.03 + height * (tv ** 1.4) + wear
            row_out.append(cb.verts.new(Vector((x, y, max(0.0, z_bowl + rib - 0.035)))))
            row_in.append(cb.verts.new(Vector((x * 0.94, y * 0.94, z_bowl + 0.018))))
        outer_grid.append(row_out)
        inner_grid.append(row_in)

    id_out = MAT_ID["CenoteBase_ClamShell"]
    id_in = MAT_ID["CenoteBase_ClamInner"]

    for iv in range(n_v - 1):
        for iu in range(n_u - 1):
            cb.faces.new((outer_grid[iv][iu], outer_grid[iv + 1][iu], outer_grid[iv + 1][iu + 1], outer_grid[iv][iu + 1])).material_index = id_out
            cb.faces.new((inner_grid[iv][iu], inner_grid[iv][iu + 1], inner_grid[iv + 1][iu + 1], inner_grid[iv + 1][iu])).material_index = id_in
    for iu in range(n_u - 1):
        cb.faces.new((outer_grid[-1][iu], inner_grid[-1][iu], inner_grid[-1][iu + 1], outer_grid[-1][iu + 1])).material_index = id_out
        cb.faces.new((inner_grid[0][iu], outer_grid[0][iu], outer_grid[0][iu + 1], inner_grid[0][iu + 1])).material_index = id_out
    for iv in range(n_v - 1):
        cb.faces.new((inner_grid[iv][0], inner_grid[iv + 1][0], outer_grid[iv + 1][0], outer_grid[iv][0])).material_index = id_out
        cb.faces.new((outer_grid[iv][-1], outer_grid[iv + 1][-1], inner_grid[iv + 1][-1], inner_grid[iv][-1])).material_index = id_out

    mat = Matrix.Translation(center) @ Euler((0, 0, rot_z), 'XYZ').to_matrix().to_4x4()
    merge_bm(bm, cb, mat)


# ================================================================ PART 1: 12 KIT PARTS (部品箱 12個: 各 300〜1,500 tris)
def build_kit(base_y=-22.0):
    kit_root = make_empty("CenoteFurniture_Kit", loc=(0, 0, 0))
    kit_objs = []

    # 1. Plank (木目と割り溝の入った流木板 長・中・短)
    bm = bmesh.new()
    add_box(bm, (1.1, 0.24, 0.05), center=(0, -0.22, 0.03), bevel_ratio=0.35, warp=0.015, bevel_segs=1, mat_name="CenoteBase_Driftwood", cuts=6)
    add_box(bm, (0.85, 0.22, 0.05), center=(0, 0.04, 0.03), bevel_ratio=0.35, warp=-0.012, bevel_segs=1, mat_name="CenoteBase_DriftwoodDark", cuts=5)
    add_box(bm, (0.65, 0.22, 0.05), center=(0, 0.28, 0.03), bevel_ratio=0.35, warp=0.010, bevel_segs=1, mat_name="CenoteBase_Driftwood", cuts=4)
    kit_objs.append(make_obj("CenoteFurniture_Kit_01_Plank", bm, location=(-8.8, base_y, 0), parent=kit_root, default_mat="CenoteBase_Driftwood"))

    # 2. Post (節くれ立った直柱と二股柱)
    bm = bmesh.new()
    add_cyl(bm, 0.06, 0.05, 0.85, segs=16, center=(-0.22, 0, 0.425), mat_name="CenoteBase_Driftwood")
    pts_stem = [Vector((0.20, 0, 0.0)), Vector((0.20, 0.02, 0.28)), Vector((0.20, 0, 0.52))]
    add_tube_along_curve(bm, pts_stem, [0.065, 0.058, 0.055], segs=14, mat_name="CenoteBase_Driftwood", twist_rib=0.05)
    for sx in (-1, 1):
        pts_br = [Vector((0.20, 0, 0.48)), Vector((0.20 + sx * 0.12, 0, 0.68)), Vector((0.20 + sx * 0.22, 0, 0.88))]
        add_tube_along_curve(bm, pts_br, [0.05, 0.045, 0.04], segs=12, mat_name="CenoteBase_Driftwood", twist_rib=0.05)
    kit_objs.append(make_obj("CenoteFurniture_Kit_02_Post", bm, location=(-7.2, base_y, 0), parent=kit_root, default_mat="CenoteBase_Driftwood"))

    # 3. Disc (年輪段差つきの流木円盤 大・小)
    bm = bmesh.new()
    add_cyl(bm, 0.24, 0.24, 0.07, segs=24, center=(-0.22, 0, 0.24), rot_euler=(math.pi / 2, 0, 0), mat_name="CenoteBase_Driftwood", bevel_segs=2)
    add_cyl(bm, 0.16, 0.16, 0.05, segs=20, center=(0.22, 0, 0.16), rot_euler=(math.pi / 2, 0, 0), mat_name="CenoteBase_Driftwood", bevel_segs=2)
    kit_objs.append(make_obj("CenoteFurniture_Kit_03_Disc", bm, location=(-5.6, base_y, 0), parent=kit_root, default_mat="CenoteBase_Driftwood"))

    # 4. Vine loop and cord (撚り目つきのつる巻きと輪)
    bm = bmesh.new()
    for i in range(6):
        z0 = 0.04 + i * 0.032
        pts = [Vector((-0.22 + math.cos(a) * 0.09, math.sin(a) * 0.09, z0)) for a in [k / 12 * math.tau for k in range(13)]]
        add_tube_along_curve(bm, pts, [0.018] * 13, segs=8, mat_name="CenoteBase_Vine", twist_rib=0.08)
    pts_loop = [Vector((0.20 + math.cos(a) * 0.12, 0, 0.22 + math.sin(a) * 0.16)) for a in [k / 16 * math.tau for k in range(17)]]
    add_tube_along_curve(bm, pts_loop, [0.022] * 17, segs=8, mat_name="CenoteBase_Vine", twist_rib=0.08)
    kit_objs.append(make_obj("CenoteFurniture_Kit_04_VineCord", bm, location=(-4.0, base_y, 0), parent=kit_root, default_mat="CenoteBase_Vine"))

    # 5. Coir rope (太いヤシ縄のとぐろ)
    bm = bmesh.new()
    pts_coil, rads_coil = [], []
    for k in range(46):
        t = k / 45
        ang = t * 3.5 * math.tau
        r = 0.22 - t * 0.06
        pts_coil.append(Vector((math.cos(ang) * r, math.sin(ang) * r, 0.035 + t * 0.18)))
        rads_coil.append(0.032)
    add_tube_along_curve(bm, pts_coil, rads_coil, segs=10, mat_name="CenoteBase_Coir", twist_rib=0.10)
    kit_objs.append(make_obj("CenoteFurniture_Kit_05_CoirRope", bm, location=(-2.4, base_y, 0), parent=kit_root, default_mat="CenoteBase_Coir"))

    # 6. Clay jar (ろくろ目と首縄つきの素焼き壺 大・小)
    bm = bmesh.new()
    prof_big = [(0.0, 0.0), (0.12, 0.0), (0.16, 0.08), (0.18, 0.18), (0.17, 0.28), (0.14, 0.35), (0.11, 0.40), (0.13, 0.44), (0.0, 0.45)]
    add_lathe(bm, prof_big, segs=22, center=(-0.22, 0, 0), wave_amp=0.004, wave_freq=8, mat_name="CenoteBase_Clay")
    add_cyl(bm, 0.12, 0.12, 0.025, segs=16, center=(-0.22, 0, 0.39), bevel_ends=False, mat_name="CenoteBase_Vine")
    add_cyl(bm, 0.135, 0.12, 0.04, segs=18, center=(-0.22, 0, 0.46), mat_name="CenoteBase_Driftwood")
    add_box(bm, (0.08, 0.03, 0.03), center=(-0.22, 0, 0.49), bevel_ratio=0.25, mat_name="CenoteBase_DriftwoodDark")
    prof_sm = [(0.0, 0.0), (0.07, 0.0), (0.105, 0.10), (0.095, 0.18), (0.065, 0.23), (0.075, 0.25), (0.0, 0.25)]
    add_lathe(bm, prof_sm, segs=18, center=(0.20, 0, 0), wave_amp=0.003, wave_freq=7, mat_name="CenoteBase_Clay")
    add_cyl(bm, 0.08, 0.07, 0.03, segs=16, center=(0.20, 0, 0.26), mat_name="CenoteBase_Driftwood")
    kit_objs.append(make_obj("CenoteFurniture_Kit_06_ClayJar", bm, location=(-0.8, base_y, 0), parent=kit_root, default_mat="CenoteBase_Clay"))

    # 7. Baskets (編み目の段と縦骨つきの丸籠S/M ＋ 細長い3仕切り籠)
    bm = bmesh.new()
    add_woven_round_basket_bm(bm, 0.12, 0.15, 0.25, center=(-0.55, 0.22, 0), n_rings=4, n_ribs=8)
    add_woven_round_basket_bm(bm, 0.20, 0.25, 0.35, center=( 0.45, 0.22, 0), n_rings=5, n_ribs=10)
    add_woven_long_basket_bm(bm, width=0.70, depth=0.35, height=0.20, center=(0.0, -0.22, 0.0))
    kit_objs.append(make_obj("CenoteFurniture_Kit_07_Baskets", bm, location=(0.9, base_y, 0), parent=kit_root, default_mat="CenoteBase_Vine"))

    # 8. Salt bin (板組みの脚付き塩箱)
    bm = bmesh.new()
    for sx in (-0.21, 0.21):
        for sy in (-0.16, 0.16):
            add_cyl(bm, 0.038, 0.032, 0.32, segs=8, center=(sx, sy, 0.16), mat_name="CenoteBase_DriftwoodDark")
    for iz, z_p in enumerate((0.36, 0.45, 0.54)):
        p_mat = "CenoteBase_DriftwoodDark" if iz == 1 else "CenoteBase_Driftwood"
        for sy in (-0.185, 0.185):
            add_box(bm, (0.50, 0.035, 0.088), center=(0, sy, z_p), bevel_ratio=0.2, mat_name=p_mat)
        for sx in (-0.235, 0.235):
            add_box(bm, (0.035, 0.40, 0.088), center=(sx, 0, z_p), bevel_ratio=0.2, mat_name=p_mat)
    add_box(bm, (0.48, 0.38, 0.03), center=(0, 0, 0.32), bevel_ratio=0.15, mat_name="CenoteBase_Driftwood")
    for px in (-0.17, 0.0, 0.17):
        add_box(bm, (0.165, 0.43, 0.04), center=(px, 0, 0.615), bevel_ratio=0.2, mat_name="CenoteBase_Driftwood")
    add_box(bm, (0.16, 0.055, 0.045), center=(0, 0, 0.655), bevel_ratio=0.25, mat_name="CenoteBase_DriftwoodDark")
    kit_objs.append(make_obj("CenoteFurniture_Kit_08_SaltBin", bm, location=(2.6, base_y, 0), parent=kit_root, default_mat="CenoteBase_Driftwood"))

    # 9. Tags (穴あき流木ステークと縫い目つき樹皮布札)
    bm = bmesh.new()
    add_box(bm, (0.14, 0.028, 0.24), center=(-0.14, 0, 0.14), bevel_ratio=0.25, mat_name="CenoteBase_Driftwood", cuts=2)
    add_cyl(bm, 0.032, 0.032, 0.035, segs=10, center=(-0.14, 0, 0.22), rot_euler=(math.pi / 2, 0, 0), bevel_ends=False, mat_name="CenoteBase_Vine")
    add_box(bm, (0.16, 0.018, 0.22), center=(0.14, 0, 0.13), rot_euler=(0.05, 0.08, 0), bevel_ratio=0.2, warp=0.008, mat_name="CenoteBase_BarkCloth", cuts=2)
    add_cyl(bm, 0.028, 0.028, 0.08, segs=10, center=(0.14, 0, 0.26), mat_name="CenoteBase_Vine")
    kit_objs.append(make_obj("CenoteFurniture_Kit_09_Tags", bm, location=(4.0, base_y, 0), parent=kit_root, default_mat="CenoteBase_BarkCloth"))

    # 10. Conch lamp
    bm = bmesh.new()
    add_conch_lamp_bm(bm, center=(0, 0, 0))
    kit_objs.append(make_obj("CenoteFurniture_Kit_10_ConchLamp", bm, location=(5.3, base_y, 0), parent=kit_root, default_mat="CenoteBase_Conch"))

    # 11. Brass wall lamp
    bm = bmesh.new()
    add_brass_wall_lamp_bm(bm, center=(0, 0, 0.22))
    kit_objs.append(make_obj("CenoteFurniture_Kit_11_BrassWallLamp", bm, location=(6.6, base_y, 0), parent=kit_root, default_mat="CenoteBase_Brass"))

    # 12. Pile (粒感のある柔らかい山)
    bm = bmesh.new()
    prof_pile = [(0.0, 0.0), (0.48, 0.0), (0.50, 0.08), (0.47, 0.18), (0.40, 0.28), (0.29, 0.38), (0.16, 0.45), (0.06, 0.49), (0.0, 0.50)]
    add_lathe(bm, prof_pile, segs=32, center=(0, 0, 0), wave_amp=0.032, wave_freq=7, mat_name="CenoteBase_Pile", grain=0.015)
    kit_objs.append(make_obj("CenoteFurniture_Kit_12_Pile", bm, location=(8.1, base_y, 0), parent=kit_root, default_mat="CenoteBase_Pile"))

    return kit_root, kit_objs


# ================================================================ PART 2: 10 ROOMS (S1〜S3 + Carved + Pivots)
def make_room_hierarchy(room_name):
    root = make_empty(f"CenoteFurniture_{room_name}")
    carved = make_empty(f"{room_name}_Carved", parent=root)
    s1 = make_empty(f"{room_name}_S1", parent=root)
    s2 = make_empty(f"{room_name}_S2", parent=root)
    s3 = make_empty(f"{room_name}_S3", parent=root)
    return root, carved, s1, s2, s3


# 1. MapTable (1階中央西寄り、段階なし)
def build_room_maptable():
    root, carved, s1, s2, s3 = make_room_hierarchy("MapTable")

    # パンダナスの編み敷物（外縁のつる縁取り＋四隅の留め縫い）
    bm_rug = bmesh.new()
    add_box(bm_rug, (3.0, 2.8, 0.03), center=(0, 0, 0.015), bevel_ratio=0.2, bevel_segs=2, mat_name="CenoteBase_Rug", grain=0.006, cuts=4)
    for sy in (-1.38, 1.38):
        add_box(bm_rug, (3.02, 0.06, 0.036), center=(0, sy, 0.018), bevel_ratio=0.3, bevel_segs=1, mat_name="CenoteBase_Vine", cuts=3)
    for sx in (-1.48, 1.48):
        add_box(bm_rug, (0.06, 2.82, 0.036), center=(sx, 0, 0.018), bevel_ratio=0.3, bevel_segs=1, mat_name="CenoteBase_Vine", cuts=3)
    make_obj("MapTable_Rug", bm_rug, parent=s1, default_mat="CenoteBase_Rug")

    bm_hero = bmesh.new()
    add_cyl(bm_hero, 0.38, 0.28, 0.34, segs=18, center=(0, 0, 0.17), mat_name="CenoteBase_DriftwoodDark", grain=0.012)
    for i in range(6):
        ang = (i / 6) * math.tau + 0.2
        pts_root = [
            Vector((math.cos(ang) * 0.22, math.sin(ang) * 0.22, 0.22)),
            Vector((math.cos(ang + 0.12) * 0.52, math.sin(ang + 0.12) * 0.52, 0.10)),
            Vector((math.cos(ang - 0.08) * 0.82, math.sin(ang - 0.08) * 0.82, 0.03)),
        ]
        add_tube_along_curve(bm_hero, pts_root, [0.115, 0.082, 0.045], segs=10, mat_name="CenoteBase_DriftwoodDark", twist_rib=0.08)
        # 枝分かれする細い副根
        pts_sub = [
            pts_root[1],
            Vector((math.cos(ang + 0.32) * 0.76, math.sin(ang + 0.32) * 0.76, 0.025)),
        ]
        add_tube_along_curve(bm_hero, pts_sub, [0.055, 0.028], segs=8, mat_name="CenoteBase_DriftwoodDark", twist_rib=0.06)

    # 食事の段（16枚の弧状流木板で組む）
    for i in range(16):
        a = (i / 16) * math.tau
        p_mat = "CenoteBase_DriftwoodDark" if i % 4 == 0 else "CenoteBase_Driftwood"
        add_box(bm_hero, (0.41, 0.26, 0.045), center=(math.cos(a) * 1.05, math.sin(a) * 1.05, 0.28),
                rot_euler=(0, 0, a + math.pi / 2), bevel_ratio=0.25, bevel_segs=1, mat_name=p_mat, cuts=1)

    # ヤシ殻の椀 4つ（中に塩スープと小さな木さじ）
    for i in range(4):
        a = (i / 4) * math.tau + 0.4
        bx, by = math.cos(a) * 1.07, math.sin(a) * 1.07
        add_lathe(bm_hero, [(0.0, 0.30), (0.06, 0.30), (0.085, 0.355), (0.065, 0.355), (0.0, 0.315)], segs=14, center=(bx, by, 0), mat_name="CenoteBase_Coconut")
        add_box(bm_hero, (0.018, 0.09, 0.012), center=(bx + 0.03, by, 0.355), rot_euler=(0.3, 0, a), bevel_ratio=0.25, bevel_segs=1, mat_name="CenoteBase_Driftwood")

    n_rim = 24
    for i in range(n_rim):
        a = (i / n_rim) * math.tau
        is_worn = abs(a + math.pi / 2) < 0.35
        wear_dip = 0.025 if is_worn else 0.0
        rh = 0.06 - wear_dip
        rmat = "CenoteBase_DriftwoodDark" if is_worn else "CenoteBase_Driftwood"
        add_box(bm_hero, (0.29, 0.075, rh), center=(math.cos(a) * 1.06, math.sin(a) * 1.06, 0.38 + rh * 0.5),
                rot_euler=(0, 0, a + math.pi / 2), bevel_ratio=0.25, bevel_segs=1, mat_name=rmat, cuts=1)
        if i % 3 == 0:
            add_cyl(bm_hero, 0.044, 0.044, 0.022, segs=8, center=(math.cos(a + 0.13) * 1.06, math.sin(a + 0.13) * 1.06, 0.38 + rh * 0.5),
                    rot_euler=(math.cos(a) * math.pi / 2, math.sin(a) * math.pi / 2, 0), bevel_ends=False, mat_name="CenoteBase_Vine")

    # ナメクジ駒4つ（胴体＋外套膜の段＋触角2本＋炭の目）
    for i in range(4):
        a = i * 1.5
        sx_p, sy_p = math.cos(a) * 0.45, math.sin(a) * 0.45
        add_box(bm_hero, (0.048, 0.11, 0.045), center=(sx_p, sy_p, 0.402), rot_euler=(0, 0, a), bevel_ratio=0.38, bevel_segs=2, mat_name="CenoteBase_DriftwoodDark")
        for ex in (-0.014, 0.014):
            ex_w = sx_p + math.cos(a) * ex - math.sin(a) * 0.042
            ey_w = sy_p + math.sin(a) * ex + math.cos(a) * 0.042
            add_cyl(bm_hero, 0.006, 0.004, 0.026, segs=6, center=(ex_w, ey_w, 0.43), bevel_ends=False, mat_name="CenoteBase_Driftwood")
            add_box(bm_hero, (0.010, 0.010, 0.010), center=(ex_w, ey_w, 0.444), bevel_ratio=0.2, bevel_segs=1, mat_name="CenoteBase_Charcoal")

    # 魚の骨のピン6本（旗は赤・藍・黄土の3色）
    flag_mats = ("CenoteBase_ClothRed", "CenoteBase_ClothIndigo", "CenoteBase_ClothOchre")
    for i in range(6):
        a = i * 1.05 + 0.3
        px, py = math.cos(a) * 0.68, math.sin(a) * 0.68
        add_cyl(bm_hero, 0.009, 0.003, 0.095, segs=6, center=(px, py, 0.42), bevel_ends=False, mat_name="CenoteBase_Bone")
        add_box(bm_hero, (0.046, 0.010, 0.030), center=(px + 0.022, py, 0.452), bevel_ratio=0.2, warp=0.006, bevel_segs=1, mat_name=flag_mats[i % 3])

    for gx, gy in ((-0.25, -0.98), (-0.20, -1.02), (-0.28, -1.01)):
        add_box(bm_hero, (0.022, 0.022, 0.018), center=(gx, gy, 0.42), rot_euler=(0.2, 0.3, 0.5), bevel_ratio=0.1, bevel_segs=1, mat_name="CenoteBase_Salt")

    # 横の流木柱に掛けた真鍮の壁灯 1つ
    add_cyl(bm_hero, 0.06, 0.048, 1.35, segs=12, center=(-1.35, 0.85, 0.675), mat_name="CenoteBase_Driftwood")
    for kz in (0.12, 0.98, 1.22):
        add_cyl(bm_hero, 0.064, 0.064, 0.03, segs=10, center=(-1.35, 0.85, kz), bevel_ends=False, mat_name="CenoteBase_Vine")
    add_brass_wall_lamp_bm(bm_hero, center=(-1.35, 0.72, 1.12), rot_z=0.4)
    make_obj("MapTable_Hero", bm_hero, parent=s1, default_mat="CenoteBase_Driftwood")

    bm_top = bmesh.new()
    add_cyl(bm_top, 1.05, 1.05, 0.06, segs=36, center=(0, 0, 0.35), mat_name="CenoteBase_Map")
    make_obj("MapTable_Top", bm_top, parent=s1, default_mat="CenoteBase_Map")

    return root


# 2. Trade (1階の横穴 5.4時, 幅 2.6 x 高さ 1.5, 奥行き 1.6)
def build_room_trade():
    root, carved, s1, s2, s3 = make_room_hierarchy("Trade")

    # Carved: 岩彫りの床・カウンター・棚＋石の腕木（コーベル）
    bm_c = bmesh.new()
    add_box(bm_c, (2.65, 1.75, 0.12), center=(0, 0.72, -0.06), bevel_ratio=0.08, bevel_segs=1, mat_name="CenoteBase_Limestone", grain=0.018, cuts=1)
    add_box(bm_c, (1.60, 0.50, 0.28), center=(-0.38, 0.25, 0.14), bevel_ratio=0.14, warp=-0.015, bevel_segs=1, mat_name="CenoteBase_Limestone", grain=0.014, cuts=1)
    for z_sh in (0.38, 0.84):
        add_box(bm_c, (2.40, 0.38, 0.065), center=(0.0, 1.32, z_sh), bevel_ratio=0.15, bevel_segs=0, mat_name="CenoteBase_Limestone", grain=0.012, cuts=1)
        for sx in (-1.06, 1.06):
            add_box(bm_c, (0.36, 0.85, 0.065), center=(sx, 0.78, z_sh), bevel_ratio=0.15, bevel_segs=0, mat_name="CenoteBase_Limestone", grain=0.012, cuts=1)
        # 棚を支える岩の突起 3か所
        for cx_cor in (-0.85, 0.0, 0.85):
            add_box(bm_c, (0.14, 0.28, 0.12), center=(cx_cor, 1.38, z_sh - 0.08), bevel_ratio=0.25, bevel_segs=0, cuts=0, mat_name="CenoteBase_LimestoneWet", grain=0.012)
    make_obj("Trade_CarvedMesh", bm_c, parent=carved, default_mat="CenoteBase_Limestone")

    bm_s1 = bmesh.new()
    bx0, by0 = 0.68, -0.18
    add_cyl(bm_s1, 0.24, 0.19, 0.07, segs=12, center=(bx0, by0, 0.035), mat_name="CenoteBase_Limestone", grain=0.010)
    add_tube_along_curve(bm_s1, [Vector((bx0, by0, 0.05)), Vector((bx0 + 0.02, by0, 0.35)), Vector((bx0, by0, 0.62))], [0.068, 0.058, 0.052], segs=10, mat_name="CenoteBase_Driftwood", twist_rib=0.06)
    for sx in (-1, 1):
        add_tube_along_curve(bm_s1, [Vector((bx0, by0, 0.58)), Vector((bx0 + sx * 0.14, by0, 0.76)), Vector((bx0 + sx * 0.22, by0, 0.92))], [0.048, 0.042, 0.036], segs=8, mat_name="CenoteBase_Driftwood", twist_rib=0.06)
    for kz in (0.10, 0.56):
        add_cyl(bm_s1, 0.072, 0.072, 0.03, segs=8, center=(bx0, by0, kz), bevel_ends=False, mat_name="CenoteBase_Vine")
    for z_bar in (0.03, 0.085):
        for dx in (0.28, 0.38):
            add_box(bm_s1, (0.075, 0.20, 0.052), center=(bx0 + dx, by0 - 0.08, z_bar), bevel_ratio=0.18, bevel_segs=0, cuts=0, mat_name="CenoteBase_Salt", grain=0.004)
    # ヤシの葉を7枚重ねたひさし屋根＋横梁＋支柱2本
    add_cyl(bm_s1, 0.032, 0.032, 1.68, segs=8, center=(-0.38, 0.48, 1.26), rot_euler=(0, math.pi / 2, 0), bevel_ends=False, mat_name="CenoteBase_DriftwoodDark")
    for idx_f in range(7):
        fx = -1.04 + idx_f * 0.22
        add_box(bm_s1, (0.26, 0.64, 0.04), center=(fx, 0.22, 1.22 + (idx_f % 2) * 0.015),
                rot_euler=(0.20, 0.04 * (idx_f - 3), 0), bevel_ratio=0.25, warp=-0.015, bevel_segs=0, mat_name="CenoteBase_Thatch", cuts=1)
    for sx in (-1.08, 0.32):
        add_cyl(bm_s1, 0.038, 0.032, 0.94, segs=8, center=(sx, 0.25, 0.75), bevel_ends=False, mat_name="CenoteBase_Driftwood")
        add_cyl(bm_s1, 0.046, 0.046, 0.04, segs=8, center=(sx, 0.25, 1.18), bevel_ends=False, mat_name="CenoteBase_Vine")
    # 二枚貝の書見台＋開いた樹皮布の帳面＋背表紙のつる綴じ＋イカ墨巻貝＋羽ペン
    add_clam_shell_bm(bm_s1, width=0.36, depth=0.28, height=0.12, center=(-0.45, 0.22, 0.29), rot_z=0.2, n_u=14, n_v=6)
    for page_i, p_ang in ((-1, 0.12), (1, -0.12)):
        add_box(bm_s1, (0.15, 0.22, 0.035), center=(-0.45 + page_i * 0.075, 0.20, 0.36), rot_euler=(-0.35, p_ang, 0.2), bevel_ratio=0.18, bevel_segs=0, cuts=1, mat_name="CenoteBase_BarkCloth")
    add_cyl(bm_s1, 0.018, 0.018, 0.23, segs=6, center=(-0.45, 0.20, 0.365), rot_euler=(math.pi / 2 - 0.35, 0, 0.2), bevel_ends=False, mat_name="CenoteBase_Vine")
    add_lathe(bm_s1, [(0.0, 0.0), (0.035, 0.0), (0.05, 0.04), (0.038, 0.08), (0.0, 0.09)], segs=10, center=(-0.12, 0.20, 0.28), wave_amp=0.004, wave_freq=5, mat_name="CenoteBase_Conch")
    add_cyl(bm_s1, 0.026, 0.026, 0.014, segs=8, center=(-0.12, 0.20, 0.365), bevel_ends=False, mat_name="CenoteBase_Charcoal")
    add_box(bm_s1, (0.035, 0.24, 0.014), center=(-0.10, 0.22, 0.42), rot_euler=(0.6, 0.2, 0.3), bevel_ratio=0.25, warp=0.01, bevel_segs=0, cuts=1, mat_name="CenoteBase_Feather")
    add_conch_lamp_bm(bm_s1, center=(-1.05, -0.15, 0.0), rot_z=0.5)
    make_obj("Trade_HeroAndCounter", bm_s1, parent=s1, default_mat="CenoteBase_Driftwood")

    bal_piv = make_empty("Trade_BalancePivot", loc=(bx0, by0, 0.88), parent=s1)
    bm_beam = bmesh.new()
    add_box(bm_beam, (1.14, 0.075, 0.068), center=(0, 0, 0), bevel_ratio=0.35, warp=0.014, bevel_segs=1, mat_name="CenoteBase_DriftwoodDark", cuts=2)
    for k in range(6):
        add_cyl(bm_beam, 0.050, 0.050, 0.016, segs=8, center=(0.16 + k * 0.022, 0, 0), rot_euler=(0, math.pi / 2, 0), bevel_ends=False, mat_name="CenoteBase_Vine")
    make_obj("Trade_BalanceBeam", bm_beam, parent=bal_piv, default_mat="CenoteBase_DriftwoodDark")

    for side_tag, sx in (("L", -0.50), ("R", 0.50)):
        pan_piv = make_empty(f"Trade_PanPivot_{side_tag}", loc=(sx, 0, 0), parent=bal_piv)
        bm_pan = bmesh.new()
        pan_z = -0.52
        add_clam_shell_bm(bm_pan, width=0.35, depth=0.32, height=0.08, center=(0, 0, pan_z), rot_z=0.0, n_u=14, n_v=6)
        for ca in (0, 2.09, 4.18):
            add_tube_along_curve(bm_pan, [Vector((0, 0, 0)), Vector((math.cos(ca) * 0.15, math.sin(ca) * 0.15, pan_z + 0.04))], [0.012, 0.012], segs=5, cap=False, mat_name="CenoteBase_Vine")
        make_obj(f"Trade_Pan_{side_tag}", bm_pan, parent=pan_piv, default_mat="CenoteBase_ClamShell")

    basket_slots = [
        (-0.76, 1.30, 0.41, 0.0, True),
        ( 0.00, 1.30, 0.41, 0.0, True),
        ( 0.76, 1.30, 0.41, 0.0, True),
        (-1.05, 0.88, 0.41, -math.pi / 2, True),
        (-1.05, 0.46, 0.41, -math.pi / 2, False),
        ( 1.05, 0.78, 0.41,  math.pi / 2, False),
        (-0.76, 1.30, 0.87, 0.0, True),
        ( 0.00, 1.30, 0.87, 0.0, True),
        ( 0.76, 1.30, 0.87, 0.0, True),
        (-1.05, 0.88, 0.87, -math.pi / 2, True),
        (-1.05, 0.46, 0.87, -math.pi / 2, False),
        ( 1.05, 0.78, 0.87,  math.pi / 2, False),
    ]
    bm_s2_bands = bmesh.new()
    bm_s3_bands = bmesh.new()

    for idx, (kx, ky, kz, krz, is_long) in enumerate(basket_slots, 1):
        bm_k = bmesh.new()
        if is_long:
            add_woven_long_basket_bm(bm_k, width=0.64, depth=0.30, height=0.18, center=(0, 0, 0))
            for cx in (-0.20, 0.0, 0.20):
                add_lathe(bm_k, [(0.0, 0.13), (0.085, 0.15), (0.055, 0.22), (0.0, 0.24)], segs=5, center=(cx, 0, 0), wave_amp=0.008, wave_freq=5, mat_name="CenoteBase_Pile")
            add_box(bm_k, (0.09, 0.022, 0.12), center=(0, -0.17, 0.10), bevel_ratio=0.0, bevel_segs=0, cuts=0, mat_name="CenoteBase_Driftwood")
            add_box(bm_k, (0.055, 0.018, 0.065), center=(0.12, -0.17, 0.12), bevel_ratio=0.0, bevel_segs=0, cuts=0, mat_name="CenoteBase_Leaf")
            add_box(bm_s2_bands, (0.66, 0.32, 0.055), center=(kx, ky, kz + 0.205), rot_euler=(0, 0, krz), bevel_ratio=0.0, bevel_segs=0, cuts=0, mat_name="CenoteBase_Vine")
            add_box(bm_s3_bands, (0.68, 0.33, 0.055), center=(kx, ky, kz + 0.265), rot_euler=(0, 0, krz), bevel_ratio=0.0, bevel_segs=0, cuts=0, mat_name="CenoteBase_Vine")
        else:
            add_woven_round_basket_bm(bm_k, 0.18, 0.22, 0.26, center=(0, 0, 0), n_rings=2, n_ribs=5)
            add_lathe(bm_k, [(0.0, 0.21), (0.16, 0.23), (0.10, 0.31), (0.0, 0.33)], segs=8, center=(0, 0, 0), wave_amp=0.010, wave_freq=6, mat_name="CenoteBase_Pile")
            add_box(bm_k, (0.08, 0.022, 0.11), center=(0, -0.23, 0.14), bevel_ratio=0.0, bevel_segs=0, cuts=0, mat_name="CenoteBase_Driftwood")
            add_box(bm_k, (0.055, 0.018, 0.065), center=(0.08, -0.23, 0.15), bevel_ratio=0.0, bevel_segs=0, cuts=0, mat_name="CenoteBase_Leaf")
            add_lathe(bm_s2_bands, [(0.20, -0.028), (0.235, 0.0), (0.20, 0.028)], segs=8, center=(kx, ky, kz + 0.29), rot_euler=(0, 0, krz), wave_amp=0.006, wave_freq=6, mat_name="CenoteBase_Vine")
            add_lathe(bm_s3_bands, [(0.205, -0.028), (0.242, 0.0), (0.205, 0.028)], segs=8, center=(kx, ky, kz + 0.35), rot_euler=(0, 0, krz), wave_amp=0.006, wave_freq=6, mat_name="CenoteBase_Vine")

        make_obj(f"Trade_Basket_{idx:02d}", bm_k, location=(kx, ky, kz), rot_euler=(0, 0, krz), parent=s1, default_mat="CenoteBase_Vine")

    make_obj("Trade_S2_BasketBands", bm_s2_bands, parent=s2, default_mat="CenoteBase_Vine")
    make_obj("Trade_S3_BasketBands", bm_s3_bands, parent=s3, default_mat="CenoteBase_Vine")
    return root


# 3. Nursery (1階、池の北のふち、+Y＝北)
def build_room_nursery():
    root, carved, s1, s2, s3 = make_room_hierarchy("Nursery")

    bm_c = bmesh.new()
    for idx_c, sx in enumerate((-1.35, -0.80, -0.25, 0.30, 0.85, 1.38)):
        m_c = "CenoteBase_Moss" if idx_c in (1, 4) else "CenoteBase_LimestoneWet"
        add_box(bm_c, (0.52, 0.24, 0.11), center=(sx, -0.58 + 0.03 * math.sin(idx_c), 0.055),
                rot_euler=(0, 0, 0.08 * math.cos(idx_c * 2)), bevel_ratio=0.35, bevel_segs=1, mat_name=m_c, grain=0.016, cuts=1)
    make_obj("Nursery_CarvedCurb", bm_c, parent=carved, default_mat="CenoteBase_LimestoneWet")

    def add_planter_arc_boxes(bm, specs, add_wear=False):
        for idx, (px, py, rz) in enumerate(specs):
            cb = bmesh.new()
            # 植え箱を4面の流木板（上下2段）＋四隅の杭＋つる縛りで組む
            for iz, zp in enumerate((0.065, 0.185)):
                p_mat = "CenoteBase_DriftwoodDark" if iz == 0 else "CenoteBase_Driftwood"
                for sy in (-0.23, 0.23):
                    add_box(cb, (0.80, 0.04, 0.11), center=(0, sy, zp), bevel_ratio=0.25, warp=0.005, bevel_segs=1, mat_name=p_mat, cuts=1)
                for sx in (-0.38, 0.38):
                    add_box(cb, (0.04, 0.50, 0.11), center=(sx, 0, zp), bevel_ratio=0.25, bevel_segs=0, mat_name=p_mat, cuts=1)
            for sx in (-0.38, 0.38):
                for sy in (-0.23, 0.23):
                    add_cyl(cb, 0.032, 0.028, 0.28, segs=6, center=(sx, sy, 0.14), bevel_ends=False, mat_name="CenoteBase_DriftwoodDark")
                    add_cyl(cb, 0.038, 0.038, 0.03, segs=6, center=(sx, sy, 0.22), bevel_ends=False, mat_name="CenoteBase_Vine")
            # 盛り上がった暗い土の塊
            add_box(cb, (0.72, 0.42, 0.21), center=(0, 0, 0.12), bevel_ratio=0.28, bevel_segs=1, mat_name="CenoteBase_Soil", grain=0.018, cuts=2)
            # 穴あき流木の名札ステーク＋若芽の葉 4枚
            add_box(cb, (0.08, 0.022, 0.22), center=(0.20, -0.10, 0.28), bevel_ratio=0.25, bevel_segs=0, cuts=0, mat_name="CenoteBase_Driftwood")
            add_cyl(cb, 0.026, 0.026, 0.03, segs=6, center=(0.20, -0.10, 0.35), rot_euler=(math.pi / 2, 0, 0), bevel_ends=False, mat_name="CenoteBase_Vine")
            for lx, ly, l_ang in [(-0.22, -0.05, 0.3), (-0.16, 0.06, -0.25), (-0.02, -0.04, 0.2), (0.04, 0.07, -0.3)]:
                add_box(cb, (0.12, 0.065, 0.16), center=(lx, ly, 0.28), rot_euler=(l_ang, 0.18, lx * 2), bevel_ratio=0.35, warp=0.01, bevel_segs=0, cuts=1, mat_name="CenoteBase_Leaf")
            # まわりの苔パッチ
            add_cyl(cb, 0.22, 0.15, 0.04, segs=10, center=(-0.28, -0.26, 0.02), bevel_ends=False, mat_name="CenoteBase_Moss", grain=0.012)
            mat = Matrix.Translation((px, py, 0)) @ Euler((0, 0, rz), 'XYZ').to_matrix().to_4x4()
            merge_bm(bm, cb, mat)
        if add_wear:
            for k in range(4):
                add_box(bm, (0.12, 0.12, 0.022), center=(-0.52, -0.30, 0.08 + k * 0.042), bevel_ratio=0.0, bevel_segs=0, cuts=0, mat_name="CenoteBase_Vine")

    bm_s1 = bmesh.new()
    add_planter_arc_boxes(bm_s1, [(-0.90, -0.10, 0.21), (0.0, 0.0, 0.0), (0.90, -0.10, -0.21)], add_wear=True)
    # 割った流木の樋 (1.2 m、左右の縁と中の水路)
    for sx_g in (-0.055, 0.055):
        add_box(bm_s1, (0.05, 1.20, 0.07), center=(-0.25 + sx_g, -0.45, 0.22), rot_euler=(0.08, 0, 0.15), bevel_ratio=0.25, bevel_segs=1, mat_name="CenoteBase_Driftwood", cuts=2)
    add_box(bm_s1, (0.07, 1.18, 0.03), center=(-0.25, -0.45, 0.20), rot_euler=(0.08, 0, 0.15), bevel_ratio=0.2, bevel_segs=0, mat_name="CenoteBase_Water", cuts=1)
    # ヤシ殻のひしゃく
    add_lathe(bm_s1, [(0.0, 0.0), (0.07, 0.0), (0.095, 0.07), (0.07, 0.07), (0.0, 0.02)], segs=12, center=(0.35, -0.42, 0.02), mat_name="CenoteBase_Coconut")
    add_cyl(bm_s1, 0.016, 0.012, 0.28, segs=8, center=(0.48, -0.42, 0.07), rot_euler=(0, math.pi / 2 - 0.2, 0), bevel_ends=False, mat_name="CenoteBase_DriftwoodDark")
    # くされの囲い（四隅の杭＋3段の横木＋つる縛り＋堆肥の山）
    cx, cy = 1.55, 0.38
    for sx in (-0.33, 0.33):
        for sy in (-0.33, 0.33):
            add_cyl(bm_s1, 0.035, 0.03, 0.34, segs=6, center=(cx + sx, cy + sy, 0.17), bevel_ends=False, mat_name="CenoteBase_DriftwoodDark")
    for z_slat in (0.07, 0.16, 0.25):
        for sy in (-0.33, 0.33):
            add_box(bm_s1, (0.72, 0.035, 0.065), center=(cx, cy + sy, z_slat), bevel_ratio=0.25, bevel_segs=0, cuts=1, mat_name="CenoteBase_Driftwood")
        for sx in (-0.33, 0.33):
            add_box(bm_s1, (0.035, 0.72, 0.065), center=(cx + sx, cy, z_slat), bevel_ratio=0.25, bevel_segs=0, cuts=1, mat_name="CenoteBase_Driftwood")
    add_lathe(bm_s1, [(0.0, 0.0), (0.31, 0.0), (0.29, 0.18), (0.18, 0.31), (0.0, 0.36)], segs=16, center=(cx, cy, 0.0), wave_amp=0.025, wave_freq=6, mat_name="CenoteBase_Compost", grain=0.018)
    make_obj("Nursery_S1_Mesh", bm_s1, parent=s1, default_mat="CenoteBase_Driftwood")

    bm_s2 = bmesh.new()
    add_planter_arc_boxes(bm_s2, [(-1.72, -0.34, 0.42), (1.72, -0.34, -0.42)])
    make_obj("Nursery_S2_Mesh", bm_s2, parent=s2, default_mat="CenoteBase_Driftwood")

    bm_s3 = bmesh.new()
    add_planter_arc_boxes(bm_s3, [(-2.42, -0.76, 0.62), (2.42, -0.76, -0.62)])
    make_obj("Nursery_S3_Mesh", bm_s3, parent=s3, default_mat="CenoteBase_Driftwood")

    return root


# 4. Dye (1階回廊下の横穴 4時, 幅 2.0 x 高さ 1.2, 奥行き 1.6)
def build_room_dye():
    root, carved, s1, s2, s3 = make_room_hierarchy("Dye")

    bm_c = bmesh.new()
    add_box(bm_c, (2.10, 1.75, 0.12), center=(0, 0.72, -0.06), bevel_ratio=0.08, bevel_segs=1, mat_name="CenoteBase_LimestoneWet", grain=0.018, cuts=3)
    for sx_ch in (-0.09, 0.09):
        add_box(bm_c, (0.07, 0.88, 0.08), center=(-0.32 + sx_ch, 0.32, 0.04), rot_euler=(0.05, 0, -0.45), bevel_ratio=0.3, bevel_segs=1, mat_name="CenoteBase_LimestoneWet", grain=0.014, cuts=2)
    add_box(bm_c, (0.12, 0.85, 0.04), center=(-0.32, 0.32, 0.02), rot_euler=(0.05, 0, -0.45), bevel_ratio=0.2, bevel_segs=0, mat_name="CenoteBase_Water", cuts=1)
    make_obj("Dye_CarvedChannel", bm_c, parent=carved, default_mat="CenoteBase_LimestoneWet")

    bm_s1 = bmesh.new()
    add_clam_shell_bm(bm_s1, width=1.30, depth=0.80, height=0.28, center=(0.0, -0.12, 0.0), rot_z=0.0, wear_front=True, n_u=26, n_v=11)
    add_clam_shell_bm(bm_s1, width=0.52, depth=0.42, height=0.16, center=(-0.55, 0.75, 0.04), rot_z=0.35, n_u=14, n_v=7)
    add_lathe(bm_s1, [(0.0, 0.0), (0.14, 0.0), (0.185, 0.10), (0.18, 0.16), (0.13, 0.16), (0.09, 0.05), (0.0, 0.05)], segs=16, center=(0.65, 0.12, 0), wave_amp=0.008, wave_freq=7, mat_name="CenoteBase_LavaStone", grain=0.010)
    add_cyl(bm_s1, 0.038, 0.026, 0.28, segs=10, center=(0.65, 0.12, 0.16), rot_euler=(0.35, 0.2, 0), mat_name="CenoteBase_DriftwoodDark")
    for px, py in ((0.46, 0.02), (0.50, -0.06), (0.82, 0.04), (0.58, -0.08)):
        add_box(bm_s1, (0.045, 0.032, 0.014), center=(px, py, 0.01), rot_euler=(0, 0, px * 4), bevel_ratio=0.25, bevel_segs=0, cuts=0, mat_name="CenoteBase_ClothRed")
    add_conch_lamp_bm(bm_s1, center=(-0.78, -0.15, 0.0), rot_z=0.3)
    make_obj("Dye_S1_ClamAndMortar", bm_s1, parent=s1, default_mat="CenoteBase_ClamShell")

    bm_wm = bmesh.new()
    add_cyl(bm_wm, 0.46, 0.50, 0.03, segs=22, center=(0.0, -0.14, 0.19), mat_name="CenoteBase_DyeRed")
    make_obj("Dye_Water_Main", bm_wm, parent=s1, default_mat="CenoteBase_DyeRed")

    bm_ws1 = bmesh.new()
    add_cyl(bm_ws1, 0.19, 0.21, 0.025, segs=14, center=(-0.55, 0.74, 0.14), mat_name="CenoteBase_DyeIndigo")
    make_obj("Dye_Water_Sub1", bm_ws1, parent=s1, default_mat="CenoteBase_DyeIndigo")

    # S2: 媒染の素焼き壺3つ（ろくろ段差＋首縄＋ふたの上の灰・岩塩・黒塩）
    bm_s2 = bmesh.new()
    jar_pos = [(0.18, 0.98), (0.50, 0.92), (0.78, 0.75)]
    for j_idx, (jx, jy) in enumerate(jar_pos):
        prof_j = [(0.0, 0.0), (0.09, 0.0), (0.135, 0.12), (0.125, 0.24), (0.085, 0.30), (0.105, 0.33), (0.0, 0.33)]
        add_lathe(bm_s2, prof_j, segs=16, center=(jx, jy, 0.0), wave_amp=0.004, wave_freq=8, mat_name="CenoteBase_Clay")
        add_cyl(bm_s2, 0.092, 0.092, 0.022, segs=12, center=(jx, jy, 0.295), bevel_ends=False, mat_name="CenoteBase_Vine")
        add_cyl(bm_s2, 0.108, 0.096, 0.032, segs=14, center=(jx, jy, 0.34), mat_name="CenoteBase_Driftwood")
        if j_idx == 0:
            add_lathe(bm_s2, [(0.0, 0.35), (0.07, 0.355), (0.035, 0.398), (0.0, 0.415)], segs=12, center=(jx, jy, 0.0), wave_amp=0.005, wave_freq=6, mat_name="CenoteBase_Ash")
        else:
            cmat = "RockSalt_Crystal" if j_idx == 1 else "CenoteBase_BlackSalt"
            for k, (dx, dy, cs) in enumerate([(-0.025, -0.015, 0.034), (0.022, 0.012, 0.030), (-0.005, 0.024, 0.028), (0.015, -0.022, 0.025), (0.0, 0.0, 0.038)]):
                add_box(bm_s2, (cs, cs, cs), center=(jx + dx, jy + dy, 0.372), rot_euler=(0.15 * k, 0.22 * k, 0.55 * k), bevel_ratio=0.08, bevel_segs=0, mat_name=cmat, grain=0.0, cuts=0)
    make_obj("Dye_S2_MordantJars", bm_s2, parent=s2, default_mat="CenoteBase_Clay")

    # S3: 小さなシャコガイ池 +2、つるの物干し綱と染めた樹皮布4枚、木ばさみ、装備吊るし腕木
    bm_s3 = bmesh.new()
    sub_mats = ("CenoteBase_DyeOchre", "CenoteBase_DyeRed")
    for s_idx, (sx, sy, rz) in enumerate([(-0.05, 0.78, -0.2), (0.52, 0.48, -0.45)]):
        add_clam_shell_bm(bm_s3, width=0.52, depth=0.42, height=0.16, center=(sx, sy, 0.03), rot_z=rz, n_u=14, n_v=7)
        add_cyl(bm_s3, 0.19, 0.21, 0.025, segs=14, center=(sx, sy, 0.12), mat_name=sub_mats[s_idx])
    for sx in (-0.82, 0.82):
        add_cyl(bm_s3, 0.048, 0.040, 1.12, segs=12, center=(sx, 0.42, 0.56), mat_name="CenoteBase_Driftwood")
        add_cyl(bm_s3, 0.054, 0.054, 0.035, segs=10, center=(sx, 0.42, 1.02), bevel_ends=False, mat_name="CenoteBase_Vine")
    add_box(bm_s3, (0.42, 0.048, 0.048), center=(0.65, 0.42, 0.96), rot_euler=(0, 0.15, 0), bevel_ratio=0.25, bevel_segs=1, mat_name="CenoteBase_Driftwood")
    add_cyl(bm_s3, 0.015, 0.015, 1.66, segs=8, center=(0, 0.42, 1.02), rot_euler=(0, math.pi / 2, 0), bevel_ends=False, mat_name="CenoteBase_Vine")
    cloth_mats = ("CenoteBase_ClothRed", "CenoteBase_ClothIndigo", "CenoteBase_ClothOchre", "CenoteBase_ClothRed")
    for idx, cx in enumerate((-0.52, -0.18, 0.16, 0.48)):
        add_box(bm_s3, (0.19, 0.020, 0.46), center=(cx, 0.42, 0.78), rot_euler=(0.05 * (idx - 1.5), 0, 0), bevel_ratio=0.18, warp=0.018, bevel_segs=2, mat_name=cloth_mats[idx], cuts=3)
        # 布を留める流木のピン 2個ずつ
        for px_off in (-0.06, 0.06):
            add_box(bm_s3, (0.018, 0.032, 0.045), center=(cx + px_off, 0.42, 1.025), bevel_ratio=0.2, bevel_segs=1, mat_name="CenoteBase_DriftwoodDark")
    make_obj("Dye_S3_ClamsAndClothLine", bm_s3, parent=s3, default_mat="CenoteBase_Driftwood")

    return root


# 5. Collection (2階回廊 12時の横穴, 幅 2.6 x 高さ 1.4, 奥行き 1.6)
def build_room_collection():
    root, carved, s1, s2, s3 = make_room_hierarchy("Collection")

    bm_c = bmesh.new()
    add_box(bm_c, (2.65, 1.75, 0.12), center=(0, 0.72, -0.06), bevel_ratio=0.08, bevel_segs=2, mat_name="CenoteBase_Limestone", grain=0.018, cuts=4)
    wall_w, wall_h, wall_d = 1.56, 1.22, 0.18
    wy = 1.05
    add_box(bm_c, (wall_w, 0.06, wall_h), center=(0, wy + 0.08, wall_h * 0.5), bevel_ratio=0.08, bevel_segs=2, mat_name="CenoteBase_Limestone", grain=0.012, cuts=3)
    for r in range(5):
        z_bar = 0.06 + r * 0.24
        add_box(bm_c, (wall_w, wall_d, 0.038), center=(0, wy, z_bar), bevel_ratio=0.22, bevel_segs=1, mat_name="CenoteBase_Limestone", grain=0.008, cuts=3)
    for c in range(6):
        x_bar = -0.65 + c * 0.26
        add_box(bm_c, (0.038, wall_d, 0.99), center=(x_bar, wy, 0.54), bevel_ratio=0.22, bevel_segs=1, mat_name="CenoteBase_Limestone", grain=0.008, cuts=3)
    # 5つの区画レリーフ印 (Z=1.11: 雨粒、5弁の花、塩の結晶、溶岩の割れ目、3峰の山)
    header_mats = ("CenoteBase_Water", "CenoteBase_ClothRed", "CenoteBase_Salt", "CenoteBase_LavaStone", "CenoteBase_LimestoneWet")
    for c in range(5):
        cx = -0.52 + c * 0.26
        add_cyl(bm_c, 0.062, 0.052, 0.032, segs=8 + c * 2, center=(cx, wy - 0.03, 1.11), rot_euler=(math.pi / 2, 0, c * 0.4), bevel_ends=True, mat_name=header_mats[c])
    # 左の段付き岩台座
    add_box(bm_c, (0.44, 0.44, 0.08), center=(-0.95, 0.35, 0.04), bevel_ratio=0.2, bevel_segs=1, mat_name="CenoteBase_LimestoneWet", grain=0.012)
    add_box(bm_c, (0.38, 0.38, 0.22), center=(-0.95, 0.35, 0.18), bevel_ratio=0.18, bevel_segs=2, mat_name="CenoteBase_Limestone", grain=0.012)
    make_obj("Collection_CarvedWall", bm_c, parent=carved, default_mat="CenoteBase_Limestone")

    idx_cub = 1
    for r in range(4):
        for c in range(5):
            cx = -0.52 + c * 0.26
            cz = 0.18 + r * 0.24
            bm_bk = bmesh.new()
            add_box(bm_bk, (0.19, 0.012, 0.19), center=(0, 0, 0), bevel_ratio=0.12, warp=0.004, bevel_segs=1, mat_name="CenoteBase_BarkCloth")
            if idx_cub == 7:
                add_box(bm_bk, (0.075, 0.016, 0.075), center=(0.03, -0.004, -0.02), rot_euler=(0, 0.15, 0), bevel_ratio=0.2, bevel_segs=1, mat_name="CenoteBase_Vine")
            make_obj(f"Collection_Cubby_{idx_cub:02d}_Back", bm_bk, location=(cx, wy + 0.03, cz), parent=s1, default_mat="CenoteBase_BarkCloth")

            bm_sh = bmesh.new()
            add_cyl(bm_sh, 0.055, 0.042, 0.010, segs=10, center=(0, 0, -0.01), rot_euler=(math.pi / 2, 0, 0), bevel_ends=False, mat_name="CenoteBase_Charcoal")
            add_cyl(bm_sh, 0.028, 0.022, 0.012, segs=8, center=(0, -0.002, 0.038), rot_euler=(math.pi / 2, 0, 0), bevel_ends=False, mat_name="CenoteBase_Charcoal")
            make_obj(f"Collection_Cubby_{idx_cub:02d}_Shadow", bm_sh, location=(cx, wy + 0.018, cz), parent=s1, default_mat="CenoteBase_Charcoal")
            idx_cub += 1

    def add_framed_board(bm_target, center, rot_z):
        cb = bmesh.new()
        add_box(cb, (0.03, 0.36, 0.46), center=(0, 0, 0), bevel_ratio=0.15, bevel_segs=1, mat_name="CenoteBase_BarkCloth", cuts=2)
        for sz in (-0.23, 0.23):
            add_box(cb, (0.055, 0.42, 0.045), center=(0, 0, sz), bevel_ratio=0.25, bevel_segs=1, mat_name="CenoteBase_Driftwood")
        for sy in (-0.19, 0.19):
            add_box(cb, (0.055, 0.045, 0.50), center=(0, sy, 0), bevel_ratio=0.25, bevel_segs=1, mat_name="CenoteBase_Driftwood")
            for sz in (-0.23, 0.23):
                add_cyl(cb, 0.032, 0.032, 0.06, segs=8, center=(0, sy, sz), rot_euler=(0, math.pi / 2, 0), bevel_ends=False, mat_name="CenoteBase_Vine")
        mat = Matrix.Translation(center) @ Euler((0, 0, rot_z), 'XYZ').to_matrix().to_4x4()
        merge_bm(bm_target, cb, mat)

    bm_s1_props = bmesh.new()
    add_framed_board(bm_s1_props, (1.02, 0.42, 0.62), -0.15)
    add_box(bm_s1_props, (0.10, 0.02, 0.14), center=(-0.95, 0.14, 0.18), bevel_ratio=0.2, bevel_segs=1, mat_name="CenoteBase_BarkCloth")
    add_box(bm_s1_props, (0.02, 0.10, 0.14), center=(1.02, 0.18, 0.45), bevel_ratio=0.2, bevel_segs=1, mat_name="CenoteBase_BarkCloth")
    add_brass_wall_lamp_bm(bm_s1_props, center=(-0.95, 0.85, 1.02), rot_z=0.35)
    add_brass_wall_lamp_bm(bm_s1_props, center=( 0.95, 0.85, 1.02), rot_z=-0.35)
    make_obj("Collection_S1_FrameAndLamps", bm_s1_props, parent=s1, default_mat="CenoteBase_Driftwood")

    bm_s2 = bmesh.new()
    for sy, sz in ((0.55, 0.78), (0.85, 0.78)):
        add_box(bm_s2, (0.08, 0.12, 0.18), center=(-1.04, sy, sz), bevel_ratio=0.25, bevel_segs=1, mat_name="CenoteBase_Driftwood")
        add_cyl(bm_s2, 0.028, 0.020, 0.18, segs=8, center=(-0.96, sy, sz + 0.04), rot_euler=(0, 1.2, 0), bevel_ends=False, mat_name="CenoteBase_DriftwoodDark")
        add_cyl(bm_s2, 0.034, 0.034, 0.03, segs=8, center=(-1.01, sy, sz + 0.01), rot_euler=(0, 1.2, 0), bevel_ends=False, mat_name="CenoteBase_Vine")
        add_box(bm_s2, (0.02, 0.09, 0.12), center=(-1.02, sy, sz - 0.16), bevel_ratio=0.2, bevel_segs=1, mat_name="CenoteBase_BarkCloth")
    make_obj("Collection_S2_TrophyHooks", bm_s2, parent=s2, default_mat="CenoteBase_Driftwood")

    bm_s3 = bmesh.new()
    add_framed_board(bm_s3, ( 1.02, 0.88, 0.62), -0.15)
    add_framed_board(bm_s3, (-1.02, 0.25, 0.78),  0.15)
    make_obj("Collection_S3_ExtraFrames", bm_s3, parent=s3, default_mat="CenoteBase_Driftwood")

    return root


# 6. Workshop (2階回廊 1時半の横穴, 幅 2.0 x 高さ 1.2, 奥行き 1.6)
def build_room_workshop():
    root, carved, s1, s2, s3 = make_room_hierarchy("Workshop")

    bm_c = bmesh.new()
    add_box(bm_c, (2.10, 1.75, 0.12), center=(0, 0.72, -0.06), bevel_ratio=0.08, bevel_segs=2, mat_name="CenoteBase_Limestone", grain=0.018, cuts=4)
    add_box(bm_c, (1.30, 0.16, 0.48), center=(0.0, 1.25, 0.72), bevel_ratio=0.18, bevel_segs=2, mat_name="CenoteBase_Limestone", grain=0.014, cuts=3)
    add_box(bm_c, (0.42, 1.02, 0.18), center=(-0.72, 0.62, 0.09), bevel_ratio=0.18, bevel_segs=2, mat_name="CenoteBase_Limestone", grain=0.014, cuts=3)
    make_obj("Workshop_CarvedLedges", bm_c, parent=carved, default_mat="CenoteBase_Limestone")

    bm_s1 = bmesh.new()
    by_bench = -0.05
    # サンゴの塊の脚2つ（強い凹凸）
    for sx in (-0.52, 0.52):
        add_box(bm_s1, (0.32, 0.48, 0.22), center=(sx, by_bench, 0.11), bevel_ratio=0.32, bevel_segs=2, mat_name="CenoteBase_Coral", grain=0.024, cuts=2)
    # 3枚の厚い流木板を並べた作業台天板＋つる鎹＋ノミの削り傷
    for iy, py_off in enumerate((-0.19, 0.0, 0.19)):
        add_box(bm_s1, (1.60, 0.19, 0.095), center=(0, by_bench + py_off, 0.25), bevel_ratio=0.22, warp=-0.008, bevel_segs=2, mat_name="CenoteBase_DriftwoodDark", cuts=4)
    for k in range(4):
        add_box(bm_s1, (0.022, 0.22, 0.014), center=(-0.28 + k * 0.055, by_bench - 0.08, 0.302), rot_euler=(0, 0, 0.25 + k * 0.1), bevel_ratio=0.2, bevel_segs=1, mat_name="CenoteBase_Driftwood")
    # 発酵壺＋真鍮の口＋つる巻き管＋圧力ボンベと留め具
    add_lathe(bm_s1, [(0.0, 0.0), (0.11, 0.0), (0.165, 0.12), (0.145, 0.21), (0.075, 0.24), (0.0, 0.24)], segs=20, center=(0.0, by_bench + 0.05, 0), wave_amp=0.005, wave_freq=8, mat_name="CenoteBase_Clay")
    add_cyl(bm_s1, 0.048, 0.042, 0.045, segs=14, center=(0.0, by_bench + 0.05, 0.25), mat_name="CenoteBase_Brass")
    pts_tube = [Vector((0.0, by_bench + 0.05, 0.26)), Vector((0.15, by_bench - 0.16, 0.28)), Vector((0.28, by_bench - 0.12, 0.34)), Vector((0.36, by_bench - 0.05, 0.33))]
    add_tube_along_curve(bm_s1, pts_tube, [0.019] * 4, segs=10, mat_name="CenoteBase_Vine", twist_rib=0.10)
    add_box(bm_s1, (0.18, 0.15, 0.08), center=(0.44, by_bench - 0.05, 0.32), bevel_ratio=0.22, bevel_segs=1, mat_name="CenoteBase_Driftwood")
    add_cyl(bm_s1, 0.044, 0.044, 0.24, segs=14, center=(0.52, by_bench - 0.05, 0.35), rot_euler=(0, math.pi / 2, 0), mat_name="CenoteBase_Brass")
    for bx_strap in (0.45, 0.57):
        add_cyl(bm_s1, 0.050, 0.050, 0.022, segs=10, center=(bx_strap, by_bench - 0.05, 0.35), rot_euler=(0, math.pi / 2, 0), bevel_ends=False, mat_name="CenoteBase_Vine")

    # 奥壁の5つの道具（1:ピッケル、2:貝刃ナイフ、3:木槌、4:魚骨の錐、5:貝殻のかんな）を1つずつ精巧に造形！
    # 道具掛けの横木
    add_box(bm_s1, (1.24, 0.04, 0.045), center=(0.0, 1.18, 0.76), bevel_ratio=0.25, bevel_segs=1, mat_name="CenoteBase_Driftwood", cuts=2)
    # 1) ピッケル (-0.48)
    add_cyl(bm_s1, 0.018, 0.015, 0.32, segs=8, center=(-0.48, 1.15, 0.72), rot_euler=(0, -0.12, 0), mat_name="CenoteBase_DriftwoodDark")
    pts_pick = [Vector((-0.60, 1.15, 0.81)), Vector((-0.48, 1.15, 0.85)), Vector((-0.36, 1.15, 0.81))]
    add_tube_along_curve(bm_s1, pts_pick, [0.008, 0.028, 0.010], segs=8, mat_name="CenoteBase_LavaStone")
    add_cyl(bm_s1, 0.032, 0.032, 0.035, segs=8, center=(-0.48, 1.15, 0.84), bevel_ends=False, mat_name="CenoteBase_Vine")
    # 2) 貝刃のナイフ (-0.24)
    add_cyl(bm_s1, 0.018, 0.016, 0.14, segs=8, center=(-0.24, 1.15, 0.65), mat_name="CenoteBase_DriftwoodDark")
    add_cyl(bm_s1, 0.022, 0.022, 0.03, segs=8, center=(-0.24, 1.15, 0.71), bevel_ends=False, mat_name="CenoteBase_Vine")
    add_box(bm_s1, (0.042, 0.014, 0.17), center=(-0.24, 1.15, 0.80), rot_euler=(0, 0.08, 0), bevel_ratio=0.25, warp=0.008, bevel_segs=1, mat_name="CenoteBase_ClamShell")
    # 3) 木槌 (0.0)
    add_cyl(bm_s1, 0.018, 0.015, 0.30, segs=8, center=(0.0, 1.15, 0.71), mat_name="CenoteBase_DriftwoodDark")
    add_cyl(bm_s1, 0.055, 0.048, 0.16, segs=10, center=(0.0, 1.15, 0.83), rot_euler=(0, math.pi / 2, 0), mat_name="CenoteBase_DriftwoodDark", grain=0.008)
    add_cyl(bm_s1, 0.030, 0.030, 0.04, segs=8, center=(0.0, 1.15, 0.83), bevel_ends=False, mat_name="CenoteBase_Vine")
    # 4) 魚骨の錐 (+0.24)
    pts_awl = [Vector((0.24, 1.15, 0.58)), Vector((0.26, 1.15, 0.72)), Vector((0.23, 1.15, 0.86))]
    add_tube_along_curve(bm_s1, pts_awl, [0.004, 0.016, 0.022], segs=8, mat_name="CenoteBase_Bone")
    add_cyl(bm_s1, 0.024, 0.024, 0.02, segs=8, center=(0.23, 1.15, 0.84), rot_euler=(math.pi / 2, 0, 0), bevel_ends=False, mat_name="CenoteBase_Vine")
    # 5) 貝殻のかんな (+0.48)
    add_box(bm_s1, (0.09, 0.055, 0.18), center=(0.48, 1.15, 0.74), bevel_ratio=0.25, bevel_segs=1, mat_name="CenoteBase_DriftwoodDark")
    add_box(bm_s1, (0.075, 0.018, 0.10), center=(0.48, 1.13, 0.75), rot_euler=(0.45, 0, 0), bevel_ratio=0.2, bevel_segs=1, mat_name="CenoteBase_ClamShell")

    # 左壁の掛け釘4本（つる根元巻き付き）
    for sy in (0.35, 0.55, 0.75, 0.95):
        add_cyl(bm_s1, 0.026, 0.020, 0.16, segs=8, center=(-0.86, sy, 0.68), rot_euler=(0, 1.3, 0), bevel_ends=False, mat_name="CenoteBase_DriftwoodDark")
        add_cyl(bm_s1, 0.032, 0.032, 0.03, segs=8, center=(-0.91, sy, 0.66), rot_euler=(0, 1.3, 0), bevel_ends=False, mat_name="CenoteBase_Vine")
    # 右壁の流木枠（4本の丸太枠＋四隅のつる縛り）＋真鍮の壁灯1つ
    for sz_m in (0.14, 0.82):
        add_box(bm_s1, (0.065, 0.54, 0.055), center=(0.82, 0.62, sz_m), rot_euler=(0, 0, -0.2), bevel_ratio=0.25, bevel_segs=1, mat_name="CenoteBase_Driftwood")
    for sy_m in (0.38, 0.86):
        add_box(bm_s1, (0.065, 0.055, 0.74), center=(0.82 + (sy_m - 0.62) * 0.2, sy_m, 0.48), rot_euler=(0, 0, -0.2), bevel_ratio=0.25, bevel_segs=1, mat_name="CenoteBase_Driftwood")
    add_brass_wall_lamp_bm(bm_s1, center=(0.82, 0.12, 0.96), rot_z=-0.5)
    make_obj("Workshop_S1_BenchAndTools", bm_s1, parent=s1, default_mat="CenoteBase_DriftwoodDark")

    bm_mir = bmesh.new()
    add_box(bm_mir, (0.03, 0.44, 0.64), center=(0.80, 0.62, 0.48), rot_euler=(0, 0, -0.2), bevel_ratio=0.08, bevel_segs=2, mat_name="CenoteBase_Obsidian", grain=0.002, cuts=2)
    make_obj("Workshop_ObsidianMirror", bm_mir, parent=s1, default_mat="CenoteBase_Obsidian")

    bm_s2 = bmesh.new()
    sx2, sy2 = -0.58, 0.48
    add_box(bm_s2, (0.38, 0.34, 0.22), center=(sx2, sy2, 0.28), bevel_ratio=0.2, bevel_segs=2, mat_name="CenoteBase_Driftwood", cuts=2)
    add_lathe(bm_s2, [(0.0, 0.39), (0.10, 0.39), (0.12, 0.46), (0.09, 0.46), (0.0, 0.41)], segs=14, center=(sx2 - 0.07, sy2, 0), mat_name="CenoteBase_LavaStone")
    add_lathe(bm_s2, [(0.0, 0.43), (0.085, 0.45), (0.06, 0.51), (0.0, 0.53)], segs=12, center=(sx2 - 0.07, sy2, 0), wave_amp=0.008, wave_freq=6, mat_name="CenoteBase_Moss")
    add_box(bm_s2, (0.13, 0.18, 0.16), center=(sx2 + 0.09, sy2, 0.45), bevel_ratio=0.25, bevel_segs=1, mat_name="CenoteBase_DriftwoodDark")
    add_cyl(bm_s2, 0.018, 0.018, 0.18, segs=8, center=(sx2 + 0.09, sy2, 0.52), rot_euler=(0, math.pi / 2, 0), mat_name="CenoteBase_Vine")
    make_obj("Workshop_S2_PressAndClamp", bm_s2, parent=s2, default_mat="CenoteBase_Driftwood")

    bm_s3 = bmesh.new()
    sx3, sy3 = 0.52, 0.62
    add_box(bm_s3, (0.42, 0.38, 0.28), center=(sx3, sy3, 0.14), bevel_ratio=0.2, bevel_segs=2, mat_name="CenoteBase_Driftwood", cuts=2)
    add_lathe(bm_s3, [(0.0, 0.28), (0.15, 0.34), (0.19, 0.46), (0.13, 0.58), (0.0, 0.63)], segs=18, center=(sx3, sy3, 0), wave_amp=0.026, wave_freq=7, mat_name="CenoteBase_Coconut", grain=0.010)
    for sx_cl in (-0.17, 0.17):
        add_box(bm_s3, (0.055, 0.22, 0.25), center=(sx3 + sx_cl, sy3, 0.38), bevel_ratio=0.25, bevel_segs=1, mat_name="CenoteBase_DriftwoodDark")
        add_cyl(bm_s3, 0.032, 0.032, 0.06, segs=8, center=(sx3 + sx_cl, sy3, 0.46), rot_euler=(0, math.pi / 2, 0), bevel_ends=False, mat_name="CenoteBase_Vine")
    make_obj("Workshop_S3_StormFruitStand", bm_s3, parent=s3, default_mat="CenoteBase_Driftwood")

    return root


# 7. Drying (2階回廊 3時の横穴, 乾いた部屋, 幅 2.0 x 高さ 1.2, 奥行き 1.6)
def build_room_drying():
    root, carved, s1, s2, s3 = make_room_hierarchy("Drying")

    bm_c = bmesh.new()
    add_box(bm_c, (2.10, 1.75, 0.12), center=(0, 0.72, -0.06), bevel_ratio=0.08, bevel_segs=2, mat_name="CenoteBase_Limestone", grain=0.018, cuts=4)
    make_obj("Drying_CarvedFloor", bm_c, parent=carved, default_mat="CenoteBase_Limestone")

    bm_s1 = bmesh.new()
    ry = 0.12
    for sx in (-0.66, 0.66):
        for sy in (ry - 0.22, ry + 0.22):
            add_cyl(bm_s1, 0.042, 0.036, 0.52, segs=10, center=(sx, sy, 0.26), mat_name="CenoteBase_Driftwood")
            add_cyl(bm_s1, 0.048, 0.048, 0.04, segs=8, center=(sx, sy, 0.46), bevel_ends=False, mat_name="CenoteBase_Vine")
    for sy in (ry - 0.22, ry + 0.22):
        add_box(bm_s1, (1.38, 0.052, 0.052), center=(0, sy, 0.46), bevel_ratio=0.25, bevel_segs=1, mat_name="CenoteBase_Driftwood", cuts=3)
    for sx in (-0.66, 0.66):
        add_box(bm_s1, (0.052, 0.48, 0.052), center=(sx, ry, 0.46), bevel_ratio=0.25, bevel_segs=1, mat_name="CenoteBase_Driftwood", cuts=1)
    for ix in range(-5, 6):
        add_cyl(bm_s1, 0.012, 0.012, 0.44, segs=6, center=(ix * 0.11, ry, 0.46), rot_euler=(math.pi / 2, 0, 0), bevel_ends=False, mat_name="CenoteBase_Vine")
    for iy in (-0.12, 0.0, 0.12):
        add_cyl(bm_s1, 0.012, 0.012, 1.30, segs=6, center=(0, ry + iy, 0.46), rot_euler=(0, math.pi / 2, 0), bevel_ends=False, mat_name="CenoteBase_Vine")
    # 網の上に広げた乾燥中の葉と、網のつる補修箇所 (Wear)
    add_cyl(bm_s1, 0.048, 0.048, 0.026, segs=8, center=(0.22, ry + 0.05, 0.465), bevel_ends=False, mat_name="CenoteBase_Vine")
    for lx_d in (-0.38, -0.12, 0.40):
        add_box(bm_s1, (0.14, 0.10, 0.025), center=(lx_d, ry, 0.485), rot_euler=(0, 0, lx_d * 3), bevel_ratio=0.3, bevel_segs=1, mat_name="CenoteBase_Thatch")
    # 天井フック3つから吊るした干し植物の束（つる紐＋複数の葉の房）
    for bx in (-0.42, 0.0, 0.42):
        add_cyl(bm_s1, 0.012, 0.012, 0.22, segs=6, center=(bx, ry + 0.10, 1.08), bevel_ends=False, mat_name="CenoteBase_Vine")
        add_cyl(bm_s1, 0.038, 0.038, 0.03, segs=8, center=(bx, ry + 0.10, 0.98), bevel_ends=False, mat_name="CenoteBase_Vine")
        for k_leaf in range(4):
            la = (k_leaf / 4) * math.tau
            add_box(bm_s1, (0.06, 0.04, 0.24), center=(bx + math.cos(la) * 0.04, ry + 0.10 + math.sin(la) * 0.04, 0.85),
                    rot_euler=(math.sin(la) * 0.22, -math.cos(la) * 0.22, la), bevel_ratio=0.35, bevel_segs=1, mat_name="CenoteBase_Leaf")
    # 塩漬けの壺＋浜の丸石の重し
    add_lathe(bm_s1, [(0.0, 0.0), (0.11, 0.0), (0.155, 0.15), (0.125, 0.28), (0.14, 0.31), (0.0, 0.31)], segs=18, center=(0.76, 0.58, 0), wave_amp=0.004, wave_freq=8, mat_name="CenoteBase_Clay")
    add_cyl(bm_s1, 0.125, 0.115, 0.025, segs=14, center=(0.76, 0.58, 0.32), mat_name="CenoteBase_Driftwood")
    add_box(bm_s1, (0.18, 0.15, 0.09), center=(0.76, 0.58, 0.36), bevel_ratio=0.42, bevel_segs=2, mat_name="CenoteBase_Limestone", grain=0.010)
    # 敷居の古びた銅の帯（鋲つき）と すぐ外の濡れた苔の足ふき
    add_box(bm_s1, (1.88, 0.048, 0.018), center=(0, -0.16, 0.009), bevel_ratio=0.2, bevel_segs=1, mat_name="CenoteBase_Copper", grain=0.003, cuts=3)
    add_box(bm_s1, (0.84, 0.38, 0.040), center=(0, -0.42, 0.020), bevel_ratio=0.38, warp=0.008, bevel_segs=2, mat_name="CenoteBase_Moss", grain=0.014, cuts=3)
    make_obj("Drying_S1_RackAndJar", bm_s1, parent=s1, default_mat="CenoteBase_Driftwood")

    bm_s2 = bmesh.new()
    for sx in (-0.66, 0.66):
        for sy in (ry - 0.22, ry + 0.22):
            add_cyl(bm_s2, 0.038, 0.032, 0.54, segs=8, center=(sx, sy, 0.76), bevel_ends=False, mat_name="CenoteBase_Driftwood")
    for z_tier in (0.74, 1.00):
        for sy in (ry - 0.22, ry + 0.22):
            add_box(bm_s2, (1.38, 0.048, 0.048), center=(0, sy, z_tier), bevel_ratio=0.25, bevel_segs=1, mat_name="CenoteBase_Driftwood", cuts=3)
        for sx in (-0.66, 0.66):
            add_box(bm_s2, (0.048, 0.48, 0.048), center=(sx, ry, z_tier), bevel_ratio=0.25, bevel_segs=1, mat_name="CenoteBase_Driftwood", cuts=1)
        for ix in range(-4, 5):
            add_cyl(bm_s2, 0.010, 0.010, 0.44, segs=5, center=(ix * 0.14, ry, z_tier), rot_euler=(math.pi / 2, 0, 0), bevel_ends=False, mat_name="CenoteBase_Vine")
    make_obj("Drying_S2_UpperTiers", bm_s2, parent=s2, default_mat="CenoteBase_Driftwood")

    fan_piv = make_empty("Drying_FanPivot", loc=(0.0, 0.85, 1.12), parent=s3)
    bm_fan = bmesh.new()
    add_cyl(bm_fan, 0.028, 0.032, 0.72, segs=8, center=(0, 0, -0.36), bevel_ends=False, mat_name="CenoteBase_Driftwood")
    add_cyl(bm_fan, 0.045, 0.045, 0.05, segs=10, center=(0, 0, -0.12), bevel_ends=False, mat_name="CenoteBase_Vine")
    for i in range(-4, 5):
        ang = i * 0.14
        add_box(bm_fan, (0.11, 0.022, 0.56), center=(math.sin(ang) * 0.28, 0, -0.42 - math.cos(ang) * 0.08),
                rot_euler=(0, ang, 0), bevel_ratio=0.28, warp=0.008, bevel_segs=1, mat_name="CenoteBase_Thatch", cuts=2)
    make_obj("Drying_S3_PalmFan", bm_fan, parent=fan_piv, default_mat="CenoteBase_Thatch")

    return root


# 8. Refinery (2階回廊 4時18分の横穴, 乾いた部屋, 幅 2.2 x 高さ 1.2, 奥行き 1.6)
def build_room_refinery():
    root, carved, s1, s2, s3 = make_room_hierarchy("Refinery")

    bm_c = bmesh.new()
    add_box(bm_c, (2.25, 1.75, 0.12), center=(0, 0.72, -0.06), bevel_ratio=0.08, bevel_segs=2, mat_name="CenoteBase_Limestone", grain=0.018, cuts=4)
    make_obj("Refinery_CarvedFloor", bm_c, parent=carved, default_mat="CenoteBase_Limestone")

    qx, qy = -0.48, -0.08
    bm_s1 = bmesh.new()
    add_cyl(bm_s1, 0.36, 0.34, 0.08, segs=22, center=(qx, qy, 0.04), mat_name="CenoteBase_Limestone", grain=0.010)
    # 受け皿の注ぎ口にこびりついた白い塩の結晶塊 (Wear)
    for sx_cr, sy_cr, sz_cr in ((0.0, -0.33, 0.075), (-0.05, -0.32, 0.07), (0.05, -0.32, 0.07)):
        add_box(bm_s1, (0.07, 0.08, 0.032), center=(qx + sx_cr, qy + sy_cr, sz_cr), rot_euler=(0.1, 0.2, sx_cr * 4), bevel_ratio=0.2, bevel_segs=1, mat_name="CenoteBase_Salt")
    add_cyl(bm_s1, 0.30, 0.30, 0.15, segs=24, center=(qx, qy, 0.155), mat_name="CenoteBase_LavaStone", grain=0.010)

    cx, cy = 0.42, 0.02
    add_box(bm_s1, (0.60, 0.40, 0.20), center=(cx, cy, 0.10), bevel_ratio=0.16, bevel_segs=2, mat_name="CenoteBase_LavaStone", grain=0.014, cuts=2)
    add_cyl(bm_s1, 0.095, 0.085, 0.22, segs=12, center=(cx - 0.10, cy - 0.02, 0.27), rot_euler=(0, 1.3, 0.4), mat_name="CenoteBase_DriftwoodDark", grain=0.008)
    add_cyl(bm_s1, 0.028, 0.022, 0.36, segs=8, center=(cx + 0.04, cy - 0.08, 0.30), rot_euler=(0.4, 1.1, 0), bevel_ends=False, mat_name="CenoteBase_DriftwoodDark")
    add_box(bm_s1, (0.24, 0.16, 0.055), center=(cx + 0.14, cy + 0.10, 0.225), bevel_ratio=0.18, bevel_segs=1, mat_name="CenoteBase_DriftwoodDark")
    for k in range(3):
        add_box(bm_s1, (0.06, 0.16, 0.042), center=(cx - 0.18 + k * 0.075, cy + 0.12, 0.22), bevel_ratio=0.18, bevel_segs=1, mat_name="CenoteBase_Salt")
    # 弾袋2種（丸袋＝散弾、細長袋＝連射、口をつる紐で縛る）
    add_lathe(bm_s1, [(0.0, 0.0), (0.075, 0.0), (0.09, 0.08), (0.04, 0.13), (0.055, 0.16), (0.0, 0.16)], segs=14, center=(cx + 0.38, cy - 0.08, 0.0), wave_amp=0.005, wave_freq=6, mat_name="CenoteBase_BarkCloth")
    add_cyl(bm_s1, 0.044, 0.044, 0.018, segs=10, center=(cx + 0.38, cy - 0.08, 0.13), bevel_ends=False, mat_name="CenoteBase_Vine")
    add_cyl(bm_s1, 0.048, 0.036, 0.22, segs=12, center=(cx + 0.48, cy + 0.06, 0.11), rot_euler=(0.2, 0, 0), mat_name="CenoteBase_BarkCloth")
    add_cyl(bm_s1, 0.042, 0.042, 0.018, segs=10, center=(cx + 0.48, cy + 0.04, 0.19), rot_euler=(0.2, 0, 0), bevel_ends=False, mat_name="CenoteBase_Vine")

    # 奥壁ぞいの脚付き塩箱 3つ（板組み＋山は 1=RockSalt_Crystal、2=CenoteBase_Salt、3=CenoteBase_BlackSalt）
    bin_pile_mats = ("RockSalt_Crystal", "CenoteBase_Salt", "CenoteBase_BlackSalt")
    for b_idx, bx in enumerate((-0.65, 0.0, 0.65)):
        by_bin = 0.92
        for lx in (-0.20, 0.20):
            for ly in (-0.15, 0.15):
                add_cyl(bm_s1, 0.034, 0.028, 0.32, segs=8, center=(bx + lx, by_bin + ly, 0.16), bevel_ends=False, mat_name="CenoteBase_DriftwoodDark")
        for iz_b, zb_p in enumerate((0.36, 0.46, 0.55)):
            b_mat = "CenoteBase_DriftwoodDark" if iz_b == 1 else "CenoteBase_Driftwood"
            for sy_b in (-0.185, 0.185):
                add_box(bm_s1, (0.50, 0.035, 0.09), center=(bx, by_bin + sy_b, zb_p), bevel_ratio=0.22, bevel_segs=1, mat_name=b_mat)
            for sx_b in (-0.235, 0.235):
                add_box(bm_s1, (0.035, 0.40, 0.09), center=(bx + sx_b, by_bin, zb_p), bevel_ratio=0.22, bevel_segs=1, mat_name=b_mat)
        add_box(bm_s1, (0.52, 0.42, 0.04), center=(bx, by_bin + 0.08, 0.64), rot_euler=(-0.42, 0, 0), bevel_ratio=0.2, bevel_segs=1, mat_name="CenoteBase_Driftwood")
        add_lathe(bm_s1, [(0.0, 0.48), (0.19, 0.52), (0.14, 0.60), (0.0, 0.64)], segs=14, center=(bx, by_bin, 0), wave_amp=0.012, wave_freq=6, mat_name=bin_pile_mats[b_idx], grain=0.008)
    for gz in (0.58, 0.78):
        add_box(bm_s1, (0.045, 0.14, 0.18), center=(-0.95, 0.10, gz), rot_euler=(0.2, 0, 0.3), bevel_ratio=0.3, bevel_segs=1, mat_name="CenoteBase_BarkCloth")
    add_box(bm_s1, (2.05, 0.048, 0.018), center=(0, -0.20, 0.009), bevel_ratio=0.2, bevel_segs=1, mat_name="CenoteBase_Copper", cuts=3)
    make_obj("Refinery_S1_QuernAndBins", bm_s1, parent=s1, default_mat="CenoteBase_Driftwood")

    q_piv = make_empty("Refinery_QuernPivot", loc=(qx, qy, 0.23), parent=s1)
    bm_qu = bmesh.new()
    add_cyl(bm_qu, 0.30, 0.29, 0.12, segs=24, center=(0, 0, 0.06), mat_name="CenoteBase_LavaStone", grain=0.010)
    add_cyl(bm_qu, 0.036, 0.030, 0.38, segs=10, center=(0.20, -0.10, 0.29), mat_name="CenoteBase_DriftwoodDark")
    for k in range(4):
        add_cyl(bm_qu, 0.044, 0.044, 0.016, segs=8, center=(0.20, -0.10, 0.21 + k * 0.022), bevel_ends=False, mat_name="CenoteBase_Vine")
    make_obj("Refinery_QuernUpper", bm_qu, parent=q_piv, default_mat="CenoteBase_LavaStone")

    bm_s2 = bmesh.new()
    vx, vy = -0.82, 0.42
    add_box(bm_s2, (0.32, 0.36, 0.28), center=(vx, vy, 0.14), bevel_ratio=0.2, bevel_segs=2, mat_name="CenoteBase_Driftwood")
    add_box(bm_s2, (0.24, 0.22, 0.12), center=(vx, vy, 0.34), bevel_ratio=0.22, bevel_segs=1, mat_name="CenoteBase_DriftwoodDark")
    add_box(bm_s2, (0.18, 0.03, 0.14), center=(vx + 0.05, vy, 0.44), rot_euler=(0, -0.4, 0), bevel_ratio=0.2, bevel_segs=1, mat_name="CenoteBase_ClamShell")
    add_box(bm_s2, (0.22, 0.18, 0.10), center=(vx + 0.04, vy - 0.28, 0.05), bevel_ratio=0.2, bevel_segs=1, mat_name="CenoteBase_Driftwood")
    make_obj("Refinery_S2_SniperVise", bm_s2, parent=s2, default_mat="CenoteBase_Driftwood")

    bm_s3 = bmesh.new()
    q2x, q2y = -0.02, 0.38
    add_cyl(bm_s3, 0.46, 0.45, 0.20, segs=12, center=(q2x, q2y, 0.10), bevel_segs=2, mat_name="CenoteBase_LavaStone", grain=0.014)
    add_cyl(bm_s3, 0.45, 0.44, 0.16, segs=12, center=(q2x, q2y, 0.28), bevel_segs=2, mat_name="CenoteBase_LavaStone", grain=0.014)
    for sx in (-0.34, 0.34):
        add_cyl(bm_s3, 0.044, 0.036, 0.48, segs=10, center=(q2x + sx, q2y - 0.05, 0.56), mat_name="CenoteBase_DriftwoodDark")
    make_obj("Refinery_S3_BigQuern", bm_s3, parent=s3, default_mat="CenoteBase_LavaStone")

    return root


# 9. Deck (2階外のデッキの観測所, 目印はデッキ床中央 (1.3, -7.7, 4.6), +Y＝北)
def build_room_deck():
    root, carved, s1, s2, s3 = make_room_hierarchy("Deck")

    bm_c = bmesh.new()
    # ヤシの葉を10枚重ねた屋根＋横垂木2本＋支柱2本
    for ry_b in (0.25, 1.15):
        add_cyl(bm_c, 0.036, 0.036, 2.85, segs=8, center=(0.0, ry_b, 1.64 + (ry_b - 0.7) * 0.14), rot_euler=(0, math.pi / 2, 0), bevel_ends=False, mat_name="CenoteBase_DriftwoodDark")
    for idx_th in range(10):
        tx_f = -1.26 + idx_th * 0.28
        add_box(bm_c, (0.31, 1.48, 0.045), center=(tx_f, 0.72, 1.69 + (idx_th % 2) * 0.018),
                rot_euler=(-0.14, 0.03 * (idx_th - 4.5), 0), bevel_ratio=0.28, warp=-0.016, bevel_segs=1, mat_name="CenoteBase_Thatch", cuts=2)
    for sx in (-1.28, 1.28):
        add_cyl(bm_c, 0.058, 0.048, 1.68, segs=12, center=(sx, 0.75, 0.84), mat_name="CenoteBase_Driftwood")
        add_cyl(bm_c, 0.065, 0.065, 0.045, segs=10, center=(sx, 0.75, 1.60), bevel_ends=False, mat_name="CenoteBase_Vine")
    # 半割り丸太のベンチ
    add_box(bm_c, (1.25, 0.38, 0.14), center=(-0.55, 0.82, 0.22), bevel_ratio=0.30, bevel_segs=2, mat_name="CenoteBase_Driftwood", cuts=3)
    for sx in (-0.95, -0.15):
        add_box(bm_c, (0.18, 0.34, 0.16), center=(sx, 0.82, 0.08), bevel_ratio=0.25, bevel_segs=1, mat_name="CenoteBase_DriftwoodDark")
    add_box(bm_c, (0.14, 0.42, 0.11), center=(1.42, -0.20, 0.38), bevel_ratio=0.28, bevel_segs=1, mat_name="CenoteBase_Vine")
    make_obj("Deck_AlwaysRoofAndBench", bm_c, parent=carved, default_mat="CenoteBase_Driftwood")

    bm_s1 = bmesh.new()
    bx, by = 0.05, -0.25
    for sx in (-0.42, 0.42):
        add_cyl(bm_s1, 0.048, 0.042, 0.78, segs=12, center=(bx + sx, by, 0.39), mat_name="CenoteBase_Driftwood")
        add_box(bm_s1, (0.11, 0.38, 0.065), center=(bx + sx, by, 0.032), bevel_ratio=0.28, bevel_segs=2, mat_name="CenoteBase_DriftwoodDark")
        for kz_f in (0.24, 0.74):
            add_cyl(bm_s1, 0.055, 0.055, 0.035, segs=8, center=(bx + sx, by, kz_f), bevel_ends=False, mat_name="CenoteBase_Vine")
    for sz_fr in (0.23, 0.77):
        add_box(bm_s1, (0.94, 0.095, 0.065), center=(bx, by, sz_fr), bevel_ratio=0.25, bevel_segs=1, mat_name="CenoteBase_Driftwood", cuts=2)
    gx, gy = 0.74, -0.25
    add_lathe(bm_s1, [(0.0, 0.0), (0.10, 0.0), (0.135, 0.18), (0.115, 0.38), (0.125, 0.42), (0.0, 0.42)], segs=18, center=(gx, gy, 0), wave_amp=0.004, wave_freq=8, mat_name="CenoteBase_Clay")
    add_box(bm_s1, (0.038, 0.022, 0.62), center=(gx, gy, 0.32), bevel_ratio=0.22, bevel_segs=1, mat_name="CenoteBase_DriftwoodDark")
    for k in range(5):
        add_box(bm_s1, (0.052, 0.028, 0.012), center=(gx, gy, 0.43 + k * 0.042), bevel_ratio=0.2, bevel_segs=1, mat_name="CenoteBase_Charcoal")
    make_obj("Deck_S1_ForecastFrameAndGauge", bm_s1, parent=s1, default_mat="CenoteBase_Driftwood")

    bm_face = bmesh.new()
    add_box(bm_face, (0.80, 0.11, 0.48), center=(bx, by, 0.50), bevel_ratio=0.08, bevel_segs=2, mat_name="CenoteBase_LavaStone", grain=0.008, cuts=3)
    make_obj("Deck_BoardFace", bm_face, parent=s1, default_mat="CenoteBase_LavaStone")

    vx, vy = 1.12, -1.05   # Oct 18：鳥の籠はデッキの東側（広げた所）に下ろすので、風見とホラガイの台は元の場所
    bm_vpole = bmesh.new()
    add_cyl(bm_vpole, 0.044, 0.034, 1.50, segs=12, center=(vx, vy, 0.75), mat_name="CenoteBase_Driftwood")
    add_cyl(bm_vpole, 0.052, 0.052, 0.04, segs=8, center=(vx, vy, 0.12), bevel_ends=False, mat_name="CenoteBase_Vine")
    make_obj("Deck_S2_VanePole", bm_vpole, parent=s2, default_mat="CenoteBase_Driftwood")

    vane_piv = make_empty("Deck_VanePivot", loc=(vx, vy, 1.52), parent=s2)
    bm_vbird = bmesh.new()
    add_box(bm_vbird, (0.14, 0.44, 0.14), center=(0, -0.05, 0.05), bevel_ratio=0.38, bevel_segs=2, mat_name="CenoteBase_DriftwoodDark")
    add_cyl(bm_vbird, 0.045, 0.012, 0.16, segs=8, center=(0, -0.32, 0.06), rot_euler=(math.pi / 2, 0, 0), mat_name="CenoteBase_Driftwood")  # くちばし
    for sx_w in (-0.22, 0.22):
        add_box(bm_vbird, (0.32, 0.18, 0.04), center=(sx_w, -0.02, 0.08), rot_euler=(0, 0.15 * (1 if sx_w > 0 else -1), 0.12 * (1 if sx_w > 0 else -1)), bevel_ratio=0.32, bevel_segs=1, mat_name="CenoteBase_Driftwood")
    add_box(bm_vbird, (0.032, 0.48, 0.18), center=(0, 0.38, 0.04), bevel_ratio=0.22, warp=0.025, bevel_segs=2, mat_name="CenoteBase_ClothRed", cuts=3)
    make_obj("Deck_S2_VaneBird", bm_vbird, parent=vane_piv, default_mat="CenoteBase_DriftwoodDark")

    turn_piv = make_empty("Deck_TurnPivot", loc=(-0.78, -0.95, 0.06), parent=s3)
    bm_turn = bmesh.new()
    add_cyl(bm_turn, 0.44, 0.42, 0.08, segs=24, center=(0, 0, 0.04), mat_name="CenoteBase_Driftwood")
    add_box(bm_turn, (0.28, 0.36, 0.18), center=(-0.14, 0.0, 0.16), bevel_ratio=0.25, bevel_segs=2, mat_name="CenoteBase_DriftwoodDark")
    prof_bigconch = [(0.0, 0.0), (0.06, 0.05), (0.14, 0.14), (0.21, 0.26), (0.23, 0.36), (0.16, 0.48), (0.08, 0.56), (0.0, 0.61)]
    add_lathe(bm_turn, prof_bigconch, segs=22, center=(-0.14, 0.28, 0.32), rot_euler=(math.pi / 2 + 0.15, 0, 0), wave_amp=0.018, wave_freq=6, mat_name="CenoteBase_Conch")
    for ta in (0.0, 2.09, 4.18):
        add_cyl(bm_turn, 0.026, 0.020, 0.52, segs=8, center=(0.18 + math.cos(ta) * 0.10, math.sin(ta) * 0.10, 0.28),
                rot_euler=(math.sin(ta) * 0.25, -math.cos(ta) * 0.25, 0), bevel_ends=False, mat_name="CenoteBase_DriftwoodDark")
    add_cyl(bm_turn, 0.045, 0.045, 0.04, segs=10, center=(0.18, 0.0, 0.52), bevel_ends=False, mat_name="CenoteBase_Vine")
    # 3段引き出し式の真鍮望遠鏡＋つる巻きグリップ
    add_cyl(bm_turn, 0.068, 0.058, 0.30, segs=16, center=(0.18, -0.18, 0.60), rot_euler=(math.pi / 2 - 0.18, 0, 0.1), mat_name="CenoteBase_Brass")
    add_cyl(bm_turn, 0.054, 0.046, 0.24, segs=14, center=(0.16, 0.04, 0.56), rot_euler=(math.pi / 2 - 0.18, 0, 0.1), mat_name="CenoteBase_Brass")
    add_cyl(bm_turn, 0.042, 0.036, 0.20, segs=12, center=(0.14, 0.22, 0.53), rot_euler=(math.pi / 2 - 0.18, 0, 0.1), mat_name="CenoteBase_Brass")
    add_cyl(bm_turn, 0.064, 0.064, 0.08, segs=12, center=(0.18, -0.12, 0.59), rot_euler=(math.pi / 2 - 0.18, 0, 0.1), bevel_ends=False, mat_name="CenoteBase_Vine")
    make_obj("Deck_S3_ConchAndTelescope", bm_turn, parent=turn_piv, default_mat="CenoteBase_Driftwood")

    moon_piv = make_empty("Deck_MoonPivot", loc=(bx + 0.32, by - 0.06, 0.68), parent=s3)
    bm_moon = bmesh.new()
    add_cyl(bm_moon, 0.15, 0.15, 0.04, segs=22, center=(0, 0, 0), rot_euler=(math.pi / 2, 0, 0), mat_name="CenoteBase_Driftwood")
    add_cyl(bm_moon, 0.068, 0.068, 0.05, segs=14, center=(0.03, -0.01, 0), rot_euler=(math.pi / 2, 0, 0), mat_name="CenoteBase_ClamInner")
    make_obj("Deck_S3_MoonDisc", bm_moon, parent=moon_piv, default_mat="CenoteBase_Driftwood")

    return root


# 10. Pier (桟橋：目印はトンネルが桟橋に出る付け根 (-2.5, -6.15, 2.0)、+Y＝北)
def build_room_pier():
    root, carved, s1, s2, s3 = make_room_hierarchy("Pier")

    bm_c = bmesh.new()
    for sx in (-1.48, 1.48):
        for iz_p in range(4):
            add_box(bm_c, (0.48, 0.68, 0.68), center=(sx + 0.03 * math.sin(iz_p * 2), 0.20, 0.34 + iz_p * 0.62),
                    rot_euler=(0, 0, 0.06 * math.cos(iz_p)), bevel_ratio=0.32, bevel_segs=2, mat_name="CenoteBase_LimestoneWet", grain=0.022, cuts=2)
    for ix_a in (-1.0, 0.0, 1.0):
        add_box(bm_c, (1.25, 0.72, 0.48), center=(ix_a, 0.20, 2.64 + (0.06 if ix_a == 0 else 0.0)),
                rot_euler=(0, 0.08 * ix_a, 0), bevel_ratio=0.32, bevel_segs=2, mat_name="CenoteBase_LimestoneWet", grain=0.022, cuts=2)
    make_obj("Pier_CarvedArch", bm_c, parent=carved, default_mat="CenoteBase_LimestoneWet")

    bm_gate = bmesh.new()
    for sx in (-1, 1):
        pts_rib = [
            Vector((sx * 1.30, -0.35, 0.0)),
            Vector((sx * 1.38, -0.35, 0.85)),
            Vector((sx * 1.12, -0.35, 1.75)),
            Vector((sx * 0.45, -0.35, 2.38)),
            Vector((-sx * 0.28, -0.35 + 0.05 * sx, 2.72)),
        ]
        add_tube_along_curve(bm_gate, pts_rib, [0.115, 0.105, 0.088, 0.068, 0.048], segs=16, mat_name="CenoteBase_WhaleBone", twist_rib=0.07)
        # 根元のヤシ縄固定と石座
        add_cyl(bm_gate, 0.19, 0.16, 0.12, segs=12, center=(sx * 1.30, -0.35, 0.06), mat_name="CenoteBase_LimestoneWet", grain=0.012)
        for kr in range(3):
            add_cyl(bm_gate, 0.135, 0.135, 0.032, segs=12, center=(sx * 1.32, -0.35, 0.16 + kr * 0.035), bevel_ends=False, mat_name="CenoteBase_Coir")
    for k in range(6):
        add_cyl(bm_gate, 0.145, 0.145, 0.032, segs=12, center=(0, -0.35, 2.46 + k * 0.032), mat_name="CenoteBase_Coir")
    for sy_st in (-2.1, -4.4):
        add_cyl(bm_gate, 0.14, 0.12, 0.42, segs=14, center=(-1.25, sy_st, 0.21), mat_name="CenoteBase_DriftwoodDark", grain=0.008)
        for kr_m in range(3):
            add_cyl(bm_gate, 0.165, 0.165, 0.035, segs=14, center=(-1.25, sy_st, 0.22 + kr_m * 0.038), mat_name="CenoteBase_Coir")
    add_cyl(bm_gate, 0.062, 0.050, 1.15, segs=12, center=(1.28, -2.20, 0.575), mat_name="CenoteBase_Driftwood")
    add_conch_lamp_bm(bm_gate, center=(1.28, -2.20, 1.15), rot_z=0.0)
    add_cyl(bm_gate, 0.058, 0.048, 0.82, segs=12, center=(1.18, -5.55, 0.41), mat_name="CenoteBase_Driftwood")
    add_lathe(bm_gate, [(0.0, 0.80), (0.20, 0.80), (0.255, 0.88), (0.20, 0.88), (0.0, 0.82)], segs=18, center=(1.18, -5.55, 0), wave_amp=0.006, wave_freq=8, mat_name="CenoteBase_Driftwood")
    add_lathe(bm_gate, [(0.0, 0.82), (0.11, 0.85), (0.08, 0.94), (0.0, 0.97)], segs=12, center=(1.18, -5.55, 0), wave_amp=0.010, wave_freq=5, mat_name="CenoteBase_Pile")
    make_obj("Pier_WhaleGateAndProps", bm_gate, parent=s1, default_mat="CenoteBase_WhaleBone")

    bm_noren = bmesh.new()
    add_cyl(bm_noren, 0.042, 0.036, 2.65, segs=14, center=(0, 0.10, 2.45), rot_euler=(0, math.pi / 2, 0), mat_name="CenoteBase_Driftwood")
    for i in range(5):
        nx = -0.96 + i * 0.48
        add_box(bm_noren, (0.43, 0.024, 0.80), center=(nx, 0.10, 2.03), bevel_ratio=0.16, warp=0.022, bevel_segs=2, mat_name="CenoteBase_Noren", cuts=3)
        for lx_loop in (-0.14, 0.14):
            add_cyl(bm_noren, 0.052, 0.052, 0.028, segs=8, center=(nx + lx_loop, 0.10, 2.45), rot_euler=(0, math.pi / 2, 0), bevel_ends=False, mat_name="CenoteBase_Vine")
    make_obj("Pier_Noren", bm_noren, parent=s1, default_mat="CenoteBase_Noren")

    return root


# 独立した2つの大物: CenoteFurniture_BirdBasket (d 1.6, h 0.6) と CenoteFurniture_Nest (差し渡し 1.4 m)
def build_bird_basket_and_nest():
    bm_b = bmesh.new()
    # 直径 2.4 m・高さ 0.4 m（Oct 18 わくさん：4匹がバッグを背負ったまま向きを変えられる大きさ。よじ登れる 0.45 m より低く、どこからでも這って入れる）
    add_woven_round_basket_bm(bm_b, 1.08, 1.20, 0.40, center=(0, 0, 0), n_rings=5, n_ribs=24)
    # 底の縁の擦り切れたヤシ縄補強
    add_lathe(bm_b, [(1.02, 0.0), (1.10, 0.03), (1.04, 0.07)], segs=40, center=(0, 0, 0), wave_amp=0.012, wave_freq=18, mat_name="CenoteBase_Coir")
    pts_arch = []
    for k in range(23):
        t = k / 22
        ang = t * math.pi
        pts_arch.append(Vector((math.cos(ang) * 1.15, 0.0, 0.38 + math.sin(ang) * 1.0)))
    add_tube_along_curve(bm_b, pts_arch, [0.07] * 23, segs=12, mat_name="CenoteBase_Coir", twist_rib=0.12)
    # 置き場所はデッキの南の丸（Oct 18 わくさん：籠は2階のデッキに下ろす）
    basket_ob = make_obj("CenoteFurniture_BirdBasket", bm_b, location=(4.4, -8.5, 4.6), default_mat="CenoteBase_Vine")

    bm_n = bmesh.new()
    prof_n = [(0.0, 0.0), (0.58, 0.0), (0.72, 0.16), (0.68, 0.34), (0.52, 0.34), (0.44, 0.10), (0.0, 0.08)]
    add_lathe(bm_n, prof_n, segs=26, center=(0, 0, 0), wave_amp=0.038, wave_freq=9, mat_name="CenoteBase_Vine", grain=0.012)
    for i in range(18):
        a = (i / 18) * math.tau
        add_box(bm_n, (0.40, 0.10, 0.08), center=(math.cos(a) * 0.60, math.sin(a) * 0.60, 0.26 + (i % 2) * 0.05),
                rot_euler=(0.15, 0.1, a + 1.2), bevel_ratio=0.28, bevel_segs=1, mat_name="CenoteBase_Driftwood", cuts=2)
    for i in range(5):
        a = i * 1.25 + 0.3
        add_box(bm_n, (0.055, 0.24, 0.016), center=(math.cos(a) * 0.24, math.sin(a) * 0.24, 0.10),
                rot_euler=(0.1, 0.15, a), bevel_ratio=0.35, warp=0.012, bevel_segs=1, mat_name="CenoteBase_Feather")
    nest_ob = make_obj("CenoteFurniture_Nest", bm_n, location=(-2.5, -6.50, 7.50), default_mat="CenoteBase_Vine")

    return basket_ob, nest_ob


# 洞窟の坂・回廊・広間の灯り
def build_cave_lamps():
    bm = bmesh.new()
    add_brass_wall_lamp_bm(bm, center=(-4.95, 1.35, 3.85), rot_z=-1.3)
    add_brass_wall_lamp_bm(bm, center=( 5.35, 1.45, 5.35), rot_z=1.3)
    conch_pts = [
        (-3.6, -2.5, 2.0),
        ( 0.2,  3.2, 2.0),
        ( 3.6,  0.4, 2.0),
        (-4.5, -1.2, 2.7),
        (-3.2,  3.1, 4.6),
        ( 2.2, -2.8, 4.6),
    ]
    for cx, cy, cz in conch_pts:
        add_conch_lamp_bm(bm, center=(cx, cy, cz), rot_z=cx)
    return make_obj("CenoteFurniture_CaveLamps", bm, default_mat="CenoteBase_Conch")


# ---------------------------------------------------------------- FBX 書き出し（rocksalt_gen.py の export_fbx 設定準拠）
def export_fbx(ob):
    os.makedirs(EXPORT_DIR, exist_ok=True)
    mod = ob.modifiers.get("outline_preview")
    if mod:
        mod.show_viewport = mod.show_render = False
    for o in bpy.context.scene.objects:
        o.select_set(o == ob)
    bpy.context.view_layer.objects.active = ob
    loc = ob.location.copy()
    ob.location = (0, 0, 0)
    with bpy.context.temp_override(selected_objects=[ob], active_object=ob):
        bpy.ops.export_scene.fbx(
            filepath=EXPORT_DIR + ob.name + ".fbx",
            use_selection=True,
            apply_unit_scale=True,
            apply_scale_options="FBX_SCALE_ALL",
            # 軸の変換は Blender で焼かず（Z が上のまま）、Unity の取り込み設定 Bake Axis Conversion で直す（Oct 18 Claude）。
            # bake_space_transform=True だと、空の親の下の子の向きと位置が壊れる（家具が倒れていた）。
            # Unity では Blender の +Y（北・部屋の奥）が +Z、+Z（上）が +Y になる。
            bake_space_transform=False,
            axis_forward='Y',
            axis_up='Z',
            object_types={"MESH"},
            use_mesh_modifiers=True,
            mesh_smooth_type="FACE",
            add_leaf_bones=False,
            bake_anim=False,
        )
    ob.location = loc
    if mod:
        mod.show_viewport = mod.show_render = True


def collect_hierarchy(root_ob):
    res = []
    stack = [root_ob]
    while stack:
        curr = stack.pop()
        res.append(curr)
        stack.extend(curr.children)
    return res


def export_hierarchy_fbx(root_ob, filename=None, reset_child_locs=False):
    os.makedirs(EXPORT_DIR, exist_ok=True)
    fname = filename or (root_ob.name + ".fbx")
    objs = collect_hierarchy(root_ob)
    saved_root_loc = root_ob.location.copy()
    saved_root_rot = root_ob.rotation_euler.copy()
    root_ob.location = (0, 0, 0)
    root_ob.rotation_euler = Euler((0, 0, 0), 'XYZ')

    saved_child_locs = {}
    if reset_child_locs:
        for ch in root_ob.children:
            saved_child_locs[ch] = ch.location.copy()
            ch.location = (0, 0, 0)

    obj_set = set(objs)
    for o in bpy.context.scene.objects:
        o.select_set(o in obj_set)
    bpy.context.view_layer.objects.active = root_ob

    with bpy.context.temp_override(selected_objects=objs, active_object=root_ob):
        bpy.ops.export_scene.fbx(
            filepath=EXPORT_DIR + fname,
            use_selection=True,
            apply_unit_scale=True,
            apply_scale_options="FBX_SCALE_ALL",
            # 軸の変換は Blender で焼かず（Z が上のまま）、Unity の取り込み設定 Bake Axis Conversion で直す（Oct 18 Claude）。
            # bake_space_transform=True だと、空の親の下の子の向きと位置が壊れる（家具が倒れていた）。
            # Unity では Blender の +Y（北・部屋の奥）が +Z、+Z（上）が +Y になる。
            bake_space_transform=False,
            axis_forward='Y',
            axis_up='Z',
            object_types={"EMPTY", "MESH"},
            use_mesh_modifiers=True,
            mesh_smooth_type="FACE",
            add_leaf_bones=False,
            bake_anim=False,
        )

    if reset_child_locs:
        for ch, loc in saved_child_locs.items():
            ch.location = loc
    root_ob.location = saved_root_loc
    root_ob.rotation_euler = saved_root_rot


# ---------------------------------------------------------------- 洞窟構造と目印（Anchor）の読み込み・配置
def import_cave_and_place_rooms(room_roots):
    keep_prefixes = (
        "CenoteBase_Shell",
        "CenoteBase_Floor",
        "CenoteBase_Ramp",
        "CenoteBase_Gallery",
        "CenoteBase_Lift",
        "CenoteBase_Pier",
        "CenoteBase_Deck",
        "CenoteBase_Anchor_",
    )
    with bpy.data.libraries.load(CAVE_BLEND_PATH, link=False) as (data_from, data_to):
        data_to.objects = [name for name in data_from.objects if name.startswith(keep_prefixes)]

    cave_mat = get_rough_material("CenoteBase_RoughCave", color=(0.64, 0.63, 0.60)) if ROUGH_MODE else None
    anchors = {}
    for ob in data_to.objects:
        if ob is None:
            continue
        bpy.context.scene.collection.objects.link(ob)
        if ob.name.startswith("CenoteBase_Anchor_"):
            rname = ob.name.replace("CenoteBase_Anchor_", "")
            anchors[rname] = ob
        elif ob.type == 'MESH' and ROUGH_MODE:
            ob.data.materials.clear()
            ob.data.materials.append(cave_mat)

    for rname, rroot in room_roots.items():
        anc = anchors.get(rname)
        if anc is not None:
            rroot.location = anc.location.copy()
            rroot.rotation_euler = anc.rotation_euler.copy()
    return anchors


def setup_lighting():
    world = bpy.context.scene.world
    if world is None:
        world = bpy.data.worlds.new("World")
        bpy.context.scene.world = world
    world.use_nodes = True
    nt = world.node_tree
    nt.nodes.clear()
    bg = nt.nodes.new("ShaderNodeBackground")
    bg.inputs["Color"].default_value = (0.38, 0.43, 0.48, 1.0)
    bg.inputs["Strength"].default_value = 0.85
    out = nt.nodes.new("ShaderNodeOutputWorld")
    nt.links.new(bg.outputs["Background"], out.inputs["Surface"])

    sun_data = bpy.data.lights.new("Sun_Furniture", type='SUN')
    sun_data.energy = 2.8
    sun_ob = bpy.data.objects.new("Sun_Furniture", sun_data)
    sun_ob.location = (0, -8, 16)
    sun_ob.rotation_euler = Euler((math.radians(32), math.radians(-15), 0), 'XYZ')
    bpy.context.scene.collection.objects.link(sun_ob)

    light_pts = [
        (0.0, 0.0, 6.8, 220.0),
        (-1.8, 0.5, 4.0, 140.0),
        (2.5, -1.8, 3.8, 160.0),
        (0.0, 2.5, 6.2, 160.0),
        (3.8, 0.5, 6.2, 160.0),
        (1.3, -8.5, 6.8, 160.0),
        (-2.5, -9.5, 5.2, 180.0),
    ]
    for idx, (lx, ly, lz, e) in enumerate(light_pts):
        pt_data = bpy.data.lights.new(f"CaveFill_{idx}", type='POINT')
        pt_data.energy = e
        pt_data.shadow_soft_size = 1.8
        pt_ob = bpy.data.objects.new(f"CaveFill_{idx}", pt_data)
        pt_ob.location = (lx, ly, lz)
        bpy.context.scene.collection.objects.link(pt_ob)


def count_tris_hierarchy(root_ob):
    total = 0
    stack = [root_ob]
    while stack:
        curr = stack.pop()
        if curr.type == 'MESH' and curr.data:
            total += sum(len(p.vertices) - 2 for p in curr.data.polygons)
        stack.extend(curr.children)
    return total


def main():
    clear_scene()
    if not ROUGH_MODE:
        for mname in MAT_LIST:
            get_palette_material(mname)

    kit_root, kit_objs = build_kit(base_y=-22.0)

    room_builders = {
        "MapTable":   build_room_maptable,
        "Trade":      build_room_trade,
        "Nursery":    build_room_nursery,
        "Dye":        build_room_dye,
        "Collection": build_room_collection,
        "Workshop":   build_room_workshop,
        "Drying":     build_room_drying,
        "Refinery":   build_room_refinery,
        "Deck":       build_room_deck,
        "Pier":       build_room_pier,
    }
    room_roots = {}
    for rname, fn in room_builders.items():
        room_roots[rname] = fn()

    basket_ob, nest_ob = build_bird_basket_and_nest()
    cave_lamps = build_cave_lamps()

    if EXPORT:
        export_hierarchy_fbx(kit_root, "CenoteFurniture_Kit.fbx", reset_child_locs=True)
        for rname, rroot in room_roots.items():
            export_hierarchy_fbx(rroot, f"CenoteFurniture_{rname}.fbx")
        export_fbx(basket_ob)
        export_fbx(nest_ob)
        export_fbx(cave_lamps)

    os.makedirs(os.path.dirname(BLEND_PATH), exist_ok=True)
    bpy.ops.wm.save_as_mainfile(filepath=BLEND_PATH)

    anchors = import_cave_and_place_rooms(room_roots)
    setup_lighting()

    bpy.ops.wm.save_as_mainfile(filepath=BLEND_PATH)

    room_tris = {rname: count_tris_hierarchy(rroot) for rname, rroot in room_roots.items()}
    print("Built & Exported CenoteFurniture High-Granularity Step 3:", room_tris)
    return room_roots


if __name__ == "__main__" or True:
    main()
