# Makes the game's clips (Assets/Audio/Game/<key>_01.wav) from わく's AI sounds (Imported) and a few Kenney picks:
# mono 44.1 kHz 16 bit, the silence before and after cut, 5 ms fades, peak 0.85.
import sys, os, glob, numpy as np, soundfile as sf
A = sys.argv[1]                      # Assets/Audio
IMP, KEN, OUT = f"{A}/Imported", f"{A}/Kenney", f"{A}/Game"
os.makedirs(OUT, exist_ok=True)
SR = 44100
def load(path):
    x, sr = sf.read(path, always_2d=True); x = x.mean(1)
    if sr != SR: x = np.interp(np.arange(0, len(x), sr / SR), np.arange(len(x)), x)
    return x
def clean(y, head_db=-40, tail_db=-50):
    a = np.abs(y); pk = a.max()
    on = np.where(a > pk * 10**(head_db/20))[0]; st = max(0, on[0] - int(0.005*SR))
    loud = np.where(a > pk * 10**(tail_db/20))[0]; en = loud[-1] + 1
    y = y[st:en].copy()
    f = min(int(0.005*SR), len(y)//4); y[-f:] *= np.linspace(1, 0, f); y[:int(0.002*SR)] *= np.linspace(0, 1, int(0.002*SR))
    return y * 0.85 / np.abs(y).max()
def find(prefix):
    fs = [f for f in glob.glob(f"{IMP}/*.wav") if os.path.basename(f).startswith(prefix)]
    assert len(fs) == 1, (prefix, fs); return fs[0]
def save(key, y): sf.write(f"{OUT}/{key}_01.wav", y, SR, subtype="PCM_16"); print(f"{key:24s} {len(y)/SR:5.2f}s")

same = ["bag_burst","bag_down","bag_hide","bag_peek","bag_rattle","bag_spit","bag_stow","bag_takeout","bag_unhide","bag_up",
        "crush_squish","gun_fire_far","gun_fire_rifle","gun_fire_shotgun","gun_last_round","gun_low_ammo","gun_reload_dry",
        "gun_reload_open","harvest_stage","moisture_squelch","moisture_stage_wetter","moss_drink","pick_pluck_Tsukishiro",
        "pickaxe_hit_default","pickaxe_hit_salt","pickaxe_hit_soft","pickaxe_swing","round_flyby","round_hit_helmet",
        "round_hit_shell","round_hit_world","shell_break","slug_breath","slug_dry_danger","slug_fall","slug_land","slug_lunge",
        "slug_prone","slug_squeeze","slug_step_dry","slug_step_grass","slug_step_puddle","slug_step_rock","slug_step_salt",
        "slug_step_soil","tool_switch"]
for k in same: save(k, clean(load(find(k + ".wav"))))
save("gun_fire_pistol", clean(load(find("gun_fire_pistol"))))                    # 「音小さい」: brought up to the same peak
save("moisture_stage_drier", clean(load(find("moisture_stage_drier"))))          # 「少し音小さい」: the same
save("gun_reload_eject", clean(load(find("eject.wav"))))
save("gun_reload_insert", clean(load(find("insert.wav"))))
save("gun_reload_seat", clean(load(find("seat.wav"))))
mush, flower = clean(load(find("pick_mushroom"))), clean(load(find("pick_flower")))
save("pick_pluck", mush); save("pick_pluck_UgoTake", mush); save("pick_pluck_Tsuyuri", flower)
weak = clean(load(find("hit_confirm_weak")))
save("hit_confirm_weak", weak)
low = np.interp(np.arange(0, len(weak), 0.8), np.arange(len(weak)), weak)       # 「これの音低くしたのが普通」: 0.8x speed, about 4 semitones down
save("hit_confirm", low * 0.85 / np.abs(low).max())
climb = load(find("これ半分"))
save("slug_climb", clean(climb[:len(climb)//2]))                                    # 「半分くらい切り取って、よじ登る」
# not made with the AI yet: Kenney for now (kenney_picks.md)
save("round_hit_slug", clean(load(f"{KEN}/round_hit_slug/mix_A_slime_hiss.wav")))
save("gun_fire_sniper", clean(load(f"{KEN}/gun_fire_sniper/mix_B_punch_low_salt.wav")))
save("moisture_sizzle", clean(load(f"{KEN}/moisture_sizzle/scratch_004.ogg")))
save("harvest_pop", clean(load(f"{KEN}/harvest_pop/impactGlass_light_000.ogg")))
