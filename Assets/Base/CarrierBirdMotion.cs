using UnityEngine;

namespace SlugBase
{
    /// <summary>
    /// The carrier bird's motion, all in code (no animation from Blender). Put it on the prefab root.
    /// Idle: breathes (body bone, folded wings and tail bob together, the legs stay put), now and then tilts its head,
    /// turns its neck toward the nearest slug, and opens its throat pouch to call. Flying: <see cref="SetFlying"/> swaps
    /// the folded wings for the open ones, which flap at the shoulder (<see cref="Flap"/>). Carrying the basket is not here.
    ///
    /// Numbers (Oct 19; slug eye 0.47 m, bird 2.1 m tall):
    /// - Breath: 3.4 s per breath, 1.5 cm up and down (for a human-sized bird about 5 cm).
    /// - Neck: turns at most 35° left/right, 25° down / 12° up; slugs within 9 m and within 110° of the beak.
    ///   Head tilt (roll) 22°, held 0.8–1.6 s, every 4–9 s. With no slug near it glances 0–18° to the sides.
    ///   Angles follow with a 0.22 s lag (0.12 s for the tilt start, so it reads as a quick curious "huh?").
    /// - Call: every 11–22 s, two honks; the pouch (pivot at the jaw hinge) opens 22° and the head lifts 10°.
    /// - Flap: ±35° at the shoulder, 1.1 flaps per second (slow, a big soft bird).
    /// Rotations are done in the bird's own space (X right, Y up, Z beak) on top of the rest pose, so the Blender bone
    /// axes (Body is turned 270° on X) do not matter.
    /// </summary>
    public class CarrierBirdMotion : MonoBehaviour
    {
        [Header("体（呼吸）")]
        [Tooltip("1回の呼吸（秒）")] public float breathPeriod = 3.4f;
        [Tooltip("体の上下の幅（m）")] public float breathLift = 0.015f;

        [Header("首")]
        [Tooltip("左右に振り向く最大（度）")] public float maxYaw = 35f;
        [Tooltip("下を向く最大（度）")] public float maxPitchDown = 25f;
        [Tooltip("上を向く最大（度）")] public float maxPitchUp = 12f;
        [Tooltip("ナメクジに気づく距離（m）")] public float lookRange = 9f;
        [Tooltip("くちばしからこの角度の中のナメクジだけ見る（度）")] public float lookCone = 110f;
        [Tooltip("首が追いつく速さ（秒）")] public float neckLag = 0.22f;
        [Tooltip("首をかしげる角度（度）")] public float tiltAngle = 22f;
        [Tooltip("かしげる間（秒、最小・最大）")] public Vector2 tiltHold = new Vector2(0.8f, 1.6f);
        [Tooltip("次にかしげるまで（秒、最小・最大）")] public Vector2 tiltEvery = new Vector2(4f, 9f);
        [Tooltip("だれもいないとき横を見る角度（度）")] public float glanceYaw = 18f;

        [Header("鳴く")]
        [Tooltip("次に鳴くまで（秒、最小・最大）")] public Vector2 callEvery = new Vector2(11f, 22f);
        [Tooltip("のど袋が開く角度（度）")] public float jawOpen = 22f;
        [Tooltip("鳴くとき頭を上げる（度）")] public float callLift = 10f;

        [Header("飛ぶ")]
        [Tooltip("羽ばたきの角度（度、上下それぞれ）")] public float flapAngle = 35f;
        [Tooltip("1秒の羽ばたき回数")] public float flapRate = 1.1f;
        [Tooltip("飛んでいる間、自分で羽ばたく")] public bool autoFlap = true;

        const float CallLength = 1.1f;            // two honks
        const float SlugEye = 0.3f;               // look at a slug this high above its origin (m)

        Transform _root, _body, _neck, _pouch, _tail;
        Transform _foldL, _foldR, _openL, _openR;
        Vector3 _bodyRest, _tailRest, _foldLRest, _foldRRest;
        Quaternion _neckRest, _pouchRest, _openLRest, _openRRest;   // neck: in the bird's space; others: local
        Vector3 _jawAxis, _neckPivot;                               // jaw axis in the neck's space; neck pivot in the bird's space

        float _yaw, _pitch, _roll, _jaw, _yawV, _pitchV, _rollV, _jawV;
        float _tiltUntil, _nextTilt, _tiltSign = 1f, _glance, _nextGlance;
        float _callStart = -99f, _nextCall, _breath, _flap;
        float _nextScan;
        Transform[] _slugs = new Transform[0];

        public bool Flying { get; private set; }
        public bool Calling => Time.time - _callStart < CallLength;

        void Awake()
        {
            _root = Find("CarrierBird_Root") ?? transform;
            _body = Find("Body");
            _neck = Find("Neck");
            _pouch = Find("CarrierBird_Pouch");
            _tail = Find("CarrierBird_Tail");
            _foldL = Find("CarrierBird_WingFolded_L");
            _foldR = Find("CarrierBird_WingFolded_R");
            _openL = Find("CarrierBird_WingOpen_L");
            _openR = Find("CarrierBird_WingOpen_R");

            if (_body) _bodyRest = _body.localPosition;
            if (_tail) _tailRest = _tail.localPosition;
            if (_foldL) _foldLRest = _foldL.localPosition;
            if (_foldR) _foldRRest = _foldR.localPosition;
            if (_openL) _openLRest = _openL.localRotation;
            if (_openR) _openRRest = _openR.localRotation;
            if (_neck)
            {
                _neckRest = Quaternion.Inverse(_root.rotation) * _neck.rotation;
                _neckPivot = _root.InverseTransformPoint(_neck.position);
                _jawAxis = Quaternion.Inverse(_neckRest) * Vector3.right;   // bird's right, seen from the head at rest
            }
            if (_pouch) _pouchRest = _pouch.localRotation;

            float t = Time.time;
            _nextTilt = t + Random.Range(tiltEvery.x, tiltEvery.y);
            _nextCall = t + Random.Range(3f, callEvery.y);   // the first call comes a bit sooner
            _nextGlance = t + Random.Range(2f, 5f);
            _breath = Random.value;
        }

        Transform Find(string name)
        {
            foreach (var t in GetComponentsInChildren<Transform>(true))
                if (t.name == name) return t;
            return null;
        }

        void Update()
        {
            float dt = Time.deltaTime, now = Time.time;

            // breath: the body, folded wings and tail go up and down together; the legs stay on the ground
            _breath = (_breath + dt / Mathf.Max(0.1f, breathPeriod)) % 1f;
            float lift = breathLift * 0.5f * (1f - Mathf.Cos(_breath * Mathf.PI * 2f));
            Vector3 up = _root.InverseTransformDirection(Vector3.up) * lift;   // root space; body bone is in its own
            if (_body) _body.position = _body.parent.TransformPoint(_bodyRest) + Vector3.up * lift;
            if (_tail) _tail.localPosition = _tailRest + up;
            if (_foldL) _foldL.localPosition = _foldLRest + up;
            if (_foldR) _foldR.localPosition = _foldRRest + up;

            // neck target: the nearest slug in front, else a slow glance to the side
            float yawT, pitchT = 0f;
            if (!Flying && LookAtSlug(out float slugYaw, out float slugPitch)) { yawT = slugYaw; pitchT = slugPitch; }   // flying: the slugs in the basket are under it
            else
            {
                if (now > _nextGlance)
                {
                    _glance = Random.value < 0.35f ? 0f : Random.Range(-glanceYaw, glanceYaw);
                    _nextGlance = now + Random.Range(2.5f, 6f);
                }
                yawT = _glance;
            }

            // tilt the head now and then
            if (now > _nextTilt && !Calling) Tilt();
            float rollT = now < _tiltUntil ? tiltAngle * _tiltSign : 0f;

            // call: two honks, the pouch opens, the head lifts
            if (now > _nextCall && now > _tiltUntil) Call();
            float jawT = 0f;
            if (Calling)
            {
                float c = (now - _callStart) / CallLength;           // 0..1
                float honk = Mathf.Sin(Mathf.Clamp01(c * 2f % 1f) * Mathf.PI);
                jawT = jawOpen * honk;
                pitchT = -callLift * Mathf.Sin(c * Mathf.PI);
                rollT = 0f;
            }

            yawT = Mathf.Clamp(yawT, -maxYaw, maxYaw);
            pitchT = Mathf.Clamp(pitchT, -maxPitchUp, maxPitchDown);
            _yaw = Mathf.SmoothDamp(_yaw, yawT, ref _yawV, neckLag);
            _pitch = Mathf.SmoothDamp(_pitch, pitchT, ref _pitchV, neckLag);
            _roll = Mathf.SmoothDamp(_roll, rollT, ref _rollV, rollT != 0f ? neckLag * 0.55f : neckLag);
            _jaw = Mathf.SmoothDamp(_jaw, jawT, ref _jawV, 0.06f);

            if (_neck) _neck.rotation = _root.rotation * Quaternion.Euler(_pitch, _yaw, _roll) * _neckRest;
            if (_pouch) _pouch.localRotation = Quaternion.AngleAxis(_jaw, _jawAxis) * _pouchRest;

            if (Flying && autoFlap)
            {
                _flap = (_flap + dt * flapRate) % 1f;
                Flap(_flap);
            }
        }

        bool LookAtSlug(out float yaw, out float pitch)
        {
            yaw = pitch = 0f;
            if (Time.time > _nextScan)
            {
                var found = FindObjectsByType<SlugMove.SlugMoveController>();
                _slugs = new Transform[found.Length];
                for (int i = 0; i < found.Length; i++) _slugs[i] = found[i].transform;
                _nextScan = Time.time + 1f;
            }

            float best = lookRange * lookRange;
            bool any = false;
            foreach (var s in _slugs)
            {
                if (!s) continue;
                Vector3 d = _root.InverseTransformPoint(s.position + Vector3.up * SlugEye) - _neckPivot;
                float flat = new Vector2(d.x, d.z).magnitude;
                float y = Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg;
                if (flat * flat + d.y * d.y > best || Mathf.Abs(y) > lookCone) continue;
                best = flat * flat + d.y * d.y;
                yaw = y;
                pitch = Mathf.Atan2(-d.y, flat) * Mathf.Rad2Deg;   // +X turns the beak down
                any = true;
            }
            return any;
        }

        /// <summary>Tilt the head to one side for a moment ("huh?").</summary>
        [ContextMenu("Tilt")]
        public void Tilt()
        {
            _tiltSign = Random.value < 0.5f ? -1f : 1f;
            _tiltUntil = Time.time + Random.Range(tiltHold.x, tiltHold.y);
            _nextTilt = _tiltUntil + Random.Range(tiltEvery.x, tiltEvery.y);
        }

        /// <summary>Open the throat pouch and honk twice.</summary>
        [ContextMenu("Call")]
        public void Call()
        {
            _callStart = Time.time;
            _nextCall = Time.time + Random.Range(callEvery.x, callEvery.y);
        }

        /// <summary>Swap the folded wings for the open ones (and back). Open wings start level.</summary>
        public void SetFlying(bool on)
        {
            Flying = on;
            if (_foldL) _foldL.gameObject.SetActive(!on);
            if (_foldR) _foldR.gameObject.SetActive(!on);
            if (_openL) _openL.gameObject.SetActive(on);
            if (_openR) _openR.gameObject.SetActive(on);
            _flap = 0f;
            Flap(0f);
        }

        [ContextMenu("Toggle Flying")]
        void ToggleFlying() => SetFlying(!Flying);

        /// <summary>Set the open wings to one point of the flap: 0 = level, 0.25 = top (+flapAngle), 0.75 = bottom.</summary>
        public void Flap(float phase)
        {
            float a = flapAngle * Mathf.Sin(phase * Mathf.PI * 2f);
            // the flap axis is the bird's forward (Z); +Z turns the right wing up, so the left wing takes -a
            if (_openL) _openL.localRotation = Quaternion.AngleAxis(-a, Vector3.forward) * _openLRest;
            if (_openR) _openR.localRotation = Quaternion.AngleAxis(a, Vector3.forward) * _openRRest;
        }
    }
}
