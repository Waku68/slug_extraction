using SlugDesign;
using SlugMove;
using SlugWorld;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SlugExtract
{
    /// <summary>
    /// The slug's moisture (docs/discussions/moisture_discussion.md). How fast it dries is set by the ground underfoot
    /// (spec 4章の表, <see cref="GroundKinds"/>): soil and grass slowly, sand and black lava faster, salt hardest (even at
    /// night), wet shining ground not at all; shade and cloud slow it, the night's dew holds it. Rain under the open sky wets
    /// it, the open ground stays wet a while after (sand dries first, the shade last), and touching water (a puddle, a
    /// stream) soaks it in seconds, even past full. While it is moist enough, health comes back very
    /// slowly and uses a little of it; at nothing the slug crawls slower and its health drains away until it falls.
    /// Two moss balls of water ride on the belt (F: two seconds, the tools wait); touching water fills them again.
    /// No numbers: the skin dulls and shrinks as it dries, the screen's edges blur white when it is nearly gone,
    /// and the moss balls show as two drops under the slug in the corner. F3 shows the values for development.
    /// </summary>
    public class SlugMoisture : MonoBehaviour
    {
        public ConditionSettings settings;
        public SlugMoveController controller;
        public SlugCameraRig cameraRig;
        public SlugBodyBase slug;
        public SlugHealth health;
        [Tooltip("左上の自分（苔玉をその下に出す）")] public SelfPortrait portrait;
        /// <summary>Holds the moss-ball key without the keyboard (tests and tools).</summary>
        [System.NonSerialized] public bool forceUseMoss;

        public float Value { get; private set; }
        public int MossBalls { get; private set; }
        /// <summary>0..1 through using a moss ball (0 when not using one).</summary>
        public float Using => _use > 0f ? Mathf.Clamp01(_use / settings.mossUseSeconds) : 0f;
        /// <summary>Why it changes right now (for development).</summary>
        public string Reason { get; private set; } = "";
        public float RatePerMinute { get; private set; }
        /// <summary>The sun is on the slug (a sunny day, out of the shade).</summary>
        public bool InSun { get; private set; }
        /// <summary>0..1: how hard the sun dries the slug right now (soil 0.3, sand 0.5, salt 1; 0 on wet ground, in the shade,
        /// under cloud, at dawn and dusk, at night).</summary>
        public float SunDrying { get; private set; }
        /// <summary>The ground underfoot (<see cref="GroundKinds"/>), before the rain's wet is counted.</summary>
        public GroundKind Ground { get; private set; }

        /// <summary>The four looks of moisture (spec 4章 Oct 18): 0 glossy (past full), 1 plain, 2 drying (dull, shrunk,
        /// blotched), 3 in danger (wrinkled, the view's edges blur).</summary>
        public int Stage { get; private set; } = 1;
        /// <summary>Once, the moment the stage changes (new stage, old stage): a sound and the tentacles go with it.</summary>
        public event System.Action<int, int> StageChanged;
        /// <summary>Once, the moment the ground starts drying the slug hard (out into the sun, onto salt): "ジュッ".</summary>
        public event System.Action DryingStarted;
        /// <summary>Once, the moment it gets into the wet (water, rain under the open sky): "ぬちゃっ".</summary>
        public event System.Action WettingStarted;

        bool _inSun, _underSky, _wasDrying, _wasWetting;
        float _probe, _use, _refill, _rainCheck, _minutesSinceRain = float.PositiveInfinity;
        Texture2D _edge, _drop;
        GUIStyle _dev;

        void Start()
        {
            Value = settings.moistureMax;
            MossBalls = settings.mossBalls;
            // the light on the player's own view (SunGlare) is no longer added (Nov 3 わくさん: it got in the way of seeing);
            // the sun shows only on the island (SunShafts)
        }

        void Update()
        {
            var weather = WeatherPresenter.Current;
            if (!weather || !controller || (health && health.Dead)) return;
            var s = settings;
            float dt = Time.deltaTime;
            var t = weather.Time;
            Vector3 pos = controller.transform.position;
            var sky = weather.WeatherAt(weather.ShownHours, pos).sky;

            // is the sun on the back, is the sky overhead? (a few times a second is plenty: shadows move slowly)
            _probe -= dt;
            if (_probe <= 0f)
            {
                _probe = 0.2f;
                Vector3 from = pos + Vector3.up * 0.7f;
                int mask = SlugHitboxes.WorldMask;
                _inSun = t.daylightAmount > 0.05f && !Physics.Raycast(from, -t.sunDirection, 400f, mask, QueryTriggerInteraction.Ignore);
                _underSky = !Physics.Raycast(from, Vector3.up, 80f, mask, QueryTriggerInteraction.Ignore);
                Ground = GroundKinds.At(pos);
            }
            // when it last rained here (every couple of seconds is plenty: the ground dries in minutes)
            _rainCheck -= dt;
            if (_rainCheck <= 0f) { _rainCheck = 2f; _minutesSinceRain = MinutesSinceRain(weather, pos); }
            bool salt = Ground == GroundKind.Salt;
            bool water = MoistureZone.Any(MoistureZone.Kind.Water, pos);
            bool raining = (sky == Sky.LightRain || sky == Sky.HeavyRain) && _underSky;

            // how much it changes a minute here, now: by the ground underfoot (spec 4章の表; cloud is the same as shade)
            float perMinute;
            if (raining)
            {
                perMinute = (sky == Sky.HeavyRain ? s.heavyRain : s.lightRain) * (salt ? s.saltRainMultiplier : 1f);
                Reason = sky == Sky.HeavyRain ? "大雨" : "小雨";
            }
            else
            {
                bool sunny = sky == Sky.Sunny && _inSun;
                var ground = Ground;
                // just after rain the open ground still shines wet and holds the body: sand dries first, the shade last
                bool afterRain = false;
                if (_underSky && (ground == GroundKind.Soil || ground == GroundKind.Sand))
                {
                    float wetFor = (ground == GroundKind.Sand ? s.wetAfterRainSand : s.wetAfterRainSoil) * (sunny ? 1f : s.wetShadeMultiplier);
                    if (_minutesSinceRain < wetFor) { ground = GroundKind.Wet; afterRain = true; }
                }
                float inSun, inShade, atNight;
                switch (ground)
                {
                    case GroundKind.Wet: inSun = inShade = atNight = s.wetGround; break;
                    case GroundKind.Salt: inSun = s.saltInSun; inShade = s.saltInShade; atNight = s.saltNight; break;
                    case GroundKind.Sand: inSun = s.sandInSun; inShade = s.sandInShade; atNight = s.night; break;
                    default: inSun = s.soilInSun; inShade = s.soilInShade; atNight = s.night; break;
                }
                perMinute = Mathf.Lerp(atNight, sunny ? inSun : inShade, t.daylightAmount);
                Reason = (t.Night ? "夜" : sky == Sky.Sunny ? (_inSun ? "晴れ・日なた" : "晴れ・日陰") : "曇り")
                         + "・" + GroundKinds.Name(ground) + (afterRain ? "（雨上がり）" : "");
            }
            float gain = perMinute / 60f * dt;
            if (water)
            {
                gain = s.waterPerSecond * dt;
                Reason = "水に触れている";
                _refill += dt;
                if (_refill >= s.mossRefillSeconds) MossBalls = s.mossBalls;
            }
            else _refill = 0f;
            RatePerMinute = gain / Mathf.Max(dt, 1e-5f) * 60f;

            // the sun's share of it: only a sunny day out of the shade (cloud counts as shade), not at dawn and dusk
            InSun = !raining && !water && sky == Sky.Sunny && _inSun && t.daylightAmount > 0.5f;
            float saltRate = Mathf.Max(-s.saltInSun, 1e-3f);
            SunDrying = InSun ? Mathf.Clamp01(-perMinute / saltRate) * (1f - t.twilight) : 0f;

            // the moment it changes, said once (spec 4章 Oct 10): out into the sun or onto salt, into the wet
            bool drying = SunDrying > 0f || (salt && !water && !raining);
            bool wetting = water || raining;
            if (drying && !_wasDrying) DryingStarted?.Invoke();
            if (wetting && !_wasWetting) WettingStarted?.Invoke();
            _wasDrying = drying;
            _wasWetting = wetting;

            // rain and water can soak it past full; the extra seeps away again
            bool soaking = water || raining;
            float cap = soaking ? s.moistureOverfill : Mathf.Max(s.moistureMax, Value);
            float v = Value + gain;
            if (!soaking && v > s.moistureMax) v = Mathf.Max(s.moistureMax, v - s.overfillDrain / 60f * dt);
            Value = Mathf.Clamp(v, 0f, cap);

            // health: back little by little while moist enough (using a little of it); away while dried out
            if (health)
            {
                if (Value >= s.regenAbove && health.Current < health.Max)
                {
                    health.Heal(health.Max / Mathf.Max(s.regenSecondsToFull, 1f) * dt);
                    Value = Mathf.Max(0f, Value - s.regenMoistureCost / 60f * dt);
                }
                if (Value <= 0f) health.TakeDamage(health.Max / Mathf.Max(s.zeroSecondsToFall, 1f) * dt, "dried");
            }

            UseMoss(dt);

            // the body shows it
            float dry = Mathf.InverseLerp(s.looksDryBelow, 0f, Value);
            float soaked = Mathf.InverseLerp(s.moistureMax, s.moistureOverfill, Value);
            if (slug) slug.SetDryness(dry, soaked);
            controller.bodyScale = 1f - 0.06f * dry;
            controller.conditionSpeed = Value <= 0f ? s.zeroSpeed : 1f;

            // the four stages (a little give at each line, so standing on it does not flicker)
            int stage = StageFor(Value, Stage);
            if (stage != Stage) { int old = Stage; Stage = stage; StageChanged?.Invoke(stage, old); }
        }

        /// <summary>Real minutes since it last rained over this spot (infinity when longer ago than the ground stays wet).
        /// The weather is worked out from the time and place alone, so the past is just read back.</summary>
        float MinutesSinceRain(WeatherPresenter weather, Vector3 pos)
        {
            var s = settings;
            float longest = Mathf.Max(s.wetAfterRainSand, s.wetAfterRainSoil) * Mathf.Max(1f, s.wetShadeMultiplier);
            double now = weather.ShownHours;
            for (float m = 0f; m <= longest; m += 0.5f)
                if (weather.WeatherAt(now - m / 60.0, pos).rain > 0.05f) return m;
            return float.PositiveInfinity;
        }

        int StageFor(float v, int current)
        {
            var s = settings;
            const float give = 1.5f;
            // glossy | plain | drying | danger: the stage is how many of these lines it is under
            float line0 = s.moistureMax + give, line1 = s.looksDryBelow, line2 = s.warnBelow;
            int stage = 0;
            // past a line downward needs a little under it, back upward a little over it
            if (v < line0 + (current > 0 ? give : -give) * 0.5f) stage = 1;
            if (v < line1 + (current > 1 ? give : -give)) stage = 2;
            if (v < line2 + (current > 2 ? give : -give)) stage = 3;
            return stage;
        }

        /// <summary>A moss ball found lying about goes onto the belt when there is room (spec 4章 Oct 18). False when full.</summary>
        public bool AddMossBall()
        {
            if (MossBalls >= settings.mossBalls) return false;
            MossBalls++;
            return true;
        }

        void UseMoss(float dt)
        {
            var s = settings;
            var kb = Keyboard.current;
            bool press = forceUseMoss || (kb != null && kb.fKey.wasPressedThisFrame && Cursor.lockState == CursorLockMode.Locked && controller.inputEnabled);
            if (_use <= 0f && press && MossBalls > 0 && Value < s.moistureMax) _use = 1e-4f;
            if (_use > 0f)
            {
                _use += dt;
                if (_use >= s.mossUseSeconds)
                {
                    MossBalls--;
                    Value = Mathf.Min(s.moistureMax, Value + s.mossAmount);
                    _use = 0f;
                }
            }
            if (cameraRig) cameraRig.beltBusy = _use > 0f;
        }

        void OnGUI()
        {
            if (!settings) return;
            if (_edge == null) BuildTextures();

            // nearly dried out: the edges of the view blur white (stronger, and throbbing, at nothing)
            if (Value < settings.warnBelow && !(health && health.Dead))
            {
                float k = Mathf.InverseLerp(settings.warnBelow, 0f, Value);
                if (Value <= 0f) k = 0.75f + 0.25f * Mathf.Sin(Time.time * 4f);
                var old = GUI.color;
                GUI.color = new Color(0.95f, 0.95f, 0.9f, 0.75f * k);
                GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), _edge, ScaleMode.StretchToFill, true);
                GUI.color = old;
            }

            // the moss balls on the belt: drops under the slug in the corner (filling while one is being used)
            Rect area = portrait && portrait.Shown ? portrait.Area : new Rect(10f, 10f, 150f, 150f);
            for (int i = 0; i < settings.mossBalls; i++)
            {
                var r = new Rect(area.x + 8f + i * 26f, area.yMax + 4f, 20f, 20f);
                bool full = i < MossBalls;
                var old = GUI.color;
                GUI.color = full ? new Color(0.45f, 0.78f, 1f, 0.95f) : new Color(1f, 1f, 1f, 0.25f);
                if (full && i == MossBalls - 1 && _use > 0f) GUI.color = Color.Lerp(GUI.color, new Color(1f, 1f, 1f, 0.3f), Using);
                GUI.DrawTexture(r, _drop, ScaleMode.StretchToFill, true);
                GUI.color = old;
            }

            // development only: the values
            if (WeatherPresenter.DevVisible)
            {
                if (_dev == null) _dev = new GUIStyle(GUI.skin.label) { fontSize = 15, richText = true, normal = { textColor = Color.white } };
                string hp = health ? $"{health.Current:0}/{health.Max:0}" : "—";
                GUI.Box(new Rect(Screen.width - 520, 214, 510, 50), GUIContent.none);
                GUI.Label(new Rect(Screen.width - 510, 218, 500, 46),
                    $"湿り気 {Value:0.0}（{RatePerMinute:+0;-0}/分、{Reason}）　体力 {hp}\n苔玉 {MossBalls}　{(_inSun ? "日なた" : "日陰")}　{(_underSky ? "空の下" : "屋根の下")}", _dev);
            }
        }

        void BuildTextures()
        {
            const int n = 64;
            _edge = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, hideFlags = HideFlags.DontSave };
            _drop = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, hideFlags = HideFlags.DontSave };
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float u = (x + 0.5f) / n * 2f - 1f, v = (y + 0.5f) / n * 2f - 1f;
                    float d = Mathf.Sqrt(u * u + v * v);
                    _edge.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.55f, 1.25f, d))));
                    // a drop: round below, pointed on top
                    float dy = v + 0.25f;
                    float r = 0.62f * Mathf.Clamp01(1f - Mathf.Max(0f, v - 0.0f) * 0.9f);
                    bool inside = u * u + dy * dy * (v > 0f ? 0.6f : 1f) <= r * r || (v > 0f && Mathf.Abs(u) < 0.6f * (1f - v) && v < 0.95f);
                    _drop.SetPixel(x, y, new Color(1f, 1f, 1f, inside ? 1f : 0f));
                }
            _edge.Apply();
            _drop.Apply();
        }
    }
}
