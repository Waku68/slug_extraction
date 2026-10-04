using UnityEngine;
using UnityEngine.InputSystem;

namespace SlugMove
{
    /// <summary>
    /// First-person crawl: slow acceleration, a slippery stop, slower uphill.
    /// The CharacterController handles the slope limit and small steps. Taller steps (up to about head height)
    /// are climbed slowly, like a snail going up a wall. The body (SlugPop) is tilted to the ground and
    /// its shader crawl is driven by the distance actually travelled.
    ///
    /// Instead of a jump, the slug shrinks and stretches: hold Jump to shrink (the body gets short, wide and low, and
    /// hardly moves: easy to hit), let go to stretch forward in a low hop, faster the longer it was held, then glide out
    /// on landing with a wobble. It is for getting out of a line of fire, not for climbing.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class SlugMoveController : MonoBehaviour
    {
        public SlugMoveSettings settings;
        public SlugCameraRig cameraRig;
        [Tooltip("SlugPop の付いた見た目。傾きはこのコンポーネントが回す")] public Transform body;
        [Tooltip("速度の倍率（荷物の重さなど、ほかのコンポーネントが変える）")] public float speedMultiplier = 1f;
        [Tooltip("false の間は入力を受け付けない（試合の終了後など）")] public bool inputEnabled = true;
        [Tooltip("上半身の前後の傾き（度、+ = 前）。ピッケルを振る動きなど、ほかのコンポーネントが変える")] public float leanDegrees;
        /// <summary>How far the front of the body is twisted round to the right (degrees; the rear stays put): looking
        /// back at the bag. Set by the bag.</summary>
        [System.NonSerialized] public float twistDegrees;
        /// <summary>Lying flat (Z toggles it): the upper body down near the ground, the face and arms moved up to the front
        /// of it. Slow, and no stretching or climbing meanwhile.</summary>
        [System.NonSerialized] public bool prone;
        /// <summary>0..1: how far down it is.</summary>
        public float Prone01 { get; private set; }

        static readonly int FreezeId = Shader.PropertyToID("_SlugFreeze");
        static readonly int TimeId = Shader.PropertyToID("_SlugTimeValue");
        const float Gravity = 9.81f;
        static int Mask => SlugHitboxes.WorldMask;        // skips Ignore Raycast (chips, effects) and slugs' hitboxes
        const QueryTriggerInteraction NoTriggers = QueryTriggerInteraction.Ignore;

        CharacterController _cc;
        InputAction _move, _jump;
        Vector3 _velocity;          // horizontal, the speed the slug wants to keep
        float _vy;
        Vector3 _groundNormal = Vector3.up;
        Vector3 _bodyBasePos;
        Quaternion _tilt = Quaternion.identity;
        SlugDesign.SlugPop _slug;
        float _tailLag, _tailLagVel;

        // shrink, then stretch forward
        bool _charging, _lunging, _airborne;
        float _charge, _cooldown, _stretch, _stretchV, _lungeAlong = 1f;
        Vector3 _bodyBaseScale = Vector3.one;

        // climbing a step taller than stepHeight
        bool _climbing;
        Vector3 _climbDir, _climbWallPoint;
        float _climbStartY, _climbTopY, _climbProgress;

        /// <summary>Crawl time fed to the slug shader (advances with the distance travelled).</summary>
        public float CrawlTime { get; private set; }
        /// <summary>Actual speed (m/s, along the ground or up the wall), smoothed.</summary>
        public float Speed { get; private set; }
        public float Speed01 => settings.maxSpeed > 0f ? Mathf.Clamp01(Speed / settings.maxSpeed) : 0f;
        /// <summary>Angle of the ground under the slug (degrees).</summary>
        public float SlopeAngle { get; private set; }
        public bool OnGround { get; private set; }
        public bool TooSteep => OnGround && !_climbing && SlopeAngle > settings.slopeLimit;
        public bool Climbing => _climbing;
        /// <summary>0..1 while shrinking (how far it is charged), 0 otherwise.</summary>
        public float LungeCharge => _charging ? _charge : 0f;
        /// <summary>True from letting go until the stretch has glided out.</summary>
        public bool Lunging => _lunging;

        /// <summary>Holds the Jump button without the keyboard (tests and tools).</summary>
        [System.NonSerialized] public bool forceJumpHold;

        void Awake()
        {
            _cc = GetComponent<CharacterController>();
            if (body) { _bodyBasePos = body.localPosition; _bodyBaseScale = body.localScale; }
        }

        void OnEnable()
        {
            _move = InputSystem.actions != null ? InputSystem.actions.FindAction("Player/Move") : null;
            _move?.Enable();
            _jump = InputSystem.actions != null ? InputSystem.actions.FindAction("Player/Jump") : null;
            _jump?.Enable();
            // the crawl of every slug shader now follows CrawlTime instead of the clock
            Shader.SetGlobalFloat(FreezeId, 1f);
        }

        void OnDisable() { Shader.SetGlobalFloat(FreezeId, 0f); }

        void Update()
        {
            Vector2 input = _move != null && inputEnabled ? Vector2.ClampMagnitude(_move.ReadValue<Vector2>(), 1f) : Vector2.zero;
            bool jump = forceJumpHold || (inputEnabled && _jump != null && _jump.IsPressed());
            var kb = Keyboard.current;
            if (inputEnabled && kb != null && kb.zKey.wasPressedThisFrame && Cursor.lockState == CursorLockMode.Locked) prone = !prone;
            Simulate(Time.deltaTime, input, jump);
        }

        /// <summary>One step of movement with this input (move stick, Jump held). Tools call it to step by hand.</summary>
        public void Simulate(float dt, Vector2 input, bool jumpHeld)
        {
            var s = settings;
            if (dt <= 0f) return;
            _cc.slopeLimit = s.slopeLimit;
            _cc.stepOffset = Mathf.Clamp(s.stepHeight, 0f, _cc.height);

            ProbeGround();
            Prone01 = Mathf.MoveTowards(Prone01, prone && !_climbing ? 1f : 0f, dt / Mathf.Max(s.proneTime, 0.01f));
            if (Prone01 > 0f) jumpHeld = false;                  // lying down: no shrinking and stretching

            // ---- input, relative to where the eyes look
            float yaw = cameraRig ? cameraRig.Yaw : transform.eulerAngles.y;
            float forward = input.y * (input.y < 0f ? s.backwardMultiplier : 1f);
            Vector3 wish = Quaternion.Euler(0f, yaw, 0f) * new Vector3(input.x * s.strafeMultiplier, 0f, forward);
            float wishAmount = Mathf.Min(wish.magnitude, 1f);
            Vector3 dir = wishAmount > 1e-4f ? wish.normalized : Vector3.zero;

            StepLunge(dt, jumpHeld, dir, yaw);
            if (_charging) wishAmount *= s.lungeChargeMove;
            wishAmount *= Mathf.Lerp(1f, s.proneSpeed, Prone01);

            Vector3 before = transform.position;
            if (!_climbing && !_lunging && !_charging && Prone01 <= 0f && OnGround && input.y > 0.1f) TryStartClimb(dir);
            if (_climbing) Climb(dir, wishAmount, dt);
            else Crawl(dir, wishAmount, dt);
            Speed = Mathf.Lerp(Speed, (transform.position - before).magnitude / dt, 1f - Mathf.Exp(-10f * dt));

            // ---- the body turns after the eyes, a little late (and faces the wall while climbing)
            float targetYaw = _climbing ? Quaternion.LookRotation(_climbDir).eulerAngles.y : yaw;
            float prevYaw = transform.eulerAngles.y;
            float bodyYaw = Mathf.MoveTowardsAngle(prevYaw, targetYaw, s.bodyTurnSpeed * dt);
            transform.rotation = Quaternion.Euler(0f, bodyYaw, 0f);
            FollowTail(Mathf.DeltaAngle(prevYaw, bodyYaw), dt);
            AlignBody(dt);

            // ---- the crawl wave advances with the distance travelled
            CrawlTime += dt * (s.idleCrawlRate + Speed * s.crawlRatePerSpeed);
            Shader.SetGlobalFloat(TimeId, CrawlTime);
        }

        // ------------------------------------------------------------------ shrink, then stretch forward
        /// <summary>
        /// Holding Jump on the ground charges (the body shrinks); letting go throws the slug forward the way it is
        /// crawling (or the way it looks), a little up off the ground, faster the longer it was held.
        /// </summary>
        void StepLunge(float dt, bool held, Vector3 dir, float yaw)
        {
            var s = settings;
            _cooldown = Mathf.Max(0f, _cooldown - dt);
            bool canCharge = !_climbing && !_lunging && OnGround && _cooldown <= 0f;
            if (held && (_charging || canCharge))
            {
                _charging = true;
                _charge = Mathf.Min(1f, _charge + dt / Mathf.Max(s.lungeChargeTime, 1e-3f));
            }
            else if (_charging)
            {
                Vector3 d = dir != Vector3.zero ? dir : Quaternion.Euler(0f, yaw, 0f) * Vector3.forward;
                float c = Mathf.SmoothStep(0f, 1f, _charge);
                _velocity = d * (Mathf.Lerp(s.lungeSpeedMin, s.lungeSpeedMax, c) * speedMultiplier);
                // the head keeps facing the way the eyes look (the eyes sit in it): a stretch along the body makes it
                // long; a stretch to the side flings the tail out the other way instead, trailing behind
                _lungeAlong = Mathf.Abs(Vector3.Dot(d, transform.forward));
                _tailLagVel += Vector3.Dot(d, transform.right) * 420f * Mathf.Lerp(0.5f, 1f, c);
                _vy = s.lungeUp * Mathf.Lerp(0.4f, 1f, c);
                _lunging = _airborne = true;
                _cooldown = s.lungeCooldown;
                _stretchV += Mathf.Lerp(0.5f, 1f, c) * Mathf.Lerp(0.3f, 1f, _lungeAlong) * s.lungeStretch * 2f * Mathf.PI * s.lungeSpringHz * 1.8f;
                _charging = false;
                _charge = 0f;
            }

            // the body's length: short while charging, long in the air, a wobble on landing
            float target = _charging ? -s.lungeSquash * Mathf.SmoothStep(0f, 1f, _charge)
                         : _airborne ? s.lungeStretch * 0.6f * Mathf.Lerp(0.3f, 1f, _lungeAlong) : 0f;
            float w = 2f * Mathf.PI * s.lungeSpringHz;
            int n = Mathf.Clamp(Mathf.CeilToInt(dt * 240f), 1, 16);
            for (int i = 0; i < n; i++)
            {
                float h = dt / n;
                _stretchV += (w * w * (target - _stretch) - 2f * s.lungeSpringDamping * w * _stretchV) * h;
                _stretch += _stretchV * h;
            }
            _stretch = Mathf.Clamp(_stretch, -0.45f, 0.6f);
        }

        // ------------------------------------------------------------------ crawling on the ground
        void Crawl(Vector3 dir, float wishAmount, float dt)
        {
            var s = settings;
            if (_lunging)
            {
                // carried by the stretch: no speed limit; it keeps its speed in the air and glides out on the ground
                if (!_airborne)
                {
                    float sp = _velocity.magnitude, floor = s.maxSpeed * speedMultiplier * wishAmount;
                    float next = Mathf.Max(floor, sp * Mathf.Exp(-dt / Mathf.Max(s.lungeSlideTime, 1e-3f)));
                    _velocity = sp > 1e-5f ? _velocity * (next / sp) : Vector3.zero;
                    if (next <= s.maxSpeed * speedMultiplier * 1.02f) _lunging = false;
                }
            }
            // speed: slow to build up, and a slippery stop (exponential glide + a small constant brake)
            else if (wishAmount > 1e-4f)
            {
                float target = s.maxSpeed * speedMultiplier * wishAmount * SlopeFactor(dir);
                _velocity = Vector3.MoveTowards(_velocity, dir * target, s.acceleration * dt);
            }
            else
            {
                float sp = _velocity.magnitude;
                float next = Mathf.Max(0f, sp * Mathf.Exp(-dt / Mathf.Max(s.slideTime, 1e-3f)) - s.deceleration * dt);
                _velocity = sp > 1e-5f ? _velocity * (next / sp) : Vector3.zero;
            }

            // follow the ground plane so the slug hugs slopes instead of launching off them
            Vector3 motion = _velocity;
            if (OnGround && !TooSteep && _velocity.sqrMagnitude > 1e-8f)
                motion = Vector3.ProjectOnPlane(_velocity, _groundNormal).normalized * _velocity.magnitude;
            if (TooSteep)
                motion += Vector3.ProjectOnPlane(Vector3.down, _groundNormal).normalized * s.steepSlideSpeed;
            if (_airborne) motion = _velocity;                          // off the ground: no hugging the slope
            _vy = _cc.isGrounded && _vy <= 0f ? -1f : _vy - Gravity * dt;

            Vector3 before = transform.position;
            _cc.Move((motion + Vector3.up * _vy) * dt);
            if (_airborne && _cc.isGrounded && _vy <= 0f)
            {
                // landed: squashes, then wobbles back
                _airborne = false;
                _stretchV -= settings.lungeStretch * 2f * Mathf.PI * settings.lungeSpringHz * 1.6f;
            }
            Vector3 moved = transform.position - before;
            moved.y = 0f;

            // blocked by a wall: lose the speed that did not turn into movement
            Vector3 expected = motion; expected.y = 0f;
            float expectedLen = expected.magnitude * dt;
            if (!TooSteep && expectedLen > 1e-5f)
                _velocity *= Mathf.Clamp01(moved.magnitude / expectedLen + 0.05f);
        }

        /// <summary>Uphill slows down (by the slope angle and how straight up we go), downhill speeds up a little.</summary>
        float SlopeFactor(Vector3 dir)
        {
            var s = settings;
            if (!OnGround || s.slopeLimit <= 0f) return 1f;
            Vector3 downhill = new Vector3(_groundNormal.x, 0f, _groundNormal.z);
            if (downhill.sqrMagnitude < 1e-6f) return 1f;
            float along = -Vector3.Dot(dir, downhill.normalized);         // +1 straight up the slope, -1 straight down
            float steep = Mathf.Clamp01(SlopeAngle / s.slopeLimit);
            return along >= 0f ? 1f - s.uphillSlowdown * steep * along : 1f + s.downhillSpeedup * steep * -along;
        }

        // ------------------------------------------------------------------ climbing a tall step
        /// <summary>
        /// Starts a climb when a wall is right in front, its top is higher than a plain step but within
        /// climbHeight, the top is walkable and there is room on it for the body.
        /// </summary>
        void TryStartClimb(Vector3 dir)
        {
            if (dir == Vector3.zero) return;
            var s = settings;
            Vector3 feet = transform.position;
            float r = _cc.radius;
            if (!Physics.Raycast(feet + Vector3.up * (_cc.stepOffset + 0.03f), dir, out var wall, r + 0.12f, Mask, NoTriggers)) return;
            if (Vector3.Angle(wall.normal, Vector3.up) < 60f) return;               // a slope, not a wall
            Vector3 into = -new Vector3(wall.normal.x, 0f, wall.normal.z).normalized;
            if (Vector3.Dot(dir, into) < 0.5f) return;                              // only when heading into it

            Vector3 probe = wall.point + into * 0.12f;
            probe.y = feet.y + s.climbHeight + 0.05f;
            if (Physics.CheckSphere(probe, 0.02f, Mask, NoTriggers)) return;        // taller than we can climb
            if (!Physics.Raycast(probe, Vector3.down, out var top, s.climbHeight + 0.05f, Mask, NoTriggers)) return;
            float h = top.point.y - feet.y;
            if (h <= _cc.stepOffset || h > s.climbHeight) return;
            if (Vector3.Angle(top.normal, Vector3.up) > s.slopeLimit) return;

            Vector3 c = top.point + into * (r + 0.05f);
            if (Physics.CheckCapsule(c + Vector3.up * (r + 0.03f), c + Vector3.up * (_cc.height - r + 0.03f), r * 0.9f, Mask, NoTriggers)) return;

            _climbing = true;
            _climbDir = into;
            _climbWallPoint = wall.point;
            _climbStartY = feet.y;
            _climbTopY = top.point.y;
            _climbProgress = 0f;
            _velocity = Vector3.zero;
            _vy = 0f;
        }

        /// <summary>Up the wall while forward is held (it sticks when let go), then over the edge. Back (S) lets go.</summary>
        void Climb(Vector3 dir, float wishAmount, float dt)
        {
            var s = settings;
            float push = Vector3.Dot(dir, _climbDir) * wishAmount;
            if (push < -0.3f) { _climbing = false; _vy = 0f; return; }
            push = Mathf.Max(0f, push);

            Vector3 feet = transform.position;
            Vector3 motion = feet.y < _climbTopY + 0.02f
                ? Vector3.up * (s.climbSpeed * speedMultiplier * push) + _climbDir * 0.05f     // up, hugging the wall
                : _climbDir * (s.climbSpeed * speedMultiplier * push);                          // over the edge
            _cc.Move(motion * dt);

            _climbProgress = Mathf.InverseLerp(_climbStartY, _climbTopY, transform.position.y);
            float past = Vector3.Dot(transform.position - _climbWallPoint, _climbDir);
            if (transform.position.y >= _climbTopY - 0.01f && past > 0.02f)
            {
                _climbing = false;
                _velocity = _climbDir * s.climbSpeed;
                _vy = -1f;
            }
        }

        // ------------------------------------------------------------------ ground and body
        /// <summary>
        /// The tail trails behind a turn: when the front turns, the tail first stays where it was (the body bends), then
        /// a soft spring draws it around after the front. Crawling makes it catch up sooner, as it follows the trail.
        /// </summary>
        void FollowTail(float turned, float dt)
        {
            var s = settings;
            _tailLag += turned;
            float w = 2f * Mathf.PI * Mathf.Max(0.05f, s.tailFollowHz + Speed * s.tailFollowPerSpeed);
            int n = Mathf.Clamp(Mathf.CeilToInt(dt * 120f), 1, 8);
            float h = dt / n;
            for (int i = 0; i < n; i++)
            {
                _tailLagVel += (-w * w * _tailLag - 2f * s.tailFollowDamping * w * _tailLagVel) * h;
                _tailLag += _tailLagVel * h;
            }
            // it bends only so far: past that the tail is dragged along
            if (Mathf.Abs(_tailLag) > s.tailMaxLag)
            {
                _tailLag = Mathf.Sign(_tailLag) * s.tailMaxLag;
                if (_tailLagVel * _tailLag > 0f) _tailLagVel = 0f;
            }
        }

        /// <summary>How far the front has turned ahead of the tail (degrees, + = clockwise seen from above).</summary>
        public float TailLag => _tailLag;

        void ProbeGround()
        {
            Vector3 origin = transform.position + Vector3.up * 0.3f;
            OnGround = Physics.Raycast(origin, Vector3.down, out var hit, 0.3f + _cc.stepOffset + 0.1f, Mask, NoTriggers);
            _groundNormal = OnGround ? hit.normal : Vector3.up;
            SlopeAngle = OnGround ? Vector3.Angle(_groundNormal, Vector3.up) : 0f;
        }

        /// <summary>Tilts the body to the ground (or up the wall while climbing), pivoting where the slug touches it.</summary>
        void AlignBody(float dt)
        {
            if (!body) return;
            Vector3 n = OnGround ? _groundNormal : Vector3.up;
            if (_climbing)
            {
                // rear up while going up, lie down again over the edge
                float angle = settings.climbTilt * Mathf.Sin(Mathf.PI * Mathf.Clamp01(_climbProgress * 0.9f + 0.1f));
                n = Vector3.RotateTowards(Vector3.up, -_climbDir, angle * Mathf.Deg2Rad, 0f);
            }
            Vector3 nLocal = Quaternion.Inverse(transform.rotation) * n;
            Quaternion target = Quaternion.FromToRotation(Vector3.up, nLocal);
            _tilt = Quaternion.Slerp(_tilt, target, 1f - Mathf.Exp(-settings.slopeAlignSpeed * dt));
            // a twist turns the whole body about the turning point, and the rear is swung back by as much (below), so
            // only the front comes round
            Quaternion twist = Quaternion.Euler(0f, twistDegrees, 0f);
            body.localRotation = _tilt * twist;
            body.localPosition = _tilt * (twist * _bodyBasePos);
            // shrinking makes it short, wide and low; stretching makes it long, thin and low
            float e = 1f + _stretch;
            var sc = _stretch < 0f ? new Vector3(Mathf.Pow(e, -0.55f), Mathf.Pow(e, 0.4f), e)
                                   : new Vector3(Mathf.Pow(e, -0.45f), Mathf.Pow(e, -0.45f), e);
            body.localScale = Vector3.Scale(_bodyBaseScale, sc);

            // bend the lifted front back by the nose-up pitch, so the upper body stays upright
            if (!_slug) _slug = body.GetComponent<SlugDesign.SlugPop>();
            if (_slug)
            {
                Vector3 f = _tilt * Vector3.forward;
                float pitchUp = Mathf.Atan2(f.y, f.z) * Mathf.Rad2Deg;
                // lying down: the front bends forward from the chest, flat to the ground
                float down = Prone01 * Prone01 * (3f - 2f * Prone01);
                _slug.SetProne(down);
                _slug.SetUpright(pitchUp * settings.uprightAmount + leanDegrees + settings.proneLean * down);
                _slug.SetTurn(_tailLag + twistDegrees, transform.position);     // the slug turns about this point
            }
        }
    }
}
