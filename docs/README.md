# プロジェクトの地図

Oct 5, 2026

ナメクジ脱出PvP（Unity 6000.5.4f1、URP 17.5.0）の、どこに何があるかの一覧。迷ったらまずここを見る。

## 最初に読むもの

| 資料 | 中身 |
| --- | --- |
| [spec.md](spec.md) | 仕様。決まったこと・仮のこと・未決定のこと（凡例【決定】【仮】【未決定】） |
| [progress.md](progress.md) | 進捗。最近決まったこと、何ができて何がまだか、まだ決まっていないこと、次にやること。**新しいチャットはまずここ（0章）から** |
| `island_map.svg` | 島の地図（区画、川、枯れた谷、塩田、境目の崖と越えられる場所）。spec の3章と同じ |
| `island_sketch.webp` | わくさんが描いた島の地図（区画の配置と広さの比率） |
| [sakurai_notes.md](sakurai_notes.md) | 「桜井政博のゲーム作るには」から、このゲームに使えることの整理（Oct 10） |
| [agenda.md](agenda.md) | ゲーム全体を見渡して、話し合うことの一覧（開発の目線・プレイヤーの目線、Oct 10） |
| [bag_spec.md](bag_spec.md) | バッグの仕様（バッグ、エコバッグ、隠し場所、カタツムリの殻、ベルトと背負い紐）。今の実装との違いと作る順番 |

## docs の中身

| フォルダ | 資料 | 中身 |
| --- | --- | --- |
| `character/` | [character_design.md](character/character_design.md) | キャラデザインの経緯（v1〜v7）と今の状態、残っている論点 |
| | [design_params.md](character/design_params.md) | 今のキャラの値の一覧（`SlugPop` のスライダー、這う動き） |
| | `face_ref.jpg` | v7 で試した平面の顔の参考画像 |
| `character/history/` | [design_candidates_v1.md](character/history/design_candidates_v1.md)、[design_candidates_v2.md](character/history/design_candidates_v2.md) | 最初の9案と、配色・目の比較の記録 |
| `discussions/` | 下の「話し合いの記録」 | Claude といつもの4人の話し合い |
| `ideas/` | [base_ideas.md](ideas/base_ideas.md) | 拠点の設備の案出し |
| `tech/` | [tool_craft_notes.md](tech/tool_craft_notes.md) | 作り方のメモ：ピッケル、銃3種、装備、当たり判定、縮んで伸びる、気をつけること |
| | [region_test_notes.md](tech/region_test_notes.md) | 灰色の区画（一辺 800 m）の試作と、這う時間の測定、分かったこと |
| | [island_test_notes.md](tech/island_test_notes.md) | 灰色の島（東西 1.5 km、島全体を1枚の地形）の試作と、這う時間・山の傾き・谷の深さの測定、分かったこと。後半は 800 m の島（IslandTest_800）：ナメクジの目の高さの見え方、這う時間 |
| | [weather_notes.md](tech/weather_notes.md) | 島の時計・天気・湿り気の作り方、開発用のキー、撮り方 |
| | [network_notes.md](tech/network_notes.md) | ネットワークの仕組みの選び方（Fish-Net ＋ Steam）、誰が何を決めるか、進める順番 |
| | [sound_list.md](tech/sound_list.md) | 音（効果音・BGM）の置き場所、ファイル名、取り込みの設定、要る音のリスト、鳴らすときのつなぎ口 |


## 話し合いの記録（`discussions/`）

廃人（タルコフ）・スプラ好きの中学生・辛口インディー評論家の3人は固定で、話題ごとにほかの人を足す（例：サーバー屋、Rust勢、看護師、生物系の院生、レベルデザイナー、写真家）。進め方は、まずそれぞれが意見を出す → 少し話し合う → 暗黙の了解をひっくり返した案（3つ）→ 極端案（2つ）→ まとめ。各ファイルの冒頭の「このあと（わくさんの判断）」が決まったこと。結論は spec.md に反映してある。

| 話題 | 資料（古い順） |
| --- | --- |
| ゲーム全体の方針 | [who_what_scope_discussion.md](discussions/who_what_scope_discussion.md)（誰に、何を、どこまで：奪い合いは面白いか、誰のためのゲームか、最初に出す形。`agenda.md` の 0-1〜0-3） |
| プレイヤーの目線 | 桜井メモ係（`sakurai_notes.md` の回だけを根拠に話す役）を入れて：[first_hour_discussion.md](discussions/first_hour_discussion.md)（はじめての1時間。agenda 2-1）、[sound_map_discussion.md](discussions/sound_map_discussion.md)（音の地図。2-9）、[friend_or_foe_discussion.md](discussions/friend_or_foe_discussion.md)（敵と仲間の見分け。2-3） |
| 島とマップ | [map_discussion.md](discussions/map_discussion.md)（河口の塩沼、スケール）→ [map_discussion_2.md](discussions/map_discussion_2.md)（広さ・人数・時間、共通の予報、晴れの日）→ [persistent_island_discussion.md](discussions/persistent_island_discussion.md)（ずっと続く島、サーバー、抜けると眠る）→ [regions_discussion.md](discussions/regions_discussion.md)（5区画）→ [map_size_discussion.md](discussions/map_size_discussion.md)（広さを数字で）→ [grand_nature_discussion.md](discussions/grand_nature_discussion.md)（雄大さは高さ、自動生成、ベースキャンプ）→ [seamless_island_discussion.md](discussions/seamless_island_discussion.md)（島ひとつをシームレスに、川と塩の流れ、島の大きさ。絵は `island_sketch.webp`）→ [meadow_landmark_discussion.md](discussions/meadow_landmark_discussion.md)（花の草原の目印：菌輪）、[forest_landmark_discussion.md](discussions/forest_landmark_discussion.md)（雨の森の目印：大ガジュマル）→ [sortie_flow_discussion.md](discussions/sortie_flow_discussion.md)（島が小さくなったあとの出撃の流れ・稼ぎ・塩のきのこの数）、[island_population_discussion.md](discussions/island_population_discussion.md)（島の人の密度）→ [terrain_detail_discussion.md](discussions/terrain_detail_discussion.md)（灰色の島がでかすぎる、谷の作り直し、細かい凹凸）、[bag_fill_discussion.md](discussions/bag_fill_discussion.md)（バッグがすぐいっぱい）→ [archipelago_discussion.md](discussions/archipelago_discussion.md)（ゼルダくらいの広さ？ 500 m の島の群島）→ [bag_full_discussion.md](discussions/bag_full_discussion.md)（いっぱいになるまで採るバッグ：積み重ね、重さ、経済）→ [island_height_discussion.md](discussions/island_height_discussion.md)（2人で：どこでも越えられる境目、平場、高さの全面見直し、火口の丘と溶岩原、ナメクジの目線とゼルダの三角）→ [island_flow_discussion.md](discussions/island_flow_discussion.md)（島ぜんぶの動線：崖の帯と尾根4本、肩の道、谷の裏口） → [sniping_discussion.md](discussions/sniping_discussion.md)（狙撃が強すぎない？：狙撃の窓、白い筋、見下ろす場所の決まり）→ [moisture_readability_discussion.md](discussions/moisture_readability_discussion.md)（乾く所・湿った所が分かりづらい：地面の濡れ具合） |
| 天気と体 | [island_clock_discussion.md](discussions/island_clock_discussion.md)（1日9時間、月と潮）、[moisture_discussion.md](discussions/moisture_discussion.md)（湿り気の数字） |
| 戦い | [health_damage_discussion.md](discussions/health_damage_discussion.md)（何発で倒れるか、塩の希少性）、[slug_gun_discussion.md](discussions/slug_gun_discussion.md)（銃を構造から考え直す）→ [movement_discussion.md](discussions/movement_discussion.md)（プレイヤーの動き：速く這うと縮んで伸びるのかぶり、目的で分ける、バッグを投げ捨てる） |
| 数字と見せ方 | [no_numbers_discussion.md](discussions/no_numbers_discussion.md)（数字を出さない世界）→ [no_numbers_discussion_2.md](discussions/no_numbers_discussion_2.md)（お金の塩だけは数字） |
| バッグと道具 | [bag_ui_discussion.md](discussions/bag_ui_discussion.md) → [bag_ui_discussion_2.md](discussions/bag_ui_discussion_2.md) → [bag_ui_discussion_3.md](discussions/bag_ui_discussion_3.md) → [tool_carry_discussion.md](discussions/tool_carry_discussion.md)（持ち方、エコバッグ） |
| 拠点とキャラ | [base_discussion.md](discussions/base_discussion.md)（拠点）、[snail_discussion.md](discussions/snail_discussion.md)（カタツムリ） |
| 採取物と経済 | [plants_discussion.md](discussions/plants_discussion.md)（島に生える植物 10 種類、場所・条件・レア度。Claude が決めた）→ [plant_names_discussion.md](discussions/plant_names_discussion.md)（名前の決まり、ふだんの呼び名と古い名前）、[plant_uses_discussion.md](discussions/plant_uses_discussion.md)（売る以外の使い道、苗床、根ごと掘ったときのサーバー）、[market_discussion.md](discussions/market_discussion.md)（店と取引：カタツムリの行商人、動く値段、手渡しだけ）→ [economy_balance_discussion.md](discussions/economy_balance_discussion.md)（経済の数字：アモレの比率、岩塩を掘る回数、弾と装備の値段、相場、出口）→ [economy_review_discussion.md](discussions/economy_review_discussion.md)（取りこぼしと改善案） |

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
| `Raid/` | 採って逃げる（採取場 `Harvestable`（形の段階つき）、手で摘む植物 `Pickable`、バッグ `SlugBag`、試合 `RaidManager`、ルール `ExtractSettings`、湿り気 `SlugMoisture`、体力 `SlugHealth`、左上の自分 `SelfPortrait`、島の時計と天気で育つ・生える `SaltRegrow`・`UgoTakeFlush`（土台は `IslandTimeFollower`）） |
| `World/` | 島の時計と天気（`IslandClock`・`IslandWeather`、見せ方 `WeatherPresenter`、湿らせる・乾かす場所 `MoistureZone`、設定 `IslandSettings`・`ConditionSettings`）。`tech/weather_notes.md` |
| `Tuning/` | **調整用のアセットはすべてここ**：`SlugMoveSettings`、`PickaxeSettings`、`RifleSettings`・`ShotgunSettings`・`SniperSettings`・`PistolSettings`、`KnifeSettings`、`LoadoutSettings`（持ち替えの時間、体に付ける位置）、`ExtractSettings`、`IslandSettings`（島の時計と天気）、`ConditionSettings`（湿り気と体力）。再生中に変えてもすぐ反映され、止めても残る |
| `TestScenes/` | 試す専用シーンの部品。`Editor/`（シーンを作るビルダー、コマ撮り `SwingFilmStrip`・`GunFilmStrip`・`MoveFilmStrip`、`HitboxShot`）、`Debug/`（画面のデバッグ表示、的）、`Generated/`（ビルダーが作る地形・マテリアル） |
| `Art/Models/` | Blender で作ったモデル（1単位＝1 m、地面に置く物は底の真ん中に原点）。`RockSalt/`：岩塩の塊3種・欠片3種と、そのマテリアル（Slug/Toon、リアル度 0.2）。`RainTurnip/`：雨カブ（畑に植わっている葉の大きい `Planted` と、掘って出てくる葉の小さい `Item`。玉は同じ）と土の小山2種。`UgoTake/`：ウゴタケと倒木。`Art/Editor/HarvestVisuals.cs` がシーンの採る場所をモデルに差し替える（メニュー `Slug/Update Harvest Visuals (open scene)`）。`Art/Editor/WorldLook.cs` が世界の物の質感の基準（形はポップ、質感はリアル度 0.2）。作り方のスクリプトと .blend はプロジェクト直下の `Blender/`（`rocksalt_gen.py`、`rainturnip_gen.py` などを Blender の MCP か Scripting タブで実行すると作り直して書き出す）。モデリングを頼むときの指示の型と作り方は `.claude/skills/modeling/`（共通の型 `SKILL.md`、`map.md`・`slug.md`・`plant.md`・`ore.md`） |
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
| `MoveTest` | 這う、坂（15°・45°・70°、登れる限界の 40°・50°・60°）、段差、縮んで伸びる | `Slug/MoveTest Build Scene` |
| `SwingTest` | ピッケルの振り（T スロー、G 軌跡） | `Slug/SwingTest Build Scene` |
| `GunTest` | 銃4種とナイフ（Tab で持ち物を選ぶ）、的、練習用ナメクジ（当たった部位と倍率） | `Slug/GunTest Build Scene` |
| `EquipTest` | 装備の見比べ（3体並び） | `Slug/EquipTest Build Scene` |
| `ExtractTest` | 出撃前に持ち物を選んで、採って逃げる（ソロ）。島の時計と天気、湿り気（F3 で開発用の数字） | `Slug/ExtractTest Build Scene`、天気だけ入れ直すなら `Slug/Update Weather (open scene)` |
| `IslandTest` | 島全体（東西 1.5 km）の灰色の試作。岩塩の山、溶岩台地と火口、砂漠の台地と枯れた谷、森と大ガジュマル（仮）、低い草原と菌輪、塩田、川2本、海。F4 で10倍速で這う。`tech/island_test_notes.md` | `Slug/IslandTest Build Scene` |
| `IslandTest_800` | 800 m の島の灰色の試作（Oct 9 の高さ）。段と平場のある岩塩の山とたまり池・滝、火口の丘と黒い溶岩原（流れ、キプカ、天窓）、低い砂漠と赤い台地・枯れた谷・岩山、森と大ガジュマル（仮）、草原と菌輪、塩田、川2本。道3本とけものみち5本（つづら折り）、隠れる物（木、下草、草むら、岩、台地の岩の柱など約 1,350 個）。`Map/見る場所` に話し合った地点。`tech/island_test_notes.md` | `Slug/IslandTest 800 m/Build Scene`、`Shoot Views`、`Measure Routes`、`Measure Ways`、`Shoot Ways And Cover`、`Shoot Rivers`、`Shoot Falls`、`Measure Flow`（14か所の間の早い這い方と、道筋の重なる地図） |
| `RegionTest` | 一辺 800 m の灰色の区画（森・川・洞窟・塩田）を這う。試作 | `Slug/RegionTest Build Scene` |

どのシーンも、1 ピッケル、2 背中の銃、3 腰の道具。古い作り方のシーンに持ち物を足すのは `Slug/Update Player Loadout (open scene)`。
| `SlugLookbook` | 今のキャラを雨の背景で見る、撮る | `Slug/Lookbook Build Scene`、`Slug/Lookbook Capture Screenshots` |

## Screenshots（プロジェクト直下）

| フォルダ | 中身 |
| --- | --- |
| `Character/` | 今のキャラ（全体、顔、這う動きのコマ）。`history/` に v1・v2 の全体図 |
| `Equip/`・`Gun/`・`Move/` | 装備・銃・移動を作ったときの確認用の絵 |
| `Weather/` | 天気の時計（朝昼夕夜、曇り・雨）、湿り気の見た目、灰色の区画の見下ろし |
| `Island/` | 灰色の島の上からの全体、斜め、這う目の高さからの見え方 |
| `Island800/` | 800 m の島：話し合った地点からのナメクジの目の高さの景色（01〜13）、道の上と隠れる物（20〜26）、川（30〜36）、滝（40〜43）、空から、動線の地図（`flow_before_map.svg`・`flow_after_map.svg`） |
| `_Old/` | **削除予定**。v1 の各案の絵、v2 の案ごとの絵 |

## 履歴（git）

Oct 4, 2026 から git で管理している（ローカルだけ。どこにも送っていない）。最初のコミット「整理前の状態（ベースライン）」に戻せば、整理する前のファイルがすべてある。
