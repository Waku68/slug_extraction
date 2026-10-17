# 拠点「セノーテの洞窟」のモデリング・プロンプト（Argon 向け）

Oct 17, 2026

わくさんと決めた拠点の構造（`../spec.md` 10章「拠点の構造」）を、Argon（Google Antigravity）に渡すプロンプトにしたもの。下の **English prompt** をそのまま貼る。日本語訳はわくさんが読むためのもので、中身は同じ。

- 参考の3D：`reference/cenote_base_viewer.html`（ブラウザで開くと回して見られる。数字はこのプロンプトと同じ）
- 決めた経緯：`../discussions/base_layout_discussion.md`、`sortie_launch_discussion.md`、`flying_carrier_discussion.md`

---

## English prompt

```
You are modeling the player's home base for "Slug Extraction PvP" (Unity 6000.5 URP).
Read AGENTS.md at the project root first, then .claude/skills/modeling/SKILL.md and
.claude/skills/modeling/shapes/cave_room.md. Follow every rule in AGENTS.md
(1 unit = 1 m, smooth normals, no painted textures, one material per part, script-built in Blender).
Reply to the user (waku) in short, plain Japanese.

## What it is
A cave home carved into a small white limestone islet in the middle of a tropical archipelago.
The inside is a jar-shaped vertical cave (like a Mexican cenote): wide inside, with one round hole
in the ceiling. Light falls through the hole as a shaft; rain falls through it as a column into a
pool on the floor. The players are slugs (body 1.1 m long, eye height 0.47 m). They return from
raids in a woven basket carried by a giant bird, which dives in through the ceiling hole with its
wings folded, opens them inside and lands on the floor. Everyone steps out and turns their own bag
upside down onto the map table. It should feel like a cozy, hand-built secret hideout: rough natural
rock outside, careful driftwood-and-vine craftsmanship inside.
Mood: rainy, damp, warm oil-lamp light. Pop silhouettes, realism 0.2.

## Coordinates (Blender, Z up)
- Origin: sea level, directly under the centre of the ceiling hole.
- "Front" = the sea mouth side = Blender -Y. Seen from above, the sea mouth is at 6 o'clock,
  the back of the cave at 12 o'clock, left (when you walk in from the sea) at 9, right at 3.
- Cave floor top: Z = 2.0 m (2 m above the sea).

## Islet (exterior)
- White-grey limestone (uplifted coral reef): rounded, pitted, eroded; dark wet streaks near water,
  a few shell fossils, small stalactites at overhangs. Low tropical scrub and moss on top.
- About 18-30 m across, top at about Z = 10.5 m, steep cliffs down to the sea.
  Keep at least 2.5 m of rock around the cave everywhere.

## The cave (atrium)
- Lathe profile, radius r at height Z (interior surface):
  Z 2.0 -> r 4.2 | Z 3.0 -> 4.8 | Z 5.0 -> 5.0 | Z 7.0 -> 4.6 | Z 8.6 -> 3.6 | Z 9.6 -> 2.2 | Z 10.0 -> 1.5
  (wide jar, narrowing to a 3 m ceiling hole). Make the wall irregular (+-0.3 m), not a perfect lathe.
- Ceiling hole: 3 m diameter, at Z 10.0. The bird must fit through with wings folded.
- Floor (Z 2.0): flat-ish limestone. In the exact centre, a shallow fresh rain-water pool,
  radius 1.3 m, 0.6 m deep, where the rain column lands. Players jump into it from any level to get
  down fast, so keep its centre clear of anything overhead.

## Rooms (niches) around the atrium
Each room = a horizontal niche dug into the wall (about 1.8 m deep) + a small driftwood balcony
(about 1.0 m) sticking out into the atrium in front of it. Wet rooms low, dry rooms high, things used
every raid near the floor.
| Room | Clock | Floor Z | Size (w x h) | Notes |
| Storage / scales / wholesaler counter | 11 | 2.0 | 2.4 x 1.5 | shelves of driftwood, a balance scale, a counter |
| Map table (open floor, not a niche) | 8:30, 2.4 m from centre | 2.0 | table d 2.5, height 0.38 | under a rock overhang at Z 4.3 so rain never hits it; flat top for a painted island map (separate part) |
| Basket landing spot | 6, 2.2 m from centre | 2.0 | pad d 1.8 | between the pool and the sea mouth |
| Nursery / dye pools (wet) | 2 | 3.4 | 2.2 x 1.2 | small water channels, moss, planter boxes |
| Resting crevices (wet) | 8 | 3.4 | 2.0 x 1.1 | narrow slots where a slug's back and belly both touch rock |
| Salt refinery / drying room (dry) | 4 | 5.8 | 2.2 x 1.2 | drying racks, a millstone; a thin aged-copper strip across the threshold (slugs avoid copper: it marks the dry, dangerous room) |
| Workshop (dry) | 10 | 5.8 | 2.0 x 1.2 | workbench, tool racks |
| Bird's nest | 11:30 | 7.6 | 2.0 x 1.3 | a high niche; the nest itself is woven driftwood and vines, about 1.4 m across |

## Getting around
- Spiral ramp: one continuous driftwood ramp hugging the wall, starting at the floor at 9:30
  (Z 2.25), climbing clockwise (seen from above) two full turns to Z 8.45. Width 0.85 m, slope about
  10-13 degrees, headroom at least 1.0 m, low railing of woven vines (0.4 m). It passes every niche.
- Small windows: 4 small openings (about 0.35 x 0.45 m) through the outer wall along the ramp
  (at 5, 7, 12:30 and 3 o'clock, heights Z 4.2 / 6.4 / 5.2 / 7.4) for glimpses of the sea.
- Well-sweep lift (tsurube): two woven baskets (d 0.85 m) on one vine rope over a wooden pulley on a
  beam near Z 9.0 at 1 o'clock. When one is at the floor the other is at the dry level (Z 5.8).
- Going down: jump into the central pool (no extra geometry needed, just keep it clear).

## Sea mouth, deck and observatory (front, 6 o'clock)
- Sea mouth: a big arch through the front wall at floor level, 2.8 m wide, 2.5 m high, a short
  tunnel (about 2.5 m) to the outside.
- Deck: driftwood deck outside the mouth at Z 2.0, about 5 x 3.2 m, low railing. A merchant's small
  boat can tie up at its edge.
- Observatory: a driftwood balcony sticking out from the cliff above the deck, floor at Z 5.0,
  about 3.2 x 2.4 m, held by two diagonal struts down to the rock, with a small leaf roof.
  Reached on foot from inside the cave by a short tunnel at Z 5.0 (from the ramp).
  On it: a wind vane, a rain gauge and a brass telescope.

## Props
- Bird basket: woven, d 1.6 m, height 0.6 m, open top; must hold 4 slugs (1.1 m long) and their bags.
- Oil lamps: brass bracket lamps with a warm orange flame, about 12 of them along the ramp, in the
  niches and at the map table. No electric light.
- Map table: driftwood, round; the top is a separate object (material CenoteBase_Map) so Unity can
  put the island map on it.

## Materials (muted, rain-wet; nothing more saturated than the slugs)
CenoteBase_Limestone #D9D2C0, CenoteBase_LimestoneWet #A9A390, CenoteBase_Driftwood #B9AE98,
CenoteBase_Vine #6B6A45, CenoteBase_Moss #6E7C4A, CenoteBase_Water #3F7F86,
CenoteBase_Copper #B07A4E, CenoteBase_Leaf #4E5F3A, CenoteBase_Brass #A8873E,
CenoteBase_LampFlame #FFB860 (emissive), CenoteBase_Map (placeholder, flat).

## Do NOT
- No human-made materials: no bricks, concrete, glass panes, metal sheets, nails you can see.
- No glowing crystals, no fantasy magic, no slime-built structures.
- No dead ends a slug cannot climb: steps over 0.45 m or slopes over 40 degrees on walking paths.
- Do not use the visual mesh as the collider. Make simple colliders (floor, ramp segments,
  balconies, deck) as separate objects named *_COL.

## Deliverables
1. Blender script Blender/Base/cenote_base_gen.py (re-running it rebuilds and re-exports everything)
   and Blender/Base/CenoteBase.blend.
2. FBX files in Assets/Art/Models/Base/CenoteBase/, one per part, so Unity can show rooms one by one
   as the base is upgraded:
   CenoteBase_Shell (islet + carved cave + hole + sea mouth + tunnels + windows),
   CenoteBase_Floor (floor, pool basin, table overhang), CenoteBase_Ramp, CenoteBase_Room_Storage,
   CenoteBase_Room_Nursery, CenoteBase_Room_Rest, CenoteBase_Room_Refinery, CenoteBase_Room_Workshop,
   CenoteBase_Nest, CenoteBase_Lift, CenoteBase_Deck, CenoteBase_Observatory, CenoteBase_Basket,
   CenoteBase_MapTable, CenoteBase_Lamp, plus the *_COL colliders.
   Export settings: copy export_fbx from Blender/Harvest/rocksalt_gen.py.
3. Triangle budget: Shell up to 20,000; each other part 300-4,000.

## How to work
Step 1 - grey rough only (no colours, no props detail). Put slug proxies (capsule 1.1 m long,
0.5 m tall) at the map table, on the ramp and on the deck. Show waku 4 images:
(a) a wide cutaway with the front half of the islet hidden, (b) slug eye height (0.47 m) at the
map table looking up at the hole, (c) from the middle of the ramp looking across the atrium,
(d) from the deck looking back into the sea mouth. List any number you changed and why.
Stop and wait for waku's OK.
Step 2 - after OK: materials, props, lamps, details. Shoot the same 4 views again.
Step 3 - export, import into Unity with the Slug/Toon shader (see Assets/Art/Editor/WorldLook.cs),
and add a line to the Art/Models row of docs/README.md.
```

---

## 日本語訳（わくさんが読む用。中身は上と同じ）

あなたは「ナメクジ脱出PvP」（Unity 6000.5 URP）のプレイヤーの拠点を作る。まずプロジェクト直下の AGENTS.md、次に `.claude/skills/modeling/SKILL.md` と `shapes/cave_room.md` を読む。AGENTS.md の決まり（1単位＝1 m、滑らかな法線、テクスチャを描かない、部品ごとのマテリアル、Blender のスクリプトで作る）をすべて守る。わくさんへの返事は、短く平たい日本語で。

### どんな物か
熱帯の群島の真ん中にある、白い石灰岩の小島に掘った洞窟の家。中は壺のような形の縦穴（メキシコのセノーテのよう）で、中は広く、天井に丸い穴が1つ。穴から光が柱になって落ち、雨は柱になって床の池へ落ちる。プレイヤーはナメクジ（体長 1.1 m、目の高さ 0.47 m）。出撃から帰ると、ばかでかい鳥が籠をつかんで、翼をたたんで天井の穴から飛び込み、中で広げて床に降りる。みんな籠から降りて、自分のバッグを上下ひっくり返して地図の台に広げる。手作りの居心地のいい秘密基地：外はごつごつした自然の岩、中は流木とつるの丁寧な手仕事。雰囲気は雨、湿り気、暖かい油の灯。シルエットはポップ、リアル度 0.2。

### 座標（Blender、上が +Z）
- 原点：海面の高さ、天井の穴の真下
- 「前」＝海側の口＝Blender の −Y。上から見て、海側の口が 6 時、洞窟の奥が 12 時、海から入って左が 9 時、右が 3 時
- 洞窟の床の上面：Z = 2.0 m（海から 2 m 上）

### 小島（外側）
- 白っぽい灰色の石灰岩（隆起したサンゴ礁）：丸く削れて穴だらけ。水に近い所は濡れて暗い筋、貝の化石が少し、張り出しの下に小さなつらら石。上には低い熱帯のやぶと苔
- 差し渡し 18〜30 m、てっぺんは Z 10.5 m くらい、まわりは海へ落ちる急な崖。洞窟のまわりはどこも 2.5 m 以上の岩を残す

### 洞窟（吹き抜け）
- 回転体の断面。高さ Z ごとの内側の半径 r：Z 2.0 → 4.2、3.0 → 4.8、5.0 → 5.0、7.0 → 4.6、8.6 → 3.6、9.6 → 2.2、10.0 → 1.5（広い壺が、直径 3 m の天井の穴へすぼまる）。壁は ±0.3 m でいびつに。きれいな回転体にしない
- 天井の穴：直径 3 m、Z 10.0。鳥が翼をたたんで通れること
- 床（Z 2.0）：だいたい平らな石灰岩。真ん中に浅い雨水の池（半径 1.3 m、深さ 0.6 m）。雨の柱がここに落ちる。どの階からでもここへ飛び込めば一瞬で下りられるので、池の真上には何も置かない

### 部屋（吹き抜けのまわりの横穴）
部屋は、壁を掘った横穴（奥行き 1.8 m くらい）＋その前に吹き抜けへ張り出した小さな流木の台（1.0 m くらい）。湿った部屋は下、乾いた部屋は上、毎回使う物は床の近く。

| 部屋 | 時計の向き | 床の高さ Z | 大きさ（幅×高さ） | メモ |
| --- | --- | --- | --- | --- |
| 倉庫・秤・問屋の窓口 | 11 時 | 2.0 | 2.4 × 1.5 | 流木の棚、天秤、窓口 |
| 地図の台（横穴ではなく床の上） | 8 時半、中心から 2.4 m | 2.0 | 直径 2.5、高さ 0.38 | Z 4.3 の岩の張り出しの下（雨が当たらない）。天板は島の地図を描く平らな面（別の部品） |
| 籠の降りる所 | 6 時、中心から 2.2 m | 2.0 | 直径 1.8 | 池と海側の口の間 |
| 苗床・染め場（湿った） | 2 時 | 3.4 | 2.2 × 1.2 | 小さな水路、苔、植え箱 |
| 休む隙間（湿った） | 8 時 | 3.4 | 2.0 × 1.1 | 背中とお腹の両方が岩に触れる細い隙間 |
| 精製所・乾燥室（乾いた） | 4 時 | 5.8 | 2.2 × 1.2 | 干し棚、石臼。入口の敷居に古びた銅の細い帯（ナメクジは銅を嫌う。乾いて危ない部屋の印） |
| 工房（乾いた） | 10 時 | 5.8 | 2.0 × 1.2 | 作業台、道具掛け |
| 鳥の巣 | 11 時半 | 7.6 | 2.0 × 1.3 | 高い横穴。巣は流木とつるで編んだ、差し渡し 1.4 m くらい |

### 行き来
- らせん坂：壁に沿った一本の流木の坂。床の 9 時半（Z 2.25）から、上から見て時計回りに2周して Z 8.45 まで。幅 0.85 m、傾き 10〜13° くらい、頭の上は 1.0 m 以上空ける、編んだつるの低い手すり（0.4 m）。どの部屋の前も通る
- 小窓：坂に沿って外壁に小さな穴を4つ（0.35 × 0.45 m くらい。5 時・7 時・12 時半・3 時、高さ Z 4.2・6.4・5.2・7.4）。海がちらっと見える
- 釣瓶の籠：1本のつるの綱でつながった編み籠2つ（直径 0.85 m）。1 時の Z 9.0 あたりの梁に木の滑車。片方が床にあるとき、もう片方は乾いた階（Z 5.8）
- 下りるとき：真ん中の池に飛び込む（形は要らない。真上を空けておくだけ）

### 海側の口、デッキ、観測所（前、6 時）
- 海側の口：前の壁を床の高さで抜く大きなアーチ。幅 2.8 m、高さ 2.5 m、外まで 2.5 m くらいの短いトンネル
- デッキ：口の外の流木のデッキ、Z 2.0、5 × 3.2 m くらい、低い手すり。行商人の小舟が端につなげる
- 観測所：デッキの上の崖から張り出す流木の台。床は Z 5.0、3.2 × 2.4 m くらい。2本の斜めの支えで岩に支え、小さな葉の屋根。洞窟の中から Z 5.0 の短いトンネル（らせん坂から）で歩いて行ける。上に風見、雨量計、真鍮の望遠鏡

### 小物
- 鳥の籠：編み籠、直径 1.6 m、高さ 0.6 m、上は開いている。ナメクジ4匹（体長 1.1 m）とバッグが入ること
- 油の灯：真鍮の腕木の灯、暖かい橙色の炎。坂、部屋、地図の台に 12 個くらい。電気の灯はなし
- 地図の台：流木の丸い台。天板は別の部品（マテリアル `CenoteBase_Map`）にして、Unity で島の地図を載せられるように

### マテリアル（雨に濡れたくすんだ色。ナメクジより鮮やかにしない）
`CenoteBase_Limestone` #D9D2C0（石灰岩）、`_LimestoneWet` #A9A390（濡れた石灰岩）、`_Driftwood` #B9AE98（流木）、`_Vine` #6B6A45（つる）、`_Moss` #6E7C4A（苔）、`_Water` #3F7F86（水）、`_Copper` #B07A4E（銅）、`_Leaf` #4E5F3A（葉）、`_Brass` #A8873E（真鍮）、`_LampFlame` #FFB860（光る炎）、`_Map`（地図の仮の面）

### やめて
- 人が作った素材はなし：レンガ、コンクリート、窓ガラス、金属板、見える釘
- 光る結晶、ファンタジーの魔法、粘液で作った建物はなし
- ナメクジが上れない行き止まりを作らない：歩く道に 0.45 m より高い段差、40° より急な坂はなし
- 見た目のメッシュを当たり判定にしない。床・坂・張り出し・デッキの単純な当たり判定を、名前の最後を `_COL` にして別に作る

### 渡す物
1. Blender のスクリプト `Blender/Base/cenote_base_gen.py`（もう一度実行すると全部作り直して書き出す）と `Blender/Base/CenoteBase.blend`
2. `Assets/Art/Models/Base/CenoteBase/` に、部品ごとの FBX（拠点を育てるにつれて、Unity で部屋を1つずつ見せられるように）：`CenoteBase_Shell`（小島＋掘った洞窟＋穴＋海側の口＋トンネル＋小窓）、`_Floor`（床、池の窪み、地図の台の上の張り出し）、`_Ramp`、`_Room_Storage`、`_Room_Nursery`、`_Room_Rest`、`_Room_Refinery`、`_Room_Workshop`、`_Nest`、`_Lift`、`_Deck`、`_Observatory`、`_Basket`、`_MapTable`、`_Lamp`、それと `*_COL` の当たり判定。書き出しの設定は `Blender/Harvest/rocksalt_gen.py` の `export_fbx` をまねる
3. 三角形の数：Shell は 20,000 まで、ほかは部品ごとに 300〜4,000

### 進め方
1. **灰色のラフだけ**（色なし、小物の細部なし）。比べるナメクジ（長さ 1.1 m・高さ 0.5 m のカプセル）を地図の台、坂、デッキに置く。わくさんに4枚見せる：(a) 小島の前半分を隠した断面の全景、(b) 地図の台でナメクジの目の高さ（0.47 m）から天井の穴を見上げる、(c) 坂の途中から吹き抜けの向こう側を見る、(d) デッキから海側の口の中を振り返る。変えた数字とその理由を書く。**わくさんの OK が出るまで止まる**
2. OK のあと：マテリアル、小物、灯、細部。同じ4枚をもう一度撮る
3. 書き出して Unity に入れ、`Slug/Toon` シェーダーでつなぐ（`Assets/Art/Editor/WorldLook.cs` を見る）。`docs/README.md` の `Art/Models` の行に1行足す
