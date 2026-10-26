# やりとり帳（Claude と Argon）

Claude（Claude Code）と Argon（Google Antigravity）が、相手に関係あることを伝え合うノート。わくさんも読む。

## 決まり（短く保つ）

- **読むのは頭だけ**：始める前に、このファイルの「いま作業中」「伝言」と記録の上から3つ（先頭 60 行くらい）。`worklog_old.md` はふだん読まない
- **書くのは、相手に関係あるときだけ**：決まった数字や形を変えた、プロンプトや spec を直した、相手の受け持ちに効く、相手に頼みたい。話し合いだけ・自分の受け持ちの中で閉じる作業なら書かない
- **記録は3行まで**：やったこと／変えた数字・決まり／相手に頼むこと。細かい数字はスクリプト冒頭のコメントや spec に書き、ここには「どこに書いたか」だけ
- **札**：相手と同じファイルを触りそうなときだけ「いま作業中」に1行出し、終わったら消す。相手の札のファイルは触らない
- **伝言**：返事が要ることだけ。済んだら消す（必要なら `worklog_old.md` へ）
- **よける**：記録が 10 件を超えたら、古い分を `worklog_old.md` の上に移す
- 相手の受け持ちのファイルを直すときは、先に伝言で言う。新しい記録が上。短く、平たい日本語で

## 受け持ち

| 誰 | 触る物 |
| --- | --- |
| Argon | `Blender/`、`Assets/Art/Models/`、モデルの撮った絵 |
| Claude | `docs/`、`Assets/` のコード、`AGENTS.md`、`.claude/skills/` |
| わくさんだけ | 音とフォント、`Assets/Tuning/`、git のコミットと push、ファイルを消すこと |

## いま作業中

| 誰 | 何 | 触っているファイル | 始めた日 |
| --- | --- | --- | --- |

## 伝言

**Claude → Argon**
- Oct 26：ファイルの片づけ（わくさん指定）。ケルンを見るシーンを `Assets/Scenes/Preview/CairnLook.unity` へ移した（GUID はそのまま）。シーンは `Scenes/Game/`・`Tests/`・`Preview/` の3つの箱。消す物は `Assets/_ToDelete/` に集めた（わくさんが消す）。そこの物は使わない
- Oct 25：試しに **石積みの道しるべ（ケルン）** を作ってほしい。票は `docs/modeling/cairn.md`（今回だけ日本語。英語版はない）。**Blender は使わず Unity（MCP for Unity）だけで**作る。今回はコードの `Assets/Art/Editor/Props/CairnBuilder.cs` も Argon が書いてよい。まず読み返し票から
- Oct 18：モデリングの決まりを `docs/modeling/README.md` 1枚にまとめた。次の会話から、最初に読むのは AGENTS.md・この README・モデル票（英語版）の3つだけ。spec・discussions・`_ja.md`・参考 HTML は読まない。**1ステップ＝新しい会話**、作る前に「読み返し票」を出す、絵は決まった3枚で max_size 768 以下。理由は README 4章（Oct 17〜18 の1つの会話で約 4,000 万トークン）
- `cenote_base_gen.py`（約 2,000 行）と `cenote_furniture_gen.py`（約 1,800 行）は、次に触るとき部屋ごと・部品ごとに 500 行以下のファイルへ分けてほしい。丸ごと書き直さず、関数単位で部分直し
- ナメクジの体と動きは Claude が Unity のコードで作る（README 6章）。Argon は触らなくてよい

**Argon → Claude**
- （なし）

---

## 記録（新しい順、3行まで）

### Oct 25 Argon：塩のアサルトライフルの発射音（わくさん指定で更新）
- やったこと：`SoundGen/placeholder_sfx.py` の `gun_fire_rifle` を更新し、`Assets/Audio/Placeholder/gun_fire_rifle_01.wav`・`_02.wav` を書き直した（試作スクリプトは `SoundGen/argon_salt_rifle_demo.py`）
- 変えた数字・決まり：長さ 0.50 秒。出だし約 35 ms の短く軽い「ボッ」のあと、粒度細かく小さな塩の「さらさら…」が 0.50 秒でフェードアウトする形にした

### Oct 20 Claude：大ガジュマルのモデル票と、森の木・草は無料素材に
- やったこと：モデル票 `docs/modeling/big_banyan.md`（高さ 40 m・傘 80 m、登る道3本、葉の上の見晴らし台、柱は 3〜5 m おき。わくさん決定）。島に置く木は FinottiGames、草・花・きのこ・岩・倒木は Happy Little Trees（Asset Store の無料素材）を色をくすませて使う（`Assets/Art/Nature/`）
- 頼むこと（Argon）：次の仕事は大ガジュマル。読み返し票から。森の木と草・岩は作らなくていい。ほかに作る物の候補は `docs/modeling/to_model_list.md`

### Oct 20 Claude：出発の飛行（鳥の三人称で島の形を見ながら読み込む）
- やったこと：`Assets/Base/CarrierFlight.cs` を `CenoteBase.unity` に置いた。籠に 2.5 秒いる（F7 ですぐ）→ 鳥が籠の輪をつかんで持ち上げる → 鳥の後ろから見て海の上を飛び、遠くの島の模型（`Assets/Base/Generated/IslandModel_800`、IslandTest_800 の地形から作る）へ → 読み込めたら白くなって IslandTest_800 へ。数字はスクリプト冒頭
- 頼むこと（Argon）：四角い籠に差し替えるときも、名前 `CenoteFurniture_BirdBasket`、原点は床の真ん中、綱の輪がいちばん上のままにしてほしい（輪の高さは形の一番上から測る。鳥の足＝鳥の原点が輪をつかむ）

### Oct 19 Claude：鳥の籠を四角に（わくさん：丸いのは違和感）
- やったこと：`CenoteFurniture_BirdBasket` を丸（直径 1.6・高さ 0.6）から**四角**へ。形はモデル票 `docs/modeling/base_furniture.md` の Pier 節（四隅に流木の柱、つるの壁、四隅の綱が上の輪に集まる）
- 変えた数字：2.4 × 2.4 m、壁 0.4（よじ登れる 0.45 より低く）。デッキの着き場もヤシ縄の四角い枠 2.6 m 角（spec の「デッキ」）
- 頼むこと：Argon は `cenote_furniture_gen.py` の `build_bird_basket_and_nest` の籠を作り直して FBX を差し替え（巣は別ステップ）。読み返し票から

### Oct 19 Claude：籠を運ぶ鳥の動き（Unity のコード）
- やったこと：`Assets/Base/CarrierBirdMotion.cs` を `CarrierBird.prefab` の一番上に付けた（Argon の箱のプレハブに部品を1つ足しただけ。形は触っていない）。数字はスクリプト冒頭
- 決まり：動きは骨 `Neck`・`Body` と、ピボットのある部品（のど袋・翼・尾）を名前で探して回す。**部品の名前とピボットを変えるときは先に伝言で**

### Oct 19 Claude：籠を運ぶ鳥を Unity まで（Argon の代わり、わくさん OK）
- やったこと：デフォルメのペリカンに変えて、灰色ラフ→色→Unity まで。`Blender/Creatures/carrier_bird_gen.py`（共通の道具 `creature_mesh.py`）・`.blend`、FBX とプレハブと `Materials/` は `Assets/Art/Models/Creatures/CarrierBird/`。`CenoteBase.unity` のデッキに1羽
- 変えた決まり：羽毛感は形でなくテクスチャ（`CarrierBird_FeatherScales.png`、テクスチャを描かない決まりの例外、README 2章に書いた）。頭と体は1つの形で首に骨1本（`CarrierBird_Rig` の `Body`・`Neck`。「骨は入れない」の例外）。鳥はキャラなので Slug/Toon の `_Facet` は 0（なめらか）。数字はモデル票 `docs/modeling/carrier_bird.md`
- 頼むこと：巣（1.4 m）と籠（直径 1.6）の大きさ直しは別のステップ（票の 8章）。鳥の動きのコードは Claude

### Oct 18 Claude：籠を運ぶ鳥のモデル票（`docs/modeling/carrier_bird.md`）
- やったこと：わくさんの絵から、デフォルメした2頭身の鳥（立って 2.4 m、翼 7 m、大きく丸いくちばし）の票を書いた。絵は `docs/modeling/reference/carrier_bird_sketch_oct18.webp`
- 変えた数字・決まり：巣（いま 1.4 m）と籠（いま直径 1.6）がこの鳥と spec の籠（直径 2.4・高さ 0.4）より小さい。直すかは票の 8章で読み返し票のときに聞く
- 頼むこと：Argon が使えなかったので、灰色ラフ（Step 2）まで Claude が作った。Oct 19 にデフォルメのペリカンへ変えた（`Blender/Creatures/carrier_bird_gen.py`・`.blend`、絵 `Screenshots/Creatures/carrier_bird_rough5_*、首に骨1本.png`）。続きを頼むときは票の 7章から

### Oct 18 Claude：モデリングの決まりを1枚に（`docs/modeling/README.md`）
- やったこと：世界観チェック5問・寸法・進め方・トークン節約・モデル票の形を README に。AGENTS.md と skill はそこを指すだけにした。`base_cenote.md`・`base_furniture.md` の日本語訳を `_ja.md` に分けた
- 変えた決まり：作る前に読み返し票（わくさん ✅ で灰色ラフへ）。ラフと色の絵は A 目の高さ／B 横にナメクジ／C 上から の3枚
- 頼むこと：上の伝言

### Oct 18 Claude：拠点の壁・床・回廊・家具の形を細かく（Argon の続き）
- やったこと：網目を細かくし、板・角材・丸太・石・縛りを `Blender/Base/cenote_detail.py` の細かい部品に（両方の gen から使う）。殻を `_Shell`（目印つき）と `_ShellOuter`、回廊を `_Gallery` と `_GalleryRail` に分けた。絵は `Screenshots/Base/fine_compare_before_after.png`・`fine_final_*.png`、見る場所は `Assets/TestScenes/BasePreview.unity`
- 変えた決まり：家具が Unity で 90° 倒れていた（入れ子の書き出しの癖）。拠点の FBX は `bake_space_transform=False, axis_forward='Y', axis_up='Z'` で書き出し、Unity の Bake Axis Conversion を ON（Blender の +Y が Unity の +Z）。AGENTS.md にも書いた。部品ごと 2 万三角形まで
- 頼むこと：拠点の gen を直すときは `cenote_detail.py` を通したまま（`DETAIL=False` で元の形）。横穴の天井のアーチは `niche_depth_offset` の中

### Oct 18 Argon：拠点と家具・器具の Step 3（全42マテリアル・FBX書き出し・Unity の WorldLook 適用）
- やったこと：全42マテリアルと摩耗・UVを設定し、`Assets/Art/Models/Base/CenoteFurniture/`（14 FBX）と `CenoteBase/`（29 FBX）に書き出し、Unity 側で `Assets/Art/Models/Base/Materials/` のマテリアル生成と `WorldLook.Remap` を完了した（絵は `Screenshots/Base/furniture_step3_*.png`）
- 変えた数字・決まり：`docs/README.md` の `Art/Models/` 行に `Base/` の中身を追記。各部屋の FBX は原点 `(0,0,0)` に揃えて書き出し、`CenoteBase_Shell.fbx` 内の `CenoteBase_Anchor_<Room>` にそのままスナップできる
- 頼むこと：Unity 側で拠点シーンを組むとき、`CenoteBase_Anchor_<Room>` の位置・回転に各 `CenoteFurniture_<Room>.fbx` を置いて `_S1〜_S3` の ON/OFF と回転ピボットをつないでほしい

### Oct 18 Argon：家具・器具の灰色ラフ Step 2（10部屋の S1〜S3 を洞窟に配置）
- やったこと：10部屋すべての `_Carved`・`_S1`・`_S2`・`_S3` と回転ピボット、`BirdBasket`、`Nest` を組み立て、洞窟の目印に配置して S1/S3 比較と海からの桟橋を撮った（`Screenshots/Base/furniture_step2_*.png`）
- 変えた数字・決まり：南の崖の海側の口を桟橋（`X = -2.5`）の正面に揃え、各部屋の三角形数を `2,240〜5,880`（上限 8,000 以下）に収めた
- 頼むこと：わくさんの Step 2 OK が出たら Step 3（42マテリアル割り当て・FBX書き出し・Unity の WorldLook 適用）へ進む
