# 効果音を全部作り直す：Adobe の AI のプロンプト（わくさん用、Nov 3, 2026）

Nov 3 わくさん「二足歩行になったから整合を考え直して。今ある音も全部作り直す」。
2本足の体（`docs/character/dino_slug_impact.md`、spec 5章）に合わせて、**今ある音（本物4つ＋仮32）と、まだない音を1枚に**まとめた。上から順に作る。

## 使い方

- プロンプトは英語のまま貼る。1つのキーで2〜4本作り、気に入った物を残す（Unity が毎回少し高さを揺らすので2本で十分。**何度も鳴る音（足音・着地）は4本**）
- 擬音を声で吹き込める欄があれば、わくさんの擬音を入れると長さとタイミングがそろう
- できたら `Assets/Audio/Imported/` に置いて Claude に言う（名前は日本語でよい）。モノラルにする・Unity に入れる・鳴らすのは Claude
- 見出しの印：**【2本足】**＝体が変わって中身を変えた音、**【新】**＝いまコードに鍵がない音（Claude がつなぐ）、**【鍵あり】**＝鍵はあって入れればすぐ鳴る

## 2本足で変わること（音の決まり）

| 前（這うナメクジ） | いま（2本足） | 音で言うと |
| --- | --- | --- |
| 這う「ぬちゃ」がずっと続く | **1歩ずつの足音**。足の裏だけナメクジ（ぺたっと吸いつく） | 1歩に1つ「ぺた」。土・岩・塩・草・水たまりで変える。湿っていると「ぺた」、乾くと「かさ」 |
| 止まるとき・着地で滑る | **滑らない、ぴたっと止まる** | 滑る「ずざー」は作らない。着地は短く切れる |
| 縮んで伸びる | **しゃがんで跳ぶ**（鍵は `slug_squeeze`・`slug_lunge` のまま） | しゃがむ＝ひざと体が締まる、跳ぶ＝足の裏が地面からはがれる「ぺりっ」から |
| 体に突っ込んで隠す | **口で飲みこむ**（鍵は `bag_hide`・`bag_unhide` のまま） | 「ごくん」「けぽっ」。塩は「ぺっ」と吐く（新） |
| 腹で這う | 伏せは**腹ばい**だけ | 腹ばいの「ずり…」は小さく |
| ― | **長い尾**が遅れてついてくる | 跳んで着くと尾が少し遅れて「ぽすっ」。切り返しの尾の振りは音なしでよい |
| 体は1本の線 | **口がある** | 苔玉を飲む・息も口から鳴らせる |

そのほか（銃・ピッケル・バッグ・弾が当たる）は体と関係ないので中身はそのまま。決まり（雨の夜、火薬でなく炭酸ガスと塩、ポップで短い、耳に痛い高音なし）は `docs/sound/README.md` の2章と同じ。

## いくつ作るか

| | 鍵の数 |
| --- | --- |
| 第1弾（いちばん何度も聞く・柱1） | 10 |
| 第2弾（体と移動の残り） | 13 |
| 第3弾（銃と当たる） | 24 |
| 第4弾（採取とバッグ） | 19 |
| 第5弾（場面・まわり。急がない） | 14 |
| **合わせて** | **約80**（足音は地面ごとに数えた） |

---

## 第1弾（10）

### 1. 足音・土 `slug_step_soil` 【2本足】【新】
いちばん聞く音。1歩ごと、0.1〜0.15 秒。「ぺた」。PvP では足音で人を聞き分けるので、地面ごとに違いが分かるように
```
A single footstep of a small creature with soft, sticky, slimy feet on damp soil, a light wet "peta" pat with a tiny sticky release, close-mic, no reverb, 0.12 seconds
```

### 2. 着地 `slug_land` 【2本足】【鍵あり】
跳んだあと毎回。0.2 秒。両足で「ぺたっ」と着いて、すぐ尾が「ぽすっ」。滑らないので短く切れる
```
Two small sticky slimy feet landing together on damp ground, a soft wet "petta" slap, followed a split second later by a soft thud of a tail touching down, no sliding, close-mic, no reverb, 0.25 seconds
```

### 3. しゃがむ `slug_squeeze` 【2本足】【鍵あり】
押した瞬間から最大 0.4 秒。わくさんの「にゅー＋ｺﾛｯ」に、足の裏が地面をつかむ「ぎゅ」。**離すと途中で切れる**ので「ｺﾛｯ」は頭の 0.1 秒以内
```
A small slimy creature crouching down to jump: one small wooden clack of gear right at the start, then a soft rubbery squish of the body tightening and sticky feet gripping the ground, quiet, close-mic, no reverb, 0.4 seconds
```

### 4. 跳ぶ `slug_lunge` 【2本足】【鍵あり】
いまはわくさんの風切り（Oct 30）。足の裏がはがれる「ぺりっ」→ 風切り「フッ」→ 背中のバッグが揺れる「カラッ」（いちばん大きく）。0.4 秒
```
A small creature springing forward: sticky slimy feet peeling off the ground with a quick "peri", a short breathy whoosh, then a loud light clatter of a backpack and its contents bouncing, close-mic, 0.4 seconds
```

### 5. 弾がナメクジに当たる `round_hit_slug` 【鍵あり】
わくさん「バッ＋スゥー」。撃たれた人にとって「当たった」はこの音だけ。0.3 秒、声なし
```
A wet slap as a small salt pellet punches through thick slime, immediately followed by a thin fading hiss like salt soaking up water, short, no voice, no gunshot, 0.3 seconds
```

### 6. ピッケルで岩塩を叩く `pickaxe_hit_salt` 【鍵あり】
柱1。0.25 秒。硬く明るく割れる。金属の「キーン」なし
```
A small pickaxe striking a chunk of rock salt crystal, a bright crisp crack with tiny crystal fragments scattering, short, no metallic ring, no reverb, 0.25 seconds
```

### 7. 手で摘む `pick_pluck` 【鍵あり】
キノコや花。0.15 秒。「ぷちっ」
```
Plucking a small mushroom out of wet soil, a soft juicy snap with a little earth rustle, close-up, no reverb, 0.15 seconds
```

### 8. 飲みこむ `bag_hide` 【2本足】【鍵あり】
手の物を口へ入れて隠し場所へ。0.4 秒。「ごくん」。かわいく、気持ち悪くしない
```
A cute cartoon creature swallowing a small item in one gulp, a soft round "gokun", a little wet but clean, not gross, close-mic, no reverb, 0.4 seconds
```

### 9. 乾きの合図「ジュッ」 `moisture_sizzle` 【鍵あり】
日なたに出た・塩に乗った瞬間に1回。0.3 秒。耳に痛くしない
```
A short, soft sizzle like a drop of water touching a warm stone, gentle and rounded, not harsh, no high-pitched hiss, 0.3 seconds
```

### 10. 空撃ち `gun_reload_dry` 【鍵あり】
弾も予備もないのに引き金。0.15 秒。炭酸ガスの銃。いまは無音
```
Dry fire of a small CO2 air gun with an empty canister, a weak hollow click and a tiny puff of air, no gunpowder, no gunshot, 0.15 seconds
```

---

## 第2弾：体と移動の残り（13）

### 足音・岩 `slug_step_rock` 【2本足】【新】
```
A single footstep of a small creature with soft sticky slimy feet on hard wet stone, a crisp little wet "pech" pat, close-mic, no reverb, 0.1 seconds
```
### 足音・塩 `slug_step_salt` 【2本足】【新】
塩の上は乾く所。ざらっと、少し痛そうに
```
A single footstep of a small creature with soft slimy feet on coarse dry salt crust, a light gritty crunch with a faint sticky sound, close-mic, no reverb, 0.12 seconds
```
### 足音・草 `slug_step_grass` 【2本足】【新】
```
A single soft footstep of a small creature with sticky feet in short wet grass, a gentle damp swish, close-mic, no reverb, 0.15 seconds
```
### 足音・水たまり `slug_step_puddle` 【2本足】【新】
```
A single small footstep in a shallow puddle, a light cute "picha" splash, close-mic, no reverb, 0.15 seconds
```
### 足音・乾いた体 `slug_step_dry` 【2本足】【新】
湿り気が減ると、どの地面でもこれが混ざる（spec 4章：「ぺた」→「かさ」。相手にも聞こえる）
```
A single footstep of a small creature whose feet have dried out, a papery dry "kasa" rustle, light, close-mic, no reverb, 0.12 seconds
```
### 腹ばいで進む `slug_prone` 【2本足】【新】
伏せたまま動く間、0.5 秒ほどを繰り返し。小さく
```
A small creature crawling on its belly over damp ground, a quiet soft slimy drag, gentle and low, close-mic, no reverb, 0.5 seconds
```
### よじ登る `slug_climb` 【2本足】【新】
段差（頭の高さまで）を手と足で。0.5 秒
```
A small slimy creature pulling itself up a ledge with its hands and feet, two or three soft sticky grabs and a quick scramble, no voice, close-mic, no reverb, 0.5 seconds
```
### 吐き出す `bag_unhide` 【2本足】【鍵あり】
最後に飲みこんだ物を手の上へ。「けぽっ」。かわいく
```
A cute cartoon creature coughing a small item back up into its hand, a soft little "kepo" hiccup pop, not gross, close-mic, no reverb, 0.3 seconds
```
### 塩を吐く `bag_spit` 【2本足】【新】
塩を飲もうとすると「ぺっ」と吐く（顔をしかめる）
```
A cute cartoon creature spitting out something salty, a quick "pe" spit with a tiny disgusted "blegh", short, close-mic, no reverb, 0.3 seconds
```
### 濡れる合図「ぬちゃっ」 `moisture_squelch` 【鍵あり】
濡れた所・雨の下に入った瞬間に1回。気持ちいい音に
```
A short, pleasant wet squelch like stepping onto soaked moss, soft and round, satisfying, close-mic, no reverb, 0.25 seconds
```
### 乾きの段が変わる `moisture_stage_drier` / `moisture_stage_wetter` 【鍵あり】
4段を越えた瞬間に1回。乾く側は「きゅっ」と縮む、濡れる側は「ふわっ」と伸びる。2つ作る
```
A small soft creaking shrink, like damp rubber tightening as it dries, quiet, short, no reverb, 0.3 seconds
```
```
A soft gentle swelling sound, like a wet sponge plumping up with water, round and pleasant, short, no reverb, 0.3 seconds
```
### 危ない乾きの間 `slug_dry_danger` 【新】
12 未満の段にいる間、数秒おき。ゼルダのハートが少ないときの代わり。乾いた「ぴしっ」
```
A faint dry crackle like thin dried skin cracking, quiet and short, no reverb, 0.2 seconds
```

---

## 第3弾：銃と当たる（24）

### 銃声は誇張する（Nov 4 わくさん）
本物のガスの銃は小さい「パスッ」だが、PvP では銃声が遠くまで届いて方向が分かることがいちばん大事なので、**空気で撃つ大砲くらいに誇張**する。どの銃も、出だしの低く太い「ボフン」（ガスが一気に抜ける）が芯。火薬の「パン」の鋭さと金属の響きはなし、そのかわり低い音と空気の押しは大きく。
- 避ける言葉：air rifle、airsoft、pellet gun、BB gun（小さい「パスッ」になる）
- 使う言葉：pneumatic cannon、compressed air blast、huge low thump、air pressure punch
- 大きさの順：拳銃 ＜ ライフル ＜ 散弾銃 ＜ 狙撃銃。どれも近くで聞くと少しびっくりするくらい

### ライフル `gun_fire_rifle`
いまは Argon の音（Oct 25 ✅）。作り直す：太い「ボフッ」＋塩の粒の「さらさら」0.5 秒。1秒に5発続くので、出だしは短く切る
```
An exaggerated handheld pneumatic air cannon firing, a punchy, heavy low "boff" blast of compressed gas that pushes the air, big low end, followed by a fine trickle of salt grains scattering, no gunpowder, no metallic ring, 0.5 seconds
```
### 拳銃 `gun_fire_pistol` 【鍵あり】
ライフルより小さく高いが、軽すぎない「ボフ」
```
An exaggerated small pneumatic air cannon pistol shot, a tight punchy "bof" burst of compressed gas with a solid low thump, a brief sprinkle of salt grains, no gunpowder, no metallic click, 0.3 seconds
```
### 散弾銃 `gun_fire_shotgun` 【鍵あり】
わくさん「ｻﾞパン×3」：先に細かい砂の塩「ｻﾞ」、すぐ本弾「パン」、それが3つ少しずれて重なる。全体を太い空気の「ドフン」が包む
```
An exaggerated pneumatic air cannon shotgun blast of coarse salt: a huge low "dofun" air pressure punch with three overlapping "za-pan" bursts inside it within a tenth of a second, each a sandy hiss then a heavy pop, no gunpowder, 0.6 seconds
```
### 狙撃銃 `gun_fire_sniper` 【鍵あり】
わくさん「ｽﾞバン」を、もう弱めでなく大砲に。低い溜めの「ｽﾞ」から重い「バン」、空気が震える余韻と、長い塩の「さらさら」
```
An exaggerated heavy pneumatic air cannon firing, a low rumbling "zu" build into a huge deep "BAN" blast of compressed air like a small cannon, the air trembling after it, then a long trickling tail of salt grains, no gunpowder, no metallic ring, 1.0 seconds
```

### 遠くの銃声4つ `gun_fire_<銃>_far` 【新】
ほかの人の銃声が遠い（60 m くらいから）とき、近くの音とだんだん入れ替える（Claude がつなぐ）。高い「さらさら」は消えて、低い「ドン」と、谷や森で返る「…ぉぉん」だけ。4つとも同じ作りで、大きさと長さだけ変える
```
A distant pneumatic air cannon shot heard from far away across a forested valley, a dull low "don" thump with no high frequencies, followed by a soft rolling echo, 1.2 seconds
```
- 拳銃 `gun_fire_pistol_far`：上の文の `a dull low "don" thump` を `a small dull "pon" thump` に、長さ 0.8 seconds
- ライフル `gun_fire_rifle_far`：上のまま
- 散弾銃 `gun_fire_shotgun_far`：`a dull low "don" thump` を `a heavy doubled "do-don" thump` に、1.4 seconds
- 狙撃銃 `gun_fire_sniper_far`：`a dull low "don" thump` を `a deep booming "doon" like a far cannon` に、2.0 seconds
### リロード4つ `gun_reload_open` / `eject` / `insert` / `seat` 【鍵あり】
この順に続けて鳴って1つのリロードに聞こえるように。流木・殻・つるの道具。金属の「ガチャ」は控えめ。4つ作る
```
A small wooden latch snapping open on a handmade air gun, a dry hollow click, 0.12 seconds
```
```
An empty shell-like magazine sliding out and dropping, a light hollow wooden knock, 0.15 seconds
```
```
A magazine packed with salt pellets pushed into a wooden gun, a gritty sliding rattle, 0.2 seconds
```
```
A magazine seating firmly into a wooden air gun, a solid satisfying wooden "kon" clunk, 0.12 seconds
```
### 残り少ない弾倉 `gun_low_ammo` 【新】
弾倉が 1/4 を切ったら撃つ音に小さく重ねる。発射音は入れない
```
A hollow rattling click of a nearly empty magazine with a few loose salt grains shaking inside, light wooden rattle, quiet, no gunshot, 0.2 seconds
```
### 最後の1発 `gun_last_round` 【新】
撃ち切った瞬間。ガスが抜けきる
```
The last puff of gas leaving an empty CO2 canister, a short "pshhh" fading out with a tiny hollow knock, no gunshot, 0.3 seconds
```
### 覗く `gun_aim` 【新】
右ボタンで構えた瞬間。小さく
```
A small creature raising a wooden gun to its eye, a soft cloth-and-strap rustle with a tiny wooden tap, quiet, close-mic, 0.15 seconds
```
### 当てた手ごたえ `hit_confirm` / 触角に当てた `hit_confirm_weak` 【新】
撃った人の耳にだけ。2つ作る（ふつう「ぷちっ」、触角は少し高く短い「ピッ」）
```
A tiny soft pop like a single bubble of slime bursting, cute and satisfying, very short, clean, no reverb, 0.08 seconds
```
```
A tiny bright "pip", a slightly higher and shorter version of a soft slime bubble pop, cute, clean, no reverb, 0.06 seconds
```
### 弾がかすめる `round_flyby` 【新】
外れた弾が耳もとを抜ける。1発目で「狙われている」と気づける
```
A small salt pellet whizzing past the ear, a quick airy "shoo" swoosh passing left to right, no crack, no gunshot, 0.2 seconds
```
### 弾が地面・岩・木に当たる `round_hit_world` 【鍵あり】
```
A small salt pellet hitting damp ground and stone, a short dull tick with a puff of salt dust, no ricochet whine, 0.15 seconds
```
### 鍋のヘルメットに当たる `round_hit_helmet` 【新】
体に当たる音と分ける（spec 8章「カン」）
```
A small salt pellet bouncing off an old iron cooking pot, a short dull "kan" clang, not ringing, no reverb, 0.2 seconds
```
### 殻アーマーに当たる `round_hit_shell` 【新】
```
A small salt pellet hitting a thick snail shell plate, a dry hard "katsu" tick, short, no reverb, 0.15 seconds
```
### 殻が割れる `shell_break` 【新】
```
A thick snail shell plate cracking apart, a sharp "pakin" snap with a few small pieces falling, short, no reverb, 0.4 seconds
```
### 倒れる `slug_fall` 【2本足】【新】
2本足の体がどさっと倒れて、体が消える
```
A small cartoon creature toppling over onto damp ground with a soft "dosa" thud, followed by a gentle fizzing dissolve, not gory, 0.8 seconds
```

---

## 第4弾：採取とバッグ（19）

### ピッケルを振る `pickaxe_swing` 【鍵あり】
```
A short light whoosh of a small wooden pickaxe being swung, close-mic, no reverb, 0.2 seconds
```
### ピッケルが植物に当たる `pickaxe_hit_soft` 【鍵あり】
```
A small pickaxe thudding into a juicy plant stem, a soft wet "zuku" chop, close-mic, no reverb, 0.2 seconds
```
### ピッケルが土・岩に当たる `pickaxe_hit_default` 【鍵あり】
```
A small pickaxe striking packed damp earth and stone, a dull short "dosu" knock with a little grit, no metallic ring, 0.2 seconds
```
### 掘れて1個出る `harvest_pop` 【鍵あり】
柱1。気持ちよく「ころん」
```
A chunk of rock salt popping free and tumbling onto the ground, a bright satisfying "koron" with a little bounce, cute, no reverb, 0.3 seconds
```
### 岩塩のきのこの形が変わる `harvest_stage` 【鍵あり】
```
A cluster of salt crystals shifting and settling into a new shape, a soft sparkly crumble, short, no reverb, 0.4 seconds
```
### 植物が潰れる `crush_squish` 【鍵あり】
```
A soft juicy plant being crushed, a wet squishy "guchu", short, close-mic, no reverb, 0.2 seconds
```
### 持ち替え `tool_switch` 【鍵あり】
伸びる腕が背中や腰から道具を取る
```
A stretchy rubbery arm reaching back and grabbing a wooden tool from a strap, a soft elastic "nyu" and a light wooden tap, close-mic, 0.25 seconds
```
### つかむ `bag_grab` 【鍵あり】
```
A small hand grabbing an item off damp ground, a quick soft grab with a tiny rustle, close-mic, no reverb, 0.12 seconds
```
### バッグに入れる `bag_stow` 【鍵あり】
岩塩はわくさんの音があるので、それ以外の物（キノコ・花・カブ）。作り直すなら岩塩も
```
Putting a soft item into a cloth backpack over the shoulder, a soft fabric rustle and a gentle muffled drop, close-mic, no reverb, 0.3 seconds
```
### バッグから出す `bag_takeout` 【鍵あり】
```
Pulling an item out of a cloth backpack, a short fabric rustle and a soft lift, close-mic, no reverb, 0.25 seconds
```
### 覗く `bag_peek` 【鍵あり】
```
Opening the flap of a cloth backpack to look inside, a soft fabric flip and a faint rattle of things inside, close-mic, 0.3 seconds
```
### 落とす・投げる `bag_drop` / `bag_throw` 【鍵あり】
2つ作る
```
A small cloth bundle dropped onto damp ground, a soft muffled "posu" thud, close-mic, no reverb, 0.2 seconds
```
```
A small cloth bundle tossed through the air and landing on damp ground, a quick whoosh then a soft thud, 0.4 seconds
```
### バッグをおろす・背負う `bag_down` / `bag_up` 【鍵あり】
おろすはわくさんの音がある（Oct 30）。作り直すなら2つ
```
A small backpack being set down on the ground, a soft fabric slump with contents settling, close-mic, no reverb, 0.4 seconds
```
```
A small backpack being swung up onto the back, a fabric swish and straps tightening, close-mic, no reverb, 0.4 seconds
```
### 跳ねたときの重いバッグ `bag_rattle` 【鍵あり】
わくさん「ゴォ＋ｻﾞｻﾞｯ」。重いときだけ跳ぶ音と同時に。低くこもらせて、跳ぶ音の「カラッ」とかぶらないように
```
A heavy stuffed cloth backpack bouncing, a low muffled compressed "goh" thump of packed contents and a rough fabric rub, low and dull, 0.35 seconds
```
### バッグが破れる `bag_burst` 【鍵あり】
```
A cloth bag tearing open and its contents spilling onto the ground, a fabric rip then a scatter of small salt chunks and vegetables, 0.7 seconds
```
### エコバッグを広げる `bag_eco_open` 【鍵あり】
```
Unfolding a thin crinkly reusable shopping bag, a quick light rustle and snap open, close-mic, 0.4 seconds
```
### 珍しい物を手にした `pick_pluck_Tsukishiro` など 【新】
あつ森の珍しい魚のジングルの代わり。短い3音（音楽は Adobe が苦手なら手で作る）
```
A short cute three-note sparkle chime on a wooden marimba, bright and happy, item get jingle, 0.6 seconds
```

---

## 第5弾：場面・まわり（14。急がない）

| 鍵 | 何のとき | プロンプト |
| --- | --- | --- |
| `moss_drink` 【新】 | 苔玉を口で飲む（2秒） | `A small cute creature sipping water from a wet moss ball, soft "chuu" and two gentle gulps, close-mic, 2 seconds` |
| `water_enter` 【新】 | 水たまり・沢に入る | `A small creature stepping into a shallow stream, a light "jabu" splash, close-mic, 0.4 seconds` |
| `slug_breath` 【新】 | 体力が減るほど速く大きく（口から） | `Quick soft panting breaths of a small tired cartoon creature, gentle, not human, loopable, 2 seconds` |
| `heal_full` 【新】 | 体力が戻りきった（塩が流れきった） | `Salt grains washing away followed by a soft slick "tsurun" shine, gentle and relieving, 0.6 seconds` |
| `bag_heavy` 【新】 | 重さの線を越えた（数字なし） | `A backpack suddenly feeling heavy, a fabric straining "gyu" and a dull "zushi" settle, 0.4 seconds` |
| `bag_full` 【新】 | いっぱいで入らない | `A stuffed cloth bag refusing one more item, a tight fabric squeeze and a small item rolling off, 0.4 seconds` |
| `pick_up_ammo` / `pick_up_moss` 【新】 | 抜け殻の横の弾・苔玉を拾う | `A small handful of salt pellets scooped up, a light "jara" rattle, 0.2 seconds` ／ `A soft wet moss ball picked up, a gentle "pofu", 0.15 seconds` |
| `snail_step` 【新】 | カタツムリ（2本足、背中に殻）の足音 | `A heavier sticky footstep of a small creature carrying a shell, a wet pat with a faint hollow shell knock, 0.15 seconds` |
| `critter_flee` 【新】 | 近くを通ると生き物が騒ぐ（Hunt のカラス） | `A frog plopping into water and small birds fluttering away in alarm, 0.8 seconds` |
| `rain_clearing` 【新】 | 雨が上がる（乾く時間の予告） | `Rain thinning out to a few last drips, then a single distant bird starting to sing, 3 seconds` |
| `extract_done` 【新】 | 脱出できた | `A short warm wooden chime with a soft breath of relief, cozy, 1.2 seconds` |
| `ui_select` / `ui_confirm` 【新】 | 準備画面の選択・決定（あつ森のボタン音の方向） | `A tiny soft wooden "pok" button tap, cute, 0.08 seconds` ／ `A bright friendly two-note wooden "po-pin" confirm, cute, 0.25 seconds` |

鳥の出発・釣瓶・池は、拠点と鳥の形が決まってから。

---

## 作らない物

- 撃たれた声 `slug_hurt`（Oct 27 わくさん。鳴らさない）
- ナイフ `knife_stab`（ナイフは作らない、Oct 26）
- 這う「ぬちゃ」の続く音（2本足で足音に替わった）、滑る音（滑るのはなし）
