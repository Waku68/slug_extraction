# 拠点「セノーテの洞窟」のモデリング・プロンプト（Argon 向け）

Oct 17, 2026（午後に2版目：わくさんの絵で2階建てに変えた）

わくさんと決めた拠点の構造（`../spec.md` 10章「拠点の構造」）を、Argon（Google Antigravity）に渡すプロンプトにしたもの。下の **English prompt** をそのまま貼る。日本語訳はわくさんが読むためのもので、中身は同じ。

- わくさんの絵：`reference/base_sketch_oct17.webp`（上が北。1階と2階の見取り図）
- 参考の3D：`reference/cenote_base_viewer.html`（ブラウザで開くと回して見られる。数字はこのプロンプトと同じ）
- 部屋ごとの家具・器具とマテリアル：`base_furniture.md`（Step 2 のうち、家具と器具はこちらのプロンプトで頼む）
- 決めた経緯：`../discussions/base_layout_discussion.md`、`sortie_launch_discussion.md`、`flying_carrier_discussion.md`、`base_storage_discussion.md`
- 1版目（3段のらせん坂、鳥が天井の穴から入る）で Argon が作った灰色のラフは `Blender/Base/cenote_base_gen.py` にある。2版目は、その道具（関数、マテリアル、書き出し）を使い回して、並びを作り直す

---

## English prompt

```
You are modeling the player's home base for "Slug Extraction PvP" (Unity 6000.5 URP).
Read AGENTS.md at the project root first, then .claude/skills/modeling/SKILL.md and
.claude/skills/modeling/shapes/cave_room.md. Follow every rule in AGENTS.md
(1 unit = 1 m, smooth normals, no painted textures, one material per part, script-built in Blender).
Reply to the user (waku) in short, plain Japanese.
Also look at waku's floor-plan sketch: docs/modeling/reference/base_sketch_oct17.webp
(north is up; top drawing = ground floor, bottom drawing = upper floor).

## This is version 2 - what changed from version 1
You already built a grey rough of version 1 in Blender/Base/cenote_base_gen.py. Keep its helpers,
materials and export code, but rebuild the layout:
- 3 levels + a 2-turn spiral ramp  ->  2 floors: a ground floor and a horseshoe gallery (upper floor),
  joined by one ramp along the west wall.
- The pool is no longer in the centre: it moves east. The map table sits west of centre.
- The ceiling hole is smaller (2 m) and sits above the pool. It is for light and rain only.
  The bird no longer flies into the cave.
- The bird sets the basket down on a pier outside the sea mouth.
- The deck moves up to the gallery level and sticks out to the south, east of the pier.
  The observatory grows on that deck.
- Rooms are reassigned (table below). The resting crevices and the separate storage room are gone.
- The bird's nest moves outside, onto the cliff above the sea mouth.
- New: a rim on the map table, a woven rug under it, storage baskets, salt bins, a "pile" mesh.

## What it is
A cave home carved into a small white limestone islet in the middle of a tropical archipelago.
Inside is one wide, dome-roofed hall (like a Mexican cenote) with a ground floor and a horseshoe
gallery around the open middle. A small round hole in the roof lets in a shaft of light, or a
column of rain that falls into a pool on the ground floor. The players are slugs (body 1.1 m long,
eye height 0.47 m). They come home in a woven basket carried by a giant bird, which sets it down on
the pier. They walk in through the sea mouth and turn their bags upside down onto the map table.
It should feel like a cozy, hand-built secret hideout: rough natural rock outside, careful
driftwood-and-vine craftsmanship inside. Mood: rainy, damp, warm oil-lamp light.
Pop silhouettes, realism 0.2.

## Coordinates (Blender, Z up)
- Origin: sea level, directly under the centre of the hall.
- North = +Y = 12 o'clock (the back of the cave). South = -Y = 6 o'clock (the sea side).
  East = +X = 3 o'clock, west = -X = 9 o'clock. Clock directions are seen from above.
- Ground floor top: Z = 2.0 m. Gallery floor top: Z = 4.6 m.

## Islet (exterior)
- White-grey limestone (uplifted coral reef): rounded, pitted, eroded; dark wet streaks near water,
  a few shell fossils, small stalactites at overhangs. Low tropical scrub and moss on top.
- About 22 x 18 m, top at about Z = 9.5 m, steep cliffs down to the sea.
  Keep at least 1.5 m of rock around the hall, the tunnels and the rooms everywhere.

## The hall
- Lathe profile around the origin, radius r at height Z (interior surface):
  Z 2.0 -> r 4.4 | Z 3.0 -> 4.8 | Z 4.6 -> 5.0 | Z 6.0 -> 4.8 | Z 7.0 -> 4.0 | Z 7.8 -> 2.8 |
  Z 8.3 -> 1.2 | Z 8.5 -> 0 (closed dome). Make the wall irregular (+-0.3 m), not a perfect lathe.
- Roof hole: a vertical shaft 2.0 m across, centred at (X 1.8, Y 0), from the dome up through
  the top of the islet. Light and rain only.
- Ground floor (Z 2.0): flat-ish limestone.
  - Pool: centre (X 1.8, Y 0), radius 1.6 m, 0.6 m deep, directly under the roof hole.
    Players jump into it from the gallery to get down fast.
  - Map table: centre (X -1.6, Y 0.6), round, diameter 2.2 m, height 0.38 m, with a low rim
    (0.06 m) so things do not roll off. The top is a separate object (material CenoteBase_Map).
    Nothing above it but the open hall.
  - Rug: a flat woven-vine mat, 3.0 x 2.8 m, under and around the map table (0.03 m thick).
  - Lift (tsurube, well-sweep): at 5 o'clock, 3.2 m from the centre, between the table and the pool.
    Two woven baskets (d 0.85 m) on one vine rope over a wooden pulley on a beam at about Z 7.0.
    When one basket is on the ground floor, the other is at the gallery's south end (Z 4.6).

## Upper floor: the gallery
- A horseshoe-shaped driftwood gallery hugging the wall at Z 4.6, running clockwise from
  11 o'clock (where the ramp arrives) over 12 and 3 o'clock to 5 o'clock (where the lift arrives).
  Outer edge at the wall, inner edge 3.4 m from the centre (about 1.6 m wide).
  Low railing of woven vines (0.4 m) on the inner edge. Held by diagonal struts into the wall;
  no posts standing in the pool.
- The open side (5 o'clock -> 11 o'clock, the west and south-west) has no gallery: from the
  map table you look up into the full height of the hall.
- Diving plank: at about 3:30 o'clock, a gap in the railing and a driftwood plank (0.6 m wide)
  sticking out 0.7 m over the pool. Its tip must be over deep water and outside the rain column.
- 2-3 small windows (about 0.35 x 0.45 m) through the outer wall between the rooms, for glimpses
  of the sea.

## Ramp
- One driftwood ramp along the west wall, from the ground floor at 8 o'clock (Z 2.0) clockwise
  to 11 o'clock, where it joins the gallery (Z 4.6). Width 1.0 m, about 20-25 degrees,
  headroom at least 1.0 m, low vine railing on the inner side.

## Rooms
Each room = a horizontal niche dug into the wall, about 1.6 m deep. Wet rooms on the ground floor,
dry rooms on the gallery. Dry rooms have a thin aged-copper strip across the threshold (slugs avoid
copper: it marks a dry, dangerous room).
| Room | Where | Floor Z | Size (w x h) | Notes |
| Trade corner: wholesaler counter, balance scale, plant baskets | ground, 6 o'clock (just east of the sea mouth) | 2.0 | 2.6 x 1.5 | the counter faces the sea mouth; 6 M storage baskets for plants along the back wall |
| Nursery (not a niche) | ground, the pool's north rim, around (X 1.8, Y 2.0) | 2.0 | 3 planter boxes 0.8 x 0.5 | at the edge of the light shaft, gets rain splash; moss |
| Dye pools | ground, 4 o'clock, east wall under the gallery | 2.0 | 2.0 x 1.2 | 3 small shallow pools fed by a little channel from the main pool |
| Collection: specimen shelf + display frames | gallery, 12 o'clock | 4.6 | 2.6 x 1.4 | faces the top of the ramp; visible from the map table below |
| Workshop | gallery, 1:30 | 4.6 | 2.0 x 1.2 | workbench; the player's guns, armour and bags hang on the back wall |
| Drying room (dry) | gallery, 3 o'clock | 4.6 | 2.0 x 1.2 | drying racks; copper strip |
| Salt refinery + salt store (dry) | gallery, 4:15 | 4.6 | 2.2 x 1.2 | a millstone, 3 salt bins; copper strip; right next to the lift's upper landing |

## Sea mouth, pier, deck and nest (south)
- Sea mouth: a big arch through the wall at 7 o'clock, at ground-floor level, 2.8 m wide, 2.5 m high.
  A short tunnel runs straight south (-Y) about 2.5 m to the outside.
- Pier: driftwood on posts, top at Z 2.0, continuing straight south from the tunnel,
  3.0 m wide and 6.0 m long, low railing on one side only. The bird sets the basket down on the
  outer half (keep a clear circle 2.0 m across there). A merchant's small boat ties up at its side.
- Deck: at gallery level (Z 4.6). From the gallery's south end, a short opening through the wall at
  about 5:30 o'clock (1.4 m wide, 1.6 m high) leads out onto a driftwood deck sticking out south over
  the sea, about 3.0 x 3.0 m, with a low railing and diagonal struts down to the cliff. It is east of
  the pier and about 0.8 m away from it sideways, looking down on the pier and the sea.
- Observatory (separate part, it grows on the deck): a wind vane, a rain gauge, a brass telescope,
  and a small leaf roof over the back half of the deck.
- Bird's nest: outside, on a ledge of the cliff above the sea mouth, at about Z 7.5; woven driftwood
  and vines, about 1.4 m across. Seen from the pier and the deck.

## Props
- Bird basket: SQUARE (Oct 19, waku), 2.4 x 2.4 m, walls 0.4 m, open top; must hold 4 slugs (1.1 m long) and their bags. Final shape: `base_furniture.md` (CenoteFurniture_BirdBasket).
- Oil lamps: brass bracket lamps with a warm orange flame, about 10 of them: map table, ramp,
  gallery, rooms, pier. No electric light.
- Storage baskets: woven, open top. S = d 0.3 m, h 0.25 m. M = d 0.5 m, h 0.35 m.
- Salt bins: low driftwood boxes 0.5 x 0.4 x 0.3 m.
- Pile: one soft rounded mound, d 1.0 m, h 0.5 m, origin at the bottom centre. Unity scales it inside
  a basket or bin to show how full it is.

## Materials (muted, rain-wet; nothing more saturated than the slugs)
CenoteBase_Limestone #D9D2C0, CenoteBase_LimestoneWet #A9A390, CenoteBase_Driftwood #B9AE98,
CenoteBase_Vine #6B6A45, CenoteBase_Moss #6E7C4A, CenoteBase_Water #3F7F86,
CenoteBase_Copper #B07A4E, CenoteBase_Leaf #4E5F3A, CenoteBase_Brass #A8873E,
CenoteBase_LampFlame #FFB860 (emissive), CenoteBase_Rug #8C7F5E, CenoteBase_Pile #C9C2B0,
CenoteBase_Map (placeholder, flat).

## Do NOT
- No human-made materials: no bricks, concrete, glass panes, metal sheets, nails you can see.
- No glowing crystals, no fantasy magic, no slime-built structures.
- No dead ends a slug cannot climb: steps over 0.45 m or slopes over 40 degrees on walking paths.
- Do not use the visual mesh as the collider. Make simple colliders (floor, ramp segments,
  gallery, pier, deck) as separate objects named *_COL.

## Deliverables
1. Blender script Blender/Base/cenote_base_gen.py (re-running it rebuilds and re-exports everything)
   and Blender/Base/CenoteBase.blend.
2. FBX files in Assets/Art/Models/Base/CenoteBase/, one per part, so Unity can show rooms one by one
   as the base is upgraded:
   CenoteBase_Shell (islet + carved hall + roof hole + sea mouth + tunnels + windows),
   CenoteBase_Floor (ground floor, pool basin), CenoteBase_Rug, CenoteBase_Ramp, CenoteBase_Gallery
   (with railing and diving plank), CenoteBase_Room_Trade, CenoteBase_Nursery, CenoteBase_Room_Dye,
   CenoteBase_Room_Collection, CenoteBase_Room_Workshop, CenoteBase_Room_Drying,
   CenoteBase_Room_Refinery, CenoteBase_Lift, CenoteBase_Pier, CenoteBase_Deck,
   CenoteBase_Observatory, CenoteBase_Nest, CenoteBase_Basket, CenoteBase_MapTable, CenoteBase_Lamp,
   CenoteBase_StorageBasket_S, CenoteBase_StorageBasket_M, CenoteBase_SaltBin, CenoteBase_Pile,
   plus the *_COL colliders.
   Export settings: copy export_fbx from Blender/Harvest/rocksalt_gen.py.
3. Triangle budget: Shell up to 20,000; each other part 300-4,000; small props 300-1,500.

## How to work
Step 1 - grey rough only (no colours, no props detail). Put slug proxies (capsule 1.1 m long,
0.5 m tall) at the map table, on the ramp, on the gallery, on the pier and on the deck.
Show waku 5 images:
(a) a wide cutaway from above-south-west with the roof and the south half of the islet hidden,
(b) slug eye height (0.47 m) at the map table looking east at the pool, the gallery and the roof hole,
(c) from the gallery at the top of the ramp (11 o'clock) looking across the hall to the lift,
(d) from the outer end of the pier looking back at the sea mouth, with the deck up on the right,
(e) straight down from above with the roof hidden (to compare with waku's sketch).
List any number you changed and why. Stop and wait for waku's OK.
Step 2 - after OK: materials, props, lamps, details. Shoot the same 5 views again.
Step 3 - export, import into Unity with the Slug/Toon shader (see Assets/Art/Editor/WorldLook.cs),
and add a line to the Art/Models row of docs/README.md.
```

---

(Japanese translation for the owner: `base_cenote_ja.md`. Do not read it.)
