# 拠点「セノーテの洞窟」の細かい形の部品（Blender 5.2）
# cenote_base_gen.py と cenote_furniture_gen.py の両方から import して使う。
# Oct 18 Claude（Argon の Step 3 から引き継ぎ）：わくさんの「壁も道具も荒い。粒度を細かく」に合わせて足した。
#
# 考え方（ゲームの見た目＝Slug/Toon の Flat Facets 基準）
# - ゲームでは三角形1枚が1つの面として塗られる。細かく見せるには「三角形を細かく」「その三角形を少しずつ違う向きに傾ける」
# - 板：上面を長さ 0.06〜0.1 m・幅 4〜8 本に分け、木目の溝（深さ 4〜7 mm）、反り、欠けた端、面取りの角。裏は1枚の面
# - 石・岩の塊：角の丸い箱を 0.05〜0.1 m の網目に分け、2段の凹凸（0.2〜0.3 m の起伏と 0.05 m のざらつき）
# - 縄：短い巻き（縛り）は2〜3周のらせん。長い手すり綱は、ゆるいたるみと 0.1 m ごとの節
# - 大きさの基準：ナメクジの目の高さ 0.47 m から 1〜3 m の距離で、面が 2〜6 cm に見える細かさ

import bmesh, math
from mathutils import Vector, Matrix
from mathutils import noise as mnoise

WOOD_MATS = ("CenoteBase_Driftwood", "CenoteBase_DriftwoodDark")
ROCK_MATS = ("CenoteBase_Limestone", "CenoteBase_LimestoneWet", "CenoteBase_Moss", "CenoteBase_LavaStone",
             "CenoteBase_Coral", "CenoteBase_Obsidian", "CenoteBase_Salt", "CenoteBase_BlackSalt")


def nz(p):
    return mnoise.noise(p)


def fbm(p, octaves=3, lac=2.03, gain=0.5):
    val, amp, freq = 0.0, 0.5, 1.0
    for _ in range(octaves):
        val += amp * mnoise.noise(p * freq)
        freq *= lac
        amp *= gain
    return val


def seed_vec(center, k=1.0):
    c = Vector(center)
    return Vector((c.x * 1.37 * k + 3.1, c.y * 1.91 * k + 7.3, c.z * 2.23 * k + 1.7))


# ---------------------------------------------------------------- 網目の箱（6面それぞれを nx×ny×nz に分けた閉じた箱）
def grid_box(cb, half, n):
    hx = (half[0], half[1], half[2])
    for a in range(3):
        b, c = (a + 1) % 3, (a + 2) % 3
        for s in (1, -1):
            u_ax, v_ax = (b, c) if s > 0 else (c, b)
            nu, nv = n[u_ax], n[v_ax]
            grid = []
            for i in range(nu + 1):
                row = []
                for j in range(nv + 1):
                    p = [0.0, 0.0, 0.0]
                    p[a] = s * hx[a]
                    p[u_ax] = -hx[u_ax] + 2 * hx[u_ax] * i / nu
                    p[v_ax] = -hx[v_ax] + 2 * hx[v_ax] * j / nv
                    row.append(cb.verts.new(Vector(p)))
                grid.append(row)
            for i in range(nu):
                for j in range(nv):
                    cb.faces.new((grid[i][j], grid[i + 1][j], grid[i + 1][j + 1], grid[i][j + 1]))
    bmesh.ops.remove_doubles(cb, verts=list(cb.verts), dist=1e-6)
    bmesh.ops.recalc_face_normals(cb, faces=list(cb.faces))


def round_box_verts(cb, half, r):
    """箱の角を半径 r で丸める（内側の箱からの距離を r にそろえる）"""
    inner = [max(1e-4, h - r) for h in half]
    for v in cb.verts:
        q = Vector((max(-inner[0], min(inner[0], v.co.x)),
                    max(-inner[1], min(inner[1], v.co.y)),
                    max(-inner[2], min(inner[2], v.co.z))))
        d = v.co - q
        if d.length > 1e-7:
            v.co = q + d.normalized() * r


def steps_for(length, step, lo=1, hi=40):
    return max(lo, min(hi, int(round(length / step))))


# ---------------------------------------------------------------- 石・岩の塊（角の丸い箱＋2段の凹凸）
def rock_cb(size, seed, step=0.07, round_k=0.42, rough=0.07, fine=0.025, flat_bottom=False):
    """size=(w,d,h)。rough は起伏の大きさ（短い辺に対する割合）、fine は細かいざらつき（m）"""
    w, d, h = size
    cb = bmesh.new()
    n = (steps_for(w, step, 2, 24), steps_for(d, step, 2, 24), steps_for(h, step, 2, 12))
    half = (w * 0.5, d * 0.5, h * 0.5)
    grid_box(cb, half, n)
    r = min(half) * round_k * 2.0
    r = min(r, min(half) * 0.98)
    round_box_verts(cb, half, r)
    s = Vector(seed)
    m = min(w, d, h)
    for v in cb.verts:
        p = v.co
        nrm = Vector((p.x / max(1e-4, half[0]) ** 2, p.y / max(1e-4, half[1]) ** 2, p.z / max(1e-4, half[2]) ** 2))
        if nrm.length < 1e-6:
            continue
        nrm.normalize()
        k = fbm(p * (2.2 / max(0.25, m)) + s, 2) * rough * m + nz(p * 14.0 + s * 1.3) * fine
        if flat_bottom and p.z < -half[2] * 0.6:
            k *= 0.3
        v.co = p + nrm * k
    return cb


# ---------------------------------------------------------------- 流木の板（上面の網目に木目の溝・反り・欠けた端・面取り、裏は1枚）
def plank_cb(L, W, T, seed, step=0.08, max_cross=6, groove=0.012, cup=0.004, bend=0.010, chip=0.022, wobble=0.04):
    """x＝長さ L、y＝幅 W、z＝厚み T。原点は中心"""
    cb = bmesh.new()
    s = Vector(seed)
    c = min(T * 0.32, W * 0.14, 0.02)          # 面取りの幅
    nl = steps_for(L - 2 * c, step, 2, 40)
    nm = max(2, min(max_cross, int(round((W - 2 * c) / 0.045))))
    x0, x1 = -L * 0.5 + c, L * 0.5 - c
    y0, y1 = -W * 0.5 + c, W * 0.5 - c
    zt = T * 0.5

    def wob(x):
        return 1.0 + wobble * nz(Vector((x * 1.7, 0.0, 0.0)) + s)

    def bend_z(x):
        return bend * math.sin(x * 1.25 + s.x) + bend * 0.5 * nz(Vector((x * 0.8, 3.0, 0.0)) + s)

    def end_chip(x, y):
        # 端の欠け：両端の近くで長さ方向に削る
        e = chip * (0.5 + 0.5 * nz(Vector((y * 21.0, x * 3.0, 1.0)) + s))
        if x > 0:
            return -e
        return e

    top = []
    for i in range(nl + 1):
        x = x0 + (x1 - x0) * i / nl
        row = []
        for j in range(nm + 1):
            y = (y0 + (y1 - y0) * j / nm) * wob(x)
            g = nz(Vector((x * 1.4, y * (nm / max(0.05, W)) * 1.1, 0.0)) + s)
            dz = -groove * max(0.0, min(1.0, 0.5 + 1.4 * g))
            dz -= cup * (2.0 * (y - (y0 + y1) * 0.5) / max(0.02, (y1 - y0))) ** 2
            xx = x
            if i == 0 or i == nl:
                xx = x + end_chip(x, y)
            row.append(cb.verts.new(Vector((xx, y, zt + dz))))
        top.append(row)
    for i in range(nl):
        for j in range(nm):
            cb.faces.new((top[i][j], top[i + 1][j], top[i + 1][j + 1], top[i][j + 1]))

    # 上面のふち（反時計回り）
    ring = []
    for i in range(nl + 1):
        ring.append(top[i][0])
    for j in range(1, nm + 1):
        ring.append(top[nl][j])
    for i in range(nl - 1, -1, -1):
        ring.append(top[i][nm])
    for j in range(nm - 1, 0, -1):
        ring.append(top[0][j])

    # 面取りの輪と裏の輪
    cham, bot = [], []
    for v in ring:
        co = v.co
        dirx = 1.0 if co.x > x1 - 1e-4 - chip * 1.2 else (-1.0 if co.x < x0 + 1e-4 + chip * 1.2 else 0.0)
        diry = 1.0 if co.y > (y1 * wob(co.x)) - 1e-4 else (-1.0 if co.y < (y0 * wob(co.x)) + 1e-4 else 0.0)
        d = Vector((dirx, diry, 0.0))
        if d.length < 1e-6:
            d = Vector((0.0, 1.0 if co.y > 0 else -1.0, 0.0))
        d.normalize()
        wear = 1.0 + 0.35 * nz(co * 9.0 + s)
        cc = co + d * c * wear - Vector((0, 0, c * 0.9))
        cham.append(cb.verts.new(cc))
        bot.append(cb.verts.new(Vector((cc.x, cc.y, -zt))))
    n = len(ring)
    for k in range(n):
        kn = (k + 1) % n
        cb.faces.new((ring[k], cham[k], cham[kn], ring[kn]))
        cb.faces.new((cham[k], bot[k], bot[kn], cham[kn]))
    f_bot = cb.faces.new(list(reversed(bot)))
    bmesh.ops.triangulate(cb, faces=[f_bot])
    bmesh.ops.recalc_face_normals(cb, faces=list(cb.faces))
    for v in cb.verts:
        v.co.z += bend_z(v.co.x)
    return cb


def plank_axes(size):
    """箱の寸法 (w,d,h) から、板として作るときの (長さ, 幅, 厚み) と、板の向きを箱へ戻す回転"""
    w, d, h = size
    dims = [w, d, h]
    order = sorted(range(3), key=lambda i: -dims[i])   # 長い順
    a_long, a_mid, a_thin = order
    rot = Matrix.Identity(3)
    cols = [None, None, None]
    basis = [Vector((1, 0, 0)), Vector((0, 1, 0)), Vector((0, 0, 1))]
    cols[0] = basis[a_long]
    cols[1] = basis[a_mid]
    cols[2] = basis[a_thin]
    if cols[0].cross(cols[1]).dot(cols[2]) < 0:
        cols[1] = -cols[1]
    for r_i in range(3):
        for c_i in range(3):
            rot[r_i][c_i] = cols[c_i][r_i]
    return dims[a_long], dims[a_mid], dims[a_thin], rot.to_4x4()


def is_plank_shape(size):
    s = sorted(size)
    return s[2] >= 3.0 * s[0] and s[0] <= 0.16


# ---------------------------------------------------------------- 角材・梁（板ほど薄くない木）：網目の箱に、長さ方向に流れる木目
def beam_cb(size, seed, step=0.08, grain=0.006):
    w, d, h = size
    L, W, T, rot = plank_axes(size)
    cb = bmesh.new()
    n = (steps_for(L, step, 2, 40), max(2, min(6, int(round(W / 0.04)))), max(2, min(5, int(round(T / 0.04)))))
    half = (L * 0.5, W * 0.5, T * 0.5)
    grid_box(cb, half, n)
    round_box_verts(cb, half, min(W, T) * 0.22)
    s = Vector(seed)
    for v in cb.verts:
        p = v.co
        rad = Vector((0.0, p.y, p.z))
        if rad.length < 1e-6:
            continue
        g = nz(Vector((p.x * 1.2, p.y * 16.0, p.z * 16.0)) + s)
        k = -grain * max(0.0, min(1.0, 0.5 + 1.3 * g)) + 0.004 * nz(p * 6.0 + s)
        v.co = p + rad.normalized() * k
        v.co.z += 0.006 * math.sin(p.x * 1.4 + s.x)
    bmesh.ops.transform(cb, matrix=rot, verts=list(cb.verts))
    return cb


# ---------------------------------------------------------------- 丸太・柱（円柱に節と縦の木目、両端の面取り）
def log_cb(r_bot, r_top, height, seed, segs=None, ring_step=0.10, grain=0.10, knots=True, cap_bevel=0.25):
    """z が軸。原点は中心。grain は半径に対する木目の溝の深さの割合"""
    cb = bmesh.new()
    s = Vector(seed)
    r_max = max(r_bot, r_top)
    if segs is None:
        segs = max(8, min(16, int(round(math.tau * r_max / 0.04))))
    nr = steps_for(height, max(ring_step, height / 14.0), 2, 40)
    cbv = min(r_max * cap_bevel, height * 0.2)
    rings = []
    zs = [-height * 0.5 + cbv * 0.0]
    for i in range(nr + 1):
        zs.append(-height * 0.5 + cbv + (height - 2 * cbv) * i / nr)
    zs.append(height * 0.5)
    for iz, z in enumerate(zs):
        t = (z + height * 0.5) / height
        r = r_bot + (r_top - r_bot) * t
        if iz == 0 or iz == len(zs) - 1:
            r -= cbv * 0.8
        ring = []
        for k in range(segs):
            a = k / segs * math.tau
            g = nz(Vector((math.cos(a) * 3.0, math.sin(a) * 3.0, z * 1.6)) + s)
            rr = r * (1.0 - grain * max(0.0, min(1.0, 0.5 + 1.5 * g)) * 0.5)
            rr *= 1.0 + 0.05 * nz(Vector((math.cos(a), math.sin(a), z * 2.5)) + s * 0.7)
            ring.append(cb.verts.new(Vector((math.cos(a) * rr, math.sin(a) * rr, z))))
        rings.append(ring)
    for i in range(len(rings) - 1):
        la, lb = rings[i], rings[i + 1]
        for k in range(segs):
            kn = (k + 1) % segs
            cb.faces.new((la[k], la[kn], lb[kn], lb[k]))
    cb.faces.new(list(reversed(rings[0])))
    cb.faces.new(rings[-1])
    if knots and height > 0.35:
        n_k = int(height / 0.45)
        for kk in range(n_k):
            z_k = -height * 0.4 + height * 0.8 * (kk + 0.5) / max(1, n_k)
            a_k = (nz(s + Vector((kk, 0, 0))) + 1.0) * math.pi
            for v in cb.verts:
                dz = v.co.z - z_k
                if abs(dz) > 0.08:
                    continue
                a = math.atan2(v.co.y, v.co.x)
                da = abs((a - a_k + math.pi) % math.tau - math.pi)
                if da < 0.5:
                    bump = 0.012 * (1.0 - da / 0.5) * (1.0 - abs(dz) / 0.08)
                    rad = Vector((v.co.x, v.co.y, 0.0))
                    if rad.length > 1e-6:
                        v.co += rad.normalized() * bump
    bmesh.ops.recalc_face_normals(cb, faces=list(cb.faces))
    return cb


# ---------------------------------------------------------------- 縛り（つる・ヤシ縄を2〜3周巻いたらせん）
def lashing_cb(center, axis, radius, width=0.05, turns=2.5, cord=0.011, samples_per_turn=7, segs=4):
    """軸 axis のまわりに半径 radius で巻く。width は巻いた幅"""
    axis = Vector(axis).normalized()
    up = Vector((0, 0, 1)) if abs(axis.z) < 0.9 else Vector((1, 0, 0))
    bx = axis.cross(up).normalized()
    by = axis.cross(bx).normalized()
    n = max(4, int(turns * samples_per_turn))
    pts = []
    for i in range(n + 1):
        t = i / n
        a = t * turns * math.tau
        p = Vector(center) + (bx * math.cos(a) + by * math.sin(a)) * (radius + cord * 0.8) + axis * (t - 0.5) * width
        pts.append(p)
    return tube_cb(pts, [cord] * len(pts), segs=segs, cap=True)


def tube_cb(points, radii, segs=6, cap=True, rib=0.0, rib_freq=3, rib_twist=0.0, lumpy=0.0, seed=(0, 0, 0)):
    """点列に沿った管。rib はらせんの撚り目（半径に対する割合）"""
    cb = bmesh.new()
    n = len(points)
    s = Vector(seed)
    loops = []
    prev_bx = None
    arclen = 0.0
    for i, (pt, r) in enumerate(zip(points, radii)):
        if i > 0:
            arclen += (points[i] - points[i - 1]).length
        if i == 0:
            tang = (points[1] - points[0]).normalized()
        elif i == n - 1:
            tang = (points[-1] - points[-2]).normalized()
        else:
            tang = (points[i + 1] - points[i - 1]).normalized()
        if prev_bx is None:
            up = Vector((0, 0, 1)) if abs(tang.z) < 0.9 else Vector((1, 0, 0))
            bx = tang.cross(up).normalized()
        else:
            bx = (prev_bx - tang * prev_bx.dot(tang))
            if bx.length < 1e-6:
                up = Vector((0, 0, 1)) if abs(tang.z) < 0.9 else Vector((1, 0, 0))
                bx = tang.cross(up)
            bx.normalize()
        prev_bx = bx
        by = tang.cross(bx).normalized()
        ring = []
        for k in range(segs):
            a = k / segs * math.tau
            rm = r * (1.0 + rib * math.cos(rib_freq * a - arclen * rib_twist) + lumpy * nz(pt * 9.0 + s))
            ring.append(cb.verts.new(pt + (bx * math.cos(a) + by * math.sin(a)) * rm))
        loops.append(ring)
    for i in range(n - 1):
        la, lb = loops[i], loops[i + 1]
        for k in range(segs):
            kn = (k + 1) % segs
            cb.faces.new((la[k], la[kn], lb[kn], lb[k]))
    if cap and n >= 2:
        cb.faces.new(list(reversed(loops[0])))
        cb.faces.new(loops[-1])
    bmesh.ops.recalc_face_normals(cb, faces=list(cb.faces))
    return cb


def sag_points(p0, p1, sag=0.03, step=0.08, wobble=0.004, seed=(0, 0, 0)):
    """2点の間にたるむ綱の点列"""
    p0, p1 = Vector(p0), Vector(p1)
    L = (p1 - p0).length
    n = steps_for(L, step, 2, 80)
    s = Vector(seed)
    pts = []
    for i in range(n + 1):
        t = i / n
        p = p0.lerp(p1, t)
        p.z -= sag * 4.0 * t * (1.0 - t)
        p += Vector((nz(p * 7.0 + s), nz(p * 7.0 + s + Vector((3, 1, 2))), nz(p * 7.0 + s + Vector((5, 4, 1))))) * wobble
        pts.append(p)
    return pts


def merge_into(bm, cb, xform=None, mat_id=None):
    if xform is not None:
        bmesh.ops.transform(cb, matrix=xform, verts=list(cb.verts))
    if mat_id is not None:
        for f in cb.faces:
            f.material_index = mat_id
    import bpy
    tmp = bpy.data.meshes.new("_tmp_detail")
    cb.to_mesh(tmp)
    cb.free()
    bm.from_mesh(tmp)
    bpy.data.meshes.remove(tmp)
