# 音（効果音・BGM）の置き場所とリスト

Oct 4, 2026

音のファイルは `Assets/Audio/` に置く。

**鳴らす仕組み（Oct 5）**：プレイヤーのナメクジの `SlugSounds` が、下の「つなぎ口」のイベントを聞いて、`Assets/Tuning/SoundBank.asset` に書いた音を鳴らす。音は出来事の名前（キー）ごとに入れる（`pickaxe_hit_salt`、`bag_stow` など。`bag_stow_RockSalt` のように物ごとの版があれば先に使う）。**わくさんの音に替えるときは、SoundBank の該当のキーの音を差し替えるだけ**（コードは触らない）。
**仮の音**：`Assets/Audio/Placeholder/` に、波形の計算で作ったつなぎの音（31種類、43個）。作り方は `SoundGen/placeholder_sfx.py`（`uv run --no-project --with numpy python SoundGen/placeholder_sfx.py`）。メニュー `Slug/Set Up Placeholder Sounds (open scene)` で、SoundBank の空いているキーに仮の音を入れ、開いているシーンのプレイヤーに `SlugSounds` を付ける（手で入れた音は消さない）。わくさんの `SE/Bag/bag_push_salt.wav` は `bag_stow_RockSalt`（岩塩をバッグに入れる）に入れてある。
柱1「ゲットした高揚感」に直結するので、どの段階でも並行して足せる（`progress.md`）。

## フォルダ

| フォルダ | 入れるもの |
| --- | --- |
| `BGM/` | 曲。拠点、試合中（平常・接敵・脱出直前）など、場面ごとの気分（`spec.md` の未決定「場面ごとの気分」） |
| `Ambience/` | 環境音のループ。雨（強さ違い）、風、沢、森、洞窟 |
| `SE/Slug/` | ナメクジの体：這う（ぬちゃ）、止まって滑る、縮む・伸びる（びよーん）、着地、よじ登る、伏せる、頭を回す、被弾の声、乾いて倒れる |
| `SE/Pickaxe/` | ピッケル：振る（風切り）、岩塩に当たる（硬い・跳ね返り）、作物にめり込む、空振り、取り出す・しまう |
| `SE/Guns/Common/` | 銃に共通：炭酸ガスの「プシュ」、塩の粒が飛ぶ音、背中から取る・しまう |
| `SE/Guns/Rifle/` など | 銃ごと（Rifle・Shotgun・Sniper・Pistol）：撃つ、リロード（下の Cue）、ボルト・ポンプ・スライド、空撃ち |
| `SE/Knife/` | ナイフ：突く、刺さる、空振り、抜く |
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

移動（這う・縮む・伸びる・伏せる）、試合（脱出・時間切れ）、天気（雨・風）にはまだイベントがないので、音を入れるときに足す。
