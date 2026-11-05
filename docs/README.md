# プロジェクトの地図

Nov 3, 2026（Oct 5 に作り、Nov 3 に見直した）

ナメクジ脱出PvP（Unity 6000.5.4f1、URP 17.5.0）の、どこに何があるかの一覧。迷ったらまずここを見る。

## 最初に読むもの

| 資料 | 中身 |
| --- | --- |
| [spec.md](spec.md) | 仕様。**いまの決まりだけ**（凡例【決定】【仮】【未決定】）。バッグの仕様も 7章 |
| [progress.md](progress.md) | 進捗。最近決まったこと、何ができて何がまだか、まだ決まっていないこと、次にやること。**新しいチャットはまずここ（0章）から** |
| [spec_history.md](spec_history.md) | 仕様の経緯：前の決まり（いつ、どう変わったか）、不採用の案、前の拠点の案。ふだんは読まない |
| `island_map.svg` | 島の地図（区画、川、枯れた谷、塩田、境目の崖と越えられる場所）。spec の3章と同じ |
| `island_sketch.webp` | わくさんが描いた島の地図（区画の配置と広さの比率） |
| [sakurai_notes.md](sakurai_notes.md) | 「桜井政博のゲーム作るには」から、このゲームに使えることの整理（Oct 10） |
| [agenda.md](agenda.md) | ゲーム全体を見渡して、話し合うことの一覧（開発の目線・プレイヤーの目線、Oct 10） |
| `../AGENTS.md` | ほかの AI（Antigravity の Argon など）がこのプロジェクトで働くときの入口。モデリングと音の決まりの場所、受け持ち、Blender と Unity の MCP の使い方 |

## docs の中身

| フォルダ | 資料 | 中身 |
| --- | --- | --- |
| `character/` | [dino_slug_impact.md](character/dino_slug_impact.md) | **2本足の体**：Nov 2 に決めたこと、作り直すコード・音・設定、進み |
| | [character_design.md](character/character_design.md) | 前の体（Nov 1 まで）のキャラデザインの経緯（v1〜v7）と状態 |
| | [design_params.md](character/design_params.md) | 前の体の値の一覧（`SlugPop` のスライダー、体を波打たせる動き） |
| | `face_ref.jpg` | v7 で試した平面の顔の参考画像 |
| `character/history/` | [design_candidates_v1.md](character/history/design_candidates_v1.md)、[design_candidates_v2.md](character/history/design_candidates_v2.md) | 最初の9案と、配色・目の比較の記録 |
| `discussions/` | 下の「話し合いの記録」 | Claude といつもの4人の話し合い |
| `ideas/` | [base_ideas.md](ideas/base_ideas.md) | 拠点の設備の案出し |
| | [body_signal_ideas.md](ideas/body_signal_ideas.md) | 触角のほかで体の状態（体力・仲間の印）を伝える案。反転案と極端案（Oct 25） |
| | [map_clutter_ideas.md](ideas/map_clutter_ideas.md) | マップをごちゃごちゃにする案（足元の層・塊・理由のある固まり・ナメクジの跡。Oct 20、仮の形で試した絵つき） |
| | [cairn_terrain_ideas.md](ideas/cairn_terrain_ideas.md) | ケルン風の地形：集めた資料（開発者の言葉・公式の絵・批評）と、このゲームでの作り方の案（遠くの影、地面の陰の段、描いた空、崖を岩の部品に。無難と攻め、Nov 3） |
| | [wind_ideas.md](ideas/wind_ideas.md) | 島の風の案（雲・葉・草が同じ風で動く、Oct 25） |
| | [sound_ideas_other_games.md](ideas/sound_ideas_other_games.md) | 他のゲームにあって、いまない音（Nov 2） |
| `modeling/` | [README.md](modeling/README.md) | **モデリングの決まり（Claude と Argon 共通、これ1枚が正）**：世界観チェック、寸法と書き出し、進め方（読み返し票→灰色ラフの3枚→色→Unity）、トークンの節約、モデル票の書き方、ナメクジの動き（動き票→止め絵→コマ撮り）、地形のコード（1ファイル 500 行まで、決まった所は焼いて固める）、わくさんの好み（ケルン風の植物、かじれるかで面を分ける、大きい物は形・小さい物は質感） |
| | [to_model_list.md](modeling/to_model_list.md) | 島のためにモデリングする物のリスト（名所・熱帯の植物・小物・ナメクジの跡・採る物、おすすめの順。島の自然は Claude が Unity のコードで作る） |
| | [big_banyan.md](modeling/big_banyan.md)（訳 [big_banyan_ja.md](modeling/big_banyan_ja.md)） | 大ガジュマルのモデル票（Oct 20） |
| | [carrier_bird_code.md](modeling/carrier_bird_code.md) | 籠を運ぶ鳥のコード版の票（Oct 30。鳥はこれで完成） |
| | [cairn.md](modeling/cairn.md) | 石積みの道しるべ（ケルン）の票（Oct 25、Argon の試し） |
| | [bag_vest_plan.md](modeling/bag_vest_plan.md) | 布のバッグとベスト（胸の弾入れ）を作る順と、できた形（Oct 26〜27） |
| | 前の版（使わない） | 前の拠点「セノーテの洞窟」[base_cenote.md](modeling/base_cenote.md)（訳 [base_cenote_ja.md](modeling/base_cenote_ja.md)）、前の拠点の部屋ごとの家具・器具 [base_furniture.md](modeling/base_furniture.md)（訳 [base_furniture_ja.md](modeling/base_furniture_ja.md)）、Blender 版の鳥 [carrier_bird.md](modeling/carrier_bird.md)（訳 [carrier_bird_ja.md](modeling/carrier_bird_ja.md)、わくさんの絵 `reference/carrier_bird_sketch_oct18.webp`）、拠点と鳥を Argon が作り直す進め方 [base_rebuild_plan.md](modeling/base_rebuild_plan.md)（Oct 30。Nov 2 に拠点は Claude が作ることになった） |
| `modeling/reference/` | | 新しい拠点の目標の絵 `base_concept_nov02_*.webp`（3枚）とそのプロンプト [base_image_prompts.md](modeling/reference/base_image_prompts.md)、前の拠点を回して見られる3D `cenote_base_viewer.html`、前の拠点で籠を下ろす場所4案の比べ `basket_landing_options.html`（Oct 18） |
| `tech/` | [tool_craft_notes.md](tech/tool_craft_notes.md) | 作り方のメモ：ピッケル、銃4種、ナイフ（作らない。コードだけ）、装備、当たり判定、縮んで伸びる、気をつけること |
| | [code_spec_gaps.md](tech/code_spec_gaps.md) | コードと spec の数字のずれ（調整の値はわくさんが直す） |
| | [region_test_notes.md](tech/region_test_notes.md) | 灰色の区画（一辺 800 m）の試作と、動く時間の測定、分かったこと |
| | [island_test_notes.md](tech/island_test_notes.md) | 灰色の島（東西 1.5 km、島全体を1枚の地形）の試作と、動く時間・山の傾き・谷の深さの測定、分かったこと。後半は 800 m の島（IslandTest_800）：ナメクジの目の高さの見え方、動く時間 |
| | [weather_notes.md](tech/weather_notes.md) | 島の時計・天気・湿り気の作り方、開発用のキー、撮り方 |
| | [network_notes.md](tech/network_notes.md) | ネットワークの仕組みの選び方（Fish-Net ＋ Steam）、誰が何を決めるか、進める順番 |
| | [sound_list.md](tech/sound_list.md) | 音（効果音・BGM）の置き場所、ファイル名、取り込みの設定、要る音のリスト、鳴らすときのつなぎ口 |
| `sound/` | [README.md](sound/README.md) | 音づくりの決まり（Argon 用）：わくさんの擬音から波形の計算で効果音を作る進め方と書き出し |
| | [requests.md](sound/requests.md) | 音の依頼票：わくさんの擬音と、作る順番 |
| | [adobe_prompts.md](sound/adobe_prompts.md) | Adobe の AI で作る音 10個（わくさん用、Nov 2） |
| （docs 直下） | [worklog.md](worklog.md) | やりとり帳。Claude と Argon の伝言と短い記録（いま作業中の札、受け持ち）。古い記録は `worklog_old.md` |
| | [waku_persona.md](waku_persona.md) | わくさんのペルソナ。これまでの会話から Claude が描いた像（大事にしていること、考え方のくせ、好きな形、AI との組み方。Nov 2） |
| | [friend_guide.md](friend_guide.md) | 仲間入りの手引き。Unity・Claude・GitHub が初めての友達向け：ゲームの概要、わくの作り方、道具のそろえ方、遊び方、GitHub の流れ、Claude への頼み方、触らない物（Nov 5） |
| | `bag_spec.md` | Nov 3 に spec 7章へ移した（中身は空。消してよい） |

## 話し合いの記録（`discussions/`）

廃人（タルコフ）・スプラ好きの中学生・辛口インディー評論家の3人は固定で、話題ごとにほかの人を足す（例：サーバー屋、Rust勢、看護師、生物系の院生、レベルデザイナー、写真家）。いまの進め方は、前提 → それぞれの考え（＋短い選択肢）。形や遊び方が大きく変わるときは「無難」と「攻め」の2つを出す。暗黙の了解をひっくり返した案・極端案は頼まれたときだけ。各ファイルの冒頭の「このあと（わくさんの判断）」が決まったこと。結論は spec.md に書いてある（変わる前の形は spec_history.md）。

| 話題 | 資料（古い順） |
| --- | --- |
| ゲーム全体の方針 | [who_what_scope_discussion.md](discussions/who_what_scope_discussion.md)（誰に、何を、どこまで：奪い合いは面白いか、誰のためのゲームか、最初に出す形。`agenda.md` の 0-1〜0-3） |
| 話し合いのやり方 | [discussion_method_discussion.md](discussions/discussion_method_discussion.md)（平均に寄らない案の出し方。おすすめを無難と攻めの2つ、Oct 18） |
| プレイヤーの目線 | 桜井メモ係（`sakurai_notes.md` の回だけを根拠に話す役）を入れて：[first_hour_discussion.md](discussions/first_hour_discussion.md)（はじめての1時間。agenda 2-1）、[sound_map_discussion.md](discussions/sound_map_discussion.md)（音の地図。2-9）、[friend_or_foe_discussion.md](discussions/friend_or_foe_discussion.md)（敵と仲間の見分け。2-3） |
| 島とマップ | [map_discussion.md](discussions/map_discussion.md)（河口の塩沼、スケール）→ [map_discussion_2.md](discussions/map_discussion_2.md)（広さ・人数・時間、共通の予報、晴れの日）→ [persistent_island_discussion.md](discussions/persistent_island_discussion.md)（ずっと続く島、サーバー、抜けると眠る）→ [regions_discussion.md](discussions/regions_discussion.md)（5区画）→ [map_size_discussion.md](discussions/map_size_discussion.md)（広さを数字で）→ [grand_nature_discussion.md](discussions/grand_nature_discussion.md)（雄大さは高さ、自動生成、ベースキャンプ）→ [seamless_island_discussion.md](discussions/seamless_island_discussion.md)（島ひとつをシームレスに、川と塩の流れ、島の大きさ。絵は `island_sketch.webp`）→ [meadow_landmark_discussion.md](discussions/meadow_landmark_discussion.md)（花の草原の目印：菌輪）、[forest_landmark_discussion.md](discussions/forest_landmark_discussion.md)（雨の森の目印：大ガジュマル）→ [sortie_flow_discussion.md](discussions/sortie_flow_discussion.md)（島が小さくなったあとの出撃の流れ・稼ぎ・塩のきのこの数）、[island_population_discussion.md](discussions/island_population_discussion.md)（島の人の密度）→ [terrain_detail_discussion.md](discussions/terrain_detail_discussion.md)（灰色の島がでかすぎる、谷の作り直し、細かい凹凸）、[bag_fill_discussion.md](discussions/bag_fill_discussion.md)（バッグがすぐいっぱい）→ [archipelago_discussion.md](discussions/archipelago_discussion.md)（ゼルダくらいの広さ？ 500 m の島の群島）→ [bag_full_discussion.md](discussions/bag_full_discussion.md)（いっぱいになるまで採るバッグ：積み重ね、重さ、経済）→ [island_height_discussion.md](discussions/island_height_discussion.md)（2人で：どこでも越えられる境目、平場、高さの全面見直し、火口の丘と溶岩原、ナメクジの目線とゼルダの三角）→ [island_flow_discussion.md](discussions/island_flow_discussion.md)（島ぜんぶの動線：崖の帯と尾根4本、肩の道、谷の裏口） → [sniping_discussion.md](discussions/sniping_discussion.md)（狙撃が強すぎない？：狙撃の窓、白い筋、見下ろす場所の決まり）→ [moisture_readability_discussion.md](discussions/moisture_readability_discussion.md)（乾く所・湿った所が分かりづらい：地面の濡れ具合）→ [buildings_discussion.md](discussions/buildings_discussion.md)（島に建物を置くか：上下の戦い、呼び名、屋根の日陰、枯れた谷の崖の村） |
| 天気と体 | [island_clock_discussion.md](discussions/island_clock_discussion.md)（島の1日の長さ、月と潮。いまは 9.5 時間）、[moisture_discussion.md](discussions/moisture_discussion.md)（湿り気の数字）、[rain_color_discussion.md](discussions/rain_color_discussion.md)（雨の日の島の色：大雨で鮮やかな色ほど目立つをどう作るか、Oct 27） |
| 戦い | [health_damage_discussion.md](discussions/health_damage_discussion.md)（何発で倒れるか、塩の希少性）、[slug_gun_discussion.md](discussions/slug_gun_discussion.md)（銃を構造から考え直す）→ [movement_discussion.md](discussions/movement_discussion.md)（プレイヤーの動き：速く動くのと縮んで伸びるのかぶり、目的で分ける、バッグを投げ捨てる） |
| 数字と見せ方 | [no_numbers_discussion.md](discussions/no_numbers_discussion.md)（数字を出さない世界）→ [no_numbers_discussion_2.md](discussions/no_numbers_discussion_2.md)（お金の塩だけは数字） → [friend_playtest_discussion.md](discussions/friend_playtest_discussion.md)（友達がボットと遊んだ感想から：銃と防具の耐久、自動回収、撃たれたときの反応と倒れた理由、乾きの見せ方）→ [peek_numbers_discussion.md](discussions/peek_numbers_discussion.md)（Q で覗くときに残りの弾とバッグの空きを数字で出すか、Oct 26） |
| バッグと道具 | [bag_ui_discussion.md](discussions/bag_ui_discussion.md) → [bag_ui_discussion_2.md](discussions/bag_ui_discussion_2.md) → [bag_ui_discussion_3.md](discussions/bag_ui_discussion_3.md) → [tool_carry_discussion.md](discussions/tool_carry_discussion.md)（持ち方、エコバッグ） |
| 装備 | 桜井メモ係を入れて（Oct 21）：[full_bag_draw_discussion.md](discussions/full_bag_draw_discussion.md)（膨らんだバッグと持ち替え）、[lunge_camera_discussion.md](discussions/lunge_camera_discussion.md)（縮んで伸びるときの画面、跳びながら撃つ）、[helmet_tentacle_discussion.md](discussions/helmet_tentacle_discussion.md)（触角とヘルメット、乾きにくい防具）、[fallen_gear_discussion.md](discussions/fallen_gear_discussion.md)（倒れたときの装備） |
| 拠点 | [base_discussion.md](discussions/base_discussion.md)（拠点）→ [base_layout_discussion.md](discussions/base_layout_discussion.md)（本版の拠点の形：大きさ・形・材質・部屋の並び、デッキ、建築家の構造案）→ [sortie_launch_discussion.md](discussions/sortie_launch_discussion.md)（島へ出発する瞬間から拠点の形を考える：吹き抜けを打ち上がる）→ [flying_carrier_discussion.md](discussions/flying_carrier_discussion.md)（拠点の飛ぶ生き物に行き帰りを運んでもらう）→ [base_storage_discussion.md](discussions/base_storage_discussion.md)（拠点での物の扱い：マス目の箱か現物か）→ [base_storage_discussion_2.md](discussions/base_storage_discussion_2.md)（まとめると傷むまでの期限が見えない）→ [base_furniture_discussion.md](discussions/base_furniture_discussion.md)（部屋ごとの家具・器具の案を直す。Claude が決めた）・[base_alive_discussion.md](discussions/base_alive_discussion.md)（拠点の生きてる感じ）・[base_comfort_discussion.md](discussions/base_comfort_discussion.md)（設備をナメクジ目線で使いやすく）・[base_shop_discussion.md](discussions/base_shop_discussion.md)（問屋の見た目）（ここまで前の拠点、Oct 15〜18）→ [base_rethink.md](discussions/base_rethink.md)（Oct 30、拠点を構想から考えなおす筋道）→ [base_growth_discussion.md](discussions/base_growth_discussion.md)（育つときの見せ方）・[base_handwork_discussion.md](discussions/base_handwork_discussion.md)（工房で手をどれだけ動かすか）→ [base_facilities_plan.md](discussions/base_facilities_plan.md)（設備の案：水の流れに沿って、動線。Oct 30〜Nov 2）→ [base_display_ui_discussion.md](discussions/base_display_ui_discussion.md)（持ち帰った物とアモレの見せ方・UI、ミニアモレ。Nov 3）→ [base_layout_v2.md](discussions/base_layout_v2.md)（構想2版：岩の段々と板の足場、水路3本。Nov 3） |
| キャラと成長 | [snail_discussion.md](discussions/snail_discussion.md)（カタツムリ）、[body_color_wipe_discussion.md](discussions/body_color_wipe_discussion.md)（体の色と大干ばつ、色が得になる名所、Oct 25）、[progression_discussion.md](discussions/progression_discussion.md)（育成：熟練・体を鍛える・秘伝書を考え直す、Oct 25〜26）、[artifact_discussion.md](discussions/artifact_discussion.md)（書物などの人工物のレアアイテム：塩とガラスの中だけ残った人の物、何を強くするか、Nov 3） |
| 採取物と経済 | [plants_discussion.md](discussions/plants_discussion.md)（島に生える植物 10 種類、場所・条件・レア度。Claude が決めた）→ [plant_names_discussion.md](discussions/plant_names_discussion.md)（名前の決まり、ふだんの呼び名と古い名前）、[plant_uses_discussion.md](discussions/plant_uses_discussion.md)（売る以外の使い道、苗床、根ごと掘ったときのサーバー）、[market_discussion.md](discussions/market_discussion.md)（店と取引：カタツムリの行商人、動く値段、手渡しだけ）→ [economy_balance_discussion.md](discussions/economy_balance_discussion.md)（経済の数字：アモレの比率、岩塩を掘る回数、弾と装備の値段、相場、出口）→ [economy_review_discussion.md](discussions/economy_review_discussion.md)（取りこぼしと改善案）→ [economy_bag30_discussion.md](discussions/economy_bag30_discussion.md) → [economy_bag30_discussion_2.md](discussions/economy_bag30_discussion_2.md)（経済をバッグ 30 で考え直す：10倍で数える、装備はいまの値段・弾を3倍、問屋10割・行商人1.3倍。Oct 25） |

## Assets の地図

| フォルダ | 中身 |
| --- | --- |
| `Slug/` | キャラの体。2本足の体：`Scripts/Runtime/DinoSlugBody`（歩く・止まる・尾・腕の付け根）、`Scripts/Editor/DinoSlugPhotoRough.cs`（形）・`DinoSlugRig.cs`（骨。`Prefabs/DinoSlug.prefab` を作る）。前の体：`Scripts/Runtime`（`SlugPop` 体と顔、`SlugEquipment` ヘルメット・殻板・バッグ・殻、`SlugCanvasBag` 布のバッグの形、`SlugMeshFactory`）、`Scripts/Editor/SlugLookbook.cs`（プレハブ、決定値、確認用シーンと撮影）、`Prefabs/Slug_Pop.prefab`。2つの体の共通の親 `SlugBodyBase`（道具・腕・背中の置き場所はこの型を見る）。`Shaders/`（`Slug/Toon`） |
| `Movement/` | 一人称の移動（`SlugMoveController`）、目とカメラ（`SlugCameraRig`）、移動の設定（`SlugMoveSettings`） |
| `Tools/Shared/` | 持ち物と持ち替え（`SlugLoadout`）、体に付いた道具（`SlugToolRack`）、背中の物の出し入れ（`BackDraw`）、出撃前の画面（`LoadoutPicker`）、その設定（`LoadoutSettings`）、伸びる腕（`SlugArmMesh`・`SlugArmSet`）、道具の形を作る（`ProfileMesh`） |
| `Tools/Pickaxe/` | ピッケル（振り `PickaxeSwing`、当たり判定 `SlugToolHolder`、形 `SlugPickaxe`、設定、欠片、面の材質） |
| `Tools/Guns/` | 銃4種（挙動 `GunHandling`、持ち方 `SlugGunHolder`、形 `SlugRifle`・`SlugShotgun`・`SlugSniper`・`SlugPistol`、弾 `SaltRound`、設定 `GunSettings`） |
| `Tools/Knife/` | ナイフ（作らない。コードだけ残す：突く `SlugKnifeHolder`、形 `SlugKnife`、設定 `KnifeSettings`） |
| `Combat/` | 部位ごとの当たり判定とダメージ倍率（`SlugHitboxes`・`SlugHitbox`） |
| `Raid/` | 採って逃げる（採取場 `Harvestable`（形の段階つき）、手で摘む植物 `Pickable`、ツユリ `TsuyuriPlant`、バッグ `SlugBag`・`GroundBag`、試合 `RaidManager`、ルール `ExtractSettings`、湿り気 `SlugMoisture`、体力 `SlugHealth`、画面の端の塩 `SaltCrust`、日なたの画面の光 `SunGlare`（Nov 3 から使わない）、左上の自分 `SelfPortrait`、ボット `SlugBot`、倒れた装備の抜け殻 `SlugHusk`・落ちた物 `FallenThing`、音 `SlugSounds`・`SoundBank`、島の時計と天気で育つ・生える `SaltRegrow`・`UgoTakeFlush`（土台は `IslandTimeFollower`）） |
| `World/` | 島の時計と天気（`IslandClock`・`IslandWeather`、見せ方と空気の霞 `WeatherPresenter`、湿らせる・乾かす場所 `MoistureZone`、地面の種類 `GroundKinds`・`IslandGroundMap`、島の光の筋 `SunShafts`、設定 `IslandSettings`・`ConditionSettings`）。`tech/weather_notes.md` |
| `Tuning/` | **調整用のアセットはすべてここ**：`SlugMoveSettings`、`PickaxeSettings`、`RifleSettings`・`ShotgunSettings`・`SniperSettings`・`PistolSettings`、`KnifeSettings`、`LoadoutSettings`（持ち替えの時間、体に付ける位置）、`ExtractSettings`、`IslandSettings`（島の時計と天気）、`ConditionSettings`（湿り気と体力）、`SoundBank`（わくさんの音の差し替え）。再生中に変えてもすぐ反映され、止めても残る |
| `TestScenes/` | 試す専用シーンの部品。`Editor/`（シーンを作るビルダー、コマ撮り `SwingFilmStrip`・`GunFilmStrip`・`MoveFilmStrip`・`DinoFilmStrip`・`DinoToolStrip`・`DrawFilmStrip`、`HitboxShot`）、`Debug/`（画面のデバッグ表示、的）、`Generated/`（ビルダーが作る地形・マテリアル） |
| `Art/Editor/` | 形を作るコード（ビルダー）。`Nature/`（島の自然。Claude。共通の部品 `NatureMeshKit`・`PropKit`・`FlowerKit`、植物の手本 `MassBushBuilder`、並べるシーン `IslandPropsLook`）、`Base/`（新しい拠点の灰色の箱 `BaseGrayboxBuilder` ほか、Argon のコード版の前の拠点 `Cenote*`）、`Creatures/`（鳥 `CarrierBirdBuilder`）、`Props/`（ケルン `CairnBuilder`）。`HarvestVisuals.cs` がシーンの採る場所をモデルに差し替える（メニュー `Slug/Update Harvest Visuals (open scene)`）。`WorldLook.cs` が世界の物の質感の基準（形はポップ、質感はリアル度 0.2） |
| `Art/Nature/` | 島の自然のビルダーが作った物（メッシュ・マテリアル・プレハブ。種類ごとの箱） |
| `Art/Models/` | モデル（1単位＝1 m、地面に置く物は底の真ん中に原点）。種類ごとの箱：`Harvest/`（採る物。`RockSalt/`：塩のきのこ `Mushroom/RockSaltMush_M5_All`・`_Half`・`_Stump`、手に持つ塊 `RockSalt_Chunk_A〜C`、`RainTurnip/`：雨カブ（畑の `Planted`、掘った `Item`、土の小山2種）、`UgoTake/`：ウゴタケと倒木）、`Base/`（拠点。`Graybox/`：新しい拠点の灰色の箱。前の拠点は Blender の FBX 版 `CenoteBase/`・`CenoteFurniture/`・`Materials/`・`Textures/`（地図の台の地図の絵 `CenoteBase_MapChart.png`）と、Argon のコード版 `CenoteBaseCode/`・`CenoteFurnitureCode/`（Oct 30、使わない）。拠点の FBX は Z が上のまま書き出し、Unity の取り込み設定 Bake Axis Conversion を ON にしている（Blender の +Y が Unity の +Z））、`Creatures/`（生き物。`CarrierBirdCode/`：籠を運ぶ鳥のコード版＝いま使う。`CarrierBird/`：Blender 版の鳥（前の版））、`Landmarks/`（名所）、`Props/`（小物）。Blender の作り方のスクリプトと .blend はプロジェクト直下の `Blender/<種類>/`（前の版。例 `Blender/Harvest/rocksalt_gen.py`、`Blender/Base/cenote_base_gen.py`、`Blender/Creatures/carrier_bird_gen.py`）。使わなくなった試作は `Blender/_Old/`。モデリングの決まりは `docs/modeling/README.md`、Claude 向けの指示の型は `.claude/skills/modeling/`。ほかの AI（Argon など）への入口はプロジェクト直下の `AGENTS.md` |
| `Base/` | 拠点のシーンの部品（前の拠点 CenoteBase 用）。部屋の段階 S1〜S3 の切り替え（`CenoteRoom`）、釣瓶（`CenoteLift`：低い板に乗ると上がる）、開発用（`CenoteBaseDev`：F6、海に落ちたら戻す）、鳥の動きと出発の飛行（`CarrierBirdMotion`・`CarrierFlight`）、`Editor/`（シーンを作る `CenoteBaseBuilder`、歩いて確かめる `CenoteBaseWalkTest`）。namespace は `SlugBase` |
| `Scenes/` | シーン（下の表）。`Game/`（島と拠点）、`Tests/`（機能を1つずつ試す）、`Preview/`（モデルやキャラを見る・撮る） |
| `Audio/` | 音。`BGM/`、`Ambience/`（環境音）、`SE/`（効果音：体・ピッケル・銃・ナイフ・当たる・採取・バッグ・試合・画面）、`Placeholder/`（仮の音。Argon が作る wav）、`Imported/`（わくさんの音）、`Mixer/`。決まりは [sound_list.md](tech/sound_list.md) |
| `Fonts/` | Noto Sans JP（日本語の表示用、SIL OFL） |
| `Settings/` | URP の設定と、その設定ツール（`Editor/URPSetup.cs`） |
| `_ToDelete/` | **わくさんが消す**。Nov 2 の片づけで `Scenes/` に要らないシーン4つ（BasePreview・CenoteBaseLook・DinoSlugRough・EquipTest）。どこからも使っていない |

コードの namespace は作った順の名前のまま：`SlugDesign`（キャラ）、`SlugMove`（移動・道具・当たり判定・デバッグ）、`SlugExtract`（採って逃げる）。

## シーン

AssetPreview と CairnLook 以外は Unity メニュー `Slug` から作り直せる（調整用アセットは上書きしない）。`Scenes/Game/`：IslandTest_800・CenoteBase、`Scenes/Tests/`：MoveTest・DinoMoveTest・SwingTest・GunTest・ExtractTest、`Scenes/Preview/`：SlugLookbook・DinoSlugPhotoRough・AssetPreview・IslandPropsLook・CairnLook・CarrierBirdLook・BaseLook。Nov 2 に片づけた4つ（BasePreview・CenoteBaseLook・DinoSlugRough・EquipTest）は `_ToDelete/Scenes/`。EquipTest と DinoSlugRough はメニュー（`Slug/EquipTest Build Scene` など）でもとの場所に作り直せる。

2本足の体で動くのは DinoMoveTest と BaseLook。ほかのシーンはまだ前の体で動く（spec の体は2本足。progress.md の頭）。

| シーン | 試すこと | 作り直すメニュー |
| --- | --- | --- |
| `MoveTest` | 前の体で動く、坂（15°・45°・70°、登れる限界の 40°・50°・60°）、段差、縮んで伸びる | `Slug/MoveTest Build Scene` |
| `DinoMoveTest` | MoveTest の地形を2本足のナメクジで歩く、道具（1 ピッケル、2 背中の銃、3 拳銃） | `Slug/Character/Build Dino Move Test` |
| `SwingTest` | ピッケルの振り（T スロー、G 軌跡） | `Slug/SwingTest Build Scene` |
| `GunTest` | 銃4種（Tab で持ち物を選ぶ。作らないナイフのコードもここで選べる）、的、練習用ナメクジ（当たった部位と倍率） | `Slug/GunTest Build Scene` |
| `ExtractTest` | 出撃前に持ち物を選んで、採って逃げる（ソロ）。島の時計と天気、湿り気（F3 で開発用の数字） | `Slug/ExtractTest Build Scene`、天気だけ入れ直すなら `Slug/Update Weather (open scene)` |
| `IslandTest_800` | 800 m の島の灰色の試作（Oct 9 の高さ）。段と平場のある岩塩の山とたまり池・滝、火口の丘と黒い溶岩原（流れ、キプカ、天窓）、低い砂漠と赤い台地・枯れた谷・岩山、森と大ガジュマル（仮）、草原と菌輪、塩田、川2本。道3本とけものみち5本（つづら折り）、隠れる物（木、下草、草むら、岩、台地の岩の柱など約 1,350 個）。`Map/見る場所` に話し合った地点。`tech/island_test_notes.md` | `Slug/IslandTest 800 m/Build Scene`、`Shoot Views`、`Measure Routes`、`Measure Ways`、`Shoot Ways And Cover`、`Shoot Rivers`、`Shoot Falls`、`Measure Flow`（14か所の間の早い道すじと、道筋の重なる地図） |
| `CenoteBase` | 前の拠点「セノーテの洞窟」を歩く。広間・池・坂・回廊・2階の横穴の部屋・デッキ・海側の口と桟橋。家具は目印の位置に置き、F6 で全部の部屋の段階を S1 → S2 → S3。F7 ですぐ鳥の籠で出発。海に落ちたら出発点へ戻る。`Slug/CenoteBase Walk Test` で主な道を自動で歩いて、挟まる所を Console に出す | `Slug/CenoteBase Build Scene` |
| `SlugLookbook` | 前の体のキャラを雨の背景で見る、撮る | `Slug/Lookbook Build Scene`、`Slug/Lookbook Capture Screenshots` |
| `DinoSlugPhotoRough` | 2本足のナメクジ（Nov 2 に決めた体）を見る | `Slug/Character/Build Dino Photo Rough` |
| `AssetPreview` | 島の植物をナメクジの目の高さで見る | なし（手で置く） |
| `IslandPropsLook` | 島の小物・植物の下書き（小物 31 種は3案ずつ、ケルン風の茂み、森の木・草、珍しい拾い物のボトルシップと塩の魚など） | `Slug/Nature/Props/Build Island Props Look` |
| `CairnLook` | 石積みの道しるべ（ケルン）を見る（Argon の作業） | なし |
| `CarrierBirdLook` | 籠を運ぶ鳥を見る | `Slug/Creatures/Build Carrier Bird` |
| `BaseLook` | 新しい拠点の灰色の箱（構想2版。再生すると2本足で桟橋から歩ける） | `Slug/Base/Build Base Graybox` |

どのシーンも、1 ピッケル、2 背中の銃、3 腰の道具。古い作り方のシーンに持ち物を足すのは `Slug/Update Player Loadout (open scene)`。消した島（IslandTest・_300〜_500）と RegionTest も、メニュー `Slug/IslandTest Build Scene`・`Slug/RegionTest Build Scene` から `Scenes/Tests/` に作り直せる。

## Screenshots（プロジェクト直下）

| フォルダ | 中身 |
| --- | --- |
| `Character/` | キャラ：前の体（全体、顔、動きのコマ）と、2本足の体（`dino_*`：形・歩く・道具）。`history/` に v1・v2 の全体図 |
| `Nature/` | 島の自然の下書き（ヤシ、クワズイモとヘゴ、砂漠の株、ツユリ・ツキシロ・ヒデリベニ、菌輪のキノコ、ケルン風の茂み、森の木など） |
| `Base/` | 拠点：前の拠点（Blender 版・Argon のコード版）と、新しい拠点の灰色の箱 `base_graybox.png` |
| `Creatures/` | 籠を運ぶ鳥 |
| `Weather/` | 天気の時計（朝昼夕夜、曇り・雨）、湿り気の見た目、灰色の区画の見下ろし、空気の霞（`air_*`） |
| `Island/` | 灰色の島の上からの全体、斜め、ナメクジの目の高さからの見え方 |
| `Island800/` | 800 m の島：話し合った地点からのナメクジの目の高さの景色（01〜13）、道の上と隠れる物（20〜26）、川（30〜36）、滝（40〜43）、空から、動線の地図（`flow_before_map.svg`・`flow_after_map.svg`） |
| そのほか | 作ったときの確認用の絵（フォルダ名＝作った物）：`Equip/`・`Gun/`・`Move/`・`Lunge/`・`Bag/`・`Peek/`・`Loadout/`・`Tools/`・`Bot/`・`Harvest/`・`RockSalt/`・`RainTurnip/`・`Props/`・`Clutter/`・`CairnTry/` |
| `_Old/` | **削除予定**。v1 の各案の絵、v2 の案ごとの絵 |

## 履歴（git）

Oct 4, 2026 から git で管理している。GitHub の非公開リポジトリ `Waku68/slug_extraction` に送っている（コミットと送るのは、わくさんが頼んだときだけ）。最初のコミット「整理前の状態（ベースライン）」に戻せば、整理する前のファイルがすべてある。
