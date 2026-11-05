# Argon 試作 v5：縮む音 (slug_squeeze) — 撮り直し実録音 (814801145.600924.mp4) ベース
# v4で不自然になった原因（過度なピッチダウンや強いバンドパスフィルター、音の二重貼り）をやめ、
# わくさんが新しく録り直した縮む音（0.28s〜0.70s）の原音の質感をそのまま活かす。
import os
import wave
import numpy as np

SR = 44100
SEEDS_DIR = os.path.join(os.path.dirname(__file__), "seeds")
OUT_DIR = os.path.join(os.path.dirname(__file__), "demo_out")


def load_wav(path):
    with wave.open(path, "rb") as w:
        n = w.getnframes()
        raw = w.readframes(n)
        ch = w.getnchannels()
    arr = np.frombuffer(raw, dtype=np.int16).astype(np.float64) / 32768.0
    if ch > 1:
        arr = arr.reshape(-1, ch).mean(axis=1)
    return arr


def fft_filter(sig, hp_hz=None, lp_hz=None):
    """位相歪みのない緩やかなFFTフィルター（原音の質感を壊さずマイクの吹かれや超高域ノイズだけ整える）"""
    n = len(sig)
    spec = np.fft.rfft(sig)
    freqs = np.fft.rfftfreq(n, 1.0 / SR)
    gain = np.ones_like(freqs)
    if hp_hz is not None and hp_hz > 0:
        gain *= 1.0 / np.sqrt(1.0 + (hp_hz / np.maximum(freqs, 1.0)) ** 4)
    if lp_hz is not None and lp_hz > 0:
        gain *= 1.0 / np.sqrt(1.0 + (freqs / lp_hz) ** 4)
    return np.fft.irfft(spec * gain, n=n)


def resample_pitch(sig, ratio):
    n_out = max(4, int(round(len(sig) / ratio)))
    idx = np.linspace(0.0, len(sig) - 1, n_out)
    return np.interp(idx, np.arange(len(sig)), sig)


def trim_fade(sig, fade_in=0.008, fade_out=0.020):
    s = sig.copy()
    fi = min(len(s), int(round(SR * fade_in)))
    fo = min(len(s), int(round(SR * fade_out)))
    if fi > 1:
        s[:fi] *= np.sin(0.5 * np.pi * np.linspace(0.0, 1.0, fi)) ** 2
    if fo > 1:
        s[-fo:] *= np.cos(0.5 * np.pi * np.linspace(0.0, 1.0, fo)) ** 2
    peak = np.max(np.abs(s)) or 1.0
    return s / peak


def place(buf, sig, time_sec, gain=1.0):
    idx = int(round(time_sec * SR))
    if idx >= len(buf) or idx < 0:
        return
    end = min(len(buf), idx + len(sig))
    buf[idx:end] += sig[: end - idx] * gain


def load_v5_seeds():
    retake = load_wav(os.path.join(SEEDS_DIR, "retake_full.wav"))
    five = load_wav(os.path.join(SEEDS_DIR, "five_sounds_full.wav"))

    # 1. 撮り直しの縮む音（0.28s〜0.70s、ちょうど0.42秒）：原音そのまま（65Hz以下のマイク振動のみカット）
    sq_raw = retake[int(0.28 * SR) : int(0.70 * SR)]
    sq_clean = trim_fade(fft_filter(sq_raw, hp_hz=65.0, lp_hz=8000.0), fade_in=0.010, fade_out=0.030)

    # 2. 撮り直しの伸びる音「ひゅー」（3.64s〜3.98s、0.34秒）：
    #    - マイクに息が当たったボフッ（240Hz以下）をすっきりさせた版
    #    - 低音の息圧も残した版
    hyuu_raw = retake[int(3.64 * SR) : int(3.99 * SR)]
    hyuu_clean = trim_fade(fft_filter(hyuu_raw, hp_hz=250.0, lp_hz=9000.0), fade_in=0.006, fade_out=0.035)
    hyuu_full = trim_fade(fft_filter(hyuu_raw, hp_hz=95.0, lp_hz=9000.0), fade_in=0.006, fade_out=0.035)

    # 3. 5種類音.MOV の1番目（0.800s〜1.055s）：
    #    不自然な二重貼りをせず、録音されたままの自然な2バウンス（0.81sと0.855s＋0.96sの転がり）をそのまま使う
    kara_natural = five[int(0.800 * SR) : int(1.055 * SR)]
    kara_natural = trim_fade(fft_filter(kara_natural, hp_hz=180.0, lp_hz=9000.0), fade_in=0.002, fade_out=0.020)

    # 4. 「ｺﾛｯ」用：5種類音.MOV の1番目のうち、落ち着いた転がり部分（0.800s〜0.915sの2連バウンス）
    koro_natural = five[int(0.800 * SR) : int(0.915 * SR)]
    koro_natural = trim_fade(fft_filter(koro_natural, hp_hz=140.0, lp_hz=3800.0), fade_in=0.002, fade_out=0.015)

    return {
        "sq_clean": sq_clean,
        "hyuu_clean": hyuu_clean,
        "hyuu_full": hyuu_full,
        "kara_natural": kara_natural,
        "koro_natural": koro_natural,
    }


def synth_slug_squeeze_v5(seeds, sq_pitch=1.0, koro_level=0.55, koro_delay=0.095, koro_pitch=0.82):
    dur = 0.42
    out = np.zeros(int(round(SR * dur)), dtype=np.float64)

    sq = seeds["sq_clean"]
    if abs(sq_pitch - 1.0) > 1e-3:
        sq = resample_pitch(sq, sq_pitch)
    place(out, sq, 0.000, 1.0)

    if koro_level > 0:
        koro = resample_pitch(seeds["koro_natural"], koro_pitch)
        place(out, koro, koro_delay, koro_level)

    return out


def save_wav(name, sig, peak=0.86):
    os.makedirs(OUT_DIR, exist_ok=True)
    x = np.asarray(sig, dtype=np.float64).copy()
    fade = min(len(x), int(SR * 0.005))
    x[-fade:] *= np.linspace(1.0, 0.0, fade)
    m = np.max(np.abs(x)) or 1.0
    pcm = (x / m * peak * 32767.0).astype(np.int16)
    path = os.path.join(OUT_DIR, name + ".wav")
    with wave.open(path, "wb") as w:
        w.setnchannels(1)
        w.setsampwidth(2)
        w.setframerate(SR)
        w.writeframes(pcm.tobytes())
    print(f"Saved: {path} ({len(x)/SR:.2f}s)")


def main():
    seeds = load_v5_seeds()

    # v5_A：撮り直した縮む音（原音そのまま）＋ 0.095秒後に自然な「ｺﾛｯ」を中くらいで添えた版
    sig_a = synth_slug_squeeze_v5(seeds, sq_pitch=1.0, koro_level=0.52, koro_delay=0.095, koro_pitch=0.82)
    save_wav("slug_squeeze_v5_A", sig_a)

    # v5_B：撮り直した縮む音（原音そのまま、余計な音を足さない純粋版）
    sig_b = synth_slug_squeeze_v5(seeds, sq_pitch=1.0, koro_level=0.0)
    save_wav("slug_squeeze_v5_B", sig_b)

    # v5_C：撮り直した縮む音をほんの少し低く重め（0.88倍）にし、0.11秒後に低めの「ｺﾛｯ」を添えた版
    sig_c = synth_slug_squeeze_v5(seeds, sq_pitch=0.88, koro_level=0.58, koro_delay=0.110, koro_pitch=0.72)
    save_wav("slug_squeeze_v5_C", sig_c)

    # 試し聞き v5_preview：短押し(0.16s) → 中押し(0.26s) → フル押し(0.40s)
    prev_len = int(SR * 1.90)
    prev_buf = np.zeros(prev_len, dtype=np.float64)
    for idx, (t0, cut_dur, sp) in enumerate([(0.05, 0.16, 0.98), (0.60, 0.26, 1.02), (1.22, 0.40, 1.00)]):
        s = synth_slug_squeeze_v5(seeds, sq_pitch=sp, koro_level=0.52, koro_delay=0.095, koro_pitch=0.82 * sp)
        s /= np.max(np.abs(s)) or 1.0
        n_cut = min(len(s), int(round(SR * cut_dur)))
        s_cut = s[:n_cut].copy()
        f_len = min(len(s_cut), int(SR * 0.005))
        s_cut[-f_len:] *= np.linspace(1.0, 0.0, f_len)
        place(prev_buf, s_cut, t0, 1.0)
    save_wav("slug_squeeze_v5_preview", prev_buf)


if __name__ == "__main__":
    main()
