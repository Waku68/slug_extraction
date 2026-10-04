using UnityEngine;

namespace SlugWorld
{
    /// <summary>The regions of the island (each one 800 m across, its own server). TestField is the prototype scene.</summary>
    public enum IslandRegion { TestField, RainForest, SaltFlat, SaltMountain, LavaPlateau, FlowerMeadow }

    /// <summary>What the sky is doing over a region (the forecast shows these).</summary>
    public enum Sky { Sunny, Cloudy, LightRain, HeavyRain }

    /// <summary>
    /// The island's clock and weather, in one asset (docs/discussions/island_clock_discussion.md, moisture_discussion.md).
    /// Everything is worked out from the time alone, so every player (and every server, even one that was asleep) sees the
    /// same sky at the same moment, and the forecast is the same for all. Read every frame, so it can be tuned in play mode.
    /// </summary>
    [CreateAssetMenu(menuName = "Slug/Island Settings", fileName = "IslandSettings")]
    public class IslandSettings : ScriptableObject
    {
        [Header("島の時計")]
        [Tooltip("ゲームの1日が現実の何時間か（9時間：毎日8時間ぶん早まり3日で一周、毎週もずれる）")] public float dayHours = 9f;
        [Tooltip("時計の起点（UTC、年-月-日）。全員が同じ時刻の島を見る")] public string epochUtc = "2026-10-01";
        [Tooltip("雨季・乾季それぞれの長さ（現実の日数）。【未決定】仮の値")] public float seasonDays = 14f;
        [Range(0.2f, 0.8f), Tooltip("乾季のいちばん昼が長いときの、1日のうち昼の割合")] public float dryDaylight = 0.62f;
        [Range(0.2f, 0.8f), Tooltip("雨季のいちばん夜が長いときの、1日のうち昼の割合")] public float rainyDaylight = 0.42f;
        [Tooltip("朝焼け・夕焼けの長さ（ゲームの時間, 時間）。短いから感動が大きい")] public float twilightHours = 0.6f;
        [Tooltip("太陽がいちばん高いときの高さ（度）")] public float noonElevation = 70f;

        [Header("月と潮の時計（太陽とは別）")]
        [Tooltip("月の満ち欠けの一周（現実の時間）。太陽の1日とずらして、満月の夜がたまにだけ来るように。【未決定】仮の値")] public float moonCycleHours = 61f;
        [Tooltip("潮の満ち引きの一周（現実の時間）。1日に約2回")] public float tideHours = 4.66f;

        [Header("天気")]
        [Tooltip("天気の種（変えると島の天気がまるごと変わる。大干ばつのたびに変える）")] public int weatherSeed = 1;
        [Tooltip("天気が変わる速さの目安（現実の時間）。小さいほどころころ変わる")] public float weatherScaleHours = 0.6f;
        [Range(0f, 1f), Tooltip("この区画の雨の多さ（0 = ほぼ降らない、1 = よく降る）。TestField, RainForest, SaltFlat, SaltMountain, LavaPlateau, FlowerMeadow の順")]
        public float[] regionWetness = { 0.5f, 0.8f, 0.2f, 0.5f, 0.25f, 0.45f };
        [Range(0f, 0.5f), Tooltip("雨季に雨が増える量")] public float rainySeasonBoost = 0.18f;
        [Range(0f, 1f), Tooltip("この強さより上は大雨")] public float heavyRain = 0.6f;

        [Header("試す用（開発中だけ）")]
        [Tooltip("時計をずらす（ゲームの時間, 時間）。F3 の画面で [ ] キーでも動かせる")] public float devOffsetGameHours = 0f;
        [Tooltip("時計の速さの倍率（1 = 本物）")] public float devTimeScale = 1f;
        [Tooltip("天気を固定する（Auto = 時刻どおり）")] public DevSky devSky = DevSky.Auto;

        public enum DevSky { Auto, Sunny, Cloudy, LightRain, HeavyRain }

        public float Wetness(IslandRegion r)
        {
            int i = (int)r;
            return regionWetness != null && i < regionWetness.Length ? regionWetness[i] : 0.5f;
        }
    }
}
