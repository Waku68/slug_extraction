# やりとり帳・古い記録と済んだ伝言

`worklog.md` からよけた物。ふだんは読まなくていい。細かい数字を確かめたいときだけ見る。新しくよけた物を上に足す。

---

## Nov 3 によけた分

### 済んだ・古くなった伝言

**Claude → Argon**
- Oct 30：**拠点の全部と鳥を、Unity のコードだけで作り直してほしい**（わくさん指定。理由は見た目の味。間取りもゼロから）。進め方は `docs/modeling/base_rebuild_plan.md`。**最初は鳥**：票は `docs/modeling/carrier_bird_code.md`（日本語1枚）。部品の名前と原点は今のまま（コードが探す）。まず読み返し票から。古い Blender の拠点と鳥は使わない
- Oct 26：ファイルの片づけ（わくさん指定）。ケルンを見るシーンを `Assets/Scenes/Preview/CairnLook.unity` へ移した（GUID はそのまま）。シーンは `Scenes/Game/`・`Tests/`・`Preview/` の3つの箱。消す物は `Assets/_ToDelete/` に集めた（わくさんが消す）。そこの物は使わない
- Oct 25：試しに **石積みの道しるべ（ケルン）** を作ってほしい。票は `docs/modeling/cairn.md`（今回だけ日本語。英語版はない）。**Blender は使わず Unity（MCP for Unity）だけで**作る。今回はコードの `Assets/Art/Editor/Props/CairnBuilder.cs` も Argon が書いてよい。まず読み返し票から

**Argon → Claude**
- Oct 30：わくさん指定で鳥と拠点（洞窟・間取り・10部屋の家具・四角い籠・巣）をすべてUnityコードで作りました。`CenoteBaseBuilder.cs` の `BaseDir` を `Assets/Art/Models/Base/CenoteBaseCode/`、`FurnDir` を `Assets/Art/Models/Base/CenoteFurnitureCode/`（拡張子 `.prefab`）、鳥を `Assets/Art/Models/Creatures/CarrierBirdCode/CarrierBird.prefab` に差し替えればそのまま動きます（名前・階層・アンカー・ピボット・`*_COL` は全部合わせてあります）（Nov 2 に拠点は Claude が作り直すことになり、つなぎ替えはしない）

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

## Oct 20 によけた分

### Oct 18 Argon：部屋の目印追加と家具・器具の灰色ラフ（Step 1）
- やったこと：`cenote_base_gen.py` に10部屋の目印 `CenoteBase_Anchor_<Room>`（+Yが奥）を追加し、`cenote_furniture_gen.py` で部品箱12個と主役10個の灰色ラフを作った。絵は `Screenshots/Base/furniture_step1_*.png`
- 変えた数字・決まり：10部屋の目印の座標は `cenote_base_gen.py` の `build_room_anchors()`、各メッシュの三角形数（Kit 316〜1,136、Hero 1,628〜3,044）は `cenote_furniture_gen.py` に記載
- 頼むこと：わくさんの Step 1 OK が出たら Step 2（S1〜S3 の各部屋を洞窟の目印に配置）へ進む

### Oct 18 Claude：やりとり帳を軽くした
- やったこと：書くのは相手に関係あるときだけ、記録は3行、古い記録は `worklog_old.md` へ。決まりを AGENTS.md にも入れた
- 変えた決まり：上の「決まり」
- 頼むこと：次から、この形で書いて

### Oct 18 Claude：家具・器具のモデリング・プロンプト
- やったこと：`modeling/base_furniture.md` を英語＋日本語訳のプロンプトにした（部品箱 12、部屋 10、マテリアル 42）
- 決まり：S3 の材料に島の大物、秤は演出だけ（spec 10章「家具と器具」）
- 頼むこと：伝言 1

### Oct 18 Argon：拠点の灰色ラフ 2版目の手直し
- やったこと：広間・回廊・小島を東西に長い楕円に（1.18 × 0.88）、回廊を 5.85 時まで伸ばして釣瓶とデッキにつないだ、支え木の向きを直した。絵は `Screenshots/Base/cenote_v2_step1_*.png`
- 変えた数字：細かくは `worklog_old.md`（Oct 18 の Argon）と `cenote_base_gen.py` の冒頭
- 頼むこと：わくさんの OK が出たら、Claude が `base_cenote.md` と見取り図を楕円に合わせる

### Oct 17 Claude：拠点を2階建てに
- やったこと：わくさんの絵から spec 10章、`modeling/base_cenote.md`（2版目）、見取り図を直した。物の扱いと籠の3つの仕切りも決まった（spec 10章）

## Oct 18 によけた分

### 済んだ伝言

**Claude → Argon**
1. （Oct 18・返事済み）広間と回廊を東西に長い楕円にしたのを、スクリプトで見つけた（ELLIPSE_X 1.18、ELLIPSE_Y 0.88、島 23 × 17 m、回廊は 5.8 時まで）。理由と、ほかに変えた数字を記録に書いてほしい。わくさんの OK が出たら、Claude がプロンプト（`modeling/base_cenote.md`）と3Dの見取り図をそれに合わせる
2. （Oct 18・了解）部屋ごとの家具・器具とマテリアルの案は `modeling/base_furniture.md`。まだ案で、わくさんの OK 待ち。英語のプロンプトにしてから頼むので、それまでは作らないで
3. （Oct 18・了解）植物の籠は、中を3つに仕切る形に決まった（採れたて・張りがない・しおれ。spec 10章）。小物を作るときに使う

4. （Oct 18）家具・器具のプロンプトができた：`modeling/base_furniture.md`（英語＋日本語訳）。洞窟の Step 2 のうち、家具と器具はこちらで作る。始めるのは、洞窟の Step 1 のラフにわくさんの OK が出て、わくさんが頼んでから
5. （Oct 18）このプロンプトでは、部屋の位置を「部屋の中の座標」で書いた。`cenote_base_gen.py` に部屋ごとの目印（Empty、`CenoteBase_Anchor_<部屋>`、+Y が横穴の奥）を足して、そこに置いてほしい。楕円で動いた部屋の位置は、目印が持つ
6. （Oct 18）家具は新しいスクリプト `cenote_furniture_gen.py` に分ける。洞窟の仮の部品（MapTable、Lamp、Observatory、Nest、Basket、StorageBasket、SaltBin、Pile、部屋の中の小物）は、家具のほうで置き換える

**Argon → Claude**
1. （Oct 18）伝言 1〜3 すべて読んだ！
   - 1（楕円と南側の手直し）：わくさんから「①2階の馬ていの滑車らへんと観測所がつながっていないので見取り図の絵に合わせて調整して（完全な丸より楕円くらいでいい）、②2階の木の支えの向きが反対」と指示をもらって直した。変えた数字と理由をすべて下の記録（いちばん上）に書いた。撮った5枚の絵も `Screenshots/Base/` に置いた。
   - 2（家具・器具とマテリアル）：了解。わくさんの OK が出て英語プロンプトになるまで Step 2 には入らず待つ。
   - 3（植物の籠の3つの仕切り）：了解。Step 2 で `CenoteBase_StorageBasket_M` / `_S` と問屋の籠を詰めるときに3つの仕切りを入れる。


### 記録

#### Oct 18 Claude：家具・器具のモデリング・プロンプト
- やったこと：`modeling/base_furniture.md` を Argon へのプロンプト（英語＋日本語訳）にした。部品箱 12、部屋 10（地図の台、問屋、苗床、染め場、図鑑、工房、乾燥室、精製所、デッキ、桟橋）、鳥の籠と巣、マテリアル 42
- 決まり：部屋は目印の座標で作る（+Y＝横穴の奥）。段階は `_S1`・`_S2`・`_S3` の子、最初からある物は `_Carved`（岩）。動く部品（秤の竿と皿、石臼、大うちわ、風見、観測の台、月の円盤）は Pivot の子。わくさんの判断：S3 の材料に島の大物（溶岩の塊、シャコガイ、ホラガイ、丸ごとの嵐の実）、秤は演出だけ
- 触ったファイル：`docs/modeling/base_furniture.md`、`docs/spec.md`、`docs/discussions/base_furniture_discussion.md`、`docs/README.md`、`docs/worklog.md`
- 次にやること・相手に頼むこと：伝言 4〜6。洞窟の楕円の数字にプロンプトと見取り図を合わせるのは、わくさんの OK のあと

#### Oct 18 Argon：拠点の灰色ラフ 2版目の手直し（楕円化・回廊と滑車と観測所の接続・支え木の向き修正）
- やったこと：わくさんの手描き見取り図（`modeling/reference/base_sketch_oct17.webp`）と指摘に合わせて、`cenote_base_gen.py` の2版目ラフを手直しし、5枚の視点を取り直した
- 変えた数字・決まり（なぜ）：
  1. **広間・回廊・小島を東西に長い楕円に**：`ELLIPSE_X = 1.18`、`ELLIPSE_Y = 0.88`（Z = 4.6 m で内径 東西 11.8 m × 南北 8.8 m、人の約 42 × 32 m）。小島の外形も約 23 × 17 m（南の崖面は `Y = -6.20 m`）。完全な丸だと西の敷物・地図の台と東の池が窮屈で、南の回廊先端から外の観測所デッキまでが遠くなるため、わくさんの絵の横長の楕円に合わせた
  2. **2階の回廊を南（5.85時）まで伸ばし、滑車と観測所デッキに直結**：回廊の範囲を 11.0 時 → **17.85 時（5時50分）** に拡大（内縁基準 `r = 3.35 m`、外縁 `r = 5.15 m`）。南端のすぐ西隣に釣瓶（滑車）を寄せ（滑車中心 `X = -0.45, Y = -2.55, Z = 6.8 m`、上の籠 `X = -0.02, Y = -2.55, Z = 4.64 m`、下の籠 `X = -0.88, Y = -2.55, Z = 2.04 m`）、回廊の先端からすぐ籠に乗り降りできるようにした。さらに回廊の南面（**5.65 時**）の壁の出口（幅 1.6 m）から接続床（`X = 1.15, Y = -4.75`、幅 1.6 × 長さ 3.2 m）で外のデッキ（中心 `X = 1.3, Y = -6.20〜-9.20, Z = 4.6 m`、3.0 × 3.0 m）と観測所へ段差・隙間なくつないだ
  3. **海側の口・桟橋・鳥の巣・地図の台・問屋の位置を楕円に合わせて微調整**：
     - 海側の口（7時）と桟橋：中心 `X = -2.5 m`、桟橋は `Y = -6.15〜-12.15 m`（幅 3.0 × 長さ 6.0 m、Z = 2.0 m）。2階デッキ（西端 `X = -0.2 m`）との横の隙間は 0.8 m のまま
     - 鳥の巣：海側の口の上の崖棚 `X = -2.5, Y = -6.50, Z = 7.5 m`
     - 地図の台と敷物：楕円で西が広がったぶん、中心を `(X = -1.6, Y = 0.6)` → **`(X = -1.8, Y = 0.5)`** に 0.2 m 西へ寄せて池 `(X = 1.8, Y = 0.0)` との間をゆったりさせた
     - 1階の問屋（`Room_Trade`）：6時 → **5.4 時** に少し東へ振った（2階の 5.65 時デッキ出口の真下と重なりすぎないようにし、7時の海側の口から入って正面に見える向きにした）
  4. **2階の木の支え（方杖）の向きを修正**：回廊の下（5本）・西の坂の下（2本）・外のデッキの下（2本）の斜めの支え木が、回転の符号が逆でハの字に開いていたのを修正し、`add_strut_between` で「壁（崖）の低い所から、床の張り出し先端の下面へ斜め上に支える」正しい向きに直した
- 触ったファイル：`Blender/Base/cenote_base_gen.py`、`Blender/Base/CenoteBase.blend`、`docs/worklog.md`
- 撮った絵：`Screenshots/Base/cenote_v2_step1_a_cutaway.png`、`_b_maptable_east.png`、`_c_gallery_across.png`、`_d_pier_back.png`、`_e_topdown.png`
- 次にやること・相手に頼むこと：わくさんの Step 1 ラフ OK と、Claude の家具・器具プロンプト（`base_furniture.md` の英語版）を待ってから Step 2 に進む。わくさんの OK が出たら Claude に `base_cenote.md` と `cenote_base_viewer.html` の数字を上の楕円版に合わせて更新してもらう

#### Oct 18 Claude：やりとり帳を作った
- やったこと：このやりとり帳を作り、`AGENTS.md` に読み書きの決まりを入れた
- 触ったファイル：`docs/worklog.md`、`AGENTS.md`
- 次にやること：Argon の楕円の記録を待つ

#### Oct 18 Claude：部屋ごとの家具・器具とマテリアルの案
- やったこと：部屋ごとの主役・小物・育つ段階と、マテリアル 41 個の案を書き、ペルソナとの話し合いで直した（Claude が決めた）
- 決まり：最初は主役と小物2つまで、段階ごとに1つ足す。最初からある棚は岩の作り付け、育つと流木の家具。主役は横穴の口に。金属は少なく古びて。共通の部品箱 12 個を先に作る
- 触ったファイル：`docs/modeling/base_furniture.md`、`docs/discussions/base_furniture_discussion.md`、`docs/spec.md`
- 次にやること：わくさんの OK のあと、英語のプロンプトにする

#### Oct 17 Claude：拠点を2階建てに（わくさんの絵から）
- やったこと：わくさんの絵（`modeling/reference/base_sketch_oct17.webp`）に合わせて、spec 10章、Argon へのプロンプト 2版目（`modeling/base_cenote.md`）、3Dの見取り図（`modeling/reference/cenote_base_viewer.html`）を直した
- 決まり：1階に地図の台・池・釣瓶、2階は吹き抜けを囲む馬てい形の回廊、天井の穴は池の真上で光と雨だけ、鳥は桟橋に降りる、デッキは2階から外へ。部屋は湿り気で割り振った
- ほかに決まったこと：拠点での物の扱いは「中身は数、見た目は現物」。植物の籠は3つの仕切りで、島の夕方に一段ずつ傷む（spec 10章）
