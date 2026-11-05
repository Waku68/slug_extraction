# 音（効果音・BGM）の置き場所とリスト

Oct 4, 2026

音のファイルは `Assets/Audio/` に置く。

**鳴らす仕組み（Oct 5）**：プレイヤーのナメクジの `SlugSounds` が、下の「つなぎ口」のイベントを聞いて、`Assets/Tuning/SoundBank.asset` に書いた音を鳴らす。音は出来事の名前（キー）ごとに入れる（`pickaxe_hit_salt`、`bag_stow` など。`bag_stow_RockSalt` のように物ごとの版があれば先に使う）。**わくさんの音に替えるときは、SoundBank の該当のキーの音を差し替えるだけ**（コードは触らない）。
**仮の音**：`Assets/Audio/Placeholder/` に、波形の計算で作ったつなぎの音（31種類、43個）。作り方は `SoundGen/placeholder_sfx.py`（`uv run --no-project --with numpy python SoundGen/placeholder_sfx.py`）。メニュー `Slug/Set Up Placeholder Sounds (open scene)` で、SoundBank の空いているキーに仮の音を入れ、開いているシーンのプレイヤーに `SlugSounds` を付ける（手で入れた音は消さない）。わくさんの `SE/Bag/bag_push_salt.wav` は `bag_stow_RockSalt`（岩塩をバッグに入れる）に入れてある。
柱1「ゲットした高揚感」に直結するので、どの段階でも並行して足せる（`progress.md`）。

## まだ作っていない音（Oct 26）

**Nov 4：わくさんの AI の音（`Assets/Audio/Imported/`）をゲームに入れた**。`SoundGen/game_clips.py` がモノラル・前後の無音を切る・大きさをそろえて `Assets/Audio/Game/<鍵>_01.wav` に作り、メニュー `Slug/Set Up Placeholder Sounds (open scene)` が SoundBank の仮の音と前の AI の音をそれに替える（`bag_stow_RockSalt` はわくさんの音のまま）。新しく鳴るようにした鍵：足音 `slug_step_<土・岩・塩・草・水たまり>`（乾くと `slug_step_dry`）、`slug_prone`・`slug_climb`・`slug_breath`（走って息切れ・体力 35% 未満）・`slug_dry_danger`（危ない乾き）・`moss_drink`・`slug_fall`（倒れた）、`gun_low_ammo`（弾倉 1/4 以下）・`gun_last_round`・`gun_fire_far`（ほかの人の銃声が 60 m より遠い）・`hit_confirm`（当てた。触角は `_weak`）・`round_flyby`（耳もと 1.5 m をかすめた）・`round_hit_helmet`・`round_hit_shell`。銃声の届く距離は拳銃 250・ライフルと散弾 300・狙撃 500 m。散弾銃の音が粒の数だけ重なっていたのを1回に。**まだ鳴らない**：`shell_break`（殻の耐久がまだない）、`bag_spit`（塩を吐く仕組みがまだない）、`pick_pluck_Tsukishiro`（ツキシロがまだない）。AI の音がまだない `round_hit_slug`・`gun_fire_sniper`・`moisture_sizzle`・`harvest_pop` は Kenney の仮（`docs/sound/kenney_picks.md`）。

**Nov 3：わくさんが今ある音も全部、Adobe の AI で作り直す**。2本足の体に合わせた全部の音（新しい鍵も）と英語のプロンプトは `docs/sound/adobe_prompts.md`（約75。下の表より新しい）。
Argon が作るときの決まりと進め方は `docs/sound/README.md`。本物の音は4つ：`gun_fire_rifle`（Argon、Oct 25）、`bag_stow_RockSalt`（わくさん）、`bag_down`・`slug_lunge`（わくさんが Adobe の AI で、Oct 30。`Assets/Audio/Imported/`）。
わくさんの音を足すときは `Assets/Raid/Editor/PlaceholderSounds.cs` の `Own` にキーとファイルを書き、メニュー `Slug/Set Up Placeholder Sounds (open scene)`。仮の音しか入っていないキーなら、わくさんの音に入れ替わる（モノラル・Decompress On Load にそろえる）。★は spec 13章の「最初に作る音」。

### A. 鳴っているが、仮の音のまま（キーはある。音を差し替えるだけ）

| キー | 何のとき |
| --- | --- |
| ★ `slug_squeeze` | 押した瞬間に縮む（きゅっ、押している間しまる） |
| ★ `slug_land` | 着地 |
| ★ `bag_rattle` | 跳ねたときの重いバッグの中身（重いほど大きい） |
| ★ `round_hit_slug` | 塩の弾がナメクジに当たる。撃たれても声は出さない（Oct 27 わくさん。`slug_hurt` は鳴らさなくした） |
| `round_hit_world` | 塩の弾が地面・岩・木に当たる |
| ★ `gun_fire_sniper` | 狙撃銃を撃つ |
| ★ `gun_fire_pistol` | 拳銃を撃つ |
| ★ `gun_fire_shotgun` | 散弾銃を撃つ |
| `gun_reload_open` / `eject` / `insert` / `seat` | リロード：開ける・出す・入れる・はめる |
| `pickaxe_swing` | ピッケルを振る |
| ★ `pickaxe_hit_salt` | ピッケルが岩塩に当たる |
| ★ `pickaxe_hit_soft` | ピッケルが植物に当たる |
| `pickaxe_hit_default` | ピッケルが土・岩・その他に当たる |
| `harvest_pop` | 掘れて1個出てくる |
| `harvest_stage` | 岩塩のきのこの形が変わる |
| ★ `pick_pluck` | 手で摘む |
| `crush_squish` | 植物がピッケルで潰れる |
| `knife_stab` | ナイフが刺さる（ナイフは作らない。コードと鍵だけ残す） |
| `tool_switch` | 持ち替え |
| `bag_grab` | つかむ（手に持つ） |
| ★ `bag_stow` | バッグに入れる（岩塩以外。岩塩はわくさんの音） |
| `bag_takeout` | バッグから出す |
| ★ `bag_peek` | バッグを覗く |
| `bag_drop` / `bag_throw` | 落とす・投げる |
| `bag_hide` / `bag_unhide` | 体の隠し場所に入れる・出す（2本足では口から「ごくん」「けぽっ」。鍵の名前はそのまま） |
| `bag_burst` | バッグが破れて中身が散る |
| `bag_eco_open` | エコバッグを広げる |
| `bag_up` | バッグを背負う |

### B. ゲームにはあるのに、音がない（鳴らすつなぎ口もまだない。音ができたら Claude がつなぐ）

| 音 | 何のとき |
| --- | --- |
| ★ 足音（ぺた） | ふだんの移動（2本足で歩く）。足元の地面で変わる（土・岩・塩・草・水たまり）。湿り気でも変わる（湿った「ぺた」→ 乾いた「かさ」）。わくさんが作る |
| ★ 腹ばい | 伏せたときの移動（小さい）。わくさんが作る |
| ごくん・けぽっ・ぺっ | 口から飲みこむ・吐き出す・塩を吐く（2本足。わくさんが作る） |
| よじ登る | 段差・壁をよじ登る |
| 乾きの合図「ジュッ」 | 日なたに出た・塩の地面に乗った瞬間（1回）。**つなぎ口はある**：`moisture_sizzle` |
| 濡れる合図「ぬちゃっ」 | 濡れた所・雨の下に入った瞬間（1回）。**つなぎ口はある**：`moisture_squelch` |
| 乾きの段が変わる | 乾く側・濡れる側。**つなぎ口はある**：`moisture_stage_drier`・`moisture_stage_wetter`（段ごとに `_<段の番号>` を付けた版も使える） |
| 水に入る | 水たまり・沢に入る（ぽちゃ）。苔玉が満タンになる |
| 苔玉で水を飲む | F の 2 秒 |
| 乾ききる | 湿り気がなくなって遅くなる |
| 倒れる | 乾くか撃たれて倒れる |
| 鍋のヘルメットに弾が当たる | カン（体に当たる音と分ける） |
| 銃の空撃ち | 弾がないのに撃つ |
| 脱出 | 脱出口に入る・脱出できた・結果の画面 |
| 釣瓶 | 拠点の釣瓶で上がる（滑車のきしみ、板が上がる・止まる） |
| 鳥の出発 | 籠をつかむ・羽ばたき・鳴き声・海の上の風・雲に入る |
| 池に飛び込む・育つ | 帰って池に入る、隠し場所が広がる演出（spec 12章） |

### C. 環境音と画面の音（効果音とは別。急がない）

雨（小雨・雨・大雨のループ）、風、沢、森、洞窟（拠点のセノーテ）、出撃前の画面の選択・決定。

## フォルダ

| フォルダ | 入れるもの |
| --- | --- |
| `BGM/` | 曲。拠点、試合中（平常・接敵・脱出直前）など、場面ごとの気分（`spec.md` の未決定「場面ごとの気分」） |
| `Ambience/` | 環境音のループ。雨（強さ違い）、風、沢、森、洞窟 |
| `SE/Slug/` | ナメクジの体：足音（ぺた）、縮む・伸びる（びよーん）、着地、よじ登る、伏せる・腹ばい、頭を回す、ごくん・けぽっ・ぺっ、乾いて倒れる（滑る音と撃たれた声はなし）。**乾きの合図**（Oct 10）：日なたに出る・塩に乗った瞬間の「ジュッ」、濡れた所に入った瞬間の「ぬちゃっ」（1回だけ鳴る。`moisture_readability_discussion.md`） |
| `SE/Pickaxe/` | ピッケル：振る（風切り）、岩塩に当たる（硬い・跳ね返り）、作物にめり込む、空振り、取り出す・しまう |
| `SE/Guns/Common/` | 銃に共通：炭酸ガスの「プシュ」、塩の粒が飛ぶ音、背中から取る・しまう |
| `SE/Guns/Rifle/` など | 銃ごと（Rifle・Shotgun・Sniper・Pistol）：撃つ、リロード（下の Cue）、ボルト・ポンプ・スライド、空撃ち |
| `SE/Knife/` | ナイフ：突く、刺さる、空振り、抜く（ナイフは作らないので、入れなくてよい） |
| `SE/Hits/` | 当たる音：塩がナメクジに当たる（部位別・防具の上）、地面・岩・木に当たる、的 |
| `SE/Harvest/` | 採取：岩塩が割れる・欠片が飛ぶ、1 個ずつ出てくる（ころん）、作物を抜く |
| `SE/Bag/` | バッグ：つかむ、入れる（中身の量で音を変える？）、出す、落とす・投げる、体に突っ込む、覗く（ふたが開く）、膨らむ、破れる（中身が散らばる）、エコバッグを広げる |
| `SE/Raid/` | 試合：出撃、脱出口に入る・カウント・脱出、雨が止みそう（警告）、時間切れ |
| `SE/UI/` | 画面：出撃前の画面の選択・決定、メッセージ |
| `Mixer/` | AudioMixer（BGM・環境音・効果音の音量を分ける）。作るときにここへ |

空のフォルダは git に残らないので、各フォルダに `.gitkeep` を置いてある（音を入れたら消してよい）。

## ファイル名

`種類_何_どうなる_番号.wav` を英語で。例：

- `SE/Pickaxe/pickaxe_hit_salt_01.wav`、`pickaxe_hit_salt_02.wav`（同じ音の揺らぎは番号で）
- `SE/Guns/Rifle/rifle_fire_01.wav`、`rifle_reload_insert_01.wav`
- `Ambience/rain_heavy_loop.wav`（ループは `_loop`）
- `BGM/bgm_raid_calm.ogg`

元の素材（長い録音、DAW のプロジェクト）は Assets に入れない。入れると Unity が全部読み込むので、別の場所に置く。

## 取り込みの設定の目安

| 種類 | 形式 | Load Type | 備考 |
| --- | --- | --- | --- |
| 短い効果音 | wav | Decompress On Load | 何度も鳴るもの（足音・撃つ・当たる） |
| 長めの効果音・環境音 | wav / ogg | Compressed In Memory | |
| BGM | ogg | Streaming | |

3D で鳴らす音（他のナメクジの足音・銃声・当たる音）はモノラル、BGM・UI はステレオ。

## 今あるつなぎ口（音を鳴らすときに使えるイベント）

| イベント | どこ | 鳴らす音 |
| --- | --- | --- |
| `GunHandling.Cue`（`open` / `eject` / `insert` / `seat`） | `Tools/Guns/GunHandling.cs` | リロードの各段階（開ける・出す・入れる・はめる） |
| `SlugGunHolder.Fired` / `RoundHit` | `Tools/Guns/SlugGunHolder.cs` | 撃つ・弾が当たる |
| `SlugToolHolder.SwingStarted` / `Struck` | `Tools/Pickaxe/SlugToolHolder.cs` | 振る・当たる |
| `SlugKnifeHolder.Struck` | `Tools/Knife/SlugKnifeHolder.cs` | 突いて当たる |
| `Harvestable.Harvested` | `Raid/Harvestable.cs` | 採れた |
| `SlugHitboxes.Hit` / `AnyHit` | `Combat/SlugHitboxes.cs` | ナメクジに当たる（部位別） |
| `SlugLoadout.Changed` | `Tools/Shared/SlugLoadout.cs` | 持ち替え |
| `SlugGunHolder.Cue` | `Tools/Guns/SlugGunHolder.cs` | リロードの各段階（`GunHandling.Cue` をそのまま外へ） |
| `SlugBag.Did`（grab, pick, takeout, stow, drop, throw, hide, unhide, burst, eco, peek） | `Raid/SlugBag.cs` | つかむ・摘む・出す・入れる・落とす・投げる・体に突っ込む・取り出す・破れる・エコバッグ・覗く |
| `Pickable.Crushed` | `Raid/Pickable.cs` | 植物がピッケルで潰れた |
| `SlugMoveController.Lunged` / `Landed` | `Movement/SlugMoveController.cs` | 縮んで伸びる（`slug_lunge`）、着地（`slug_land`）、跳ねたときのバッグの中身（`bag_rattle`、重いほど大きい） |

移動（歩く・伏せる）、試合（脱出・時間切れ）、天気（雨・風）にはまだイベントがないので、音を入れるときに足す。
