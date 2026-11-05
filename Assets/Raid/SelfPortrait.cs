using System.Collections.Generic;
using SlugMove;
using UnityEngine;
using UnityEngine.Rendering;

namespace SlugExtract
{
    /// <summary>
    /// The player's own slug, small in the top left corner: filmed from in front and a little to the right of the way it
    /// faces (as if crawling toward the camera), so what it carries, how swollen the bag is, what it holds and how it
    /// moves are always in sight. Only the slug itself is drawn, on nothing (no ground, no world): just before its camera
    /// draws, everything of the slug (its own hierarchy, and the tools, arms and things in its hands marked with
    /// <see cref="SlugBodyPart"/>) is moved onto a layer only that camera sees, and put back right after. It draws the
    /// body as everyone else sees it (not the first-person copies).
    /// In the sun (Nov 5 わくさん, in place of the "ジュッ" sound): a warm glow rises behind the slug, from the side the sun
    /// is on, stronger the harder the sun dries it there (<see cref="SlugMoisture.SunDrying"/>: soil faint, salt strongest).
    /// </summary>
    [DefaultExecutionOrder(200)]
    public class SelfPortrait : MonoBehaviour
    {
        /// <summary>The layer the slug is drawn on, for this camera only.</summary>
        public const int PortraitLayer = 30;

        public SlugMoveController controller;
        [Tooltip("画面に表示する")] public bool showHud = true;
        [Tooltip("画面上の大きさ（px）")] public float size = 150f;
        [Tooltip("カメラの位置（体の向き基準：右, 上, 前, m）。進行方向の斜め右前から撮る")] public Vector3 offset = new Vector3(0.7f, 0.5f, 1.3f);
        [Tooltip("カメラが見る点（体の向き基準：右, 上, 前, m）")] public Vector3 lookAt = new Vector3(0f, 0.3f, -0.12f);
        [Tooltip("画角（度）")] public float fieldOfView = 32f;
        [Tooltip("日なたで体のうしろに出る光の色")] public Color sunGlowColor = new Color(1f, 0.82f, 0.45f);
        [Tooltip("日なたの光の濃さ（土の日なた → 塩の日なた）")] public Vector2 sunGlowAlpha = new Vector2(0.45f, 0.95f);

        Camera _cam;
        SlugMoisture _moisture;
        float _glow, _glowPower;
        Vector2 _glowSide;
        static Texture2D _glowTex;
        RenderTexture _rt;
        readonly List<Renderer> _renderers = new List<Renderer>();
        readonly List<GameObject> _moved = new List<GameObject>();
        readonly List<int> _movedLayers = new List<int>();

        /// <summary>Where it is drawn on the screen (GUI coordinates).</summary>
        public Rect Area => new Rect(10f, 10f, size, size);
        public bool Shown => showHud && isActiveAndEnabled && _rt != null;

        void Start()
        {
            int px = Mathf.Max(64, Mathf.RoundToInt(size * 1.5f));
            _rt = new RenderTexture(px, px, 24, RenderTextureFormat.ARGB32) { name = "SelfPortrait", antiAliasing = 2 };
            var go = new GameObject("SelfPortraitCamera") { hideFlags = HideFlags.DontSave };
            _cam = go.AddComponent<Camera>();
            _cam.targetTexture = _rt;
            _cam.clearFlags = CameraClearFlags.SolidColor;
            _cam.backgroundColor = Color.clear;
            _cam.cullingMask = 1 << PortraitLayer;
            _cam.nearClipPlane = 0.05f;
            _cam.farClipPlane = 10f;
            _cam.depth = -10f;
            var data = go.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
            data.renderShadows = false;
            data.renderPostProcessing = false;
            _moisture = GetComponent<SlugMoisture>();
        }

        void OnEnable()
        {
            RenderPipelineManager.beginCameraRendering += OnBeginCamera;
            RenderPipelineManager.endCameraRendering += OnEndCamera;
        }

        void OnDisable()
        {
            RenderPipelineManager.beginCameraRendering -= OnBeginCamera;
            RenderPipelineManager.endCameraRendering -= OnEndCamera;
            PutBack();
        }

        void OnDestroy()
        {
            if (_cam) Destroy(_cam.gameObject);
            if (_rt) { _rt.Release(); Destroy(_rt); }
        }

        void LateUpdate()
        {
            if (!_cam || !controller) return;
            _cam.enabled = showHud;
            if (!showHud) return;
            _cam.fieldOfView = fieldOfView;
            var t = controller.transform;
            Vector3 pos = t.TransformPoint(offset), at = t.TransformPoint(lookAt);
            _cam.transform.SetPositionAndRotation(pos, Quaternion.LookRotation(at - pos, Vector3.up));
            SunGlow();
        }

        /// <summary>How strong the glow behind the slug is, and which side of the picture the sun is on.</summary>
        void SunGlow()
        {
            float drying = _moisture ? _moisture.SunDrying : 0f;
            // comes up quickly as it steps into the sun, fades slower as it leaves
            _glow = Mathf.MoveTowards(_glow, drying > 0f ? 1f : 0f, Time.deltaTime / (drying > 0f ? 0.25f : 0.6f));
            if (drying > 0f) _glowPower = Mathf.MoveTowards(_glowPower, drying, Time.deltaTime);
            var weather = SlugWorld.WeatherPresenter.Current;
            if (!weather) return;
            // the sun seen from the portrait camera: right / up in the picture (GUI y runs down)
            Vector3 toSun = -weather.Time.sunDirection.normalized;
            var ct = _cam.transform;
            var side = new Vector2(Vector3.Dot(toSun, ct.right), -Vector3.Dot(toSun, ct.up));
            _glowSide = Vector2.Lerp(_glowSide, side, 1f - Mathf.Exp(-4f * Time.deltaTime));
        }

        static Texture2D GlowTexture()
        {
            if (_glowTex) return _glowTex;
            const int n = 64;
            _glowTex = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, hideFlags = HideFlags.DontSave };
            var px = new Color[n * n];
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float dx = (x + 0.5f) / n * 2f - 1f, dy = (y + 0.5f) / n * 2f - 1f;
                    float a = Mathf.Clamp01(1f - Mathf.Sqrt(dx * dx + dy * dy));
                    px[y * n + x] = new Color(1f, 1f, 1f, a * a * (3f - 2f * a));
                }
            _glowTex.SetPixels(px);
            _glowTex.Apply();
            return _glowTex;
        }

        /// <summary>Just before this camera draws: everything of the slug onto the portrait layer.</summary>
        void OnBeginCamera(ScriptableRenderContext context, Camera camera)
        {
            if (camera != _cam || !controller) return;
            PutBack();
            _renderers.Clear();
            controller.GetComponentsInChildren(true, _renderers);
            Transform owner = controller.body;
            var parts = new List<Renderer>();
            foreach (var part in SlugBodyPart.All)
            {
                if (!part || part.owner != owner) continue;
                part.GetComponentsInChildren(true, parts);
                _renderers.AddRange(parts);
            }
            foreach (var r in _renderers)
            {
                if (!r) continue;
                var go = r.gameObject;
                _moved.Add(go);
                _movedLayers.Add(go.layer);
                go.layer = PortraitLayer;
            }
        }

        void OnEndCamera(ScriptableRenderContext context, Camera camera)
        {
            if (camera == _cam) PutBack();
        }

        void PutBack()
        {
            for (int i = 0; i < _moved.Count; i++) if (_moved[i]) _moved[i].layer = _movedLayers[i];
            _moved.Clear();
            _movedLayers.Clear();
        }

        void OnGUI()
        {
            if (!Shown) return;
            if (_glow > 0.001f)
            {
                // behind the slug, leaning toward the sun's side of the picture
                Rect a = Area;
                float r = a.width * Mathf.Lerp(0.55f, 0.7f, _glowPower);
                Vector2 c = a.center + _glowSide * (a.width * 0.22f);
                var old = GUI.color;
                var col = sunGlowColor;
                col.a = _glow * Mathf.Lerp(sunGlowAlpha.x, sunGlowAlpha.y, _glowPower);
                GUI.color = col;
                GUI.DrawTexture(new Rect(c.x - r, c.y - r, 2f * r, 2f * r), GlowTexture(), ScaleMode.StretchToFill, true);
                GUI.color = old;
            }
            GUI.DrawTexture(Area, _rt, ScaleMode.StretchToFill, true);
        }
    }
}
