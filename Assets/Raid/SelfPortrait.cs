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

        Camera _cam;
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
            GUI.DrawTexture(Area, _rt, ScaleMode.StretchToFill, true);
        }
    }
}
