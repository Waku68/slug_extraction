# 籠を運ぶ鳥（CarrierBird）を作る。モデル票：docs/modeling/carrier_bird.md
# Blender 5.2 で exec(open(r"C:/UnityProjects/ExtractionGame/Blender/Creatures/carrier_bird_gen.py", encoding="utf-8").read())
#
# 決めた数字（Oct 18〜19 わくさん。灰色ラフ3回目＝首をちぢめたデフォルメのペリカン）：
#   立った高さ 約 2.1 m（頭のてっぺん、房を除く）。体はしずく形 幅 1.3 × 奥 1.7 × 高 1.2、中心 Z 0.9
#   頭 直径約 0.86、首は見せない（頭が胸に乗る）。頭の後ろに房3つ（後ろへなびく）
#   くちばし：顔から前へ約 1.0、付け根の幅 0.6 → 先 0.34（横に広く、Oct 19）、先だけ丸い（とがらせない）。横に細い線の盛り上がり
#   のど袋：くちばしの下に垂れる。いちばん深い所 約 0.45（頭の近く）、くちばしの8割で上へ合わさる
#   翼：たたむと体に貼りつく3枚重ね（クリーム・茶・炭色）。広げて約 7 m（片翼 3.0 m）、同じ3色の帯
#   尾：短くとがって上へ 25°。脚 直径 0.15、見えるのは約 0.3。足は水かき（長さ 0.4、幅 0.35）
#   原点は両足の間の地面。上 +Z、くちばしは +Y（Unity では +Z）
#   頭と体は1つのなめらかな形（首の折れ目なし）。首に骨を1本：CarrierBird_Rig の骨 Body と Neck
#   （Oct 19 わくさん「攻め」：なめらかなまま頭をかしげる・振り向く）。頭の部品（くちばし・のど袋・目・房）は Neck の骨に付ける
#   ほかの部品は別オブジェクト、CarrierBird_Root（空）の子。ピボットは回る所（あご・肩・腰・尾の付け根）
#   三角形 12,000 まで（広げた翼とたたんだ翼の両方を数える。羽の重なりで 9,000 から上げた、Oct 19）
#
# COLOR=False で灰色ラフ（Step 2）、True で色（Step 3）。EXPORT=True で FBX を書き出す（Step 4）
import bpy, bmesh, math
from mathutils import Vector, Matrix

COLOR = globals().get("COLOR", False)
EXPORT = globals().get("EXPORT", False)
PREVIEW = globals().get("PREVIEW", True)   # 比べるナメクジと、飛ぶ姿の見本を横に置く（書き出さない）
EXPORT_DIR = r"C:/UnityProjects/ExtractionGame/Assets/Art/Models/Creatures/CarrierBird/"
# 共通の形の道具（clear_scene・make_obj・ellipsoid・merge・loft・mesh_from_mods）
exec(open(r"C:/UnityProjects/ExtractionGame/Blender/Creatures/creature_mesh.py", encoding="utf-8").read())

# ---- 色（あとで染められる。はじめの色） ----
COLORS = {
    "CarrierBird_Feather": (0.96, 0.90, 0.82),   # クリーム
    "CarrierBird_Plumage": (0.96, 0.90, 0.82),   # 頭と体（羽の模様のテクスチャを掛ける。色はクリームと同じ）
    "CarrierBird_WingMid": (0.82, 0.64, 0.42),   # 茶の帯
    "CarrierBird_WingTip": (0.22, 0.21, 0.21),   # 炭色の羽先
    "CarrierBird_Beak":    (0.96, 0.58, 0.16),   # オレンジ
    "CarrierBird_Pouch":   (0.96, 0.82, 0.32),   # 黄色ののど袋
    "CarrierBird_Foot":    (0.95, 0.70, 0.15),   # 黄みのオレンジ
    "CarrierBird_Eye":     (0.05, 0.05, 0.06),
    "Ref_Slug":            (0.45, 0.62, 0.40),   # 比べるナメクジ（書き出さない）
}
GREY = (0.62, 0.62, 0.62)

# ---- 寸法 ----
BODY_C = Vector((0, -0.05, 0.9)); BODY_R = Vector((0.65, 0.85, 0.60))
HEAD_C = Vector((0, 0.42, 1.58)); HEAD_R = 0.46
BEAK_BASE = Vector((0, HEAD_C.y + 0.38, 1.52)); BEAK_LEN = 1.0
SHOULDER = Vector((0.50, 0.22, 1.15))     # 右肩（左は x を反転）
HIP = Vector((0.20, 0.0, 0.40))           # 右の腰
NECK = Vector((0, 0.30, 1.22))            # 首の骨の付け根（頭はここで回る）
BODY_HEAD_TRIS = 2600                     # 頭と体の形の三角形の目安


def material(name):
    mat = bpy.data.materials.get(name) or bpy.data.materials.new(name)
    mat.use_nodes = True
    nt = mat.node_tree
    bsdf = next((n for n in nt.nodes if n.type == "BSDF_PRINCIPLED"), None)
    if bsdf is None:
        bsdf = nt.nodes.new("ShaderNodeBsdfPrincipled")
    out = next((n for n in nt.nodes if n.type == "OUTPUT_MATERIAL"), None)
    if out is None:
        out = nt.nodes.new("ShaderNodeOutputMaterial")
    if not out.inputs[0].is_linked:
        nt.links.new(bsdf.outputs[0], out.inputs[0])
    col = COLORS[name] if (COLOR or name in ("CarrierBird_Eye", "Ref_Slug")) else GREY
    col = tuple(c ** 2.2 for c in col)        # COLORS は見た目の色（sRGB）。Blender の Base Color は linear
    bsdf.inputs["Base Color"].default_value = (*col, 1)
    bsdf.inputs["Roughness"].default_value = 0.8
    if name == "CarrierBird_Plumage" and COLOR:   # Blender の見本でも羽の模様を掛ける
        img = bpy.data.images.load(FEATHER_TEX, check_existing=True)
        tex = next((n for n in nt.nodes if n.type == "TEX_IMAGE"), None) or nt.nodes.new("ShaderNodeTexImage")
        tex.image = img
        mix = next((n for n in nt.nodes if n.type == "MIX"), None) or nt.nodes.new("ShaderNodeMix")
        mix.data_type = "RGBA"; mix.blend_type = "MULTIPLY"
        mix.inputs[0].default_value = 1.0
        mix.inputs[6].default_value = (*col, 1)
        nt.links.new(tex.outputs[0], mix.inputs[7])
        nt.links.new(mix.outputs[2], bsdf.inputs["Base Color"])
    mat.diffuse_color = (*col, 1)
    return mat


# ---- 体の形 ----
def body_shape(co):
    """楕円の点をしずく形へ：胸は丸く前へ、背中は細く尾の付け根へ上がる。下は少しつぶす。"""
    y = (co.y - BODY_C.y) / BODY_R.y
    dz = co.z - BODY_C.z
    if y < 0:
        t = (-y) ** 1.5
        k = 1.0 - 0.38 * t
        co = Vector((co.x * k, co.y, BODY_C.z + dz * k + 0.18 * t))
        dz = co.z - BODY_C.z
    if dz < 0:
        co.z = BODY_C.z + dz * 0.9
    return co


def on_body(p, lift):
    """点 p を体の表面へ（中心から放射状に）写し、外へ lift だけ浮かせる。"""
    q = Vector(((p.x - BODY_C.x) / BODY_R.x, (p.y - BODY_C.y) / BODY_R.y, (p.z - BODY_C.z) / BODY_R.z))
    q.normalize()
    b = BODY_C + Vector((q.x * BODY_R.x, q.y * BODY_R.y, q.z * BODY_R.z))
    n = Vector((q.x / BODY_R.x, q.y / BODY_R.y, q.z / BODY_R.z)).normalized()
    return body_shape(b) + n * lift


# ---- 部品 ----
FEATHER_TEX = r"C:/UnityProjects/ExtractionGame/Assets/Art/Models/Creatures/CarrierBird/Textures/CarrierBird_FeatherScales.png"
U_REP = 4               # 羽の模様を横に 4 回（1周で羽 16 枚、幅約 0.25 m）。縦はテクスチャ1枚（羽 17 段、高さ約 0.12 m）。Oct 19 わくさん「小さく」
TEX_BODY_TOP, TEX_HEAD_V = 0.80, 0.97   # テクスチャの縦：体の羽はここまで、頭は白い所（「頭はテクスチャはずして」）


def body_uvs(bm):
    """頭と体に羽の模様の UV：中心から見た向き（横＝ぐるり、縦＝上下の角度）。中心は首の骨の重みで
    体の中心から頭の中心へなめらかに動くので、首でつなぎ目が出ない。羽のふちは下を向く。つなぎ目は背中の真後ろ。"""
    uv = bm.loops.layers.uv.verify()
    for f in bm.faces:
        us = []
        for l in f.loops:
            co = l.vert.co
            c = BODY_C.lerp(HEAD_C, neck_weight(co))
            d = co - c
            az = math.atan2(d.x, d.y)                                  # 前が 0、真後ろが ±π
            el = math.atan2(d.z, math.hypot(d.x, d.y))
            w = neck_weight(co)                                         # 頭ほど上の白い所へ（首でなめらかに消える）
            us.append(((az / (2 * math.pi) + 0.5) * U_REP, (el / math.pi + 0.5) * TEX_BODY_TOP * (1 - w) + TEX_HEAD_V * w))
        if max(u for u, _ in us) - min(u for u, _ in us) > U_REP / 2:  # 真後ろのつなぎ目をまたぐ面
            us = [(u + U_REP if u < U_REP / 2 else u, v) for u, v in us]
        for l, (u, v) in zip(f.loops, us):
            l[uv].uv = (u, v)


def neck_weight(co):
    """首の骨の重み：頭は 1、体は 0。首のまわりでなめらかに変わる。"""
    axis = Vector((0, 0.25, 0.97)).normalized()
    sd = (co - NECK).dot(axis)
    t = min(max((sd + 0.05) / 0.30, 0), 1)
    w = t * t * (3 - 2 * t)
    if (co - HEAD_C).length > HEAD_R + 0.45:
        w = 0.0
    return w


def build_body_head(root):
    """頭と体を1つのなめらかな形に：2つを溶かしてくっつけ（ボクセル）、首のくぼみをならし、三角形を減らす。
    首に骨（Neck）を入れ、頭の側だけ骨で動くように重みを付ける。"""
    body = ellipsoid(BODY_C, BODY_R, 40, 20)
    for v in body.verts:
        v.co = body_shape(v.co)
    head = ellipsoid(HEAD_C, (HEAD_R, HEAD_R, HEAD_R * 0.95), 32, 16)
    bm = mesh_from_mods(merge([body, head]), [("REMESH", {"mode": "VOXEL", "voxel_size": 0.025})])
    # 首のくぼみをならす：頭の球と体の楕円の両方の表面に近い所だけ、まわりの平均へ寄せる
    wt = {}
    for v in bm.verts:
        q = v.co - BODY_C
        rb = Vector((q.x / BODY_R.x, q.y / BODY_R.y, q.z / BODY_R.z)).length
        rh = (v.co - HEAD_C).length / HEAD_R
        w = max(0.0, 1.0 - (abs(rb - 1) + abs(rh - 1)) / 0.55)
        if w > 0:
            wt[v] = w
    for _ in range(40):
        new = {}
        for v, w in wt.items():
            nb = [e.other_vert(v).co for e in v.link_edges]
            avg = sum(nb, Vector()) / len(nb)
            new[v] = v.co.lerp(avg, 0.6 * w)
        for v, co in new.items():
            v.co = co
    ratio = min(1.0, BODY_HEAD_TRIS / max(sum(len(f.verts) - 2 for f in bm.faces), 1))
    bm = mesh_from_mods(bm, [("DECIMATE", {"ratio": ratio})])
    body_uvs(bm)
    ob = make_obj("CarrierBird_Body", bm, "CarrierBird_Plumage", None, Vector((0, 0, 0)))
    # 骨
    arm = bpy.data.armatures.new("CarrierBird_Rig")
    rig = bpy.data.objects.new("CarrierBird_Rig", arm)
    bpy.context.scene.collection.objects.link(rig)
    rig.parent = root
    bpy.context.view_layer.objects.active = rig
    bpy.ops.object.mode_set(mode="EDIT")
    b = arm.edit_bones.new("Body"); b.head = (0, 0, 0.4); b.tail = (0, 0, 1.0)
    n = arm.edit_bones.new("Neck"); n.head = NECK; n.tail = NECK + Vector((0, 0.12, 0.5)); n.parent = b
    bpy.ops.object.mode_set(mode="OBJECT")
    gb, gn = ob.vertex_groups.new(name="Body"), ob.vertex_groups.new(name="Neck")
    for v in ob.data.vertices:
        w = neck_weight(ob.matrix_world @ v.co)
        if w < 1: gb.add([v.index], 1 - w, "REPLACE")
        if w > 0: gn.add([v.index], w, "REPLACE")
    ob.parent = rig
    ob.modifiers.new("Armature", "ARMATURE").object = rig
    return ob, rig


def bone_parent(ob, rig, bone="Neck"):
    """ob を骨に付ける（見た目の位置はそのまま）。"""
    bpy.context.view_layer.update()
    mw = ob.matrix_world.copy()
    ob.parent = rig; ob.parent_type = "BONE"; ob.parent_bone = bone
    bpy.context.view_layer.update()
    ob.matrix_world = mw


def build_crest():
    """頭の後ろの房3つ（後ろへなびく）。"""
    parts = []
    for x, ln, up in ((-0.11, 0.30, 0.15), (0.0, 0.38, 0.28), (0.11, 0.30, 0.15)):
        d = Vector((x * 0.8, -0.8, up)).normalized()
        base = HEAD_C + Vector((x, -0.30, 0.24))
        rot = d.to_track_quat("Y", "Z").to_matrix()
        bm = bmesh.new()
        bmesh.ops.create_uvsphere(bm, u_segments=8, v_segments=6, radius=1.0)
        for v in bm.verts:
            t = (v.co.y + 1) / 2                   # 0 = 付け根、1 = 先
            w = 1.0 - 0.7 * t
            co = Vector((v.co.x * 0.09 * w, t * ln, v.co.z * 0.07 * w + 0.06 * t * t))
            v.co = rot @ co + base
        parts.append(bm)
    return make_obj("CarrierBird_Crest", merge(parts), "CarrierBird_Feather", None, HEAD_C + Vector((0, -0.3, 0.24)))


def build_beak(head):
    """上のくちばし：長くほぼまっすぐ、先だけ丸い。横に細い盛り上がりの線。"""
    N, K = 16, 12
    rings = []
    for i in range(N + 1):
        t = i / N
        w = 0.30 - 0.13 * t                           # 横に広い（Oct 19 わくさん）
        h = 0.075 - 0.03 * t
        if t > 0.9:                                   # 先を丸く閉じる
            f = math.sqrt(max(1 - ((t - 0.9) / 0.1) ** 2, 0.0))
            w *= max(f, 0.08); h *= max(f, 0.08)
        y = BEAK_BASE.y + t * BEAK_LEN
        z = BEAK_BASE.z - 0.07 * t ** 3               # 先が少し下へ
        ring = []
        for k in range(K):
            a = 2 * math.pi * k / K
            sz = math.sin(a)
            hz = h * (1.25 if sz > 0 else 0.5)        # 上は丸く、下は平ら
            ring.append(Vector((math.cos(a) * w, y, z + sz * hz)))
        rings.append(ring)
    parts = [loft(rings)]
    for s in (1, -1):                                 # 横の線（盛り上がり）
        rot = Matrix.Rotation(math.radians(7 * s), 3, "Z") @ Matrix.Rotation(math.radians(-2), 3, "X")
        c = Vector((s * 0.21, BEAK_BASE.y + 0.42, BEAK_BASE.z + 0.005))
        parts.append(ellipsoid(c, (0.02, 0.40, 0.018), 8, 4, rot))
    return [make_obj("CarrierBird_BeakUpper", merge(parts), "CarrierBird_Beak", head, BEAK_BASE + Vector((0, -0.05, 0)))]


def build_pouch(head):
    """下あごとのど袋。くちばしの下に垂れ、頭の近くがいちばん深い。"""
    N, K = 14, 12
    y0, y1 = BEAK_BASE.y - 0.12, BEAK_BASE.y + 0.8 * BEAK_LEN
    top = BEAK_BASE.z - 0.045
    rings = []
    for i in range(N + 1):
        t = i / N
        d = 0.48 * max(1 - t ** 2.5, 0) ** 0.5 * (0.7 + 0.3 * min(t / 0.25, 1)) + 0.03   # 丸くふくらんだ袋
        w = 0.28 - 0.12 * t
        y = y0 + t * (y1 - y0)
        z = top - 0.07 * (t * 0.8) ** 3
        ring = []
        for k in range(K):
            a = 2 * math.pi * k / K
            sz = math.sin(a)
            dz = sz * 0.03 if sz > 0 else sz * d     # 上は平ら、下へ垂れる
            ww = w * (1.0 if sz > 0 else 1.0 + 0.6 * math.sin(-a if sz < 0 else 0) * min(d / 0.45, 1))  # 袋は下でふくらむ
            ring.append(Vector((math.cos(a) * ww, y, z + dz)))
        rings.append(ring)
    return make_obj("CarrierBird_Pouch", loft(rings), "CarrierBird_Pouch", head, Vector((0, y0 + 0.05, top)))


def build_eyes(head):
    eyes = []
    for side, s in (("R", 1), ("L", -1)):
        d = Vector((0.72 * s, 0.5, 0.45)).normalized()
        p = HEAD_C + d * HEAD_R * 0.985
        rot = d.to_track_quat("Y", "Z").to_matrix()
        bm = ellipsoid(Vector((0, 0, 0)), (0.036, 0.03, 0.10), 12, 8, rot)   # 細く縦長（Oct 19 わくさん）
        hl = ellipsoid(rot @ Vector((0.008 * s, 0.026, 0.04)), (0.01, 0.008, 0.014), 8, 4)
        bm = merge([bm, hl])
        for v in bm.verts:
            v.co += p
        eyes.append(make_obj(f"CarrierBird_Eye_{side}", bm, "CarrierBird_Eye", head, p))
    return eyes


WING_MATS = ["CarrierBird_WingTip", "CarrierBird_WingMid", "CarrierBird_Feather"]


def build_folded_wing(root, s, side):
    """体に貼りつく翼。3枚重ね：下が炭色の長い羽先、中が茶の帯、上がクリーム。先が段になる。"""
    sh = Vector((SHOULDER.x * s, SHOULDER.y, SHOULDER.z))
    tip = Vector((0.30 * s, -0.88, 0.80))
    parts = []
    for lf, wf, lift in ((1.0, 0.9, 0.02), (0.70, 1.0, 0.05), (0.44, 1.05, 0.08)):   # 炭色・茶・クリームの帯が見える長さ
        t_end = sh + (tip - sh) * lf
        axis = t_end - sh; L = axis.length; axis.normalize()
        mid = (sh + t_end) / 2
        out = Vector((mid.x, 0, 0.2 * (mid.z - BODY_C.z))).normalized()
        zl = -axis
        xl = (out - zl * out.dot(zl)).normalized()
        yl = zl.cross(xl)
        bm = bmesh.new()
        bmesh.ops.create_uvsphere(bm, u_segments=14, v_segments=8, radius=1.0)
        for v in bm.verts:
            t = (v.co.z + 1) / 2          # 0 = 先、1 = 肩
            w = 0.45 + 0.55 * math.sin(min(t * 1.5, 1) * math.pi / 2)
            base = mid + yl * (v.co.y * 0.30 * wf * w) + zl * (v.co.z * L / 2)
            v.co = on_body(base, lift + 0.03 + v.co.x * 0.045 * w)
        parts.append(bm)
    return make_obj(f"CarrierBird_WingFolded_{side}", merge(parts, [0, 1, 2]), WING_MATS, root, sh)


def scallop(u, n=3, depth=0.12):
    return depth * abs(math.sin(n * math.pi * u))


def wing_loft(sh, s, u_end, chord_k, th0, z_off, n_sc, depth):
    N, K, span = int(16 * u_end) + 4, 10, 3.0
    rings = []
    for i in range(N + 1):
        u = u_end * i / N
        chord = (1.0 - 0.5 * u) * chord_k
        if u > 0.82:
            chord *= max(math.sqrt(max(1 - ((u - 0.82) / 0.18) ** 2, 0)), 0.12)
        th = th0 * (1 - 0.6 * u)
        lead = 0.25 - 0.3 * u
        ring = []
        for k in range(K):
            a = 2 * math.pi * k / K
            y = lead - chord / 2 + math.cos(a) * chord / 2
            z = math.sin(a) * th / 2 + z_off + 0.25 * u * u
            if math.cos(a) < 0:   # 後ろ半分に波（羽のふち）
                y -= scallop(u / u_end, n_sc, depth) * (-math.cos(a)) * chord_k
            ring.append(sh + Vector((s * (0.05 + u * span), y, z)))
        rings.append(ring)
    return loft(rings)


def build_open_wing(root, s, side):
    """飛ぶ用の翼。肩から外へ 3.0、付け根の幅 1.0、先 0.5。下から炭色（全体）・茶・クリームを重ね、
    上から見ると前がクリーム、帯が茶、外の先と後ろのふちが炭色。"""
    sh = Vector((SHOULDER.x * s, SHOULDER.y, SHOULDER.z))
    parts = [wing_loft(sh, s, 1.0, 1.0, 0.20, 0.0, 3, 0.12),
             wing_loft(sh, s, 0.80, 0.72, 0.12, 0.07, 4, 0.08),
             wing_loft(sh, s, 0.50, 0.48, 0.10, 0.12, 3, 0.06)]
    return make_obj(f"CarrierBird_WingOpen_{side}", merge(parts, [0, 1, 2]), WING_MATS, root, sh)


def build_tail(root):
    """短くとがった尾。上へ 25°。ふちにやわらかい波。"""
    base = Vector((0, -0.78, 0.98))
    rot = Matrix.Rotation(math.radians(25), 3, "X")
    bm = bmesh.new()
    bmesh.ops.create_uvsphere(bm, u_segments=14, v_segments=8, radius=1.0)
    for v in bm.verts:
        t = (1 - v.co.y) / 2              # 0 = 付け根（前）、1 = 先（後ろ）
        w = 1.0 - 0.75 * t ** 1.3
        co = Vector((v.co.x * 0.28 * w, -t * 0.55, v.co.z * 0.07 * w))
        if t > 0.6:                       # 先のふちに波
            co.y += 0.04 * abs(math.sin(3 * math.pi * (v.co.x * 0.5 + 0.5)))
        v.co = rot @ co + base
    return make_obj("CarrierBird_Tail", bm, "CarrierBird_Feather", root, base)


def build_leg_foot(root, s, side):
    hip = Vector((HIP.x * s, HIP.y, HIP.z))
    bm = bmesh.new()
    bmesh.ops.create_cone(bm, cap_ends=True, segments=10, radius1=0.075, radius2=0.075, depth=hip.z)
    for v in bm.verts:
        v.co += Vector((hip.x, hip.y, hip.z / 2))
    leg = make_obj(f"CarrierBird_Leg_{side}", bm, "CarrierBird_Foot", root, hip)
    # 水かき：前に指3本（0°・±30°）を膜でつないだ平たいひれ
    ankle = Vector((hip.x, hip.y, 0.0))
    yaw = math.radians(8 * s)
    top, bot = [], []
    M = 28
    bm = bmesh.new()
    for i in range(M):
        th = -math.pi + 2 * math.pi * i / M
        if abs(th) <= math.radians(38):
            r = 0.4 * (0.84 + 0.16 * math.cos(12 * th))
        else:
            r = 0.09
        x, y = math.sin(th + yaw) * r, math.cos(th + yaw) * r
        top.append(bm.verts.new(ankle + Vector((x, y, 0.06))))
        bot.append(bm.verts.new(ankle + Vector((x, y, 0.0))))
    ct = bm.verts.new(ankle + Vector((0, 0.1, 0.075))); cb = bm.verts.new(ankle + Vector((0, 0.1, 0)))
    for i in range(M):
        j = (i + 1) % M
        bm.faces.new((top[i], top[j], ct)); bm.faces.new((bot[j], bot[i], cb))
        bm.faces.new((top[j], top[i], bot[i], bot[j]))
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces[:])
    foot = make_obj(f"CarrierBird_Foot_{side}", bm, "CarrierBird_Foot", leg, ankle + Vector((0, 0, 0.04)))
    return leg, foot


def build_bird(name="CarrierBird_Root", offset=Vector((0, 0, 0))):
    root = bpy.data.objects.new(name, None)
    bpy.context.scene.collection.objects.link(root)
    root.empty_display_size = 0.3
    body, rig = build_body_head(root)
    for ob in [build_crest(), *build_beak(None), build_pouch(None), *build_eyes(None)]:
        bone_parent(ob, rig)
    for s, side in ((1, "R"), (-1, "L")):
        build_folded_wing(root, s, side)
        ow = build_open_wing(root, s, side)
        ow.hide_set(True); ow.hide_render = True
        build_leg_foot(root, s, side)
    build_tail(root)
    root.location = offset
    return root


def set_pose(root, fly):
    for ob in root.children_recursive:
        if "WingOpen" in ob.name:
            ob.hide_set(not fly); ob.hide_render = not fly
        if "WingFolded" in ob.name:
            ob.hide_set(fly); ob.hide_render = fly


def ref_slug(pos):
    """比べるナメクジ：体長 1.1、高さ 0.5 のカプセル（書き出さない）。"""
    bm = ellipsoid(Vector((0, 0, 0.25)), (0.25, 0.55, 0.25), 20, 10)
    for v in bm.verts:
        if v.co.z < 0.08:
            v.co.z = 0.08 - (0.08 - v.co.z) * 0.3
    ob = make_obj("Ref_Slug", bm, "Ref_Slug", None, Vector((0, 0, 0)))
    ob.location = pos
    return ob


def tri_count(root):
    n = 0
    for ob in [root] + list(root.children_recursive):
        if ob.type == "MESH":
            n += sum(len(p.vertices) - 2 for p in ob.data.polygons)
    return n


def export_fbx(root):
    import os
    os.makedirs(EXPORT_DIR, exist_ok=True)
    for ob in root.children_recursive:   # 書き出しは隠した物も含める
        ob.hide_set(False)
    bpy.ops.object.select_all(action="DESELECT")
    root.select_set(True)
    for ob in root.children_recursive:
        ob.select_set(True)
    bpy.context.view_layer.objects.active = root
    loc = root.location.copy(); root.location = (0, 0, 0)
    bpy.ops.export_scene.fbx(filepath=EXPORT_DIR + "CarrierBird.fbx", use_selection=True,
                             apply_unit_scale=True, apply_scale_options="FBX_SCALE_ALL",
                             bake_space_transform=False, axis_forward="Y", axis_up="Z",
                             object_types={"EMPTY", "MESH", "ARMATURE"}, use_mesh_modifiers=True,
                             mesh_smooth_type="FACE", add_leaf_bones=False, bake_anim=False)
    root.location = loc
    set_pose(root, False)


def main():
    clear_scene()
    root = build_bird()
    print("CarrierBird_Root tris:", tri_count(root))
    if PREVIEW:
        ref_slug(Vector((1.3, 2.4, 0)))
        fly = build_bird("Preview_Fly", Vector((6.5, 0, 0.6)))
        set_pose(fly, True)
    if EXPORT:
        export_fbx(root)
        print("exported", EXPORT_DIR + "CarrierBird.fbx")


main()
