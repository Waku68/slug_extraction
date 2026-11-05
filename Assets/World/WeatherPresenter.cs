using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SlugWorld
{
    /// <summary>
    /// Shows the island's clock, wind and weather in a region (<see cref="IslandClock"/>, <see cref="IslandWeather"/>,
    /// <see cref="IslandWind"/>): the sun moves (east to west, longer days in the dry season), dawn and dusk are short and
    /// warm, the night is lit by the moon (bright enough to harvest by), large rain clouds drift on the east trade wind and
    /// cast dappled shadows and columns of rain, rain slants in the wind, near surfaces deepen and glisten wet while far
    /// island surfaces fade into rain haze (leaving bright slugs visible), and plants sway in gusts.
    /// Others read the moment from <see cref="Current"/> (the slug's moisture, for one).
    /// F3 shows the clock, wind, moving clouds and forecast for development (the island itself shows no numbers):
    /// [ / ] move the clock an hour, \ steps through fixed skies, / runs the clock fast.
    /// </summary>
    [DefaultExecutionOrder(-50)]
    public class WeatherPresenter : MonoBehaviour
    {
        public IslandSettings settings;
        [Tooltip("この場面がどの区画か（天気はここで決まる）")] public IslandRegion region = IslandRegion.TestField;
        [Tooltip("太陽（と夜は月）の光。空なら場面の平行光源を探す")] public Light sun;
        [Tooltip("雨を降らせる場所（目のまわり）。空ならメインカメラ")] public Transform rainAround;
        [Tooltip("昼の光の強さ")] public float dayIntensity = 1.25f;
        [Tooltip("月夜の光の強さ（新月 → 満月）。夜でも採取できる明るさに")] public Vector2 nightIntensity = new Vector2(0.32f, 0.5f);
        [Tooltip("霧の濃さの倍率。島全体を見渡す場面（IslandTest）では薄くする。空気（airLook）を切ったときだけ効く")] public float fogScale = 1f;
        [Tooltip("ケルン風の空気：遠くが空の色に溶けて層になり（晴れでも）、影に時刻の色がつく（晴れは青、夕方は紫）。切ると前の見た目")] public bool airLook = true;
        [Tooltip("空気：晴れの昼に、半分だけ空の色に溶ける距離 (m)。手前はほぼ澄んで、この倍でほぼ消える")] public float hazeHalfClear = 95f;
        [Tooltip("空気：大雨で、半分だけ溶ける距離 (m)")] public float hazeHalfRain = 32f;
        [Tooltip("空気：晴れの日に、いちばん遠い陸がどれだけ空に溶けるか（1 で消える。少し残すと遠くの陸が薄い影の層になる）")] [Range(0.5f, 1f)] public float farLand = 0.8f;
        [Tooltip("ケルン風の空：上は深い青、地平線は空気の色、太陽のまわりが暖かく、地平線に積雲、頭の上にちぎれ雲（Slug/PaintedSky）。切ると無地の色")] public bool paintedSky = true;
        [Tooltip("空のマテリアル（空なら Slug/PaintedSky から作る）")] public Material skyMaterial;
        [Tooltip("谷にたまる朝の霧：朝と雨上がりだけ、低い所ほど濃い（高い所から見下ろすと層になる）")] public bool valleyMist = true;
        [Tooltip("霧：いちばん濃いとき、海の高さで 1 m あたりどれだけ白くなるか")] public float mistDensity = 0.03f;
        [Tooltip("霧：この高さ (m) 上がるごとに約 1/3 に薄くなる")] public float mistHeight = 5f;
        [Tooltip("朝夕に高い所だけ桃色に光る（白い岩塩の山が光る）")] public bool dawnGlow = true;
        [Tooltip("光り始める高さ (m) と、そこから全部光るまでの高さ (m)")] public Vector2 glowHeight = new Vector2(55f, 45f);
        [Tooltip("地面に流れる雲の影の強さ（晴れの昼に、風で地面をまだらが渡る）")] [Range(0f, 0.6f)] public float cloudShadowStrength = 0.34f;

        /// <summary>The presenter of the open scene (one per region).</summary>
        public static WeatherPresenter Current { get; private set; }
        /// <summary>The development view (F3) is open: others may show their values too.</summary>
        public static bool DevVisible => Current && Current._dev;
        /// <summary>Real hours since the epoch, as this scene shows them (the developer can move it).</summary>
        public double Hours { get; private set; }
        public IslandTime Time { get; private set; }
        public IslandWeatherState Weather { get; private set; }
        public IslandWindState Wind { get; private set; }
        /// <summary>The colour of the sky (and the far fog) right now. Cameras that clear to the sky use it.</summary>
        public Color SkyColor { get; private set; }
        public IslandSettings.DevSky FixedSky => _devSky;

        /// <summary>True only in small prototype scenes (ExtractTest), false on the 800 m island even if region was left at default.</summary>
        public bool IsSmallTestField => region == IslandRegion.TestField && (Terrain.activeTerrain == null || Terrain.activeTerrain.terrainData.size.x < 400f);

        /// <summary>Where the player/camera is looking from right now.</summary>
        public Vector3 ObserverPosition
        {
            get
            {
                if (rainAround) return rainAround.position;
                var cam = Camera.main;
                return cam ? cam.transform.position : transform.position;
            }
        }
        public Vector2 ObserverXZ { get { var p = ObserverPosition; return new Vector2(p.x, p.z); } }

        double _offsetHours;
        IslandSettings.DevSky _devSky;      // a sky fixed in this scene only (the settings asset is left alone)
        bool _dev, _fast;
        ParticleSystem _rain;
        GUIStyle _style;
        readonly List<RainCloud> _clouds = new List<RainCloud>(4);

        void Awake() => Hours = IslandClock.RealHoursNow(settings);   // before anyone's Start reads the time
        void OnEnable() => Current = this;
        void OnDisable()
        {
            if (Current == this) Current = null;
            Shader.SetGlobalVector(AirId, Vector4.zero);         // other scenes (previews) see the plain toon again
            Shader.SetGlobalVector(ShadeTintId, Vector4.zero);
            Shader.SetGlobalVector(MistId, Vector4.zero);
            Shader.SetGlobalVector(GlowRangeId, Vector4.zero);
            Shader.SetGlobalVector(WetId, Vector4.zero);
            Shader.SetGlobalVector(CloudShadowId, Vector4.zero);
            Shader.SetGlobalVector(RainCloud0Id, Vector4.zero);
            Shader.SetGlobalVector(RainCloud1Id, Vector4.zero);
            Shader.SetGlobalVector(WindId, Vector4.zero);
            Shader.SetGlobalVector(GustId, Vector4.zero);
        }

        static readonly int AirId = Shader.PropertyToID("_SlugAir");
        static readonly int AirColorId = Shader.PropertyToID("_SlugAirColor");
        static readonly int ShadeTintId = Shader.PropertyToID("_SlugShadeTint");
        static readonly int MistId = Shader.PropertyToID("_SlugMist");
        static readonly int MistColorId = Shader.PropertyToID("_SlugMistColor");
        static readonly int GlowId = Shader.PropertyToID("_SlugGlow");
        static readonly int GlowRangeId = Shader.PropertyToID("_SlugGlowRange");
        static readonly int WetId = Shader.PropertyToID("_SlugWet");
        static readonly int CloudShadowId = Shader.PropertyToID("_SlugCloudShadow");
        static readonly int RainCloud0Id = Shader.PropertyToID("_SlugRainCloud0");
        static readonly int RainCloud1Id = Shader.PropertyToID("_SlugRainCloud1");
        static readonly int WindId = Shader.PropertyToID("_SlugWind");
        static readonly int GustId = Shader.PropertyToID("_SlugGust");

        void Start()
        {
            if (!sun)
                foreach (var l in FindObjectsByType<Light>())
                    if (l.type == LightType.Directional) { sun = l; break; }
            // the sky is painted from the time and the weather, or a plain colour (the built-in sky turns orange at the horizon)
            RenderSettings.skybox = SkyMaterial();
            if (sun) RenderSettings.sun = sun;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            BuildRain();
            if (!GetComponent<SunShafts>()) gameObject.AddComponent<SunShafts>();
            if (!GetComponent<RainCloudVisuals>()) gameObject.AddComponent<RainCloudVisuals>();
            if (!GetComponent<WindEffects>()) gameObject.AddComponent<WindEffects>();
            Step(0f);
        }

        void Update()
        {
            var kb = Keyboard.current;
            if (kb != null)
            {
                if (kb.f3Key.wasPressedThisFrame) _dev = !_dev;
                if (_dev)
                {
                    float gameHour = settings.dayHours / 24f;
                    if (kb.leftBracketKey.wasPressedThisFrame) _offsetHours -= gameHour;
                    if (kb.rightBracketKey.wasPressedThisFrame) _offsetHours += gameHour;
                    if (kb.backslashKey.wasPressedThisFrame) _devSky = (IslandSettings.DevSky)(((int)_devSky + 1) % 5);
                    if (kb.slashKey.wasPressedThisFrame) _fast = !_fast;
                }
            }
            Step(UnityEngine.Time.deltaTime);
        }

        void Step(float dt)
        {
            float scale = settings.devTimeScale * (_fast ? 240f : 1f);
            Hours += dt * scale / 3600.0;
            double shown = ShownHours;
            Time = IslandClock.At(settings, shown);
            Weather = IslandWeather.At(settings, region, shown, ObserverPosition, _devSky);
            Wind = IslandWind.At(settings, shown, Weather.rain, Weather.cloud);
            ApplyLight();
            ApplyRain();
        }

        /// <summary>
        /// Development: shows the island at <paramref name="gameHour"/> (0..24) of today with this sky, at once (rain already
        /// falling). For film strips and tests.
        /// </summary>
        public void ShowNow(float gameHour, IslandSettings.DevSky sky)
        {
            _devSky = sky;
            float now = IslandClock.At(settings, ShownHours).GameHour;
            float delta = Mathf.Repeat(gameHour - now + 12f, 24f) - 12f;
            _offsetHours += delta / 24.0 * settings.dayHours;
            Step(0f);
            if (_rain && Weather.rain > 0f) { _rain.Simulate(1.2f, true, true); _rain.Play(); }
            else if (_rain) _rain.Clear();
        }

        /// <summary>The island's moment as this scene shows it (for others that need the real hours, e.g. a forecast).</summary>
        public double ShownHours => Hours + _offsetHours + settings.devOffsetGameHours / 24.0 * settings.dayHours;

        /// <summary>The weather this scene shows at the observer's position at any moment.</summary>
        public IslandWeatherState WeatherAt(double hours) => IslandWeather.At(settings, region, hours, ObserverPosition, _devSky);

        /// <summary>The weather this scene shows at a given world position and moment.</summary>
        public IslandWeatherState WeatherAt(double hours, Vector3 worldPos) => IslandWeather.At(settings, region, hours, worldPos, _devSky);

        void ApplyLight()
        {
            var t = Time;
            var w = Weather;
            float day = t.daylightAmount;
            float overcast = Mathf.Max(w.cloud * 0.7f, w.rain);

            // sun by day, moon by night (the moon stays high in the south so the night still has shape)
            // Dyer's light colours (rain_color_discussion.md): warm golden light when sunny, cool blue-tinted light in overcast & rain
            if (sun)
            {
                Quaternion sunRot = Quaternion.LookRotation(t.sunDirection.y < -0.05f ? t.sunDirection : new Vector3(t.sunDirection.x, Mathf.Min(t.sunDirection.y, -0.05f), t.sunDirection.z));
                Quaternion moonRot = Quaternion.Euler(55f, 160f + 40f * t.moonPhase, 0f);
                sun.transform.rotation = Quaternion.Slerp(moonRot, sunRot, Mathf.SmoothStep(0f, 1f, day));
                float nightI = Mathf.Lerp(nightIntensity.x, nightIntensity.y, t.moonLight);
                float dayI = dayIntensity * (1f - 0.45f * overcast);
                sun.intensity = Mathf.Lerp(nightI, dayI, day);
                Color sunnyLight = new Color(1f, 0.96f, 0.85f);
                Color rainyLight = Color.Lerp(new Color(0.80f, 0.87f, 0.96f), new Color(0.70f, 0.80f, 0.94f), w.rain);
                Color dayC = Color.Lerp(sunnyLight, rainyLight, overcast);
                Color duskC = new Color(1f, 0.72f, 0.5f);
                Color nightC = new Color(0.55f, 0.66f, 1f);
                Color c = Color.Lerp(nightC, dayC, day);
                sun.color = Color.Lerp(c, duskC, 0.75f * t.twilight * (1f - overcast * 0.7f));
                sun.shadowStrength = Mathf.Lerp(0.5f, 1f - 0.6f * overcast, day);
            }

            // the air: ambient light, the sky, the fog
            Color skyDay = Color.Lerp(new Color(0.55f, 0.68f, 0.85f), new Color(0.50f, 0.56f, 0.65f), overcast);
            Color skyNight = new Color(0.12f, 0.15f, 0.26f) * Mathf.Lerp(0.8f, 1.3f, t.moonLight);
            Color sky = Color.Lerp(skyNight, skyDay, day);
            sky = Color.Lerp(sky, new Color(0.85f, 0.6f, 0.5f), t.twilight * 0.25f);
            // the colour of the shade right now: blue on a clear day, violet at dusk, cool blue-slate under rain
            Color dayShade = Color.Lerp(new Color(0.88f, 0.94f, 1.1f), new Color(0.86f, 0.93f, 1.07f), overcast);
            Color shade = Color.Lerp(new Color(0.9f, 0.94f, 1.08f), dayShade, day);
            shade = Color.Lerp(shade, new Color(1f, 0.9f, 1.05f), t.twilight * (1f - overcast * 0.7f));
            Color shadeAmb = airLook ? shade : Color.white;   // the Lit ground's shade is its ambient
            RenderSettings.ambientSkyColor = sky * shadeAmb;
            RenderSettings.ambientEquatorColor = sky * 0.8f * shadeAmb;
            RenderSettings.ambientGroundColor = sky * 0.45f * shadeAmb;
            Color clearDay = new Color(0.66f, 0.78f, 0.92f), greyDay = new Color(0.56f, 0.62f, 0.69f);
            Color skyCol = Color.Lerp(new Color(0.05f, 0.07f, 0.14f) * Mathf.Lerp(0.8f, 1.4f, t.moonLight), Color.Lerp(clearDay, greyDay, overcast), day);
            skyCol = Color.Lerp(skyCol, new Color(0.95f, 0.7f, 0.55f), t.twilight * 0.45f * (1f - overcast * 0.6f));
            SkyColor = skyCol;
            var skyMat = SkyMaterial();
            RenderSettings.skybox = skyMat;
            if (skyMat) PaintSky(skyMat, t, w, skyCol, day, overcast);
            foreach (var c in Camera.allCameras)
            {
                if (c.targetTexture != null) continue;        // (the slug's own portrait clears to nothing)
                c.clearFlags = skyMat ? CameraClearFlags.Skybox : CameraClearFlags.SolidColor;
                c.backgroundColor = skyCol;
                if (!IsSmallTestField && c.farClipPlane < 1200f) c.farClipPlane = 1200f;
            }
            RenderSettings.fog = true;
            RenderSettings.fogColor = skyCol;

            // Global wind uniforms for plants, grass and palms (SlugWind.hlsl)
            float windClock = (float)((ShownHours * 3600.0) % 10000.0);
            Shader.SetGlobalVector(WindId, new Vector4(Wind.direction.x, Wind.direction.y, Wind.strength, windClock));
            Shader.SetGlobalVector(GustId, new Vector4(0.085f, 3.2f, 0.65f + 0.55f * Wind.gust, 11f));

            // Moving rain clouds and wind-drifting ground cloud shadows
            IslandWeather.CloudsAt(settings, ShownHours, _clouds, _devSky);
            bool smallField = IsSmallTestField;
            float posScale = smallField ? 1f / 2.2f : 1f;
            float radScale = smallField ? 0.55f : 1f;
            Vector4 rc0 = Vector4.zero, rc1 = Vector4.zero;
            if (_clouds.Count > 0)
            {
                var c0 = _clouds[0];
                rc0 = new Vector4(c0.center.x * posScale, c0.center.y * posScale, 1f / Mathf.Max(c0.radius * radScale, 1f), c0.rain);
            }
            if (_clouds.Count > 1)
            {
                var c1 = _clouds[1];
                rc1 = new Vector4(c1.center.x * posScale, c1.center.y * posScale, 1f / Mathf.Max(c1.radius * radScale, 1f), c1.rain);
            }
            Shader.SetGlobalVector(RainCloud0Id, rc0);
            Shader.SetGlobalVector(RainCloud1Id, rc1);

            Vector2 shadowDrift = -Wind.direction * (windClock * 0.018f);
            float shadowAmt = cloudShadowStrength * day * (1f - 0.65f * w.rain);
            Shader.SetGlobalVector(CloudShadowId, new Vector4(shadowDrift.x, shadowDrift.y, shadowAmt, 0.011f));

            // Wetness & rain look (SlugAir.hlsl): sunny ground dries first after rain, shade stays wet 4x longer
            WetAfterRainAmount(out float wetSun, out float wetShade);
            Shader.SetGlobalVector(WetId, new Vector4(wetSun, wetShade, w.rain, 1f));

            if (airLook)
            {
                // clear near, then the far islands fade into the sky in layers (exp squared). Rain closes it in: aiming far
                // is easy on a clear day and hard in the rain
                float half = Mathf.Lerp(hazeHalfClear, hazeHalfRain, w.rain) * (t.Night ? 0.8f : 1f);
                float k = 0.8326f / Mathf.Max(half, 1f);    // exp(-(half k)^2) = 0.5
                RenderSettings.fogMode = FogMode.ExponentialSquared;
                RenderSettings.fogDensity = k;
                // far land never melts away quite completely on a clear day: it stays a pale silhouette, in layers
                Shader.SetGlobalVector(AirId, new Vector4(k, Mathf.Lerp(farLand, 0.985f, Mathf.Max(w.rain, overcast * 0.6f)), 0f, 1f));
                Shader.SetGlobalColor(AirColorId, skyCol);
                Shader.SetGlobalVector(ShadeTintId, new Vector4(shade.r, shade.g, shade.b, 1f));

                // low mist in the valleys: mornings and after rain, thick by the sea, thin up the mountain
                float mist = valleyMist ? MistAmount(t, overcast) : 0f;
                Shader.SetGlobalVector(MistId, new Vector4(mistDensity * mist, 1f / Mathf.Max(mistHeight, 0.5f), 0f, mist > 0.001f ? 1f : 0f));
                Shader.SetGlobalColor(MistColorId, Color.Lerp(skyCol, new Color(0.93f, 0.93f, 0.95f) * Mathf.Lerp(0.25f, 1f, day), 0.45f));

                // the summit glows pink at dawn and dusk (only the high ground catches the low sun)
                float sunUp = -t.sunDirection.y;
                float glow = dawnGlow ? Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(-0.04f, 0.02f, sunUp)) * (1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.07f, 0.2f, sunUp))) * (1f - overcast) : 0f;
                Color glowC = new Color(1f, 0.42f, 0.36f) * (0.8f * glow);
                Shader.SetGlobalVector(GlowId, new Vector4(glowC.r, glowC.g, glowC.b, glowHeight.x));
                Shader.SetGlobalVector(GlowRangeId, new Vector4(glow > 0.001f ? Mathf.Max(glowHeight.y, 1f) : 0f, 0f, 0f, 0f));
            }
            else
            {
                RenderSettings.fogMode = FogMode.Exponential;
                RenderSettings.fogDensity = (Mathf.Lerp(0.004f, 0.035f, w.rain) + (t.Night ? 0.006f : 0f)) * fogScale;
                Shader.SetGlobalVector(AirId, Vector4.zero);
                Shader.SetGlobalVector(ShadeTintId, Vector4.zero);
                Shader.SetGlobalVector(MistId, Vector4.zero);
                Shader.SetGlobalVector(GlowRangeId, Vector4.zero);
            }
        }

        /// <summary>How wet the ground is right now in sun vs shade (0..1), matching SlugMoisture's after-rain drying times.</summary>
        void WetAfterRainAmount(out float wetSun, out float wetShade)
        {
            float nowRain = Weather.rain;
            if (nowRain > 0.25f)
            {
                wetSun = wetShade = Mathf.Clamp01(nowRain * 1.4f);
                return;
            }
            // Look back up to 80 real minutes (soil in shade stays wet for 20 * 4 = 80 min; in sun ~15-20 min)
            double now = ShownHours;
            Vector3 obs = ObserverPosition;
            float minSince = float.PositiveInfinity;
            for (float m = 0f; m <= 80f; m += 2f)
                if (WeatherAt(now - m / 60.0, obs).rain > 0.05f) { minSince = m; break; }

            if (float.IsPositiveInfinity(minSince))
            {
                wetSun = wetShade = Mathf.Clamp01(nowRain * 1.4f);
                return;
            }
            wetSun = Mathf.Max(nowRain * 1.4f, 1f - Mathf.SmoothStep(4f, 18f, minSince));
            wetShade = Mathf.Max(nowRain * 1.4f, 1f - Mathf.SmoothStep(16f, 72f, minSince));
        }

        Material SkyMaterial()
        {
            if (!paintedSky) return null;
            if (!skyMaterial)
            {
                var sh = Shader.Find("Slug/PaintedSky");
                if (!sh) return null;
                skyMaterial = new Material(sh) { name = "PaintedSky (made at run)", hideFlags = HideFlags.DontSave };
            }
            return skyMaterial;
        }

        /// <summary>The painted sky's colours and clouds for this moment (top deeper than the air, warm round the sun).</summary>
        void PaintSky(Material m, IslandTime t, IslandWeatherState w, Color horizon, float day, float overcast)
        {
            Color top = Color.Lerp(horizon * 0.55f, new Color(0.24f, 0.44f, 0.78f), 0.6f * day * (1f - overcast));
            top = Color.Lerp(top, horizon * 0.85f, overcast * 0.7f);
            Vector3 toSun = -t.sunDirection;
            Color sunC = sun ? sun.color * Mathf.Lerp(0.2f, 1f, day) * (1f - 0.8f * overcast) : Color.white;
            Color lit = Color.Lerp(new Color(1f, 0.98f, 0.94f), new Color(1f, 0.78f, 0.62f), t.twilight * (1f - overcast));
            lit = Color.Lerp(horizon * 0.6f, lit, Mathf.Lerp(0.15f, 1f, day));
            lit = Color.Lerp(lit, new Color(0.72f, 0.74f, 0.77f) * Mathf.Lerp(0.3f, 1f, day), overcast * 0.8f);
            Color shadeC = Color.Lerp(horizon, top, 0.35f) * new Color(0.86f, 0.9f, 1.02f) * 0.92f;
            m.SetColor("_SkyTop", top);
            m.SetColor("_SkyHorizon", horizon);
            m.SetVector("_SunDir", toSun);
            m.SetColor("_SunColor", sunC * (1f - overcast));
            m.SetColor("_CloudLit", lit);
            m.SetColor("_CloudShade", shadeC);
            m.SetFloat("_Cover", Mathf.Clamp01(0.15f + 0.85f * Mathf.Max(w.cloud, w.rain)));
            m.SetFloat("_Drift", (float)(ShownHours % 1000.0) * 60f * Mathf.Lerp(0.7f, 1.6f, Wind.strength));
        }

        /// <summary>How much low mist lies in the valleys now (0..1): early morning, and for a while after rain.</summary>
        float MistAmount(IslandTime t, float overcast)
        {
            float h = t.GameHour, rise = t.sunrise * 24f;
            float morning = Mathf.Clamp01(1f - Mathf.InverseLerp(rise + 0.5f, rise + 3f, h)) * Mathf.Clamp01(Mathf.InverseLerp(rise - 3f, rise - 1f, h));
            float wet = 0f;
            double gameHour = settings.dayHours / 24.0;
            for (int k = 1; k <= 4; k++) wet = Mathf.Max(wet, WeatherAt(ShownHours - k * 0.5 * gameHour).rain * (1f - 0.18f * k));
            wet *= 1f - Weather.rain;                     // while it pours, the rain itself closes the air
            return Mathf.Clamp01(Mathf.Max(morning, wet * 0.8f)) * (1f - 0.3f * overcast);
        }

        void BuildRain()
        {
            if (_rain) return;
            var existing = transform.Find("Rain");
            if (existing)
            {
                _rain = existing.GetComponent<ParticleSystem>();
                if (_rain) return;
            }
            var go = new GameObject("Rain");
            go.transform.SetParent(transform, false);
            _rain = go.AddComponent<ParticleSystem>();
            _rain.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = _rain.main;
            main.loop = true;
            main.startLifetime = 1.1f;
            main.startSpeed = 16f;
            main.startSize = 0.012f;
            main.maxParticles = 8000;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startColor = new Color(0.75f, 0.82f, 0.9f, 0.35f);
            var shape = _rain.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(28f, 28f, 0.1f);
            shape.rotation = new Vector3(90f, 0f, 0f);       // emit straight down
            var vel = _rain.velocityOverLifetime;
            vel.enabled = true;
            vel.space = ParticleSystemSimulationSpace.World;
            var emission = _rain.emission;
            emission.rateOverTime = 0f;
            var r = go.GetComponent<ParticleSystemRenderer>();
            r.renderMode = ParticleSystemRenderMode.Stretch;
            r.lengthScale = 2.2f;
            r.velocityScale = 0.02f;
            var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (shader)
            {
                var m = new Material(shader) { hideFlags = HideFlags.DontSave };
                m.SetFloat("_Surface", 1f);
                m.SetColor("_BaseColor", new Color(0.8f, 0.86f, 0.95f, 0.5f));
                m.SetOverrideTag("RenderType", "Transparent");
                m.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                m.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                m.SetInt("_ZWrite", 0);
                m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                m.renderQueue = 3000;
                r.sharedMaterial = m;
            }
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _rain.Play();
        }

        void ApplyRain()
        {
            if (!_rain) return;
            var around = rainAround ? rainAround : (Camera.main ? Camera.main.transform : null);
            // Slant rain with the wind (wind_ideas.md #9) and shift the emitter upwind so slanted rain lands around the eyes
            float gustHere = around ? IslandWind.GustWaveAt(ShownHours, around.position, Wind) : Wind.gust;
            Vector3 windPush = new Vector3(Wind.direction.x, 0f, Wind.direction.y) * (Wind.Speed * (0.65f + 0.35f * gustHere));
            if (around) _rain.transform.position = around.position + Vector3.up * 11f - windPush * 0.65f;
            var vel = _rain.velocityOverLifetime;
            vel.x = windPush.x;
            vel.y = 0f;
            vel.z = windPush.z;
            var emission = _rain.emission;
            emission.rateOverTime = Weather.rain * 4500f;
        }

        void OnGUI()
        {
            if (!_dev) return;
            if (_style == null) _style = new GUIStyle(GUI.skin.label) { fontSize = 15, richText = true, normal = { textColor = Color.white } };
            var t = Time;
            var w = Weather;
            int h = Mathf.FloorToInt(t.GameHour), m = Mathf.FloorToInt((t.GameHour - h) * 60f);
            string sky = SkyName(w.sky);
            var sb = new System.Text.StringBuilder();
            sb.Append($"<b>島の時計（開発用）</b>　{t.day} 日目 {h:00}:{m:00}　{IslandClock.SeasonName(t.season)} {t.seasonProgress * 100f:0}%（乾き {t.dryness:0.00}）\n");
            sb.Append($"日の出 {t.sunrise * 24f:0.0}時 / 日の入り {t.sunset * 24f:0.0}時　昼 {t.daylightAmount:0.00}　朝夕 {t.twilight:0.00}\n");
            sb.Append($"月 {t.moonPhase:0.00}（明るさ {t.moonLight:0.00}）　潮 {t.tide:+0.00;-0.00}\n");
            sb.Append($"空：{sky}　雨 {w.rain:0.00}　雲 {w.cloud:0.00}　風：{IslandWind.DirectionName(Wind.direction)} {Wind.Speed:0.1} m/s（雨雲 {_clouds.Count}）{(_devSky != IslandSettings.DevSky.Auto || settings.devSky != IslandSettings.DevSky.Auto ? " <color=#ffb080>固定</color>" : "")}{(_fast ? " <color=#ffb080>早回し</color>" : "")}\n");
            sb.Append("予報（1時間ごと）：");
            foreach (var f in IslandWeather.Forecast(settings, region, ObserverPosition, ShownHours, 8f, 1f)) sb.Append(SkyMark(f.sky));
            sb.Append("\n[ ] 1時間ずらす　\\ 天気を固定　/ 早回し");
            GUI.Box(new Rect(Screen.width - 520, 60, 510, 150), GUIContent.none);
            GUI.Label(new Rect(Screen.width - 510, 64, 500, 145), sb.ToString(), _style);
        }

        public static string SkyName(Sky s) => s == Sky.Sunny ? "晴れ" : s == Sky.Cloudy ? "曇り" : s == Sky.LightRain ? "小雨" : "大雨";
        static string SkyMark(Sky s) => s == Sky.Sunny ? "晴" : s == Sky.Cloudy ? "曇" : s == Sky.LightRain ? "雨" : "<color=#80c0ff>雨</color>";
    }
}
