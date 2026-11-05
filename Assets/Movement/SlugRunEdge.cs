using UnityEngine;

namespace SlugMove
{
    /// <summary>
    /// The breath for running, on the player's own view (Nov 3 わくさん: no number, no bar). Below staminaWarnBelow the
    /// edges of the view darken little by little, up to staminaEdgeDark when it is gone; out of breath it stays dark,
    /// throbbing slowly with the heaving, and fades as the breath comes back. Added by <see cref="SlugMoveController"/>
    /// to the two-legged slug; it draws nothing for a bot or while the view is not this slug's.
    /// </summary>
    public class SlugRunEdge : MonoBehaviour
    {
        [Tooltip("暗くなる・戻る速さ（秒）")] public float fade = 0.25f;

        SlugMoveController _ctrl;
        Texture2D _edge;
        float _shown;

        void Awake() => _ctrl = GetComponent<SlugMoveController>();

        void OnDestroy() { if (_edge) Destroy(_edge); }

        void Update()
        {
            if (!_ctrl || !_ctrl.settings) return;
            var s = _ctrl.settings;
            float k = Mathf.InverseLerp(Mathf.Max(s.staminaWarnBelow, 1e-3f), 0f, _ctrl.Stamina01);
            if (_ctrl.Breathless) k = Mathf.Max(k, 0.6f) * (0.85f + 0.15f * Mathf.Sin(Time.time * 2f * Mathf.PI * 1.6f));
            _shown = Mathf.MoveTowards(_shown, k * s.staminaEdgeDark, Time.deltaTime / Mathf.Max(fade, 1e-3f));
        }

        void OnGUI()
        {
            if (_shown <= 0.002f || !_ctrl || _ctrl.driven) return;
            var rig = _ctrl.cameraRig;
            if (!rig || !rig.cam || !rig.cam.isActiveAndEnabled || rig.driven) return;
            if (Event.current.type != EventType.Repaint) return;
            if (!_edge) Build();
            var old = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, _shown);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), _edge, ScaleMode.StretchToFill, true);
            GUI.color = old;
        }

        /// <summary>Clear in the middle, dark toward the edges and corners (a soft oval).</summary>
        void Build()
        {
            const int n = 64;
            _edge = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, hideFlags = HideFlags.DontSave };
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float u = (x + 0.5f) / n * 2f - 1f, v = (y + 0.5f) / n * 2f - 1f;
                    float d = Mathf.Sqrt(u * u + v * v);
                    _edge.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.6f, 1.35f, d))));
                }
            _edge.Apply();
        }
    }
}
