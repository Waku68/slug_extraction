using SlugMove;
using SlugWorld;
using UnityEngine;

namespace SlugExtract
{
    /// <summary>
    /// The sun on the slug, on the player's own view (Oct 26 わくさん: in the sun the slug dries, and drying kills, so say it
    /// loudly). While the slug is in the sun on a sunny day, streaks of light pour in from the side of the screen the sun is
    /// on (from the sun itself when it is in view), the way the salt grains come in from the side a round came from.
    /// How strong they are is how fast the sun dries the slug there (<see cref="SlugMoisture.SunDrying"/>: soil half, salt
    /// full); in the shade, under cloud, at dawn and dusk and at night there are none, so a streak always means danger.
    /// Stepping out of the shade flares them once. The streaks fade before the middle so the crosshair stays clear.
    /// Added to the player by <see cref="SlugMoisture"/>. The island's own shafts of light (seen by everyone) are
    /// <see cref="SlugWorld.SunShafts"/>.
    /// </summary>
    public class SunGlare : MonoBehaviour
    {
        public SlugMoisture moisture;
        [Tooltip("光の筋の本数")] public int streaks = 14;
        [Tooltip("いちばん強いとき（塩の上）の濃さ")] public float maxAlpha = 0.7f;
        [Tooltip("いちばん弱いとき（土・草の日なた）でも、これだけは出す（日なたにいることは必ず分かる）")] public float minShare = 0.45f;
        [Tooltip("筋の長さ（画面の高さの割合）：短い筋・長い筋")] public Vector2 length = new Vector2(0.45f, 0.95f);
        [Tooltip("筋の太さ（画面の高さの割合）：細い筋・太い筋")] public Vector2 width = new Vector2(0.03f, 0.14f);
        [Tooltip("筋の広がる角度（度）")] public float spreadDegrees = 70f;
        [Tooltip("光の色")] public Color color = new Color(1f, 0.93f, 0.68f, 1f);
        [Tooltip("日なたに出た瞬間に強く光る時間（秒）")] public float flareTime = 0.6f;
        [Tooltip("出る・消える速さ（秒）")] public float fadeIn = 0.25f, fadeOut = 0.5f;

        SlugCameraRig _rig;
        Texture2D _streak, _glow;
        float _shown, _flare;
        bool _wasInSun;
        float[] _seed;

        void Awake()
        {
            if (!moisture) moisture = GetComponent<SlugMoisture>();
            _rig = moisture && moisture.cameraRig ? moisture.cameraRig : GetComponentInChildren<SlugCameraRig>(true);
        }

        void OnDestroy()
        {
            if (_streak) Destroy(_streak);
            if (_glow) Destroy(_glow);
        }

        bool Own => _rig && _rig.cam && !_rig.driven && _rig.cam.isActiveAndEnabled;

        void Update()
        {
            if (!moisture) return;
            float target = moisture.SunDrying > 0f ? Mathf.Lerp(minShare, 1f, Mathf.InverseLerp(0.5f, 1f, moisture.SunDrying)) : 0f;
            float speed = 1f / Mathf.Max(target > _shown ? fadeIn : fadeOut, 1e-3f);
            _shown = Mathf.MoveTowards(_shown, target, speed * Time.deltaTime);
            // out of the shade: one flare, so the moment it starts is caught even without looking at anything
            if (moisture.InSun && !_wasInSun) _flare = flareTime;
            _wasInSun = moisture.InSun;
            _flare = Mathf.Max(0f, _flare - Time.deltaTime);
        }

        /// <summary>Where the sun is on the screen (0..1, y up) and the way into the screen from there.</summary>
        bool SunOnScreen(out Vector2 at, out Vector2 inward)
        {
            at = inward = Vector2.zero;
            var weather = WeatherPresenter.Current;
            if (!weather) return false;
            var cam = _rig.cam;
            Vector3 toSun = -weather.Time.sunDirection;
            Vector3 local = cam.transform.InverseTransformDirection(toSun);
            if (local.z > 0.05f)
            {
                Vector3 vp = cam.WorldToViewportPoint(cam.transform.position + toSun * 100f);
                if (vp.x > -0.1f && vp.x < 1.1f && vp.y > -0.1f && vp.y < 1.1f)
                {
                    at = new Vector2(vp.x, vp.y);
                    inward = (new Vector2(0.5f, 0.45f) - at);
                    if (inward.sqrMagnitude < 1e-4f) inward = Vector2.down;
                    inward.Normalize();
                    return true;
                }
            }
            // off the screen (or behind): from the edge on its side. Straight behind or above has no side: the top edge
            Vector2 dir = new Vector2(local.x, local.y);
            if (dir.sqrMagnitude < 0.02f) dir = Vector2.up;
            dir.Normalize();
            float aspect = (float)Screen.width / Mathf.Max(Screen.height, 1);
            Vector2 d = new Vector2(dir.x / aspect, dir.y);
            float t = 0.5f / Mathf.Max(Mathf.Abs(d.x), Mathf.Abs(d.y), 1e-4f);
            at = new Vector2(0.5f, 0.5f) + d * t;
            inward = -dir;
            return true;
        }

        void OnGUI()
        {
            if (_shown <= 0.001f || !Own || _rig.ShowsBody || Event.current.type != EventType.Repaint) return;
            if (!SunOnScreen(out var at, out var inward)) return;
            if (_streak == null) BuildTextures();
            float w = Screen.width, h = Screen.height;
            Vector2 origin = new Vector2(at.x * w, (1f - at.y) * h);
            Vector2 inGui = new Vector2(inward.x, -inward.y);
            float baseAngle = Mathf.Atan2(inGui.y, inGui.x) * Mathf.Rad2Deg;
            float flare = flareTime > 0f ? _flare / flareTime : 0f;
            float a = maxAlpha * _shown * (1f + 0.8f * flare);
            var oldColor = GUI.color;
            var oldMatrix = GUI.matrix;

            // a warm wash where the light comes from
            float g = h * (0.55f + 0.25f * flare);
            GUI.color = new Color(color.r, color.g, color.b, a * 0.7f);
            GUI.DrawTexture(new Rect(origin.x - g, origin.y - g, g * 2f, g * 2f), _glow, ScaleMode.StretchToFill, true);

            // the streaks: fanned round the way in, slowly swaying and breathing like light through moving air
            for (int i = 0; i < streaks; i++)
            {
                float k = streaks > 1 ? i / (streaks - 1f) : 0.5f;
                float s0 = _seed[i % _seed.Length];
                float ang = baseAngle + (k - 0.5f) * spreadDegrees + Mathf.Sin(Time.time * 0.35f + s0 * 6.3f) * 3f;
                float len = h * Mathf.Lerp(length.x, length.y, Mathf.Repeat(s0 * 7.13f, 1f)) * (1f + 0.25f * flare);
                float wid = h * Mathf.Lerp(width.x, width.y, Mathf.Repeat(s0 * 3.71f, 1f));
                float breathe = 0.65f + 0.35f * Mathf.Sin(Time.time * (0.6f + s0) + s0 * 11f);
                GUI.matrix = oldMatrix;
                GUIUtility.RotateAroundPivot(ang, origin);
                GUI.color = new Color(color.r, color.g, color.b, a * breathe);
                GUI.DrawTexture(new Rect(origin.x, origin.y - wid * 0.5f, len, wid), _streak, ScaleMode.StretchToFill, true);
            }
            GUI.matrix = oldMatrix;
            GUI.color = oldColor;
        }

        void BuildTextures()
        {
            var rnd = new System.Random(11);
            _seed = new float[16];
            for (int i = 0; i < _seed.Length; i++) _seed[i] = (float)rnd.NextDouble();

            // a streak: bright at the light's end, thinning and fading toward its tip, soft at the sides
            const int L = 128, W = 32;
            _streak = new Texture2D(L, W, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, hideFlags = HideFlags.DontSave };
            for (int y = 0; y < W; y++)
                for (int x = 0; x < L; x++)
                {
                    float u = (x + 0.5f) / L, v = (y + 0.5f) / W * 2f - 1f;
                    float half = Mathf.Lerp(1f, 0.35f, u);
                    float side = Mathf.Clamp01(1f - Mathf.Abs(v) / half);
                    float along = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0f, 0.06f, u)) * Mathf.Pow(1f - u, 1.6f);
                    _streak.SetPixel(x, y, new Color(1f, 1f, 1f, side * side * along));
                }
            _streak.Apply();

            const int n = 64;
            _glow = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, hideFlags = HideFlags.DontSave };
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float u = (x + 0.5f) / n * 2f - 1f, v = (y + 0.5f) / n * 2f - 1f;
                    float d = Mathf.Sqrt(u * u + v * v);
                    _glow.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Pow(Mathf.Clamp01(1f - d), 2.2f)));
                }
            _glow.Apply();
        }
    }
}
