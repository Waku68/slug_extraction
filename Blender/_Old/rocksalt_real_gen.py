# 岩塩の見直し（リアル寄り）3案を作るスクリプト（Blender 5.2）
# 第1版（rocksalt_gen.py）は縦長の柱が花束のように広がる、ファンタジーの水晶寄りの形だった。
# 本物の岩塩（ハライト）に寄せる：
#   A 立方体の群れ  … 結晶はほぼ立方体。大小ばらばらの小さな立方体が、ランダムな向きで絡み合って生える
#   B 地層の塊      … 鉱山の岩塩のように、ピンク・白・橙の層が重なった塊。立方体に割れる段差
#   C 塩をかぶった岩 … 洞窟の岩の上面を白い塩の殻が覆い、細かい粒と少しの結晶が付く
# 書き出すメッシュは滑らかな法線（Unity の Slug/Toon の Flat Facets＋輪郭線で描く）。
# 使い方：Blender の MCP から実行するか、Scripting タブで開いて実行。
import bpy, bmesh, math, random
from mathutils import Vector, Matrix
from mathutils import noise as mnoise

EXPORT_DIR = "C:/UnityProjects/ExtractionGame/Assets/_Old/Art/RockSalt/Real/"
EXPORT = True

# Blender のプレビュー用の色（Unity 側はマテリアルで決める）
COLORS = {
    "RockSaltReal_Rock": (0.24, 0.22, 0.2),
    "RockSaltReal_Crystal": (0.93, 0.85, 0.83),
    "RockSaltReal_BandPink": (0.86, 0.6, 0.55),
    "RockSaltReal_BandWhite": (0.93, 0.9, 0.86),
    "RockSaltReal_BandOrange": (0.8, 0.5, 0.36),
    "RockSaltReal_BandGray": (0.55, 0.5, 0.48),
    "RockSaltReal_Crust": (0.95, 0.94, 0.92),
}


def get_mat(name):
    m = bpy.data.materials.get(name)
    if m is None:
        m = bpy.data.materials.new(name)
        nt = m.node_tree
        nt.nodes.clear()
        out = nt.nodes.new("ShaderNodeOutputMaterial")
        b = nt.nodes.new("ShaderNodeBsdfPrincipled")
        b.inputs["Base Color"].default_value = (*COLORS[name], 1)
        b.inputs["Roughness"].default_value = 0.6
        nt.links.new(b.outputs["BSDF"], out.inputs["Surface"])
    m.diffuse_color = (*COLORS[name], 1)  # ビューポートの色
    return m


def clear_scene():
    for o in list(bpy.data.objects):
        bpy.data.objects.remove(o, do_unlink=True)
    for m in list(bpy.data.meshes):
        bpy.data.meshes.remove(m)


# ---------------------------------------------------------------- 部品
def cube(size, bevel=0.03, hopper=0):
    """ほぼ立方体の結晶。底は z=0。hopper=上面の階段状のくぼみの段数。"""
    w, d, h = size
    cb = bmesh.new()
    bmesh.ops.create_cube(cb, size=1.0)
    for v in cb.verts:
        v.co = Vector((v.co.x * w, v.co.y * d, v.co.z * h + h / 2))
    if bevel > 0:
        bmesh.ops.bevel(cb, geom=list(cb.edges), offset=min(w, d, h) * bevel, segments=1,
                        affect="EDGES", profile=0.5, clamp_overlap=True)
    cb.normal_update()
    if hopper:
        top = max(cb.faces, key=lambda f: f.normal.z)
        for i in range(hopper):
            bmesh.ops.inset_individual(cb, faces=[top], thickness=min(w, d) * 0.11, depth=0)
            bmesh.ops.translate(cb, vec=Vector((0, 0, -h * 0.07)), verts=list(top.verts))
    return cb


def lumpy_rock(rng, rx, ry, rz, subdiv=3, rough=0.2, flat=-0.04):
    """ごつごつした岩。原点の上に置き、底は平ら。"""
    rb = bmesh.new()
    bmesh.ops.create_icosphere(rb, subdivisions=subdiv, radius=1.0)
    off = Vector((rng.uniform(0, 50), rng.uniform(0, 50), rng.uniform(0, 50)))
    for v in rb.verts:
        p = v.co
        k = 1 + rough * nz(p * 1.4 + off) + rough * 0.35 * nz(p * 4.0 + off * 2)
        p = Vector((p.x * rx * k, p.y * ry * k, p.z * rz * k))
        if p.z < flat:
            p.z = flat + (p.z - flat) * 0.1  # 底は平らに（少しだけ丸みを残す）
        v.co = p
    return rb


def nz(p):
    return mnoise.noise(p)


def merge(bm, cb, mat=None, mat_index=0):
    """cb を変換して bm に足す（cb は解放）。メッシュ経由だと番号が落ちるので付け直す。"""
    if mat is not None:
        bmesh.ops.transform(cb, matrix=mat, verts=list(cb.verts))
    tmp = bpy.data.meshes.new("_tmp")
    cb.to_mesh(tmp); cb.free()
    n0 = len(bm.faces)
    bm.from_mesh(tmp)
    bm.faces.ensure_lookup_table()
    for f in bm.faces[n0:]:
        f.material_index = mat_index
    bpy.data.meshes.remove(tmp)


def merge_keep(bm, cb, mat=None):
    """cb の面ごとの material_index を残したまま足す。"""
    if mat is not None:
        bmesh.ops.transform(cb, matrix=mat, verts=list(cb.verts))
    idx = [f.material_index for f in cb.faces]
    tmp = bpy.data.meshes.new("_tmp")
    cb.to_mesh(tmp); cb.free()
    n0 = len(bm.faces)
    bm.from_mesh(tmp)
    bm.faces.ensure_lookup_table()
    for f, i in zip(bm.faces[n0:], idx):
        f.material_index = i
    bpy.data.meshes.remove(tmp)


def rand_rot(rng, max_tilt):
    axis = Vector((rng.uniform(-1, 1), rng.uniform(-1, 1), 0))
    if axis.length < 1e-3:
        axis = Vector((1, 0, 0))
    return Matrix.Rotation(rng.uniform(0, max_tilt), 4, axis.normalized()) @ Matrix.Rotation(rng.uniform(0, math.tau), 4, "Z")


def surface_z(rx, ry, rz, x, y):
    e = (x / rx) ** 2 + (y / ry) ** 2
    return rz * math.sqrt(max(0.0, 1.0 - e))


def make_object(name, bm, mats, ground=-0.03):
    zmin = min(v.co.z for v in bm.verts)
    bmesh.ops.translate(bm, vec=Vector((0, 0, ground - zmin)), verts=bm.verts)
    bmesh.ops.remove_doubles(bm, verts=bm.verts, dist=1e-5)
    me = bpy.data.meshes.new(name)
    for m in mats:  # 先に枠を作る（枠がないと material_index が 0 に戻る）
        me.materials.append(get_mat(m))
    bm.normal_update()
    bm.to_mesh(me); bm.free()
    for p in me.polygons:
        p.use_smooth = True
    ob = bpy.data.objects.new(name, me)
    bpy.context.scene.collection.objects.link(ob)
    box_uv(ob)
    return ob


def box_uv(ob):
    me = ob.data
    bm = bmesh.new(); bm.from_mesh(me)
    uv = bm.loops.layers.uv.verify()
    for f in bm.faces:
        n = f.normal
        ax = max(range(3), key=lambda i: abs(n[i]))
        for l in f.loops:
            c = l.vert.co
            l[uv].uv = (c.y, c.z) if ax == 0 else ((c.x, c.z) if ax == 1 else (c.x, c.y))
    bm.to_mesh(me); bm.free()


def break_off(bm, rng, s, cuts, fill_index):
    """斜めに割る（割れ口は fill_index の面）。"""
    for k in range(cuts):
        no = Vector((rng.uniform(-1, 1), rng.uniform(-1, 1), rng.uniform(-0.4, 1))).normalized()
        co = no * s * rng.uniform(0.25, 0.35) + Vector((0, 0, s * 0.45))
        geom = list(bm.verts) + list(bm.edges) + list(bm.faces)
        res = bmesh.ops.bisect_plane(bm, geom=geom, plane_co=co, plane_no=no, clear_outer=True)
        cut = [e for e in res["geom_cut"] if isinstance(e, bmesh.types.BMEdge)]
        if cut:
            for f in bmesh.ops.holes_fill(bm, edges=cut, sides=0)["faces"]:
                f.material_index = fill_index


# ---------------------------------------------------------------- A 立方体の群れ
A_MATS = ["RockSaltReal_Rock", "RockSaltReal_Crystal"]


def build_cubes(seed):
    rng = random.Random(seed)
    bm = bmesh.new()
    rx, ry, rz = 0.45, 0.38, 0.24
    merge(bm, lumpy_rock(rng, rx, ry, rz, subdiv=3, rough=0.22), mat_index=0)
    for i in range(55):
        r = rng.random()
        s = 0.045 + 0.22 * r ** 2.5        # ほとんど小さく、少しだけ大きい
        # 大きい物ほど真ん中寄り
        spread = 0.25 + 0.7 * (1 - min(1, s / 0.2))
        a = rng.uniform(0, math.tau)
        dist = math.sqrt(rng.random()) * spread
        x, y = math.cos(a) * rx * dist, math.sin(a) * ry * dist
        w, d, h = (s * rng.uniform(0.9, 1.12) for _ in range(3))
        hop = 2 if s > 0.17 else (1 if s > 0.1 and rng.random() < 0.6 else 0)
        cb = cube((w, d, h), bevel=0.025, hopper=hop)
        z = surface_z(rx, ry, rz, x, y) * 0.9 - h * rng.uniform(0.1, 0.3)
        m = Matrix.Translation((x, y, z)) @ rand_rot(rng, math.radians(40))
        merge(bm, cb, m, mat_index=1)
    return make_object("RockSaltReal_Cubes_Node", bm, A_MATS)


def build_cubes_chunk(seed):
    rng = random.Random(seed)
    s = 0.16
    bm = cube((s, s * 0.95, s * 0.92), bevel=0.025, hopper=1)
    for f in bm.faces:
        f.material_index = 1
    for k in range(3):  # くっついた小さな立方体
        t = rng.uniform(0.04, 0.07)
        c = cube((t, t, t), bevel=0.025)
        for f in c.faces:
            f.material_index = 1
        m = Matrix.Translation((rng.uniform(-1, 1) * s * 0.45, rng.uniform(-1, 1) * s * 0.45, rng.uniform(0.2, 0.8) * s)) @ rand_rot(rng, 0.8)
        merge_keep(bm, c, m)
    break_off(bm, rng, s, 1, 1)
    return make_object("RockSaltReal_Cubes_Chunk", bm, A_MATS, ground=0.0)


# ---------------------------------------------------------------- B 地層の塊
B_MATS = ["RockSaltReal_Rock", "RockSaltReal_BandPink", "RockSaltReal_BandWhite",
          "RockSaltReal_BandOrange", "RockSaltReal_BandGray"]
BAND_ORDER = [4, 1, 2, 1, 3, 2, 1]   # 下から：灰・ピンク・白・ピンク・橙・白・ピンク


def slab(rng, w, d, t, jitter, band):
    cb = cube((w, d, t), bevel=0.04)
    for v in cb.verts:
        v.co += Vector((rng.uniform(-1, 1), rng.uniform(-1, 1), rng.uniform(-0.3, 0.3))) * jitter
    for f in cb.faces:
        f.material_index = band
    return cb


def band_cut(bm, rng, n_b, lo, hi, step=(0.06, 0.14)):
    """斜めの平面で切れ目を入れ、面を縞ごとの色に分ける（地層）。"""
    offsets = []
    k = lo + rng.uniform(*step)
    while k < hi:
        offsets.append(k)
        geom = list(bm.verts) + list(bm.edges) + list(bm.faces)
        bmesh.ops.bisect_plane(bm, geom=geom, plane_co=n_b * k, plane_no=n_b)
        k += rng.uniform(*step)
    for f in bm.faces:
        d = f.calc_center_median().dot(n_b)
        i = sum(1 for o in offsets if d > o)
        f.material_index = BAND_ORDER[i % len(BAND_ORDER)]


def blocky_rock(rng, size, chips, cuts=3, jitter=0.06):
    """角ばった岩（立方体に割れる岩塩らしく、欠けた面は軸に近い向き）。"""
    w, d, h = size
    bm = bmesh.new()
    bmesh.ops.create_cube(bm, size=1.0)
    bmesh.ops.subdivide_edges(bm, edges=list(bm.edges), cuts=cuts, use_grid_fill=True)
    off = Vector((rng.uniform(0, 50), rng.uniform(0, 50), rng.uniform(0, 50)))
    for v in bm.verts:
        p = v.co
        p = p + Vector((nz(p * 2 + off), nz(p * 2 + off + Vector((9, 0, 0))), nz(p * 2 + off + Vector((0, 9, 0))))) * jitter
        v.co = Vector((p.x * w, p.y * d, (p.z + 0.5) * h))
    for k in range(chips):  # 角や縁を浅く欠く
        sx, sy = rng.choice((-1, 1)), rng.choice((-1, 1))
        corner = Vector((sx * w * 0.5, sy * d * 0.5, h if rng.random() < 0.7 else 0.0))
        ax = Vector((sx * rng.uniform(0.3, 1), sy * rng.uniform(0.3, 1), (1 if corner.z > 0 else -0.3) * rng.uniform(0.3, 1))).normalized()
        co = corner - ax * min(w, d, h) * rng.uniform(0.15, 0.32)
        geom = list(bm.verts) + list(bm.edges) + list(bm.faces)
        res = bmesh.ops.bisect_plane(bm, geom=geom, plane_co=co, plane_no=ax, clear_outer=True)
        cut = [e for e in res["geom_cut"] if isinstance(e, bmesh.types.BMEdge)]
        if cut:
            bmesh.ops.holes_fill(bm, edges=cut, sides=0)
    for v in bm.verts:
        if v.co.z < 0:
            v.co.z = 0
    return bm


def build_layered(seed):
    rng = random.Random(seed)
    bm = blocky_rock(rng, (1.0, 0.8, 0.72), chips=11, jitter=0.1)
    n_b = Vector((0.3, 0.12, 1.0)).normalized()   # 地層は少し傾いている
    zs = [v.co.dot(n_b) for v in bm.verts]
    band_cut(bm, rng, n_b, min(zs), max(zs))
    # 足元の岩くず
    for k in range(4):
        a = rng.uniform(0, math.tau)
        rb = lumpy_rock(rng, rng.uniform(0.08, 0.14), rng.uniform(0.07, 0.12), rng.uniform(0.05, 0.09), subdiv=2, rough=0.25)
        merge(bm, rb, Matrix.Translation((math.cos(a) * 0.6, math.sin(a) * 0.48, 0.0)), mat_index=0)
    return make_object("RockSaltReal_Layered_Node", bm, B_MATS)


def build_layered_chunk(seed):
    rng = random.Random(seed)
    bm = blocky_rock(rng, (0.2, 0.17, 0.16), chips=3, cuts=2, jitter=0.05)
    n_b = Vector((0.3, 0.12, 1.0)).normalized()
    zs = [v.co.dot(n_b) for v in bm.verts]
    band_cut(bm, rng, n_b, min(zs), max(zs), step=(0.03, 0.05))
    return make_object("RockSaltReal_Layered_Chunk", bm, B_MATS, ground=0.0)


# ---------------------------------------------------------------- C 塩をかぶった岩
C_MATS = ["RockSaltReal_Rock", "RockSaltReal_Crust", "RockSaltReal_Crystal"]


def crust_over(bm, rng, thickness, up=0.3, patch=-0.15, scale=2.6):
    """上を向いた面の一部を、白い殻（material 1）として外へ押し出す。"""
    bm.normal_update()
    off = Vector((rng.uniform(0, 50), rng.uniform(0, 50), 0))
    faces = [f for f in bm.faces if f.normal.z > up and nz(f.calc_center_median() * scale + off) > patch]
    if not faces:
        return []
    res = bmesh.ops.extrude_face_region(bm, geom=faces)
    verts = [v for v in res["geom"] if isinstance(v, bmesh.types.BMVert)]
    bm.normal_update()
    for v in verts:
        v.co += v.normal * thickness * rng.uniform(0.7, 1.3)
    new_faces = [f for f in res["geom"] if isinstance(f, bmesh.types.BMFace)]
    for f in new_faces:
        f.material_index = 1
    # 側面（押し出しでできた縁）も殻の色に
    for v in verts:
        for f in v.link_faces:
            f.material_index = 1
    return new_faces


def build_crust(seed):
    rng = random.Random(seed)
    bm = bmesh.new()
    merge(bm, lumpy_rock(rng, 0.55, 0.45, 0.36, subdiv=4, rough=0.25), mat_index=0)  # 細かくして殻の縁をなめらかに
    top = crust_over(bm, rng, 0.018, up=0.25, patch=0.0, scale=2.2)
    bm.faces.ensure_lookup_table()
    # 殻の上の細かい粒
    off = Vector((rng.uniform(0, 50), rng.uniform(0, 50), 0))
    dense = [f for f in top if nz(f.calc_center_median() * 5 + off) > 0.15]  # 粒は固まって付く
    picks = rng.sample(dense, min(55, len(dense)))
    for f in picks:
        c = f.calc_center_median()
        s = rng.uniform(0.015, 0.045)
        cb = cube((s, s, s), bevel=0.0)
        merge(bm, cb, Matrix.Translation(c - f.normal * s * 0.3) @ rand_rot(rng, 1.2), mat_index=1)
    # くぼみに少しだけ育った結晶
    a = rng.uniform(0, math.tau)
    for k in range(6):
        s = rng.uniform(0.07, 0.15)
        x = math.cos(a) * 0.12 + rng.uniform(-0.1, 0.1)
        y = math.sin(a) * 0.1 + rng.uniform(-0.08, 0.08)
        cb = cube((s, s * rng.uniform(0.9, 1.1), s * rng.uniform(0.9, 1.1)), bevel=0.025, hopper=1 if s > 0.11 else 0)
        z = surface_z(0.55, 0.45, 0.36, x, y) * 0.95 - s * 0.35
        merge(bm, cb, Matrix.Translation((x, y, z)) @ rand_rot(rng, math.radians(35)), mat_index=2)
    return make_object("RockSaltReal_Crust_Node", bm, C_MATS)


def build_crust_chunk(seed):
    rng = random.Random(seed)
    bm = bmesh.new()
    merge(bm, lumpy_rock(rng, 0.11, 0.09, 0.08, subdiv=2, rough=0.3, flat=-0.03), mat_index=0)
    crust_over(bm, rng, 0.012, up=0.2, patch=-0.6, scale=8)
    for k in range(2):
        s = rng.uniform(0.04, 0.06)
        merge(bm, cube((s, s, s), bevel=0.025), Matrix.Translation((rng.uniform(-0.04, 0.04), rng.uniform(-0.03, 0.03), 0.06)) @ rand_rot(rng, 0.6), mat_index=2)
    return make_object("RockSaltReal_Crust_Chunk", bm, C_MATS, ground=0.0)


# ---------------------------------------------------------------- 書き出し
def export_fbx(ob):
    import os
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
    objs = [build_cubes(3), build_layered(7), build_crust(5),
            build_cubes_chunk(2), build_layered_chunk(4), build_crust_chunk(9)]
    for i, ob in enumerate(objs[:3]):
        ob.location = (i * 1.6, 0, 0)
    for i, ob in enumerate(objs[3:]):
        ob.location = (i * 1.6, -1.0, 0)
    if EXPORT:
        for ob in objs:
            export_fbx(ob)
    return objs


built = main()
print("built", [(o.name, sum(len(p.vertices) - 2 for p in o.data.polygons)) for o in built])
