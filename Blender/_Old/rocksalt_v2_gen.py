# 岩塩の採る場所を、形の考え方から10種類作り直すスクリプト（Blender 5.2）
# 質感は Unity の本番マテリアル（RockSalt_Crystal / RockSalt_Rock、リアル度 0.2）をそのまま使うので、違うのは形だけ。
# 形はポップのまま、それぞれに「塩らしさ」の手がかり（立方体、階段状のくぼみ、白いこぶ、割れ目の結晶など）を入れる。
#  01 塩の塔     立方体の塊を少しずつずらして積んだ塔
#  02 塩のきのこ  死海の塩の塊のような、白くこぶこぶしたきのこ形
#  03 大きな一粒  大きな立方体の結晶が1つ、斜めに埋まっている
#  04 骸晶       面がピラミッド状にくぼんだ、岩塩らしい結晶
#  05 露頭       地面から突き出した塩の壁。上が階段状
#  06 積み塩     角の丸い塩の塊の山
#  07 鉱脈       暗い岩が割れて、割れ目に塩の結晶が挟まっている
#  08 塩の花     結晶が花びらのように外へ広がる
#  09 塩筍       洞窟の石筍のような、塩のとんがりの群れ
#  10 晶洞       岩が割れて、中に結晶がびっしり
import bpy, bmesh, math, random
from mathutils import Vector, Matrix
from mathutils import noise as mnoise

EXPORT_DIR = "C:/UnityProjects/ExtractionGame/Assets/_Old/Art/RockSalt/V2/"
EXPORT = True
MATS = ["RockSalt_Rock", "RockSalt_Crystal"]   # Unity の本番マテリアルと同じ名前
ROCK, CRY = 0, 1
COLORS = {"RockSalt_Rock": (0.3, 0.28, 0.24), "RockSalt_Crystal": (0.84, 0.72, 0.68)}


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


# ---------------------------------------------------------------- 部品
def set_mat(bm, idx):
    for f in bm.faces:
        f.material_index = idx
    return bm


def cube(size, bevel=0.06, hopper=0, mat=CRY):
    """立方体の結晶（底は z=0）。hopper=上面の階段状のくぼみの段数。"""
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
        hopper_face(cb, top, min(w, d), h * 0.035, hopper)
    return set_mat(cb, mat)


def hopper_face(cb, face, span, depth, steps):
    """面を浅い階段状にくぼませる（岩塩の骸晶）。空き箱に見えないよう、段は浅く細かく。"""
    step_in = span * 0.36 / max(steps, 1) / 2
    for i in range(steps):
        bmesh.ops.inset_individual(cb, faces=[face], thickness=step_in, depth=0)   # 平らな段
        bmesh.ops.inset_individual(cb, faces=[face], thickness=0, depth=-depth)    # まっすぐ下がる壁


def lumpy(rng, rx, ry, rz, subdiv=3, rough=0.2, flat=-0.03, mat=ROCK, freq=1.4):
    """こぶのある塊。原点の上に置き、底は平ら。"""
    rb = bmesh.new()
    bmesh.ops.create_icosphere(rb, subdivisions=subdiv, radius=1.0)
    off = Vector((rng.uniform(0, 50), rng.uniform(0, 50), rng.uniform(0, 50)))
    for v in rb.verts:
        p = v.co
        k = 1 + rough * nz(p * freq + off) + rough * 0.35 * nz(p * freq * 3 + off * 2)
        p = Vector((p.x * rx * k, p.y * ry * k, p.z * rz * k))
        if p.z < flat:
            p.z = flat + (p.z - flat) * 0.1
        v.co = p
    return set_mat(rb, mat)


def cone(rng, r1, r2, depth, segs=8, bend=0.0, mat=CRY):
    cb = bmesh.new()
    bmesh.ops.create_cone(cb, cap_ends=True, cap_tris=False, segments=segs, radius1=r1, radius2=r2, depth=depth)
    off = Vector((rng.uniform(0, 50), 0, 0))
    for v in cb.verts:
        t = (v.co.z + depth / 2) / depth
        k = 1 + 0.12 * nz(v.co * 6 + off)
        v.co = Vector((v.co.x * k + bend * t * t * depth, v.co.y * k, v.co.z + depth / 2))
    return set_mat(cb, mat)


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


def tilt(rng, max_tilt, axis=None):
    if axis is None:
        axis = Vector((rng.uniform(-1, 1), rng.uniform(-1, 1), 0))
    if axis.length < 1e-3:
        axis = Vector((1, 0, 0))
    return Matrix.Rotation(rng.uniform(0, max_tilt), 4, axis.normalized())


def tilt_out(direction, angle):
    """direction（水平）の向きへ外側に倒す。"""
    axis = Vector((0, 0, 1)).cross(Vector((direction.x, direction.y, 0)))
    if axis.length < 1e-3:
        return Matrix.Identity(4)
    return Matrix.Rotation(angle, 4, axis.normalized())


def pebbles(bm, rng, n, r_in, r_out, size=(0.05, 0.1), mat=ROCK):
    """足元の小石。"""
    for k in range(n):
        a = rng.uniform(0, math.tau)
        d = rng.uniform(r_in, r_out)
        s = rng.uniform(*size)
        if mat == CRY:
            merge(bm, cube((s, s, s), bevel=0.1), T(math.cos(a) * d, math.sin(a) * d, -s * 0.2) @ tilt(rng, 0.6) @ RZ(rng.uniform(0, 3)))
        else:
            merge(bm, lumpy(rng, s, s * 0.9, s * 0.7, subdiv=2, rough=0.25), T(math.cos(a) * d, math.sin(a) * d, 0))


def make_object(name, bm, ground=-0.03):
    zmin = min(v.co.z for v in bm.verts)
    bmesh.ops.translate(bm, vec=Vector((0, 0, ground - zmin)), verts=bm.verts)
    me = bpy.data.meshes.new(name)
    for m in MATS:  # 先に枠を作る（枠がないと material_index が 0 に戻る）
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


def cut(bm, co, no, fill_mat):
    """平面で切り落として、切り口をふさぐ。"""
    geom = list(bm.verts) + list(bm.edges) + list(bm.faces)
    res = bmesh.ops.bisect_plane(bm, geom=geom, plane_co=co, plane_no=no, clear_outer=True)
    edges = [e for e in res["geom_cut"] if isinstance(e, bmesh.types.BMEdge)]
    faces = []
    if edges:
        faces = bmesh.ops.holes_fill(bm, edges=edges, sides=0)["faces"]
        for f in faces:
            f.material_index = fill_mat
    return faces


# ---------------------------------------------------------------- 10 種類
def b01_tower(rng):
    bm = bmesh.new()
    z = 0.0
    for i, s in enumerate((0.55, 0.46, 0.38, 0.3, 0.22)):
        h = s * rng.uniform(0.38, 0.5)
        c = cube((s * rng.uniform(0.95, 1.05), s * rng.uniform(0.9, 1.0), h), bevel=0.08, hopper=1 if i == 4 else 0)
        merge(bm, c, T(rng.uniform(-0.04, 0.04), rng.uniform(-0.04, 0.04), z) @ RZ(rng.uniform(-0.35, 0.35)) @ tilt(rng, 0.06))
        z += h * 0.96
    pebbles(bm, rng, 5, 0.35, 0.5, (0.05, 0.09), CRY)
    return bm


def b02_mushroom(rng):
    bm = bmesh.new()
    for (x, y, s) in ((0, 0, 1.0), (0.4, 0.2, 0.6), (-0.32, 0.26, 0.5)):
        stem_h = 0.34 * s
        merge(bm, lumpy(rng, 0.2 * s, 0.19 * s, stem_h, subdiv=3, rough=0.18, flat=0, mat=CRY), T(x, y, 0))
        merge(bm, lumpy(rng, 0.36 * s, 0.33 * s, 0.26 * s, subdiv=3, rough=0.22, flat=-0.6, mat=CRY, freq=2.0),
              T(x, y, stem_h * 0.95) @ tilt(rng, 0.25))
    return bm


def b03_one_grain(rng):
    bm = bmesh.new()
    s = 0.62
    merge(bm, cube((s, s * 0.96, s * 0.94), bevel=0.05, hopper=5), T(0, 0, -s * 0.28) @ tilt(rng, 0.0, None) @ Matrix.Rotation(0.38, 4, Vector((1, 0.4, 0)).normalized()) @ RZ(0.5) @ T(0, 0, 0))
    merge(bm, lumpy(rng, 0.55, 0.48, 0.1, subdiv=3, rough=0.25), T(0, 0, 0))
    for k in range(4):
        t = rng.uniform(0.08, 0.15)
        a = rng.uniform(0, math.tau)
        merge(bm, cube((t, t, t), bevel=0.08), T(math.cos(a) * 0.42, math.sin(a) * 0.38, 0.0) @ tilt(rng, 0.5) @ RZ(rng.uniform(0, 3)))
    return bm


def b04_hopper(rng):
    bm = bmesh.new()
    s = 0.6
    cb = bmesh.new()
    bmesh.ops.create_cube(cb, size=s)
    bmesh.ops.bevel(cb, geom=list(cb.edges), offset=s * 0.04, segments=1, affect="EDGES", profile=0.5, clamp_overlap=True)
    cb.normal_update()
    big = [f for f in cb.faces if f.calc_area() > s * s * 0.5]
    for f in big:
        hopper_face(cb, f, s, s * 0.03, 4)
    set_mat(cb, CRY)
    merge(bm, cb, T(0, 0, s * 0.32) @ Matrix.Rotation(0.5, 4, Vector((1, 1, 0)).normalized()) @ RZ(0.3))
    merge(bm, lumpy(rng, 0.5, 0.45, 0.12, subdiv=3, rough=0.25), T(0, 0, 0))
    pebbles(bm, rng, 4, 0.4, 0.55, (0.06, 0.1), CRY)
    return bm


def b05_outcrop(rng):
    bm = bmesh.new()
    x = -0.5
    while x < 0.5:
        w = rng.uniform(0.16, 0.26)
        h = 0.45 + 0.4 * math.sin((x + 0.5) * math.pi) * rng.uniform(0.75, 1.1)
        d = rng.uniform(0.3, 0.4)
        merge(bm, cube((w, d, h), bevel=0.05, hopper=1 if h > 0.7 else 0),
              T(x + w / 2, rng.uniform(-0.04, 0.04), -0.05) @ RZ(rng.uniform(-0.08, 0.08)) @ tilt(rng, 0.05))
        x += w * 0.92
    merge(bm, lumpy(rng, 0.62, 0.32, 0.1, subdiv=3, rough=0.25), T(0, 0, 0))
    return bm


def b06_pile(rng):
    bm = bmesh.new()
    rings = ((7, 0.36, 0.0, (0.2, 0.26)), (5, 0.2, 0.17, (0.18, 0.24)), (2, 0.07, 0.33, (0.17, 0.21)))
    for n, r, z, size in rings:
        for k in range(n):
            a = k / n * math.tau + rng.uniform(-0.2, 0.2)
            s = rng.uniform(*size)
            merge(bm, cube((s, s * rng.uniform(0.85, 1.1), s * rng.uniform(0.7, 0.95)), bevel=0.18),
                  T(math.cos(a) * r, math.sin(a) * r, z) @ tilt(rng, 0.45) @ RZ(rng.uniform(0, 3)))
    return bm


def b07_vein(rng):
    bm = bmesh.new()
    no = Vector((1.0, 0.0, 0.35)).normalized()   # 割れ目は立っていて、横から見える
    gap = 0.2
    seed = rng.randint(0, 9999)
    lower = lumpy(random.Random(seed), 0.5, 0.45, 0.5, subdiv=3, rough=0.18)
    cut(lower, Vector((0, 0, 0.2)), no, CRY)
    upper = lumpy(random.Random(seed), 0.5, 0.45, 0.5, subdiv=3, rough=0.18)
    cut(upper, Vector((0, 0, 0.2)), -no, CRY)
    merge(bm, lower)
    merge(bm, upper, T(*(no * gap)))
    # 割れ目に挟まった結晶
    side = Vector((0, 1, 0))
    along = no.cross(side).normalized()
    for k in range(16):
        s = rng.uniform(0.12, 0.22)
        p = Vector((0, 0, 0.2)) + no * gap * 0.5 + along * rng.uniform(-0.15, 0.55) + side * rng.uniform(-0.4, 0.4)
        merge(bm, cube((s, s, s), bevel=0.06), T(*p) @ tilt(rng, 0.7) @ RZ(rng.uniform(0, 3)) @ T(0, 0, -s / 2))
    return bm


def b08_flower(rng):
    bm = bmesh.new()
    merge(bm, lumpy(rng, 0.42, 0.42, 0.08, subdiv=3, rough=0.2), T(0, 0, 0))
    merge(bm, cube((0.22, 0.22, 0.22), bevel=0.06, hopper=1), T(0, 0, 0.02) @ RZ(0.4))
    for ring, (n, r, ang, size) in enumerate(((6, 0.17, 0.75, 0.24), (9, 0.36, 1.05, 0.2))):
        for k in range(n):
            a = k / n * math.tau + ring * 0.3 + rng.uniform(-0.1, 0.1)
            d = Vector((math.cos(a), math.sin(a), 0))
            s = size * rng.uniform(0.85, 1.1)
            m = T(d.x * r, d.y * r, 0.02) @ tilt_out(d, ang + rng.uniform(-0.1, 0.1)) @ RZ(a)
            merge(bm, cube((s, s * 0.9, s * 0.75), bevel=0.07, hopper=1 if ring == 0 else 0), m)
    return bm


def b09_spikes(rng):
    bm = bmesh.new()
    merge(bm, lumpy(rng, 0.5, 0.44, 0.12, subdiv=3, rough=0.25), T(0, 0, 0))
    spots = [(0, 0, 0.85, 0.2)] + [(0, 0, 0, 0)] * 0
    for k in range(8):
        a = rng.uniform(0, math.tau)
        d = rng.uniform(0.12, 0.38) if k else 0.0
        h = 0.85 - d * 1.2 + rng.uniform(-0.1, 0.1)
        r1 = 0.09 + h * 0.13
        dirv = Vector((math.cos(a), math.sin(a), 0))
        merge(bm, cone(rng, r1, 0.015, h, segs=7, bend=0.0), T(dirv.x * d, dirv.y * d, 0) @ tilt_out(dirv, d * 0.6))
    return bm


def b10_geode(rng):
    bm = lumpy(rng, 0.5, 0.45, 0.45, subdiv=3, rough=0.15)
    no = Vector((0, -0.75, 1.0)).normalized()   # 手前の上が割れて開いている
    faces = cut(bm, Vector((0, -0.05, 0.42)), no, CRY)
    if faces:
        f = max(faces, key=lambda x: x.calc_area())
        bmesh.ops.inset_region(bm, faces=[f], thickness=0.07, depth=0)
        c = f.calc_center_median()
        bmesh.ops.translate(bm, vec=-no * 0.28, verts=list(f.verts))
        # くぼみの中を結晶で埋める
        bottom = c - no * 0.28
        for k in range(16):
            s = rng.uniform(0.07, 0.14)
            u = Vector((1, 0, 0)) * rng.uniform(-0.25, 0.25) + no.cross(Vector((1, 0, 0))).normalized() * rng.uniform(-0.2, 0.2)
            p = bottom + u
            m = T(*p) @ no.to_track_quat("Z", "Y").to_matrix().to_4x4() @ tilt(rng, 0.5) @ RZ(rng.uniform(0, 3))
            merge(bm, cube((s, s, s * 1.2), bevel=0.06), m @ T(0, 0, -s * 0.3))
    return bm


BUILDERS = [("01_Tower", b01_tower), ("02_Mushroom", b02_mushroom), ("03_OneGrain", b03_one_grain),
            ("04_Hopper", b04_hopper), ("05_Outcrop", b05_outcrop), ("06_Pile", b06_pile),
            ("07_Vein", b07_vein), ("08_Flower", b08_flower), ("09_Spikes", b09_spikes), ("10_Geode", b10_geode)]


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
    for i, (name, fn) in enumerate(BUILDERS):
        ob = make_object("RockSaltV2_" + name, fn(random.Random(100 + i)))
        ob.location = ((i % 5) * 1.6, -(i // 5) * 1.8, 0)
        objs.append(ob)
    if EXPORT:
        for ob in objs:
            export_fbx(ob)
    return objs


built = main()
print("built", [(o.name[12:], sum(len(p.vertices) - 2 for p in o.data.polygons), tuple(round(d, 2) for d in o.dimensions)) for o in built])
