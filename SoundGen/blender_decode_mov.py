import aud
import glob
import os
import wave
import numpy as np

imported_dir = r"C:\UnityProjects\ExtractionGame\Assets\Audio\Imported"
seeds_dir = r"C:\UnityProjects\ExtractionGame\SoundGen\seeds"
os.makedirs(seeds_dir, exist_ok=True)

for idx, mov in enumerate(sorted(glob.glob(os.path.join(imported_dir, "*.MOV"))), start=1):
    bname = os.path.splitext(os.path.basename(mov))[0]
    snd = aud.Sound(mov)
    sr, ch = snd.specs
    sr = int(round(sr))
    data = snd.data()  # (N, ch) float32
    mono = data.mean(axis=1).astype(np.float64) if data.ndim > 1 else data.astype(np.float64)

    if sr != 44100:
        new_n = int(round(len(mono) * 44100 / sr))
        mono = np.interp(np.linspace(0, len(mono) - 1, new_n), np.arange(len(mono)), mono)
        sr = 44100

    safe_name = "five_sounds_full.wav" if "5" in bname else "desk_scratch_full.wav"
    out_wav = os.path.join(seeds_dir, safe_name)
    pcm = np.clip(mono * 32767.0, -32768, 32767).astype(np.int16)
    with wave.open(out_wav, "wb") as w:
        w.setnchannels(1)
        w.setsampwidth(2)
        w.setframerate(44100)
        w.writeframes(pcm.tobytes())
    print(f"Saved {safe_name} from '{bname}': {len(mono)/44100:.2f}s, peak={np.max(np.abs(mono)):.4f}")
