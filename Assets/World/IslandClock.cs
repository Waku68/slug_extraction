using System;
using System.Collections.Generic;
using UnityEngine;

namespace SlugWorld
{
    /// <summary>The island at one moment: the time of day, the season, the sun, the moon and the tide.</summary>
    public struct IslandTime
    {
        /// <summary>Real hours since the epoch (the island's clock runs on this).</summary>
        public double hours;
        /// <summary>Which game day it is (counted from the epoch).</summary>
        public long day;
        /// <summary>0..1 through the game day, from midnight.</summary>
        public float dayPhase;
        /// <summary>0 = the height of the rainy season, 1 = the height of the dry season.</summary>
        public float dryness;
        public bool DrySeason => dryness >= 0.5f;
        /// <summary>The part of the day that is day (longer in the dry season).</summary>
        public float daylight;
        /// <summary>When the sun rises and sets (day phase).</summary>
        public float sunrise, sunset;
        /// <summary>The way the sunlight travels (from the sun to the ground). Below the horizon at night.</summary>
        public Vector3 sunDirection;
        /// <summary>The sun's height (sine of its elevation; below 0 at night).</summary>
        public float sunHeight;
        /// <summary>0 = night .. 1 = full day, easing through dawn and dusk.</summary>
        public float daylightAmount;
        /// <summary>0..1: how much it is dawn or dusk right now (the warm, low light).</summary>
        public float twilight;
        public bool Night => daylightAmount < 0.5f;
        /// <summary>0 = new moon, 0.5 = full moon.</summary>
        public float moonPhase;
        /// <summary>0 (new moon) .. 1 (full moon): how bright the night is.</summary>
        public float moonLight;
        /// <summary>-1 = low tide .. 1 = high tide; the swing is biggest at new and full moon (spring tides).</summary>
        public float tide;
        /// <summary>The hour on a 24-hour face (for people; the island shows no numbers to the player).</summary>
        public float GameHour => dayPhase * 24f;
    }

    /// <summary>
    /// The island's clock: everything is worked out from the real time alone (no state), so it is the same for every
    /// player and every server, and an empty region can sleep and wake up at the right moment
    /// (docs/discussions/island_clock_discussion.md). The day is <see cref="IslandSettings.dayHours"/> real hours long
    /// (not a divisor of 24, so the island's time of day drifts from day to day); the seasons change how long the day
    /// is; the moon and the tide run on their own clocks.
    /// </summary>
    public static class IslandClock
    {
        /// <summary>Real hours since the settings' epoch, right now (UTC).</summary>
        public static double RealHoursNow(IslandSettings s)
        {
            var epoch = DateTime.TryParse(s.epochUtc, System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.AssumeUniversal | System.Globalization.DateTimeStyles.AdjustToUniversal, out var e)
                ? e : new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc);
            return (DateTime.UtcNow - epoch).TotalHours;
        }

        public static IslandTime At(IslandSettings s, double hours)
        {
            var t = new IslandTime { hours = hours };
            double dayLen = Math.Max(0.01, s.dayHours);
            t.day = (long)Math.Floor(hours / dayLen);
            t.dayPhase = (float)(hours / dayLen - t.day);

            // seasons: a smooth swing between the rainy and the dry season
            double seasonLen = Math.Max(0.01, s.seasonDays) * 24.0;
            t.dryness = 0.5f - 0.5f * Mathf.Cos((float)(2.0 * Math.PI * Frac(hours / (2.0 * seasonLen))));
            t.daylight = Mathf.Lerp(s.rainyDaylight, s.dryDaylight, t.dryness);
            t.sunrise = 0.5f - t.daylight * 0.5f;
            t.sunset = 0.5f + t.daylight * 0.5f;

            // the sun: rises in the east (+x), goes over the south, sets in the west
            float p = t.dayPhase;
            float el, az;
            if (p >= t.sunrise && p <= t.sunset)
            {
                float u = (p - t.sunrise) / Mathf.Max(t.daylight, 1e-3f);
                el = Mathf.Sin(Mathf.PI * u) * s.noonElevation;
                az = Mathf.Lerp(90f, 270f, u);
            }
            else
            {
                float nightLen = 1f - t.daylight;
                float u = Frac01((p - t.sunset) / Mathf.Max(nightLen, 1e-3f));
                el = -Mathf.Sin(Mathf.PI * u) * 30f;
                az = Mathf.Lerp(270f, 450f, u);
            }
            float elR = el * Mathf.Deg2Rad, azR = az * Mathf.Deg2Rad;
            Vector3 toSun = new Vector3(Mathf.Cos(elR) * Mathf.Sin(azR), Mathf.Sin(elR), Mathf.Cos(elR) * Mathf.Cos(azR));
            t.sunDirection = -toSun;
            t.sunHeight = toSun.y;

            // day and night ease through a short dawn and dusk
            float tw = Mathf.Max(s.twilightHours / 24f, 1e-3f);
            float fromRise = Mathf.Abs(DeltaPhase(p, t.sunrise)), fromSet = Mathf.Abs(DeltaPhase(p, t.sunset));
            t.twilight = Mathf.Clamp01(1f - Mathf.Min(fromRise, fromSet) / tw);
            float dayRamp = Mathf.InverseLerp(t.sunrise - tw * 0.5f, t.sunrise + tw * 0.5f, p) * (1f - Mathf.InverseLerp(t.sunset - tw * 0.5f, t.sunset + tw * 0.5f, p));
            t.daylightAmount = dayRamp * dayRamp * (3f - 2f * dayRamp);

            // the moon and the tide, on their own clocks
            t.moonPhase = (float)Frac(hours / Math.Max(0.01, s.moonCycleHours));
            t.moonLight = 0.5f - 0.5f * Mathf.Cos(2f * Mathf.PI * t.moonPhase);
            float spring = 0.55f + 0.45f * Mathf.Abs(Mathf.Cos(2f * Mathf.PI * t.moonPhase));
            t.tide = Mathf.Sin((float)(2.0 * Math.PI * Frac(hours / Math.Max(0.01, s.tideHours)))) * spring;
            return t;
        }

        /// <summary>The real hours at which the island's game hour reaches <paramref name="gameHours"/> after <paramref name="fromHours"/>.</summary>
        public static double AddGameHours(IslandSettings s, double fromHours, double gameHours) => fromHours + gameHours / 24.0 * s.dayHours;

        static double Frac(double x) => x - Math.Floor(x);
        static float Frac01(float x) => x - Mathf.Floor(x);
        /// <summary>Signed distance between two day phases, wrapping round midnight.</summary>
        static float DeltaPhase(float a, float b) { float d = Frac01(a - b + 0.5f) - 0.5f; return d; }
    }

    /// <summary>The weather over a region at one moment.</summary>
    public struct IslandWeatherState
    {
        /// <summary>0 = dry .. 1 = the heaviest rain.</summary>
        public float rain;
        /// <summary>0 = clear .. 1 = overcast.</summary>
        public float cloud;
        public Sky sky;
    }

    /// <summary>
    /// The weather, worked out from the time alone (a smooth noise in time, a different one for each region), so it is the
    /// same for everyone and can be forecast. Wet regions rain more, and everywhere rains more in the rainy season.
    /// </summary>
    public static class IslandWeather
    {
        /// <summary>The weather at that moment (<paramref name="fixedSky"/>: a sky fixed for development, Auto = as the clock says).</summary>
        public static IslandWeatherState At(IslandSettings s, IslandRegion region, double hours, IslandSettings.DevSky fixedSky = IslandSettings.DevSky.Auto)
        {
            var w = new IslandWeatherState();
            switch (fixedSky != IslandSettings.DevSky.Auto ? fixedSky : s.devSky)
            {
                case IslandSettings.DevSky.Sunny: w.sky = Sky.Sunny; return w;
                case IslandSettings.DevSky.Cloudy: w.cloud = 0.8f; w.sky = Sky.Cloudy; return w;
                case IslandSettings.DevSky.LightRain: w.cloud = 1f; w.rain = 0.35f; w.sky = Sky.LightRain; return w;
                case IslandSettings.DevSky.HeavyRain: w.cloud = 1f; w.rain = 0.9f; w.sky = Sky.HeavyRain; return w;
            }
            var time = IslandClock.At(s, hours);
            double x = hours / Math.Max(0.01, s.weatherScaleHours);
            int key = (int)region * 7919 + s.weatherSeed * 104729;
            float n = 0.65f * Noise(x, key) + 0.35f * Noise(x * 2.7 + 17.3, key + 31);
            float wet = s.Wetness(region) * 0.6f + s.rainySeasonBoost * (1f - time.dryness);
            float threshold = 1f - wet;
            w.rain = Smooth(threshold, threshold + 0.22f, n);
            w.cloud = Mathf.Max(Smooth(threshold - 0.22f, threshold, n), 0.6f * Noise(x * 1.9 + 101.7, key + 77));
            w.sky = w.rain >= s.heavyRain ? Sky.HeavyRain : w.rain > 0.05f ? Sky.LightRain : w.cloud > 0.5f ? Sky.Cloudy : Sky.Sunny;
            return w;
        }

        /// <summary>
        /// The forecast for a region: one entry every <paramref name="stepGameHours"/> (the observatory makes the step finer),
        /// for <paramref name="aheadGameHours"/>, each the wettest sky in its stretch.
        /// </summary>
        public static List<(double hours, Sky sky, float rain)> Forecast(IslandSettings s, IslandRegion region, double fromHours,
                                                                         float aheadGameHours, float stepGameHours)
        {
            var list = new List<(double, Sky, float)>();
            int steps = Mathf.Max(1, Mathf.RoundToInt(aheadGameHours / Mathf.Max(stepGameHours, 0.01f)));
            for (int i = 0; i < steps; i++)
            {
                double a = IslandClock.AddGameHours(s, fromHours, i * stepGameHours);
                float worstRain = 0f, maxCloud = 0f;
                for (int k = 0; k < 6; k++)
                {
                    var w = At(s, region, IslandClock.AddGameHours(s, a, stepGameHours * (k + 0.5f) / 6f));
                    worstRain = Mathf.Max(worstRain, w.rain);
                    maxCloud = Mathf.Max(maxCloud, w.cloud);
                }
                Sky sky = worstRain >= s.heavyRain ? Sky.HeavyRain : worstRain > 0.05f ? Sky.LightRain : maxCloud > 0.5f ? Sky.Cloudy : Sky.Sunny;
                list.Add((a, sky, worstRain));
            }
            return list;
        }

        static float Smooth(float a, float b, float x) { float t = Mathf.Clamp01((x - a) / (b - a)); return t * t * (3f - 2f * t); }

        /// <summary>Smooth value noise in one dimension, 0..1.</summary>
        static float Noise(double x, int key)
        {
            double f = Math.Floor(x);
            long i = (long)f;
            float u = (float)(x - f);
            u = u * u * (3f - 2f * u);
            return Mathf.Lerp(Hash(i, key), Hash(i + 1, key), u);
        }

        static float Hash(long i, int key)
        {
            unchecked
            {
                uint h = (uint)i * 374761393u + (uint)(i >> 32) * 2246822519u + (uint)key * 668265263u;
                h = (h ^ (h >> 13)) * 1274126177u;
                h ^= h >> 16;
                return (h & 0xFFFFFF) / (float)0xFFFFFF;
            }
        }
    }
}
