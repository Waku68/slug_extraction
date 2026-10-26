# セノーテの拠点（CenoteBase）第2版を作るスクリプト（Blender 5.2）
# Oct 17 午後の2階建て変更版 ＋ わくさんの絵に合わせた東西に長い楕円調整版
# Step 3 細密化版: 石灰岩の地層・浸食凹凸・鍾乳石・貝化石・屋上の灌木と苔・流木の板張り1枚ずつの造形とつる縛り
# Oct 18 Claude 細かさの版（わくさん「壁も道具も荒い。粒度を細かく」、ゲームの見た目基準、部品ごと 2 万三角形まで）:
#   殻を内壁 _Shell（目印つき）と外崖 _ShellOuter に分け、網目を横 180 分割・縦 0.2 m に。層のふち（0.55 m ごと、最大 12 cm）、
#   起伏とざらつき、上の縦の溝。横穴は側面を斜めに・天井をアーチにして大きな平らな面をなくした。床は 0.16 m ごとの輪と割れ目。
#   板・角材・丸太・石・縛りは cenote_detail.py（木目の溝・反り・欠け・面取り、2 段の凹凸、らせんの縛り）。
#   回廊は床板を1段2枚に割り、床 _Gallery と手すり・支え木 _GalleryRail に分けた。
#   書き出しは Z が上のまま（Unity の Bake Axis Conversion で直す。export_fbx のコメント）。
# Oct 18 Claude 這える拠点（Unity の CenoteBase シーン、Assets/Base/）：当たり判定に内壁 _Shell_COL（横穴と口の形つき）、
#   手すり _Rail_COL（高さ 0.7 m、回廊の飛び込み口は空ける）、池の鉢と2階の横穴の床つきの _Floor_COL を足した。
#   坂の _Ramp_COL は下が 0.6 m より低い所を床まで詰めた。デッキへの口のまわりと坂・回廊ぞいは壁のうねり・ふくらみを止めた。
# 1 単位 ＝ 1 m、原点＝海面（広間の真ん中の真下）、北＝+Y（12時）、南＝-Y（6時）、東＝+X（3時）、西＝-X（9時）。
# 1階床上面 Z = 2.0 m、2階回廊上面 Z = 4.6 m。

import bpy, bmesh, math, random, os, sys, importlib
from mathutils import Vector, Matrix, Euler
from mathutils import noise as mnoise

# 細かい形の部品（板・角材・丸太・石・縛り）は cenote_detail.py にまとめた（Oct 18 Claude）
sys.path.insert(0, "C:/UnityProjects/ExtractionGame/Blender/Base")
import cenote_detail as D
importlib.reload(D)

EXPORT_DIR = "C:/UnityProjects/ExtractionGame/Assets/Art/Models/Base/CenoteBase/"
BLEND_PATH = "C:/UnityProjects/ExtractionGame/Blender/Base/CenoteBase.blend"
EXPORT = True
ROUGH_MODE = False

ELLIPSE_X = 1.18
ELLIPSE_Y = 0.88

# デッキ（2階、崖の外）。Oct 18 わくさん：桟橋と反対の東へ広げ、直径 2.4 m の鳥の籠を東側に置く。
# 西の端 X -0.95（観測所の家具の目印 (1.3, -7.7) はそのまま）、東の端 X 6.6、南の端 Y -10.8。
# 北は X 3.2 より西が Y -6.2 の崖、東は崖が北へ下がるので、デッキも崖まで北へのばす（崖の線は cliff_y で測る）
DECK_X0, DECK_X1 = -0.95, 6.6
DECK_Y0, DECK_Y1 = -6.2, -10.8
DECK_PAD = (4.4, -8.5)        # 鳥の籠の真ん中。籠のまわりに 1 m の通り道
DECK_PAD_R = 1.35             # 発着の丸（ヤシ縄の輪）の半径。籠は半径 1.2 m


def cliff_y(x, z):
    """外崖の南の面の Y（X・Z の所）。外崖のメッシュに南から光線を当てて測る"""
    ob = bpy.data.objects.get("CenoteBase_ShellOuter")
    if ob is None:
        return DECK_Y0
    hit, loc, nrm, idx = ob.ray_cast(Vector((x, -16.0, z)), Vector((0.0, 1.0, 0.0)))
    return loc.y if hit else DECK_Y0


def deck_north_y(x):
    """デッキの北の端：西は Y -6.2、崖が北へ下がる東は崖まで（崖に 5 cm 食い込ませる）"""
    if x < 3.2:
        return DECK_Y0
    return max(DECK_Y0, cliff_y(x, 4.5) + 0.05)


def nz(p):
    return mnoise.noise(p)


def fbm(p, octaves=3):
    val = 0.0
    amp = 0.5
    freq = 1.0
    for _ in range(octaves):
        val += amp * mnoise.noise(p * freq)
        freq *= 2.07
        amp *= 0.48
    return val


# ---------------------------------------------------------------- マテリアル定義（洞窟と家具で共有）
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
    "CenoteBase_ClothOchre":    "#C2A55E",   # 行商人の舟の帆（Oct 18）
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
    "CenoteBase_Pile":          "#C9C2B0",
    "CenoteBase_Water":         "#3F7F86",
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


def get_rough_material(name="CenoteBase_RoughGrey", color=(0.74, 0.72, 0.68)):
    m = bpy.data.materials.get(name) or bpy.data.materials.new(name)
    m.use_nodes = True
    nt = m.node_tree
    nt.nodes.clear()
    out = nt.nodes.new("ShaderNodeOutputMaterial")
    bsdf = nt.nodes.new("ShaderNodeBsdfPrincipled")
    bsdf.inputs["Base Color"].default_value = (*color, 1.0)
    bsdf.inputs["Roughness"].default_value = 0.85
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
    col = hex_to_LinearRGB(PALETTE_HEX.get(name, "#D9D2C0"))
    m.diffuse_color = (*col, 1.0)
    bsdf.inputs["Base Color"].default_value = (*col, 1.0)

    if name == "CenoteBase_Obsidian":
        bsdf.inputs["Roughness"].default_value = 0.12
    elif name in ("CenoteBase_Water", "CenoteBase_ClamInner"):
        bsdf.inputs["Roughness"].default_value = 0.22
    elif name in ("CenoteBase_Copper", "CenoteBase_Brass"):
        bsdf.inputs["Roughness"].default_value = 0.50
    else:
        bsdf.inputs["Roughness"].default_value = 0.82

    if name == "CenoteBase_LampFlame":
        if "Emission Color" in bsdf.inputs:
            bsdf.inputs["Emission Color"].default_value = (*col, 1.0)
            bsdf.inputs["Emission Strength"].default_value = 4.0
    elif name not in ("CenoteBase_Obsidian", "CenoteBase_Water"):
        # WorldLook.cs と同じ色むら（Mottle）＋細密な岩肌・木目・編み目の2段バンプを付与
        if name in MOTTLE_HEX:
            mcol = hex_to_LinearRGB(MOTTLE_HEX[name])
        else:
            mcol = (col[0] * 0.74, col[1] * 0.73, col[2] * 0.71)
        is_rock = ("Limestone" in name or name in ("CenoteBase_LavaStone", "CenoteBase_CoralSand", "CenoteBase_Clay"))
        tc = nt.nodes.new("ShaderNodeTexCoord")
        noi = nt.nodes.new("ShaderNodeTexNoise")
        noi.inputs["Scale"].default_value = 7.5 if is_rock else 14.0
        noi.inputs["Detail"].default_value = 6.0
        noi.inputs["Roughness"].default_value = 0.72
        noi_fine = nt.nodes.new("ShaderNodeTexNoise")
        noi_fine.inputs["Scale"].default_value = 42.0 if is_rock else 58.0
        noi_fine.inputs["Detail"].default_value = 5.0
        noi_fine.inputs["Roughness"].default_value = 0.65
        mix_n = nt.nodes.new("ShaderNodeMix")
        mix_n.data_type = 'FLOAT'
        mix_n.inputs["Factor"].default_value = 0.42
        cr = nt.nodes.new("ShaderNodeValToRGB")
        cr.color_ramp.elements[0].position = 0.30
        cr.color_ramp.elements[0].color = (*col, 1.0)
        cr.color_ramp.elements[1].position = 0.68
        cr.color_ramp.elements[1].color = (*mcol, 1.0)
        nt.links.new(tc.outputs["Object"], noi.inputs["Vector"])
        nt.links.new(tc.outputs["Object"], noi_fine.inputs["Vector"])
        nt.links.new(noi.outputs["Fac"], mix_n.inputs[2])
        nt.links.new(noi_fine.outputs["Fac"], mix_n.inputs[3])
        nt.links.new(mix_n.outputs[0], cr.inputs["Fac"])
        nt.links.new(cr.outputs["Color"], bsdf.inputs["Base Color"])

        bump = nt.nodes.new("ShaderNodeBump")
        bump.inputs["Strength"].default_value = 0.58 if is_rock else 0.36
        bump.inputs["Distance"].default_value = 0.065 if is_rock else 0.035
        nt.links.new(mix_n.outputs[0], bump.inputs["Height"])
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


# ---------------------------------------------------------------- 座標とプロファイル
def clock_to_rad(clock_hr):
    return math.radians((3.0 - clock_hr) * 30.0)


def ellipse_xy_from_rad(th, r, z=0.0):
    return Vector((math.cos(th) * r * ELLIPSE_X, math.sin(th) * r * ELLIPSE_Y, z))


def polar_xy(clock_hr, r, z=0.0):
    return ellipse_xy_from_rad(clock_to_rad(clock_hr), r, z)


def wall_outward_rot_z(th):
    nx = math.cos(th) / ELLIPSE_X
    ny = math.sin(th) / ELLIPSE_Y
    return math.atan2(ny, nx) - math.pi / 2


HALL_PROFILE = [
    (2.0, 4.4),
    (3.0, 4.8),
    (4.6, 5.0),
    (6.0, 4.8),
    (7.0, 4.0),
    (7.8, 2.8),
    (8.3, 1.2),
    (8.5, 0.0),
]


def hall_radius_at(z):
    if z <= HALL_PROFILE[0][0]:
        return HALL_PROFILE[0][1]
    for i in range(1, len(HALL_PROFILE)):
        z0, r0 = HALL_PROFILE[i - 1]
        z1, r1 = HALL_PROFILE[i]
        if z <= z1:
            t = (z - z0) / (z1 - z0)
            return r0 + (r1 - r0) * t
    return 0.0


def clear_scene():
    for o in list(bpy.data.objects):
        bpy.data.objects.remove(o, do_unlink=True)
    for m in list(bpy.data.meshes):
        bpy.data.meshes.remove(m)
    for mat in list(bpy.data.materials):
        bpy.data.materials.remove(mat)
    for lib in list(bpy.data.libraries):
        bpy.data.libraries.remove(lib)


def make_obj(name, bm, mat=None, location=(0, 0, 0), default_mat="CenoteBase_Limestone"):
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
        default_id = MAT_ID.get(default_mat, MAT_ID["CenoteBase_Limestone"])
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
    bpy.context.scene.collection.objects.link(ob)
    return ob


def merge_cb(bm, cb, xform=None, mat_name=None):
    if xform is not None:
        bmesh.ops.transform(cb, matrix=xform, verts=list(cb.verts))
    if mat_name is not None and mat_name in MAT_ID:
        mid = MAT_ID[mat_name]
        for f in cb.faces:
            f.material_index = mid
    tmp = bpy.data.meshes.new("_tmp")
    cb.to_mesh(tmp)
    cb.free()
    bm.from_mesh(tmp)
    bpy.data.meshes.remove(tmp)


def add_box_bm(bm, size, center=(0, 0, 0), rot_z=0.0, rot_x=0.0, rot_y=0.0, bevel_ratio=0.08, mat_name=None, grain=0.012, cuts=2, detail_step=None):
    w, d, h = size
    xform = Matrix.Translation(center) @ Euler((rot_x, rot_y, rot_z), 'XYZ').to_matrix().to_4x4()
    # 細かい形：木は板か角材、石は角の丸い網目の塊（当たり判定の grain=0 はそのまま単純な箱）
    if grain > 0 and not ROUGH_MODE:
        seed = D.seed_vec(center)
        if mat_name in D.WOOD_MATS:
            if D.is_plank_shape(size):
                L, W, T, rot = D.plank_axes(size)
                cb = D.plank_cb(L, W, T, seed, step=0.24 if L > 2.4 else (0.13 if L > 1.2 else 0.07), max_cross=5)
                merge_cb(bm, cb, xform @ rot, mat_name=mat_name)
            else:
                merge_cb(bm, D.beam_cb(size, seed), xform, mat_name=mat_name)
            return
        if mat_name in D.ROCK_MATS or mat_name == "CenoteBase_Leaf":
            m = min(w, d, h)
            st = detail_step if detail_step is not None else max(0.05, min(0.12, max(w, d, h) / 14.0))
            cb = D.rock_cb(size, seed, step=st,
                           round_k=max(0.2, min(0.48, bevel_ratio * 1.3)), rough=0.10, fine=min(0.02, m * 0.08))
            merge_cb(bm, cb, xform, mat_name=mat_name)
            return
    cb = bmesh.new()
    bmesh.ops.create_cube(cb, size=1.0)
    for v in cb.verts:
        v.co = Vector((v.co.x * w, v.co.y * d, v.co.z * h))
    if bevel_ratio > 0:
        bmesh.ops.bevel(cb, geom=list(cb.edges), offset=min(w, d, h) * bevel_ratio,
                        segments=1, affect="EDGES", profile=0.5, clamp_overlap=True)
    if cuts > 0 and max(w, d, h) > 0.25:
        long_edges = [e for e in cb.edges if (e.verts[0].co - e.verts[1].co).length > 0.28]
        if long_edges:
            bmesh.ops.subdivide_edges(cb, edges=long_edges, cuts=cuts, use_grid_fill=True)
    if grain > 0:
        seed_v = Vector(center) * 1.7
        for v in cb.verts:
            p = v.co * 3.5 + seed_v
            v.co += Vector((nz(p), nz(p + Vector((7, 3, 1))), nz(p + Vector((2, 9, 5))))) * grain
    mat = Matrix.Translation(center) @ Euler((rot_x, rot_y, rot_z), 'XYZ').to_matrix().to_4x4()
    merge_cb(bm, cb, mat, mat_name=mat_name)


def add_cyl_bm(bm, r_bottom, r_top, height, segs=16, center=(0, 0, 0), rot_euler=(0, 0, 0), mat_name=None, grain=0.008, h_cuts=2):
    xform = Matrix.Translation(center) @ Euler(rot_euler, 'XYZ').to_matrix().to_4x4()
    if grain > 0 and not ROUGH_MODE:
        seed = D.seed_vec(center)
        r_max = max(r_bottom, r_top)
        if mat_name in D.WOOD_MATS and height > 0.12:
            merge_cb(bm, D.log_cb(r_bottom, r_top, height, seed), xform, mat_name=mat_name)
            return
        if mat_name in ("CenoteBase_Vine", "CenoteBase_Coir") and height <= 0.12 and r_max > height * 0.4:
            # 薄い輪＝縛り。つる・ヤシ縄を2〜3周巻いたらせんにする
            cord = max(0.008, min(0.016, height * 0.22))
            cb = D.lashing_cb((0, 0, 0), (0, 0, 1), r_max - cord, width=height, turns=max(2.0, height / (cord * 1.6)), cord=cord)
            merge_cb(bm, cb, xform, mat_name=mat_name)
            return
        if r_max >= 0.09:
            segs = max(segs, min(24, int(round(math.tau * r_max / 0.04))))
        h_cuts = max(h_cuts, min(12, int(height / 0.14)))
    cb = bmesh.new()
    bmesh.ops.create_cone(cb, cap_ends=True, cap_tris=False, segments=segs,
                          radius1=r_bottom, radius2=r_top, depth=height)
    if h_cuts > 0 and height > 0.25:
        v_edges = [e for e in cb.edges if abs(e.verts[0].co.z - e.verts[1].co.z) > height * 0.4]
        if v_edges:
            bmesh.ops.subdivide_edges(cb, edges=v_edges, cuts=h_cuts)
    if grain > 0:
        seed_v = Vector(center) * 2.1
        for v in cb.verts:
            k = 1.0 + (grain / max(0.04, r_bottom)) * nz(v.co * 4.5 + seed_v)
            v.co.x *= k
            v.co.y *= k
    mat = Matrix.Translation(center) @ Euler(rot_euler, 'XYZ').to_matrix().to_4x4()
    merge_cb(bm, cb, mat, mat_name=mat_name)


def add_strut_between(bm, p_wall, p_support, thickness=0.14, mat_name="CenoteBase_Driftwood"):
    diff = p_support - p_wall
    length = diff.length
    if length < 1e-4:
        return
    mid = (p_wall + p_support) * 0.5
    quat = diff.normalized().to_track_quat('Z', 'X')
    if not ROUGH_MODE:
        # 細かい形：節と木目の丸太＋両端のつるの縛り
        m_rot = Matrix.Translation(mid) @ quat.to_matrix().to_4x4()
        merge_cb(bm, D.log_cb(thickness * 0.58, thickness * 0.48, length, D.seed_vec(mid)), m_rot, mat_name=mat_name)
        for t_pos in (0.12, 0.88):
            pt = p_wall.lerp(p_support, t_pos)
            r_here = thickness * (0.58 + (0.48 - 0.58) * t_pos)
            cb_l = D.lashing_cb(pt, diff.normalized(), r_here, width=0.06, turns=2.5, cord=0.012)
            merge_cb(bm, cb_l, None, mat_name="CenoteBase_Vine")
        return
    cb = bmesh.new()
    bmesh.ops.create_cone(cb, cap_ends=True, cap_tris=False, segments=8,
                          radius1=thickness * 0.58, radius2=thickness * 0.48, depth=length)
    v_edges = [e for e in cb.edges if abs(e.verts[0].co.z - e.verts[1].co.z) > length * 0.4]
    if v_edges:
        bmesh.ops.subdivide_edges(cb, edges=v_edges, cuts=1)
    for v in cb.verts:
        v.co.x += 0.015 * nz(v.co * 3.0 + mid)
        v.co.y += 0.015 * nz(v.co * 3.0 + mid + Vector((5, 2, 1)))
    mat = Matrix.Translation(mid) @ quat.to_matrix().to_4x4()
    merge_cb(bm, cb, mat, mat_name=mat_name)
    # 壁際と先端のつる巻き (CenoteBase_Vine)
    for t_pos in (0.12, 0.88):
        pt = p_wall.lerp(p_support, t_pos)
        for k in (-0.016, 0.016):
            cb_v = bmesh.new()
            bmesh.ops.create_cone(cb_v, cap_ends=True, cap_tris=False, segments=8,
                                  radius1=thickness * 0.62, radius2=thickness * 0.62, depth=0.025)
            m_v = Matrix.Translation(pt + diff.normalized() * k) @ quat.to_matrix().to_4x4()
            merge_cb(bm, cb_v, m_v, mat_name="CenoteBase_Vine")


# ---------------------------------------------------------------- 1. CenoteBase_Shell（高密度な石灰岩の地層・浸食・鍾乳石・貝化石・屋上の低木と苔）
def build_shell():
    """洞窟の殻。Oct 18 Claude：網目を細かく（横 180 分割≒0.18 m、縦 0.2 m）し、内壁と外崖の2つに分けた（それぞれ 2 万三角形まで）。
    内壁：石灰岩の層の張り出し（0.55 m ごと、下が切り立ったふち、最大 12 cm）、0.1〜0.4 m の起伏（最大 7.5 cm）と細かいざらつき、5.3 m より上の縦の溝（深さ 6 cm）。
    トゥーンの3段の影は小さな傾きでは段が変わらないので、凹凸は「ゲームで見えるぎりぎり」まで強めにしてある。
    外崖：同じ起伏と層。屋上の苔と灌木は角の丸い網目の塊。"""
    bm_in = bmesh.new()
    bm_out = bmesh.new()
    n_theta = 180
    z_wall_levels = [0.0, 0.65, 1.3]
    z = 2.0
    while z < 8.15 - 1e-6:
        z_wall_levels.append(round(z, 4))
        z += 0.2
    z_wall_levels += [8.15, 8.35, 8.55, 8.75, 8.95, 9.15, 9.3, 9.4, 9.5]
    nz_cnt = len(z_wall_levels)
    iz_floor = z_wall_levels.index(2.0)

    hole_cx, hole_cy = 1.8, 0.0
    hole_r = 1.0

    def ang_diff(a, b):
        return abs((a - b + math.pi) % (2 * math.pi) - math.pi)

    niches = [
        (5.4,  2.0, 2.6, 1.5),
        (4.0,  2.0, 2.0, 1.2),
        (12.0, 4.6, 2.6, 1.4),
        (1.5,  4.6, 2.0, 1.2),
        (3.0,  4.6, 2.0, 1.2),
        (4.3,  4.6, 2.2, 1.2),
    ]

    def smooth01(e0, e1, x):
        t = max(0.0, min(1.0, (x - e0) / (e1 - e0)))
        return t * t * (3.0 - 2.0 * t)

    def niche_depth_offset(th, z):
        # 横穴：側面は斜めに広がり（網目 4〜5 列）、天井はアーチ（3 列）。1 列で 1.5 m 引っ込むと大きな平らな面になるため（Oct 18 Claude）
        best = 0.0
        for nc, nz0, nw, nh in niches:
            nth = clock_to_rad(nc)
            r_here = hall_radius_at(nz0 + nh * 0.5)
            half_ang = (nw * 0.5) / max(2.5, r_here)
            da = ang_diff(th, nth)
            if da > half_ang * 1.5:
                continue
            a = 1.0 - smooth01(half_ang * 0.9, half_ang * 1.45, da)
            top = nz0 + nh + 0.12 + 0.18 * max(0.0, 1.0 - (da / (half_ang * 1.2)) ** 2)
            b = smooth01(nz0 - 0.10, nz0 + 0.06, z) * (1.0 - smooth01(top - 0.55, top + 0.05, z))
            best = max(best, 1.55 * a * b)
        return best

    th_7 = clock_to_rad(7.0)
    th_56 = clock_to_rad(5.65)

    # 口の形（Oct 18 Claude：四角い穴をアーチに）。(真ん中の向き, 半分の幅 rad, 下, アーチの始まり, てっぺん)
    # 海側の口（7時、1階）と、デッキへの口（5時半すぎ、2階）。アーチは半楕円
    OPENINGS = [(th_7, 0.28, 1.95, 3.35, 4.55), (th_56, 0.18, 4.55, 5.45, 6.25)]

    def open_half(op, z):
        thc, hw, zb, zs, zt = op
        if z < zb or z > zt:
            return -1.0
        if z <= zs:
            return hw
        k = (z - zs) / (zt - zs)
        return hw * math.sqrt(max(0.0, 1.0 - k * k))

    def is_opening(th, z):
        for op in OPENINGS:
            if ang_diff(th, op[0]) < open_half(op, z):
                return True
        return False

    def opening_outline(op, n=60):
        """口のふちの線（左の縦・アーチ・右の縦・下）を (向き, 高さ) の点で"""
        thc, hw, zb, zs, zt = op
        pts = []
        for i in range(n + 1):
            pts.append((thc - hw, zb + (zs - zb) * i / n))
        for i in range(n + 1):
            a = math.pi - math.pi * i / n
            pts.append((thc + math.cos(a) * hw, zs + math.sin(a) * (zt - zs)))
        for i in range(n + 1):
            pts.append((thc + hw, zs - (zs - zb) * i / n))
        for i in range(n + 1):
            pts.append((thc + hw - 2 * hw * i / n, zb))
        return pts

    def opening_calm(th, z):
        # 口のまわりは壁のうねりを止める。うねりで敷居が持ち上がると這って通れず、アーチの形もゆがむ（Oct 18 Claude）
        k = 1.0
        if 4.0 <= z <= 6.8:
            k = min(k, smooth01(0.22, 0.45, ang_diff(th, th_56)))
        if z <= 5.0:
            k = min(k, smooth01(0.33, 0.60, ang_diff(th, th_7)))
        return k

    def walkway_mask(th, z):
        # 坂（8時→11時、Z 2.0→4.6）と回廊（11時→5時51分、Z 4.6）に沿った壁。ここでは壁が内へふくらまないようにする
        # （ふくらむと 1 m 幅の坂が狭まって這って通れない。Oct 18 Claude）
        c = (3.0 - math.degrees(th) / 30.0) % 12.0
        m = 0.0
        if 7.6 <= c <= 11.4:
            zr = 2.0 + 2.6 * max(0.0, min(1.0, (c - 8.0) / 3.0))
            dz = z - zr
            a = smooth01(7.6, 7.95, c) * (1.0 - smooth01(11.05, 11.4, c))
            m = max(m, a * smooth01(-0.45, -0.15, dz) * (1.0 - smooth01(1.0, 1.4, dz)))
        cg = c if c >= 10.0 else c + 12.0       # 11時〜翌5時51分を 11〜17.85 に
        if 10.8 <= cg <= 18.1:
            a = smooth01(10.8, 11.1, cg) * (1.0 - smooth01(17.85, 18.1, cg))
            m = max(m, a * smooth01(4.15, 4.45, z) * (1.0 - smooth01(5.6, 6.0, z)))
        return m

    def fine_relief(th, z, r_ref, strata_amp=0.12, flute=True):
        """細かい凹凸（m）。外向きが正。r_ref はおおよその半径（周波数をメートルにそろえる）"""
        pw = Vector((math.cos(th) * r_ref, math.sin(th) * r_ref, z))
        rel = 0.075 * fbm(pw * 1.6, 3) + 0.035 * nz(pw * 4.2) + 0.014 * nz(pw * 8.5)
        # 石灰岩の層：0.55 m ごとに下が切り立ったふち（下のふちが一番出ていて、上へ向かって引っ込む）
        u = (z + 0.18 * nz(pw * 0.45)) / 0.55
        f = u - math.floor(u)
        lip = max(0.0, 1.0 - f / 0.38) ** 1.4
        amp = strata_amp * max(0.0, min(1.0, 0.55 + 1.3 * nz(pw * 0.32 + Vector((4.0, 1.0, 2.0)))))
        rel += lip * amp
        # 上のほうの縦の溝（雨水が削った筋）
        if flute and z > 5.3:
            fade = min(1.0, (z - 5.3) / 1.2)
            fl = max(0.0, math.cos(th * 47.0 + 1.6 * nz(pw * 0.7))) ** 3
            rel -= 0.06 * fl * fade
        return rel

    def ext_point(th, z, fine=True):
        rx = 11.2 if z < 2.0 else (10.9 - (z - 2.0) * 0.28)
        ry = 8.2  if z < 2.0 else (7.9  - (z - 2.0) * 0.20)
        south_blend = max(0.0, min(1.0, (-math.sin(th) - 0.45) / 0.45))
        rx_eff = rx * (1.0 - south_blend) + 5.2 * south_blend
        x = math.cos(th) * rx_eff + 0.3 * (1.0 - south_blend)
        y = math.sin(th) * ry
        if y < -6.20:
            y = -6.20 + (y + 6.20) * 0.15
        p3 = Vector((math.cos(th) * 2.2, math.sin(th) * 2.2, z * 0.55))
        strata = 0.13 * math.sin(z * 5.2 + nz(p3) * 2.4)
        crag = (1.0 - 0.55 * south_blend) * (0.45 * fbm(p3, 3) + strata)
        if z < 9.35 and fine:
            crag += fine_relief(th, z, 9.0, strata_amp=0.14, flute=False)
        x += math.cos(th) * crag
        y += math.sin(th) * crag
        wave_z = 0.22 * math.sin(th * 3.0 + 0.5) + 0.14 * math.cos(th * 5.0 - 0.8) + 0.12 * nz(p3 * 1.8)
        ze = z + (wave_z * opening_calm(th, z) if 2.3 <= z <= 8.8 else 0.04 * nz(p3 * 2.0))
        if z >= 9.4:
            ze = 9.5 + 0.28 * fbm(Vector((x * 0.25, y * 0.25, 1.0)), 3) + 0.03 * nz(Vector((x * 2.5, y * 2.5, 3.0)))
        return Vector((x, y, ze))

    def int_point(th, z, fine=True):
        p3 = Vector((math.cos(th) * 2.5, math.sin(th) * 2.5, z * 0.65))
        if z >= 8.15:
            r_sh = hole_r + 0.10 * fbm(p3 * 1.8, 2) + 0.03 * nz(p3 * 6.0)
            ze = z if z < 9.4 else (9.45 + 0.18 * fbm(p3, 2))
            return Vector((hole_cx + math.cos(th) * r_sh, hole_cy + math.sin(th) * r_sh, ze))
        r0 = hall_radius_at(max(2.0, z)) + niche_depth_offset(th, z)
        # 石灰岩の横縞（隆起サンゴ礁の層）と浸食されたくぼみ
        strata = 0.11 * math.sin(z * 5.6 + nz(p3) * 2.2)
        pocket = 0.28 * fbm(p3 * 1.25, 3)
        # 床ぎわ（2.0〜2.25 m）は床とすき間が出ないよう細かい凹凸を弱める
        k_fine = max(0.0, min(1.0, (z - 2.0) / 0.25)) if fine else 0.0
        bump = strata + pocket + k_fine * fine_relief(th, z, 5.0)
        if bump < 0.0:
            bump *= 1.0 - walkway_mask(th, z)
        r = max(1.05, r0 + bump)
        shift_t = max(0.0, min(1.0, (z - 6.0) / (8.15 - 6.0))) ** 1.5
        cx = hole_cx * shift_t
        cy = hole_cy * shift_t
        rx_here = (r * ELLIPSE_X) * (1.0 - shift_t) + hole_r * shift_t
        ry_here = (r * ELLIPSE_Y) * (1.0 - shift_t) + hole_r * shift_t
        wave_z = 0.24 * math.sin(th * 3.0 + 0.4) + 0.15 * math.cos(th * 5.0 - 0.6) + 0.10 * nz(p3 * 2.0)
        ze = z + (wave_z * opening_calm(th, z) if 2.3 <= z <= 7.8 else 0.0)
        return Vector((cx + math.cos(th) * rx_here, cy + math.sin(th) * ry_here, ze))

    def cell_open(iz, it):
        if iz < iz_floor or iz >= nz_cnt - 1:
            return False
        z_mid = 0.5 * (z_wall_levels[iz] + z_wall_levels[iz + 1])
        if z_mid > 8.45:
            return False
        th_mid = ((it % n_theta + 0.5) / n_theta) * math.tau - math.pi
        return is_opening(th_mid, z_mid)

    # 網目の (向き, 高さ)。口のふちに触れる頂点は、口の線の一番近い所へ寄せる（段々のふちをなめらかなアーチに。Oct 18 Claude）
    TH = [[(it / n_theta) * math.tau - math.pi for it in range(n_theta)] for _ in z_wall_levels]
    ZZ = [[z for _ in range(n_theta)] for z in z_wall_levels]
    R_M = 5.5   # 向きの差をメートルに直す半径
    outlines = [(op, opening_outline(op)) for op in OPENINGS]
    for iz in range(iz_floor, nz_cnt - 1):
        for it in range(n_theta):
            cells = [cell_open(iz - 1, it - 1), cell_open(iz - 1, it), cell_open(iz, it - 1), cell_open(iz, it)]
            if not (any(cells) and not all(cells)):
                continue
            th0, z0 = TH[iz][it], ZZ[iz][it]
            op, pts = min(outlines, key=lambda o: ang_diff(th0, o[0][0]))
            best, bd = None, 1e9
            for th_b, z_b in pts:
                dth = (th_b - th0 + math.pi) % (2 * math.pi) - math.pi
                d = (dth * R_M) ** 2 + (z_b - z0) ** 2
                if d < bd:
                    bd, best = d, (th0 + dth, z_b)
            if bd < 0.35 ** 2:
                TH[iz][it], ZZ[iz][it] = best

    # 頂点は両方のメッシュに作り、使わなかった物は最後に消す
    ext_pts = [[ext_point(TH[iz][it], ZZ[iz][it]) for it in range(n_theta)] for iz in range(nz_cnt)]
    int_pts = [[int_point(TH[iz][it], ZZ[iz][it]) if iz >= iz_floor else None for it in range(n_theta)]
               for iz in range(nz_cnt)]

    def make_verts(bm):
        ev = [[bm.verts.new(p) for p in row] for row in ext_pts]
        iv = [[(bm.verts.new(p) if p is not None else None) for p in row] for row in int_pts]
        return ev, iv

    ext_o, int_o = make_verts(bm_out)
    ext_i, int_i = make_verts(bm_in)

    id_lime = MAT_ID["CenoteBase_Limestone"]
    id_wet = MAT_ID["CenoteBase_LimestoneWet"]
    id_moss = MAT_ID["CenoteBase_Moss"]

    def pick_ring_mat(iz, is_interior=True):
        z_mid = 0.5 * (z_wall_levels[iz] + z_wall_levels[min(iz + 1, nz_cnt - 1)])
        if z_mid < 2.95:
            return id_wet
        if not is_interior and z_mid >= 9.05:
            return id_moss
        if is_interior and z_mid >= 8.75:
            return id_moss
        return id_lime

    for iz in range(nz_cnt - 1):
        for it in range(n_theta):
            it_next = (it + 1) % n_theta
            if not cell_open(iz, it):
                f_ext = bm_out.faces.new((ext_o[iz][it], ext_o[iz][it_next], ext_o[iz + 1][it_next], ext_o[iz + 1][it]))
                f_ext.material_index = pick_ring_mat(iz, is_interior=False)
                if iz >= iz_floor:
                    f_int = bm_in.faces.new((int_i[iz][it], int_i[iz + 1][it], int_i[iz + 1][it_next], int_i[iz][it_next]))
                    f_int.material_index = pick_ring_mat(iz, is_interior=True)

    # 開いた口の側面（トンネルの壁、内壁側のメッシュに入れる）。Oct 18 Claude：奥行きを 6 つに分け、ごつごつさせる
    # （前は内壁から外崖まで1枚の大きな平らな面だった）。途中の頂点は隣の面と共有して、すき間を作らない
    N_DEPTH = 6
    chains = {}

    def chain(iz, it):
        key = (iz, it % n_theta)
        if key in chains:
            return chains[key]
        a, b = int_i[iz][it % n_theta], ext_i[iz][it % n_theta]
        vs = [a]
        for k in range(1, N_DEPTH):
            t = k / N_DEPTH
            pnt = a.co.lerp(b.co, t)
            env = math.sin(math.pi * t)
            off = Vector((nz(pnt * 2.2), nz(pnt * 2.2 + Vector((5.1, 2.3, 7.7))), nz(pnt * 2.2 + Vector((3.3, 8.4, 1.2))) * 0.6))
            off += Vector((nz(pnt * 6.0 + Vector((1, 1, 1))), nz(pnt * 6.0 + Vector((4, 2, 9))), 0.0)) * 0.35
            if pnt.z < 2.2:
                off.z = 0.0
            vs.append(bm_in.verts.new(pnt + off * 0.12 * env))
        vs.append(b)
        chains[key] = vs
        return vs

    def strip(A, B, mat, swap=False):
        for k in range(N_DEPTH):
            q = (A[k], A[k + 1], B[k + 1], B[k]) if swap else (A[k], B[k], B[k + 1], A[k + 1])
            bm_in.faces.new(q).material_index = mat

    for iz in range(iz_floor, nz_cnt - 1):
        for it in range(n_theta):
            if not cell_open(iz, it):
                continue
            it_next = (it + 1) % n_theta
            it_prev = (it - 1) % n_theta
            m = pick_ring_mat(iz, True)
            if not cell_open(iz - 1, it):
                strip(chain(iz, it), chain(iz, it_next), m)
            if not cell_open(iz + 1, it):
                strip(chain(iz + 1, it_next), chain(iz + 1, it), m)
            if not cell_open(iz, it_prev):
                strip(chain(iz, it), chain(iz + 1, it), m, swap=True)
            if not cell_open(iz, it_next):
                strip(chain(iz + 1, it_next), chain(iz, it_next), m, swap=True)

    # 屋上の縁（外崖側）と底の輪（外崖側）
    for it in range(n_theta):
        it_next = (it + 1) % n_theta
        f_top = bm_out.faces.new((ext_o[nz_cnt - 1][it], ext_o[nz_cnt - 1][it_next], int_o[nz_cnt - 1][it_next], int_o[nz_cnt - 1][it]))
        f_top.material_index = id_moss
        f_bot = bm_out.faces.new((ext_o[0][it_next], ext_o[0][it], int_o[iz_floor][it], int_o[iz_floor][it_next]))
        f_bot.material_index = id_wet

    for bmx in (bm_in, bm_out):
        loose = [v for v in bmx.verts if not v.link_faces]
        bmesh.ops.delete(bmx, geom=loose, context="VERTS")

    # 天窓の縁や張り出しの下に垂れる小さなつらら石（鍾乳石）
    rng = random.Random(42)
    for i in range(18):
        ang = (i / 18) * math.tau + rng.uniform(-0.08, 0.08)
        r_drip = hole_r + rng.uniform(0.05, 0.45)
        dx = hole_cx + math.cos(ang) * r_drip
        dy = hole_cy + math.sin(ang) * r_drip
        dh = rng.uniform(0.28, 0.62)
        dr = rng.uniform(0.045, 0.085)
        add_cyl_bm(bm_in, 0.012, dr, dh, segs=9, center=(dx, dy, 8.15 - dh * 0.45), mat_name="CenoteBase_LimestoneWet", grain=0.008, h_cuts=3)

    # 海側の口のアーチ下や横穴の上縁のつらら石
    for clk, z_top in [(6.8, 4.45), (7.0, 4.48), (7.2, 4.45), (5.2, 3.45), (5.6, 3.45), (3.9, 3.15), (4.1, 3.15), (11.8, 5.95), (12.2, 5.95)]:
        p_d = polar_xy(clk, hall_radius_at(z_top) - 0.05, z_top)
        dh = rng.uniform(0.22, 0.42)
        add_cyl_bm(bm_in, 0.010, 0.055, dh, segs=8, center=p_d - Vector((0, 0, dh * 0.45)), mat_name="CenoteBase_LimestoneWet", grain=0.006, h_cuts=3)

    # 内壁の苔むした岩棚（壁に深く埋めて、層のふちから生えたように）
    ledge_spots = [
        (7.8, 3.2, "CenoteBase_Moss"), (9.3, 2.7, "CenoteBase_LimestoneWet"),
        (10.4, 3.1, "CenoteBase_Moss"), (11.4, 2.9, "CenoteBase_Moss"),
        (2.3, 3.3, "CenoteBase_Limestone"), (3.4, 2.7, "CenoteBase_LimestoneWet"),
        (6.2, 3.3, "CenoteBase_LimestoneWet"), (12.6, 3.4, "CenoteBase_Limestone"),
    ]
    for l_clk, l_z, l_mat in ledge_spots:
        th_l = clock_to_rad(l_clk)
        if niche_depth_offset(th_l, l_z) > 0.05 or niche_depth_offset(th_l, l_z + 0.5) > 0.05:
            continue  # 横穴の口には置かない（宙に浮くため）
        p_wall_l = int_point(th_l, l_z)
        out_dir_l = Vector((p_wall_l.x, p_wall_l.y, 0.0)).normalized()
        p_l = p_wall_l + out_dir_l * 0.20
        rz_l = wall_outward_rot_z(th_l)
        add_box_bm(bm_in, (rng.uniform(1.1, 1.7), rng.uniform(0.55, 0.75), rng.uniform(0.22, 0.34)),
                   center=p_l, rot_z=rz_l, bevel_ratio=0.35, mat_name=l_mat, grain=0.035, cuts=1, detail_step=0.16)

    # 南〜東西の外崖の岩棚・張り出し
    for ext_clk, ext_z, ext_mat in [
        (6.3, 5.2, "CenoteBase_Limestone"), (5.9, 3.0, "CenoteBase_LimestoneWet"),   # 5.9 時：海側の口のアーチにかからないよう（Oct 18）
        (7.6, 4.8, "CenoteBase_Limestone"), (7.9, 6.5, "CenoteBase_Limestone"),
        (5.1, 3.6, "CenoteBase_LimestoneWet"), (4.8, 5.8, "CenoteBase_Limestone"),
        (8.4, 5.5, "CenoteBase_Limestone"), (3.8, 5.2, "CenoteBase_Limestone"),
    ]:
        th_e = clock_to_rad(ext_clk)
        pe = ext_point(th_e, ext_z)
        pe = pe - Vector((pe.x, pe.y, 0.0)).normalized() * 0.22  # 外崖に埋め込む
        rz_e = wall_outward_rot_z(th_e)
        add_box_bm(bm_out, (rng.uniform(1.2, 2.1), rng.uniform(0.6, 0.85), rng.uniform(0.28, 0.50)),
                   center=pe, rot_z=rz_e, bevel_ratio=0.35, mat_name=ext_mat, grain=0.045, cuts=1, detail_step=0.25)

    # 壁に埋まった巻貝・二枚貝の化石 (CenoteBase_ClamShell)
    fossil_spots = [(8.4, 3.1), (9.8, 4.4), (10.6, 5.2), (1.0, 3.4), (2.4, 3.6), (2.6, 5.8), (6.3, 3.2), (11.2, 3.0)]
    for f_clk, f_z in fossil_spots:
        th_f = clock_to_rad(f_clk)
        p_f = int_point(th_f, f_z)
        rz_f = wall_outward_rot_z(th_f)
        add_cyl_bm(bm_in, 0.14, 0.09, 0.06, segs=14, center=p_f, rot_euler=(math.pi / 2, rng.uniform(0, 3.0), rz_f), mat_name="CenoteBase_ClamShell", grain=0.006, h_cuts=0)

    # 小島頂上の低い熱帯の灌木と苔むした岩塊 (Z ≈ 9.5 m)
    for i in range(14):
        ang = (i / 14) * math.tau + 0.2
        r_top = rng.uniform(3.2, 6.2)
        bx = math.cos(ang) * r_top * 1.15
        by = math.sin(ang) * r_top * 0.85
        bz = 9.35 + 0.15 * math.sin(ang * 3)
        add_box_bm(bm_out, (rng.uniform(0.9, 1.6), rng.uniform(0.7, 1.3), rng.uniform(0.35, 0.65)),
                   center=(bx, by, bz), rot_z=ang, bevel_ratio=0.35, mat_name="CenoteBase_Moss", grain=0.04, cuts=1, detail_step=0.32)
        if i % 2 == 0:
            add_box_bm(bm_out, (rng.uniform(0.7, 1.2), rng.uniform(0.6, 1.0), rng.uniform(0.45, 0.80)),
                       center=(bx + 0.2, by, bz + 0.32), rot_z=ang + 0.5, bevel_ratio=0.4, mat_name="CenoteBase_Leaf", grain=0.05, cuts=1, detail_step=0.30)

    shell = make_obj("CenoteBase_Shell", bm_in, default_mat="CenoteBase_Limestone")
    outer = make_obj("CenoteBase_ShellOuter", bm_out, default_mat="CenoteBase_Limestone")

    # 当たり判定 CenoteBase_Shell_COL（Oct 18 Claude）：内壁を粗い網目（周り 72・縦 0.2 m、細かい凹凸なし）で。
    # 横穴の形と、海側の口・デッキへの口（穴と、その側面）も同じ形にする。床から 8.15 m（天窓の下）まで
    bm_c = bmesh.new()
    nc = 72
    zc = [z for z in z_wall_levels if 2.0 - 1e-6 <= z <= 8.15 + 1e-6]
    ths = [(it / nc) * math.tau - math.pi for it in range(nc)]
    iv = [[bm_c.verts.new(int_point(th, z, fine=False)) for th in ths] for z in zc]
    ev = [[bm_c.verts.new(ext_point(th, z, fine=False)) for th in ths] for z in zc]

    def c_open(iz, it):
        if iz < 0 or iz >= len(zc) - 1:
            return False
        z_mid = 0.5 * (zc[iz] + zc[iz + 1])
        th_mid = ((it + 0.5) / nc) * math.tau - math.pi
        return is_opening(th_mid, z_mid)

    for iz in range(len(zc) - 1):
        for it in range(nc):
            itn = (it + 1) % nc
            if not c_open(iz, it):
                bm_c.faces.new((iv[iz][it], iv[iz + 1][it], iv[iz + 1][itn], iv[iz][itn]))
                continue
            if not c_open(iz - 1, it):
                bm_c.faces.new((iv[iz][it], iv[iz][itn], ev[iz][itn], ev[iz][it]))
            if not c_open(iz + 1, it):
                bm_c.faces.new((iv[iz + 1][itn], iv[iz + 1][it], ev[iz + 1][it], ev[iz + 1][itn]))
            if not c_open(iz, (it - 1) % nc):
                bm_c.faces.new((iv[iz][it], ev[iz][it], ev[iz + 1][it], iv[iz + 1][it]))
            if not c_open(iz, itn):
                bm_c.faces.new((ev[iz][itn], iv[iz][itn], iv[iz + 1][itn], ev[iz + 1][itn]))
    loose = [v for v in bm_c.verts if not v.link_faces]
    bmesh.ops.delete(bm_c, geom=loose, context="VERTS")
    shell_col = make_obj("CenoteBase_Shell_COL", bm_c, default_mat="CenoteBase_Limestone")
    shell_col.hide_render = True
    return shell, outer, shell_col


# ---------------------------------------------------------------- 2. CenoteBase_Floor（細かい岩盤分割・池の縁の自然石・水面波紋）
def build_floor():
    """床。Oct 18 Claude：輪を 0.16 m ごと・周りを 168 分割に細かくし、岩盤の細かい起伏と浅い割れ目（幅 0.1 m 前後、深さ 2.6 cm）を足した。"""
    bm = bmesh.new()
    n_theta = 168
    radii_cfg = [
        (0.12, 1.38), (0.40, 1.385), (0.65, 1.39), (0.90, 1.40), (1.15, 1.41), (1.32, 1.425),
        (1.45, 1.44), (1.52, 1.58), (1.56, 1.72), (1.61, 1.88), (1.66, 2.00),
    ]
    n_pool = len(radii_cfg)
    r = 1.82
    while r < 5.85 - 1e-6:
        radii_cfg.append((round(r, 3), 2.00))
        r += 0.16
    radii_cfg.append((5.85, 2.00))
    pool_cx, pool_cy = 1.8, 0.0
    ring_verts = []
    ring_r = []
    for ir, (r_val, z_base) in enumerate(radii_cfg):
        row = []
        for it in range(n_theta):
            th = (it / n_theta) * math.tau
            if ir < n_pool:
                x = pool_cx + math.cos(th) * r_val
                y = pool_cy + math.sin(th) * r_val
            else:
                blend = (r_val - 1.66) / (5.85 - 1.66)
                cx_here = pool_cx * (1.0 - blend)
                x = cx_here + math.cos(th) * r_val * (1.0 + (ELLIPSE_X - 1.0) * blend)
                y = math.sin(th) * r_val * (1.0 + (ELLIPSE_Y - 1.0) * blend)
            p3 = Vector((x * 0.6, y * 0.6, z_base))
            pw = Vector((x, y, 0.0))
            if z_base >= 1.99:
                dz = 0.028 * fbm(p3, 2) + 0.022 * fbm(pw * 2.6, 2) + 0.008 * nz(pw * 7.0)
                crack = abs(nz(pw * 0.85 + Vector((2.0, 5.0, 0.0))))
                dz -= 0.026 * max(0.0, 1.0 - crack / 0.07)
                if r_val < 1.9:
                    dz *= 0.5  # 池の縁は削れて丸い
            else:
                dz = 0.04 * fbm(p3 * 1.5, 2) + 0.01 * nz(pw * 5.0)
            row.append(bm.verts.new(Vector((x, y, z_base + dz))))
        ring_verts.append(row)
        ring_r.append(r_val)

    id_lime = MAT_ID["CenoteBase_Limestone"]
    id_wet = MAT_ID["CenoteBase_LimestoneWet"]
    id_moss = MAT_ID["CenoteBase_Moss"]

    v_center = bm.verts.new(Vector((pool_cx, pool_cy, 1.38)))
    for it in range(n_theta):
        it_next = (it + 1) % n_theta
        f = bm.faces.new((v_center, ring_verts[0][it], ring_verts[0][it_next]))
        f.material_index = id_wet

    for ir in range(len(radii_cfg) - 1):
        for it in range(n_theta):
            it_next = (it + 1) % n_theta
            f = bm.faces.new((ring_verts[ir][it], ring_verts[ir + 1][it], ring_verts[ir + 1][it_next], ring_verts[ir][it_next]))
            fc = f.calc_center_median()
            dist_pool = (Vector((fc.x - pool_cx, fc.y - pool_cy, 0))).length
            n_val = fbm(fc * 0.8, 2)
            r_in = ring_r[ir]
            if r_in < 1.66 or dist_pool < 2.05 + 0.45 * n_val:
                f.material_index = id_moss if (1.5 <= r_in <= 2.3 and fc.y > 0.6 and n_val > 0.08) else id_wet
            else:
                f.material_index = id_wet if (fc.y < -2.6 and fc.x < -0.5) else id_lime

    skirt = [bm.verts.new(Vector((math.cos(it / n_theta * math.tau) * 5.85 * ELLIPSE_X,
                                  math.sin(it / n_theta * math.tau) * 5.85 * ELLIPSE_Y, 1.2))) for it in range(n_theta)]
    for it in range(n_theta):
        it_next = (it + 1) % n_theta
        f = bm.faces.new((ring_verts[-1][it], skirt[it], skirt[it_next], ring_verts[-1][it_next]))
        f.material_index = id_wet

    # 池のふちを囲む自然の石灰岩の縁石・苔石 16個
    rng = random.Random(19)
    for i in range(16):
        ang = (i / 16) * math.tau + rng.uniform(-0.08, 0.08)
        if 1.1 < ang < 2.1:  # 北側は苗床の石縁があるので空ける
            continue
        rx = pool_cx + math.cos(ang) * rng.uniform(1.62, 1.78)
        ry = pool_cy + math.sin(ang) * rng.uniform(1.62, 1.78)
        m_stone = "CenoteBase_Moss" if i % 4 == 0 else "CenoteBase_LimestoneWet"
        add_box_bm(bm, (rng.uniform(0.32, 0.55), rng.uniform(0.22, 0.34), rng.uniform(0.07, 0.12)),
                   center=(rx, ry, 2.01), rot_z=ang + math.pi / 2, bevel_ratio=0.35, mat_name=m_stone, grain=0.015, cuts=1)

    # 7時の海側の口のトンネル床（濡れた岩畳）
    add_box_bm(bm, (2.8, 3.0, 0.24), center=(-2.5, -4.75, 1.88), bevel_ratio=0.12, mat_name="CenoteBase_LimestoneWet", grain=0.02, cuts=3)
    # 池の水面 (CenoteBase_Water、雨の波紋つきの円盤)
    add_cyl_bm(bm, 1.54, 1.56, 0.04, segs=40, center=(pool_cx, pool_cy, 1.88), mat_name="CenoteBase_Water", grain=0.004, h_cuts=0)

    return make_obj("CenoteBase_Floor", bm, default_mat="CenoteBase_Limestone")


# ---------------------------------------------------------------- 3. CenoteBase_Rug
def build_rug():
    bm = bmesh.new()
    add_box_bm(bm, (3.0, 2.8, 0.03), center=(-1.8, 0.5, 2.015), bevel_ratio=0.18, mat_name="CenoteBase_Rug", grain=0.005, cuts=6)
    return make_obj("CenoteBase_Rug", bm, default_mat="CenoteBase_Rug")


# ---------------------------------------------------------------- 4. CenoteBase_MapTable
def build_map_table():
    bm = bmesh.new()
    tx, ty = -1.8, 0.5
    add_cyl_bm(bm, 0.36, 0.28, 0.30, segs=16, center=(tx, ty, 2.15), mat_name="CenoteBase_DriftwoodDark", grain=0.012, h_cuts=2)
    add_cyl_bm(bm, 1.10, 1.10, 0.08, segs=36, center=(tx, ty, 2.34), mat_name="CenoteBase_Map", grain=0.003, h_cuts=0)
    n_seg = 24
    for i in range(n_seg):
        a = (i / n_seg) * math.tau
        px = tx + math.cos(a) * 1.05
        py = ty + math.sin(a) * 1.05
        add_box_bm(bm, (0.30, 0.08, 0.06), center=(px, py, 2.41), rot_z=a + math.pi / 2, bevel_ratio=0.25, mat_name="CenoteBase_Driftwood", grain=0.005, cuts=1)
    return make_obj("CenoteBase_MapTable", bm, default_mat="CenoteBase_Driftwood")


# ---------------------------------------------------------------- 5. CenoteBase_Ramp（流木の横板張り＋つる手すり＋つる縛り）
def build_ramp():
    bm = bmesh.new()
    n_steps = 30
    th_start = clock_to_rad(8.0)
    th_end = clock_to_rad(11.0)
    total_angle = th_end - th_start
    z_start = 2.0
    z_end = 4.6
    ramp_w = 1.0
    ramp_thick = 0.10

    id_wood = MAT_ID["CenoteBase_Driftwood"]
    id_dark = MAT_ID["CenoteBase_DriftwoodDark"]
    id_vine = MAT_ID["CenoteBase_Vine"]

    # 下を通る2本の縦梁（ストリンガー）と、その上に並ぶ流木の横板 30枚
    ring_loops = []
    for i in range(n_steps + 1):
        t = i / n_steps
        z = z_start + (z_end - z_start) * t
        th = th_start + total_angle * t
        r_wall = hall_radius_at(z)
        r_outer = r_wall - 0.04
        r_inner = r_outer - ramp_w

        c_out = ellipse_xy_from_rad(th, r_outer, z - 0.03)
        c_in = ellipse_xy_from_rad(th, r_inner, z - 0.03)

        v_top_in = bm.verts.new(c_in)
        v_top_out = bm.verts.new(c_out)
        v_bot_out = bm.verts.new(c_out - Vector((0, 0, ramp_thick)))
        v_bot_in = bm.verts.new(c_in - Vector((0, 0, ramp_thick)))
        ring_loops.append((v_top_in, v_top_out, v_bot_out, v_bot_in))

        # 流木の横板を1枚ずつ上に張る
        if i < n_steps:
            p_mid = (c_in + c_out) * 0.5 + Vector((0, 0, 0.02))
            rz_p = wall_outward_rot_z(th)
            p_mat = "CenoteBase_DriftwoodDark" if i % 5 == 0 else "CenoteBase_Driftwood"
            add_box_bm(bm, (0.22, ramp_w + 0.04, 0.045), center=p_mid, rot_z=rz_p, bevel_ratio=0.25, mat_name=p_mat, grain=0.006, cuts=0)

        # 手すり支柱と根元のつる巻き
        if i % 5 == 0 and 2 <= i <= n_steps - 2:
            post_p = c_in + Vector((0, 0, 0.22))
            add_cyl_bm(bm, 0.04, 0.032, 0.46, segs=8, center=post_p, mat_name="CenoteBase_DriftwoodDark", grain=0.005, h_cuts=1)
            add_cyl_bm(bm, 0.048, 0.048, 0.05, segs=8, center=c_in + Vector((0, 0, 0.04)), mat_name="CenoteBase_Vine", grain=0.002, h_cuts=0)
            add_cyl_bm(bm, 0.042, 0.042, 0.05, segs=8, center=c_in + Vector((0, 0, 0.39)), mat_name="CenoteBase_Vine", grain=0.002, h_cuts=0)

    for i in range(n_steps):
        a0, a1, a2, a3 = ring_loops[i]
        b0, b1, b2, b3 = ring_loops[i + 1]
        for q in ((a0, a1, b1, b0), (a1, a2, b2, b1), (a2, a3, b3, b2), (a3, a0, b0, b3)):
            bm.faces.new(q).material_index = id_dark

    s0, s1, s2, s3 = ring_loops[0]
    bm.faces.new((s0, s3, s2, s1)).material_index = id_dark
    e0, e1, e2, e3 = ring_loops[-1]
    bm.faces.new((e0, e1, e2, e3)).material_index = id_dark

    # つるの手すり（Oct 18 Claude：0.1 m ごとに点を打ち、ゆるい撚りと節をつけた）
    n_rail = n_steps * 3
    rail_pts = []
    for i in range(n_rail + 1):
        t = i / n_rail
        z = z_start + (z_end - z_start) * t + 0.38 + 0.012 * math.sin(t * n_steps * 1.2)
        th = th_start + total_angle * t
        r_in = hall_radius_at(z - 0.38) - 0.04 - ramp_w + 0.04
        rail_pts.append(ellipse_xy_from_rad(th, r_in, z))
    cb_rail = D.tube_cb(rail_pts, [0.032] * len(rail_pts), segs=8, rib=0.12, rib_freq=2, rib_twist=18.0, lumpy=0.10, seed=(1, 2, 3))
    merge_cb(bm, cb_rail, None, mat_name="CenoteBase_Vine")

    for clk in (8.6, 9.4, 10.3):
        th = clock_to_rad(clk)
        z_here = z_start + (z_end - z_start) * ((clk - 8.0) / 3.0)
        p_wall = ellipse_xy_from_rad(th, hall_radius_at(z_here - 0.85) - 0.05, z_here - 0.85)
        p_sup = ellipse_xy_from_rad(th, hall_radius_at(z_here) - 0.85, z_here - ramp_thick)
        add_strut_between(bm, p_wall, p_sup, thickness=0.13, mat_name="CenoteBase_Driftwood")

    ramp = make_obj("CenoteBase_Ramp", bm, default_mat="CenoteBase_Driftwood")
    fix_normals(ramp)
    return ramp


# ---------------------------------------------------------------- 6. CenoteBase_Gallery（馬てい形回廊：流木の放射板張り＋つる手すり＋方杖）
def build_gallery():
    """回廊。Oct 18 Claude：床板を1段2枚の細い板（幅 0.235 m）に割り、床（CenoteBase_Gallery）と
    手すり・支柱・支え木・飛び込み板（CenoteBase_GalleryRail）の2つに分けた（それぞれ 2 万三角形まで）。"""
    bm = bmesh.new()
    bm_r = bmesh.new()
    n_steps = 26
    z_gal = 4.6
    gal_thick = 0.12
    r_inner = 3.35
    r_outer = 5.15

    clk_start = 11.0
    clk_end = 17.85

    id_wood = MAT_ID["CenoteBase_Driftwood"]
    id_dark = MAT_ID["CenoteBase_DriftwoodDark"]
    id_vine = MAT_ID["CenoteBase_Vine"]

    ring_loops = []
    for i in range(n_steps + 1):
        t = i / n_steps
        clk = clk_start + (clk_end - clk_start) * t
        th = clock_to_rad(clk)
        c_in = ellipse_xy_from_rad(th, r_inner, z_gal - 0.025)
        c_out = ellipse_xy_from_rad(th, r_outer, z_gal - 0.025)

        v_top_in = bm.verts.new(c_in)
        v_top_out = bm.verts.new(c_out)
        v_bot_out = bm.verts.new(c_out - Vector((0, 0, gal_thick)))
        v_bot_in = bm.verts.new(c_in - Vector((0, 0, gal_thick)))
        ring_loops.append((v_top_in, v_top_out, v_bot_out, v_bot_in))

        # 回廊の上に放射状に並んだ流木の床板（1段に2枚、26段で52枚）
        if i < n_steps:
            th_mid = clock_to_rad(clk_start + (clk_end - clk_start) * (i + 0.5) / n_steps)
            p_mid = (ellipse_xy_from_rad(th_mid, r_inner, z_gal - 0.025) + ellipse_xy_from_rad(th_mid, r_outer, z_gal - 0.025)) * 0.5 + Vector((0, 0, 0.015))
            rz_p = wall_outward_rot_z(th_mid)
            tdir = Vector((math.cos(rz_p), math.sin(rz_p), 0.0))
            for k, off in enumerate((-0.125, 0.125)):
                p_mat = "CenoteBase_DriftwoodDark" if (2 * i + k) % 7 == 0 else "CenoteBase_Driftwood"
                L = 1.82 - 0.06 * ((2 * i + k) % 3)
                add_box_bm(bm, (0.235, L, 0.045), center=p_mid + tdir * off, rot_z=rz_p + 0.012 * math.sin(i * 2.3 + k),
                           bevel_ratio=0.22, mat_name=p_mat, grain=0.007, cuts=0)

        # 手すり支柱と上下のつる縛り
        if i % 3 == 0 and abs(clk - 15.5) > 0.35 and clk < 16.25:   # 4時15分から先は釣瓶の着き場で手すりなし（Oct 18）
            add_cyl_bm(bm_r, 0.04, 0.032, 0.44, segs=6, center=c_in + Vector((0, 0, 0.22)), mat_name="CenoteBase_DriftwoodDark", grain=0.005, h_cuts=1)
            add_cyl_bm(bm_r, 0.048, 0.048, 0.045, segs=6, center=c_in + Vector((0, 0, 0.04)), mat_name="CenoteBase_Vine", grain=0.002, h_cuts=0)
            add_cyl_bm(bm_r, 0.042, 0.042, 0.045, segs=6, center=c_in + Vector((0, 0, 0.39)), mat_name="CenoteBase_Vine", grain=0.002, h_cuts=0)

    for i in range(n_steps):
        a0, a1, a2, a3 = ring_loops[i]
        b0, b1, b2, b3 = ring_loops[i + 1]
        for q in ((a0, a1, b1, b0), (a1, a2, b2, b1), (a2, a3, b3, b2), (a3, a0, b0, b3)):
            bm.faces.new(q).material_index = id_dark

    s0, s1, s2, s3 = ring_loops[0]
    bm.faces.new((s0, s3, s2, s1)).material_index = id_dark
    e0, e1, e2, e3 = ring_loops[-1]
    bm.faces.new((e0, e1, e2, e3)).material_index = id_dark

    # 2段のつるの手すり綱（上段 Z+0.38、中段 Z+0.20）。Oct 18 Claude：0.1 m ごとの点、撚りと節
    for z_rail_off, r_rope in ((0.38, 0.032), (0.20, 0.022)):
        for clk_a, clk_b in ((11.0, 15.1), (15.9, 16.2)):
            seg_cnt = int((clk_b - clk_a) * 14)
            r_pts = []
            for j in range(seg_cnt + 1):
                clk = clk_a + (clk_b - clk_a) * (j / seg_cnt)
                th = clock_to_rad(clk)
                r_pts.append(ellipse_xy_from_rad(th, r_inner + 0.04, z_gal + z_rail_off + 0.01 * math.sin(j * 0.5)))
            cb_r = D.tube_cb(r_pts, [r_rope] * len(r_pts), segs=8 if r_rope > 0.025 else 6, rib=0.12, rib_freq=2,
                             rib_twist=18.0, lumpy=0.10, seed=(clk_a, z_rail_off, 1))
            merge_cb(bm_r, cb_r, None, mat_name="CenoteBase_Vine")

    # 壁への斜めの支え木（方杖）7本
    for clk in (11.3, 12.4, 13.5, 14.6, 15.8, 16.6, 17.4):
        th = clock_to_rad(clk)
        p_wall = ellipse_xy_from_rad(th, hall_radius_at(3.40) - 0.05, 3.40)
        p_inner = ellipse_xy_from_rad(th, r_inner + 0.18, z_gal - gal_thick)
        add_strut_between(bm_r, p_wall, p_inner, thickness=0.15, mat_name="CenoteBase_Driftwood")

    # 飛び込み板（3時半、滑り止めのつる巻きつき）
    th_dive = clock_to_rad(3.5)
    dive_c = ellipse_xy_from_rad(th_dive, 3.05, z_gal + 0.03)
    rz_dive = wall_outward_rot_z(th_dive)
    add_box_bm(bm_r, (0.60, 1.02, 0.07), center=dive_c, rot_z=rz_dive, bevel_ratio=0.18, mat_name="CenoteBase_Driftwood", grain=0.01, cuts=2)
    for d_off in (-0.32, -0.18, -0.04):
        p_strip = ellipse_xy_from_rad(th_dive, 2.75 - d_off, z_gal + 0.068)
        add_box_bm(bm_r, (0.62, 0.035, 0.02), center=p_strip, rot_z=rz_dive, bevel_ratio=0.2, mat_name="CenoteBase_Vine", grain=0.003, cuts=0)

    gal = make_obj("CenoteBase_Gallery", bm, default_mat="CenoteBase_Driftwood")
    fix_normals(gal)
    rail = make_obj("CenoteBase_GalleryRail", bm_r, default_mat="CenoteBase_Driftwood")
    return gal, rail


# ---------------------------------------------------------------- 7. 部屋と苗床（CenoteBase 単体用の基本パーツ）
def build_niche_room(name, clock_hr, z_floor, width, height, extra_fn=None):
    bm = bmesh.new()
    th = clock_to_rad(clock_hr)
    r_wall = hall_radius_at(z_floor)
    r_center = r_wall + 0.55
    pos = ellipse_xy_from_rad(th, r_center, z_floor - 0.06)
    rot_z = wall_outward_rot_z(th)
    add_box_bm(bm, (width, 1.8, 0.12), center=pos, rot_z=rot_z, bevel_ratio=0.10, mat_name="CenoteBase_Limestone", grain=0.018, cuts=2)

    if extra_fn is not None:
        extra_fn(bm, pos, rot_z, width, height)

    return make_obj(name, bm, default_mat="CenoteBase_Limestone")


def build_rooms_and_nursery():
    objs = []

    def extra_trade(bm, pos, rot_z, w, h):
        M = Euler((0, 0, rot_z), 'XYZ').to_matrix()
        add_box_bm(bm, (1.4, 0.45, 0.42), center=pos + M @ Vector((-0.4, -0.55, 0.27)), rot_z=rot_z + 0.3, mat_name="CenoteBase_Limestone")
        add_cyl_bm(bm, 0.04, 0.03, 0.50, segs=8, center=pos + M @ Vector((-0.1, -0.55, 0.68)), mat_name="CenoteBase_Driftwood")
        for i in range(6):
            bx = -1.0 + i * 0.40
            add_cyl_bm(bm, 0.22, 0.25, 0.35, segs=12, center=pos + M @ Vector((bx, 0.45, 0.23)), mat_name="CenoteBase_Vine")

    objs.append(build_niche_room("CenoteBase_Room_Trade", 5.4, 2.0, 2.6, 1.5, extra_trade))

    bm_nur = bmesh.new()
    for px, py, rz in [(1.0, 1.95, 0.15), (1.8, 2.10, 0.0), (2.6, 1.90, -0.18)]:
        add_box_bm(bm_nur, (0.80, 0.50, 0.26), center=(px, py, 2.13), rot_z=rz, bevel_ratio=0.1, mat_name="CenoteBase_Driftwood")
        add_box_bm(bm_nur, (0.70, 0.40, 0.22), center=(px, py, 2.16), rot_z=rz, bevel_ratio=0.1, mat_name="CenoteBase_Soil")
    objs.append(make_obj("CenoteBase_Nursery", bm_nur, default_mat="CenoteBase_Driftwood"))

    def extra_dye(bm, pos, rot_z, w, h):
        M = Euler((0, 0, rot_z), 'XYZ').to_matrix()
        for sx in (-0.58, 0.0, 0.58):
            add_cyl_bm(bm, 0.26, 0.28, 0.18, segs=14, center=pos + M @ Vector((sx, 0.15, 0.15)), mat_name="CenoteBase_ClamShell")
        add_box_bm(bm, (0.22, 1.2, 0.10), center=pos + M @ Vector((-0.2, -0.8, 0.09)), rot_z=rot_z, mat_name="CenoteBase_LimestoneWet")

    objs.append(build_niche_room("CenoteBase_Room_Dye", 4.0, 2.0, 2.0, 1.2, extra_dye))

    def extra_collection(bm, pos, rot_z, w, h):
        M = Euler((0, 0, rot_z), 'XYZ').to_matrix()
        add_box_bm(bm, (2.2, 0.38, 0.08), center=pos + M @ Vector((0, 0.55, 0.48)), rot_z=rot_z, mat_name="CenoteBase_Limestone")
        add_box_bm(bm, (2.2, 0.38, 0.08), center=pos + M @ Vector((0, 0.55, 0.88)), rot_z=rot_z, mat_name="CenoteBase_Limestone")
        for sx in (-0.7, 0.0, 0.7):
            add_box_bm(bm, (0.48, 0.08, 0.36), center=pos + M @ Vector((sx, 0.72, 1.15)), rot_z=rot_z, mat_name="CenoteBase_Driftwood")

    objs.append(build_niche_room("CenoteBase_Room_Collection", 12.0, 4.6, 2.6, 1.4, extra_collection))

    def extra_workshop(bm, pos, rot_z, w, h):
        M = Euler((0, 0, rot_z), 'XYZ').to_matrix()
        add_box_bm(bm, (1.3, 0.60, 0.40), center=pos + M @ Vector((0, 0.25, 0.26)), rot_z=rot_z, mat_name="CenoteBase_DriftwoodDark")
        add_box_bm(bm, (1.6, 0.10, 0.55), center=pos + M @ Vector((0, 0.70, 0.82)), rot_z=rot_z, mat_name="CenoteBase_Driftwood")

    objs.append(build_niche_room("CenoteBase_Room_Workshop", 1.5, 4.6, 2.0, 1.2, extra_workshop))

    def extra_drying(bm, pos, rot_z, w, h):
        M = Euler((0, 0, rot_z), 'XYZ').to_matrix()
        add_box_bm(bm, (w * 0.92, 0.10, 0.03), center=pos + M @ Vector((0, -0.78, 0.08)), rot_z=rot_z, mat_name="CenoteBase_Copper")
        for sx in (-0.52, 0.52):
            add_box_bm(bm, (0.65, 1.1, 0.75), center=pos + M @ Vector((sx, 0.15, 0.44)), rot_z=rot_z, mat_name="CenoteBase_Driftwood")

    objs.append(build_niche_room("CenoteBase_Room_Drying", 3.0, 4.6, 2.0, 1.2, extra_drying))

    def extra_refinery(bm, pos, rot_z, w, h):
        M = Euler((0, 0, rot_z), 'XYZ').to_matrix()
        add_box_bm(bm, (w * 0.92, 0.10, 0.03), center=pos + M @ Vector((0, -0.78, 0.08)), rot_z=rot_z, mat_name="CenoteBase_Copper")
        add_cyl_bm(bm, 0.38, 0.38, 0.28, segs=16, center=pos + M @ Vector((-0.50, 0.10, 0.20)), mat_name="CenoteBase_LavaStone")
        for i in range(3):
            add_box_bm(bm, (0.50, 0.40, 0.30), center=pos + M @ Vector((0.35, -0.35 + i * 0.48, 0.21)), rot_z=rot_z, mat_name="CenoteBase_Driftwood")

    objs.append(build_niche_room("CenoteBase_Room_Refinery", 4.3, 4.6, 2.2, 1.2, extra_refinery))

    return objs


# ---------------------------------------------------------------- 8. 釣瓶 (Lift)、桟橋 (Pier)、デッキ (Deck)、観測所 (Observatory)、鳥の巣 (Nest)
# 釣瓶（Oct 18 わくさん：上り専用、縁なしの板。低いほうの板に乗ると自動で上がる＝Unity の CenoteLift）
# 2枚の板は、回廊の南の端の内側に東西に並んで吊られる（4時45分と5時39分、半径 2.85）。上がった板はちょうど回廊の内ふちに着く。
# 綱は板 A → 滑車 A → 滑車 B → 板 B とつながり、片方が上がるともう片方が下がる。回廊の手すりはここだけ空ける（4時12分〜5時51分）
LIFT_CLOCKS = (4.75, 5.65)
LIFT_R = 2.85
LIFT_LOW_Z, LIFT_HIGH_Z = 2.0, 4.55     # 板の原点（板の下面）の高さ。上面は +0.05
LIFT_BEAM_Z = 6.9
LIFT_BOARD = 1.0


def lift_spot(i, z):
    return polar_xy(LIFT_CLOCKS[i], LIFT_R, z)


def build_lift():
    """釣瓶の動かない所（梁・滑車2つ・滑車の間の綱）。板と縦の綱は CenoteBase_LiftBoard / _LiftRope を Unity で動かす"""
    bm = bmesh.new()
    pa, pb = lift_spot(0, LIFT_BEAM_Z), lift_spot(1, LIFT_BEAM_Z)
    # 滑車の間を渡す梁（板の並びに沿って）と、壁から張り出す2本の腕
    mid = (pa + pb) * 0.5
    d = pb - pa
    add_box_bm(bm, (d.length + 0.9, 0.20, 0.20), center=mid + Vector((0, 0, 0.42)), rot_z=math.atan2(d.y, d.x), bevel_ratio=0.15,
               mat_name="CenoteBase_DriftwoodDark", grain=0.015, cuts=3)
    for i in range(2):
        c = LIFT_CLOCKS[i]
        p_in = lift_spot(i, LIFT_BEAM_Z + 0.42)
        p_wall = polar_xy(c, hall_radius_at(LIFT_BEAM_Z + 0.42) + 0.15, LIFT_BEAM_Z + 0.42)
        dd = p_wall - p_in
        add_box_bm(bm, (0.18, dd.length, 0.18), center=(p_in + p_wall) * 0.5, rot_z=math.atan2(dd.y, dd.x) - math.pi / 2, bevel_ratio=0.15,
                   mat_name="CenoteBase_Driftwood", grain=0.012, cuts=3)
        # 滑車（軸は回廊の半径の向き、輪は板の並びの向きに立つ）
        p_c = lift_spot(i, LIFT_BEAM_Z)
        rz = math.atan2(d.y, d.x)
        for off, rr, mat in ((-0.05, 0.36, "CenoteBase_DriftwoodDark"), (0.0, 0.30, "CenoteBase_Driftwood"), (0.05, 0.36, "CenoteBase_DriftwoodDark")):
            n_dir = Vector((-math.sin(rz), math.cos(rz), 0.0))
            add_cyl_bm(bm, rr, rr, 0.04 if off else 0.08, segs=20, center=p_c + n_dir * off, rot_euler=(math.pi / 2, 0, rz),
                       mat_name=mat, grain=0.006, h_cuts=0)
        add_cyl_bm(bm, 0.03, 0.03, 0.42, segs=8, center=p_c + Vector((0, 0, 0.21)), mat_name="CenoteBase_Vine", grain=0.002, h_cuts=0)
    # 滑車の上を渡る綱
    merge_cb(bm, D.tube_cb([pa + Vector((0, 0, 0.30)), pb + Vector((0, 0, 0.30))], [0.026, 0.026], segs=8, cap=True,
                           rib=0.15, rib_freq=3, rib_twist=60.0), None, mat_name="CenoteBase_Vine")
    return make_obj("CenoteBase_Lift", bm, default_mat="CenoteBase_Vine")


def build_lift_parts():
    """動く部品：板1枚（原点は板の下面の真ん中、四隅の縄が 0.75 m 上の輪に集まる）と、縦の綱（長さ 1 m、原点が上の端で下へ）"""
    bm = bmesh.new()
    for k in range(4):
        add_box_bm(bm, (LIFT_BOARD, 0.235, 0.05), center=(0, -0.375 + k * 0.25, 0.025), bevel_ratio=0.22,
                   mat_name="CenoteBase_DriftwoodDark" if k == 1 else "CenoteBase_Driftwood", grain=0.006, cuts=0)
    for sx in (-0.38, 0.38):
        add_box_bm(bm, (0.08, LIFT_BOARD, 0.05), center=(sx, 0, -0.025), bevel_ratio=0.22, mat_name="CenoteBase_DriftwoodDark", grain=0.006, cuts=0)
    top = Vector((0, 0, 0.75))
    for cx in (-0.45, 0.45):
        for cy in (-0.45, 0.45):
            merge_cb(bm, D.tube_cb([Vector((cx, cy, 0.05)), top], [0.014, 0.014], segs=5, cap=True), None, mat_name="CenoteBase_Vine")
    add_cyl_bm(bm, 0.06, 0.06, 0.04, segs=10, center=top, mat_name="CenoteBase_Vine", grain=0.002, h_cuts=0)
    board = make_obj("CenoteBase_LiftBoard", bm, default_mat="CenoteBase_Driftwood")
    bm_r = bmesh.new()
    pts = [Vector((0, 0, -i / 12.0)) for i in range(13)]
    merge_cb(bm_r, D.tube_cb(pts, [0.026] * 13, segs=8, cap=True, rib=0.15, rib_freq=3, rib_twist=60.0), None, mat_name="CenoteBase_Vine")
    rope = make_obj("CenoteBase_LiftRope", bm_r, default_mat="CenoteBase_Vine")
    return board, rope


def build_pier():
    bm = bmesh.new()
    px = -2.5
    py0, py1 = -6.15, -12.15
    # 縦の大引き（梁）2本と、その上に1枚ずつ並べた流木の横板 22枚
    for sx in (px - 1.15, px + 1.15):
        add_box_bm(bm, (0.22, 6.0, 0.14), center=(sx, (py0 + py1) * 0.5, 1.86), bevel_ratio=0.15, mat_name="CenoteBase_DriftwoodDark", grain=0.015, cuts=4)
    n_planks = 22
    for i in range(n_planks):
        py_p = py0 - 0.14 - i * (5.72 / (n_planks - 1))
        p_mat = "CenoteBase_DriftwoodDark" if i % 4 == 1 else "CenoteBase_Driftwood"
        w_var = 3.0 + 0.06 * math.sin(i * 1.7)
        add_box_bm(bm, (w_var, 0.23, 0.06), center=(px, py_p, 1.97), rot_z=0.008 * math.cos(i * 2.3), bevel_ratio=0.22, mat_name=p_mat, grain=0.008, cuts=2)
    # 杭 6本＋海面近くの濡れ色＋梁とのヤシ縄縛り
    for sx in (px - 1.32, px + 1.32):
        for sy in (-6.8, -9.2, -11.7):
            add_cyl_bm(bm, 0.11, 0.095, 1.35, segs=10, center=(sx, sy, 1.32), mat_name="CenoteBase_DriftwoodDark", grain=0.01, h_cuts=2)
            add_cyl_bm(bm, 0.125, 0.11, 0.70, segs=10, center=(sx, sy, 0.35), mat_name="CenoteBase_LimestoneWet", grain=0.015, h_cuts=1)
            add_cyl_bm(bm, 0.125, 0.125, 0.09, segs=10, center=(sx, sy, 1.86), mat_name="CenoteBase_Coir", grain=0.004, h_cuts=0)
    # 東側の低い手すり（支柱＋上下のヤシ縄巻き）
    rx = px + 1.42
    add_box_bm(bm, (0.09, 5.8, 0.08), center=(rx, (py0 + py1) * 0.5, 2.38), bevel_ratio=0.25, mat_name="CenoteBase_Driftwood", grain=0.01, cuts=4)
    for sy in (-6.6, -7.9, -9.2, -10.5, -11.8):
        add_cyl_bm(bm, 0.045, 0.038, 0.42, segs=8, center=(rx, sy, 2.18), mat_name="CenoteBase_DriftwoodDark", grain=0.005, h_cuts=1)
        add_cyl_bm(bm, 0.055, 0.055, 0.05, segs=8, center=(rx, sy, 2.36), mat_name="CenoteBase_Coir", grain=0.002, h_cuts=0)
    return make_obj("CenoteBase_Pier", bm, default_mat="CenoteBase_Driftwood")


# 行商人の舟（Oct 18 Claude。わくさん「桟橋に停留する船を適当に」）。桟橋の西に横づけ、舳先は南（海）。
# 丸木をくりぬいた舟（長さ 3.4 m、幅 0.9 m）に、西側へ張り出したアウトリガー（浮き丸太）、真ん中にヤシの葉の小屋根、
# 前寄りに短い帆柱と、二本の竿に張った蟹のはさみ形の帆（黄土色の布）。荷は素焼きの壺・編み籠・樹皮布の包み。
# 舟べりは海面から 0.45 m。桟橋（上面 Z 2.0）の杭へもやい綱。ナメクジ（体長 1.1 m）が2〜3匹乗れる大きさ。
BOAT_C = (-5.15, -9.6)      # 舟の真ん中（海面の上）
BOAT_L, BOAT_W, BOAT_H = 3.4, 0.9, 0.55


def build_boat():
    bm = bmesh.new()
    cx, cy = BOAT_C
    z0 = -0.12                   # 竜骨（少し水に沈む）
    n_s, n_u = 26, 12
    outer, inner = [], []
    for i in range(n_s + 1):
        t = i / n_s
        y = cy + (t - 0.5) * BOAT_L
        e = abs(2 * t - 1)
        w = BOAT_W * 0.5 * max(0.02, (1 - e ** 2.4)) ** 0.55
        sheer = z0 + BOAT_H + 0.16 * e ** 3          # 舳先と艫は少し反り上がる
        depth = BOAT_H * (1 - 0.35 * e ** 2)
        ro, ri = [], []
        for k in range(n_u + 1):
            a = math.pi * k / n_u                     # 0 = 東の舟べり、π = 西の舟べり
            x = math.cos(a) * w
            zz = sheer - math.sin(a) * depth
            jit = 0.006 * nz(Vector((x * 6, y * 3, zz * 6)))
            ro.append(bm.verts.new(Vector((cx + x * (1 + jit), y, zz))))
            wi = max(0.0, w - 0.06)
            ri.append(bm.verts.new(Vector((cx + math.cos(a) * wi, y, sheer - 0.03 - math.sin(a) * max(0.0, depth - 0.07)))))
        outer.append(ro); inner.append(ri)
    id_w, id_d = MAT_ID["CenoteBase_Driftwood"], MAT_ID["CenoteBase_DriftwoodDark"]
    for i in range(n_s):
        for k in range(n_u):
            bm.faces.new((outer[i][k], outer[i + 1][k], outer[i + 1][k + 1], outer[i][k + 1])).material_index = id_d
            bm.faces.new((inner[i][k], inner[i][k + 1], inner[i + 1][k + 1], inner[i + 1][k])).material_index = id_w
        for k in (0, n_u):          # 舟べりの上面（外と内をつなぐ）
            a, b = (outer, inner) if k == 0 else (inner, outer)
            bm.faces.new((a[i][k], b[i][k], b[i + 1][k], a[i + 1][k])).material_index = id_w
    for i in (0, n_s):              # 舳先と艫を閉じる
        ring = outer[i] + list(reversed(inner[i]))
        f = bm.faces.new(ring if i == n_s else list(reversed(ring)))
        f.material_index = id_d
    bmesh.ops.recalc_face_normals(bm, faces=list(bm.faces))
    # 舟の中の腰掛け板 2枚
    for dy in (-0.75, 0.55):
        add_box_bm(bm, (BOAT_W - 0.16, 0.2, 0.045), center=(cx, cy + dy, z0 + BOAT_H - 0.12), bevel_ratio=0.22, mat_name="CenoteBase_Driftwood", grain=0.006, cuts=0)
    # アウトリガー：西へ 1.35 m の浮き丸太と、2本の腕木（縛りつき）
    fx = cx - 1.35
    merge_cb(bm, D.log_cb(0.11, 0.09, 2.5, D.seed_vec((fx, cy, 0))), Matrix.Translation((fx, cy, 0.05)) @ Euler((math.pi / 2, 0, 0), 'XYZ').to_matrix().to_4x4(), mat_name="CenoteBase_DriftwoodDark")
    for dy in (-0.8, 0.8):
        a = Vector((cx + 0.35, cy + dy, z0 + BOAT_H + 0.04))
        b = Vector((fx, cy + dy, 0.16))
        add_strut_between(bm, a, b, thickness=0.09, mat_name="CenoteBase_Driftwood")
    # ヤシの葉の小屋根（真ん中より後ろ）：4本の柱と、ゆるく丸い屋根
    ry = cy + 0.45
    for sx in (-0.33, 0.33):
        for sy in (-0.35, 0.35):
            add_cyl_bm(bm, 0.03, 0.026, 0.85, segs=8, center=(cx + sx, ry + sy, z0 + BOAT_H + 0.35), mat_name="CenoteBase_Driftwood", grain=0.004, h_cuts=2)
    for k in range(7):
        a = (k - 3) / 3 * 0.9
        add_box_bm(bm, (0.24, 0.95, 0.035), center=(cx + math.sin(a) * 0.42, ry, z0 + BOAT_H + 0.78 + math.cos(a) * 0.1),
                   rot_y=a, bevel_ratio=0.3, mat_name="CenoteBase_Thatch", grain=0.008, cuts=1)
    # 帆柱と蟹のはさみ形の帆（前寄り）
    my = cy - 0.75
    mast_top = Vector((cx, my, z0 + BOAT_H + 1.75))
    add_cyl_bm(bm, 0.045, 0.032, 1.8, segs=10, center=(cx, my, z0 + BOAT_H + 0.88), mat_name="CenoteBase_DriftwoodDark", grain=0.005, h_cuts=4)
    foot = Vector((cx, my + 0.15, z0 + BOAT_H + 0.15))
    yard1 = foot + Vector((0.15, -0.5, 1.75))
    yard2 = foot + Vector((0.25, -1.25, 0.55))
    for tip in (yard1, yard2):
        merge_cb(bm, D.tube_cb([foot, tip], [0.025, 0.02], segs=6, cap=True, lumpy=0.05), None, mat_name="CenoteBase_Driftwood")
    # 帆の布：二本の竿の間を、外へふくらむ弧で張る（厚み 2 cm の閉じた形）
    sail_pts = []
    n_c = 14
    for k in range(n_c + 1):
        t = k / n_c
        p = yard1.lerp(yard2, t)
        mid = (yard1 + yard2) * 0.5
        bulge = (p - foot).normalized() * 0.0 + Vector((0.18, -0.25, 0.1)) * math.sin(math.pi * t)
        sail_pts.append(p + bulge)
    cbs = bmesh.new()
    fan_f, fan_b = [], []
    for side, lst in ((0.01, fan_f), (-0.01, fan_b)):
        lst.append(cbs.verts.new(foot + Vector((side, 0, 0))))
        for q in [yard1] + sail_pts + [yard2]:
            lst.append(cbs.verts.new(q + Vector((side, 0, 0))))
    for k in range(1, len(fan_f) - 1):
        cbs.faces.new((fan_f[0], fan_f[k], fan_f[k + 1]))
        cbs.faces.new((fan_b[0], fan_b[k + 1], fan_b[k]))
        cbs.faces.new((fan_f[k], fan_b[k], fan_b[k + 1], fan_f[k + 1]))
    bmesh.ops.recalc_face_normals(cbs, faces=list(cbs.faces))
    merge_cb(bm, cbs, None, mat_name="CenoteBase_ClothOchre" if "CenoteBase_ClothOchre" in MAT_ID else "CenoteBase_BarkCloth")
    # 荷：素焼きの壺 3つ、編み籠 2つ、樹皮布の包み
    floor_z = z0 + 0.12
    for k, (dx, dy, h) in enumerate(((-0.2, -0.15, 0.34), (0.18, -0.05, 0.28), (0.0, 0.25, 0.3))):
        add_cyl_bm(bm, 0.11, 0.08, h, segs=12, center=(cx + dx, cy + dy, floor_z + h * 0.5), mat_name="CenoteBase_Clay", grain=0.004, h_cuts=2)
        add_cyl_bm(bm, 0.07, 0.07, 0.04, segs=10, center=(cx + dx, cy + dy, floor_z + h + 0.02), mat_name="CenoteBase_Coir", grain=0.002, h_cuts=0)
    for dy in (0.95, 1.15):
        add_cyl_bm(bm, 0.17, 0.2, 0.22, segs=14, center=(cx + 0.05, cy + dy, floor_z + 0.11), mat_name="CenoteBase_Vine", grain=0.006, h_cuts=2)
    add_box_bm(bm, (0.42, 0.32, 0.22), center=(cx - 0.08, cy - 0.45, floor_z + 0.11), rot_z=0.3, bevel_ratio=0.4, mat_name="CenoteBase_BarkCloth", grain=0.01, cuts=2)
    # もやい綱：舳先の近くから桟橋の杭へ
    bow = Vector((cx + 0.2, cy - 1.45, z0 + BOAT_H + 0.1))
    post = Vector((-3.82, -9.2 - 1.6, 1.75))
    merge_cb(bm, D.tube_cb(D.sag_points(bow, post, sag=0.35, step=0.1), [0.018] * len(D.sag_points(bow, post, sag=0.35, step=0.1)), segs=6, cap=True,
                           rib=0.16, rib_freq=3, rib_twist=60.0), None, mat_name="CenoteBase_Coir")
    return make_obj("CenoteBase_Boat", bm, default_mat="CenoteBase_Driftwood")


def build_deck():
    """デッキ（2階、海側）。Oct 18 Claude：わくさんの決めで、桟橋と反対の東へ広げて鳥の籠の発着場にした（横 7.55 m、奥 4.6 m、
    東は崖まで北へのびる）。西に観測所の屋根と腰掛け・予報の板、東に直径 2.4 m の籠を下ろす丸（ヤシ縄の輪）。"""
    bm = bmesh.new()      # 床板と発着の輪（CenoteBase_Deck）
    bm_f = bmesh.new()    # 通路・梁・手すり・支え木（CenoteBase_DeckFrame）。2 万三角形に収めるため分けた
    x0, x1, y0, y1 = DECK_X0, DECK_X1, DECK_Y0, DECK_Y1
    # 回廊からデッキへの通路床（板張り 10枚）
    add_box_bm(bm_f, (1.50, 3.2, 0.12), center=(1.15, -4.75, 4.48), bevel_ratio=0.12, mat_name="CenoteBase_DriftwoodDark", grain=0.012, cuts=2)
    for i in range(10):
        py_t = -3.30 - i * 0.30
        add_box_bm(bm_f, (1.62, 0.26, 0.055), center=(1.15, py_t, 4.57), bevel_ratio=0.22, mat_name="CenoteBase_Driftwood", grain=0.007, cuts=1)
    # 下梁（南北に渡す）。東の梁は崖まで
    for sx in (-0.5, 0.9, 2.3, 3.7, 5.1, 6.35):
        yn = deck_north_y(sx)
        add_box_bm(bm_f, (0.20, yn - y1 + 0.05, 0.14), center=(sx, (yn + y1) * 0.5, 4.46), bevel_ratio=0.15, mat_name="CenoteBase_DriftwoodDark", grain=0.012, cuts=2)
    # 流木の床板（東西に渡す、0.235 m ごと）。長い列は継ぎ目で2枚に分ける（継ぎ目は列ごとにずらす）
    k = 0
    y = y1 + 0.12
    while True:
        if y <= y0 - 0.1:
            xa = x0
        else:
            xa = None   # 崖が北へ下がった東だけ：東の端から西へ、崖に当たるまで
            xx = x1
            while xx > x0:
                if deck_north_y(xx) < y + 0.12:
                    break
                xa = xx
                xx -= 0.1
            if xa is None or x1 - xa < 0.6:
                break
        p_mat = "CenoteBase_DriftwoodDark" if k % 4 == 2 else "CenoteBase_Driftwood"
        L = x1 - xa + 0.04
        if L > 4.2:
            xj = 2.6 + (0.45 if k % 2 else -0.45)
            for a, b in ((xa - 0.02, xj - 0.01), (xj + 0.01, x1 + 0.02)):
                add_box_bm(bm, (b - a, 0.22, 0.06), center=((a + b) * 0.5, y, 4.57), rot_z=0.006 * math.sin(k * 1.9 + a), bevel_ratio=0.22, mat_name=p_mat, grain=0.008, cuts=2)
        else:
            add_box_bm(bm, (L, 0.22, 0.06), center=((xa + x1) * 0.5, y, 4.57), rot_z=0.006 * math.sin(k * 1.9), bevel_ratio=0.22, mat_name=p_mat, grain=0.008, cuts=2)
        k += 1
        y += 0.235
    # 籠を下ろす丸の目印（ヤシ縄の輪）
    ring = [Vector((DECK_PAD[0] + math.cos(t / 56 * math.tau) * DECK_PAD_R, DECK_PAD[1] + math.sin(t / 56 * math.tau) * DECK_PAD_R, 4.615)) for t in range(57)]
    merge_cb(bm, D.tube_cb(ring, [0.024] * len(ring), segs=6, cap=False, rib=0.16, rib_freq=3, rib_twist=40.0, lumpy=0.06, seed=(3, 1, 4)), None, mat_name="CenoteBase_Coir")
    # 低い手すり：西・南・東（東は崖まで）。支柱とつる巻き
    rails = [((x0 + 0.03, y0), (x0 + 0.03, y1)), ((x0, y1 + 0.08), (x1, y1 + 0.08)), ((x1 - 0.03, y1), (x1 - 0.03, deck_north_y(x1) - 0.05))]
    for (ax, ay), (bx, by) in rails:
        a, b = Vector((ax, ay, 4.98)), Vector((bx, by, 4.98))
        L = (b - a).length
        rz = math.atan2(b.y - a.y, b.x - a.x)
        add_box_bm(bm_f, (L, 0.09, 0.075), center=(a + b) * 0.5, rot_z=rz, bevel_ratio=0.25, mat_name="CenoteBase_Driftwood", grain=0.008, cuts=3)
        n_p = max(2, int(L / 1.3) + 1)
        for j in range(n_p):
            pp = a.lerp(b, j / (n_p - 1))
            add_cyl_bm(bm_f, 0.045, 0.038, 0.42, segs=8, center=(pp.x, pp.y, 4.78), mat_name="CenoteBase_DriftwoodDark", grain=0.005, h_cuts=1)
            add_cyl_bm(bm_f, 0.055, 0.055, 0.05, segs=8, center=(pp.x, pp.y, 4.96), mat_name="CenoteBase_Vine", grain=0.002, h_cuts=0)
    # 崖からデッキ下面への斜めの支え木（崖の面まで）＋横つなぎ貫
    for sx in (0.0, 2.3, 4.4, 6.0):
        add_strut_between(bm_f, Vector((sx, cliff_y(sx, 2.75) + 0.1, 2.75)), Vector((sx, y1 + 0.9, 4.44)), thickness=0.16, mat_name="CenoteBase_Driftwood")
    add_box_bm(bm_f, (6.4, 0.12, 0.12), center=(2.95, -8.2, 3.7), bevel_ratio=0.2, mat_name="CenoteBase_DriftwoodDark", grain=0.01, cuts=2)
    deck = make_obj("CenoteBase_Deck", bm, default_mat="CenoteBase_Driftwood")
    frame = make_obj("CenoteBase_DeckFrame", bm_f, default_mat="CenoteBase_Driftwood")
    return deck, frame


def build_observatory():
    bm = bmesh.new()
    dx = 1.3
    dy0, dy1 = -6.20, -9.20
    add_box_bm(bm, (3.2, 1.5, 0.14), center=(dx, dy0 - 0.75, 6.20), rot_x=-0.12, bevel_ratio=0.15, mat_name="CenoteBase_Thatch")
    for sx in (dx - 1.38, dx + 1.38):
        add_cyl_bm(bm, 0.05, 0.045, 1.6, segs=8, center=(sx, dy0 - 1.35, 5.40), mat_name="CenoteBase_Driftwood")
    add_cyl_bm(bm, 0.04, 0.03, 1.45, segs=8, center=(dx + 1.1, dy1 + 0.45, 5.32), mat_name="CenoteBase_Driftwood")
    add_box_bm(bm, (0.52, 0.08, 0.12), center=(dx + 1.25, dy1 + 0.45, 6.02), mat_name="CenoteBase_DriftwoodDark")
    add_cyl_bm(bm, 0.11, 0.09, 0.30, segs=10, center=(dx - 1.1, dy1 + 0.40, 4.75), mat_name="CenoteBase_Clay")
    add_cyl_bm(bm, 0.08, 0.05, 0.82, segs=10, center=(dx, dy1 + 0.35, 5.05), rot_euler=(-1.2, 0, 0), mat_name="CenoteBase_Brass")
    return make_obj("CenoteBase_Observatory", bm, default_mat="CenoteBase_Driftwood")


def build_nest():
    bm = bmesh.new()
    nx, ny, nz_val = -2.5, -6.50, 7.5
    add_box_bm(bm, (1.85, 1.25, 0.24), center=(nx, ny + 0.15, nz_val - 0.14), bevel_ratio=0.25, mat_name="CenoteBase_Limestone", grain=0.025, cuts=2)
    n_seg = 18
    for i in range(n_seg):
        a = (i / n_seg) * math.tau
        px = nx + math.cos(a) * 0.56
        py = ny + math.sin(a) * 0.56
        add_box_bm(bm, (0.38, 0.22, 0.26), center=(px, py, nz_val + 0.12), rot_z=a + 0.25, bevel_ratio=0.25, mat_name="CenoteBase_Vine", grain=0.015, cuts=1)
    return make_obj("CenoteBase_Nest", bm, default_mat="CenoteBase_Vine")


# ---------------------------------------------------------------- 9. 小物
def build_basket():
    bm = bmesh.new()
    c = Vector((-2.5, -10.6, 2.0))
    n_seg = 18
    add_cyl_bm(bm, 0.66, 0.70, 0.06, segs=n_seg, center=c + Vector((0, 0, 0.03)), mat_name="CenoteBase_Rug")
    for z0, r0 in [(0.14, 0.72), (0.32, 0.76), (0.50, 0.80)]:
        for i in range(n_seg):
            a = (i / n_seg) * math.tau
            p = c + Vector((math.cos(a) * r0, math.sin(a) * r0, z0))
            add_box_bm(bm, (0.28, 0.08, 0.17), center=p, rot_z=a + math.pi / 2, bevel_ratio=0.18, mat_name="CenoteBase_Vine")
    return make_obj("CenoteBase_Basket", bm, default_mat="CenoteBase_Vine")


def build_lamps():
    bm = bmesh.new()
    lamp_pts = [
        polar_xy(8.6, 4.3, 2.9),
        polar_xy(10.2, 4.5, 3.9),
        polar_xy(12.0, 4.7, 5.5),
        polar_xy(2.0, 4.7, 5.5),
        polar_xy(4.6, 4.7, 5.5),
        polar_xy(6.2, 4.3, 2.9),
        polar_xy(3.6, 4.3, 2.9),
        polar_xy(1.0, 4.3, 2.9),
        Vector((-1.08, -8.1, 2.75)),
        Vector((2.7, -6.6, 5.2)),
    ]
    for p in lamp_pts:
        add_cyl_bm(bm, 0.08, 0.05, 0.12, segs=10, center=p, mat_name="CenoteBase_Brass")
        add_cyl_bm(bm, 0.045, 0.01, 0.12, segs=10, center=p + Vector((0, 0, 0.11)), mat_name="CenoteBase_LampFlame")
    return make_obj("CenoteBase_Lamp", bm, default_mat="CenoteBase_Brass")


def build_storage_basket(name, d, h, loc):
    bm = bmesh.new()
    r = d * 0.5
    for k in range(5):
        zk = (k + 0.5) * (h / 5)
        rk = r * (0.85 + 0.15 * (k / 4))
        add_cyl_bm(bm, rk, rk + 0.01, h / 4.6, segs=16, center=(0, 0, zk), mat_name="CenoteBase_Vine", grain=0.005, h_cuts=0)
    return make_obj(name, bm, location=loc, default_mat="CenoteBase_Vine")


def build_salt_bin(loc):
    bm = bmesh.new()
    for sx in (-0.21, 0.21):
        for sy in (-0.16, 0.16):
            add_cyl_bm(bm, 0.035, 0.03, 0.30, segs=8, center=(sx, sy, 0.15), mat_name="CenoteBase_DriftwoodDark", grain=0.005, h_cuts=1)
    add_box_bm(bm, (0.50, 0.40, 0.30), center=(0, 0, 0.45), bevel_ratio=0.12, mat_name="CenoteBase_Driftwood", grain=0.008, cuts=2)
    add_box_bm(bm, (0.52, 0.42, 0.04), center=(0, 0, 0.62), bevel_ratio=0.2, mat_name="CenoteBase_DriftwoodDark", grain=0.006, cuts=1)
    add_box_bm(bm, (0.14, 0.05, 0.04), center=(0, 0, 0.66), bevel_ratio=0.25, mat_name="CenoteBase_Driftwood", grain=0.004, cuts=0)
    return make_obj("CenoteBase_SaltBin", bm, location=loc, default_mat="CenoteBase_Driftwood")


def build_pile(loc):
    bm = bmesh.new()
    cb = bmesh.new()
    bmesh.ops.create_icosphere(cb, subdivisions=3, radius=0.5)
    for v in cb.verts:
        x, y, z = v.co.x, v.co.y, v.co.z
        if z < 0:
            z *= 0.15
        k = 1.0 + 0.10 * nz(v.co * 4.5)
        v.co = Vector((x * k, y * k, z * k + 0.04))
    merge_cb(bm, cb, mat_name="CenoteBase_Pile")
    return make_obj("CenoteBase_Pile", bm, location=loc, default_mat="CenoteBase_Pile")


def fix_normals(ob):
    """閉じた部品ごとに面の向きを外向きにそろえる"""
    bmx = bmesh.new()
    bmx.from_mesh(ob.data)
    bmesh.ops.recalc_face_normals(bmx, faces=list(bmx.faces))
    bmx.to_mesh(ob.data)
    bmx.free()


# ---------------------------------------------------------------- 10. 当たり判定用の単純メッシュ (*_COL)
def build_colliders():
    cols = []
    # 床（Oct 18 Claude）：広間は Z 2.0 の平ら。池は底 Z 1.45 の鉢で、ふちは 34° の坂（見た目の縁石より緩い。這い出せるように）
    bm_fc = bmesh.new()
    n_f = 48
    pool_cx = 1.8
    rings = [(0.85, 1.45), (1.25, 1.72), (1.70, 2.0), (2.6, 2.0), (4.0, 2.0), (5.9, 2.0)]
    rv = []
    for r_val, zr in rings:
        row = []
        for it in range(n_f):
            th = it / n_f * math.tau
            if r_val <= 1.70:
                p = Vector((pool_cx + math.cos(th) * r_val, math.sin(th) * r_val, zr))
            else:
                blend = (r_val - 1.70) / (5.9 - 1.70)
                p = Vector((pool_cx * (1.0 - blend) + math.cos(th) * r_val * (1.0 + (ELLIPSE_X - 1.0) * blend),
                            math.sin(th) * r_val * (1.0 + (ELLIPSE_Y - 1.0) * blend), zr))
            row.append(bm_fc.verts.new(p))
        rv.append(row)
    for ir in range(len(rings) - 1):
        for it in range(n_f):
            itn = (it + 1) % n_f
            bm_fc.faces.new((rv[ir][it], rv[ir + 1][it], rv[ir + 1][itn], rv[ir][itn]))
    vc = bm_fc.verts.new(Vector((pool_cx, 0.0, 1.45)))
    for it in range(n_f):
        bm_fc.faces.new((vc, rv[0][it], rv[0][(it + 1) % n_f]))
    add_box_bm(bm_fc, (2.8, 3.0, 0.20), center=(-2.5, -4.75, 1.90), bevel_ratio=0.0, grain=0.0, cuts=0)
    # 2階の横穴の床（目印の口から奥へ 1.9 m、幅は横穴と同じ）
    for clk_n, w_n in ((12.0, 2.6), (1.5, 2.0), (3.0, 2.0), (4.3, 2.2)):
        th_n = clock_to_rad(clk_n)
        c_n = ellipse_xy_from_rad(th_n, hall_radius_at(4.6) + 0.75, 4.5)
        add_box_bm(bm_fc, (w_n, 2.1, 0.20), center=c_n, rot_z=wall_outward_rot_z(th_n), bevel_ratio=0.0, grain=0.0, cuts=0)
    cols.append(make_obj("CenoteBase_Floor_COL", bm_fc, default_mat="CenoteBase_Limestone"))

    bm_rc = bmesh.new()
    n_steps = 24
    th_start = clock_to_rad(8.0)
    th_end = clock_to_rad(11.0)
    loops = []
    for i in range(n_steps + 1):
        t = i / n_steps
        z = 2.0 + 2.6 * t
        th = th_start + (th_end - th_start) * t
        r_out = hall_radius_at(z) - 0.04
        c_out = ellipse_xy_from_rad(th, r_out, z)
        c_in = ellipse_xy_from_rad(th, r_out - 1.0, z)
        # 坂の下の高さがナメクジ（0.6 m）より低い所は、床まで詰めた坂にする（下にもぐって挟まらないように。Oct 18 Claude）
        z_bot = z - 0.14 if z - 0.14 >= 2.6 else 1.9
        loops.append((
            bm_rc.verts.new(c_in),
            bm_rc.verts.new(c_out),
            bm_rc.verts.new(Vector((c_out.x, c_out.y, z_bot))),
            bm_rc.verts.new(Vector((c_in.x, c_in.y, z_bot))),
        ))
    for i in range(n_steps):
        a0, a1, a2, a3 = loops[i]
        b0, b1, b2, b3 = loops[i + 1]
        bm_rc.faces.new((a0, a1, b1, b0))
        bm_rc.faces.new((a1, a2, b2, b1))
        bm_rc.faces.new((a2, a3, b3, b2))
        bm_rc.faces.new((a3, a0, b0, b3))
    cols.append(make_obj("CenoteBase_Ramp_COL", bm_rc, default_mat="CenoteBase_Driftwood"))

    bm_gc = bmesh.new()
    n_steps = 18
    loops_g = []
    for i in range(n_steps + 1):
        clk = 11.0 + (17.85 - 11.0) * (i / n_steps)
        th = clock_to_rad(clk)
        c_in = ellipse_xy_from_rad(th, 3.35, 4.6)
        c_out = ellipse_xy_from_rad(th, 5.15, 4.6)
        loops_g.append((
            bm_gc.verts.new(c_in),
            bm_gc.verts.new(c_out),
            bm_gc.verts.new(c_out - Vector((0, 0, 0.16))),
            bm_gc.verts.new(c_in - Vector((0, 0, 0.16))),
        ))
    for i in range(n_steps):
        a0, a1, a2, a3 = loops_g[i]
        b0, b1, b2, b3 = loops_g[i + 1]
        bm_gc.faces.new((a0, a1, b1, b0))
        bm_gc.faces.new((a1, a2, b2, b1))
        bm_gc.faces.new((a2, a3, b3, b2))
        bm_gc.faces.new((a3, a0, b0, b3))
    # 飛び込み板（3時半）。乗って先まで這い、池へ落ちられるように（Oct 18 Claude）
    th_dive = clock_to_rad(3.5)
    add_box_bm(bm_gc, (0.60, 1.02, 0.12), center=ellipse_xy_from_rad(th_dive, 3.05, 4.6 - 0.03), rot_z=wall_outward_rot_z(th_dive),
               bevel_ratio=0.0, grain=0.0, cuts=0)
    cols.append(make_obj("CenoteBase_Gallery_COL", bm_gc, default_mat="CenoteBase_Driftwood"))

    bm_pc = bmesh.new()
    add_box_bm(bm_pc, (3.0, 6.0, 0.16), center=(-2.5, -9.15, 1.92), bevel_ratio=0.0, grain=0.0, cuts=0)
    cols.append(make_obj("CenoteBase_Pier_COL", bm_pc, default_mat="CenoteBase_Driftwood"))

    bm_dc = bmesh.new()
    add_box_bm(bm_dc, (1.6, 3.2, 0.16), center=(1.15, -4.75, 4.52), bevel_ratio=0.0, grain=0.0, cuts=0)
    # デッキの形（北の端は崖まで）を厚さ 0.16 m の板にする
    outline = [(DECK_X0, DECK_Y1), (DECK_X1, DECK_Y1)]
    xs = [DECK_X1 - i * 0.25 for i in range(int((DECK_X1 - DECK_X0) / 0.25) + 1)] + [DECK_X0]
    outline += [(x, deck_north_y(x)) for x in xs]
    top = [bm_dc.verts.new(Vector((x, y, 4.6))) for x, y in outline]
    bot = [bm_dc.verts.new(Vector((x, y, 4.44))) for x, y in outline]
    ft = bm_dc.faces.new(top)
    fb = bm_dc.faces.new(list(reversed(bot)))
    n_o = len(outline)
    for i in range(n_o):
        j = (i + 1) % n_o
        bm_dc.faces.new((bot[i], bot[j], top[j], top[i]))
    bmesh.ops.triangulate(bm_dc, faces=[ft, fb])
    cols.append(make_obj("CenoteBase_Deck_COL", bm_dc, default_mat="CenoteBase_Driftwood"))

    # 手すり（Oct 18 Claude）：高さ 0.7 m の薄い壁。よじ登れる高さ（0.45 m）より高く。回廊の飛び込み口（3時半）は空ける
    bm_rl = bmesh.new()

    def rail_band(pts, h=0.7, t=0.06):
        loops = []
        for i, p in enumerate(pts):
            tang = (pts[min(i + 1, len(pts) - 1)] - pts[max(i - 1, 0)])
            tang.z = 0.0
            side = Vector((-tang.y, tang.x, 0.0)).normalized() * (t * 0.5)
            loops.append((bm_rl.verts.new(p + side), bm_rl.verts.new(p - side),
                          bm_rl.verts.new(p - side + Vector((0, 0, h))), bm_rl.verts.new(p + side + Vector((0, 0, h)))))
        for i in range(len(loops) - 1):
            a0, a1, a2, a3 = loops[i]
            b0, b1, b2, b3 = loops[i + 1]
            for q in ((a0, b0, b1, a1), (a1, b1, b2, a2), (a2, b2, b3, a3), (a3, b3, b0, a0)):
                bm_rl.faces.new(q)
        bm_rl.faces.new((loops[0][0], loops[0][1], loops[0][2], loops[0][3]))
        bm_rl.faces.new((loops[-1][3], loops[-1][2], loops[-1][1], loops[-1][0]))

    for clk_a, clk_b in ((11.0, 15.15), (15.85, 16.2)):
        n_r = int((clk_b - clk_a) * 6)
        rail_band([ellipse_xy_from_rad(clock_to_rad(clk_a + (clk_b - clk_a) * j / n_r), 3.39, 4.6) for j in range(n_r + 1)])
    n_r = 22
    pts_r = []
    for j in range(n_r + 1):
        t = 0.12 + 0.88 * j / n_r   # 坂の足もと（下から 0.3 m まで）は手すりなし。横からも乗れる
        z = 2.0 + 2.6 * t
        th = clock_to_rad(8.0) + (clock_to_rad(11.0) - clock_to_rad(8.0)) * t
        pts_r.append(ellipse_xy_from_rad(th, hall_radius_at(z) - 0.04 - 1.0 + 0.04, z - 0.03))
    rail_band(pts_r)
    rail_band([Vector((DECK_X0 + 0.03, DECK_Y0, 4.6)), Vector((DECK_X0 + 0.03, DECK_Y1, 4.6))])
    rail_band([Vector((DECK_X0, DECK_Y1 + 0.08, 4.6)), Vector((DECK_X1, DECK_Y1 + 0.08, 4.6))])
    rail_band([Vector((DECK_X1 - 0.03, DECK_Y1, 4.6)), Vector((DECK_X1 - 0.03, deck_north_y(DECK_X1), 4.6))])
    rail_band([Vector((-2.5 + 1.42, -6.3, 2.0)), Vector((-2.5 + 1.42, -12.1, 2.0))])
    cols.append(make_obj("CenoteBase_Rail_COL", bm_rl, default_mat="CenoteBase_Driftwood"))

    # 閉じた形の当たり判定は面の向きを外向きにそろえる（坂の帯の面が内向きで、ナメクジが中にもぐり込んでいた。Oct 18 Claude）
    for c in cols:
        if c.name not in ("CenoteBase_Floor_COL", "CenoteBase_Shell_COL"):   # 開いた面は元の向き（広間側）のまま
            fix_normals(c)
        c.hide_render = True
    return cols


# ---------------------------------------------------------------- 11. 比べるナメクジ 5匹
def build_slug_proxy(name, pos, heading_deg):
    bm = bmesh.new()
    mat_slug = get_rough_material("CenoteBase_SlugProxy", color=(0.52, 0.65, 0.42))
    cb = bmesh.new()
    bmesh.ops.create_uvsphere(cb, u_segments=16, v_segments=10, radius=0.5)
    for v in cb.verts:
        v.co = Vector((v.co.x * 0.44, v.co.y * 1.10, v.co.z * 0.42 + 0.21))
    merge_cb(bm, cb)
    add_cyl_bm(bm, 0.025, 0.02, 0.18, segs=8, center=(-0.08, 0.42, 0.42), rot_euler=(0.3, 0, 0), grain=0.0, h_cuts=0)
    add_cyl_bm(bm, 0.025, 0.02, 0.18, segs=8, center=(0.08, 0.42, 0.42), rot_euler=(0.3, 0, 0), grain=0.0, h_cuts=0)

    rot = Euler((0, 0, math.radians(heading_deg)), 'XYZ').to_matrix().to_4x4()
    bmesh.ops.transform(bm, matrix=Matrix.Translation(pos) @ rot, verts=list(bm.verts))
    return make_obj(name, bm, mat=mat_slug)


# ---------------------------------------------------------------- ライティング
def setup_lighting():
    world = bpy.context.scene.world
    if world is None:
        world = bpy.data.worlds.new("World")
        bpy.context.scene.world = world
    world.use_nodes = True
    nt = world.node_tree
    nt.nodes.clear()
    bg = nt.nodes.new("ShaderNodeBackground")
    bg.inputs["Color"].default_value = (0.62, 0.68, 0.72, 1.0)
    bg.inputs["Strength"].default_value = 0.75
    out = nt.nodes.new("ShaderNodeOutputWorld")
    nt.links.new(bg.outputs["Background"], out.inputs["Surface"])

    sun_data = bpy.data.lights.new("Sun_Cenote", type='SUN')
    sun_data.energy = 3.2
    sun_ob = bpy.data.objects.new("Sun_Cenote", sun_data)
    sun_ob.location = (1.8, 0, 18)
    sun_ob.rotation_euler = Euler((math.radians(15), math.radians(-8), 0), 'XYZ')
    bpy.context.scene.collection.objects.link(sun_ob)

    for idx, (lx, ly, lz, e) in enumerate([
        (1.8, 0.0, 7.5, 300.0),
        (-1.2, 0.5, 4.5, 220.0),
        (0.0, -9.0, 4.5, 180.0),
    ]):
        pt_data = bpy.data.lights.new(f"FillLight_{idx}", type='POINT')
        pt_data.energy = e
        pt_data.shadow_soft_size = 1.5
        pt_ob = bpy.data.objects.new(f"FillLight_{idx}", pt_data)
        pt_ob.location = (lx, ly, lz)
        bpy.context.scene.collection.objects.link(pt_ob)


# ---------------------------------------------------------------- 12. 家具・器具用の目印（CenoteBase_Anchor_<Room>）と書き出し
def make_anchor(name, loc, rot_z=0.0):
    emp = bpy.data.objects.new(name, None)
    emp.empty_display_type = 'ARROWS'
    emp.empty_display_size = 0.6
    emp.location = loc
    emp.rotation_euler = Euler((0.0, 0.0, rot_z), 'XYZ')
    bpy.context.scene.collection.objects.link(emp)
    return emp


def build_room_anchors():
    anchors = []
    anchors.append(make_anchor("CenoteBase_Anchor_MapTable", Vector((-1.8, 0.5, 2.0)), 0.0))
    anchors.append(make_anchor("CenoteBase_Anchor_Nursery", Vector((1.8, 2.1, 2.0)), 0.0))
    anchors.append(make_anchor("CenoteBase_Anchor_Deck", Vector((1.3, -7.7, 4.6)), 0.0))
    anchors.append(make_anchor("CenoteBase_Anchor_Pier", Vector((-2.5, -6.15, 2.0)), 0.0))

    niche_specs = [
        ("Trade",      5.4,  2.0),
        ("Dye",        4.0,  2.0),
        ("Collection", 12.0, 4.6),
        ("Workshop",   1.5,  4.6),
        ("Drying",     3.0,  4.6),
        ("Refinery",   4.3,  4.6),
    ]
    for rname, clk, z_fl in niche_specs:
        th = clock_to_rad(clk)
        r_mouth = hall_radius_at(z_fl)
        loc = ellipse_xy_from_rad(th, r_mouth, z_fl)
        rz = wall_outward_rot_z(th)
        anchors.append(make_anchor(f"CenoteBase_Anchor_{rname}", loc, rz))
    return anchors


def export_fbx(ob, extra_empties=None):
    os.makedirs(EXPORT_DIR, exist_ok=True)
    mod = ob.modifiers.get("outline_preview")
    if mod:
        mod.show_viewport = mod.show_render = False
    sel_list = [ob]
    if extra_empties:
        sel_list.extend(extra_empties)
    sel_set = set(sel_list)
    for o in bpy.context.scene.objects:
        o.select_set(o in sel_set)
    bpy.context.view_layer.objects.active = ob
    loc = ob.location.copy()
    ob.location = (0, 0, 0)
    obj_types = {"MESH", "EMPTY"} if extra_empties else {"MESH"}
    with bpy.context.temp_override(selected_objects=sel_list, active_object=ob):
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
            object_types=obj_types,
            use_mesh_modifiers=True,
            mesh_smooth_type="FACE",
            add_leaf_bones=False,
            bake_anim=False,
        )
    ob.location = loc
    if mod:
        mod.show_viewport = mod.show_render = True


def main():
    clear_scene()
    shell, shell_outer, shell_col = build_shell()
    floor = build_floor()
    rug = build_rug()
    map_table = build_map_table()
    ramp = build_ramp()
    gallery, gallery_rail = build_gallery()
    rooms = build_rooms_and_nursery()
    lift = build_lift()
    lift_board, lift_rope = build_lift_parts()
    pier = build_pier()
    deck, deck_frame = build_deck()
    boat = build_boat()
    obs = build_observatory()
    nest = build_nest()
    basket = build_basket()
    lamps = build_lamps()

    sb_s = build_storage_basket("CenoteBase_StorageBasket_S", 0.30, 0.25, (-0.1, -3.5, 2.0))
    sb_m = build_storage_basket("CenoteBase_StorageBasket_M", 0.50, 0.35, (0.5, -3.5, 2.0))
    sbin = build_salt_bin((3.2, -2.2, 4.6))
    pile = build_pile((0.5, -3.5, 2.15))

    cols = [shell_col] + build_colliders()
    anchors = build_room_anchors()

    build_slug_proxy("SlugProxy_MapTable", Vector((-2.8, 0.2, 2.0)), -75.0)
    p_ramp = polar_xy(9.5, 4.2, 3.3)
    build_slug_proxy("SlugProxy_Ramp", p_ramp, -15.0)
    p_gal = polar_xy(11.3, 4.1, 4.6)
    build_slug_proxy("SlugProxy_Gallery", p_gal, -125.0)
    build_slug_proxy("SlugProxy_Pier", Vector((-1.8, -8.8, 2.0)), 0.0)
    build_slug_proxy("SlugProxy_Deck", Vector((1.3, -7.8, 4.6)), 110.0)

    setup_lighting()

    os.makedirs(os.path.dirname(BLEND_PATH), exist_ok=True)
    bpy.ops.wm.save_as_mainfile(filepath=BLEND_PATH)

    all_meshes = [
        shell, shell_outer, floor, rug, map_table, ramp, gallery, gallery_rail, *rooms,
        lift, lift_board, lift_rope, pier, boat, deck, deck_frame, obs, nest, basket, lamps, sb_s, sb_m, sbin, pile, *cols
    ]

    if EXPORT:
        for ob in all_meshes:
            if ob == shell:
                export_fbx(ob, extra_empties=anchors)
            else:
                export_fbx(ob)

    info = [(o.name, sum(len(p.vertices) - 2 for p in o.data.polygons)) for o in all_meshes]
    print("Built & Exported CenoteBase High-Granularity Step 3:", info)
    return all_meshes


if __name__ == "__main__" or True:
    main()
