# 島の時計・天気・湿り気の作り方メモ

Oct 5, 2026

仕様は `spec.md`（天気と季節、島と拠点）、決めた経緯は `discussions/island_clock_discussion.md`・`moisture_discussion.md`。

## しくみ

| 部品 | どこ | すること |
| --- | --- | --- |
| `IslandSettings` | `World/`、アセットは `Tuning/IslandSettings.asset` | 1日の長さ（9時間）、時計の起点（UTC）、季節の長さ（仮 14日）、昼の長さ（乾季 0.62・雨季 0.42）、朝夕の長さ、月と潮の周期（仮）、天気の種、区画ごとの雨の多さ |
| `IslandClock` | `World/IslandClock.cs` | 現実の時刻（起点からの時間）だけから、島の時刻・季節・太陽の向き・昼夜・朝夕・月・潮を計算する。状態を持たない |
| `IslandWeather` | 同じファイル | 現実の時刻と区画から、雨の強さ・雲・空（晴れ・曇り・小雨・大雨）を計算する。予報（何時間先まで、何時間ごと）も出せる |
| `WeatherPresenter` | `World/WeatherPresenter.cs` | 場面に1つ。光（太陽・月）、朝夕の色、曇り、空の色、霧、雨のつぶを、時刻と天気で動かす。`WeatherPresenter.Current` からほかが今の時刻と天気を読む |
| `ConditionSettings` | `World/`、アセットは `Tuning/ConditionSettings.asset` | 湿り気と体力の数字（話し合いで決めた値） |
| `MoistureZone` | `World/MoistureZone.cs` | 塩の上（乾く）・水（湿る）の箱。軸にそろった箱で、中にいるかだけ見る |
| `SlugMoisture` | `Raid/SlugMoisture.cs` | プレイヤーの湿り気。日なた・日陰・空の下かを、1秒に5回レイで調べる。苔玉（F）。体の見た目と這う速さに出す |
| `SlugHealth` | `Raid/SlugHealth.cs` | 体力。乾ききると減って倒れる（`Died`）。湿り気が多いとゆっくり戻る |

**時刻だけから計算する理由**：全員が同じ時刻に同じ空を見る（共通の予報）。誰もいない区画のサーバーを止めておいて、入ったときにその時刻の姿で起こせる（`discussions/persistent_island_discussion.md` の 3-1）。

## 見え方

- 空は単色（時刻と天気から色を決める）。Unity の標準の空は、曇りでも地平線がオレンジになるのでやめた
- 夜は月の光（新月 0.32 → 満月 0.5）。夜でも採取できる明るさ
- 体の色は `SlugPop.SetDryness`：乾くと、肌（体・触角・手・足）の色をくすませて暗くする。湿りすぎ（100 超え）は少しつやを足す。装備の色は変えない
- 乾くと体が少し縮む（`SlugMoveController.bodyScale`）。0 で這う速さ ×0.8（`conditionSpeed`。荷物の重さの `speedMultiplier` とは別に掛ける）
- 画面の端のかすみ（湿り気 12 未満）と、苔玉のしずく（左上の自分の下）は IMGUI

## 開発用のキー（ExtractTest）

| キー | すること |
| --- | --- |
| F3 | 開発用の時計・天気・予報と、湿り気・体力の数字を出す |
| [ / ] | 島の時計を1時間ずらす（F3 を出している間） |
| \ | 天気を固定（晴れ → 曇り → 小雨 → 大雨 → 時刻どおり）。その場面だけで、アセットは変えない |
| / | 早回し（240倍） |
| F | 水の苔玉を使う（ゲームのキー） |

## 撮り方

再生中に MCP から：

```csharp
SlugExtract.WeatherFilmStrip.Shoot("day_cycle", new float[] { 6.6f, 12f, 17.6f, 23f }, null);
```

上の段がプレイヤーの目、下の段が外から。`Screenshots/Weather/` に保存する。空を固定するときは `SlugWorld.IslandSettings.DevSky` の配列を渡す。

## 場面に入れる

`Slug/Update Weather (open scene)`：開いている場面に、天気（`Island`）、塩田と水たまりの `MoistureZone`、巨大な木と岩、プレイヤーの湿り気と体力を入れる。何度やり直してもいい（小物は作り直す、設定アセットは最初の1回だけ作る）。`Slug/ExtractTest Build Scene` も最後にこれを呼ぶ。

## 気をつけること

- 再生中にアセット（`IslandSettings`、`ConditionSettings`）を変えると、止めても残る。試すときは場面の中の固定（\ キー、`WeatherPresenter.ShowNow`）を使う
- 影の判定は太陽の向きへのレイ。当たり判定のない物は影を作らない（巨大な木の葉の球にも当たり判定を付けてある）
- 雨の中でも、上に屋根（当たり判定）があると「曇り」の扱い
- 「雨が止むまで」の制限時間は `RaidManager.useTimeLimit` で戻せる
