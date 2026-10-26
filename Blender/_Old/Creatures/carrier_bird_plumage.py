# 籠を運ぶ鳥の羽毛（形で出す羽の重なり）。carrier_bird_gen.py が exec で読み込み、頭と体の形に足す。
# Oct 19 わくさん「羽毛の質感がない」→「攻め：形で羽を重ねる」。
#   丸い羽の小片（うろこ状）を、胸・背中・頭に段々に並べて貼る。先は少し浮かせて、輪郭線で1枚ずつ見える。
#   向き：体は下と後ろへ流れる、頭は後ろへ流れる。体の羽 長さ 0.30 × 幅 0.27、頭 0.18 × 0.15（松かさに見えないよう大きく少なく）。
#   よける所：おなかの下、頭の前の下（くちばし・のど袋）、目のまわり。三角形は約 3,500 まで。
#   BODY_C・BODY_R・HEAD_C・HEAD_R・on_body・Vector などは gen 側のものを使う。
PLUME_BODY = (0.30, 0.27)     # 体の羽 長さ・幅
PLUME_HEAD = (0.18, 0.15)     # 頭の羽
PLUME_LIFT = 0.028            # 羽の先の浮き


def plume(bm, p, n, flow, L, W):
    """p に、法線 n、流れの向き flow で、長さ L・幅 W の丸い羽を1枚足す（閉じた薄い形）。"""
    n = n.normalized()
    d = flow - n * flow.dot(n)
    if d.length < 1e-4:
        d = Vector((0, -1, 0)) - n * n.y
    d.normalize()
    side = n.cross(d)
    K = 6
    c = p + d * L * 0.3                      # 見える部分は流れの下手
    top, bot = [], []
    for k in range(K):
        a = 2 * math.pi * k / K
        u, w = math.cos(a) * L / 2, math.sin(a) * W / 2
        if u > 0:                            # 先は少し丸くとがらせる（しずく）
            w *= 1.0 - 0.25 * (u / (L / 2)) ** 2
        lift = 0.012 + PLUME_LIFT * max(u / (L / 2) + 0.4, 0) / 1.4
        q = c + d * u + side * w
        top.append(bm.verts.new(q + n * lift))
        bot.append(bm.verts.new(q - n * 0.02))
    ct = bm.verts.new(c + n * (0.012 + PLUME_LIFT * 0.45))
    cb = bm.verts.new(c - n * 0.025)
    for k in range(K):
        j = (k + 1) % K
        bm.faces.new((top[k], top[j], ct))
        bm.faces.new((bot[j], bot[k], cb))
        bm.faces.new((top[j], top[k], bot[k], bot[j]))


def build_plumage(eyes):
    """胸・背中・頭の羽を1つの bm に（ワールド座標）。eyes は目の位置のリスト（よける）。"""
    bm = bmesh.new()
    # 体：楕円の角度で段に並べ、1段ごとに半分ずらす
    el = -12.0; row = 0
    while el <= 84:
        ce = math.cos(math.radians(el))
        n_az = max(int(2 * math.pi * ce * 0.68 / (PLUME_BODY[1] * 0.85)), 6)
        for i in range(n_az):
            az = 2 * math.pi * (i + 0.5 * (row % 2)) / n_az
            q = Vector((ce * math.sin(az), ce * math.cos(az), math.sin(math.radians(el))))
            p = on_body(BODY_C + Vector((q.x * BODY_R.x, q.y * BODY_R.y, q.z * BODY_R.z)), 0.0)
            if (p - HEAD_C).length < HEAD_R * 0.98:
                continue                      # 頭に埋まる所
            nrm = Vector((q.x / BODY_R.x, q.y / BODY_R.y, q.z / BODY_R.z))
            flow = Vector((0, -0.75, -0.65)) if q.y < 0.3 else Vector((0, -0.25, -1))
            k = 0.8 if el > 60 else 1.0       # 上（首のまわり）は少し小さく
            plume(bm, p, nrm, flow, PLUME_BODY[0] * k, PLUME_BODY[1] * k)
        el += 14.0; row += 1
    # 頭：後ろへ流れる。前の下（くちばし・のど袋）と目のまわりはよける
    el = -5.0; row = 0
    while el <= 80:
        ce = math.cos(math.radians(el))
        n_az = max(int(2 * math.pi * ce * HEAD_R / (PLUME_HEAD[1] * 0.85)), 5)
        for i in range(n_az):
            az = 2 * math.pi * (i + 0.5 * (row % 2)) / n_az
            q = Vector((ce * math.sin(az), ce * math.cos(az), math.sin(math.radians(el))))
            if q.y > 0.35 and el < 40:
                continue
            p = HEAD_C + Vector((q.x * HEAD_R, q.y * HEAD_R, q.z * HEAD_R * 0.95))
            if any((p - e).length < 0.16 for e in eyes):
                continue
            plume(bm, p, q, Vector((0, -1, -0.35)), *PLUME_HEAD)
        el += 18.0; row += 1
    return bm


def eye_points():
    """build_eyes と同じ目の位置。"""
    pts = []
    for s in (1, -1):
        d = Vector((0.72 * s, 0.5, 0.45)).normalized()
        pts.append(HEAD_C + d * HEAD_R)
    return pts
