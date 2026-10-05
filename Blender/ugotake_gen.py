# ウゴタケ（雨後茸）と、それが生える倒木を作るスクリプト（Blender 5.2）
# ウゴタケ1本（摘むと手に入る、約 11 cm）と、苔の生えた短い倒木。Unity では倒木の上にウゴタケを何本か並べ、1本ずつ摘める。
# 形はポップ（ころんとしたかさ、太めの柄）、質感は Unity の WorldLook（リアル度 0.2）。
import bpy, bmesh, math, random
from mathutils import Vector, Matrix
from mathutils import noise as mnoise

EXPORT_DIR = "C:/UnityProjects/ExtractionGame/Assets/Art/Models/UgoTake/"
EXPORT = True
COLORS = {
    "UgoTake_Cap": (0.62, 0.46, 0.33),    # 雨に濡れた茶色のかさ
    "UgoTake_Stem": (0.9, 0.86, 0.76),    # クリーム色の柄
    "UgoTake_Gill": (0.78, 0.7, 0.58),    # かさの裏のひだ
    "UgoTake_Bark": (0.36, 0.27, 0.2),    # 倒木の皮
    "UgoTake_Wood": (0.66, 0.54, 0.38),   # 切り口
    "UgoTake_Moss": (0.43, 0.48, 0.30),   # 苔（ナメクジの体色）
}


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


def merge(bm, cb, mat=None):
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


def make_object(name, bm, mats, ground=0.0):
    zmin = min(v.co.z for v in bm.verts)
    bmesh.ops.translate(bm, vec=Vector((0, 0, ground - zmin)), verts=bm.verts)
    me = bpy.data.meshes.new(name)
    for m in mats:   # 先に枠を作る（枠がないと material_index が 0 に戻る）
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


# ---------------------------------------------------------------- ウゴタケ1本
MUSH_MATS = ["UgoTake_Cap", "UgoTake_Stem", "UgoTake_Gill"]
CAP, STEM, GILL = 0, 1, 2


def mushroom(rng):
    bm = bmesh.new()
    # 柄：少し太めで、根元がふくらむ
    h, r = 0.07, 0.016
    sb = bmesh.new()
    bmesh.ops.create_cone(sb, cap_ends=True, cap_tris=False, segments=10, radius1=r * 1.35, radius2=r, depth=h)
    for v in sb.verts:
        v.co.z += h / 2
        v.co.x += 0.004 * (v.co.z / h) ** 2      # 少し曲がる
    for f in sb.faces:
        f.material_index = STEM
    merge(bm, sb)
    # かさ：ころんと丸く、下は平ら。裏はひだの色
    cb = bmesh.new()
    bmesh.ops.create_uvsphere(cb, u_segments=16, v_segments=10, radius=1.0)
    off = Vector((rng.uniform(0, 50), 0, 0))
    for v in cb.verts:
        x, y, z = v.co
        k = 1 + 0.05 * nz(v.co * 3 + off)
        if z < -0.1:
            z = -0.1 + (z + 0.1) * 0.25          # 裏は平ら
        v.co = Vector((x * 0.055 * k, y * 0.055 * k, z * 0.04 * k))
    for f in cb.faces:
        f.material_index = GILL if f.calc_center_median().z < -0.002 else CAP
    merge(bm, cb, Matrix.Translation((0.004, 0, h * 0.95)) @ Matrix.Rotation(rng.uniform(-0.15, 0.15), 4, "X"))
    return bm


# ---------------------------------------------------------------- 倒木
LOG_MATS = ["UgoTake_Bark", "UgoTake_Wood", "UgoTake_Moss"]
BARK, WOOD, MOSS = 0, 1, 2


def log(rng, length=0.9, r=0.12):
    bm = bmesh.new()
    bmesh.ops.create_cone(bm, cap_ends=True, cap_tris=False, segments=12, radius1=r, radius2=r * 0.85, depth=length)
    bmesh.ops.subdivide_edges(bm, edges=[e for e in bm.edges if abs(e.verts[0].co.z - e.verts[1].co.z) > 1e-4], cuts=5)
    off = Vector((rng.uniform(0, 50), rng.uniform(0, 50), 0))
    for v in bm.verts:
        k = 1 + 0.08 * nz(v.co * 9 + off)
        v.co = Vector((v.co.x * k, v.co.y * k, v.co.z))
    bm.normal_update()
    for f in bm.faces:
        n = f.normal
        if abs(n.z) > 0.9:
            f.material_index = WOOD        # 切り口
        elif n.y > 0.35 and nz(f.calc_center_median() * 6 + off) > -0.2:
            f.material_index = MOSS        # 上に苔
        else:
            f.material_index = BARK
    # 横に寝かせる（長さは x、上が +z）
    bmesh.ops.transform(bm, matrix=Matrix.Rotation(math.radians(90), 4, "Y") @ Matrix.Rotation(math.radians(-90), 4, "Z"), verts=list(bm.verts))
    bmesh.ops.transform(bm, matrix=Matrix.Rotation(math.radians(90), 4, "X"), verts=list(bm.verts))
    return bm


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
    objs = [make_object("UgoTake_Item", mushroom(random.Random(3)), MUSH_MATS),
            make_object("UgoTake_Log", log(random.Random(5)), LOG_MATS, ground=-0.02)]
    objs[1].location = (0.4, 0, 0)
    if EXPORT:
        for ob in objs:
            export_fbx(ob)
    return objs


built = main()
print("built", [(o.name, sum(len(p.vertices) - 2 for p in o.data.polygons), tuple(round(d, 3) for d in o.dimensions)) for o in built])
