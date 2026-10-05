# 雨カブを作るスクリプト（Blender 5.2）
# 採る場所（土に植わっていて、葉とカブの頭がのぞく）2種と、抜いた雨カブ1個（約 15 cm）。
# 形はポップ（ころんと丸いカブ、細い根、へら形の葉）、質感は Unity の WorldLook（リアル度 0.8）。
# 色：上が彩度低めの青、下が白。
# 書き出すメッシュは滑らかな法線（Slug/Toon の Flat Facets＋輪郭線）。葉は薄い厚みを持たせて閉じた形にする（輪郭線が出るように）。
import bpy, bmesh, math, random
from mathutils import Vector, Matrix
from mathutils import noise as mnoise

EXPORT_DIR = "C:/UnityProjects/ExtractionGame/Assets/Art/Models/RainTurnip/"
EXPORT = True
NODE_SCALE = 1.8   # 畑に植わっているときは大きく見せる（抜いて手に持つと今の大きさ）

COLORS = {
    "RainTurnip_Root": (0.92, 0.9, 0.86),   # 白い下側
    "RainTurnip_Top": (0.56, 0.63, 0.76),   # 彩度低めの青の上側
    "RainTurnip_Leaf": (0.45, 0.6, 0.33),   # みずみずしい緑（ナメクジの苔色より明るい）
    "RainTurnip_Soil": (0.3, 0.24, 0.18),
}
MATS = list(COLORS.keys())
ROOT, TOP, LEAF, SOIL = range(4)


def get_mat(name):
    m = bpy.data.materials.get(name)
    if m is None:
        m = bpy.data.materials.new(name)
        nt = m.node_tree
        nt.nodes.clear()
        out = nt.nodes.new("ShaderNodeOutputMaterial")
        b = nt.nodes.new("ShaderNodeBsdfPrincipled")
        b.inputs["Base Color"].default_value = (*COLORS[name], 1)
        nt.links.new(b.outputs["BSDF"], out.inputs["Surface"])
    m.diffuse_color = (*COLORS[name], 1)
    return m


def clear_scene():
    for o in list(bpy.data.objects):
        bpy.data.objects.remove(o, do_unlink=True)
    for m in list(bpy.data.meshes):
        bpy.data.meshes.remove(m)


def nz(p):
    return mnoise.noise(p)


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


# ---------------------------------------------------------------- カブ
def bulb(rng, radius):
    """ころんと丸いカブ。底は細い根に伸びる。原点は玉の真ん中、上が +z。上の約4割が青。"""
    cb = bmesh.new()
    bmesh.ops.create_uvsphere(cb, u_segments=14, v_segments=10, radius=1.0)
    off = Vector((rng.uniform(0, 50), rng.uniform(0, 50), 0))
    for v in cb.verts:
        x, y, z = v.co
        r = 1.0 + 0.05 * nz(v.co * 2 + off)
        if z > 0.75:            # 上は少し平らで、葉の付け根がくぼむ
            z = 0.75 + (z - 0.75) * 0.4
        if z < -0.35:           # 下は細い根へ
            t = (-0.35 - z) / 0.65
            k = 1 - 0.88 * t ** 0.7
            x, y = x * k, y * k
            z = z - 1.1 * t ** 1.6
        v.co = Vector((x * r, y * r, z * 0.92)) * radius
    for f in cb.faces:
        c = f.calc_center_median()
        a = math.atan2(c.y, c.x)
        edge = radius * (0.12 + 0.08 * math.sin(a * 3 + off.x))  # 境目は少し波打つ
        f.material_index = TOP if c.z > edge else ROOT
    return cb


# ---------------------------------------------------------------- 葉
def leaf(rng, length, width, droop, base_angle=math.radians(40)):
    """へら形の葉（茎つき）。付け根が原点、+y へ伸びて上へ反り、先が垂れる。薄い厚みつき。"""
    lb = bmesh.new()
    nu, nv = 8, 4
    grid = []
    stem = 0.38
    for i in range(nu + 1):
        u = i / nu
        if u < stem:
            w = width * 0.08
        else:
            s = (u - stem) / (1 - stem)
            w = width * math.sin(math.pi * min(1.0, s ** 0.75)) * (1.0 if s < 0.95 else 0.6)
            w = max(w, width * 0.05)
        row = []
        for j in range(nv + 1):
            vv = j / nv * 2 - 1
            fold = abs(vv) * w * 0.35        # 真ん中の筋で少し折れる
            row.append(lb.verts.new((vv * w, u * length, fold)))
        grid.append(row)
    for i in range(nu):
        for j in range(nv):
            lb.faces.new((grid[i][j], grid[i][j + 1], grid[i + 1][j + 1], grid[i + 1][j]))
    # 反って垂れる：中心線の傾きは付け根で base_angle 上向き、先へ行くほど垂れる
    def angle(u):
        return base_angle - droop * u * u
    steps = 40
    line = [Vector((0, 0, 0))]
    for k in range(steps):
        a = angle((k + 0.5) / steps)
        line.append(line[-1] + Vector((0, math.cos(a), math.sin(a))) * (length / steps))
    for v in lb.verts:
        u = max(0.0, min(1.0, v.co.y / length))
        c = line[int(round(u * steps))]
        a = angle(u)
        nrm = Vector((0, -math.sin(a), math.cos(a)))   # 中心線に垂直（葉の表の向き）
        v.co = c + Vector((v.co.x, 0, 0)) + nrm * v.co.z
    bmesh.ops.solidify(lb, geom=list(lb.faces), thickness=0.004)
    for f in lb.faces:
        f.material_index = LEAF
    return lb


def leaves(bm, rng, n, at, length, droop):
    for k in range(n):
        yaw = k / n * math.tau + rng.uniform(-0.3, 0.3)
        lb = leaf(rng, length * rng.uniform(0.85, 1.1), length * rng.uniform(0.22, 0.28), droop * rng.uniform(0.8, 1.2),
                  math.radians(rng.uniform(32, 50)))
        m = Matrix.Translation(at) @ Matrix.Rotation(yaw, 4, "Z")
        merge(bm, lb, m)


def soil_mound(rng, radius):
    sb = bmesh.new()
    bmesh.ops.create_icosphere(sb, subdivisions=3, radius=1.0)
    off = Vector((rng.uniform(0, 50), rng.uniform(0, 50), 0))
    for v in sb.verts:
        p = v.co
        k = 1 + 0.18 * nz(p * 2.5 + off)
        z = max(p.z, -0.1) * 0.22
        v.co = Vector((p.x * radius * k, p.y * radius * k, z * radius * k))
    for f in sb.faces:
        f.material_index = SOIL
    return sb


# ---------------------------------------------------------------- 組み立て
def make_object(name, bm, ground=0.0, keep_height=False):
    if not keep_height:  # 置く物は、いちばん下を地面に
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


def build_node(name, seed):
    """畑に植わった雨カブ：土の小山から、カブの頭（上4割くらい）と葉がのぞく。"""
    rng = random.Random(seed)
    bm = bmesh.new()
    merge(bm, soil_mound(rng, rng.uniform(0.15, 0.18)))
    R = rng.uniform(0.07, 0.08)
    merge(bm, bulb(rng, R), Matrix.Translation((0, 0, R * 0.2)) @ Matrix.Rotation(rng.uniform(-0.15, 0.15), 4, "X"))
    leaves(bm, rng, rng.randint(5, 7), Vector((0, 0, R * 0.2 + R * 0.72)), rng.uniform(0.17, 0.21), math.radians(rng.uniform(45, 65)))
    bmesh.ops.scale(bm, vec=Vector((NODE_SCALE, NODE_SCALE, NODE_SCALE)), verts=bm.verts)
    # 土の底が地面（z=0）より少し下。根は地面の下に埋まる
    return make_object(name, bm, keep_height=True)


def build_item(name, seed):
    """抜いた雨カブ1個：玉と根と、少ししなった葉。全体で約 15 cm。"""
    rng = random.Random(seed)
    bm = bmesh.new()
    R = 0.055
    merge(bm, bulb(rng, R))
    leaves(bm, rng, 5, Vector((0, 0, R * 0.7)), 0.1, math.radians(40))
    return make_object(name, bm, ground=0.0)


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
    objs = [build_node("RainTurnip_Node_A", 3), build_node("RainTurnip_Node_B", 8), build_item("RainTurnip_Item", 5)]
    for i, ob in enumerate(objs):
        ob.location = (i * 0.5, 0, 0)
    if EXPORT:
        for ob in objs:
            export_fbx(ob)
    return objs


built = main()
print("built", [(o.name, sum(len(p.vertices) - 2 for p in o.data.polygons), tuple(round(d, 3) for d in o.dimensions)) for o in built])
