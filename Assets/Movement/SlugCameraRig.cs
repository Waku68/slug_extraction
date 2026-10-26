using SlugDesign;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SlugMove
{
    /// <summary>
    /// Eyes of the slug. First person sits on the face and sways with the crawl wave; V steps on to a
    /// third-person orbit to check the body, then to one over the right shoulder (<see cref="shoulder"/>), then back. Blows shake it (<see cref="Shake"/>).
    /// Esc frees the cursor, a click locks it again.
    /// </summary>
    public class SlugCameraRig : MonoBehaviour
    {
        public SlugMoveSettings settings;
        public SlugMoveController controller;
        public SlugPop slug;
        public Camera cam;
        public bool thirdPerson;
        /// <summary>In third person: over the right shoulder (closer, the body on the left of the view) instead of straight behind.</summary>
        public bool shoulder;
        /// <summary>Magnification (a scope sets it): narrows the view and slows the look to match. 1 = none.</summary>
        [System.NonSerialized] public float zoom = 1f;
        /// <summary>Turned by something else (a bot, later the network) through <see cref="SetLook"/>: no mouse, no cursor lock.</summary>
        [System.NonSerialized] public bool driven;
        /// <summary>A menu is open (the loadout picker): the cursor stays free, a click does not lock it, the eyes do not turn.</summary>
        [System.NonSerialized] public bool cursorFree;
        /// <summary>0..1: how far the slug has bent round to look at its own back (set by the bag; eased here).</summary>
        [System.NonSerialized] public float peek;
        /// <summary>What the eyes look at while peeking (world): the middle of the bag.</summary>
        [System.NonSerialized] public Vector3 peekTarget;
        /// <summary>Looking at its own back: the tools cannot be used.</summary>
        public bool Peeking => peek > 0.02f;
        /// <summary>Busy with something else (setting the bag down, shouldering one): the tools cannot be used.</summary>
        [System.NonSerialized] public bool handsBusy;
        /// <summary>The hands are not free for the tools (peeking, or busy).</summary>
        /// <summary>Using something on the belt (a moss ball of water): the tools cannot be used meanwhile.</summary>
        [System.NonSerialized] public bool beltBusy;
        public bool HandsBusy => Peeking || handsBusy || beltBusy;
        /// <summary>The own body is in view (third person, or the eyes bent round out of the head to look at the bag): draw
        /// the body's tools and what is in the hands, not the first-person copies (those would follow the eyes round).</summary>
        public bool ShowsBody => thirdPerson || peek > 0.35f;
        /// <summary>The own camera does not draw the body's tools, arms and what is in the hands (only their shadows): in
        /// first person (the copies are drawn instead), and while peeking (they would cover the bag).</summary>
        public bool OwnToolsHidden => !ShowsBody || Peeking;

        /// <summary>Where the eyes look (degrees). The body follows it.</summary>
        public float Yaw { get; private set; }
        /// <summary>Up / down look (degrees, + = down).</summary>
        public float Pitch => _pitch;

        float _pitch;
        float _bob01;
        InputAction _look;
        Material _bodyMat;
        float _shakeAmp, _shakeTime, _shakeDuration;
        // shrink and stretch: a springy offset of the eyes (right, up, forward of the look, m) and their pitch (+ = down),
        // and the field of view (narrowed while charging, flung wide by the stretch)
        Vector3 _kickPos, _kickPosV;
        float _kickPitch, _kickPitchV, _fovAdd, _fovPunch;

        void OnEnable()
        {
            _look = InputSystem.actions != null ? InputSystem.actions.FindAction("Player/Look") : null;
            _look?.Enable();
            if (controller) { controller.Squeezed += OnSqueezed; controller.Lunged += OnLunged; controller.Landed += OnLanded; }
        }

        void Start()
        {
            Yaw = controller.transform.eulerAngles.y;
            if (!driven) LockCursor(true);
            // the helmet only casts its shadow for this camera while it sits in the head
            var gear = slug ? slug.GetComponent<SlugDesign.SlugEquipment>() : null;
            if (gear) gear.SetOwnCamera(cam, () => !ShowsBody);
            cam.cullingMask |= 1 << SlugToolHolder.BodyToolLayer;
        }

        void OnDisable()
        {
            if (!driven) LockCursor(false);
            if (controller) { controller.Squeezed -= OnSqueezed; controller.Lunged -= OnLunged; controller.Landed -= OnLanded; }
        }

        // ------------------------------------------------------------------ shrink and stretch
        /// <summary>The press: the eyes dip and nod a little at once.</summary>
        void OnSqueezed()
        {
            float k = settings.lungeCamKick;
            _kickPosV.y -= 0.6f * k;
            _kickPitchV += 45f * k;
        }

        /// <summary>The stretch: the view widens for a moment, the eyes trail a few cm behind and lift a little.</summary>
        void OnLunged(float charge)
        {
            float k = settings.lungeCamKick, a = Mathf.Lerp(0.4f, 1f, charge);
            _kickPosV.z -= 2.5f * k * a;
            _kickPitchV -= 60f * k * a;
            _fovPunch = Mathf.Max(_fovPunch, settings.lungeFovKick * k * a);
            _fovAdd = Mathf.Max(_fovAdd, 0f);       // the narrowing of the charge lets go at once
        }

        /// <summary>Landing: the eyes sink and nod, and bounce back.</summary>
        void OnLanded()
        {
            float k = settings.lungeCamKick;
            _kickPosV.y -= 1f * k;
            _kickPitchV += 60f * k;
        }

        /// <summary>Steps the kick springs (on 1/240 s steps: stiff springs blow up on a long frame).</summary>
        void StepKick(float dt)
        {
            float k = settings.lungeCamKick, charge = controller ? controller.LungeCharge : 0f;
            bool charging = controller && controller.Charging;
            // while shrinking: a little lower and nodding, and the view narrows as it charges
            Vector3 posTarget = charging ? new Vector3(0f, -0.012f * k * (0.4f + 0.6f * charge), 0f) : Vector3.zero;
            float pitchTarget = charging ? 0.8f * k * charge : 0f;
            float w = 2f * Mathf.PI * 7f, z = 0.45f;
            int n = Mathf.Clamp(Mathf.CeilToInt(dt * 240f), 1, 16);
            for (int i = 0; i < n; i++)
            {
                float h = dt / n;
                _kickPosV += (w * w * (posTarget - _kickPos) - 2f * z * w * _kickPosV) * h;
                _kickPos += _kickPosV * h;
                _kickPitchV += (w * w * (pitchTarget - _kickPitch) - 2f * z * w * _kickPitchV) * h;
                _kickPitch += _kickPitchV * h;
            }
            // the punch comes at once and eases back
            _fovPunch *= Mathf.Exp(-dt / 0.25f);
            float fovTarget = -2.5f * k * charge + _fovPunch;
            _fovAdd = Mathf.Lerp(_fovAdd, fovTarget, 1f - Mathf.Exp(-dt * (fovTarget > _fovAdd ? 60f : 12f)));
        }

        static void LockCursor(bool on)
        {
            Cursor.lockState = on ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !on;
        }

        /// <summary>Turns the eyes by this much (degrees; pitch + = down), e.g. the climb of a rifle's recoil.</summary>
        public void AddLook(float yaw, float pitch)
        {
            Yaw += yaw;
            _pitch = Mathf.Clamp(_pitch + pitch, settings.minPitch, settings.maxPitch);
        }

        /// <summary>Sets where the eyes look (degrees; pitch + = down).</summary>
        public void SetLook(float yaw, float pitch)
        {
            Yaw = yaw;
            _pitch = Mathf.Clamp(pitch, settings.minPitch, settings.maxPitch);
        }

        /// <summary>A short, decaying shake (1 = a full-power blow). A stronger shake replaces a weaker one.</summary>
        public void Shake(float strength, float duration)
        {
            float left = _shakeDuration > 0f ? _shakeAmp * (1f - Mathf.Clamp01(_shakeTime / _shakeDuration)) : 0f;
            if (strength < left) return;
            _shakeAmp = strength;
            _shakeTime = 0f;
            _shakeDuration = Mathf.Max(duration, 0.01f);
        }

        void Update()
        {
            if (driven) return;
            var kb = Keyboard.current;
            var mouse = Mouse.current;
            if (kb != null && kb.vKey.wasPressedThisFrame)
            {
                // first person -> behind -> right shoulder -> first person
                if (!thirdPerson) { thirdPerson = true; shoulder = false; }
                else if (!shoulder) shoulder = true;
                else { thirdPerson = false; shoulder = false; }
            }
            if (cursorFree)
            {
                if (Cursor.lockState != CursorLockMode.None) LockCursor(false);
                return;
            }
            if (kb != null && kb.escapeKey.wasPressedThisFrame) LockCursor(false);
            if (mouse != null && mouse.leftButton.wasPressedThisFrame && Cursor.lockState != CursorLockMode.Locked) LockCursor(true);

            if (Cursor.lockState == CursorLockMode.Locked && _look != null)
            {
                Vector2 d = _look.ReadValue<Vector2>() * (settings.mouseSensitivity / Mathf.Max(zoom, 1f));
                Yaw += d.x;
                _pitch = Mathf.Clamp(_pitch - d.y, settings.minPitch, settings.maxPitch);
            }
        }

        void LateUpdate()
        {
            var s = settings;
            float dt = Time.deltaTime;
            if (!_bodyMat && slug)
            {
                foreach (var r in slug.GetComponentsInChildren<MeshRenderer>(true))
                    if (r.name == "Body") { _bodyMat = r.sharedMaterial; break; }
            }
            StepKick(dt);
            cam.fieldOfView = zoom > 1.001f && !thirdPerson
                ? 2f * Mathf.Atan(Mathf.Tan(s.fieldOfView * 0.5f * Mathf.Deg2Rad) / zoom) * Mathf.Rad2Deg : s.fieldOfView + _fovAdd;
            Quaternion look = Quaternion.Euler(_pitch, Yaw, 0f);
            Vector3 pos;
            Quaternion rot;

            if (thirdPerson)
            {
                Vector3 pivot = controller.transform.position + Vector3.up * s.thirdPersonHeight;
                float dist = s.thirdPersonDistance;
                if (shoulder)
                {
                    // move the pivot out to the right first, stopping short of a wall
                    Vector3 right = Quaternion.Euler(0f, Yaw, 0f) * Vector3.right;
                    float side = s.shoulderSide;
                    if (Physics.SphereCast(pivot, 0.1f, right, out var sideHit, side, SlugHitboxes.WorldMask, QueryTriggerInteraction.Ignore))
                        side = Mathf.Max(0f, sideHit.distance);
                    pivot += right * side;
                    // and up, so the camera looks down over the body a little
                    float up = s.shoulderUp;
                    if (Physics.SphereCast(pivot, 0.1f, Vector3.up, out var upHit, up, SlugHitboxes.WorldMask, QueryTriggerInteraction.Ignore))
                        up = Mathf.Max(0f, upHit.distance);
                    pivot += Vector3.up * up;
                    dist = s.shoulderDistance;
                }
                Vector3 back = look * Vector3.back;
                if (Physics.SphereCast(pivot, 0.1f, back, out var hit, dist, SlugHitboxes.WorldMask, QueryTriggerInteraction.Ignore))
                    dist = Mathf.Max(0.2f, hit.distance);
                pos = pivot + back * dist;
                rot = look;
            }
            else
            {
                // ---- on the face
                Vector3 eye = slug.FaceCenterWorld + slug.FaceForwardWorld * s.eyeForward + slug.FaceUpWorld * s.eyeUp;

                // the same body breathing as the shader: the head moves by (stretch - 1) * (head - tail)
                float stretch = 0.07f, waveSpeed = 0.8f, wavePhase = 0f;
                if (_bodyMat)
                {
                    stretch = _bodyMat.GetFloat("_Stretch");
                    waveSpeed = _bodyMat.GetFloat("_WaveSpeed");
                    wavePhase = _bodyMat.GetFloat("_WavePhase");
                }
                float theta = 2f * Mathf.PI * waveSpeed * controller.CrawlTime + wavePhase;
                eye += (slug.FaceCenterWorld - slug.TailWorld) * (stretch * Mathf.Sin(theta) * s.stretchFollow);

                // ---- sway with the wave, only while moving
                _bob01 = Mathf.Lerp(_bob01, controller.Speed01, 1f - Mathf.Exp(-6f * dt));
                float b = s.bobStrength * _bob01;
                float ph = theta * s.bobFrequency;
                Quaternion yawOnly = Quaternion.Euler(0f, Yaw, 0f);
                eye += Vector3.up * (Mathf.Sin(ph) * s.bobVertical * b);
                eye += yawOnly * Vector3.right * (Mathf.Sin(ph * 0.5f) * s.bobLateral * b);
                float roll = Mathf.Sin(ph * 0.5f + 0.6f) * s.bobRoll * b;
                pos = eye;
                rot = look * Quaternion.Euler(0f, 0f, roll);
            }

            // ---- peeking: the head turns round over the right shoulder and the eyes look down at the bag on the back
            if (peek > 0f)
            {
                float w = peek * peek * (3f - 2f * peek);
                Quaternion yawOnly = Quaternion.Euler(0f, Yaw, 0f);
                Vector3 over = slug.FaceCenterUnturnedWorld + yawOnly * s.peekEye;     // (from where the face was before it turned)
                Vector3 to = peekTarget - over;
                Quaternion peekRot = to.sqrMagnitude > 1e-6f ? Quaternion.LookRotation(to, Vector3.up) : rot;
                pos = Vector3.Lerp(pos, over, w);
                rot = Quaternion.Slerp(rot, peekRot, w);
            }

            // ---- shrink and stretch
            pos += rot * _kickPos;
            rot *= Quaternion.Euler(_kickPitch, 0f, 0f);

            // ---- shake: a few millimetres and a degree at most, fading out quickly
            if (_shakeTime < _shakeDuration)
            {
                _shakeTime += dt;
                float k = 1f - Mathf.Clamp01(_shakeTime / _shakeDuration);
                float a = _shakeAmp * k * k;
                float t = Time.time * 38f;
                Vector3 n = new Vector3(Mathf.PerlinNoise(t, 0.3f), Mathf.PerlinNoise(1.7f, t), Mathf.PerlinNoise(t, 5.1f)) * 2f - Vector3.one;
                pos += rot * new Vector3(n.x, n.y, 0f) * (0.006f * a);
                rot *= Quaternion.Euler(n.y * 0.9f * a, n.x * 0.6f * a, n.z * 1.2f * a);
            }
            cam.transform.SetPositionAndRotation(pos, rot);
        }
    }
}
