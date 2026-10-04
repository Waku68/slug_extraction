using SlugMove;
using UnityEngine;

namespace SlugExtract
{
    /// <summary>
    /// The player's own slug, small in the top left corner: filmed from in front and a little to the right of the way it
    /// faces (as if crawling toward the camera), so what it carries, how swollen the bag is, what it holds and how it
    /// moves are always in sight. Its own camera draws the body as everyone else sees it (not the first-person copies),
    /// with only what is near it (the far clip shows the backdrop instead of the distance).
    /// </summary>
    [DefaultExecutionOrder(200)]
    public class SelfPortrait : MonoBehaviour
    {
        public SlugMoveController controller;
        [Tooltip("画面に表示する")] public bool showHud = true;
        [Tooltip("画面上の大きさ（px）")] public float size = 150f;
        [Tooltip("カメラの位置（体の向き基準：右, 上, 前, m）。進行方向の斜め右前から撮る")] public Vector3 offset = new Vector3(0.7f, 0.5f, 1.3f);
        [Tooltip("カメラが見る点（体の向き基準：右, 上, 前, m）")] public Vector3 lookAt = new Vector3(0f, 0.3f, -0.12f);
        [Tooltip("画角（度）")] public float fieldOfView = 36f;
        [Tooltip("これより遠くは映さず、背景の色にする（m）")] public float farClip = 3f;
        [Tooltip("背景の色")] public Color background = new Color(0.17f, 0.2f, 0.22f);

        Camera _cam;
        RenderTexture _rt;

        /// <summary>Where it is drawn on the screen (GUI coordinates).</summary>
        public Rect Area => new Rect(10f, 10f, size, size);
        public bool Shown => showHud && isActiveAndEnabled && _rt != null;

        void Start()
        {
            int px = Mathf.Max(64, Mathf.RoundToInt(size * 1.5f));
            _rt = new RenderTexture(px, px, 24) { name = "SelfPortrait", antiAliasing = 2 };
            var go = new GameObject("SelfPortraitCamera") { hideFlags = HideFlags.DontSave };
            _cam = go.AddComponent<Camera>();
            _cam.targetTexture = _rt;
            _cam.clearFlags = CameraClearFlags.SolidColor;
            _cam.backgroundColor = background;
            _cam.nearClipPlane = 0.05f;
            _cam.depth = -10f;
            var data = go.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
            data.renderShadows = false;
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
            _cam.farClipPlane = farClip;
            _cam.backgroundColor = background;
            var t = controller.transform;
            Vector3 pos = t.TransformPoint(offset), at = t.TransformPoint(lookAt);
            _cam.transform.SetPositionAndRotation(pos, Quaternion.LookRotation(at - pos, Vector3.up));
        }

        void OnGUI()
        {
            if (!Shown) return;
            var r = Area;
            GUI.Box(new Rect(r.x - 2f, r.y - 2f, r.width + 4f, r.height + 4f), GUIContent.none);
            GUI.DrawTexture(r, _rt, ScaleMode.StretchToFill, false);
        }
    }
}
