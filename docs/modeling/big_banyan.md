# Big banyan (model sheet)

Oct 20, 2026. Japanese translation for waku: `big_banyan_ja.md` (do not read it).
No reference picture. The grey-island stand-in (a cylinder with a flat ball on top, 35 m) is what
this replaces; do not copy it.

## 1. One line

The rain forest's landmark: one giant banyan whose wide, flat umbrella of leaves rises far above the
forest. From the mountain and the mesa it must read as "an umbrella over the forest" (the other
landmarks are a peak, a crater, a box and a ring). From slug eye height under it, it is a dim hall of
root pillars. Three root ramps lead up to a lookout on top of the leaves, where a slug can see the
whole island. It is wild nature: nothing in or on it is made by slugs.

## 2. Size

- Height 40 m to the top of the leaves. About 85x a slug's eye height (0.47 m); for a human, a tree
  about 145 m tall. The forest trees around it are about 7.5 m (the big banyan is about 5x taller).
- Crown about 80 m across (roughly round, edge a little ragged). Underside of the leaves at about 26 m
  near the trunk, drooping to about 20 m at the rim. Leaves are about 14 m thick at the middle,
  about 6 m at the rim: a flat-topped dome, much wider than tall.
- Origin: on the ground at the centre of the trunk. Up = Blender +Z. Blender +Y = north on the map.
  Roots, trunk and pillars go 2 m below z = 0 so the tree can sit on uneven ground.
- Where: the east rain forest (Claude places it in Unity; the east river does NOT run under it).
  The trail from the river shallows arrives from the south-south-east (bearing about 150°).
- Comparison slug for screenshots: capsule 1.1 m long, 0.5 m high. Slugs crawl up slopes to 50°
  and climb steps up to 0.45 m.

## 3. Shape words

- **傘 (umbrella)**: the silhouette is a wide flat dome on a short, thick trunk. Exaggerate the width.
- **ねじれた束 (twisted bundle)**: the trunk is not one cylinder but 8-12 thick roots fused and twisted
  together, with deep flutes between them.
- **柱の林 (pillar forest)**: under the crown, hanging roots that reached the ground and grew into
  pillars. Mostly straight and vertical, a few leaning or forked.

## 4. Parts

Hierarchy: `BigBanyan_Root` (empty, at the origin) → the objects below. Flat facets like the
forest trees (Slug/Toon `_Facet` 1). All thin parts are closed meshes.

| Object | Size / place | Material |
| --- | --- | --- |
| `BigBanyan_Trunk` | Fused bundle of 8-12 root-trunks, footprint about 12 m across at the ground, narrowing to about 7 m at 10 m, splitting into the limbs at 16-20 m. At the base, 4-6 flutes form shallow nooks 1-2 m deep and 1.5-3 m wide (a slug fits; not caves) | `BigBanyan_Bark` |
| `BigBanyan_Limbs` | 6 main limbs from the fork, reaching out near-horizontally to 28-36 m from the centre, rising a little then drooping (about 20-26 m high). About 2.5 m thick at the fork, 0.8 m at the tip, with side branches up into the leaves | `BigBanyan_Bark` |
| `BigBanyan_Crown` | 10-14 big faceted leaf clumps (like the forest trees' clumps, but huge) forming the dome. No single leaves. 2-3 gaps 5-8 m wide let light patches through; the three climb routes pass up through gaps. Top faces one colour, undersides and inner faces darker | `BigBanyan_LeafTop` / `BigBanyan_LeafUnder` |
| `BigBanyan_Pillars` | 80-120 pillars, 0.6-1.8 m thick (thicker near the trunk), each from the ground up into a limb or branch at 20-26 m (never ending in the air). Grouped in 6 groves under the 6 limbs plus a ring around the trunk. Inside a grove, neighbours are 3-5 m apart (gaps at least 1.5 m so a slug can pass and turn). Between groves, aisles 6-8 m wide. Bottoms flare into the ground | `BigBanyan_Bark` |
| `BigBanyan_HangingRoots` | 60-100 thin roots 0.12-0.3 m thick, in curtains under the limbs, ending 1-8 m above the ground. Visual only. They must not cross a climb route's walking surface | `BigBanyan_RootYoung` |
| `BigBanyan_Climb_A`, `_B`, `_C` | The three ways up, about 120° apart: A at bearing 150° (SSE, toward the river trail), B at 270° (W), C at 30° (NNE). Each starts on the ground in an aisle 26-34 m from the centre: a giant leaning root up to a limb, along the limb's top toward the trunk, then up a curving branch through a crown gap to the lookout. Walking top at least 1.6 m wide and fairly flat across. Slope mostly 25-35°, never over 45°. Bumps at most 0.3 m. A flat landing (3 x 2 m) every 15 m or so of length. Each route about 75-90 m long. The three routes never touch or cross; they meet only at the lookout. Top faces moss, sides bark | `BigBanyan_Bark` + `BigBanyan_Moss` |
| `BigBanyan_Lookout` | The knot at the top of the fork, pushed up through the leaves: an irregular bark platform about 8 x 6 m at 38-39 m, slope at most 10°, its edge 1-2 m above the surrounding leaf tops so the view is open all round. Edges rounded and convex: from the middle you see far away, not straight down. No railing. Routes A, B, C arrive at three different edges | `BigBanyan_Bark` + `BigBanyan_Moss` |
| `BigBanyan_Slide_1`, `_2` | The fast way down: 2 smooth roots from the lookout edge to the ground, 50-60° (steeper than slugs can climb, so down only), a U-trough 1.2 m wide with 0.3 m lips. Slide 1 at bearing 90° (E, between A and C), Slide 2 at 210° (SSW, between A and B). Each about 45-50 m long, landing in an aisle about 30 m from the centre | `BigBanyan_RootYoung` |

Empties (positions for Claude's code, under the root): `BigBanyan_ClimbStart_A` / `_B` / `_C`,
`BigBanyan_Lookout`, `BigBanyan_SlideTop_1` / `_2`, `BigBanyan_SlideEnd_1` / `_2`, and
`BigBanyan_FruitSpot_01` to `_10` (where the storm fruit hangs: on branch undersides at 18-26 m,
10-32 m from the centre, each above open ground, not above a pillar, the trunk or a route).

Colours (sRGB, matched to the muted forest trees; Claude sets the real materials in Unity):

| Material | Colour | Use |
| --- | --- | --- |
| `BigBanyan_Bark` | #554740 dark grey-brown | trunk, limbs, pillars, route sides |
| `BigBanyan_LeafTop` | #66804C olive green | leaf tops |
| `BigBanyan_LeafUnder` | #404C33 dark olive | leaf undersides and inner faces (reads as shade) |
| `BigBanyan_Moss` | #73804C yellow-olive | walking tops of the routes and the lookout ("you can crawl here") |
| `BigBanyan_RootYoung` (accent) | #7A6A58 pale brown | hanging roots and the slides ("this one is different") |

## 5. States

One state. The storm fruit is a separate object (not this job); Unity hangs it at the fruit empties
during heavy rain. Dry-season colours, if any, are a material swap in Unity later.

## 6. Do NOT

- Not one straight cylinder trunk with a ball on top. Not a pine or oak shape: it must be wide and flat.
- No paths along the branches across the crown, no walkable leaves. Only the 3 routes, the lookout
  and the 2 slides are surfaces to stand on.
- No rooms or hollow inside the trunk (it is not a house). Base nooks stay shallow (2 m at most).
- Nothing slug-made: no ladders, ropes, planks, steps, signs, lamps. No flowers, no fruit in this job.
- Not brighter or more saturated than the forest trees. No single leaves, no leaf cards with textures.
- No pillar or root that floats or ends in mid-air above 8 m. No pillar standing on a route.
- No painted textures (colour per material only).

## 7. Deliverables

- Script `Blender/Landmarks/big_banyan_gen.py` plus part files (e.g. `big_banyan_roots.py`,
  `big_banyan_crown.py`), each under 500 lines; print only object names, triangle counts and errors.
  `Blender/Landmarks/big_banyan.blend`.
- FBX `Assets/Art/Models/Landmarks/BigBanyan/BigBanyan.fbx`, all parts nested under the root.
  Nested export: `bake_space_transform=False, axis_forward='Y', axis_up='Z'`, and Bake Axis
  Conversion ON in Unity (same as `Blender/Base/cenote_furniture_gen.py`).
- Triangles: at most 20,000 per object, about 60,000 for the whole tree. Smooth normals off
  (flat facets) is fine here; it matches the forest trees.
- Far version `BigBanyan_LOD1` in the same FBX (hidden): trunk, 6 limbs and the dome as a few
  clumps, no pillars or hanging roots, at most 6,000 triangles. Seen from the mountain and the mesa.
- Collision, separate objects in the same FBX: `BigBanyan_COL_Walk` (one mesh: the route tops,
  landings, lookout, slide troughs and the trunk base; at most 4,000 triangles; Unity uses it as a
  non-convex MeshCollider) and `BigBanyan_COL_Pillars` (each pillar as a 6-sided prism; at most
  3,000 triangles). Leaves and hanging roots get no collision.
- Steps: read-back sheet → grey rough → colour → Unity, waku's ✅ between each
  (`docs/modeling/README.md` §3-4). Grey rough shots: A (slug eye height 0.47 m, standing in an aisle
  15 m from the trunk, looking at the trunk through the pillars, with a climb root in view),
  B (side view of the whole tree with the comparison slug and a 7.5 m cone as a forest tree at its
  foot), C (from above at 45°; in the rough only, colour the routes A red, B green, C blue, slides yellow).

## 8. Decided (Oct 20, waku)

1. Height 40 m, crown 80 m (spec said 40-50 m and 100 m; 40 / 80 keeps the umbrella clear and the
   cost down).
2. Three climb routes (not one or two).
3. The top is a lookout above the leaves (open, sees far, can be seen), not a hidden nook in the leaves.
4. Pillars 3-5 m apart inside the groves (close-quarters fighting under the tree).

## 9. Open questions

1. Two slides (east and south-south-west) - or one beside each route (three)?
2. 2-3 light gaps in the crown: keep, or close the crown fully (darker floor)?
