using SlugMove;
using SlugWorld;
using UnityEngine;

namespace SlugExtract
{
    /// <summary>
    /// The sun on the slug, on the player's own view (Oct 26 わくさん: in the sun the slug dries, and drying kills, so say it
    /// loudly). While the slug is in the sun on a sunny day, broad bands of light lie across the whole view, all running
    /// about the same way: in from the side of the screen the sun is on (from the sun itself when it is in view), brighter
    /// on that side and fading as they cross (Oct 27 わくさん: not fanned out from one point at the edge).
    /// How strong they are is how fast the sun dries the slug there (<see cref="SlugMoisture.SunDrying"/>: soil faint, sand more, salt
    /// full); in the shade, under cloud, at dawn and dusk and at night there are none, so the light always means danger.
    /// Stepping out of the shade flares them once. The bands drift and breathe slowly, like light through moving air.
    /// Not used since Nov 3 (わくさん: it got in the way of seeing; the sun shows only on the island, in
    /// <see cref="SlugWorld.SunShafts"/>). Nothing adds it any more; the code is kept.
    /// </summary>
    public class SunGlare : MonoBehaviour
    {
        public SlugMoisture moisture;
        [Tooltip("光の帯の本数")] public int bands = 9;
        [Tooltip("いちばん強いとき（塩の上）の、帯1本の濃さ")] public float maxAlpha = 0.34f;
        [Tooltip("太陽の側の画面の明るみの濃さ（いちばん強いとき）")] public float washAlpha = 0.28f;
        [Tooltip("いちばん弱いとき（土・草の日なた）でも、これだけは出す（日なたにいることは必ず分かる）")] public float minShare = 0.55f;
        [Tooltip("帯の太さ（画面の高さの割合）：細い帯・太い帯")] public Vector2 width = new Vector2(0.04f, 0.16f);
        [Tooltip("帯どうしの向きのずれ（度）。0 ならまったく平行")] public float angleJitter = 4f;
        [Tooltip("光の色")] public Color color = new Color(1f, 0.93f, 0.68f, 1f);
        [Tooltip("日なたに出た瞬間に強く光る時間（秒）")] public float flareTime = 0.6f;
        [Tooltip("出る・消える速さ（秒）")] public float fadeIn = 0.25f, fadeOut = 0.5f;

        SlugCameraRig _rig;
        Texture2D _band, _wash;
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
            if (_band) Destroy(_band);
            if (_wash) Destroy(_wash);
        }

        bool Own => _rig && _rig.cam && !_rig.driven && _rig.cam.isActiveAndEnabled;

        void Update()
        {
            if (!moisture) return;
            float target = moisture.SunDrying > 0f ? Mathf.Lerp(minShare, 1f, Mathf.InverseLerp(0.3f, 1f, moisture.SunDrying)) : 0f;
            float speed = 1f / Mathf.Max(target > _shown ? fadeIn : fadeOut, 1e-3f);
            _shown = Mathf.MoveTowards(_shown, target, speed * Time.deltaTime);
            // out of the shade: one flare, so the moment it starts is caught even without looking at anything
            if (moisture.InSun && !_wasInSun) _flare = flareTime;
            _wasInSun = moisture.InSun;
            _flare = Mathf.Max(0f, _flare - Time.deltaTime);
        }

        /// <summary>The way the light runs across the screen (x right, y up): away from where the sun is.</summary>
        bool LightOnScreen(out Vector2 inward)
        {
            inward = Vector2.down;
            var weather = WeatherPresenter.Current;
            if (!weather) return false;
            var cam = _rig.cam;
            Vector3 toSun = -weather.Time.sunDirection;
            Vector3 local = cam.transform.InverseTransformDirection(toSun);
            Vector2 from;
            Vector3 vp = cam.WorldToViewportPoint(cam.transform.position + toSun * 100f);
            if (local.z > 0.05f && vp.x > -0.1f && vp.x < 1.1f && vp.y > -0.1f && vp.y < 1.1f)
                from = new Vector2(vp.x - 0.5f, vp.y - 0.5f) * new Vector2((float)Screen.width / Mathf.Max(Screen.height, 1), 1f);
            else from = new Vector2(local.x, local.y);
            // straight behind or overhead has no side to it: from the top
            if (from.sqrMagnitude < 0.004f) from = Vector2.up;
            inward = -from.normalized;
            return true;
        }

        void OnGUI()
        {
            if (_shown <= 0.001f || !Own || _rig.ShowsBody || Event.current.type != EventType.Repaint) return;
            if (!LightOnScreen(out var inward)) return;
            if (_band == null) BuildTextures();
            float w = Screen.width, h = Screen.height;
            float diag = Mathf.Sqrt(w * w + h * h);
            var center = new Vector2(w * 0.5f, h * 0.5f);
            Vector2 d = new Vector2(inward.x, -inward.y);            // (GUI y runs down)
            Vector2 across = new Vector2(-d.y, d.x);
            float angle = Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg;
            float flare = flareTime > 0f ? _flare / flareTime : 0f;
            float strength = _shown * (1f + 0.8f * flare);
            var oldColor = GUI.color;
            var oldMatrix = GUI.matrix;

            // the sun's side of the view is lit, fading across
            Vector2 washStart = center - d * (diag * 0.5f);
            GUIUtility.RotateAroundPivot(angle, washStart);
            GUI.color = new Color(color.r, color.g, color.b, washAlpha * strength);
            GUI.DrawTexture(new Rect(washStart.x, washStart.y - diag * 0.7f, diag * 0.85f, diag * 1.4f), _wash, ScaleMode.StretchToFill, true);

            // the bands: spread over the whole view side by side, all running the same way (a few degrees apart),
            // starting off the screen on the sun's side; they drift sideways and breathe slowly
            for (int i = 0; i < bands; i++)
            {
                float s0 = _seed[i % _seed.Length], s1 = _seed[(i + 5) % _seed.Length];
                float k = (i + 0.5f) / bands;
                float off = (k - 0.5f) * diag * 1.05f + (s0 - 0.5f) * diag * 0.06f + Mathf.Sin(Time.time * 0.06f + s1 * 6.3f) * diag * 0.025f;
                Vector2 start = center - d * (diag * (0.55f + 0.15f * s1)) + across * off;
                float len = diag * Mathf.Lerp(0.9f, 1.4f, s0);
                float wid = h * Mathf.Lerp(width.x, width.y, Mathf.Repeat(s0 * 3.71f + s1, 1f));
                float breathe = 0.6f + 0.4f * Mathf.Sin(Time.time * (0.35f + 0.4f * s1) + s0 * 11f);
                GUI.matrix = oldMatrix;
                GUIUtility.RotateAroundPivot(angle + (s1 - 0.5f) * 2f * angleJitter, start);
                GUI.color = new Color(color.r, color.g, color.b, maxAlpha * strength * breathe);
                GUI.DrawTexture(new Rect(start.x, start.y - wid * 0.5f, len, wid), _band, ScaleMode.StretchToFill, true);
            }
            GUI.matrix = oldMatrix;
            GUI.color = oldColor;
        }

        void BuildTextures()
        {
            var rnd = new System.Random(11);
            _seed = new float[16];
            for (int i = 0; i < _seed.Length; i++) _seed[i] = (float)rnd.NextDouble();

            // a band: soft at both sides, coming in gently and fading out along its length
            const int L = 128, W = 32;
            _band = new Texture2D(L, W, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, hideFlags = HideFlags.DontSave };
            for (int y = 0; y < W; y++)
                for (int x = 0; x < L; x++)
                {
                    float u = (x + 0.5f) / L, v = (y + 0.5f) / W * 2f - 1f;
                    float side = Mathf.SmoothStep(0f, 1f, 1f - Mathf.Abs(v));
                    float along = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0f, 0.25f, u)) * Mathf.Pow(1f - u, 1.2f);
                    _band.SetPixel(x, y, new Color(1f, 1f, 1f, side * along));
                }
            _band.Apply();

            // the wash: full at the sun's edge, gone across the view
            const int n = 64;
            _wash = new Texture2D(n, 4, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, hideFlags = HideFlags.DontSave };
            for (int y = 0; y < 4; y++)
                for (int x = 0; x < n; x++)
                    _wash.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Pow(1f - (x + 0.5f) / n, 2.2f)));
            _wash.Apply();
        }
    }
}
