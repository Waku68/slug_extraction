# Argon 試作 v3：塩のアサルトライフル
# 調整：
#  1. 「ボッ」をもっと短く・軽く（約30msでスッと抜ける軽快な低中音パフ）
#  2. 「さらさら」の粒度を細かく（重なりすぎて「サーー」というホワイトノイズにならないよう、
#     極小0.3〜0.6msの塩の微結晶が隙間を保ってチリチリ・さらさらと鳴る設計）、音量も小さく
#  3. 全体がちょうど 0.50 秒で綺麗にフェードアウトして消える
import os
import wave
import numpy as np

SR = 44100
OUT_DIR = os.path.join(os.path.dirname(__file__), "demo_out")


def t_axis(dur):
    return np.arange(int(SR * dur)) / SR


def env_ad(dur, attack=0.0005, decay=0.015, hold=0.0, curve=1.0):
    t = t_axis(dur)
    a = np.clip(t / max(attack, 1e-5), 0.0, 1.0) ** curve
    d = np.exp(-np.clip(t - attack - hold, 0.0, None) / max(decay, 1e-5))
    return a * d


def biquad_static(x, kind, f0, q=0.707):
    w0 = 2.0 * np.pi * min(max(f0, 20.0), SR * 0.48) / SR
    c, s = np.cos(w0), np.sin(w0)
    alpha = s / (2.0 * q)
    if kind == "lp":
        b0, b1, b2 = (1 - c) / 2, 1 - c, (1 - c) / 2
    elif kind == "hp":
        b0, b1, b2 = (1 + c) / 2, -(1 + c), (1 + c) / 2
    elif kind == "bp":
        b0, b1, b2 = alpha, 0.0, -alpha
    else:
        raise ValueError(kind)
    a0, a1, a2 = 1 + alpha, -2 * c, 1 - alpha
    b0, b1, b2, a1, a2 = b0 / a0, b1 / a0, b2 / a0, a1 / a0, a2 / a0

    y = np.zeros_like(x, dtype=np.float64)
    x1 = x2 = y1 = y2 = 0.0
    for n in range(len(x)):
        xn = float(x[n])
        yn = b0 * xn + b1 * x1 + b2 * x2 - a1 * y1 - a2 * y2
        x2, x1 = x1, xn
        y2, y1 = y1, yn
        y[n] = yn
    return y


def biquad_sweep(x, kind, f_arr, q=0.707):
    f_arr = np.broadcast_to(np.asarray(f_arr, dtype=np.float64), x.shape)
    y = np.zeros_like(x, dtype=np.float64)
    x1 = x2 = y1 = y2 = 0.0
    for n in range(len(x)):
        w0 = 2.0 * np.pi * min(max(f_arr[n], 20.0), SR * 0.48) / SR
        c, s = np.cos(w0), np.sin(w0)
        alpha = s / (2.0 * q)
        if kind == "lp":
            b0, b1, b2 = (1 - c) / 2, 1 - c, (1 - c) / 2
        elif kind == "hp":
            b0, b1, b2 = (1 + c) / 2, -(1 + c), (1 + c) / 2
        else:
            b0, b1, b2 = alpha, 0.0, -alpha
        a0, a1, a2 = 1 + alpha, -2 * c, 1 - alpha
        xn = float(x[n])
        yn = (b0 * xn + b1 * x1 + b2 * x2 - a1 * y1 - a2 * y2) / a0
        x2, x1 = x1, xn
        y2, y1 = y1, yn
        y[n] = yn
    return y


def place(buf, sig, time_sec, gain=1.0):
    idx = int(round(time_sec * SR))
    if idx >= len(buf) or idx < 0:
        return
    end = min(len(buf), idx + len(sig))
    buf[idx:end] += sig[: end - idx] * gain


def make_short_light_bo(rng, p_mul=1.0, lightness=1.0):
    """
    さらに短く軽い「ボッ」：
    約28〜35msでスッと切れ、重苦しさを残さない軽快な低中音パフ
    """
    d = 0.042
    t = t_axis(d)
    raw_n = rng.standard_normal(len(t))

    # 1. 短く軽い低中域の空気の芯（215Hz -> 105Hz へ 6.5ms で素早く抜ける）
    f_core = (215.0 * np.exp(-t / 0.0065) + 105.0) * p_mul * lightness
    wobble = biquad_static(rng.standard_normal(len(t)), "lp", 140.0, q=0.7) * 0.22
    ph = 2.0 * np.pi * np.cumsum(f_core * (1.0 + wobble)) / SR
    core = np.sin(ph) * env_ad(d, attack=0.0005, decay=0.0072 / lightness, hold=0.0008)

    # 2. 「ボッ」の軽い筒パフ感（185Hz / 330Hz / 580Hz）
    puff_low = biquad_static(raw_n, "bp", 185.0 * p_mul * lightness, q=1.5) * env_ad(
        d, 0.0005, 0.0085 / lightness
    )
    puff_bo1 = biquad_sweep(
        raw_n, "bp", (390.0 * np.exp(-t / 0.007) + 245.0) * p_mul * lightness, q=1.8
    ) * env_ad(d, 0.0004, 0.0075 / lightness)
    puff_bo2 = biquad_sweep(
        raw_n, "bp", (760.0 * np.exp(-t / 0.0055) + 460.0) * p_mul * lightness, q=2.0
    ) * env_ad(d, 0.0003, 0.0060 / lightness)

    out = core * 0.65 + puff_low * 0.85 + puff_bo1 * 1.25 + puff_bo2 * 0.65

    # 重すぎる100Hz以下をすっきり切り、1350Hz以上を丸める
    out = biquad_static(out, "hp", 98.0 * lightness, q=0.707)
    out = biquad_static(out, "lp", 1350.0 * p_mul, q=0.75)
    out = np.tanh(out * 1.35)
    peak = np.max(np.abs(out)) or 1.0
    return out / peak


def make_fine_sarasara_salt(rng, total_dur=0.50, start_sec=0.016, p_mul=1.0):
    """
    粒度の細かい「さらさらさら…」：
    粒が重なりすぎて「サーー」という風ノイズにならないよう、
    0.25ms〜0.55ms の極小クリスタル粒子を適度な間隔（約140粒）で配置し、
    1粒ずつが細かく澄んだ高域（5.5kHz〜12kHz）で「さらさら…」と鳴って0.50秒で消える。
    """
    dur = total_dur - start_sec
    n_samples = int(round(SR * dur))
    out = np.zeros(n_samples, dtype=np.float64)
    t = np.arange(n_samples) / SR

    # 準等間隔ジッター配置＋小さな波で、粒同士が潰れず「さらさら」と細かく聴こえるようにする
    n_grains = 145
    base_times = np.linspace(0.0, dur - 0.006, n_grains)
    for idx, bt in enumerate(base_times):
        # 少しずつランダムに前後にずらし、自然な粒のばらつきを作る
        g_time = np.clip(bt + rng.uniform(-0.0022, 0.0022), 0.0, dur - 0.003)

        # 後半に行くほど粒の間隔が少し空くように間引く（自然なフェードアウト感）
        progress = g_time / dur
        if rng.random() < 0.38 * (progress ** 1.4):
            continue

        # 1粒は極小（0.25ms〜0.58ms）＋高Qフィルターで「細かい塩の粒」にする
        g_dur = rng.uniform(0.00025, 0.00058)
        g_len = max(5, int(SR * g_dur))
        # スパイク性を持たせた極小インパルス
        g_raw = rng.standard_normal(g_len) * np.exp(-np.linspace(0.0, 3.8, g_len))

        # 5.2kHz〜12.2kHz の澄んだ高域で、粒ごとに少しずつ音色が違う
        f_grain = rng.uniform(5200.0, 12200.0) * (1.0 - 0.15 * progress) * p_mul
        grain = biquad_static(g_raw, "bp", f_grain, q=rng.uniform(4.2, 7.5))

        # 微小な2粒目の反射（0.8ms後）を添えて「さらっ」とした質感にする
        place(out, grain, g_time, rng.uniform(0.35, 1.0))
        if rng.random() < 0.55 and g_time + 0.0012 < dur - 0.002:
            place(out, grain * rng.uniform(0.3, 0.6), g_time + rng.uniform(0.0006, 0.0014), 1.0)

    # 時間エンベロープ：発射直後にすっと立ち上がり、さら・さら・さらと揺れながら0.50秒で完全に消える
    attack_env = np.clip(t / 0.020, 0.0, 1.0) ** 1.2
    norm_t = np.clip(t / dur, 0.0, 1.0)
    # 0.50秒（norm_t=1.0）に向かって滑らかにゼロへフェードアウト
    fade_out = np.exp(-norm_t * 2.2) * (0.5 * (1.0 + np.cos(np.pi * (norm_t ** 1.1))))
    # 「さら・さら・さら」の微かなリズム感（約16Hz）
    ripple = 0.72 + 0.28 * np.sin(2.0 * np.pi * 16.0 * t - 0.5)

    out *= attack_env * fade_out * ripple

    # 低中域の濁りを完全にカットして細かい塩粒の帯域だけ残す
    out = biquad_static(out, "hp", 4200.0 * p_mul, q=0.707)
    out = biquad_static(out, "lp", 13800.0, q=0.707)

    peak = np.max(np.abs(out)) or 1.0
    return out / peak


def synth_salt_rifle_v3(
    rng,
    pitch_var=0.0,
    bo_lightness=1.0,
    sara_level=0.075,
):
    """
    全体きっちり 0.50 秒：
    0.000s〜0.035s: 短く軽い「ボッ」
    0.016s〜0.500s: 粒度細かく小さな「さらさらさら…」が0.50秒でフェードアウト完了
    """
    total_dur = 0.50
    out = np.zeros(int(round(SR * total_dur)), dtype=np.float64)
    p_mul = 2.0 ** (pitch_var / 12.0)

    # 1. 短く軽い「ボッ」
    bo = make_short_light_bo(rng, p_mul=p_mul, lightness=bo_lightness)
    place(out, bo, 0.0, 1.0)

    # 2. 細かく小さな「さらさらさら…」（0.50秒でフェードアウト）
    sara = make_fine_sarasara_salt(rng, total_dur=total_dur, start_sec=0.016, p_mul=p_mul)
    place(out, sara, 0.016, sara_level)

    return out


def save_wav(name, sig, peak=0.86):
    os.makedirs(OUT_DIR, exist_ok=True)
    x = np.asarray(sig, dtype=np.float64)
    m = np.max(np.abs(x)) or 1.0
    pcm = (x / m * peak * 32767.0).astype(np.int16)
    path = os.path.join(OUT_DIR, name + ".wav")
    with wave.open(path, "wb") as w:
        w.setnchannels(1)
        w.setsampwidth(2)
        w.setframerate(SR)
        w.writeframes(pcm.tobytes())
    print(f"Saved: {path} ({len(x)/SR:.2f}s)")
    return path


def main():
    # パターン1：標準（短く軽い「ボッ」＋粒度細かく小さな「さらさら…」0.50sフェードアウト）
    for i, pv in enumerate([-0.25, 0.25], start=1):
        rng = np.random.default_rng(1000 + i)
        sig = synth_salt_rifle_v3(rng, pitch_var=pv, bo_lightness=1.05, sara_level=0.085)
        save_wav(f"salt_ar_v3_standard_0{i}", sig)

    # パターン2：「ボッ」をさらに軽く短く、さらさらを極小(0.055)にした繊細版
    rng_light = np.random.default_rng(2001)
    sig_light = synth_salt_rifle_v3(rng_light, pitch_var=0.8, bo_lightness=1.25, sara_level=0.055)
    save_wav("salt_ar_v3_lighter_01", sig_light)

    # パターン3：「さらさら」の粒がもう少しだけ聴き取りやすい版（sara_level=0.13）
    rng_sara = np.random.default_rng(3001)
    sig_sara = synth_salt_rifle_v3(rng_sara, pitch_var=0.0, bo_lightness=1.05, sara_level=0.13)
    save_wav("salt_ar_v3_soft_sara_01", sig_sara)

    # 連射＋単発プレビュー（3連射 → 一拍置いて1発撃ち、0.5秒のフェードアウトまで確認）
    burst_len = int(SR * 1.40)
    burst_buf = np.zeros(burst_len, dtype=np.float64)
    shot_times = [0.04, 0.145, 0.25, 0.75]
    for idx, t0 in enumerate(shot_times):
        rng_s = np.random.default_rng(5000 + idx)
        pv = rng_s.uniform(-0.4, 0.4)
        s = synth_salt_rifle_v3(rng_s, pitch_var=pv, bo_lightness=1.05, sara_level=0.085)
        place(burst_buf, s, t0, rng_s.uniform(0.93, 1.0))
    save_wav("salt_ar_v3_burst_preview", burst_buf)


if __name__ == "__main__":
    main()
