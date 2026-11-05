# Kenney の音から抜き出す物（わくさん用、Nov 4, 2026）

わくさん「Adobe は擬音と、ポップで現実にない音が苦手。ポップめで大事そうなのを洗い出して、kenney.nl のを使いたい」。
`adobe_prompts.md` の約80のうち、**ポップで現実にない音（合図・手ごたえ・画面）**と、**Kenney に合う物がある道具の音**を選んだ。現実の物の音（足音・銃声・弾が当たる・割れる）は Adobe のまま。

## Nov 4：候補を取ってきた（`Assets/Audio/Kenney/`）

Claude が Kenney から6つのパック（下の5つ＋ Sci-fi Sounds）を落として、**鍵ごとのフォルダに候補をコピー**した（156 個、ogg のまま。`License.txt` は CC0）。Unity でフォルダを開いて聞き、**残す物を選んで教えてほしい**（使わない物は消してよい）。選んだ物を Claude が鍵につなぐ。Claude には音が聞こえないので、候補は長さと音の高さで選んだ。

わくさんが直したかった4つ：

| フォルダ | 中身 |
| --- | --- |
| `round_hit_slug/` | Sci-fi の `slime_`（ぬるっと潰れる）、Impact の `impactPunch_medium_`（バッ）。**重ねた試し2つ**：`mix_A_slime_hiss`（slime ＋ scratch の「スゥー」）、`mix_B_punch_slime_hiss`（punch ＋ slime ＋ scratch） |
| `moisture_sizzle/` | Interface の `scratch_` 5つ（短い「ジュッ」に近いのは 001〜003、長めは 004・005） |
| `gun_fire_sniper/` | Kenney に銃はないので、Sci-fi の低い爆発 `lowFrequency_explosion_` と `explosionCrunch_`。**重ねた試し2つ**：`mix_A_low_crunch`（低い「ドン」＋ざらっと）、`mix_B_punch_low_salt`（「ｽﾞ」＋低い「バン」＋塩の「さらさら」の代わりの scratch） |
| `pick_pluck_Tsukishiro/` | Music Jingles の短い物：ピチカート `PIZZI00・16・04・05・08`（0.5 秒前後）、スチールドラム `STEEL09・08` |

ほかの鍵のフォルダ：`hit_confirm`、`hit_confirm_weak`、`harvest_pop`、`moisture_stage_drier`、`moisture_stage_wetter`、`gun_low_ammo`、`gun_reload_dry`、`heal_full`、`bag_full`、`bag_heavy`、`ui_select`、`ui_confirm`、`extract_done`、`bag_hand`（つかむ・入れる・出す・覗く）、`bag_drop`、`gun_reload_open_seat`、`tool_switch`、`round_hit_world`、`pickaxe_hit_default`。中身は下の表の「探すパック・ファイル」のとおり。

△ の体の音（ごくん・けぽっ・ぺっ・ぬちゃっ・にゅー）は、Kenney にも合う物がなかった。

## Kenney の音のパック（kenney.nl/assets の Audio。どれも CC0：自由に使える、名前を出さなくてよい）

| パック | 中身 | 使い道 |
| --- | --- | --- |
| **Interface Sounds** | クリック、ポン、ピッ、開く・閉じる、決定、エラー、はじく（pluck）、小さく・大きく（minimize・maximize） | 合図と手ごたえのほとんど |
| **UI Audio** | 柔らかいクリック、切り替え | 画面のボタン |
| **Music Jingles** | 短いジングル（木琴・ピチカート・スチールドラムなど） | 珍しい物、脱出できた |
| **RPG Audio** | 布の音、革を落とす、留め具、金具のカチッ、鍋 | バッグ、リロード |
| **Impact Sounds** | 当たる音（柔らかい・木・ガラス・金属、軽い〜重い）、足音 | 岩塩のころん、弾が地面に当たる |
| Digital Audio | ピコピコの上がる・下がる | 使わないかも（電子の音は世界に合わない） |

ファイル名はパックの中で `pluck_001.ogg` のように番号つき。気に入った物を `Assets/Audio/Imported/` に入れて、どの鍵に使うか教えてくれれば Claude がつなぐ（ogg のままでよい）。

---

## ◎ ポップで大事（Kenney でいちばん作りたい物）

| 鍵 | 何のとき | 欲しい感じ | 探すパック・ファイル |
| --- | --- | --- | --- |
| `hit_confirm` | 自分の弾が当たった（撃った人の耳だけ） | 小さく気持ちいい「ぷちっ」 | Interface Sounds の `pluck_`、`drop_`、`tick_` |
| `hit_confirm_weak` | 触角に当てた | 上より高く短い「ピッ」 | Interface Sounds の `pluck_`（高い方）、`tick_` |
| `pick_pluck_<珍しい物>` | ツキシロなど珍しい物を取った | 3音の「ぴろりん」 | Music Jingles のいちばん短い物（ピチカート・スチール） |
| `harvest_pop` | 掘れて1個出る（柱1） | 明るい「ころん」 | Impact Sounds の `impactGlass_light`、Interface Sounds の `drop_` |
| `moisture_stage_drier` | 乾く側の段を越えた | 「きゅっ」と縮む | Interface Sounds の `minimize_` |
| `moisture_stage_wetter` | 濡れる側の段を越えた | 「ふわっ」と伸びる | Interface Sounds の `maximize_` |
| `gun_low_ammo` | 弾倉が 1/4 を切った（撃つ音に重ねる） | 空っぽの「カラ」 | Interface Sounds の `tick_`、RPG Audio の `metalClick` |
| `gun_reload_dry` | 弾がないのに引き金 | 力のない「カチッ」 | Interface Sounds の `click_`・`switch_` |
| `heal_full` | 体力が戻りきった | ほっとする「つるん」 | Interface Sounds の `confirmation_` |
| `bag_full` | いっぱいで入らない | 「むぎゅ」とはじかれる | Interface Sounds の `error_`（いちばん柔らかい物） |
| `bag_heavy` | 重さの線を越えた | 「ずしっ」 | Impact Sounds の `impactSoft_heavy` |
| `ui_select` / `ui_confirm` | 準備画面の選択・決定 | あつ森のボタンの「ぽこっ」「ぽぴん」 | Interface Sounds の `select_`・`confirmation_`、UI Audio の `click` |
| `extract_done` | 脱出できた | あたたかい短い音楽 | Music Jingles |

## ○ 道具の音（Kenney に合う物がある。Adobe でもよい）

| 鍵 | 何のとき | 探すパック・ファイル |
| --- | --- | --- |
| `bag_grab` / `bag_stow` / `bag_takeout` / `bag_peek` | つかむ・入れる・出す・覗く | RPG Audio の `cloth`、`handleSmallLeather` |
| `bag_drop` / `bag_down` | 落とす・おろす | RPG Audio の `dropLeather` |
| `gun_reload_open` / `seat` | 開ける・はめる | RPG Audio の `metalLatch`、Impact Sounds の `impactWood` |
| `tool_switch` | 持ち替え | RPG Audio の `cloth` ＋ Interface Sounds の `switch_` |
| `round_hit_world` | 弾が地面・岩に当たる | Impact Sounds の `impactSoft_medium`、`impactGeneric_light` |
| `pickaxe_hit_default` | ピッケルが土・岩に | Impact Sounds の `impactMining`、`impactWood` |

## △ ポップで大事だけど、Kenney にも合う物がなさそう

体の音は、Kenney にもない。わくさんの口で録る（擬音そのまま）のがいちばん早いかもしれない（2本足の体の音はわくさんが作る、Nov 2）。

| 鍵 | 何のとき | 欲しい感じ |
| --- | --- | --- |
| `bag_hide` | 飲みこむ | 「ごくん」 |
| `bag_unhide` | 吐き出す | 「けぽっ」 |
| `bag_spit` | 塩を吐く | 「ぺっ」 |
| `moisture_sizzle` | 日なた・塩に乗った | 「ジュッ」（Interface Sounds の `scratch_` が近いかも） |
| `moisture_squelch` | 濡れた所に入った | 「ぬちゃっ」 |
| `slug_squeeze` | しゃがむ | 「にゅー＋ｺﾛｯ」 |
| `slug_dry_danger` | 危ない乾きの間 | 乾いた「ぴしっ」（Interface Sounds の `glass_` の軽い物が近いかも） |

## Adobe のままの物

足音、着地、跳ぶ、銃声（近く・遠く）、弾がナメクジ・ヘルメット・殻に当たる、殻が割れる、倒れる、ピッケルで岩塩・植物、植物が潰れる、バッグが破れる、水に入る、雨が上がる、生き物が騒ぐ。
