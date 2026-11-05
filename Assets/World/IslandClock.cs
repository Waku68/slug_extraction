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
        /// <summary>Which of the four seasons it is.</summary>
        public Season season;
        /// <summary>0..1 through this season.</summary>
        public float seasonProgress;
        /// <summary>The seasons counted from the epoch (four to a season of play). A plant dug up by the roots remembers this.</summary>
        public long seasonCount;
        /// <summary>The seasons of play counted from the epoch (each ends with the great drought).</summary>
        public long seasonOfPlay;
        public bool DrySeason => season == Season.Dry || season == Season.BigDry;
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

            // seasons (spec 4章 Oct 25): 大雨季 → 乾季 → 雨季 → 大乾季, then the great drought and round again
            Seasons(s, hours, ref t);
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

        /// <summary>Where in each season its dryness peaks: the big rains early (the season of play opens wet), the big dry
        /// late (driest, and the longest days, just before the great drought), the other two in the middle.</summary>
        static readonly float[] PeakAt = { 0.25f, 0.5f, 0.5f, 0.8f };

        static void Seasons(IslandSettings s, double hours, ref IslandTime t)
        {
            var start = new double[4];
            double total = 0;
            for (int i = 0; i < 4; i++) { start[i] = total; total += s.SeasonWeeks(i) * 7.0 * 24.0; }
            t.seasonOfPlay = (long)Math.Floor(hours / total);
            double x = hours - t.seasonOfPlay * total;
            int k = 3;
            while (k > 0 && x < start[k]) k--;
            t.season = (Season)k;
            t.seasonProgress = (float)((x - start[k]) / (s.SeasonWeeks(k) * 7.0 * 24.0));
            t.seasonCount = t.seasonOfPlay * 4 + k;

            // the dryness eases from one season's peak to the next (round past the drought into the next big rains)
            double Peak(int i) => start[i] + PeakAt[i] * s.SeasonWeeks(i) * 7.0 * 24.0;
            int a = 3;
            while (a >= 0 && x < Peak(a)) a--;
            double pa, pb;
            int b;
            if (a < 0) { a = 3; b = 0; pa = Peak(3) - total; pb = Peak(0); }
            else if (a == 3) { b = 0; pa = Peak(3); pb = Peak(0) + total; }
            else { b = a + 1; pa = Peak(a); pb = Peak(b); }
            float f = (float)((x - pa) / Math.Max(pb - pa, 1e-6));
            t.dryness = Mathf.Lerp(s.SeasonDryness(a), s.SeasonDryness(b), 0.5f - 0.5f * Mathf.Cos(Mathf.PI * f));
        }

        /// <summary>The season's name, for people.</summary>
        public static string SeasonName(Season season) =>
            season == Season.BigRainy ? "大雨季" : season == Season.Dry ? "乾季" : season == Season.Rainy ? "雨季" : "大乾季";

        /// <summary>The real hours at which the island's game hour reaches <paramref name="gameHours"/> after <paramref name="fromHours"/>.</summary>
        public static double AddGameHours(IslandSettings s, double fromHours, double gameHours) => fromHours + gameHours / 24.0 * s.dayHours;

        static double Frac(double x) => x - Math.Floor(x);
        static float Frac01(float x) => x - Mathf.Floor(x);
        /// <summary>Signed distance between two day phases, wrapping round midnight.</summary>
        static float DeltaPhase(float a, float b) { float d = Frac01(a - b + 0.5f) - 0.5f; return d; }
    }

    /// <summary>The weather over a region or a world point at one moment.</summary>
    public struct IslandWeatherState
    {
        /// <summary>0 = dry .. 1 = the heaviest rain.</summary>
        public float rain;
        /// <summary>0 = clear .. 1 = overcast.</summary>
        public float cloud;
        public Sky sky;
    }

    /// <summary>One large rain cloud drifting across the island on the wind (300..450 m across).</summary>
    public struct RainCloud
    {
        /// <summary>World XZ centre (m).</summary>
        public Vector2 center;
        /// <summary>Radius of the rain footprint (m; 150..225 m = 300..450 m diameter). The cloud canopy reaches ~1.35x this.</summary>
        public float radius;
        /// <summary>Peak rain at the middle (0..1).</summary>
        public float rain;
        /// <summary>Cloud thickness (0..1).</summary>
        public float cloud;
        /// <summary>Drift velocity in world XZ (m per real second).</summary>
        public Vector2 velocity;
        /// <summary>0 = healthy .. 1 = drying out over the western desert.</summary>
        public float desertDissipation;
    }

    /// <summary>The island's wind at one moment: direction (world XZ), strength (0..1) and gust (0..1).</summary>
    public struct IslandWindState
    {
        /// <summary>Unit vector in world XZ showing which way the wind blows (default: from the east toward -X).</summary>
        public Vector2 direction;
        /// <summary>0 = dead calm .. 1 = gale (calm at night, breeze by day, strong before and during heavy rain).</summary>
        public float strength;
        /// <summary>0..1 pulse of the current gust.</summary>
        public float gust;
        /// <summary>Wind speed in m/s for particles and sway.</summary>
        public float Speed => Mathf.Lerp(0.5f, 10.5f, strength) * (1f + 0.45f * gust);
    }

    /// <summary>
    /// The island's wind, worked out from the clock alone (docs/ideas/wind_ideas.md): trade wind from the east by default,
    /// stronger before and during rain, calm at night (夜凪), with gusts that roll across the grass and trees in waves.
    /// </summary>
    public static class IslandWind
    {
        public static IslandWindState At(IslandSettings s, double hours, float localRain = 0f, float localCloud = 0f)
        {
            var time = IslandClock.At(s, hours);
            int seed = s ? s.weatherSeed * 48611 : 48611;
            // Trade wind blows from the east (-X), swaying slowly by +-26 degrees over a few hours
            float angleDeg = (IslandWeather.SampleNoise(hours * 0.35, seed + 3) - 0.5f) * 52f;
            float rad = (180f + angleDeg) * Mathf.Deg2Rad;
            Vector2 dir = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)).normalized;

            float baseStr = s ? s.baseWindStrength : 0.38f;
            // Calm at night (夜凪), steady breeze by day, stronger when clouds gather and during heavy rain
            float dayCalm = Mathf.Lerp(0.32f, 1f, time.daylightAmount);
            float weatherBoost = Mathf.Max(localRain * 0.55f, Mathf.Max(0f, localCloud - 0.4f) * 0.45f);
            float slowVar = (IslandWeather.SampleNoise(hours * 1.4, seed + 19) - 0.5f) * 0.16f;
            float strength = Mathf.Clamp01((baseStr + slowVar) * dayCalm + weatherBoost);

            // Gust pulses everyseveral seconds (hours * 3600 = real seconds)
            double sec = hours * 3600.0;
            float g1 = IslandWeather.SampleNoise(sec * 0.14, seed + 41);
            float g2 = IslandWeather.SampleNoise(sec * 0.31 + 11.7, seed + 73);
            float gust = Mathf.SmoothStep(0.52f, 0.88f, g1 * 0.65f + g2 * 0.35f) * Mathf.Lerp(0.35f, 1f, strength);

            return new IslandWindState { direction = dir, strength = strength, gust = gust };
        }

        /// <summary>0..1 gust wave at a world position (travels downwind across the meadow and forest).</summary>
        public static float GustWaveAt(double hours, Vector3 worldPos, IslandWindState wind)
        {
            float along = Vector2.Dot(new Vector2(worldPos.x, worldPos.z), wind.direction);
            float phase = along * 0.085f - (float)((hours * 3600.0) % 10000.0) * 3.2f;
            return Mathf.Pow(0.5f + 0.5f * Mathf.Sin(phase), 2.2f) * wind.gust;
        }

        public static string DirectionName(Vector2 dir)
        {
            // Name by where the wind blows FROM (-dir)
            Vector2 from = -dir;
            float a = Mathf.Repeat(Mathf.Atan2(from.y, from.x) * Mathf.Rad2Deg, 360f);
            if (a < 22.5f || a >= 337.5f) return "東風";
            if (a < 67.5f) return "北東の風";
            if (a < 112.5f) return "北風";
            if (a < 157.5f) return "北西の風";
            if (a < 202.5f) return "西風";
            if (a < 247.5f) return "南西の風";
            if (a < 292.5f) return "南風";
            return "南東の風";
        }
    }

    /// <summary>
    /// The weather, worked out from the time and place alone (no state): 1-2 large rain clouds (300..450 m across) drift
    /// from east to west on the trade wind, raining under their core and shrinking as they enter the dry western desert
    /// (spec 4章). Everyone sees the same clouds at the same moment, and forecasts read their path ahead in time.
    /// </summary>
    public static class IslandWeather
    {
        /// <summary>Canonical centre of each region on the 800 m island (world XZ, m).</summary>
        public static Vector2 RegionCenter(IslandRegion region)
        {
            switch (region)
            {
                case IslandRegion.RainForest: return new Vector2(235f, -55f);
                case IslandRegion.SaltMountain: return new Vector2(16f, -4f);
                case IslandRegion.FlowerMeadow: return new Vector2(-35f, -198f);
                case IslandRegion.SaltFlat: return new Vector2(-276f, -124f);
                case IslandRegion.LavaPlateau: return new Vector2(96f, 195f);
                default: return Vector2.zero;
            }
        }

        /// <summary>The active moving rain clouds over the island at <paramref name="hours"/> (up to <see cref="IslandSettings.rainCloudCount"/>).</summary>
        public static void CloudsAt(IslandSettings s, double hours, List<RainCloud> clouds, IslandSettings.DevSky fixedSky = IslandSettings.DevSky.Auto)
        {
            clouds.Clear();
            if (!s) return;
            var dev = fixedSky != IslandSettings.DevSky.Auto ? fixedSky : s.devSky;
            if (dev == IslandSettings.DevSky.Sunny)
            {
                Vector2 center = WeatherPresenter.Current ? WeatherPresenter.Current.ObserverXZ : Vector2.zero;
                var wind = IslandWind.At(s, hours, 0f, 0.2f);
                clouds.Add(new RainCloud
                {
                    center = center + new Vector2(-210f, 290f),
                    radius = 175f,
                    rain = 0f,
                    cloud = 0.35f,
                    velocity = wind.direction * (s.rainCloudSpeedPerMin / 60f),
                    desertDissipation = 0f,
                });
                return;
            }
            if (dev == IslandSettings.DevSky.LightRain || dev == IslandSettings.DevSky.HeavyRain)
            {
                // When the sky is pinned to rain for development, place one cloud overhead and one visible across the sky
                Vector2 center = WeatherPresenter.Current ? WeatherPresenter.Current.ObserverXZ : Vector2.zero;
                float rRain = dev == IslandSettings.DevSky.HeavyRain ? 0.9f : 0.38f;
                var wind = IslandWind.At(s, hours, rRain, 1f);
                clouds.Add(new RainCloud
                {
                    center = center,
                    radius = 200f,
                    rain = rRain,
                    cloud = 1f,
                    velocity = wind.direction * (s.rainCloudSpeedPerMin / 60f),
                    desertDissipation = 0f,
                });
                clouds.Add(new RainCloud
                {
                    center = center + new Vector2(-220f, 310f),
                    radius = 185f,
                    rain = rRain,
                    cloud = 1f,
                    velocity = wind.direction * (s.rainCloudSpeedPerMin / 60f),
                    desertDissipation = 0f,
                });
                return;
            }

            var time = IslandClock.At(s, hours);
            int count = Mathf.Clamp(s.rainCloudCount, 1, 3);
            int seed = s.weatherSeed * 104729 + 17;
            float seasonWet = Mathf.Clamp01(1f - time.dryness * 0.75f + s.rainySeasonBoost);
            Vector2 rRange = s.rainCloudRadius.x > 10f ? s.rainCloudRadius : new Vector2(150f, 225f);
            float baseSpeedPerHour = Mathf.Max(4f, s.rainCloudSpeedPerMin) * 60f;   // metres per real hour

            for (int slot = 0; slot < count; slot++)
            {
                int slotKey = seed + slot * 7919;
                // Larger clouds travel slightly slower; each cycle crosses ~960 m from east (+480 m) to west (-480 m)
                double approxCycleHours = 960.0 / baseSpeedPerHour;
                double u = hours / Math.Max(0.15, approxCycleHours) + slot / (double)count;
                long gen = (long)Math.Floor(u);
                float phase = (float)(u - gen);   // 0 = entering from the east .. 1 = leaving to the west

                float size01 = Hash(gen, slotKey + 3);
                float radius = Mathf.Lerp(rRange.x, rRange.y, size01) * Mathf.Lerp(0.85f, 1.12f, seasonWet);
                // Slower when larger
                float speedScale = Mathf.Lerp(1.15f, 0.82f, size01);
                var wind = IslandWind.At(s, hours);
                Vector2 driftDir = Vector2.Lerp(new Vector2(-1f, 0f), wind.direction, 0.65f).normalized;
                Vector2 perp = new Vector2(-driftDir.y, driftDir.x);

                float travel = Mathf.Lerp(-480f, 480f, phase);
                // Lane offset across north-south (-230..+210 m), with a gentle curve along the path
                float laneZ = Mathf.Lerp(-220f, 200f, Hash(gen, slotKey + 11))
                              + Mathf.Sin(phase * Mathf.PI * 1.5f + Hash(gen, slotKey + 23) * 6f) * 45f;
                Vector2 center = driftDir * travel + perp * laneZ;

                // Does this cloud carry rain? Wet seasons produce heavy rain clouds; dry seasons sometimes pass as dry shade clouds
                float vitality = Hash(gen, slotKey + 37) * 0.7f + seasonWet * 0.55f;
                float edgeFade = Smooth(0f, 0.08f, phase) * Smooth(1f, 0.90f, phase);
                if (vitality < 0.28f || edgeFade <= 0.001f) continue;

                // Desert dissipation (spec 4章: 雨雲は砂漠の上で消えやすい): the western desert is roughly x in [-380..-60], z in [-80..180]
                float inDesertX = Smooth(-40f, -260f, center.x);
                float inDesertZ = 1f - Smooth(90f, 220f, Mathf.Abs(center.y - 35f));
                float dissipate = Mathf.Clamp01(inDesertX * inDesertZ * s.desertDissipate);

                float radiusNow = radius * Mathf.Lerp(1f, 0.48f, dissipate);
                float rainNow = Mathf.Clamp01((vitality - 0.28f) * 1.35f) * edgeFade * (1f - 0.9f * dissipate);
                float cloudNow = Mathf.Clamp01(vitality * 1.15f) * edgeFade * (1f - 0.65f * dissipate);
                if (cloudNow < 0.05f) continue;

                clouds.Add(new RainCloud
                {
                    center = center,
                    radius = radiusNow,
                    rain = rainNow,
                    cloud = cloudNow,
                    velocity = driftDir * (baseSpeedPerHour * speedScale / 3600f),
                    desertDissipation = dissipate,
                });
            }
        }

        static readonly List<RainCloud> _scratchClouds = new List<RainCloud>(4);

        /// <summary>The weather at a world point <paramref name="worldPos"/> and moment <paramref name="hours"/>.</summary>
        public static IslandWeatherState At(IslandSettings s, IslandRegion region, double hours, Vector3 worldPos,
                                            IslandSettings.DevSky fixedSky = IslandSettings.DevSky.Auto)
        {
            var w = new IslandWeatherState();
            if (!s) return w;
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

            // Moving rain clouds over the island: rain falls under a cloud's core, clouds spread wider around its rim
            Vector2 p = new Vector2(worldPos.x, worldPos.z);
            if (region == IslandRegion.TestField && p.sqrMagnitude < 140f * 140f)
            {
                // In small test scenes (ExtractTest), scale world coordinates so moving clouds sweep across the field too
                p *= 2.2f;
            }

            float cloudRain = 0f, cloudCover = 0f;
            lock (_scratchClouds)
            {
                CloudsAt(s, hours, _scratchClouds, fixedSky);
                for (int i = 0; i < _scratchClouds.Count; i++)
                {
                    var c = _scratchClouds[i];
                    float d = Vector2.Distance(p, c.center) / Mathf.Max(c.radius, 1f);
                    // Rain falls inside radius (heavy in the inner 60%, light rain out to 100%)
                    float rHere = c.rain * (1f - Smooth(0.35f, 1.0f, d));
                    // Cloud canopy extends to 1.38x the rain radius
                    float cHere = c.cloud * (1f - Smooth(0.65f, 1.38f, d));
                    cloudRain = Mathf.Max(cloudRain, rHere);
                    cloudCover = Mathf.Max(cloudCover, cHere);
                }
            }

            // Combine the moving rain cloud at this spot with the regional sky
            float regionalCloud = Mathf.Max(Smooth(threshold - 0.25f, threshold + 0.05f, n) * 0.65f, 0.55f * Noise(x * 1.9 + 101.7, key + 77));
            w.rain = cloudRain;
            w.cloud = Mathf.Clamp01(Mathf.Max(cloudCover, regionalCloud));
            w.sky = w.rain >= s.heavyRain ? Sky.HeavyRain : w.rain > 0.05f ? Sky.LightRain : w.cloud > 0.5f ? Sky.Cloudy : Sky.Sunny;
            return w;
        }

        /// <summary>The weather over a region at that moment (<paramref name="fixedSky"/>: a sky fixed for development, Auto = as the clock says).</summary>
        public static IslandWeatherState At(IslandSettings s, IslandRegion region, double hours, IslandSettings.DevSky fixedSky = IslandSettings.DevSky.Auto)
        {
            Vector2 c = RegionCenter(region);
            return At(s, region, hours, new Vector3(c.x, 0f, c.y), fixedSky);
        }

        /// <summary>
        /// The forecast for a region or spot: one entry every <paramref name="stepGameHours"/> (the observatory makes the step finer),
        /// for <paramref name="aheadGameHours"/>, each the wettest sky in its stretch.
        /// </summary>
        public static List<(double hours, Sky sky, float rain)> Forecast(IslandSettings s, IslandRegion region, double fromHours,
                                                                         float aheadGameHours, float stepGameHours)
        {
            Vector2 c = RegionCenter(region);
            return Forecast(s, region, new Vector3(c.x, 0f, c.y), fromHours, aheadGameHours, stepGameHours);
        }

        public static List<(double hours, Sky sky, float rain)> Forecast(IslandSettings s, IslandRegion region, Vector3 worldPos,
                                                                         double fromHours, float aheadGameHours, float stepGameHours)
        {
            var list = new List<(double, Sky, float)>();
            int steps = Mathf.Max(1, Mathf.RoundToInt(aheadGameHours / Mathf.Max(stepGameHours, 0.01f)));
            for (int i = 0; i < steps; i++)
            {
                double a = IslandClock.AddGameHours(s, fromHours, i * stepGameHours);
                float worstRain = 0f, maxCloud = 0f;
                for (int k = 0; k < 6; k++)
                {
                    var w = At(s, region, IslandClock.AddGameHours(s, a, stepGameHours * (k + 0.5f) / 6f), worldPos);
                    worstRain = Mathf.Max(worstRain, w.rain);
                    maxCloud = Mathf.Max(maxCloud, w.cloud);
                }
                Sky sky = worstRain >= s.heavyRain ? Sky.HeavyRain : worstRain > 0.05f ? Sky.LightRain : maxCloud > 0.5f ? Sky.Cloudy : Sky.Sunny;
                list.Add((a, sky, worstRain));
            }
            return list;
        }

        public static float SampleNoise(double x, int key) => Noise(x, key);

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
