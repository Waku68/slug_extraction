# -*- coding: utf-8 -*-
"""Economy check for the 30-volume bag (docs/discussions/economy_bag30_discussion.md).

Run:  python docs/tech/economy_bag30.py
Prints the tables used in the discussion. Every number here is a guess to be tuned by playing;
change the constants at the top and run again.

Model, in short
- A run: crawl out light, harvest a typical load, crawl back at the load's speed, then time in the base.
- Money per run = survive * load + kills * (what a kill pays) - die * gear - ammo fired.
  Kills per run for an average player = deaths by other players (PvP is zero-sum in count).
- Per hour = money per run * runs per hour. Season = per hour * hours per week * weeks.
- Rock salt supply: mushrooms * pieces / (regrow time / dry share of the weather).
"""

ISLAND_HOUR_MIN = 9.5 * 60 / 24          # one island hour in real minutes (island day = 9.5 h)

# ---- weight -> speed (Assets/Tuning/ExtractSettings.asset, unchanged) ----
THRESHOLD, LIGHT_SLOW, OVER_SPEED, HEAVIEST, HEAVY_RANGE = 20, 0.25, 0.75, 0.45, 20
TOOLS_WEIGHT = 3.0                        # gun + pickaxe + empty bag


def speed_for(w):
    if w <= THRESHOLD:
        return 1 - LIGHT_SLOW * w / THRESHOLD
    over = min(1.0, (w - THRESHOLD) / HEAVY_RANGE)
    return OVER_SPEED + (HEAVIEST - OVER_SPEED) * over


# ---- items: weight, volume (crop = 1), amore, seconds to take one (harvest + put in bag) ----
ITEMS = {
    "rock":    (1.5, 1.0, 3, 11),   # rock salt
    "black":   (1.5, 1.0, 2, 9),    # black salt
    "coarse":  (1.0, 1.0, 1, 4),    # coarse salt (scooped)
    "crop":    (0.5, 1.0, 1, 6),    # rain turnip
    "ugo":     (0.15, 0.4, 1, 4),   # ugotake
    "hgreen":  (0.2, 0.7, 1, 6),    # hideribeni, green (wet season)
    "hred":    (0.2, 0.7, 2, 6),    # hideribeni, red (dry season)
    "moss":    (0.1, 0.8, 1, 5),    # shibuki moss
    "flower":  (0.02, 0.1, 0.2, 3), # colour flower
    "seed":    (0.05, 0.1, 1, 3),
    "tsuyuri": (0.05, 0.15, 12, 3), # rare things x2 (6 -> 12) so the best find stays about one run
    "hibana":  (0.1, 0.25, 12, 3),
}
SALTS = ("rock", "black", "coarse")
PLANT_SELL = 1.0           # the wholesaler pays plants in full (Oct 25 わく). Persistent island: about 1.15
                           # (half sold to the flower pedlar, who comes once an island day and pays about 1.3x the market)

# ---- areas: (distance camp<->area m, survive share, gear tier, typical loads, minutes roaming between spots) ----
# loads: "lo" = a so-so run (wet season / crowded), "hi" = a good run (dry season / quiet)
AREAS = {
    "forest":   (150, 0.80, "light",  {"lo": {"crop": 24},
                                       "hi": {"crop": 22, "ugo": 15}}, 8),
    "meadow":   (300, 0.85, "light",  {"lo": {"flower": 25, "seed": 4, "crop": 8},
                                       "hi": {"flower": 25, "seed": 4, "crop": 8, "tsuyuri": 2}}, 12),
    "saltflat": (150, 0.72, "light",  {"lo": {"coarse": 18, "hgreen": 12},
                                       "hi": {"coarse": 18, "hred": 12}}, 6),
    "lava":     (250, 0.70, "normal", {"lo": {"black": 14, "crop": 6},
                                       "hi": {"black": 18, "hibana": 2}}, 9),
    "mountain": (300, 0.60, "normal", {"lo": {"rock": 8, "moss": 5, "crop": 6},
                                       "hi": {"rock": 18, "moss": 4}}, 10),
}
SHARE = {"forest": 0.30, "meadow": 0.10, "saltflat": 0.20, "lava": 0.15, "mountain": 0.25}

LIGHT_SPEED = 1.3          # m/s, crawling with some lunges
ALERT = 2.0                # harvesting takes this many times longer (looking around, hiding, fights)
BASE_MIN = 6               # minutes in the base per run (spread the bag, sell, eat, fly out)
PVP_SHARE = 0.6            # deaths by another slug (the rest: drying, sea, snails)
BAG_AT_DEATH = 0.5         # the bag is half full when you fall, on average
GEAR_RESALE = 0.4          # the wholesaler buys gear at 40%

# ---- options: gear prices; ammo is sold in batches (rifle 10 / pistol 15 / shotgun 3 / sniper 1 rounds) ----
# batch price = salt value x k  (old k = 1: an ordinary batch is 1 amore, a rock salt batch is 3)
OLD = dict(name="old prices",
           pistol=4, rifle=12, armor=8, helmet=4, leather=5, basic=3, k=1)
SAFE = dict(name="A (safe)",                       # gear x2, ammo x2: same shape as before
            pistol=8, rifle=24, armor=16, helmet=8, leather=10, basic=3, k=2)
BOLD = dict(name="B (bold)",                       # gear as before, ammo x3: the bag is the stake
            pistol=4, rifle=12, armor=8, helmet=4, leather=5, basic=3, k=3)
BAG_TAKEN = 0.4            # share of a victim's bag the killer brings home
for _o in (OLD, SAFE, BOLD):
    _o["amore_per_round"] = _o["k"] / 10

ROUNDS_FIRED = {"light": 15, "normal": 35}         # per run, survived or not
ROUNDS_CARRIED = {"light": 15, "normal": 40}       # lost on death with the belt


def gear_cost(o, tier):
    if tier == "light":
        return o["basic"] + o["pistol"] + ROUNDS_CARRIED["light"] * o["amore_per_round"]
    return (o["basic"] + o["rifle"] + o["armor"] + o["helmet"] + o["leather"]
            + ROUNDS_CARRIED["normal"] * o["amore_per_round"])


def lost_on_death(o, tier):
    # basic tools (pickaxe, moss balls, cloth bag) are kept (spec 8)
    return gear_cost(o, tier) - o["basic"]


def load_stats(load):
    w = sum(ITEMS[k][0] * n for k, n in load.items())
    vol = sum(ITEMS[k][1] * n for k, n in load.items())
    val = sum(ITEMS[k][2] * n * (1 if k in SALTS else PLANT_SELL) for k, n in load.items())
    sec = sum(ITEMS[k][3] * n for k, n in load.items())
    return w, vol, val, sec


def run(area, which, o):
    dist, p, tier, loads, roam = AREAS[area]
    w, vol, val, sec = load_stats(loads[which])
    spd = speed_for(w + TOOLS_WEIGHT)
    minutes = dist / LIGHT_SPEED / 60 + sec * ALERT / 60 + roam + dist / (LIGHT_SPEED * spd) / 60
    die = 1 - p
    kills = die * PVP_SHARE
    kill_pay = GEAR_RESALE * lost_on_death(o, tier) + BAG_TAKEN * BAG_AT_DEATH * val
    ammo = ROUNDS_FIRED[tier] * o["amore_per_round"]
    ev = p * val + kills * kill_pay - die * lost_on_death(o, tier) - ammo
    per_hour = ev * 60 / (minutes + BASE_MIN)
    return dict(w=w, vol=vol, val=val, spd=spd, min=minutes, ev=ev, ph=per_hour, gear=gear_cost(o, tier))


def kill_cost(o, hits=8, hit_rate=0.35, armor_mult=1.0):
    return hits / hit_rate * armor_mult * o["amore_per_round"]


def table_runs(o):
    print(f"\n### {o['name']}: one run (survived value, minutes, money per run and per hour)\n")
    print("| area | load | vol | weight | speed | minutes | value | money/run | money/h |")
    print("| --- | --- | --- | --- | --- | --- | --- | --- | --- |")
    avg = 0.0
    avg_val = 0.0
    for a in AREAS:
        for which in ("lo", "hi"):
            r = run(a, which, o)
            print(f"| {a} | {which} | {r['vol']:.0f} | {r['w'] + TOOLS_WEIGHT:.0f} | x{r['spd']:.2f} | {r['min']:.0f} "
                  f"| {r['val']:.0f} | {r['ev']:+.1f} | {r['ph']:+.0f} |")
            avg += SHARE[a] * r["ph"] / 2
            avg_val += SHARE[a] * r["val"] / 2
    light, normal = gear_cost(o, "light"), gear_cost(o, "normal")
    print(f"\naverage player: {avg:.0f} amore per hour; average run value {avg_val:.0f}")
    print(f"light set {light:.0f}, normal set {normal:.0f} = {normal / avg_val:.1f} average runs, "
          f"{normal / run('mountain', 'hi', o)['val']:.1f} good mountain runs")
    print(f"kill cost (rifle, bare) {kill_cost(o):.1f}, armored {kill_cost(o, armor_mult=1 / 0.6):.1f}; "
          f"empty light target pays {GEAR_RESALE * lost_on_death(o, 'light'):.1f}; "
          f"full rock salt bag (90) = {90 / normal:.1f} normal sets")
    return avg


# ---- rock salt supply on a persistent island ----
PIECES = 3
PHASE_DRY = {"big wet": 0.25, "wet": 0.40, "dry": 0.75, "big dry": 0.90}   # share of dry weather on the mountain
PHASE_MOUNTAIN = {"big wet": 0.15, "wet": 0.20, "dry": 0.30, "big dry": 0.35}  # share of players on the mountain


def salt_table(mushrooms, stage_hours, players=15, run_min=33):
    full_regrow_real_h = 2 * stage_hours * ISLAND_HOUR_MIN / 60    # stump -> half -> full
    rows = []
    for ph in PHASE_DRY:
        supply = mushrooms * PIECES / full_regrow_real_h * PHASE_DRY[ph]          # pieces per real hour
        runs = players * PHASE_MOUNTAIN[ph] * 60 / (run_min + BASE_MIN)          # mountain runs per hour
        rows.append((ph, supply, supply / runs))
    return full_regrow_real_h, rows


def print_salt():
    print("\n### rock salt per mountain run (persistent island, 15 players)\n")
    print("| mushrooms | one stage (island h) | full regrow (real) | big wet | wet | dry | big dry |")
    print("| --- | --- | --- | --- | --- | --- | --- |")
    for m, st in ((40, 3), (90, 3), (90, 2), (120, 2), (40, 1)):
        full, rows = salt_table(m, st)
        cells = " | ".join(f"{r[2]:.0f}" for r in rows)
        print(f"| {m} | {st} | {full * 60:.0f} min | {cells} |")
    print("\n(players 12 and 18 with 90 mushrooms, 2 island h a stage)")
    for n in (12, 18):
        _, rows = salt_table(90, 2, players=n)
        print(n, " / ".join(f"{r[0]} {r[2]:.0f}" for r in rows))


# ---- season ----
PROFILES = {"casual 11h/wk": 11, "mid 18h/wk": 18, "heavy 28h/wk": 28}


MUST_BASE_HOURS, ALL_BASE_HOURS = 10, 30      # base costs are set in hours of average play


def print_season(o, per_hour, weeks_list=(8, 10, 12)):
    print(f"\n### {o['name']}: money kept over a season (average player {per_hour:.0f}/h)\n")
    print(f"must-have base = {MUST_BASE_HOURS} h = {per_hour * MUST_BASE_HOURS:.0f}, "
          f"all of the base = {ALL_BASE_HOURS} h = {per_hour * ALL_BASE_HOURS:.0f}\n")
    print("| player | week the base is done | " + " | ".join(f"{w} weeks" for w in weeks_list) + " |")
    print("| --- | --- | " + " | ".join("---" for _ in weeks_list) + " |")
    for name, h in PROFILES.items():
        print(f"| {name} | {ALL_BASE_HOURS / h:.1f} | " + " | ".join(f"{per_hour * h * w:.0f}" for w in weeks_list) + " |")


def print_first_version(players_list=(2, 4, 8), per_player=6):
    print("\n### first version (a match): mushrooms full at the start = players x", per_player)
    for n in players_list:
        m = n * per_player
        print(f"{n} players: {m} mushrooms, {m * PIECES} rock salt, {m * PIECES / n:.0f} each if shared evenly")


if __name__ == "__main__":
    print("crop full bag: 30 crop =", 30, "amore, weight", 15 + TOOLS_WEIGHT, f"speed x{speed_for(15 + TOOLS_WEIGHT):.2f}")
    print("rock full bag: 30 rock =", 90, "amore, weight", 45 + TOOLS_WEIGHT, f"speed x{speed_for(45 + TOOLS_WEIGHT):.2f}")
    ph = {}
    for o in (OLD, SAFE, BOLD):
        ph[o["name"]] = table_runs(o)
    print_salt()
    print_first_version()
    for o in (SAFE, BOLD):
        print_season(o, ph[o["name"]])
    PLANT_SELL = 1.15
    print("\npersistent island (half the plants to the pedlar at 1.3x):",
          ", ".join(f"{o['name']} {sum(SHARE[a] * (run(a, 'lo', o)['ph'] + run(a, 'hi', o)['ph']) / 2 for a in AREAS):.0f}/h"
                    for o in (SAFE, BOLD)))
