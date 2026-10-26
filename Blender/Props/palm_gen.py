# 立っているヤシを作るスクリプト（Blender 5.2）
# 灰色ラフ（Oct 20）：3案を作って、わくさんに選んでもらう。
#   A 曲がり   高さ 8 m、幹が海側へ弓なりに傾く、葉 10 枚（浜のヤシ）
#   B まっすぐ 高さ 10 m、ほぼまっすぐで少し S 字、葉 9 枚は上向き寄り
#   C ずんぐり 高さ 5.5 m、太い幹、葉 8 枚は大きめ（ポップ寄り）
# ナメクジの目の高さ 0.47 m は人の約 1/3.6。高さ 8 m のヤシは人なら約 29 m（本物のココヤシと同じくらい）。
# Oct 20 わくさん：C ずんぐりに決定。実は本物の大きさで、房にして見えるように。
# 形：幹は 7 角の筒に 0.45 m ごとの節（ふくらみ）、根元は太く広がる。葉は弓なりの軸に、V 字に折れた細長い葉身、
#     ふちはぎざぎざ（小葉の切れ込み）。枯れて垂れた葉 2 枚、実 4 個。どれも厚みのある閉じた形。
# 原点は根元の真ん中、上は +Z。三角形の目安 3,000 以下。
# 使い方：Blender の MCP から実行、または  blender -b --python palm_gen.py
import bpy, bmesh, math, random, os
from mathutils import Vector

EXPORT_DIR = "C:/UnityProjects/ExtractionGame/Assets/Art/Models/Props/Palm/"
BLEND_PATH = "C:/UnityProjects/ExtractionGame/Blender/Props/Palm.blend"
EXPORT = True

# マテリアルの番号（Unity でマテリアルを当てる。色はプレビュー用）
TRUNK, LEAF, DEAD, NUT, RING = 0, 1, 2, 3, 4
# Unity の色（Oct 20 わくさんと決定）：幹 #B0A28A、節の溝 #7A6C5A、葉 #788A50、枯れた葉 #8A7A58、実 #6A5A3A（Materials/Palm_*.mat）
MATS = [("Palm_Trunk", (0.42, 0.36, 0.30)), ("Palm_Leaf", (0.45, 0.52, 0.30)),
        ("Palm_LeafDead", (0.48, 0.41, 0.30)), ("Palm_Nut", (0.36, 0.30, 0.20)),
        ("Palm_TrunkRing", (0.30, 0.26, 0.22))]

VARIANTS = {
    "Palm_A": dict(h=8.0, r0=0.22, r1=0.15, lean=0.22, s_curve=0.0, fronds=10, flen=3.8, fwid=0.62,
                   pitch=(22, 45), droop=0.55, seed=1),
    "Palm_B": dict(h=10.0, r0=0.21, r1=0.15, lean=0.05, s_curve=0.35, fronds=9, flen=3.6, fwid=0.58,
                   pitch=(35, 62), droop=0.45, seed=2),
    "Palm_C": dict(h=5.5, r0=0.34, r1=0.25, lean=0.10, s_curve=0.0, fronds=8, flen=3.4, fwid=0.75,
                   pitch=(20, 45), droop=0.6, seed=3),
}


def clear_scene():
    for o in list(bpy.data.objects):
        bpy.data.objects.remove(o, do_unlink=True)
    for m in list(bpy.data.meshes):
        bpy.data.meshes.remove(m)


def material(name, color):
    m = bpy.data.materials.get(name) or bpy.data.materials.new(name)
    m.use_nodes = True
    nt = m.node_tree
    bsdf = next((n for n in nt.nodes if n.type == "BSDF_PRINCIPLED"), None)
    if bsdf is None:
        nt.nodes.clear()
        bsdf = nt.nodes.new("ShaderNodeBsdfPrincipled")
        out = nt.nodes.new("ShaderNodeOutputMaterial")
        nt.links.new(bsdf.outputs[0], out.inputs[0])
    bsdf.inputs["Base Color"].default_value = (*color, 1.0)
    bsdf.inputs["Roughness"].default_value = 0.8
    return m


# ---------------------------------------------------------------- 部品
def ring(bm, center, side, up, r, sides, twist=0.0):
    vs = []
    for i in range(sides):
        a = twist + 2 * math.pi * i / sides
        vs.append(bm.verts.new(center + (side * math.cos(a) + up * math.sin(a)) * r))
    return vs


def bridge(bm, a, b, mat):
    n = len(a)
    for i in range(n):
        f = bm.faces.new((a[i], a[(i + 1) % n], b[(i + 1) % n], b[i]))
        f.material_index = mat


def cap(bm, vs, mat, flip=False):
    f = bm.faces.new(list(reversed(vs)) if flip else vs)
    f.material_index = mat


def trunk_point(p, t):
    """幹の中心線（t=0 根元、1 てっぺん）。lean で弓なり、s_curve で少し S 字。"""
    h = p["h"]
    x = p["lean"] * h * t ** 1.6 + p["s_curve"] * 0.6 * math.sin(t * math.pi * 2) * (1 - t * 0.5)
    return Vector((x, 0.0, h * t))


def build_trunk(bm, p, rnd):
    sides, step = 7, 0.45
    n = max(6, int(p["h"] / step))
    prev = None
    rings = []
    for k in range(n + 1):
        # 節（Oct 20 わくさん「幹がのっぺり」）：ふくらみ → 下がってくびれ → 暗い溝の帯。凹凸は半径の ±10% と強め
        # （Slug/Toon は小さい凹凸を消すので）。溝の帯だけ別の暗いマテリアル Palm_TrunkRing
        for frac, scale, mat in ((0.0, 1.10, TRUNK), (0.2, 0.90, TRUNK), (0.35, 0.90, RING)):
            t = min(1.0, (k + frac) / n)
            c = trunk_point(p, t)
            c2 = trunk_point(p, min(1.0, t + 0.01)) - trunk_point(p, max(0.0, t - 0.01))
            tan = c2.normalized()
            side = Vector((0, 1, 0))
            up = side.cross(tan).normalized()
            r = p["r0"] + (p["r1"] - p["r0"]) * t
            r *= 1.0 + 0.9 * math.exp(-t * p["h"] / 0.35)          # 根元の広がり
            r *= scale
            vs = ring(bm, c, side, up, r, sides, twist=k * 0.25)
            if prev:
                bridge(bm, prev, vs, mat)
            prev = vs
            rings.append(vs)
            if t >= 1.0:
                break
        if t >= 1.0:
            break
    cap(bm, rings[0], TRUNK, flip=True)
    # てっぺんの葉の付け根（ふくらんだ鞘）
    top = trunk_point(p, 1.0)
    sheath_r = p["r1"] * 1.9
    a = ring(bm, top + Vector((0, 0, 0.0)), Vector((0, 1, 0)), Vector((1, 0, 0)), p["r1"] * 1.05, sides)
    bridge(bm, rings[-1], a, TRUNK)
    b = ring(bm, top + Vector((0, 0, 0.35)), Vector((0, 1, 0)), Vector((1, 0, 0)), sheath_r, sides)
    bridge(bm, a, b, LEAF)
    c = ring(bm, top + Vector((0, 0, 0.75)), Vector((0, 1, 0)), Vector((1, 0, 0)), sheath_r * 0.5, sides)
    bridge(bm, b, c, LEAF)
    cap(bm, c, LEAF)
    return top + Vector((0, 0, 0.45))


def build_frond(bm, base, yaw, pitch_deg, length, width, droop, mat, segs=14, thick=0.03):
    """弓なりの軸に沿って、V 字に折れた葉身。ふちは交互に細くしてぎざぎざ。"""
    h_dir = Vector((math.cos(yaw), math.sin(yaw), 0))
    pitch = math.radians(pitch_deg)
    d0 = h_dir * math.cos(pitch) + Vector((0, 0, math.sin(pitch)))
    pts = []
    for i in range(segs + 1):
        s = i / segs
        pts.append(base + d0 * length * s + Vector((0, 0, -droop * length * s * s)))
    tops, bots = [], []
    for i, c in enumerate(pts):
        s = i / segs
        tan = (pts[min(segs, i + 1)] - pts[max(0, i - 1)]).normalized()
        side = tan.cross(Vector((0, 0, 1)))
        if side.length < 1e-4:
            side = h_dir.cross(Vector((0, 0, 1)))
        side.normalize()
        up = side.cross(tan).normalized()
        w = width * (math.sin(math.pi * min(1.0, s * 1.05)) ** 0.6) * (1.0 - 0.35 * s)
        if i % 2 == 1:
            w *= 0.62                                   # 小葉の切れ込み
        w = max(w, 0.02)
        fold = 0.45                                     # V 字（かもめの翼のように下へ）
        l = c + side * w - up * (w * fold)
        r = c - side * w - up * (w * fold)
        mid = c + up * 0.02
        row_t = [bm.verts.new(v) for v in (l, mid, r)]
        row_b = [bm.verts.new(v - up * thick) for v in (l, mid, r)]
        tops.append(row_t); bots.append(row_b)
    for i in range(segs):
        a, b = tops[i], tops[i + 1]
        bm.faces.new((a[0], b[0], b[1], a[1])).material_index = mat
        bm.faces.new((a[1], b[1], b[2], a[2])).material_index = mat
        a, b = bots[i], bots[i + 1]
        bm.faces.new((a[1], b[1], b[0], a[0])).material_index = mat
        bm.faces.new((a[2], b[2], b[1], a[1])).material_index = mat
        for j in (0, 2):                                # ふちの厚み
            ta, tb, ba, bb = tops[i][j], tops[i + 1][j], bots[i][j], bots[i + 1][j]
            f = bm.faces.new((ta, ba, bb, tb) if j == 0 else (ta, tb, bb, ba))
            f.material_index = mat
    for row_t, row_b in ((tops[0], bots[0]), (tops[-1], bots[-1])):
        for j in range(2):
            f = bm.faces.new((row_t[j], row_t[j + 1], row_b[j + 1], row_b[j]))
            f.material_index = mat


def build_nut(bm, center, r):
    tmp = bmesh.new()
    bmesh.ops.create_icosphere(tmp, subdivisions=1, radius=r)
    for v in tmp.verts:
        v.co = v.co * Vector((1, 1, 1.15)) + center
    me = bpy.data.meshes.new("_nut"); tmp.to_mesh(me); tmp.free()
    n0 = len(bm.faces)
    bm.from_mesh(me)
    bm.faces.ensure_lookup_table()
    for f in bm.faces[n0:]:
        f.material_index = NUT
    bpy.data.meshes.remove(me)


def build_palm(name, p):
    rnd = random.Random(p["seed"])
    bm = bmesh.new()
    crown = build_trunk(bm, p, rnd)
    n = p["fronds"]
    y0 = rnd.uniform(0, math.tau)
    for i in range(n):
        yaw = y0 + math.tau * i / n + rnd.uniform(-0.15, 0.15)
        pitch = rnd.uniform(*p["pitch"])
        build_frond(bm, crown, yaw, pitch, p["flen"] * rnd.uniform(0.88, 1.08), p["fwid"],
                    p["droop"] * rnd.uniform(0.85, 1.15), LEAF)
    for i in range(2):                                   # 枯れて垂れた葉
        yaw = y0 + math.tau * (i + 0.5) / 2 + 0.4
        build_frond(bm, crown - Vector((0, 0, 0.25)), yaw, -55, p["flen"] * 0.7, p["fwid"] * 0.55,
                    0.12, DEAD, segs=8)
    # 実：本物の大きさ（直径 約 28 cm。Oct 20 わくさん「本物の大きさで付ける」）。葉の付け根の下に房が2つ、4〜5個ずつ
    for b in range(2):
        a0 = y0 + math.pi * b + 0.6
        for k in range(5 if b == 0 else 4):
            a = a0 + (k - 2) * 0.42
            rr = p["r1"] * 1.35 + 0.16 + 0.06 * (k % 2)
            c = crown + Vector((math.cos(a) * rr, math.sin(a) * rr, -0.55 - 0.2 * (k % 2)))
            build_nut(bm, c, 0.14)
    bmesh.ops.remove_doubles(bm, verts=bm.verts, dist=1e-5)
    bm.normal_update()
    me = bpy.data.meshes.new(name)
    for mname, col in MATS:
        me.materials.append(material(mname, col))
    bm.to_mesh(me); bm.free()
    for poly in me.polygons:
        poly.use_smooth = True
    ob = bpy.data.objects.new(name, me)
    bpy.context.scene.collection.objects.link(ob)
    return ob


def export_fbx(ob):
    os.makedirs(EXPORT_DIR, exist_ok=True)
    for o in bpy.context.scene.objects:
        o.select_set(o == ob)
    bpy.context.view_layer.objects.active = ob
    loc = ob.location.copy(); ob.location = (0, 0, 0)
    bpy.ops.export_scene.fbx(filepath=EXPORT_DIR + ob.name + ".fbx", use_selection=True,
                             apply_unit_scale=True, apply_scale_options="FBX_SCALE_ALL",
                             bake_space_transform=True, object_types={"MESH"},
                             use_mesh_modifiers=True, mesh_smooth_type="FACE",
                             add_leaf_bones=False, bake_anim=False)
    ob.location = loc


def main():
    clear_scene()
    x = 0.0
    for name, p in VARIANTS.items():
        ob = build_palm(name, p)
        ob.location.x = x
        x += 9.0
        tris = sum(len(poly.vertices) - 2 for poly in ob.data.polygons)
        print(f"{name}: {tris} tris, height {ob.dimensions.z:.2f} m")
        if EXPORT:
            export_fbx(ob)
    bpy.ops.wm.save_as_mainfile(filepath=BLEND_PATH)


main()
