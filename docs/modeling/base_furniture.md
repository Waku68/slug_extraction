# 拠点「セノーテの洞窟」の家具・器具のモデリング・プロンプト（Argon 向け）

Oct 18, 2026

> **Outdated (Nov 2, 2026): do not build from this sheet.** Furniture for the previous base. The base is being remade by Claude in Unity code; see `../spec.md` §10. 前の拠点の家具の票（Nov 2 から使わない）。

拠点の洞窟（`base_cenote.md`。Argon が灰色のラフを作った）の中に置く、部屋ごとの家具・器具と、細かいマテリアル。下の **English prompt** をそのまま貼る。日本語訳はわくさんが読むためのもので、中身は同じ。

- 決めた経緯：`../discussions/base_furniture_discussion.md`（Claude がペルソナと話して決めた。わくさんの判断：段階の材料に島の大物を使う、秤は演出だけ）、`../discussions/base_storage_discussion.md`・`_2.md`（物の扱い、籠の3つの仕切り）
- 洞窟のほうのプロンプトの Step 2（小物・灯・細部）の、家具と器具の部分をこのプロンプトにした。洞窟の Step 1 のラフにわくさんの OK が出てから頼む
- 洞窟の部屋の位置は、Argon が楕円に合わせて動かしている（`../worklog.md` Oct 18）。このプロンプトでは部屋ごとに「その部屋の中の座標」で書き、洞窟の中には目印（アンカー）で置く

---

## English prompt

```
You are modeling the furniture and equipment inside the player's home base ("CenoteBase") for
"Slug Extraction PvP" (Unity 6000.5 URP). You already built the cave itself (grey rough, version 2,
Blender/Base/cenote_base_gen.py). This job fills the rooms.
Read first: AGENTS.md, docs/worklog.md (post your row in "いま作業中" before you start, and add a log
entry when you finish), .claude/skills/modeling/SKILL.md, docs/modeling/base_cenote.md.
Follow every rule in AGENTS.md (1 unit = 1 m, smooth normals, no painted textures, one material per
part, script-built in Blender). Reply to waku in short, plain Japanese.

## Rules for every object
- Only things that can be found on a tropical island after humans are gone: driftwood, vines,
  palm leaves and coconut fibre rope, woven pandanus leaves, bark cloth (tapa), sea shells (giant clam,
  big conch, bivalves, small whelks), limestone, black lava stone, obsidian, coral rubble, beach
  pebbles, whale bone, coconut shells, unglazed clay, moss, charcoal, squid ink.
- Metal only as small inherited parts: brass (wall lamps, the telescope, gas-cylinder mouths) and
  copper (threshold strips of the dry rooms). Never shiny or new: a little green patina, and one or two
  places mended with vine. No glass, no nails, no sheet metal, no bricks, no plastic.
  No glowing crystals, no magic, nothing built from slime.
- Few objects. Each room starts (stage S1) with ONE hero object plus at most TWO small ones.
  Each later stage (S2, S3) adds exactly ONE object or one tight group.
- The hero is big, simple and exaggerated (pop silhouette), stands at the MOUTH of the niche (not deep
  inside) so it can be seen from across the hall and from the ground floor, and is the thing the
  player interacts with.
- Built-ins that exist from the start (shelves, ledges, counters, the specimen cubbies) are carved from
  the cave rock: separate objects named <Room>_Carved, material CenoteBase_Limestone.
  Everything added later is driftwood. The player should read "rock = was here, wood = I added it".
- Keep found shapes recognisable: in every room at least one object is plainly what it was found as
  (a giant clam shell used as a bath, a conch used as a lamp, a coconut shell bowl, a standing bivalve
  shell as a book rest).
- Wear and repairs, 1-2 per room, as GEOMETRY (no textures): a dip worn into a surface, knife grooves,
  a crack lashed with vine, a patch on cloth.
- One accent colour per room so rooms are told apart from afar (listed per room).
- Text and pictures (map, forecast, tags) are added in Unity: model only blank boards and give each
  board face its own object.
- Thin parts (cloth, leaves, nets) still have thickness and are closed meshes. Smooth normals.
- Slug scale: body 1.1 m long, eye height 0.47 m, stretchy arms. Work surfaces are 0.3 m high;
  the map table is 0.38 m.

## How the work is split (important)
- Write a NEW script Blender/Base/cenote_furniture_gen.py and Blender/Base/CenoteFurniture.blend.
  It imports the kit and room objects into the cave scene for preview, but builds them separately.
- In cenote_base_gen.py, add one Empty per room, named CenoteBase_Anchor_<Room>, at the floor of the
  niche mouth (centre of the line where the niche floor meets the hall or gallery floor), rotated so its
  local +Y points INTO the niche. For free-standing rooms (MapTable, Nursery, Deck, Pier) see below.
  Change nothing else in the cave script for this job.
- Each room is modeled in its own local coordinates = the anchor: +Y into the niche, +X to the right
  when you look into the niche from the hall, +Z up, origin on the floor at the niche mouth.
  The niche itself (hollow in the rock) belongs to the cave job; it is about w wide, h high and
  1.6 m deep (Y 0 to 1.6). Sizes per room are below.
- Stages: each room is one parent object CenoteFurniture_<Room> with children <Room>_S1,
  <Room>_S2, <Room>_S3 (S2 and S3 contain only what that stage adds) and <Room>_Carved.
  Unity switches the stage children on and off.
- Moving parts get an Empty at the pivot named <Room>_<Part>Pivot with the moving mesh parented to it.
- This job replaces these placeholder parts of the cave job: CenoteBase_MapTable, CenoteBase_Lamp,
  CenoteBase_Observatory, CenoteBase_Nest, CenoteBase_Basket, CenoteBase_StorageBasket_S/M,
  CenoteBase_SaltBin, CenoteBase_Pile, and any props inside CenoteBase_Room_*. The rock niches,
  floors, ramp, gallery, pier and deck platforms, lift and pool stay in the cave job.

## The kit (build this first; rooms are mostly kit pieces plus their hero)
| # | Part | Shape | Material |
| 1 | Plank | long and short driftwood planks, slightly warped, rounded ends | Driftwood |
| 2 | Post | straight post, and a Y-shaped forked post | Driftwood |
| 3 | Disc | a driftwood log slice (pulley wheels, moon disc) | Driftwood |
| 4 | Vine loop and cord | railing cord, hook loops, and a "repair wrap" (5-8 turns of vine) | Vine |
| 5 | Coir rope | thick rope | Coir |
| 6 | Clay jar | large (d 0.35, h 0.45) and small (d 0.2, h 0.25), with lids | Clay |
| 7 | Baskets | round S (d 0.3, h 0.25), round M (d 0.5, h 0.35), long 3-compartment basket (0.7 x 0.35 x 0.2, two vine dividers; on the front lip, 1, 2 and 3 carved notches above the three compartments) | Vine, Rug (lining) |
| 8 | Salt bin | low driftwood box 0.5 x 0.4 x 0.3 on short legs (bottom 0.3 m above the floor: salt is poison to slugs), with a lid | Driftwood, Rug |
| 9 | Tags | a thin driftwood tag, and a bark-cloth tag tied with vine | Driftwood, BarkCloth |
| 10 | Conch lamp | a whelk shell lying on its side, oil inside, flame at the opening (floor lamp) | Conch, LampFlame |
| 11 | Brass wall lamp | bracket lamp, green patina, one vine-mended spot | Brass, LampFlame |
| 12 | Pile | one soft rounded mound, d 1.0, h 0.5, origin at bottom centre; Unity scales and recolours it | Pile |

## Rooms
Positions are in room-local metres (X right, Y into the niche, Z up).

### 1. MapTable - ground floor, free-standing. Accent: the map.
Anchor: on the floor under the table centre, +Y = north. (The cave job has the table at about X -1.8, Y 0.5.)
- HERO table: round, d 2.2, top at Z 0.38, low rim 0.06 high. ONE leg: a big driftwood root stump
  whose roots spread over the floor like octopus arms (about 1.6 m across). Material DriftwoodDark.
  The top is a separate disc object MapTable_Top with material CenoteBase_Map.
- Meal ring: a ring-shaped ledge 0.12 below the rim, 0.18 wide, all around the table. The party eats
  here before leaving, looking at the map. 4 coconut-shell bowls on it.
- Loose small objects: 4 slug-shaped pieces carved from driftwood (0.1 long), 6 pins (a thorn with a
  small cloth flag), a few salt grains.
- Rug: woven pandanus mat 3.0 x 2.8 x 0.03 (material Rug), centred under the table.
- Wear: one place on the rim worn a little lower.
- No stages.

### 2. Trade - wholesaler counter, balance and plant baskets. Ground floor niche, 2.6 w x 1.5 h. Accent: the plants.
S1:
- HERO balance at the mouth, about X +0.7, Y -0.2: a Y-shaped driftwood post 0.9 high, a driftwood
  beam 1.1 long, two bivalve-shell pans (d 0.35) hanging from the beam ends on vine cords.
  Rig: Trade_BalancePivot at the beam centre (beam rotates about local X); Trade_PanPivot_L and _R at
  the beam ends so the pans can stay level. It is only a show: when the player sells, the wholesaler
  puts salt bars on a pan until it balances. Beside it a stack of 4 salt bars (each 0.2 x 0.07 x 0.05,
  material Salt, wrapped with a vine band).
- Counter: carved rock 1.6 x 0.5, top 0.3, at about X -0.4, Y 0.3, with a small palm-thatch awning
  above. On it the ledger: a thick book of bark-cloth pages bound with vine (0.3 x 0.22 x 0.08) on a
  standing bivalve shell used as a book rest, a feather quill (from the carrier bird) and squid ink in a
  small whelk shell.
- Basket racks: carved rock ledges, two tiers, along the back and both side walls, with 12 basket
  slots: 8 long 3-compartment baskets (kit 7) and 4 round M baskets. Each basket gets one Pile object
  per compartment, and a vine cord in front where one sample plant hangs, with a driftwood tag above.
  (Baskets appear in the game one by one as new plant kinds are brought home: make each basket its
  own object, Trade_Basket_01..12.)
S2: a woven extension band (0.06 high) on top of every basket rim, as one object.
S3: a second band on top of that.
Wear: the beam mended with a vine wrap; a worn dip in the counter top.

### 3. Nursery - ground floor, free-standing on the pool's north rim. Accent: moss.
Anchor: floor at the middle planter, +Y = away from the pool (north).
S1:
- HERO 3 planter boxes (driftwood frame, dark soil, 0.8 x 0.5 x 0.25) on a gentle arc around the pool:
  X -0.9, 0, +0.9, each turned to follow the arc (about +-12 deg). Moss around them.
  Nursery tags: small driftwood stakes with one hole in the head, stuck in the soil.
- A split-driftwood gutter 1.2 long from the pool edge (Y -0.6) to the boxes, and a coconut-shell
  ladle. (On rainy days the pool rises and water runs down the gutter - the pool is the cave job.)
- Compost pen: a low woven-vine fence 0.7 x 0.7 x 0.3 at X +1.6, with a Pile inside (material Compost).
S2: +2 boxes continuing the arc (X about +-1.8). S3: +2 more (X about +-2.6).
Wear: one cracked box frame lashed with vine.

### 4. Dye - ground floor niche under the gallery, 2.0 w x 1.2 h. Accent: the dye water.
S1:
- HERO soaking bath at the mouth (Y about -0.2): one valve of a giant clam, 1.3 x 0.8, deep wavy ribs
  on the outside, smooth inside (ClamShell outside, ClamInner inside). A slug can lie in it.
  The water surface is a separate object Dye_Water_Main (material DyeRed; Unity changes it).
- One small giant-clam pool (d 0.5) at the back left, with a little channel to the bath; water object.
- A black lava mortar with a driftwood pestle, flower mash inside (small lumps, DyeRed).
S2: three lidded clay jars (mordants: ash, rock salt, black salt). On each lid a small heap of what is
inside, told apart by shape: fine powder (Ash), white cubes (Salt), black cubes (BlackSalt).
S3: two more small clam pools, and a vine line between two posts with 3-5 dyed bark-cloth strips
(ClothRed, ClothIndigo, ClothOchre); on one post an arm to hang a gun or a bag over a pool.
(S3 is unlocked by bringing giant clams home from the island: make them look freshly found.)
Wear: the bath rim worn smooth and a little lower where slugs climb in.

### 5. Collection - specimen wall and display. Gallery niche, 2.6 w x 1.4 h. Accent: bark-cloth white.
S1:
- HERO specimen wall (carved): 20 cubbies (0.22 x 0.22, 0.15 deep) on the back wall in 5 columns x
  4 rows, a region sign carved above each column (a raindrop, a flower, a salt cube, a lava crack,
  a mountain). Each cubby has a bark-cloth back plate (Collection_Cubby_01..20_Back) and in front of
  it a thin charcoal cut-out silhouette plate (_Shadow, 5 mm proud) that Unity hides when the
  specimen is found. Leave the cubbies empty otherwise (specimens are the harvest models).
- Two display spots: a low carved rock plinth on the left (for a whole storm fruit), and one
  driftwood frame 0.4 x 0.5 on the right wall. Each with a bark-cloth tag tied with vine.
S2: two driftwood hooks for trophy gear, with tags. S3: two more driftwood frames.
Wear: one back plate patched.
Lamps: two brass wall lamps (kit 11) lighting the wall; at night the wall glows faintly when seen from
the ground floor.

### 6. Workshop - gallery niche, 2.0 w x 1.2 h. Accent: brass.
S1:
- HERO workbench at the mouth: a thick driftwood log split lengthwise, 1.6 x 0.6, top 0.3
  (DriftwoodDark), on two coral-rubble blocks as legs. Under the bench a large clay fermentation jar
  (ripe fruit ferments and gives off CO2) with a brass mouth and a thin vine-wrapped tube up to a small
  clamp on the bench where a gas cylinder is filled.
- Tool wall (back): a carved recess with vine loops holding a pickaxe, knife, mallet, fish-bone awl
  and shell plane.
- Gear wall (side): 4 driftwood pegs for the player's gun, armour, helmet and bag (no gear models), a
  carved low ledge 1.0 x 0.4 where the ready-to-go set lies, and a polished obsidian mirror 0.5 x 0.7
  in a driftwood frame.
S2: a small driftwood stand with a moss-ball press mould and a clamp for mending shell armour.
S3: a carving stand with a whole storm-fruit husk clamped in it (S3 is unlocked by bringing a whole
storm fruit home: make it look like the real fruit husk).
Wear: a worn dip and knife grooves in the bench top.
Lamp: one brass wall lamp.

### 7. Drying - gallery niche, dry room, 2.0 w x 1.2 h. Accent: copper.
S1:
- HERO drying rack at the mouth: a driftwood frame with a vine net shelf, 1.4 x 0.5, 0.5 high; bundles
  of dried plants hanging from 3 hooks under the niche ceiling.
- A clay pickling jar with a beach-pebble weight on the lid.
- Threshold: an aged copper strip (0.04 wide) across the floor at Y 0, and a wet-moss doormat just
  outside it (slugs re-wet themselves when they leave).
S2: the rack grows to 3 shelves, 1.0 high (one object with the added upper tiers).
S3: a big fan made from a single whole palm leaf, hanging from a pivot (Drying_FanPivot, swings
about local X; it moves faster in strong wind). The cave job adds a small vent hole in the back wall
behind it - just leave room.
Wear: a tear in the net mended with vine.

### 8. Refinery - salt refinery and salt store. Gallery niche, dry room, 2.2 w x 1.2 h. Accent: salt white.
How the salt is ground decides the ammo: crushed coarse = shotgun, ground fine = rifle,
carved from a lump = sniper. Keep the three tools clearly separate.
S1:
- HERO quern at the mouth: two black lava millstones d 0.6 (lower 0.15 thick on a low base, upper
  0.12 thick), a vertical driftwood handle. The upper stone is parented to Refinery_QuernPivot
  (rotates about Z).
- Crushing block: a flat lava slab 0.6 x 0.4 x 0.2 and a big mallet made from a driftwood root knob;
  on its end an amore mould (a driftwood block with 4 bar-shaped grooves), 3 finished salt bars wrapped
  with vine, and ammo pouches of bark cloth (round pouch = shotgun, long pouch = rifle).
- Salt store: 3 salt bins on legs with lids (kit 8) along the back wall, one each for coarse salt,
  black salt and rock salt, a Pile inside each. At the entrance a hook with 2 pairs of thick bark-cloth
  gloves (slugs handle salt only with gloves). Aged copper threshold strip.
S2: a driftwood vise with a shell blade for carving sniper rounds, and a small box of them.
S3: a second, bigger quern (d 0.9) with two handles, turned by two slugs. Its stone is cut from a raw
black-lava block brought home from the island: fresher, sharper edges than the old quern.
Wear: the quern handle mended with a vine wrap.

### 9. Deck - the observatory on the outside deck (gallery level). Accent: the wind-vane cloth.
Anchor: the centre of the deck floor, +Y = north (toward the cliff). The deck platform is the cave job.
Always there (no stage): a palm-thatch roof over the north half on two posts, and a bench made from a
half log.
S1: HERO forecast board: a black lava slab 0.8 x 0.5 in a driftwood frame on two legs, facing south
(its face is a separate object Deck_BoardFace for Unity text; the forecast is chalk-written like a
mountain hut notice). Beside it a rain gauge: a tall clay jar with a notched stick standing in it.
S2: wind vane: a 1.5 m pole topped by a driftwood carving of the carrier bird with a bark-cloth tail
(ClothRed); the bird and tail are parented to Deck_VanePivot (rotates about Z).
S3: a driftwood turntable (Deck_TurnPivot, rotates about Z) carrying a big conch (0.6 long) in a cradle
with its mouth to the sea (to hear distant thunder) and a brass telescope on a driftwood tripod; plus a
moon disc (driftwood disc d 0.3 with a crescent-shaped hole) mounted on the forecast board,
parented to Deck_MoonPivot (rotates about the board's normal). (S3 is unlocked by bringing a big conch
home from the island: make it look found, not carved.)
Wear: one patched piece of the deck railing (the railing itself is the cave job - add only the patch).

### 10. Pier - the face of the house. Accent: the noren.
Anchor: the pier root where the tunnel meets the pier (floor top, Z of the pier), +Y = north into the
tunnel; the pier runs toward -Y. (The cave job has the pier centre at about X -2.5.)
- HERO whale-rib gate: two whale ribs about 2.8 long, standing at the pier root edges (X about +-1.3),
  curving up and crossing above the centre at about Z 2.6, tied with coir rope. A landmark seen from
  the sea. Material WhaleBone.
- Noren: a bark-cloth curtain 2.4 x 0.8 with 4 vertical slits, hanging from a driftwood rod across the
  top of the sea-mouth arch at the tunnel's outer end (Y about +0.1, top at about Z 2.45). Material
  Noren: Unity recolours it to the colour the player dyed. It is the only place the player decorates.
- Two driftwood mooring stumps, a lamp post with a conch lamp on top.
- Feeder: a shallow driftwood dish on a post at the far end of the pier (fruit for the bird).
Separate objects with their own origins (bottom centre):
- CenoteFurniture_BirdBasket: a SQUARE basket (Oct 19, waku: the round one felt wrong), not round.
  Inside 2.4 x 2.4 m, walls 0.4 high, open top, floor about 0.06 thick. (Slug eye height 0.47 m; for a
  human that is a square room about 8.6 m on a side with chest-high walls about 1.4 m.) Four driftwood
  corner posts and a driftwood rail along the top of each wall; the walls between are woven vine.
  Walls stay below 0.45 so a slug can climb in from any side. Four coir ropes, one from each corner
  post top, rise to one thick braided-vine ring about 1.6 above the floor over the centre: the bird
  grabs this ring. It must hold 4 slugs (1.1 m long) with their bags and let them turn around.
  Origin bottom centre; sides face +-X and +-Y. Materials: Driftwood (posts, rails), Vine (walls,
  ring), Coir (ropes). Replaces the old round basket (d 1.6, h 0.6).
- CenoteFurniture_Nest: woven driftwood and vines, about 1.4 across, a few feathers inside.
(The merchants' outrigger canoes come later.)

## Lamps
Six brass wall lamps (kit 11) in all: Collection 2, Workshop 1, one on a driftwood pole beside the map
table, one on the gallery, one on the ramp. Every other light is a conch floor lamp (kit 10).

## Materials (Unity shader Slug/Toon)
Create these names in Blender with the base colour (Principled BSDF Base Color). In Unity, set them up
with SlugDesign.WorldLook.Apply(m, colour, rim, mottle, mottleColour, 12, dent, 70, doubleSided)
from Assets/Art/Editor/WorldLook.cs (same as Assets/Art/Editor/HarvestVisuals.cs does), and save them in
Assets/Art/Models/Base/Materials/ so the cave and the furniture share them. Muted, rain-wet colours;
nothing more saturated than the slugs (the dye water is the most saturated thing).
| Name | Used for | Colour | rim | mottle | dent | Notes |
| CenoteBase_Limestone | cave rock, carved built-ins, plinths, pebble weights | #D9D2C0 | 0.1 | 0.3 | 0.4 | |
| CenoteBase_LimestoneWet | wet rock near water | #A9A390 | 0.2 | 0.3 | 0.4 | |
| CenoteBase_Driftwood | floors, added furniture, posts | #B9AE98 | 0.05 | 0.25 | 0.2 | |
| CenoteBase_DriftwoodDark | workbench, tool handles, map-table leg | #8E7F68 | 0.1 | 0.25 | 0.2 | oiled by use |
| CenoteBase_Vine | vines, cords, baskets, dividers, repair wraps | #6B6A45 | 0.05 | 0.15 | 0.1 | |
| CenoteBase_Coir | coconut-fibre rope | #8A6E4E | 0.05 | 0.2 | 0.3 | |
| CenoteBase_Rug | woven pandanus (rug, basket and bin linings) | #8C7F5E | 0.05 | 0.2 | 0.1 | double-sided |
| CenoteBase_Leaf | living leaves | #4E5F3A | 0.1 | 0.2 | 0.1 | double-sided |
| CenoteBase_Thatch | dry palm leaf (roofs, awning, fan, sails) | #A39466 | 0.05 | 0.25 | 0.2 | double-sided |
| CenoteBase_Moss | moss, doormat | #6E7C4A | 0.05 | 0.4 | 0.5 | |
| CenoteBase_BarkCloth | ledger, tags, cubby backs, gloves, pouches | #D8CBAE | 0.05 | 0.15 | 0.1 | thin parts double-sided |
| CenoteBase_Noren | the noren | #D8CBAE | 0.05 | 0.1 | 0.05 | double-sided; Unity recolours |
| CenoteBase_ClothRed | dyed cloth, red | #A8655A | 0.05 | 0.1 | 0.05 | double-sided |
| CenoteBase_ClothIndigo | dyed cloth, indigo | #5E6E8A | 0.05 | 0.1 | 0.05 | double-sided |
| CenoteBase_ClothOchre | dyed cloth, ochre | #C2A55E | 0.05 | 0.1 | 0.05 | double-sided |
| CenoteBase_ClamShell | giant clam outside, bivalve pans and book rest | #E3DCCB | 0.1 | 0.2 | 0.3 | ribs are geometry |
| CenoteBase_ClamInner | giant clam inside | #EEE8DD | 0.35 | 0.05 | 0 | a little sheen |
| CenoteBase_Conch | big conch, conch lamps, ink shell | #D9B79A | 0.25 | 0.35 | 0.1 | mottle colour #A5785A |
| CenoteBase_WhaleBone | whale-rib gate | #E4DCC8 | 0.05 | 0.25 | 0.4 | mottle colour #B9B09C (rain streaks) |
| CenoteBase_Coconut | coconut bowls and ladle | #6E5240 | 0.1 | 0.3 | 0.3 | |
| CenoteBase_Clay | clay jars, rain gauge | #B5876A | 0.05 | 0.2 | 0.15 | |
| CenoteBase_LavaStone | querns, crushing slab, mortar, forecast board | #4A4744 | 0.05 | 0.3 | 0.5 | mottle colour #3A3836 |
| CenoteBase_Obsidian | mirror | #2A2A30 | 0.45 | 0 | 0 | the only glossy thing |
| CenoteBase_Coral | workbench legs | #CFC4B4 | 0.05 | 0.2 | 0.6 | strong dents |
| CenoteBase_Soil | planter soil | #5A4A3C | 0.02 | 0.3 | 0.4 | |
| CenoteBase_Compost | compost heap | #463A30 | 0.02 | 0.4 | 0.5 | |
| CenoteBase_Ash | ash on a jar lid | #9A968F | 0.02 | 0.2 | 0.3 | |
| CenoteBase_Salt | coarse salt, salt bars, grains | #EEEBE4 | 0.15 | 0.1 | 0.3 | |
| CenoteBase_BlackSalt | black salt | #4F4B4E | 0.2 | 0.15 | 0.3 | |
| (rock salt) | rock-salt piles | reuse the harvest rock-salt material | | | | |
| CenoteBase_Pile | generic pile before Unity recolours it | #C9C2B0 | 0.05 | 0.2 | 0.3 | |
| CenoteBase_Water | pool | #3F7F86 | 0.3 | 0 | 0 | |
| CenoteBase_DyeRed | dye water, red | #8E5550 | 0.3 | 0 | 0 | |
| CenoteBase_DyeIndigo | dye water, indigo | #4F6A80 | 0.3 | 0 | 0 | |
| CenoteBase_DyeOchre | dye water, ochre | #A99350 | 0.3 | 0 | 0 | |
| CenoteBase_Copper | threshold strips | #B07A4E | 0.25 | 0.3 | 0.1 | mottle colour #6F8A72 (patina) |
| CenoteBase_Brass | wall lamps, telescope, cylinder mouths | #A8873E | 0.2 | 0.25 | 0.05 | mottle colour #6F8A72; never shiny |
| CenoteBase_LampFlame | flames | #FFB860 | | | | emissive |
| CenoteBase_Feather | quill, nest feathers | #EEE8DC | 0.05 | 0.05 | 0 | double-sided |
| CenoteBase_Charcoal | silhouettes, ink, eyes of the pieces | #3A3634 | 0.02 | 0.1 | 0 | |
| CenoteBase_Bone | fish-bone awl | #E6DFCF | 0.1 | 0.05 | 0.1 | |
| CenoteBase_Map | map-table top | placeholder | | | | the picture is added in Unity |
Outline colour = base colour darkened (WorldLook already does this). "Double-sided" = doubleSided
true in Apply, but the meshes are still closed with thickness.

## Deliverables
1. Blender/Base/cenote_furniture_gen.py (re-running it rebuilds and re-exports everything) and
   Blender/Base/CenoteFurniture.blend. The anchors added to Blender/Base/cenote_base_gen.py.
2. FBX in Assets/Art/Models/Base/CenoteFurniture/: CenoteFurniture_Kit (all 12 kit parts, each its
   own object), CenoteFurniture_MapTable, _Trade, _Nursery, _Dye, _Collection, _Workshop, _Drying,
   _Refinery, _Deck, _Pier (each with its _S1/_S2/_S3/_Carved children and pivots),
   CenoteFurniture_BirdBasket, CenoteFurniture_Nest.
   Export settings: copy export_fbx from Blender/Harvest/rocksalt_gen.py.
3. Triangles: each hero 1,500-4,000; kit parts and small props 300-1,500; one whole room with all
   stages up to 8,000.

## How to work
Step 1 - grey only: the 12 kit parts and the 10 heroes, lined up in a row next to a slug proxy
(capsule 1.1 m long, 0.5 m tall). Show waku (a) the whole row, (b) each hero from slug eye height
(0.47 m) - put them on one sheet. List any number you changed and why. Stop and wait for waku's OK.
Step 2 - grey rooms: build every room S1-S3 and place it in the cave at its anchor. Show waku, for each
room, a view from across the hall at slug eye height with S1 and S3 side by side, plus the pier and
gate seen from the sea. Stop and wait for waku's OK.
Step 3 - materials, wear and details; export; import into Unity and set up the materials with
WorldLook; add a line to the Art/Models row of docs/README.md; add your log entry to docs/worklog.md.
```

---

(Japanese translation for the owner: `base_furniture_ja.md`. Do not read it.)
