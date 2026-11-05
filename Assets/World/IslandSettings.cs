using UnityEngine;

namespace SlugWorld
{
    /// <summary>The regions of the island (each one 800 m across, its own server). TestField is the prototype scene.</summary>
    public enum IslandRegion { TestField, RainForest, SaltFlat, SaltMountain, LavaPlateau, FlowerMeadow }

    /// <summary>What the sky is doing over a region (the forecast shows these).</summary>
    public enum Sky { Sunny, Cloudy, LightRain, HeavyRain }

    /// <summary>The four seasons of one season of play (spec 4章 Oct 25), in order. The great drought ends the last.</summary>
    public enum Season { BigRainy, Dry, Rainy, BigDry }

    /// <summary>
    /// The island's clock and weather, in one asset (docs/discussions/island_clock_discussion.md, moisture_discussion.md).
    /// Everything is worked out from the time alone, so every player (and every server, even one that was asleep) sees the
    /// same sky at the same moment, and the forecast is the same for all. Read every frame, so it can be tuned in play mode.
    /// </summary>
    [CreateAssetMenu(menuName = "Slug/Island Settings", fileName = "IslandSettings")]
    public class IslandSettings : ScriptableObject
    {
        [Header("島の時計")]
        [Tooltip("ゲームの1日が現実の何時間か（9.5時間：毎日同じ時刻に入ると島の時刻が約半日ずれて、19日で全部の時間帯を回る）")] public float dayHours = 9.5f;
        [Tooltip("時計の起点（UTC、年-月-日）。全員が同じ時刻の島を見る")] public string epochUtc = "2026-10-01";
        [Tooltip("1シーズンの4つの季節の長さ（現実の週）。大雨季・乾季・雨季・大乾季の順。【仮】Oct 25 わくさん：3・2・2・3 の10週。終わりに大干ばつ")]
        public float[] seasonWeeks = { 3f, 2f, 2f, 3f };
        [Tooltip("それぞれの季節のいちばん乾いたとき（0 = いちばん雨、1 = いちばん乾く）。大雨季・乾季・雨季・大乾季の順。昼の長さと雨の多さがこれで動く")]
        public float[] seasonDryness = { 0f, 0.7f, 0.3f, 1f };
        [Range(0.2f, 0.8f), Tooltip("乾季のいちばん昼が長いときの、1日のうち昼の割合")] public float dryDaylight = 0.62f;
        [Range(0.2f, 0.8f), Tooltip("雨季のいちばん夜が長いときの、1日のうち昼の割合")] public float rainyDaylight = 0.42f;
        [Tooltip("朝焼け・夕焼けの長さ（ゲームの時間, 時間）。短いから感動が大きい")] public float twilightHours = 0.6f;
        [Tooltip("太陽がいちばん高いときの高さ（度）")] public float noonElevation = 70f;

        [Header("月と潮の時計（太陽とは別）")]
        [Tooltip("月の満ち欠けの一周（現実の時間）。72時間＝3日。島の1日で割り切れないので、満月が島の何時に来るかは毎回ずれる")] public float moonCycleHours = 72f;
        [Tooltip("潮の満ち引きの一周（現実の時間）。1日に約2回")] public float tideHours = 4.66f;

        [Header("天気")]
        [Tooltip("天気の種（変えると島の天気がまるごと変わる。大干ばつのたびに変える）")] public int weatherSeed = 1;
        [Tooltip("天気が変わる速さの目安（現実の時間）。小さいほどころころ変わる")] public float weatherScaleHours = 0.6f;
        [Range(0f, 1f), Tooltip("この区画の雨の多さ（0 = ほぼ降らない、1 = よく降る）。TestField, RainForest, SaltFlat, SaltMountain, LavaPlateau, FlowerMeadow の順")]
        public float[] regionWetness = { 0.5f, 0.8f, 0.2f, 0.5f, 0.25f, 0.45f };
        [Range(0f, 0.5f), Tooltip("雨季に雨が増える量")] public float rainySeasonBoost = 0.18f;
        [Range(0f, 1f), Tooltip("この強さより上は大雨")] public float heavyRain = 0.6f;

        [Header("風と動く雨雲")]
        [Tooltip("ふだんのそよ風の強さ（0〜1）。雨の前と大雨で強まり、夜は凪になる")] public float baseWindStrength = 0.38f;
        [Tooltip("島の上を同時に流れる大きな雨雲の最大数（1〜2個）")] [Range(1, 3)] public int rainCloudCount = 2;
        [Tooltip("雨雲の半径 (m)：小さい雲・大きい雲（150〜225 m ＝ 直径 300〜450 m、区画をまるごと覆う大きさ）")]
        public Vector2 rainCloudRadius = new Vector2(150f, 225f);
        [Tooltip("雨雲が東風で西へ流れる速さ（現実の1分あたり m。大きい雲はこれよりゆっくり流れる）")]
        public float rainCloudSpeedPerMin = 15f;
        [Range(0f, 1f), Tooltip("砂漠の乾いた空気で雨雲が痩せて消える強さ（1 で砂漠の奥までに消える）")]
        public float desertDissipate = 0.8f;

        [Header("試す用（開発中だけ）")]
        [Tooltip("時計をずらす（ゲームの時間, 時間）。F3 の画面で [ ] キーでも動かせる")] public float devOffsetGameHours = 0f;
        [Tooltip("時計の速さの倍率（1 = 本物）")] public float devTimeScale = 1f;
        [Tooltip("天気を固定する（Auto = 時刻どおり）")] public DevSky devSky = DevSky.Auto;

        public enum DevSky { Auto, Sunny, Cloudy, LightRain, HeavyRain }

        public float SeasonWeeks(int i) => Mathf.Max(0.01f, seasonWeeks != null && i < seasonWeeks.Length ? seasonWeeks[i] : 2.5f);
        public float SeasonDryness(int i) => Mathf.Clamp01(seasonDryness != null && i < seasonDryness.Length ? seasonDryness[i] : (i == 1 || i == 3 ? 1f : 0f));

        public float Wetness(IslandRegion r)
        {
            int i = (int)r;
            return regionWetness != null && i < regionWetness.Length ? regionWetness[i] : 0.5f;
        }
    }
}
