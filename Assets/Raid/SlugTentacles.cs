using SlugDesign;
using SlugMove;
using UnityEngine;

namespace SlugExtract
{
    /// <summary>
    /// The tentacles (the ears): they stand while the slug is well and droop to the sides as its health runs down, and a
    /// hit pulls them in for a moment (the side it came from most). Everyone sees the body's tentacles do it
    /// (<see cref="SlugPop.SetEarPose"/>; the hitboxes and a tentacle helmet's horns go with them). The drooping is only
    /// an extra: health itself is shown by the white salt left where the slug was hit (spec 8章, Oct 25), and the player
    /// sees no tentacles of its own in first person. Under a tentacle helmet the player hears the world muffled (the high
    /// sounds go). Added to every slug with health by <see cref="SlugHealth"/>.
    /// </summary>
    public class SlugTentacles : MonoBehaviour
    {
        [Header("体の触角（みんなに見える）")]
        [Range(0f, 1f), Tooltip("体力がゼロのとき、触角が地面の方へどれだけ倒れるか（1 = 地面を向く）")] public float droopMax = 0.6f;
        [Range(0f, 1f), Tooltip("体力がこの割合を切ると垂れ始める")] public float droopFrom = 0.9f;
        [Range(0.1f, 1f), Tooltip("撃たれた瞬間に引っ込んだときの長さ（1 = 引っ込まない）")] public float retractLength = 0.35f;
        [Tooltip("引っ込むまでの時間・引っ込んだままの時間・戻るまでの時間（秒）")] public float retractIn = 0.05f;
        public float retractHold = 0.12f, retractOut = 0.5f;
        [Range(0f, 1f), Tooltip("撃たれたのと反対側の触角が引っ込む割合")] public float retractOtherSide = 0.5f;

        [Header("触角の形の兜")]
        [Tooltip("音がこもる：これより高い音を消す（Hz）")] public float muffleCutoff = 2200f;

        SlugPop _slug;
        SlugHealth _health;
        SlugHitboxes _hitboxes;
        SlugCameraRig _rig;
        SlugGearBase _gear;

        // per side (0 = left, 1 = right): how far drawn in (0..1), how hard the last hit pulled, the time since it
        readonly float[] _pull = new float[2], _pullTo = new float[2], _since = { 99f, 99f };
        float _droop;
        readonly Renderer[] _ears = new Renderer[2];
        AudioLowPassFilter _muffle;

        void Awake()
        {
            _slug = GetComponentInChildren<SlugPop>(true);
            _health = GetComponentInChildren<SlugHealth>(true);
            _hitboxes = GetComponentInChildren<SlugHitboxes>(true);
            _rig = GetComponentInChildren<SlugCameraRig>(true);
            if (_slug) _gear = _slug.GetComponent<SlugGearBase>();
        }

        void OnEnable()
        {
            if (_hitboxes) _hitboxes.Hit += OnHit;
            if (!_moisture) _moisture = transform.root.GetComponentInChildren<SlugMoisture>(true);
            if (_moisture) { _moisture.DryingStarted += OnDrying; _moisture.StageChanged += OnStage; }
            UnityEngine.Rendering.RenderPipelineManager.beginCameraRendering += OnBeginCamera;
        }

        void OnDisable()
        {
            if (_hitboxes) _hitboxes.Hit -= OnHit;
            if (_moisture) { _moisture.DryingStarted -= OnDrying; _moisture.StageChanged -= OnStage; }
            UnityEngine.Rendering.RenderPipelineManager.beginCameraRendering -= OnBeginCamera;
        }

        SlugMoisture _moisture;

        /// <summary>Out into the sun or onto salt: both tentacles flinch in a little ("ジュッ", spec 4章 Oct 10).</summary>
        void OnDrying() => Twitch(0.45f);

        /// <summary>Moisture crossed into a drier stage: a flinch, harder the drier (into the wet: none, the sound says it).</summary>
        void OnStage(int now, int was)
        {
            if (now > was) Twitch(now >= 3 ? 0.8f : 0.55f);
        }

        /// <summary>Both tentacles pull in by <paramref name="amount"/> (0..1) for a moment, as a hit does but evenly.</summary>
        public void Twitch(float amount)
        {
            for (int i = 0; i < 2; i++)
            {
                _pullTo[i] = Mathf.Max(amount, _since[i] < retractIn + retractHold ? _pullTo[i] : 0f);
                _since[i] = 0f;
            }
        }

        bool Helmeted => _gear && _gear.helmet && _gear.tentacleHelmet;
        /// <summary>This slug is the player's own (its eyes are the camera), not a bot's.</summary>
        bool Own => _rig && _rig.cam && !_rig.driven && _rig.cam.isActiveAndEnabled;

        /// <summary>A hit pulls both tentacles in, the one on the side it came from all the way.</summary>
        void OnHit(SlugHitbox box, RaycastHit hit)
        {
            Transform from = Own ? _rig.cam.transform : transform;
            int near = Vector3.Dot(hit.point - from.position, from.right) < 0f ? 0 : 1;
            for (int i = 0; i < 2; i++)
            {
                _pullTo[i] = Mathf.Max(i == near ? 1f : retractOtherSide, _since[i] < retractIn + retractHold ? _pullTo[i] : 0f);
                _since[i] = 0f;
            }
        }

        void LateUpdate()
        {
            if (!_slug || _slug.VisualRoot == null) return;
            float dt = Time.deltaTime;
            float f = _health ? _health.Fraction : 1f;
            float d01 = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(droopFrom, 0f, f));
            _droop = Mathf.Lerp(_droop, d01, 1f - Mathf.Exp(-dt * 4f));
            for (int i = 0; i < 2; i++)
            {
                _since[i] += dt;
                float target = _since[i] < retractIn + retractHold ? _pullTo[i] : 0f;
                float rate = target > _pull[i] ? 1f / Mathf.Max(retractIn, 1e-3f) : 1f / Mathf.Max(retractOut, 1e-3f);
                _pull[i] = Mathf.MoveTowards(_pull[i], target, rate * dt);

                // falls out to its side and a little forward (clear of the helmet's brim), and draws in
                float side = i == 0 ? -1f : 1f;
                _slug.EarRoot(side, out _, out var axis, out _, out _);
                Vector3 down = (Vector3.right * side + Vector3.down * 0.6f + Vector3.forward * 0.25f).normalized;
                Quaternion bend = Quaternion.Slerp(Quaternion.identity, Quaternion.FromToRotation(axis, down), droopMax * _droop);
                float x = _pull[i];
                _slug.SetEarPose(side, bend, Mathf.Lerp(1f, retractLength, x * x * (3f - 2f * x)));
            }
            UpdateMuffle();
        }

        /// <summary>The own camera in first person sees only the shadows of the body's tentacles (drooping, they could
        /// fall in front of the eyes); every other camera sees them.</summary>
        void OnBeginCamera(UnityEngine.Rendering.ScriptableRenderContext context, Camera camera)
        {
            if (!_rig || !_rig.cam || !_slug || _slug.VisualRoot == null) return;
            if (!_ears[0] || !_ears[1])
                for (int i = 0; i < 2; i++)
                {
                    var t = _slug.VisualRoot.Find(i == 0 ? "Ear_L" : "Ear_R");
                    _ears[i] = t ? t.GetComponent<Renderer>() : null;
                }
            bool firstPerson = camera == _rig.cam && Own && !_rig.ShowsBody;
            var mode = firstPerson ? UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly : UnityEngine.Rendering.ShadowCastingMode.On;
            foreach (var r in _ears) if (r) r.shadowCastingMode = mode;
        }

        void UpdateMuffle()
        {
            if (!Own) return;
            bool on = Helmeted;
            if (!_muffle)
            {
                if (!on) return;
                var listener = _rig.cam.GetComponent<AudioListener>();
                if (!listener) listener = FindAnyObjectByType<AudioListener>();
                if (!listener) return;
                _muffle = listener.GetComponent<AudioLowPassFilter>();
                if (!_muffle) _muffle = listener.gameObject.AddComponent<AudioLowPassFilter>();
            }
            _muffle.cutoffFrequency = muffleCutoff;
            if (_muffle.enabled != on) _muffle.enabled = on;
        }
    }
}
