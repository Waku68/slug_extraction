# プロジェクトの地図

Oct 4, 2026

ナメクジ脱出PvP（Unity 6000.5.4f1、URP 17.5.0）の、どこに何があるかの一覧。迷ったらまずここを見る。

## 最初に読むもの

| 資料 | 中身 |
| --- | --- |
| [spec.md](spec.md) | 仕様。決まったこと・仮のこと・未決定のこと（凡例【決定】【仮】【未決定】） |
| [progress.md](progress.md) | 進捗。仕様と照らして、何ができて何がまだか。次にやること |
| [bag_spec.md](bag_spec.md) | バッグの仕様（バッグ、エコバッグ、隠し場所、カタツムリの殻、ベルトと背負い紐）。今の実装との違いと作る順番 |

## docs の中身

| フォルダ | 資料 | 中身 |
| --- | --- | --- |
| `character/` | [character_design.md](character/character_design.md) | キャラデザインの経緯（v1〜v7）と今の状態、残っている論点 |
| | [design_params.md](character/design_params.md) | 今のキャラの値の一覧（`SlugPop` のスライダー、這う動き） |
| | `face_ref.jpg` | v7 で試した平面の顔の参考画像 |
| `character/history/` | [design_candidates_v1.md](character/history/design_candidates_v1.md)、[design_candidates_v2.md](character/history/design_candidates_v2.md) | 最初の9案と、配色・目の比較の記録 |
| `discussions/` | [base_discussion.md](discussions/base_discussion.md) | 拠点の話し合い（種類、飾り、集会所） |
| | [snail_discussion.md](discussions/snail_discussion.md) | カタツムリの見た目の話し合い |
| | [bag_ui_discussion.md](discussions/bag_ui_discussion.md) | バッグUIの話し合い 1回目（覗き込む、数字の出し方） |
| | [bag_ui_discussion_2.md](discussions/bag_ui_discussion_2.md) | バッグの話し合い 2回目（入れ物の分け方、カタツムリの殻、隠し場所） |
| | [bag_ui_discussion_3.md](discussions/bag_ui_discussion_3.md) | バッグの話し合い 3回目（耐久値、ごちゃまぜ、弾は持ち込みだけ、網バッグ） |
| | [slug_gun_discussion.md](discussions/slug_gun_discussion.md) | ナメクジの銃を構造から考え直す（握り込む、空気、塩に触れない、体で受ける） |
| | [tool_carry_discussion.md](discussions/tool_carry_discussion.md) | 道具の持ち方・持ち替え・バッグが壊れたあと（エコバッグ、大物を抱える） |
| | [map_discussion.md](discussions/map_discussion.md) | マップ案（河口の塩沼、雲が東から西へ、大きさは這う速さから、スケール・増水・苔の壁） |
| | [map_discussion_2.md](discussions/map_discussion_2.md) | マップ 2回目（広さ・人数・時間、共通の天気予報、晴れの日：乾きを湿り気に、動く影、ぬめり跡） |
| | [island_clock_discussion.md](discussions/island_clock_discussion.md) | 島の時計（1日を3・5・7・9時間のどれに。毎日・毎週ずれるか、影の速さ） |
| | [moisture_discussion.md](discussions/moisture_discussion.md) | 湿り気の数字（決めた：晴れの日なた5分、塩の上は2倍、水の苔玉、120までためる） |
| | [health_damage_discussion.md](discussions/health_damage_discussion.md) | 体力とダメージ（何発で倒れるか、塩の希少性と弾の値段、ダウン、回復） |
| | [regions_discussion.md](discussions/regions_discussion.md) | 島の5区画（候補12、組み合わせ案、巣穴を掘れる場所、天気でつながる区画） |
| | [no_numbers_discussion.md](discussions/no_numbers_discussion.md) | 数字を出さない世界（バッグの数字を島でも拠点でもなくしたら：天秤、秤、体で感じる重さ、色に頼らない） |
| | [grand_nature_discussion.md](discussions/grand_nature_discussion.md) | 雄大な自然（谷の深さと見晴らし、借景）、自動生成の仕組みと完成度、ベースキャンプ、アセットとMCPでの開発 |
| | [map_size_discussion.md](discussions/map_size_discussion.md) | 区画の広さ（数字つき：一辺ごとの作る量・這う時間・人や巣穴までの距離、区画ごとに広さを変える、名所だけ作り込む、友達と作る） |
| | [no_numbers_discussion_2.md](discussions/no_numbers_discussion_2.md) | 数字 2回目：お金の塩だけは数字（人が作った物にだけ数字、天秤と分銅、塩の板、秤の解放）。学芸員を足した |
| | [persistent_island_discussion.md](discussions/persistent_island_discussion.md) | 島がずっと続く世界（試合なし）：サーバーの節約、人が必ずいて落ち着かないか、巣穴に帰る、抜けると眠る。いつもの4人＋サーバー屋・Rust勢・探索好きの社会人 |
| `ideas/` | [base_ideas.md](ideas/base_ideas.md) | 拠点の設備の案出し |
| `tech/` | [tool_craft_notes.md](tech/tool_craft_notes.md) | 作り方のメモ：ピッケル、銃3種、装備、当たり判定、縮んで伸びる、気をつけること |
| | [region_test_notes.md](tech/region_test_notes.md) | 灰色の区画（一辺 800 m）の試作と、這う時間の測定、分かったこと |
| | [weather_notes.md](tech/weather_notes.md) | 島の時計・天気・湿り気の作り方、開発用のキー、撮り方 |
| | [sound_list.md](tech/sound_list.md) | 音（効果音・BGM）の置き場所、ファイル名、取り込みの設定、要る音のリスト、鳴らすときのつなぎ口 |

話し合い（`discussions/`）は「Claude といつもの4人」の記録。廃人・スプラ好き・辛口インディーの3人は固定で、話題ごとにほかの人を足す（例：サーバー屋、Rust勢、看護師、生物系の院生、レベルデザイナー）。進め方は、まずそれぞれが意見を出す → 少し話し合う → 暗黙の了解をひっくり返した案（3つ）→ 極端案（2つ）→ まとめ。結論は spec.md に反映してある。

## Assets の地図

| フォルダ | 中身 |
| --- | --- |
| `Slug/` | キャラの体。`Scripts/Runtime`（`SlugPop` 体と顔、`SlugEquipment` ヘルメット・殻板・バッグ・殻、`SlugMeshFactory`）、`Scripts/Editor/SlugLookbook.cs`（プレハブ、決定値、確認用シーンと撮影）、`Shaders/`（`Slug/Toon`）、`Prefabs/Slug_Pop.prefab` |
| `Movement/` | 一人称で這う（`SlugMoveController`）、目とカメラ（`SlugCameraRig`）、移動の設定（`SlugMoveSettings`） |
| `Tools/Shared/` | 持ち物と持ち替え（`SlugLoadout`）、体に付いた道具（`SlugToolRack`）、出撃前の画面（`LoadoutPicker`）、その設定（`LoadoutSettings`）、伸びる腕（`SlugArmMesh`・`SlugArmSet`）、道具の形を作る（`ProfileMesh`） |
| `Tools/Pickaxe/` | ピッケル（振り `PickaxeSwing`、当たり判定 `SlugToolHolder`、形 `SlugPickaxe`、設定、欠片、面の材質） |
| `Tools/Guns/` | 銃4種（挙動 `GunHandling`、持ち方 `SlugGunHolder`、形 `SlugRifle`・`SlugShotgun`・`SlugSniper`・`SlugPistol`、弾 `SaltRound`、設定 `GunSettings`） |
| `Tools/Knife/` | ナイフ（突く `SlugKnifeHolder`、形 `SlugKnife`、設定 `KnifeSettings`） |
| `Combat/` | 部位ごとの当たり判定とダメージ倍率（`SlugHitboxes`・`SlugHitbox`） |
| `Raid/` | 採って逃げる（採取場 `Harvestable`、バッグ `SlugBag`、試合 `RaidManager`、ルール `ExtractSettings`、湿り気 `SlugMoisture`、体力 `SlugHealth`、左上の自分 `SelfPortrait`） |
| `World/` | 島の時計と天気（`IslandClock`・`IslandWeather`、見せ方 `WeatherPresenter`、湿らせる・乾かす場所 `MoistureZone`、設定 `IslandSettings`・`ConditionSettings`）。`tech/weather_notes.md` |
| `Tuning/` | **調整用のアセットはすべてここ**：`SlugMoveSettings`、`PickaxeSettings`、`RifleSettings`・`ShotgunSettings`・`SniperSettings`・`PistolSettings`、`KnifeSettings`、`LoadoutSettings`（持ち替えの時間、体に付ける位置）、`ExtractSettings`、`IslandSettings`（島の時計と天気）、`ConditionSettings`（湿り気と体力）。再生中に変えてもすぐ反映され、止めても残る |
| `TestScenes/` | 試す専用シーンの部品。`Editor/`（シーンを作るビルダー、コマ撮り `SwingFilmStrip`・`GunFilmStrip`・`MoveFilmStrip`、`HitboxShot`）、`Debug/`（画面のデバッグ表示、的）、`Generated/`（ビルダーが作る地形・マテリアル） |
| `Scenes/` | シーン（下の表） |
| `Audio/` | 音。`BGM/`、`Ambience/`（環境音）、`SE/`（効果音：体・ピッケル・銃・ナイフ・当たる・採取・バッグ・試合・画面）、`Mixer/`。決まりは [sound_list.md](tech/sound_list.md) |
| `Fonts/` | Noto Sans JP（日本語の表示用、SIL OFL） |
| `Settings/` | URP の設定と、その設定ツール（`Editor/URPSetup.cs`） |
| `_Old/` | **削除予定**。キャラデザ試作 v1〜v6 の生成物・シーン・スクリプト。今のシーンからは使っていない |

コードの namespace は作った順の名前のまま：`SlugDesign`（キャラ）、`SlugMove`（移動・道具・当たり判定・デバッグ）、`SlugExtract`（採って逃げる）。

## シーン

どれも Unity メニュー `Slug` から作り直せる（調整用アセットは上書きしない）。

| シーン | 試すこと | 作り直すメニュー |
| --- | --- | --- |
| `MoveTest` | 這う、坂、段差、縮んで伸びる | `Slug/MoveTest Build Scene` |
| `SwingTest` | ピッケルの振り（T スロー、G 軌跡） | `Slug/SwingTest Build Scene` |
| `GunTest` | 銃4種とナイフ（Tab で持ち物を選ぶ）、的、練習用ナメクジ（当たった部位と倍率） | `Slug/GunTest Build Scene` |
| `EquipTest` | 装備の見比べ（3体並び） | `Slug/EquipTest Build Scene` |
| `ExtractTest` | 出撃前に持ち物を選んで、採って逃げる（ソロ）。島の時計と天気、湿り気（F3 で開発用の数字） | `Slug/ExtractTest Build Scene`、天気だけ入れ直すなら `Slug/Update Weather (open scene)` |
| `RegionTest` | 一辺 800 m の灰色の区画（森・川・洞窟・塩田）を這う。試作 | `Slug/RegionTest Build Scene` |

どのシーンも、1 ピッケル、2 背中の銃、3 腰の道具。古い作り方のシーンに持ち物を足すのは `Slug/Update Player Loadout (open scene)`。
| `SlugLookbook` | 今のキャラを雨の背景で見る、撮る | `Slug/Lookbook Build Scene`、`Slug/Lookbook Capture Screenshots` |

## Screenshots（プロジェクト直下）

| フォルダ | 中身 |
| --- | --- |
| `Character/` | 今のキャラ（全体、顔、這う動きのコマ）。`history/` に v1・v2 の全体図 |
| `Equip/`・`Gun/`・`Move/` | 装備・銃・移動を作ったときの確認用の絵 |
| `Weather/` | 天気の時計（朝昼夕夜、曇り・雨）、湿り気の見た目、灰色の区画の見下ろし |
| `_Old/` | **削除予定**。v1 の各案の絵、v2 の案ごとの絵 |

## 履歴（git）

Oct 4, 2026 から git で管理している（ローカルだけ。どこにも送っていない）。最初のコミット「整理前の状態（ベースライン）」に戻せば、整理する前のファイルがすべてある。
