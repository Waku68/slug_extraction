# 仮の効果音を波形の計算で作るスクリプト（わくさんの本物の音ができるまでのつなぎ）
# 書き出し先：Assets/Audio/Placeholder/（わくさんの音の SE/ とは分ける）。44.1 kHz、16 bit、モノラル。
# 使い方：uv run --no-project --with numpy python SoundGen/placeholder_sfx.py [音の名前 …]（名前を渡すとその音だけ）
# 音の考え方：雨の夜のナメクジの世界。硬い物（岩塩）は明るく割れる、柔らかい物（植物、体）はくぐもって湿っている。
# 銃は火薬ではなく炭酸ガスなので「パン」ではなく「プシュ」。同じ音の揺らぎは番号違い（_01、_02）で作る。
import os
import sys
import wave
import zlib
import numpy as np

SR = 44100
OUT = os.path.join(os.path.dirname(__file__), "..", "Assets", "Audio", "Placeholder")


# ---------------------------------------------------------------- 部品
def t_axis(dur):
    return np.arange(int(SR * dur)) / SR


def env(dur, attack=0.002, decay=0.1, hold=0.0):
    t = t_axis(dur)
    a = np.clip(t / max(attack, 1e-4), 0, 1)
    d = np.exp(-np.clip(t - attack - hold, 0, None) / max(decay, 1e-4))
    return a * d


def noise(dur, rng):
    return rng.uniform(-1, 1, int(SR * dur))


def biquad(x, kind, f, q=0.707, gain_db=0.0):
    """RBJ のフィルター（lowpass / highpass / bandpass）。f は固定か、サンプルごとの配列。"""
    f = np.broadcast_to(np.asarray(f, dtype=float), x.shape)
    y = np.zeros_like(x)
    x1 = x2 = y1 = y2 = 0.0
    for n in range(len(x)):
        w0 = 2 * np.pi * min(f[n], SR * 0.45) / SR
        c, s = np.cos(w0), np.sin(w0)
        alpha = s / (2 * q)
        if kind == "lp":
            b0, b1, b2 = (1 - c) / 2, 1 - c, (1 - c) / 2
        elif kind == "hp":
            b0, b1, b2 = (1 + c) / 2, -(1 + c), (1 + c) / 2
        else:  # band
            b0, b1, b2 = alpha, 0.0, -alpha
        a0, a1, a2 = 1 + alpha, -2 * c, 1 - alpha
        yn = (b0 * x[n] + b1 * x1 + b2 * x2 - a1 * y1 - a2 * y2) / a0
        x2, x1, y2, y1 = x1, x[n], y1, yn
        y[n] = yn
    return y


def tone(dur, f0, f1=None, shape="sine"):
    """f0 から f1 へ滑る音。"""
    t = t_axis(dur)
    f = np.full_like(t, f0) if f1 is None else f0 * (f1 / f0) ** (t / dur)
    ph = 2 * np.pi * np.cumsum(f) / SR
    if shape == "tri":
        return 2 / np.pi * np.arcsin(np.sin(ph))
    return np.sin(ph)


def pad(x, dur):
    n = int(SR * dur)
    return np.pad(x, (0, max(0, n - len(x))))[:n]


def mix(*parts):
    n = max(len(p) for p in parts)
    return sum(np.pad(p, (0, n - len(p))) for p in parts)


def at(x, start):
    return np.concatenate([np.zeros(int(SR * start)), x])


def save(name, x, peak=0.85):
    os.makedirs(OUT, exist_ok=True)
    x = np.asarray(x, dtype=float)
    fade = min(len(x), int(SR * 0.005))
    x[-fade:] *= np.linspace(1, 0, fade)        # 最後のプツ音を消す
    m = np.max(np.abs(x)) or 1.0
    data = (x / m * peak * 32767).astype(np.int16)
    path = os.path.join(OUT, name + ".wav")
    with wave.open(path, "wb") as w:
        w.setnchannels(1); w.setsampwidth(2); w.setframerate(SR)
        w.writeframes(data.tobytes())
    print("wrote", name, f"{len(x) / SR:.2f}s")


# ---------------------------------------------------------------- 音
def pickaxe_swing(rng, v):
    d = 0.28
    n = noise(d, rng)
    t = t_axis(d)
    f = 500 + 2200 * np.sin(np.pi * np.clip(t / d, 0, 1)) ** 2       # 振りの真ん中で一番高い
    x = biquad(n, "band", f * (1 + 0.1 * v), q=1.2)
    return x * np.sin(np.pi * np.clip(t / d, 0, 1)) ** 1.5


def pickaxe_hit_salt(rng, v):
    """岩塩：明るく割れる。カチッ＋高い響き＋ざらっとした欠片。"""
    d = 0.45
    click = biquad(noise(0.012, rng), "hp", 3000) * env(0.012, 0.0005, 0.004)
    ring = sum(tone(d, f * (1 + 0.04 * v)) * env(d, 0.001, dec) * g
               for f, dec, g in ((2350, 0.09, 0.6), (3720, 0.06, 0.4), (5150, 0.04, 0.25)))
    grit = biquad(noise(0.3, rng), "hp", 2500) * env(0.3, 0.003, 0.07) * 0.5
    thump = tone(0.12, 180, 110) * env(0.12, 0.001, 0.03) * 0.5
    return mix(click * 1.2, ring, at(grit, 0.01), thump)


def pickaxe_hit_soft(rng, v):
    """作物・植物：くぐもってめり込む。"""
    d = 0.25
    thud = tone(d, 140 + 20 * v, 70) * env(d, 0.002, 0.05)
    squelch = biquad(noise(d, rng), "lp", np.linspace(1800, 400, int(SR * d))) * env(d, 0.004, 0.05) * 0.8
    return mix(thud, squelch)


def pickaxe_hit_default(rng, v):
    """そのほか（地面、木、岩）：鈍いコツン。"""
    d = 0.2
    knock = tone(d, 420 + 30 * v, 300) * env(d, 0.001, 0.025)
    body = biquad(noise(d, rng), "band", 900, q=2) * env(d, 0.001, 0.03) * 0.8
    return mix(knock, body)


def harvest_pop(rng, v):
    """欠片や雨カブが1個ぽこっと出る。かわいく。"""
    d = 0.16
    return mix(tone(d, 820 + 60 * v, 430, "tri") * env(d, 0.002, 0.045), biquad(noise(0.02, rng), "hp", 2000) * 0.2)


def harvest_stage(rng, v):
    """塩のきのこの形が崩れる：ばきっ＋ぼろぼろ。"""
    crack = pickaxe_hit_salt(rng, v) * 0.8
    crumble = np.zeros(int(SR * 0.6))
    for k in range(14):
        s = int(SR * (0.03 + 0.5 * rng.random() ** 1.4))
        g = biquad(noise(0.03, rng), "band", rng.uniform(1500, 4500), q=3) * env(0.03, 0.001, 0.008)
        crumble[s:s + len(g)] += g[:max(0, len(crumble) - s)] * rng.uniform(0.3, 0.8)
    low = tone(0.3, 95, 60) * env(0.3, 0.003, 0.09) * 0.7
    return mix(crack, crumble, low)


def pick_pluck(rng, v):
    """手で摘む：ぷちっ。"""
    d = 0.14
    snap = biquad(noise(0.01, rng), "band", 2600, q=2) * env(0.01, 0.0005, 0.003)
    pop = tone(d, 640 + 40 * v, 900) * env(d, 0.001, 0.03) * 0.6
    return mix(snap * 1.5, at(pop, 0.004))


def crush_squish(rng, v):
    """潰れる：ぐしゃっと湿った音。"""
    d = 0.32
    t = t_axis(d)
    n = noise(d, rng)
    f = 1200 * (1 + 0.5 * np.sin(2 * np.pi * 23 * t)) * np.exp(-t / 0.15) + 250
    return biquad(n, "lp", f) * env(d, 0.003, 0.08) + tone(d, 110, 60) * env(d, 0.002, 0.05) * 0.6


def rustle(rng, d, bright=1800, grains=40, decay=0.1):
    """布の袋がこすれる：ざざっ。"""
    x = np.zeros(int(SR * d))
    for k in range(grains):
        s = int(SR * d * rng.random() * 0.8)
        g = biquad(noise(0.02, rng), "band", rng.uniform(bright * 0.6, bright * 1.6), q=1.5) * env(0.02, 0.002, 0.006)
        x[s:s + len(g)] += g[:max(0, len(x) - s)] * rng.uniform(0.3, 1)
    return x * env(d, 0.01, decay, hold=d * 0.2)


def bag_grab(rng, v):
    return mix(rustle(rng, 0.16, 2200, 14, 0.05), pick_pluck(rng, v) * 0.3)


def bag_stow(rng, v):
    """入れる：ざざっ＋中でごとっ。"""
    thunk = tone(0.18, 160 + 20 * v, 90) * env(0.18, 0.002, 0.04) * 0.9
    return mix(rustle(rng, 0.32, 1600, 30, 0.08), at(thunk, 0.2))


def bag_takeout(rng, v):
    return rustle(rng, 0.3, 2000, 28, 0.1)


def bag_drop(rng, v):
    return mix(tone(0.2, 200, 90) * env(0.2, 0.001, 0.04), biquad(noise(0.1, rng), "lp", 900) * env(0.1, 0.001, 0.02) * 0.6)


def bag_throw(rng, v):
    return mix(pickaxe_swing(rng, v) * 0.6, at(bag_drop(rng, v) * 0.7, 0.25))


def bag_hide(rng, v):
    """体に突っ込む：ぬぷっ。"""
    d = 0.4
    t = t_axis(d)
    goo = biquad(noise(d, rng), "lp", 300 + 900 * np.exp(-t / 0.12)) * env(d, 0.02, 0.12)
    gulp = tone(d, 300, 120) * env(d, 0.03, 0.08) * 0.7
    return mix(goo, gulp)


def bag_unhide(rng, v):
    d = 0.4
    t = t_axis(d)
    goo = biquad(noise(d, rng), "lp", 300 + 900 * (t / d)) * env(d, 0.05, 0.1, hold=0.15)
    pop = tone(0.1, 200, 500) * env(0.1, 0.002, 0.03) * 0.8
    return mix(goo, at(pop, 0.28))


def bag_burst(rng, v):
    """破れる：びりっ＋中身がばらばら落ちる。"""
    rip = biquad(noise(0.25, rng), "band", np.linspace(3000, 1200, int(SR * 0.25)), q=0.8) * env(0.25, 0.002, 0.08)
    fall = np.zeros(int(SR * 0.9))
    for k in range(9):
        s = int(SR * (0.18 + 0.6 * rng.random()))
        g = bag_drop(rng, rng.random()) * rng.uniform(0.3, 0.7)
        fall[s:s + len(g)] += g[:max(0, len(fall) - s)]
    return mix(rip * 1.3, fall)


def bag_eco_open(rng, v):
    return mix(rustle(rng, 0.9, 2600, 60, 0.3), at(tone(0.08, 900, 600) * env(0.08, 0.001, 0.02) * 0.4, 0.8))


def bag_down(rng, v):
    """背中のバッグを地面に下ろす：ざざっ＋どさっ。"""
    thump = mix(tone(0.25, 130, 70) * env(0.25, 0.003, 0.06), biquad(noise(0.15, rng), "lp", 700) * env(0.15, 0.002, 0.04) * 0.7)
    return mix(rustle(rng, 0.5, 1500, 30, 0.15), at(thump, 0.4))


def bag_up(rng, v):
    """地面のバッグを背負う：ざざっ（持ち上がる）。"""
    lift = biquad(noise(0.3, rng), "band", np.linspace(500, 1600, int(SR * 0.3)), q=1) * env(0.3, 0.05, 0.1) * 0.6
    return mix(rustle(rng, 0.55, 1800, 34, 0.18), lift)


def bag_peek(rng, v):
    return mix(rustle(rng, 0.2, 1400, 10, 0.06), tone(0.12, 500, 700) * env(0.12, 0.002, 0.03) * 0.3)


def gun_fire(rng, v, length=0.25, low=600, high=7000, pop=900, body=0.4):
    """炭酸ガスの「プシュ」：はじける音＋ガスが抜ける音。火薬の「パン」にはしない。"""
    t = t_axis(length)
    hiss = biquad(noise(length, rng), "band", high * np.exp(-t / (length * 0.6)) + low, q=0.9) * env(length, 0.001, length * 0.3)
    p = tone(0.06, pop * (1 + 0.05 * v), pop * 0.6) * env(0.06, 0.0005, 0.012) * body
    click = biquad(noise(0.006, rng), "hp", 4000) * env(0.006, 0.0002, 0.002)
    return mix(hiss, p, click * 0.8)


def gun_cue(rng, v, f=1800, d=0.08, metal=True):
    c = biquad(noise(0.01, rng), "band", f, q=3) * env(0.01, 0.0003, 0.003)
    ring = tone(d, f * 1.3) * env(d, 0.0005, 0.015) * (0.4 if metal else 0.1)
    return mix(c * 1.4, ring)


def gun_insert(rng, v):
    """塩を注ぐ・詰める：さらさら＋かちっ。"""
    pour = biquad(noise(0.35, rng), "hp", 3500) * env(0.35, 0.03, 0.1, hold=0.15) * 0.5
    return mix(pour, at(gun_cue(rng, v, 2100), 0.3))


def round_hit_slug(rng, v):
    """塩の粒がナメクジに当たる：ぺちっ（湿った体）。"""
    d = 0.14
    slap = biquad(noise(d, rng), "lp", 1400) * env(d, 0.0008, 0.025)
    return mix(slap, tone(d, 260 + 30 * v, 150) * env(d, 0.001, 0.02) * 0.7)


def round_hit_world(rng, v):
    """塩の粒が地面や岩に当たる：ちっ。"""
    d = 0.1
    return biquad(noise(d, rng), "band", 3200 + 400 * v, q=2) * env(d, 0.0005, 0.012)


def knife_stab(rng, v):
    swish = pickaxe_swing(rng, v)[: int(SR * 0.15)] * 0.6
    thunk = mix(tone(0.15, 220, 120) * env(0.15, 0.001, 0.03), biquad(noise(0.1, rng), "lp", 1200) * env(0.1, 0.001, 0.02) * 0.6)
    return mix(swish, at(thunk, 0.1))


def slug_hurt(rng, v):
    """撃たれたナメクジの小さな声：きゅっ。"""
    d = 0.22
    t = t_axis(d)
    f = 700 + 300 * v + 250 * np.sin(np.pi * t / d)
    ph = 2 * np.pi * np.cumsum(f) / SR
    voice = (np.sin(ph) + 0.3 * np.sin(2 * ph)) * env(d, 0.01, 0.07)
    return voice


def slug_lunge(rng, v):
    """縮んで伸びる：むにゅっと縮んで、びよーんと伸びる。"""
    d = 0.3
    t = t_axis(d)
    squelch = biquad(noise(0.08, rng), "lp", 900 + 300 * v) * env(0.08, 0.004, 0.025)
    f = (190 + 20 * v) * (520 / 190) ** np.clip(t / 0.18, 0, 1) * (1 + 0.06 * np.sin(2 * np.pi * 18 * t))
    ph = 2 * np.pi * np.cumsum(f) / SR
    boing = (np.sin(ph) + 0.25 * np.sin(2 * ph)) * env(d, 0.03, 0.09, hold=0.04) * 0.5
    return mix(squelch * 1.2, at(boing, 0.03))


def slug_land(rng, v):
    """着地：ぺちょっと湿った音＋ぷるんと揺れる。"""
    d = 0.24
    t = t_axis(d)
    slap = biquad(noise(0.06, rng), "lp", 1400 + 300 * v) * env(0.06, 0.001, 0.015)
    thud = tone(d, 150, 70) * env(d, 0.002, 0.04) * 0.6
    wob = np.sin(2 * np.pi * (260 + 30 * v) * t) * (0.5 + 0.5 * np.sin(2 * np.pi * 9 * t)) * env(d, 0.02, 0.06) * 0.25
    return mix(slap * 1.3, thud, at(wob, 0.02))


def bag_rattle(rng, v):
    """跳ねたときのバッグの中身：岩塩の塊がごとごと、かちかち鳴る。"""
    d = 0.34
    x = np.zeros(int(SR * d))
    for k in range(9):
        s = int(SR * (0.01 + 0.24 * rng.random()))
        click = biquad(noise(0.03, rng), "band", rng.uniform(1300, 3200), q=3) * env(0.03, 0.001, 0.008)
        knock = tone(0.06, rng.uniform(160, 260), 110) * env(0.06, 0.001, 0.02) * 0.6
        g = mix(click * rng.uniform(0.4, 1), knock * rng.uniform(0.3, 1))
        x[s:s + len(g)] += g[:max(0, len(x) - s)]
    return mix(x, rustle(rng, 0.2, 1600, 10, 0.06) * 0.4)


def tool_switch(rng, v):
    return mix(rustle(rng, 0.12, 2000, 8, 0.04), at(gun_cue(rng, v, 1400, 0.06, False), 0.06))


SOUNDS = {
    "pickaxe_swing": (pickaxe_swing, 2),
    "pickaxe_hit_salt": (pickaxe_hit_salt, 3),
    "pickaxe_hit_soft": (pickaxe_hit_soft, 2),
    "pickaxe_hit_default": (pickaxe_hit_default, 2),
    "harvest_pop": (harvest_pop, 2),
    "harvest_stage": (harvest_stage, 1),
    "pick_pluck": (pick_pluck, 2),
    "crush_squish": (crush_squish, 1),
    "bag_grab": (bag_grab, 1),
    "bag_stow": (bag_stow, 2),
    "bag_takeout": (bag_takeout, 1),
    "bag_drop": (bag_drop, 1),
    "bag_throw": (bag_throw, 1),
    "bag_hide": (bag_hide, 1),
    "bag_unhide": (bag_unhide, 1),
    "bag_burst": (bag_burst, 1),
    "bag_eco_open": (bag_eco_open, 1),
    "bag_peek": (bag_peek, 1),
    "bag_down": (bag_down, 1),
    "bag_up": (bag_up, 1),
    "gun_fire_rifle": (lambda r, v: gun_fire(r, v, 0.22, 700, 6500, 950, 0.35), 2),
    "gun_fire_pistol": (lambda r, v: gun_fire(r, v, 0.16, 900, 7500, 1200, 0.3), 1),
    "gun_fire_shotgun": (lambda r, v: gun_fire(r, v, 0.38, 400, 5000, 520, 0.6), 1),
    "gun_fire_sniper": (lambda r, v: gun_fire(r, v, 0.5, 300, 8000, 380, 0.8), 1),
    "gun_reload_open": (lambda r, v: gun_cue(r, v, 1500), 1),
    "gun_reload_eject": (lambda r, v: gun_cue(r, v, 1100, 0.1), 1),
    "gun_reload_insert": (gun_insert, 1),
    "gun_reload_seat": (lambda r, v: gun_cue(r, v, 2300, 0.12), 1),
    "round_hit_slug": (round_hit_slug, 2),
    "round_hit_world": (round_hit_world, 2),
    "knife_stab": (knife_stab, 1),
    "slug_hurt": (slug_hurt, 2),
    "tool_switch": (tool_switch, 1),
    "slug_lunge": (slug_lunge, 2),
    "slug_land": (slug_land, 2),
    "bag_rattle": (bag_rattle, 2),
}


def main():
    only = sys.argv[1:]          # 名前を渡すと、その音だけ作り直す
    for name, (fn, count) in SOUNDS.items():
        if only and name not in only:
            continue
        for i in range(count):
            rng = np.random.default_rng(zlib.crc32(name.encode()) + i * 31)   # 毎回同じ音になる
            save(f"{name}_{i + 1:02d}", fn(rng, i / max(count - 1, 1) - 0.5 if count > 1 else 0.0))


if __name__ == "__main__":
    main()
