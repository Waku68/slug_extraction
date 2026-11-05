# Argon 試作 v5：伸びる音 (slug_lunge) — 撮り直し実録音 (814801145.600924.mp4) ベース
# わくさんが新しく録り直した「ひゅー」（3.64s〜3.99s）の原音を壊さずにそのまま使い、
# 5種類音.MOV の1番目の録音（「カラッ」）も二重貼りせず自然な1テイクのまま重ねる。
import os
import wave
import numpy as np
from slug_squeeze_demo import (
    SR,
    OUT_DIR,
    resample_pitch,
    place,
    load_v5_seeds,
    synth_slug_squeeze_v5,
    save_wav,
)


def synth_slug_lunge_v5(
    seeds,
    use_full_low=False,
    hyuu_speed=1.18,
    hyuu_level=0.85,
    kara_level=0.95,
    kara_delay=0.035,
    kara_pitch=1.0,
):
    """
    撮り直した「ひゅー」原音 ＋ 自然な「カラッ」原音：
    余計なバンドパスや歪みをかけず、録音そのままのクリアな質感で鳴らす。
    """
    dur = 0.30
    out = np.zeros(int(round(SR * dur)), dtype=np.float64)

    hyuu_src = seeds["hyuu_full"] if use_full_low else seeds["hyuu_clean"]
    hyuu = resample_pitch(hyuu_src, hyuu_speed)
    place(out, hyuu, 0.000, hyuu_level)

    if kara_level > 0:
        kara = resample_pitch(seeds["kara_natural"], kara_pitch)
        place(out, kara, kara_delay, kara_level)

    return out


def main():
    seeds = load_v5_seeds()

    # v5_A：標準（撮り直した「ひゅー」のマイク吹かれだけ整えたクリアな原音 ＋ 自然な「カラッ」原音）
    sig_a = synth_slug_lunge_v5(
        seeds, use_full_low=False, hyuu_speed=1.15, hyuu_level=0.85, kara_level=0.95, kara_delay=0.035, kara_pitch=1.0
    )
    save_wav("slug_lunge_v5_A", sig_a, peak=0.90)

    # v5_B：撮り直した「ひゅー」の原音そのまま（ピッチ等倍 1.0・「カラッ」控えめで「ひゅー」が一番よく聴こえる版）
    sig_b = synth_slug_lunge_v5(
        seeds, use_full_low=False, hyuu_speed=1.00, hyuu_level=1.00, kara_level=0.55, kara_delay=0.040, kara_pitch=1.0
    )
    save_wav("slug_lunge_v5_B", sig_b, peak=0.90)

    # v5_C：撮り直した「ひゅー」の低音の息圧（ボフッとした勢い）も残し、「カラッ」をしっかり重ねた力強い版
    sig_c = synth_slug_lunge_v5(
        seeds, use_full_low=True, hyuu_speed=1.10, hyuu_level=0.90, kara_level=1.00, kara_delay=0.032, kara_pitch=0.95
    )
    save_wav("slug_lunge_v5_C", sig_c, peak=0.90)

    # 試し聞き v5_preview：
    # 前半：伸びる音単発3回（小0.65 → 中0.82 → 大1.0）
    # 後半：縮む(v5) → 離して伸びる(v5) を3回（短・中・フル溜め）
    prev_len = int(SR * 4.00)
    prev_buf = np.zeros(prev_len, dtype=np.float64)

    for idx, (t0, vol, ps) in enumerate([(0.05, 0.65, 0.97), (0.48, 0.82, 1.00), (0.92, 1.00, 1.03)]):
        s = synth_slug_lunge_v5(seeds, hyuu_speed=1.15 * ps, kara_pitch=1.0 * ps)
        s /= np.max(np.abs(s)) or 1.0
        place(prev_buf, s, t0, vol)

    combo_specs = [
        (1.50, 0.16, 0.65, 0.98),
        (2.15, 0.26, 0.82, 1.02),
        (2.95, 0.40, 1.00, 1.00),
    ]
    for idx, (t_sq, hold_dur, lunge_vol, ps) in enumerate(combo_specs):
        sq = synth_slug_squeeze_v5(seeds, sq_pitch=ps)
        sq /= np.max(np.abs(sq)) or 1.0
        n_cut = min(len(sq), int(round(SR * hold_dur)))
        sq_cut = sq[:n_cut].copy()
        f_len = min(len(sq_cut), int(SR * 0.005))
        sq_cut[-f_len:] *= np.linspace(1.0, 0.0, f_len)
        place(prev_buf, sq_cut, t_sq, 0.85)

        lg = synth_slug_lunge_v5(seeds, hyuu_speed=1.15 * ps, kara_pitch=1.0 * ps)
        lg /= np.max(np.abs(lg)) or 1.0
        place(prev_buf, lg, t_sq + hold_dur, lunge_vol)

    save_wav("slug_lunge_v5_preview", prev_buf, peak=0.90)


if __name__ == "__main__":
    main()
