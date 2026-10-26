# 生き物のモデルで使う共通の形の道具（Blender 5.2）。carrier_bird_gen.py などが exec で読み込む。
# make_obj は読み込んだ側の material(name) を使う。
import bpy, bmesh, math
from mathutils import Vector, Matrix


def clear_scene():
    for ob in list(bpy.data.objects):
        bpy.data.objects.remove(ob, do_unlink=True)
    for me in list(bpy.data.meshes):
        if me.users == 0:
            bpy.data.meshes.remove(me)
    for arm in list(bpy.data.armatures):
        if arm.users == 0:
            bpy.data.armatures.remove(arm)


def make_obj(name, bm, mats, parent, pivot):
    """bm の頂点はワールド座標。pivot をオブジェクトの位置にして、頂点をそこからの相対に直す。
    mats は名前1つか名前のリスト（面の material_index の順）。"""
    if isinstance(mats, str):
        mats = [mats]
    for v in bm.verts:
        v.co -= pivot
    me = bpy.data.meshes.new(name)
    for m in mats:
        me.materials.append(material(m))
    bm.to_mesh(me); bm.free()
    for p in me.polygons:
        p.use_smooth = True
    ob = bpy.data.objects.new(name, me)
    bpy.context.scene.collection.objects.link(ob)
    ob.location = pivot
    if parent:
        bpy.context.view_layer.update()
        ob.parent = parent
        ob.matrix_parent_inverse = parent.matrix_world.inverted()
    return ob


def ellipsoid(center, radii, seg=24, ring=12, rot=None):
    bm = bmesh.new()
    bmesh.ops.create_uvsphere(bm, u_segments=seg, v_segments=ring, radius=1.0)
    for v in bm.verts:
        v.co = Vector((v.co.x * radii[0], v.co.y * radii[1], v.co.z * radii[2]))
        if rot:
            v.co = rot @ v.co
        v.co += center
    return bm


def merge(bms, mat_idx=None):
    """いくつかの bm を1つに。mat_idx を渡すと、それぞれの面に material_index を付ける。"""
    out = bmesh.new()
    for i, b in enumerate(bms):
        if mat_idx is not None:
            for f in b.faces:
                f.material_index = mat_idx[i]
        me = bpy.data.meshes.new("_tmp"); b.to_mesh(me); b.free()
        out.from_mesh(me); bpy.data.meshes.remove(me)
    return out


def loft(rings, cap=True):
    """同じ点数の輪の列（ワールド座標の Vector のリスト）から閉じた筒を作る。"""
    bm = bmesh.new(); K = len(rings[0])
    vs = [[bm.verts.new(p) for p in r] for r in rings]
    for i in range(len(vs) - 1):
        for k in range(K):
            bm.faces.new((vs[i][k], vs[i][(k + 1) % K], vs[i + 1][(k + 1) % K], vs[i + 1][k]))
    if cap:
        for r in (vs[0], vs[-1]):
            cen = bm.verts.new(sum((v.co for v in r), Vector()) / K)
            for k in range(K):
                bm.faces.new((r[k], r[(k + 1) % K], cen))
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces[:])
    return bm


def mesh_from_mods(bm, mods):
    """bm に modifier を順にかけた結果の bm を返す（bpy.ops を使わない）。"""
    me = bpy.data.meshes.new("_tmp"); bm.to_mesh(me); bm.free()
    ob = bpy.data.objects.new("_tmp", me)
    bpy.context.scene.collection.objects.link(ob)
    for kind, props in mods:
        m = ob.modifiers.new(kind, kind)
        for k, v in props.items():
            setattr(m, k, v)
    dg = bpy.context.evaluated_depsgraph_get()
    me2 = bpy.data.meshes.new_from_object(ob.evaluated_get(dg))
    bpy.data.objects.remove(ob, do_unlink=True); bpy.data.meshes.remove(me)
    out = bmesh.new(); out.from_mesh(me2); bpy.data.meshes.remove(me2)
    return out
