# 岩塩の採る場所「塩のきのこ」を、「塩だ！掘ろう！」と思わせる手がかりつきで作るスクリプト（Blender 5.2）
# 設定：塩は雨で溶け、乾くと育つ（仕様）。根元ははね返りの雨と流れる水で先に細り、乾きやすい上に塩が育って、かさが広がる。
#       掘られたきのこは、乾いた天気が続くとかさが育って戻る（戻る様子が形で見える）。死海にも本当に塩のきのこがある。
# 掘りたくなる手がかり：
#   冠    … かさの上に立方体の結晶が生える（「いい所はここ」）
#   割れ口 … かさの端が欠けて、中の真っ白な新しい塩がのぞく。足元に欠片（「割れる物だ」）
#   輪    … 柄に、雨の水位の跡のような塩の殻の輪
#   段階  … 満タン → 掘りかけ → 切り株
# 質感は Unity の本番マテリアル（RockSalt_Rock / RockSalt_Crystal、リアル度 0.2）＋割れ口の RockSalt_Fresh（明るく、キラキラ強め）。
import bpy, bmesh, math, random
from mathutils import Vector, Matrix
from mathutils import noise as mnoise

EXPORT_DIR = "C:/UnityProjects/ExtractionGame/Assets/Art/Models/RockSalt/Mushroom/"
EXPORT = True
MATS = ["RockSalt_Rock", "RockSalt_Crystal", "RockSalt_Fresh"]
ROCK, CRY, FRESH = 0, 1, 2
COLORS = {"RockSalt_Rock": (0.3, 0.28, 0.24), "RockSalt_Crystal": (0.84, 0.72, 0.68), "RockSalt_Fresh": (0.97, 0.95, 0.93)}


def get_mat(name):
    m = bpy.data.materials.get(name) or bpy.data.materials.new(name)
    m.diffuse_color = (*COLORS[name], 1)
    return m


def nz(p):
    return mnoise.noise(p)


def clear_scene():
    for o in list(bpy.data.objects):
        bpy.data.objects.remove(o, do_unlink=True)
    for m in list(bpy.data.meshes):
        bpy.data.meshes.remove(m)


def set_mat(bm, idx):
    for f in bm.faces:
        f.material_index = idx
    return bm


def merge(bm, cb, mat=None):
    """cb の面ごとの material_index を残したまま bm に足す（cb は解放）。"""
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


def T(x, y, z):
    return Matrix.Translation((x, y, z))


def RZ(a):
    return Matrix.Rotation(a, 4, "Z")


def tilt(rng, max_tilt):
    axis = Vector((rng.uniform(-1, 1), rng.uniform(-1, 1), 0))
    if axis.length < 1e-3:
        axis = Vector((1, 0, 0))
    return Matrix.Rotation(rng.uniform(0, max_tilt), 4, axis.normalized())


def hopper_face(cb, face, span, depth, steps):
    step_in = span * 0.36 / max(steps, 1) / 2
    for i in range(steps):
        bmesh.ops.inset_individual(cb, faces=[face], thickness=step_in, depth=0)
        bmesh.ops.inset_individual(cb, faces=[face], thickness=0, depth=-depth)


def cube(size, bevel=0.07, hopper=0, mat=CRY):
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
        hopper_face(cb, top, min(w, d), h * 0.04, hopper)
    return set_mat(cb, mat)


def lumpy(rng, rx, ry, rz, subdiv=3, rough=0.2, flat=None, mat=CRY, freq=2.0):
    rb = bmesh.new()
    bmesh.ops.create_icosphere(rb, subdivisions=subdiv, radius=1.0)
    off = Vector((rng.uniform(0, 50), rng.uniform(0, 50), rng.uniform(0, 50)))
    for v in rb.verts:
        p = v.co
        k = 1 + rough * nz(p * freq + off) + rough * 0.35 * nz(p * freq * 3 + off * 2)
        q = Vector((p.x * rx * k, p.y * ry * k, p.z * rz * k))
        if flat is not None and q.z < flat:
            q.z = flat + (q.z - flat) * 0.15
        v.co = q
    return set_mat(rb, mat)


def cut(bm, co, no, fill_mat):
    geom = list(bm.verts) + list(bm.edges) + list(bm.faces)
    res = bmesh.ops.bisect_plane(bm, geom=geom, plane_co=co, plane_no=no, clear_outer=True)
    edges = [e for e in res["geom_cut"] if isinstance(e, bmesh.types.BMEdge)]
    if edges:
        for f in bmesh.ops.holes_fill(bm, edges=edges, sides=0)["faces"]:
            f.material_index = fill_mat


# ---------------------------------------------------------------- 部品
def stem(rng, r, h, rings):
    """柄：根元が少し細く（雨で溶けた）、上へ太くなる。rings=水位の跡の輪の数。"""
    sb = bmesh.new()
    bmesh.ops.create_cone(sb, cap_ends=True, cap_tris=False, segments=12, radius1=r * 0.85, radius2=r * 1.1, depth=h)
    bmesh.ops.subdivide_edges(sb, edges=[e for e in sb.edges if abs(e.verts[0].co.z - e.verts[1].co.z) > 1e-4], cuts=4)
    off = Vector((rng.uniform(0, 50), 0, 0))
    for v in sb.verts:
        k = 1 + 0.12 * nz(v.co * 5 / max(r, 0.05) * 0.2 + off)
        v.co = Vector((v.co.x * k, v.co.y * k, v.co.z + h / 2))
    set_mat(sb, CRY)
    for i in range(rings):
        z = h * (0.2 + 0.6 * i / max(rings - 1, 1)) + rng.uniform(-0.02, 0.02)
        ring = lumpy(rng, r * 1.22, r * 1.22, h * 0.06, subdiv=3, rough=0.15, mat=CRY, freq=3)
        merge(sb, ring, T(0, 0, z))
    return sb


def cap(rng, rx, rz):
    """かさ：横に広いこぶの塊。下は少し平ら。"""
    return lumpy(rng, rx, rx * 0.92, rz, subdiv=3, rough=0.2, flat=-rz * 0.45, mat=CRY, freq=2.0)


def crown(cb, rng, rx, rz, n):
    """かさの上に立方体の結晶を生やす。"""
    for k in range(n):
        a = rng.uniform(0, math.tau)
        d = math.sqrt(rng.random()) * rx * 0.55
        s = rng.uniform(0.08, 0.17) * (1.0 if d < rx * 0.3 else 0.75)
        e = (d / rx) ** 2
        z = rz * math.sqrt(max(0.0, 1 - e)) * 0.85
        merge(cb, cube((s, s * rng.uniform(0.9, 1.1), s * rng.uniform(0.9, 1.15)), hopper=2 if s > 0.13 else 0),
              T(math.cos(a) * d, math.sin(a) * d, z - s * 0.3) @ tilt(rng, 0.45) @ RZ(rng.uniform(0, 3)))


def break_edge(cb, rng, rx, rz, a, depth):
    """かさの端を欠く。割れ口は新しい塩（明るい白）。"""
    no = Vector((math.cos(a), math.sin(a), rng.uniform(0.15, 0.4))).normalized()
    cut(cb, no * rx * (1 - depth), no, FRESH)


def chunks(bm, rng, n, a, dist, mat=FRESH):
    """足元に落ちた欠片。"""
    for k in range(n):
        aa = a + rng.uniform(-0.6, 0.6)
        d = dist * rng.uniform(0.85, 1.25)
        s = rng.uniform(0.08, 0.14)
        merge(bm, cube((s, s * 0.9, s * 0.8), bevel=0.15, mat=mat if k % 2 == 0 else CRY),
              T(math.cos(aa) * d, math.sin(aa) * d, -s * 0.15) @ tilt(rng, 0.7) @ RZ(rng.uniform(0, 3)))


def base(bm, rng, r):
    merge(bm, lumpy(rng, r, r * 0.9, 0.08, subdiv=3, rough=0.25, flat=-0.02, mat=ROCK, freq=1.4))


# ---------------------------------------------------------------- 組み立て
def mushroom(rng, crown_n=0, broken=False, rings=0, stage=0, extra=True):
    """stage 0 満タン、1 掘りかけ（かさが小さく、大きく欠けている）、2 切り株（かさなし、柄の上が新しい塩）。"""
    bm = bmesh.new()
    base(bm, rng, 0.55)
    R, H = 0.16, 0.42
    rx, rz = 0.36, 0.2
    if stage == 2:
        H *= 0.7
    sb = stem(rng, R, H, rings if stage < 2 else max(rings - 1, 0))
    if stage == 2:
        cut(sb, Vector((0, 0, H * 0.95)), Vector((0.25, 0, 1)).normalized(), FRESH)
    merge(bm, sb, T(0, 0, -0.02))
    a_break = rng.uniform(0, math.tau)
    if stage < 2:
        s = 1.0 if stage == 0 else 0.7
        cb = cap(rng, rx * s, rz * s)
        if crown_n and stage == 0:
            crown(cb, rng, rx, rz, crown_n)
        if broken or stage == 1:
            break_edge(cb, rng, rx * s, rz * s, a_break, 0.35 if stage == 0 else 0.55)
            if stage == 1:
                break_edge(cb, rng, rx * s, rz * s, a_break + 2.3, 0.4)
        merge(bm, cb, T(0, 0, H * 0.95) @ tilt(rng, 0.15))
    if broken or stage > 0:
        chunks(bm, rng, 3 if stage == 0 else 5, a_break, 0.48)
    if extra:  # 横に中と小のきのこ（育ちかけ。前回の「02 塩のきのこ」の群れ）
        a0 = a_break + math.pi
        for k, sc in enumerate((0.62, 0.48)):
            a = a0 + (k * 2 - 1) * 0.75 + rng.uniform(-0.2, 0.2)
            small = stem(rng, R * sc, H * sc, 1 if rings else 0)
            c2 = cap(rng, rx * sc, rz * sc)
            if crown_n and stage == 0 and k == 0:
                crown(c2, rng, rx * sc, rz * sc, 3)
            merge(small, c2, T(0, 0, H * sc * 0.95) @ tilt(rng, 0.25))
            merge(bm, small, T(math.cos(a) * 0.42, math.sin(a) * 0.36, -0.02))
    return bm


VARIANTS = [
    ("M1_Plain", dict(crown_n=0, broken=False, rings=0)),
    ("M2_Crown", dict(crown_n=7, broken=False, rings=0)),
    ("M3_Broken", dict(crown_n=0, broken=True, rings=0)),
    ("M4_Rings", dict(crown_n=0, broken=False, rings=3)),
    ("M5_All", dict(crown_n=7, broken=True, rings=3)),
    ("M5_All_Half", dict(crown_n=7, broken=True, rings=3, stage=1)),
    ("M5_All_Stump", dict(crown_n=7, broken=True, rings=3, stage=2)),
]


def make_object(name, bm, ground=-0.03):
    zmin = min(v.co.z for v in bm.verts)
    bmesh.ops.translate(bm, vec=Vector((0, 0, ground - zmin)), verts=bm.verts)
    me = bpy.data.meshes.new(name)
    for m in MATS:
        me.materials.append(get_mat(m))
    bm.normal_update()
    bm.to_mesh(me); bm.free()
    for p in me.polygons:
        p.use_smooth = True
    ob = bpy.data.objects.new(name, me)
    bpy.context.scene.collection.objects.link(ob)
    uvb = bmesh.new(); uvb.from_mesh(me)
    uv = uvb.loops.layers.uv.verify()
    for f in uvb.faces:
        ax = max(range(3), key=lambda i: abs(f.normal[i]))
        for l in f.loops:
            c = l.vert.co
            l[uv].uv = (c.y, c.z) if ax == 0 else ((c.x, c.z) if ax == 1 else (c.x, c.y))
    uvb.to_mesh(me); uvb.free()
    return ob


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
    objs = []
    for i, (name, kw) in enumerate(VARIANTS):
        ob = make_object("RockSaltMush_" + name, mushroom(random.Random(7), **kw))
        ob.location = (i * 1.4, 0, 0)
        objs.append(ob)
    if EXPORT:
        for ob in objs:
            export_fbx(ob)
    return objs


built = main()
print("built", [(o.name[13:], sum(len(p.vertices) - 2 for p in o.data.polygons), tuple(round(d, 2) for d in o.dimensions)) for o in built])
