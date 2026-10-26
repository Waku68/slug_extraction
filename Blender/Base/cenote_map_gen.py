# 拠点の地図の台にのせる「古びた海賊の地図」の絵を作るスクリプト（Blender 5.2）
# Oct 18 Claude：わくさん「地図は平面でいい。海賊の地図みたいな、古びてるイメージ」。
# 拠点の物は「テクスチャを描かない」決まりだが、地図は絵そのものが中身なので、この1枚だけ絵（PNG）にする。
#
# 作り方：docs/island_map.svg（800 m の島、Oct 9）の形を読み、色をセピアのインクに置き換えて、羊皮紙の上に描く。
# - 羊皮紙：シミ（ノイズ）、四つ折りの折り目、ふちの焦げ
# - 海岸線：濃いインクの線＋外へ3重のさざ波の線
# - 地名：HG行書体（Windows の HGRGY.TTC）。ないときは游ゴシック
# - 方位の星（南西の海）と、丸いふちの二重線と目盛り
# 地図の台は丸いので、絵も丸（SVG の (350, 275) を中心に半径 330）。書き出しは 2048 px 四方の PNG。
# 1 SVG 単位 = 0.01 Blender 単位。北が上。

import bpy, bmesh, math, os, re, random
import xml.etree.ElementTree as ET
from mathutils import Vector

SVG_PATH = "C:/UnityProjects/ExtractionGame/docs/island_map.svg"
OUT_PNG = "C:/UnityProjects/ExtractionGame/Assets/Art/Models/Base/Textures/CenoteBase_MapChart.png"
BLEND_PATH = "C:/UnityProjects/ExtractionGame/Blender/Base/CenoteMap.blend"
RES = 2048
CX, CY, RADIUS = 350.0, 275.0, 330.0
S = 0.01
LEGEND_Y = 520.0      # これより下（凡例）は描かない
FONT_PATHS = ["C:/Windows/Fonts/HGRGY.TTC", "C:/Windows/Fonts/YuGothM.ttc"]

INK = "#3B2A1C"
PALETTE = {   # 元の色 -> 古びた色
    "#86D3CF": "#B8B593", "#D4E4A6": "#E3D3A6", "#F2E2BC": "#DCC294", "#5DAE6B": "#8E935C", "#9AD46F": "#A6A66C",
    "#FBFAF6": "#EFE4C8", "#D9A876": "#C08E5F", "#C98E5E": "#A9774C", "#9A5B32": "#6E4528", "#EADCC2": "#D9C69E",
    "#707070": "#6A5A48", "#5C5A57": "#5B4B3D", "#3E3C3A": "#43352A", "#4A4846": "#4E3F32", "#1E1E1E": "#2B1E14",
    "#E2804A": "#A55A33", "#2A2A2A": "#3B2A1C", "#BDBDB8": "#C6B48C", "#8F8F8A": "#8A7253", "#3F86C8": "#4D6874",
    "#E6F1FB": "#E7DCBF", "#FFFFFF": "#EADFC4", "#2F7A3B": "#5E6B3C", "#173404": "#33301A", "#712B13": "#5E2414",
    "#2C2C2A": "#3B2A1C", "#2F6B2A": "#4C5530", "#CFC4A8": "#B3A07A", "#B9824F": "#8E603A", "#E9DDB6": INK,
    "#F1EFE8": "#F3E9CF", "#633806": "#4A2C10", "#27500A": "#36401C", "#444441": "#3B2A1C", "#EAF3DE": "#E9DEC0",
}


def hex_lin(h):
    h = PALETTE.get(h.upper(), h) if h.startswith("#") else h
    h = h.lstrip("#")
    c = [int(h[i:i + 2], 16) / 255.0 for i in (0, 2, 4)]
    return tuple((v / 12.92 if v <= 0.04045 else ((v + 0.055) / 1.055) ** 2.4) for v in c)


def P(x, y):
    return Vector(((x - CX) * S, -(y - CY) * S, 0.0))


# ---------------------------------------------------------------- SVG のパス
def parse_path(d):
    toks = re.findall(r"[MLCQAZmlcqaz]|-?\d*\.?\d+(?:e-?\d+)?", d)
    subs, cur, pos, start, cmd, i = [], [], (0.0, 0.0), (0.0, 0.0), None, 0

    def num():
        nonlocal i
        v = float(toks[i]); i += 1
        return v
    while i < len(toks):
        t = toks[i]
        if re.match(r"[A-Za-z]", t):
            cmd = t; i += 1
            if cmd in "Zz":
                cur.append(start); subs.append((cur, True)); cur = []; pos = start
                continue
        if cmd == "M":
            if cur:
                subs.append((cur, False))
            pos = (num(), num()); start = pos; cur = [pos]; cmd = "L"
        elif cmd == "L":
            pos = (num(), num()); cur.append(pos)
        elif cmd == "C":
            p1, p2, p3 = (num(), num()), (num(), num()), (num(), num())
            p0 = pos
            for k in range(1, 13):
                t_ = k / 12; u = 1 - t_
                cur.append((u ** 3 * p0[0] + 3 * u * u * t_ * p1[0] + 3 * u * t_ * t_ * p2[0] + t_ ** 3 * p3[0],
                            u ** 3 * p0[1] + 3 * u * u * t_ * p1[1] + 3 * u * t_ * t_ * p2[1] + t_ ** 3 * p3[1]))
            pos = p3
        elif cmd == "Q":
            p1, p2 = (num(), num()), (num(), num())
            p0 = pos
            for k in range(1, 11):
                t_ = k / 10; u = 1 - t_
                cur.append((u * u * p0[0] + 2 * u * t_ * p1[0] + t_ * t_ * p2[0], u * u * p0[1] + 2 * u * t_ * p1[1] + t_ * t_ * p2[1]))
            pos = p2
        elif cmd == "A":
            rx, ry, rot, large, sweep, x2, y2 = num(), num(), num(), num(), num(), num(), num()
            cur.extend(arc_points(pos, (x2, y2), rx, ry, rot, large, sweep))
            pos = (x2, y2)
        else:
            i += 1
    if cur:
        subs.append((cur, False))
    return subs


def arc_points(p1, p2, rx, ry, rot, large, sweep, n=16):
    phi = math.radians(rot)
    cp, sp = math.cos(phi), math.sin(phi)
    dx, dy = (p1[0] - p2[0]) / 2, (p1[1] - p2[1]) / 2
    x1p, y1p = cp * dx + sp * dy, -sp * dx + cp * dy
    lam = (x1p / rx) ** 2 + (y1p / ry) ** 2
    if lam > 1:
        rx *= math.sqrt(lam); ry *= math.sqrt(lam)
    num_ = rx * rx * ry * ry - rx * rx * y1p * y1p - ry * ry * x1p * x1p
    den = rx * rx * y1p * y1p + ry * ry * x1p * x1p
    co = math.sqrt(max(0.0, num_ / den)) * (-1 if large == sweep else 1)
    cxp, cyp = co * rx * y1p / ry, -co * ry * x1p / rx
    cx = cp * cxp - sp * cyp + (p1[0] + p2[0]) / 2
    cy = sp * cxp + cp * cyp + (p1[1] + p2[1]) / 2

    def ang(u, v):
        a = math.atan2(u[0] * v[1] - u[1] * v[0], u[0] * v[0] + u[1] * v[1])
        return a
    t1 = ang((1, 0), ((x1p - cxp) / rx, (y1p - cyp) / ry))
    dt = ang(((x1p - cxp) / rx, (y1p - cyp) / ry), ((-x1p - cxp) / rx, (-y1p - cyp) / ry))
    if not sweep and dt > 0:
        dt -= 2 * math.pi
    elif sweep and dt < 0:
        dt += 2 * math.pi
    pts = []
    for k in range(1, n + 1):
        t = t1 + dt * k / n
        x, y = rx * math.cos(t), ry * math.sin(t)
        pts.append((cp * x - sp * y + cx, sp * x + cp * y + cy))
    return pts


def chaikin(pts, closed=True, rounds=3):
    """折れ線の角を丸める（手描きの海岸線のように）"""
    for _ in range(rounds):
        out = []
        n = len(pts)
        rng = range(n) if closed else range(n - 1)
        for i in rng:
            a, b = pts[i], pts[(i + 1) % n]
            out.append((a[0] * 0.75 + b[0] * 0.25, a[1] * 0.75 + b[1] * 0.25))
            out.append((a[0] * 0.25 + b[0] * 0.75, a[1] * 0.25 + b[1] * 0.75))
        if not closed:
            out = [pts[0]] + out + [pts[-1]]
        pts = out
    return pts


def ellipse_points(cx, cy, rx, ry, rot=0.0, n=48):
    phi = math.radians(rot)
    out = []
    for k in range(n):
        a = k / n * math.tau
        x, y = math.cos(a) * rx, math.sin(a) * ry
        out.append((cx + x * math.cos(phi) - y * math.sin(phi), cy + x * math.sin(phi) + y * math.cos(phi)))
    return out


# ---------------------------------------------------------------- Blender の部品
_mats = {}


def ink_mat(hexcol, strength=1.0):
    key = (hexcol, strength)
    if key in _mats:
        return _mats[key]
    m = bpy.data.materials.new("Map_" + hexcol.strip("#"))
    m.use_nodes = True
    nt = m.node_tree
    nt.nodes.clear()
    out = nt.nodes.new("ShaderNodeOutputMaterial")
    em = nt.nodes.new("ShaderNodeEmission")
    em.inputs["Color"].default_value = (*hex_lin(hexcol), 1.0)
    em.inputs["Strength"].default_value = strength
    nt.links.new(em.outputs[0], out.inputs["Surface"])
    _mats[key] = m
    return m


_z = [0.0]


def next_z():
    _z[0] += 0.0005
    return _z[0]


def fill_poly(pts, hexcol, name="fill"):
    if len(pts) < 3:
        return
    if pts[0] == pts[-1]:
        pts = pts[:-1]
    bm = bmesh.new()
    z = next_z()
    vs = [bm.verts.new(P(*p) + Vector((0, 0, z))) for p in pts]
    try:
        f = bm.faces.new(vs)
    except ValueError:
        bm.free()
        return
    bmesh.ops.triangulate(bm, faces=[f])
    me = bpy.data.meshes.new(name)
    bm.to_mesh(me); bm.free()
    ob = bpy.data.objects.new(name, me)
    me.materials.append(ink_mat(hexcol))
    bpy.context.scene.collection.objects.link(ob)


def stroke(pts, width, hexcol, name="stroke", wobble=0.0, seed=0, dash=None):
    """太さ width（SVG 単位）の線。継ぎ目は丸く。wobble で手描きのゆれ、dash=(線, すき間) で点線"""
    if len(pts) < 2:
        return
    rnd = random.Random(seed)
    w = width * S * 0.5
    bm = bmesh.new()
    z = next_z()
    P2 = []
    for (x, y) in pts:
        v = P(x, y)
        if wobble:
            v += Vector((rnd.uniform(-1, 1), rnd.uniform(-1, 1), 0)) * wobble * S
        P2.append(v + Vector((0, 0, z)))
    run = 0.0
    for a, b in zip(P2[:-1], P2[1:]):
        d = b - a
        L = d.length
        if L < 1e-6:
            continue
        segs = [(0.0, 1.0)]
        if dash:
            segs = []
            t = 0.0
            while t < L:
                phase = (run + t) % (dash[0] + dash[1])
                if phase < dash[0]:
                    e = min(L, t + dash[0] - phase)
                    segs.append((t / L, e / L)); t = e
                else:
                    t += dash[0] + dash[1] - phase
        n = Vector((-d.y, d.x, 0)).normalized() * w
        for s0, s1 in segs:
            p0, p1 = a + d * s0, a + d * s1
            bm.faces.new([bm.verts.new(p0 - n), bm.verts.new(p1 - n), bm.verts.new(p1 + n), bm.verts.new(p0 + n)])
        run += L
    if not dash:
        for v in P2:
            ring = [bm.verts.new(v + Vector((math.cos(k / 10 * math.tau), math.sin(k / 10 * math.tau), 0)) * w) for k in range(10)]
            bm.faces.new(ring)
    me = bpy.data.meshes.new(name)
    bm.to_mesh(me); bm.free()
    ob = bpy.data.objects.new(name, me)
    me.materials.append(ink_mat(hexcol))
    bpy.context.scene.collection.objects.link(ob)


_font = [None]


def font():
    if _font[0] is None:
        for fp in FONT_PATHS:
            if os.path.exists(fp):
                try:
                    _font[0] = bpy.data.fonts.load(fp)
                    break
                except Exception:
                    pass
    return _font[0]


def text(body, x, y, size, hexcol, anchor="middle"):
    cu = bpy.data.curves.new("label", type="FONT")
    cu.body = body
    f = font()
    if f:
        cu.font = f
    cu.size = size * S * 1.15
    cu.align_x = {"middle": "CENTER", "start": "LEFT", "end": "RIGHT"}.get(anchor, "CENTER")
    ob = bpy.data.objects.new("label", cu)
    ob.location = P(x, y) + Vector((0, 0, next_z() + 0.01))
    cu.materials.append(ink_mat(hexcol))
    bpy.context.scene.collection.objects.link(ob)


# ---------------------------------------------------------------- 羊皮紙（シミ・折り目・焦げたふち）
def parchment():
    bm = bmesh.new()
    r = RADIUS * S * 1.02
    bmesh.ops.create_circle(bm, cap_ends=True, radius=r, segments=128)
    me = bpy.data.meshes.new("parchment")
    bm.to_mesh(me); bm.free()
    ob = bpy.data.objects.new("parchment", me)
    bpy.context.scene.collection.objects.link(ob)
    m = bpy.data.materials.new("Map_Parchment")
    m.use_nodes = True
    nt = m.node_tree
    nt.nodes.clear()
    out = nt.nodes.new("ShaderNodeOutputMaterial")
    em = nt.nodes.new("ShaderNodeEmission")
    tc = nt.nodes.new("ShaderNodeTexCoord")
    n1 = nt.nodes.new("ShaderNodeTexNoise"); n1.inputs["Scale"].default_value = 1.6; n1.inputs["Detail"].default_value = 8.0
    n2 = nt.nodes.new("ShaderNodeTexNoise"); n2.inputs["Scale"].default_value = 9.0; n2.inputs["Detail"].default_value = 4.0
    ramp = nt.nodes.new("ShaderNodeValToRGB")
    ramp.color_ramp.elements[0].position = 0.38
    ramp.color_ramp.elements[0].color = (*hex_lin("#EAD9B0"), 1)
    ramp.color_ramp.elements[1].position = 0.72
    ramp.color_ramp.elements[1].color = (*hex_lin("#C9AE7C"), 1)
    mix = nt.nodes.new("ShaderNodeMix"); mix.data_type = 'FLOAT'; mix.inputs["Factor"].default_value = 0.3
    nt.links.new(tc.outputs["Object"], n1.inputs["Vector"])
    nt.links.new(tc.outputs["Object"], n2.inputs["Vector"])
    nt.links.new(n1.outputs["Fac"], mix.inputs[2])
    nt.links.new(n2.outputs["Fac"], mix.inputs[3])
    nt.links.new(mix.outputs[0], ramp.inputs["Fac"])
    # ふちの焦げ：中心からの距離で暗く
    sep = nt.nodes.new("ShaderNodeSeparateXYZ")
    nt.links.new(tc.outputs["Object"], sep.inputs[0])
    vlen = nt.nodes.new("ShaderNodeVectorMath"); vlen.operation = 'LENGTH'
    nt.links.new(tc.outputs["Object"], vlen.inputs[0])
    edge = nt.nodes.new("ShaderNodeMapRange")
    edge.inputs["From Min"].default_value = r * 0.80
    edge.inputs["From Max"].default_value = r * 1.0
    nt.links.new(vlen.outputs["Value"], edge.inputs["Value"])
    burn = nt.nodes.new("ShaderNodeMix"); burn.data_type = 'RGBA'
    burn.inputs["B"].default_value = (*hex_lin("#5A3A20"), 1)
    nt.links.new(ramp.outputs["Color"], burn.inputs["A"])
    pw = nt.nodes.new("ShaderNodeMath"); pw.operation = 'POWER'; pw.inputs[1].default_value = 2.2
    nt.links.new(edge.outputs["Result"], pw.inputs[0])
    nt.links.new(pw.outputs[0], burn.inputs["Factor"])
    # 四つ折りの折り目：x=0 と y=0 の近くを少し暗く
    fold = nt.nodes.new("ShaderNodeMix"); fold.data_type = 'RGBA'
    fold.inputs["B"].default_value = (*hex_lin("#B39668"), 1)
    ax = nt.nodes.new("ShaderNodeMath"); ax.operation = 'ABSOLUTE'
    ay = nt.nodes.new("ShaderNodeMath"); ay.operation = 'ABSOLUTE'
    nt.links.new(sep.outputs["X"], ax.inputs[0]); nt.links.new(sep.outputs["Y"], ay.inputs[0])
    mn = nt.nodes.new("ShaderNodeMath"); mn.operation = 'MINIMUM'
    nt.links.new(ax.outputs[0], mn.inputs[0]); nt.links.new(ay.outputs[0], mn.inputs[1])
    fr = nt.nodes.new("ShaderNodeMapRange")
    fr.inputs["From Min"].default_value = 0.0; fr.inputs["From Max"].default_value = 0.045
    fr.inputs["To Min"].default_value = 0.55; fr.inputs["To Max"].default_value = 0.0
    nt.links.new(mn.outputs[0], fr.inputs["Value"])
    nt.links.new(burn.outputs["Result"], fold.inputs["A"])
    nt.links.new(fr.outputs["Result"], fold.inputs["Factor"])
    nt.links.new(fold.outputs["Result"], em.inputs["Color"])
    nt.links.new(em.outputs[0], out.inputs["Surface"])
    me.materials.append(m)


def compass(cx, cy, r):
    """方位の星（8方向）。北の針は濃く、ほかは薄く。上に N"""
    for k in range(8):
        a = k / 8 * math.tau
        long_ = r if k % 2 == 0 else r * 0.6
        tip = (cx + math.sin(a) * long_, cy - math.cos(a) * long_)
        side = r * (0.16 if k % 2 == 0 else 0.11)
        l = (cx + math.sin(a - math.pi / 2) * side, cy - math.cos(a - math.pi / 2) * side)
        rr = (cx + math.sin(a + math.pi / 2) * side, cy - math.cos(a + math.pi / 2) * side)
        fill_poly([tip, l, (cx, cy)], INK if k % 2 == 0 else "#7A5A3A", "rose")
        fill_poly([tip, (cx, cy), rr], "#C9AE7C" if k % 2 == 0 else "#B39668", "rose")
    stroke(ellipse_points(cx, cy, r * 0.42, r * 0.42) + [ellipse_points(cx, cy, r * 0.42, r * 0.42)[0]], 1.4, INK, "rose_ring")
    text("北", cx, cy - r - 6, 15, INK)


def main():
    # 新しいシーン
    for o in list(bpy.data.objects):
        bpy.data.objects.remove(o, do_unlink=True)
    for coll in (bpy.data.meshes, bpy.data.materials, bpy.data.curves, bpy.data.cameras):
        for item in list(coll):
            coll.remove(item)
    sc = bpy.context.scene
    parchment()
    root = ET.parse(SVG_PATH).getroot()
    ns = "{http://www.w3.org/2000/svg}"
    island_outline = None
    idx = [0]

    def walk(el, inherit):
        attrs = dict(inherit)
        attrs.update({k: v for k, v in el.attrib.items() if k in ("fill", "stroke", "stroke-width", "transform")})
        style = el.attrib.get("style", "")
        m_ = re.search(r"fill:\s*(#[0-9A-Fa-f]{6})", style)
        if m_:
            attrs["fill"] = m_.group(1)
        tag = el.tag.replace(ns, "")
        idx[0] += 1
        fill, strk = attrs.get("fill", "#000000"), attrs.get("stroke")
        sw = float(attrs.get("stroke-width", "1"))
        rot = 0.0
        m_ = re.search(r"rotate\(([-\d.]+)", attrs.get("transform", ""))
        if m_:
            rot = float(m_.group(1))
        nonlocal_outline = None
        if tag in ("ellipse", "circle"):
            cx, cy = float(el.attrib["cx"]), float(el.attrib["cy"])
            rx = float(el.attrib.get("rx", el.attrib.get("r", 0)))
            ry = float(el.attrib.get("ry", el.attrib.get("r", 0)))
            if cy > LEGEND_Y:
                return
            pts = ellipse_points(cx, cy, rx, ry, rot)
            if idx[0] <= 4 and rx > 300:      # 海：塗らずに羊皮紙のまま（波の印とさざ波の線だけ）
                return
            if fill != "none":
                fill_poly(pts, fill, tag)
            if strk:
                stroke(pts + [pts[0]], sw, strk, tag + "_line")
        elif tag == "path":
            straight = not re.search(r"[CQAcqa]", el.attrib["d"])
            for pts, closed in parse_path(el.attrib["d"]):
                if min(p[1] for p in pts) > LEGEND_Y:
                    continue
                if closed and straight and len(pts) > 5:      # 直線だけの閉じた形（島・赤い台地・溶岩原）は角を丸める
                    pts = chaikin(pts[:-1] if pts[0] == pts[-1] else pts)
                    pts = pts + [pts[0]]
                if closed and fill != "none":
                    fill_poly(pts, fill, "area")
                if strk:
                    if strk.upper() == "#E9DDB6":      # 島の海岸線：濃いインク＋外へ3重のさざ波
                        walk_coast(pts)
                    else:
                        stroke(pts, sw, strk, "line", wobble=0.6, seed=idx[0])
        elif tag == "line":
            x1, y1, x2, y2 = (float(el.attrib[k]) for k in ("x1", "y1", "x2", "y2"))
            if y1 > LEGEND_Y:
                return
            stroke([(x1, y1), (x2, y2)], sw, strk or "#000000", "line")
        elif tag == "rect":
            x, y, w, h = (float(el.attrib[k]) for k in ("x", "y", "width", "height"))
            if y > LEGEND_Y:
                return
            pts = [(x, y), (x + w, y), (x + w, y + h), (x, y + h)]
            fill_poly(pts, fill, "rect")
            if strk:
                stroke(pts + [pts[0]], sw, strk, "rect_line")
        elif tag == "text":
            y = float(el.attrib["y"])
            if y > LEGEND_Y:
                return
            size = float(el.attrib.get("font-size", "12"))
            text("".join(el.itertext()), float(el.attrib["x"]), y, size * 1.1, attrs.get("fill", INK), el.attrib.get("text-anchor", "start"))
            return
        for ch in el:
            walk(ch, attrs)

    def walk_coast(pts):
        cxp = sum(p[0] for p in pts) / len(pts)
        cyp = sum(p[1] for p in pts) / len(pts)
        for k, (sc_, w_, col) in enumerate(((1.035, 1.3, "#6F5A40"), (1.07, 1.0, "#8A7556"), (1.11, 0.8, "#9C8866"))):
            ring = [(cxp + (x - cxp) * sc_, cyp + (y - cyp) * sc_) for x, y in pts]
            stroke(ring, w_, col, "ripple", wobble=0.8, seed=40 + k)
        stroke(pts, 3.2, INK, "coast", wobble=0.7, seed=7)

    walk(root, {})
    # 方位の星、ふちの二重線と目盛り、海の小さな波
    compass(120, 440, 46)
    circ = ellipse_points(CX, CY, RADIUS * 0.975, RADIUS * 0.975, n=180)
    stroke(circ + [circ[0]], 2.4, INK, "border")
    circ2 = ellipse_points(CX, CY, RADIUS * 0.955, RADIUS * 0.955, n=180)
    stroke(circ2 + [circ2[0]], 1.0, "#6F5A40", "border2")
    for k in range(72):
        a = k / 72 * math.tau
        r0, r1 = RADIUS * 0.955, RADIUS * (0.975 if k % 6 else 0.99)
        stroke([(CX + math.cos(a) * r0, CY + math.sin(a) * r0), (CX + math.cos(a) * r1, CY + math.sin(a) * r1)], 1.0, INK, "tick")
    rnd = random.Random(5)
    for k in range(26):
        while True:
            x, y = rnd.uniform(60, 640), rnd.uniform(40, 510)
            sea = ((x - 350) / 310) ** 2 + ((y - 275) / 245) ** 2
            land = ((x - 370) / 240) ** 2 + ((y - 265) / 170) ** 2
            if sea < 0.92 and land > 1.15 and ((x - 120) ** 2 + (y - 440) ** 2) > 70 ** 2:
                break
        wave = [(x + i * 3, y - 2.5 * math.sin(i / 6 * math.pi)) for i in range(7)]
        stroke(wave, 1.0, "#7E6A4C", "wave")
    # 撮る
    cam_d = bpy.data.cameras.new("MapCam")
    cam_d.type = 'ORTHO'
    cam_d.ortho_scale = RADIUS * S * 2.0
    cam = bpy.data.objects.new("MapCam", cam_d)
    cam.location = (0, 0, 5)
    sc.collection.objects.link(cam)
    sc.camera = cam
    try:
        sc.render.engine = 'BLENDER_EEVEE'
    except TypeError:
        pass
    sc.render.resolution_x = sc.render.resolution_y = RES
    sc.render.resolution_percentage = 100
    sc.render.film_transparent = False
    try:
        sc.view_settings.view_transform = 'Standard'
        sc.view_settings.look = 'None'
    except TypeError:
        pass
    if sc.world is None:
        sc.world = bpy.data.worlds.new("World")
    sc.world.use_nodes = True
    bg = next(n for n in sc.world.node_tree.nodes if n.type == "BACKGROUND")
    bg.inputs["Color"].default_value = (*hex_lin("#5A3A20"), 1)
    bg.inputs["Strength"].default_value = 1.0
    sc.render.image_settings.file_format = 'PNG'
    os.makedirs(os.path.dirname(OUT_PNG), exist_ok=True)
    sc.render.filepath = OUT_PNG
    bpy.ops.render.render(write_still=True)
    bpy.ops.wm.save_as_mainfile(filepath=BLEND_PATH)
    print("map written", OUT_PNG)


if __name__ == "__main__" or True:
    main()
