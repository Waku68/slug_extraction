# SoundGen/seeds/five_sounds_full.wav を読み込み、
# 44.1kHz 16bit モノラルに変換した上で、無音区間で5つの音（seed_01〜seed_05）に分割し、
# それぞれの時間・周波数特性（低音/中音/高音比率、重心周波数、アタック感）を表示するスクリプト。
import os
import wave
import numpy as np

SR = 44100
SEEDS_DIR = os.path.join(os.path.dirname(__file__), "seeds")


def load_wav_mono_44k(path):
    with wave.open(path, "rb") as w:
        ch = w.getnchannels()
        sw = w.getsampwidth()
        sr = w.getframerate()
        n = w.getnframes()
        raw = w.readframes(n)
    if sw == 2:
        arr = np.frombuffer(raw, dtype=np.int16).astype(np.float64) / 32768.0
    elif sw == 4:
        arr = np.frombuffer(raw, dtype=np.int32).astype(np.float64) / 2147483648.0
    else:
        raise ValueError(f"Unsupported sample width: {sw}")
    if ch > 1:
        arr = arr.reshape(-1, ch).mean(axis=1)
    if sr != SR:
        new_n = int(round(len(arr) * SR / sr))
        arr = np.interp(np.linspace(0, len(arr) - 1, new_n), np.arange(len(arr)), arr)
    return arr


def save_wav(path, sig, norm_peak=0.88):
    os.makedirs(os.path.dirname(path), exist_ok=True)
    x = np.asarray(sig, dtype=np.float64).copy()
    if norm_peak is not None:
        m = np.max(np.abs(x)) or 1.0
        x = x / m * norm_peak
    pcm = np.clip(x * 32767.0, -32768, 32767).astype(np.int16)
    with wave.open(path, "wb") as w:
        w.setnchannels(1)
        w.setsampwidth(2)
        w.setframerate(SR)
        w.writeframes(pcm.tobytes())


def analyze_and_split():
    full_path = os.path.join(SEEDS_DIR, "five_sounds_full.wav")
    arr = load_wav_mono_44k(full_path)
    total_dur = len(arr) / SR
    print(f"Loaded five_sounds_full.wav: {total_dur:.2f}s, peak={np.max(np.abs(arr)):.4f}")

    # Re-save as clean 44.1kHz mono
    save_wav(full_path, arr, norm_peak=0.90)

    salt_path = os.path.join(SEEDS_DIR, "salt_melt_full.wav")
    if os.path.exists(salt_path):
        s_arr = load_wav_mono_44k(salt_path)
        save_wav(salt_path, s_arr, norm_peak=0.90)
        print(f"Loaded salt_melt_full.wav: {len(s_arr)/SR:.2f}s, peak={np.max(np.abs(s_arr)):.4f}")

    # 15ms window, 3ms hop RMS
    win = int(SR * 0.015)
    hop = int(SR * 0.003)
    rms = np.array([
        np.sqrt(np.mean(arr[i : i + win] ** 2))
        for i in range(0, len(arr) - win, hop)
    ])
    times = np.arange(len(rms)) * (hop / SR)

    noise_floor = np.percentile(rms, 20)
    thresh = max(noise_floor * 3.2, np.max(rms) * 0.035)
    active = rms > thresh

    segs = []
    in_seg = False
    start_t = 0.0
    for idx, is_on in enumerate(active):
        t = times[idx]
        if is_on and not in_seg:
            start_t = max(0.0, t - 0.025)
            in_seg = True
        elif not is_on and in_seg:
            end_t = min(total_dur, t + 0.055)
            if segs and start_t - segs[-1][1] < 0.14:
                segs[-1] = (segs[-1][0], end_t)
            else:
                segs.append((start_t, end_t))
            in_seg = False
    if in_seg:
        segs.append((start_t, total_dur))

    segs = [(s, e) for (s, e) in segs if (e - s) >= 0.04]
    print(f"\nFound {len(segs)} sound events:")
    for idx, (s, e) in enumerate(segs, start=1):
        seg = arr[int(s * SR) : int(e * SR)].copy()
        # Highpass 60Hz to remove mic handling rumble
        spec = np.fft.rfft(seg)
        freqs = np.fft.rfftfreq(len(seg), 1.0 / SR)
        spec[freqs < 55.0] *= (freqs[freqs < 55.0] / 55.0) ** 2
        seg = np.fft.irfft(spec, n=len(seg))

        mag = np.abs(spec)
        centroid = np.sum(freqs * mag) / (np.sum(mag) + 1e-12)
        e_low = np.sum(mag[(freqs >= 60) & (freqs < 500)] ** 2)
        e_mid = np.sum(mag[(freqs >= 500) & (freqs < 2500)] ** 2)
        e_high = np.sum(mag[freqs >= 2500] ** 2)
        e_tot = e_low + e_mid + e_high + 1e-12

        out_p = os.path.join(SEEDS_DIR, f"seed_{idx:02d}.wav")
        save_wav(out_p, seg, norm_peak=0.88)
        print(
            f"  seed_{idx:02d}.wav: {s:.2f}s..{e:.2f}s (dur={e-s:.2f}s) | "
            f"centroid={centroid:.0f}Hz | low={e_low/e_tot*100:.0f}% mid={e_mid/e_tot*100:.0f}% high={e_high/e_tot*100:.0f}%"
        )


if __name__ == "__main__":
    analyze_and_split()
