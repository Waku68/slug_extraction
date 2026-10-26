# 籠を運ぶ鳥の羽の模様のテクスチャ（灰色、色に掛ける）。Blender 5.2 で exec して PNG を書き出す。
# Oct 19 わくさん「形の羽は違和感すぎる。なくして、テクスチャにしてみて」→「小さく、うすく」→「頭はテクスチャはずして」。
#   うろこ状に重なる丸い羽：上の段の羽が下の段に重なり、丸いふちが下を向く。ふちに細く暗い線、羽の上のほうが少し暗い。
#   縦長の1枚（横 TEX_W × 縦 TEX_H）。下の BODY_TOP までが体の羽（FEATHER_ROWS 段）、そこから FADE_TOP までで白へ消え、
#   上は白（頭）。横は FEATHER_COLS 枚でつながる（Unity は横 Repeat・縦 Clamp）。白 = 地の色のまま。
#   UV は carrier_bird_gen.py の body_uvs（横は U_REP 回くり返す。縦は首の骨の重みで頭ほど白い所へ）。
import bpy, numpy as np

TEX_W, TEX_H = 512, 1024
FEATHER_COLS, FEATHER_ROWS = 4, 17      # 横の羽の数（つながる）、体の羽の段の数
BODY_TOP, FADE_TOP = 0.80, 0.92         # 羽の模様が消えはじめる所、消えきる所（縦の割合）
RIM_DARK = 0.15                          # ふちの線の暗さ（Oct 19 わくさん「うすく」0.30→0.15）
RIM_WIDTH = 0.10                         # ふちの線の太さ（羽の幅に対して）
TOP_DARK = 0.06                          # 羽の上のほう（上の羽の下に入る所）の暗さ
OUT = r"C:/UnityProjects/ExtractionGame/Assets/Art/Models/Creatures/CarrierBird/Textures/CarrierBird_FeatherScales.png"


def feather_scales():
    # 座標は横幅 1 を単位にする（縦は TEX_H / TEX_W 倍）。丸が丸のまま描ける
    H = TEX_H / TEX_W
    w = 1.0 / FEATHER_COLS
    h = BODY_TOP * H / FEATHER_ROWS
    rad = w * 0.62                                        # 羽の丸の半径（となりと少し重なる）
    ys, xs = np.mgrid[0:TEX_H, 0:TEX_W] / TEX_W           # ys は 0〜H、上向き
    val = np.ones((TEX_H, TEX_W))
    owner = np.full((TEX_H, TEX_W), -1e9)                 # いちばん上の段の羽が勝つ
    for r in range(-3, int(FADE_TOP * H / h) + 2):
        off = 0.5 * w * (r % 2)
        for c in range(-1, FEATHER_COLS + 2):
            cx, cy = c * w + off + w / 2, r * h + rad     # 丸の下のふちが段の線 r*h に来る
            dx = (xs - cx + 0.5) % 1.0 - 0.5              # 左右につながる距離
            dy = ys - cy
            d = np.sqrt(dx * dx + dy * dy)
            inside = (d < rad) & (r > owner)
            edge = (rad - d) / (w * RIM_WIDTH)            # 0 = ふち
            rim = 1.0 - RIM_DARK * np.clip(1.0 - edge, 0, 1) ** 1.5
            top = 1.0 - TOP_DARK * np.clip(dy / rad, 0, 1)   # 羽の上のほうを少し暗く
            val = np.where(inside, rim * top, val)
            owner = np.where(inside, r, owner)
    t = np.clip((ys / H - BODY_TOP) / (FADE_TOP - BODY_TOP), 0, 1)   # 上で白へ消す
    t = t * t * (3 - 2 * t)
    return val * (1 - t) + t


def save(val):
    import os
    os.makedirs(os.path.dirname(OUT), exist_ok=True)
    rgba = np.ones((TEX_H, TEX_W, 4), dtype=np.float32)
    rgba[..., 0] = rgba[..., 1] = rgba[..., 2] = val
    name = "CarrierBird_FeatherScales"
    img = bpy.data.images.get(name)
    if img is None or tuple(img.size) != (TEX_W, TEX_H):
        if img: bpy.data.images.remove(img)
        img = bpy.data.images.new(name, TEX_W, TEX_H, alpha=False)
    img.pixels.foreach_set(rgba.ravel())                  # Blender の画像は下の行から
    img.filepath_raw = OUT
    img.file_format = "PNG"
    img.save()
    print("saved", OUT, "min", round(float(val.min()), 3))


save(feather_scales())
