# Carrier bird (model sheet)

Oct 18, 2026. Japanese translation for waku: `carrier_bird_ja.md` (do not read it).

> **Outdated (Oct 30, 2026):** this is the Blender version. The bird is finished as Unity code (`Assets/Art/Editor/Creatures/CarrierBirdBuilder.cs`, sheet `carrier_bird_code.md`). Do not change the finished bird. Blender 版の票（Oct 30 にコード版で完成。使わない）。
References:
- `reference/carrier_bird_sketch_oct18.webp` — waku's first pencil sketch (photo turned sideways:
  rotate 90° counter-clockwise; left = side view facing right, right = front view).
- Direction since grey rough 2 (Oct 18): **a chibi pelican with the neck squashed away**. waku showed a
  flat cartoon pelican (stock illustration, not kept in the repo). What to take from it is written below.
  The "line under the eye and bulge" in the pencil sketch is the pelican's throat pouch.

## 1. One line

The one big bird that lives at the player's base and flies the party to the islands and back in a
basket. Hatched from an egg and raised by the player each season: a partner, not a monster.
A sea bird that eats fish, not slugs. From slug eye height it must read as "big, soft, friendly,
a bit silly" — never as a predator.

## 2. Size

- Standing height about 2.1 m (top of head, crest not counted). About 4.5x a slug's eye height
  (0.47 m); for a human, a bird about 7.5 m tall. Body width about 1.3 m.
- Wingspan, wings open: about 7 m tip to tip.
- Origin: on the ground, centre between the two feet. Up = Blender +Z.
  The beak points to Blender +Y (= Unity +Z after export, see Deliverables).
- Where it stands in the game: on the deck of the base (lands on the rope ring) and in its nest on the
  outer cliff above the sea mouth. Comparison slug for screenshots: capsule 1.1 m long, 0.5 m high.

## 3. Shape words

- **ずんぐり (stubby)**: NO visible neck — the head sits straight on the chest. Short legs.
- **しずく (teardrop) body**: chest bulges forward, the back tapers into a short pointed tail that tips up.
- **big beak, round tip**: the long beak and its hanging pouch are the biggest thing on the bird.
  The tip is rounded, never pointed or hooked.

## 4. Parts

Hierarchy: `CarrierBird_Root` (empty, at the origin) → `CarrierBird_Rig` (armature with 2 bones:
`Body`, and its child `Neck` with its root at (0, 0.30, 1.22)). Head + body is one skinned mesh.
Beak, pouch, eyes and crest are parented to bone `Neck`, so turning/tilting the neck bone moves the
whole head smoothly. Wings, legs and tail are separate objects under the root with pivots where they
rotate. Unity moves the bones and parts with code; no animation is made in Blender.

| Object | Size / place | Material | Colour (default, can be dyed later) |
| --- | --- | --- | --- |
| `CarrierBird_Body` | Head + body as ONE smooth mesh (no crease at the neck; Oct 19 waku). Body: teardrop 1.3 W x 1.7 D x 1.2 H, centre Z 0.9, chest round and forward, back tapers up into the tail root. Head: sphere d about 0.92, centre Z 1.58, sitting on the front-top of the body; the neck is a smooth short dip. Skinned to the rig: bone `Body` (weight 1 on the body) and `Neck` (weight 1 on the head, blended across the neck). About 2,600 triangles | `CarrierBird_Feather` | cream |
| `CarrierBird_Crest` | 3 soft tufts at the BACK of the head, swept backward, up to 0.38 long. Parented to bone `Neck` | `CarrierBird_Feather` | cream |
| `CarrierBird_BeakUpper` | Long, nearly straight: about 1.0 forward from the face, WIDE: 0.6 wide at the base, 0.34 near the tip (Oct 19 waku: wider). The tip is ROUNDED (r about 0.08) and dips down a little. A thin raised ridge along each side (the "darker line") | `CarrierBird_Beak` | orange |
| `CarrierBird_Pouch` | The lower jaw + throat pouch, hanging under the upper beak: deepest (about 0.45) near the head, curving up to meet the beak at about 80% of its length. Soft, round belly-like curve. Pivot at the jaw hinge (opens to call, can bulge) | `CarrierBird_Pouch` | yellow |
| `CarrierBird_Eye_L` / `_R` | Thin, tall dark ovals, about 0.07 W x 0.2 H (Oct 19 waku: thinner and taller), high on the head just above the beak base. No eye ring (waku: eye colour does not matter) | `CarrierBird_Eye` | near-black, tiny white highlight |
| `CarrierBird_WingFolded_L` / `_R` | Hugs the body side (never sticks out like an arm from the front). 3 stacked layers, each a different colour, with stepped scalloped edges: big cream layer, a tan band, dark tips at the back near the tail | `CarrierBird_Feather` / `CarrierBird_WingMid` / `CarrierBird_WingTip` | cream / tan / charcoal |
| `CarrierBird_WingOpen_L` / `_R` | Spread wing: 3.0 long from the shoulder, 1.0 wide at the root, tip 0.5. Same 3 colour bands: cream front, tan band, charcoal outer tips and trailing edge, scalloped. Pivot at the shoulder. Hidden by default | same 3 | same 3 |
| `CarrierBird_Tail` | Short pointed tail, 0.55 long, tips up about 25°, 2–3 soft scallops on the edge | `CarrierBird_Feather` | cream |
| `CarrierBird_Leg_L` / `_R` | Short, d 0.15, about 0.3 visible, 0.4 apart. Pivot at the hip | `CarrierBird_Foot` | yellow-orange |
| `CarrierBird_Foot_L` / `_R` | WEBBED foot: 3 toes forward joined by a web, a flat rounded paddle about 0.4 long, 0.35 wide, thick enough to be a closed mesh. Grips the basket rope when flying | `CarrierBird_Foot` | yellow-orange |

Colours: cream (body), tan and charcoal (wing bands), orange/yellow family (beak, pouch, feet).
Feather feel comes from a TEXTURE on the body only (head plain, Oct 19 waku: "remove the texture from the head"; the pattern fades out across the neck. Texture 512 x 1024: 17 rows of feathers in the lower 80%, fading to white by 92%; Unity wrap U Repeat, V Clamp; head UVs go to the white top by the `Neck` weight) (Oct 19 waku: shape feathers looked wrong, removed; exception to the no-texture rule): `Assets/Art/Models/Creatures/CarrierBird/Textures/CarrierBird_FeatherScales.png` (grey overlapping round scales, multiplies the colour, made by `Blender/Creatures/carrier_bird_feather_tex.py`), on material `CarrierBird_Plumage` (Slug/Toon `_BaseMap`). UVs from `body_uvs` in the gen script: around = azimuth (4 repeats, about 16 feathers round the body, about 0.25 m wide), up/down = elevation (about 17 rows, about 0.12 m; Oct 19 waku: smaller), rims darken the colour by 15% (Oct 19 waku: lighter), seam at the middle of the back, round edges point down. Plus the stepped colour bands on the wings and the soft crest.
The bird is a character: a bit more saturated than world objects, softer than the slugs.

## 5. States

- **Stand** (default): folded wings shown, open wings hidden. This is what the slug sees on the deck.
- **Fly**: folded wings hidden, open wings shown. Feet hang down; the basket hangs from the feet.
- Unity swaps the two wing pairs and flaps the open wings by rotating them at the shoulder.
  Do not animate in Blender. Make sure the open wings look right at 0° and at +-35° up/down.
- Growth stages (chick → adult) are later, NOT in this job.

## 6. Do NOT

- No long neck. No long S-shaped pelican neck. The short neck is one smooth curve, never a crease.
- No pointed or hooked beak tip, no sharp claws, no glaring eyes. Not scary.
- No single thin feathers, no hair strands. Feathers = soft crest, stepped bands.
- Not a dragon, not a bat, no armour, no saddle, no harness, no metal anywhere.
  The basket is a separate object (not part of this job).
- No text or logo from the reference picture.

## 7. Deliverables

- Script `Blender/Creatures/carrier_bird_gen.py` + shared helpers `Blender/Creatures/creature_mesh.py` (under 500 lines; print only object names,
  triangle counts and errors) and `Blender/Creatures/carrier_bird.blend`.
- FBX `Assets/Art/Models/Creatures/CarrierBird/CarrierBird.fbx` (all parts, nested under the root).
  Nested export: `bake_space_transform=False, axis_forward='Y', axis_up='Z'`, and Bake Axis
  Conversion ON in Unity (same as `Blender/Base/cenote_furniture_gen.py`).
- Triangles: 12,000 total at most (now about 7,800) (open + folded wings both counted). Smooth normals.
- Collider in Unity, not in the FBX: one capsule for the body + one sphere for the head
  (salt shots hit the bird and startle it; it is never hurt).
- Steps: read-back sheet → grey rough → colour → Unity, waku's ✅ between each
  (`docs/modeling/README.md` §3–4). Grey rough shots: A (slug eye height, from in front),
  B (side view next to the comparison slug), C (from above at an angle). Show stand and fly side by side.

## 8. Decided (Oct 18, waku)

1. Nest and basket are resized in a separate later step (nest about 3.2 m across; basket SQUARE 2.4 x 2.4, walls 0.4 - Oct 19 waku, see `base_furniture.md`).
   This job is the bird only.
2. Argon was not available on Oct 18, so Claude (Opus) builds this bird.
3. Grey rough 1 (2.4 m, round puffin) felt intimidating → lowered to about 2.1 m.
   Grey rough 2 (round puffin with tufts) → changed to the chibi pelican above: neck squashed,
   beak tip rounded only at the tip, eye colour does not matter.

4. Oct 19: grey rough OK (waku ✅). Colour step: `Screenshots/Creatures/carrier_bird_color_*.png`. Folded wing band lengths 1.0 / 0.70 / 0.44 so the charcoal and tan bands show.
5. Oct 19: in Unity. `Assets/Art/Models/Creatures/CarrierBird/CarrierBird.fbx` (Bake Axis Conversion ON), materials `Materials/CarrierBird_*.mat` (Slug/Toon via `WorldLook.Apply`, but `_Facet` 0 = smooth, because the bird is a character), prefab `CarrierBird.prefab` (open wings off, capsule on the body, sphere on bone `Neck`). One placed on the deck of `Assets/Scenes/CenoteBase.unity` at (6.2, 4.6, -9.0). Shots `Screenshots/Creatures/carrier_bird_unity_*.png`.
6. Oct 19: look approved by waku (done). Next: motion code in Unity (neck bone, wing swap and flap), and resizing the nest and basket.
7. Oct 19: motion in Unity code: `Assets/Base/CarrierBirdMotion.cs` on the prefab root (numbers in its header). Idle: breath, head tilt, neck turns to the nearest slug (35° max), call with the pouch. `SetFlying` swaps the wings, `Flap` flaps them ±35°. Shots `Screenshots/Creatures/carrier_bird_motion_sheet.png`.

## 9. Open questions

1. Keep about 2.1 m tall and this chunky (head + beak about 40% of the height)?
