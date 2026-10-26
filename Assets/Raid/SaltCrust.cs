using SlugMove;
using UnityEngine;

namespace SlugExtract
{
    /// <summary>
    /// The player's own health, with no gauge (spec 8章, Oct 25): every round that lands throws white salt grains onto the
    /// edge of the view on the side it came from, and they stay there until the health they cost comes back. The more is
    /// lost, the deeper the crust reaches in from the edge (never as far as the crosshair). As the slug recovers (moisture,
    /// rain) the oldest grains go first. Losing health to drying leaves no salt: drying has its own look (4章).
    /// Only the player's own first-person view. Added to every slug with health by <see cref="SlugHealth"/> (it stays idle
    /// on bots).
    /// </summary>
    public class SaltCrust : MonoBehaviour
    {
        [Tooltip("体力 1% あたりに、こびりつく粒の数")] public float grainsPerPercent = 2.6f;
        [Tooltip("1発で広がる角度（度）。飛んできた方向を中心に")] public float spreadDegrees = 22f;
        [Tooltip("縁から内へ届く深さ（画面の高さの割合）：少し撃たれたとき・体力がほぼないとき")] public float depthLight = 0.04f;
        public float depthHeavy = 0.2f;
        [Tooltip("粒の大きさ（画面の高さの割合）：小さい粒・大きい粒")] public float grainMin = 0.007f, grainMax = 0.026f;
        [Tooltip("粒の色。白い塩に、明るい所でも見えるよう少し灰色のふち")] public Color grainColor = new Color(0.97f, 0.97f, 0.95f, 0.95f);
        [Tooltip("当たった瞬間、粒がはじけて現れる時間（秒）")] public float popTime = 0.08f;

        struct Grain
        {
            public Vector2 edge;      // where on the edge (0..1 of the screen), and the way in from it
            public Vector2 inward;
            public float depth, size, angle, born;
            public int shape;
        }

        readonly System.Collections.Generic.List<Grain> _grains = new System.Collections.Generic.List<Grain>();
        SlugHealth _health;
        SlugHitboxes _hitboxes;
        SlugCameraRig _rig;
        Texture2D[] _shapes;
        float _lastHealth = -1f;
        // a hit this frame: where it came from (screen direction), waiting for the damage it did
        Vector2 _pendingDir;
        bool _pending;
        // salt still on the view, in health points (healing takes grains away in proportion)
        float _saltLoss;

        void Awake()
        {
            _health = GetComponentInChildren<SlugHealth>(true);
            _hitboxes = GetComponentInChildren<SlugHitboxes>(true);
            _rig = GetComponentInChildren<SlugCameraRig>(true);
        }

        void OnEnable() { if (_hitboxes) _hitboxes.Hit += OnHit; }
        void OnDisable() { if (_hitboxes) _hitboxes.Hit -= OnHit; }

        void OnDestroy()
        {
            if (_shapes != null) foreach (var t in _shapes) if (t) Destroy(t);
        }

        bool Own => _rig && _rig.cam && !_rig.driven && _rig.cam.isActiveAndEnabled;

        /// <summary>Where the round came from, as a direction on the screen (x right, y up).</summary>
        void OnHit(SlugHitbox box, RaycastHit hit)
        {
            if (!Own) return;
            var cam = _rig.cam.transform;
            Vector3 from = -box.LastVelocity;
            if (from.sqrMagnitude < 1e-6f) from = cam.position - hit.point;          // (nothing to go by: from the hit point)
            Vector3 local = cam.InverseTransformDirection(from.normalized);
            Vector2 dir = new Vector2(local.x, local.y);
            // straight from behind or in front there is no side to it: the bottom edge (behind) or the top (in front)
            if (dir.sqrMagnitude < 0.04f) dir = local.z < 0f ? Vector2.down : Vector2.up;
            _pendingDir = dir.normalized;
            _pending = true;
            // in words, for when it falls (spec 8章: only the way it came, never who or how far)
            Vector3 flat = new Vector3(local.x, 0f, local.z);
            LastFrom = flat.sqrMagnitude < 1e-4f ? "上" : flat.z < -0.6f * flat.magnitude ? "うしろ" : flat.z > 0.6f * flat.magnitude ? "前" : flat.x > 0f ? "右" : "左";
        }

        /// <summary>The way the last round came from, in words ("うしろ", "右", "左", "前"; null before any).</summary>
        public string LastFrom { get; private set; }

        void LateUpdate()
        {
            if (!_health) return;
            float now = _health.Current;
            if (_lastHealth < 0f) _lastHealth = now;
            float change = now - _lastHealth;
            _lastHealth = now;
            float max = Mathf.Max(_health.Max, 1e-3f);

            if (change < 0f && _pending)
            {
                // salt: new grains round the way it came in, reaching deeper the more is gone
                float lost = -change;
                _saltLoss += lost;
                float heavy = 1f - _health.Fraction;
                int n = Mathf.Max(3, Mathf.RoundToInt(lost / max * 100f * grainsPerPercent));
                float baseAngle = Mathf.Atan2(_pendingDir.y, _pendingDir.x) * Mathf.Rad2Deg;
                for (int i = 0; i < n; i++)
                {
                    float a = (baseAngle + Gauss() * spreadDegrees * 0.5f) * Mathf.Deg2Rad;
                    var g = new Grain
                    {
                        edge = EdgePoint(new Vector2(Mathf.Cos(a), Mathf.Sin(a)), out var inward),
                        inward = inward,
                        depth = Mathf.Pow(Random.value, 2.2f) * Mathf.Lerp(depthLight, depthHeavy, heavy),
                        size = Mathf.Lerp(grainMin, grainMax, Mathf.Pow(Random.value, 1.8f)),
                        angle = Random.Range(0f, 360f),
                        born = Time.time + Random.value * 0.05f,
                        shape = Random.Range(0, 4),
                    };
                    _grains.Add(g);
                }
            }
            else if (change > 0f && _saltLoss > 0f)
            {
                // healing: the oldest salt goes first, in proportion to what came back
                float back = Mathf.Min(change, _saltLoss);
                int remove = Mathf.RoundToInt(_grains.Count * back / _saltLoss);
                if (back >= _saltLoss - 1e-4f) remove = _grains.Count;
                _saltLoss -= back;
                _grains.RemoveRange(0, Mathf.Min(remove, _grains.Count));
            }
            _pending = false;
        }

        static float Gauss() => (Random.value + Random.value + Random.value - 1.5f) * 1.4f;

        /// <summary>Where a ray from the middle of the screen leaves it (0..1, y up), and the way back in.</summary>
        static Vector2 EdgePoint(Vector2 dir, out Vector2 inward)
        {
            float aspect = (float)Screen.width / Mathf.Max(Screen.height, 1);
            Vector2 d = new Vector2(dir.x / aspect, dir.y);                     // in screen fractions
            float t = 0.5f / Mathf.Max(Mathf.Abs(d.x), Mathf.Abs(d.y), 1e-4f);
            Vector2 p = new Vector2(0.5f, 0.5f) + d * t;
            // straight in from that edge (not toward the middle), so the crust hugs the edge
            inward = Mathf.Abs(d.x) * 1f > Mathf.Abs(d.y) ? new Vector2(-Mathf.Sign(d.x), 0f) : new Vector2(0f, -Mathf.Sign(d.y));
            return p;
        }

        void OnGUI()
        {
            if (_grains.Count == 0 || !Own || _rig.ShowsBody || Event.current.type != EventType.Repaint) return;
            if (_shapes == null) BuildShapes();
            float w = Screen.width, h = Screen.height;
            var oldColor = GUI.color;
            var oldMatrix = GUI.matrix;
            foreach (var g in _grains)
            {
                float age = Time.time - g.born;
                if (age < 0f) continue;
                float pop = popTime > 0f ? Mathf.Clamp01(age / popTime) : 1f;
                float s = g.size * h * Mathf.Lerp(1.6f, 1f, pop);
                Vector2 p = g.edge + g.inward * g.depth * new Vector2(h / w, 1f);
                Vector2 c = new Vector2(p.x * w, (1f - p.y) * h);
                GUI.matrix = oldMatrix;
                GUIUtility.RotateAroundPivot(g.angle, c);
                GUI.color = grainColor;
                GUI.DrawTexture(new Rect(c.x - s * 0.5f, c.y - s * 0.5f, s, s), _shapes[g.shape], ScaleMode.StretchToFill, true);
            }
            GUI.matrix = oldMatrix;
            GUI.color = oldColor;
        }

        /// <summary>Four crystal outlines: rough, many-sided, white with a grey rim so they show on a bright sky too.</summary>
        void BuildShapes()
        {
            const int n = 32;
            _shapes = new Texture2D[4];
            var rnd = new System.Random(7);
            for (int k = 0; k < 4; k++)
            {
                int sides = 4 + k % 3;
                var radii = new float[sides];
                float turn = (float)rnd.NextDouble();
                for (int i = 0; i < sides; i++) radii[i] = 0.6f + 0.35f * (float)rnd.NextDouble();
                var t = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, hideFlags = HideFlags.DontSave };
                for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float u = (x + 0.5f) / n * 2f - 1f, v = (y + 0.5f) / n * 2f - 1f;
                    float ang = Mathf.Repeat(Mathf.Atan2(v, u) / (Mathf.PI * 2f) + turn, 1f) * sides;
                    int i0 = Mathf.FloorToInt(ang) % sides, i1 = (i0 + 1) % sides;
                    float edge = Mathf.Lerp(radii[i0], radii[i1], ang - Mathf.Floor(ang));
                    float r = Mathf.Sqrt(u * u + v * v);
                    float a = Mathf.Clamp01((edge - r) * n * 0.5f);
                    float rim = Mathf.Clamp01((r - (edge - 0.22f)) * 6f);
                    float shade = Mathf.Lerp(1f, 0.62f, rim) * (0.92f + 0.08f * v);
                    t.SetPixel(x, y, new Color(shade, shade, shade * 0.98f, a));
                }
                t.Apply();
                _shapes[k] = t;
            }
        }
    }
}
