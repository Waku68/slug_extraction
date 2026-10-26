using UnityEngine;
using UnityEngine.InputSystem;

namespace SlugWorld
{
    /// <summary>
    /// Shows the island's clock and weather in a region (<see cref="IslandClock"/>, <see cref="IslandWeather"/>): the sun
    /// moves (east to west, longer days in the dry season), dawn and dusk are short and warm, the night is lit by the moon
    /// (bright enough to harvest by), clouds dull the light, rain falls around the eyes and the air thickens with it.
    /// Others read the moment from <see cref="Current"/> (the slug's moisture, for one).
    /// F3 shows the clock and the forecast for development (the island itself shows no numbers):
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
        [Tooltip("霧の濃さの倍率。島全体を見渡す場面（IslandTest）では薄くする")] public float fogScale = 1f;

        /// <summary>The presenter of the open scene (one per region).</summary>
        public static WeatherPresenter Current { get; private set; }
        /// <summary>The development view (F3) is open: others may show their values too.</summary>
        public static bool DevVisible => Current && Current._dev;
        /// <summary>Real hours since the epoch, as this scene shows them (the developer can move it).</summary>
        public double Hours { get; private set; }
        public IslandTime Time { get; private set; }
        public IslandWeatherState Weather { get; private set; }
        /// <summary>The colour of the sky (and the far fog) right now. Cameras that clear to the sky use it.</summary>
        public Color SkyColor { get; private set; }

        double _offsetHours;
        IslandSettings.DevSky _devSky;      // a sky fixed in this scene only (the settings asset is left alone)
        bool _dev, _fast;
        ParticleSystem _rain;
        GUIStyle _style;

        void Awake() => Hours = IslandClock.RealHoursNow(settings);   // before anyone's Start reads the time
        void OnEnable() => Current = this;
        void OnDisable() { if (Current == this) Current = null; }

        void Start()
        {
            if (!sun)
                foreach (var l in FindObjectsByType<Light>())
                    if (l.type == LightType.Directional) { sun = l; break; }
            // the sky is a plain colour set from the time and the weather (the built-in sky turns orange at the horizon)
            RenderSettings.skybox = null;
            if (sun) RenderSettings.sun = sun;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            BuildRain();
            if (!GetComponent<SunShafts>()) gameObject.AddComponent<SunShafts>();
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
            double shown = Hours + _offsetHours + settings.devOffsetGameHours / 24.0 * settings.dayHours;
            Time = IslandClock.At(settings, shown);
            Weather = IslandWeather.At(settings, region, shown, _devSky);
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

        /// <summary>The weather this scene shows at any moment (with the sky fixed for development, if it is).</summary>
        public IslandWeatherState WeatherAt(double hours) => IslandWeather.At(settings, region, hours, _devSky);

        void ApplyLight()
        {
            var t = Time;
            var w = Weather;
            float day = t.daylightAmount;
            float overcast = Mathf.Max(w.cloud * 0.7f, w.rain);

            // sun by day, moon by night (the moon stays high in the south so the night still has shape)
            if (sun)
            {
                Quaternion sunRot = Quaternion.LookRotation(t.sunDirection.y < -0.05f ? t.sunDirection : new Vector3(t.sunDirection.x, Mathf.Min(t.sunDirection.y, -0.05f), t.sunDirection.z));
                Quaternion moonRot = Quaternion.Euler(55f, 160f + 40f * t.moonPhase, 0f);
                sun.transform.rotation = Quaternion.Slerp(moonRot, sunRot, Mathf.SmoothStep(0f, 1f, day));
                float nightI = Mathf.Lerp(nightIntensity.x, nightIntensity.y, t.moonLight);
                float dayI = dayIntensity * (1f - 0.45f * overcast);
                sun.intensity = Mathf.Lerp(nightI, dayI, day);
                Color dayC = Color.Lerp(new Color(1f, 0.97f, 0.9f), new Color(0.82f, 0.86f, 0.92f), overcast);
                Color duskC = new Color(1f, 0.72f, 0.5f);
                Color nightC = new Color(0.55f, 0.66f, 1f);
                Color c = Color.Lerp(nightC, dayC, day);
                sun.color = Color.Lerp(c, duskC, 0.75f * t.twilight * (1f - overcast * 0.7f));
                sun.shadowStrength = Mathf.Lerp(0.5f, 1f - 0.6f * overcast, day);
            }

            // the air: ambient light, the sky, the fog
            Color skyDay = Color.Lerp(new Color(0.55f, 0.68f, 0.85f), new Color(0.55f, 0.58f, 0.62f), overcast);
            Color skyNight = new Color(0.12f, 0.15f, 0.26f) * Mathf.Lerp(0.8f, 1.3f, t.moonLight);
            Color sky = Color.Lerp(skyNight, skyDay, day);
            sky = Color.Lerp(sky, new Color(0.85f, 0.6f, 0.5f), t.twilight * 0.25f);
            RenderSettings.ambientSkyColor = sky;
            RenderSettings.ambientEquatorColor = sky * 0.8f;
            RenderSettings.ambientGroundColor = sky * 0.45f;
            Color clearDay = new Color(0.66f, 0.78f, 0.92f), greyDay = new Color(0.6f, 0.63f, 0.66f);
            Color skyCol = Color.Lerp(new Color(0.05f, 0.07f, 0.14f) * Mathf.Lerp(0.8f, 1.4f, t.moonLight), Color.Lerp(clearDay, greyDay, overcast), day);
            skyCol = Color.Lerp(skyCol, new Color(0.95f, 0.7f, 0.55f), t.twilight * 0.45f * (1f - overcast * 0.6f));
            SkyColor = skyCol;
            foreach (var c in Camera.allCameras)
            {
                if (c.targetTexture != null) continue;        // (the slug's own portrait clears to nothing)
                c.clearFlags = CameraClearFlags.SolidColor;
                c.backgroundColor = skyCol;
            }
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Exponential;
            RenderSettings.fogColor = skyCol;
            RenderSettings.fogDensity = (Mathf.Lerp(0.004f, 0.035f, w.rain) + (t.Night ? 0.006f : 0f)) * fogScale;
        }

        void BuildRain()
        {
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
            if (around) _rain.transform.position = around.position + Vector3.up * 11f;
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
            sb.Append($"<b>島の時計（開発用）</b>　{t.day} 日目 {h:00}:{m:00}　{(t.DrySeason ? "乾季" : "雨季")}（乾き {t.dryness:0.00}）\n");
            sb.Append($"日の出 {t.sunrise * 24f:0.0}時 / 日の入り {t.sunset * 24f:0.0}時　昼 {t.daylightAmount:0.00}　朝夕 {t.twilight:0.00}\n");
            sb.Append($"月 {t.moonPhase:0.00}（明るさ {t.moonLight:0.00}）　潮 {t.tide:+0.00;-0.00}\n");
            sb.Append($"空：{sky}　雨 {w.rain:0.00}　雲 {w.cloud:0.00}　{(_devSky != IslandSettings.DevSky.Auto || settings.devSky != IslandSettings.DevSky.Auto ? "<color=#ffb080>固定</color>" : "")}{(_fast ? "　<color=#ffb080>早回し</color>" : "")}\n");
            sb.Append("予報（1時間ごと）：");
            foreach (var f in IslandWeather.Forecast(settings, region, ShownHours, 8f, 1f)) sb.Append(SkyMark(f.sky));
            sb.Append("\n[ ] 1時間ずらす　\\ 天気を固定　/ 早回し");
            GUI.Box(new Rect(Screen.width - 520, 60, 510, 150), GUIContent.none);
            GUI.Label(new Rect(Screen.width - 510, 64, 500, 145), sb.ToString(), _style);
        }

        public static string SkyName(Sky s) => s == Sky.Sunny ? "晴れ" : s == Sky.Cloudy ? "曇り" : s == Sky.LightRain ? "小雨" : "大雨";
        static string SkyMark(Sky s) => s == Sky.Sunny ? "晴" : s == Sky.Cloudy ? "曇" : s == Sky.LightRain ? "雨" : "<color=#80c0ff>雨</color>";
    }
}
