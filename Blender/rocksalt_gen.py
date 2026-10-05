# 岩塩を作るスクリプト（Blender 5.2）
# 採る場所の塊（Node）と、叩くと出てくる約 20 cm の欠片（Chunk）を作る。
# 岩塩は立方体の結晶なので、角を落とした箱を基本にし、大きい結晶の上面は階段状にくぼませる（ホッパー結晶）。
# Unity ではナメクジと同じ Slug/Toon（Flat Facets＋輪郭線）で描く。輪郭線は法線方向に押し出すので、
# 書き出すメッシュは滑らかな法線にしておき、カクカクした陰影はシェーダー側で出す。
# 使い方：Blender の MCP から実行するか、Blender の Scripting タブで開いて実行。
import bpy, bmesh, math, random
from mathutils import Vector, Matrix, Euler

EXPORT_DIR = "C:/UnityProjects/ExtractionGame/Assets/Art/Models/RockSalt/"
EXPORT = True

# ---------------------------------------------------------------- 色（Blender のプレビュー用。Unity 側はマテリアルで決める）
SALT = (0.89, 0.71, 0.68)      # くすんだピンク
SALT_SHADE = (0.72, 0.6, 0.78) # 影は少し紫へ（ナメクジのシェーダーの既定と同じ）
ROCK = (0.36, 0.34, 0.29)      # 暗い灰色
MOSS = (0.43, 0.48, 0.30)      # ナメクジの体色 #6E7A4C


def clear_scene():
    for o in list(bpy.data.objects):
        bpy.data.objects.remove(o, do_unlink=True)
    for m in list(bpy.data.meshes):
        bpy.data.meshes.remove(m)


# ---------------------------------------------------------------- 形
def crystal(size, mat_index, hopper=0, bevel=0.08):
    """角を落とした箱を別の bmesh で作る。hopper=段の数（上面を階段状にくぼませる）。底は z=0。"""
    w, d, h = size
    cb = bmesh.new()
    bmesh.ops.create_cube(cb, size=1.0)
    for v in cb.verts:
        v.co = Vector((v.co.x * w, v.co.y * d, v.co.z * h + h / 2))
    bmesh.ops.bevel(cb, geom=list(cb.edges), offset=min(w, d, h) * bevel, segments=1,
                    affect="EDGES", profile=0.5, clamp_overlap=True)
    cb.normal_update()
    if hopper:
        top = max(cb.faces, key=lambda f: f.normal.z)
        span = min(w, d)
        for i in range(hopper):
            bmesh.ops.inset_individual(cb, faces=[top], thickness=span * (0.13 if i == 0 else 0.1), depth=0)
            bmesh.ops.translate(cb, vec=Vector((0, 0, -h * 0.09)), verts=list(top.verts))
    for f in cb.faces:
        f.material_index = mat_index
    return cb


def merge(bm, cb, mat=None, mat_index=1):
    """cb を変換してから bm に足す（cb は解放する）。メッシュ経由だと番号が落ちるので付け直す。"""
    if mat is not None:
        bmesh.ops.transform(cb, matrix=mat, verts=list(cb.verts))
    tmp = bpy.data.meshes.new("_tmp")  # 枠なしなので番号は落ちる。下で付け直す
    cb.to_mesh(tmp); cb.free()
    n0 = len(bm.faces)
    bm.from_mesh(tmp)
    bm.faces.ensure_lookup_table()
    for f in bm.faces[n0:]:
        f.material_index = mat_index
    bpy.data.meshes.remove(tmp)


def make_mesh_object(name, bm):
    me = bpy.data.meshes.new(name)
    # 先に枠を作っておく（枠がないメッシュに書くと material_index が 0 に戻される）
    me.materials.append(toon_material("RockSalt_Rock", ROCK, mottle=MOSS))
    me.materials.append(toon_material("RockSalt_Crystal", SALT))
    bm.normal_update()
    bm.to_mesh(me)
    bm.free()
    ob = bpy.data.objects.new(name, me)
    bpy.context.scene.collection.objects.link(ob)
    return ob


def rock_base(bm, rng, rx, ry, rz):
    """平たくてごつごつした土台の岩。material 0。"""
    geom = bmesh.ops.create_icosphere(bm, subdivisions=2, radius=1.0)
    vs = geom["verts"]
    for v in vs:
        j = Vector((rng.uniform(-1, 1), rng.uniform(-1, 1), rng.uniform(-1, 1))) * 0.12
        p = v.co + j
        p = Vector((p.x * rx, p.y * ry, p.z * rz))
        if p.z < -0.04:
            p.z = -0.04 - (p.z + 0.04) * 0.15  # 底は平らに（地面に埋める分を少し残す）
        v.co = p
    for f in {f for v in vs for f in v.link_faces}:
        f.material_index = 0


def build_node(name, seed):
    rng = random.Random(seed)
    bm = bmesh.new()
    rx, ry, rz = rng.uniform(0.45, 0.55), rng.uniform(0.38, 0.45), rng.uniform(0.12, 0.16)
    rock_base(bm, rng, rx, ry, rz)
    n_main, n_small = rng.randint(5, 7), rng.randint(3, 5)
    for i in range(n_main + n_small):
        if i == 0:  # 真ん中の大きい柱
            s = rng.uniform(0.26, 0.32); h = rng.uniform(0.55, 0.7)
            r, ang = rng.uniform(0.0, 0.05), rng.uniform(0, math.tau)
        elif i < n_main:  # まわりの結晶
            s = rng.uniform(0.14, 0.24); h = s * rng.uniform(1.0, 2.0)
            ang = i / n_main * math.tau + rng.uniform(-0.35, 0.35)
            r = rng.uniform(0.16, 0.3)
        else:  # ふちの小さい粒
            s = rng.uniform(0.06, 0.1); h = s * rng.uniform(0.9, 1.4)
            ang = rng.uniform(0, math.tau)
            r = rng.uniform(0.3, 0.42)
        w, d = s * rng.uniform(0.85, 1.1), s * rng.uniform(0.85, 1.1)
        hop = 2 if s > 0.25 else (1 if s > 0.18 else 0)
        cb = crystal((w, d, h), 1, hopper=hop)
        tilt = r * rng.uniform(1.6, 2.4)  # 外側ほど外へ傾く（中心から放射状）
        out = Vector((math.cos(ang), math.sin(ang), 0))
        axis = Vector((0, 0, 1)).cross(out).normalized() if r > 1e-3 else Vector((1, 0, 0))
        e = (out.x * r / rx) ** 2 + (out.y * r / ry) ** 2
        zsurf = rz * math.sqrt(max(0.0, 1.0 - e)) * 0.85  # 岩の表面の高さ
        m = (Matrix.Translation((out.x * r, out.y * r, zsurf - h * 0.18))
             @ Matrix.Rotation(tilt, 4, axis)
             @ Matrix.Rotation(rng.uniform(0, math.pi / 2), 4, "Z"))
        merge(bm, cb, m)
    return make_mesh_object(name, bm)


def build_chunk(name, seed):
    """叩くと出てくる欠片。約 20 cm の結晶を斜めに2回割った形。全部 material 1。"""
    rng = random.Random(seed)
    s = 0.19  # 仕様：採ると 20 cm くらいの岩塩が出る
    bm = crystal((s * rng.uniform(0.95, 1.15), s * rng.uniform(0.85, 1.0), s * rng.uniform(0.8, 1.05)), 1,
                 hopper=1 if rng.random() < 0.5 else 0, bevel=0.07)
    bmesh.ops.translate(bm, vec=Vector((0, 0, -s * 0.45)), verts=bm.verts)
    for k in range(2):  # 割れ口
        no = Vector((rng.uniform(-1, 1), rng.uniform(-1, 1), rng.uniform(-0.6, 1))).normalized()
        co = no * s * rng.uniform(0.22, 0.32)
        geom = list(bm.verts) + list(bm.edges) + list(bm.faces)
        res = bmesh.ops.bisect_plane(bm, geom=geom, plane_co=co, plane_no=no, clear_outer=True)
        cut = [e for e in res["geom_cut"] if isinstance(e, bmesh.types.BMEdge)]
        if cut:
            new = bmesh.ops.holes_fill(bm, edges=cut, sides=0)
            for f in new["faces"]:
                f.material_index = 1
    # 置いたとき転がり出さないよう、いちばん下を z=0 に
    zmin = min(v.co.z for v in bm.verts)
    bmesh.ops.translate(bm, vec=Vector((0, 0, -zmin)), verts=bm.verts)
    bmesh.ops.remove_doubles(bm, verts=bm.verts, dist=1e-5)
    return make_mesh_object(name, bm)


# ---------------------------------------------------------------- プレビュー用のトゥーン（Blender だけ）
def toon_material(name, color, mottle=None):
    m = bpy.data.materials.get(name) or bpy.data.materials.new(name)
    nt = m.node_tree
    nt.nodes.clear()
    out = nt.nodes.new("ShaderNodeOutputMaterial")
    diff = nt.nodes.new("ShaderNodeBsdfDiffuse")
    s2r = nt.nodes.new("ShaderNodeShaderToRGB")
    ramp = nt.nodes.new("ShaderNodeValToRGB")
    ramp.color_ramp.interpolation = "CONSTANT"
    e = ramp.color_ramp.elements
    e[0].position, e[0].color = 0.0, (*SALT_SHADE, 1)
    e[1].position, e[1].color = 0.25, (0.88, 0.85, 0.9, 1)
    e.new(0.6).color = (1, 1, 1, 1)
    mul = nt.nodes.new("ShaderNodeMix"); mul.data_type = "RGBA"; mul.blend_type = "MULTIPLY"
    mul.inputs[0].default_value = 1.0
    nt.links.new(diff.outputs[0], s2r.inputs[0])
    nt.links.new(s2r.outputs["Color"], ramp.inputs["Fac"])
    nt.links.new(ramp.outputs["Color"], mul.inputs[7])
    base = (*color, 1)
    if mottle:
        tc = nt.nodes.new("ShaderNodeTexCoord")
        noi = nt.nodes.new("ShaderNodeTexNoise"); noi.inputs["Scale"].default_value = 6
        cr = nt.nodes.new("ShaderNodeValToRGB"); cr.color_ramp.interpolation = "CONSTANT"
        cr.color_ramp.elements[0].color = base
        cr.color_ramp.elements[1].position = 0.56; cr.color_ramp.elements[1].color = (*mottle, 1)
        nt.links.new(tc.outputs["Object"], noi.inputs["Vector"])
        nt.links.new(noi.outputs["Fac"], cr.inputs["Fac"])
        nt.links.new(cr.outputs["Color"], mul.inputs[6])
    else:
        mul.inputs[6].default_value = base
    em = nt.nodes.new("ShaderNodeEmission")
    nt.links.new(mul.outputs[2], em.inputs["Color"])
    nt.links.new(em.outputs[0], out.inputs["Surface"])
    return m


def outline_material(name, color):
    m = bpy.data.materials.get(name) or bpy.data.materials.new(name)
    nt = m.node_tree
    nt.nodes.clear()
    out = nt.nodes.new("ShaderNodeOutputMaterial")
    em = nt.nodes.new("ShaderNodeEmission"); em.inputs["Color"].default_value = (*color, 1)
    nt.links.new(em.outputs[0], out.inputs["Surface"])
    m.use_backface_culling = True
    return m


def setup_materials(ob, outline=True):
    for p in ob.data.polygons:
        p.use_smooth = True  # 輪郭線を切らさないため滑らかな法線（Unity 側で Flat Facets）
    if outline:
        ob.data.materials.append(outline_material("RockSalt_Outline_Preview", (0.30, 0.18, 0.2)))
        sol = ob.modifiers.new("outline_preview", "SOLIDIFY")
        sol.thickness = -0.012; sol.use_flip_normals = True; sol.use_rim = False
        sol.material_offset = 2; sol.show_render = True


# ---------------------------------------------------------------- 書き出し
def export_fbx(ob):
    import os
    os.makedirs(EXPORT_DIR, exist_ok=True)
    mod = ob.modifiers.get("outline_preview")
    if mod:
        mod.show_viewport = mod.show_render = False
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
    if mod:
        mod.show_viewport = mod.show_render = True


def smart_uv(ob):
    me = ob.data
    bm = bmesh.new(); bm.from_mesh(me)
    uv = bm.loops.layers.uv.verify()
    # 箱投影（結晶はほぼ箱なので十分）。Slug/Toon の模様は uv を使う
    for f in bm.faces:
        n = f.normal
        ax = max(range(3), key=lambda i: abs(n[i]))
        for l in f.loops:
            c = l.vert.co
            l[uv].uv = (c.y, c.z) if ax == 0 else ((c.x, c.z) if ax == 1 else (c.x, c.y))
    bm.to_mesh(me); bm.free()


def main():
    clear_scene()
    objs = []
    for i, seed in enumerate((11, 23, 37)):
        ob = build_node("RockSalt_Node_" + "ABC"[i], seed)
        ob.location = (i * 1.6, 0, 0)
        objs.append(ob)
    for i, seed in enumerate((5, 8, 13)):
        ob = build_chunk("RockSalt_Chunk_" + "ABC"[i], seed)
        ob.location = (i * 0.35 + 1.25, -1.0, 0)
        objs.append(ob)
    for ob in objs:
        smart_uv(ob)
        setup_materials(ob)
    if EXPORT:
        for ob in objs:
            export_fbx(ob)
    return objs


if __name__ == "__main__" or True:
    built = main()
    print("built", [o.name for o in built], "tris", [sum(len(p.vertices) - 2 for p in o.data.polygons) for o in built])
