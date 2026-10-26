using System.Collections.Generic;
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
    /// Instead of a jump, the slug shrinks and stretches: press Jump to shrink a little and stretch forward in a low hop
    /// (pressed again and again, a little faster than crawling, and a little harder to hit), or hold it to shrink more
    /// (the body gets short, wide and low, and hardly moves: easy to hit) and let go to stretch further. It glides out
    /// on landing with a wobble, and it is heard. It is for getting about and out of a line of fire, not for climbing.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class SlugMoveController : MonoBehaviour
    {
        public SlugMoveSettings settings;
        public SlugCameraRig cameraRig;
        [Tooltip("SlugPop の付いた見た目。傾きはこのコンポーネントが回す")] public Transform body;
        [Tooltip("速度の倍率（荷物の重さなど、ほかのコンポーネントが変える）")] public float speedMultiplier = 1f;
        /// <summary>A second speed factor for the body's condition (dried out: slower). Set by the moisture.</summary>
        [System.NonSerialized] public float conditionSpeed = 1f;
        /// <summary>Development only (<c>DevFastCrawl</c>): crawls this many times faster, starting and stopping as quickly.</summary>
        [System.NonSerialized] public float devSpeed = 1f;
        /// <summary>How big the body looks (dried out: a little shrunk). Set by the moisture.</summary>
        [System.NonSerialized] public float bodyScale = 1f;
        /// <summary>0..1: how far a gun is brought up to look along its sights or through its scope. Set by the gun.</summary>
        [System.NonSerialized] public float aiming;
        float SpeedFactor => speedMultiplier * conditionSpeed * devSpeed * Mathf.Lerp(1f, settings.aimSpeedMultiplier, aiming);
        [Tooltip("false の間は入力を受け付けない（試合の終了後など）")] public bool inputEnabled = true;
        /// <summary>Moved by something else (a bot, later the network) through <see cref="Simulate"/>: no keys are read.</summary>
        [System.NonSerialized] public bool driven;
        [Tooltip("上半身の前後の傾き（度、+ = 前）。ピッケルを振る動きなど、ほかのコンポーネントが変える")] public float leanDegrees;
        /// <summary>How far the head is turned round on the neck to the right (degrees; the body stays put): looking
        /// back at the bag. Set by the bag.</summary>
        [System.NonSerialized] public float twistDegrees;
        /// <summary>Lying low (Z toggles it): the head sinks straight down (the neck squashes) and the arms move up toward
        /// it. Slow, and no stretching or climbing meanwhile.</summary>
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
        Vector3 _lastKey;       // the last direction of the move keys (relative to the eyes), kept a moment after letting go
        float _lastKeyAge, _kick, _kickCooldown;
        float _pressed;         // a press a moment too early (still shrinking back from the last one) is kept this long
        float _hold, _snap;     // after letting go: the shrunk shape is held this long, then the body snaps out on a stiff spring
        float _heldSquash, _pendingKick;
        Vector3 _bodyBaseScale = Vector3.one;

        // climbing a step taller than stepHeight
        bool _climbing;
        Vector3 _climbDir, _climbWallPoint;
        float _climbStartY, _climbTopY, _climbProgress;

        // the path the feet crawled (newest first, [0] = the feet now): the rear is laid along it (up a wall, over an edge)
        readonly List<Vector3> _trail = new List<Vector3>();

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
        /// <summary>Shrinking (Jump held, or a press still shrinking a little before it stretches).</summary>
        public bool Charging => _charging;
        /// <summary>Started shrinking (the press). The body snaps part of the way at once.</summary>
        public event System.Action Squeezed;
        /// <summary>The body stretched off the ground (how far it was charged, 0..1). Heard: the stretch and what is in the bag.</summary>
        public event System.Action<float> Lunged;
        /// <summary>Back on the ground after a stretch.</summary>
        public event System.Action Landed;

        /// <summary>Holds the Jump button without the keyboard (tests and tools).</summary>
        [System.NonSerialized] public bool forceJumpHold;

        void Awake()
        {
            _cc = GetComponent<CharacterController>();
            if (body) { _bodyBasePos = body.localPosition; _bodyBaseScale = body.localScale; }
            if (!GetComponent<SlugSplash>()) gameObject.AddComponent<SlugSplash>();     // slime flung off by the stretch
        }

        void OnEnable()
        {
            _move = InputSystem.actions != null ? InputSystem.actions.FindAction("Player/Move") : null;
            _move?.Enable();
            _jump = InputSystem.actions != null ? InputSystem.actions.FindAction("Player/Jump") : null;
            _jump?.Enable();
            // the crawl of every slug shader now follows CrawlTime instead of the clock
            if (!driven) Shader.SetGlobalFloat(FreezeId, 1f);
        }

        void OnDisable() { if (!driven) Shader.SetGlobalFloat(FreezeId, 0f); }

        void Update()
        {
            if (driven) return;
            Vector2 input = _move != null && inputEnabled ? Vector2.ClampMagnitude(_move.ReadValue<Vector2>(), 1f) : Vector2.zero;
            bool jump = forceJumpHold || (inputEnabled && _jump != null && (_jump.IsPressed() || _jump.WasPressedThisFrame()));
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
            Vector3 key = new Vector3(input.x * s.strafeMultiplier, 0f, forward);
            Vector3 wish = Quaternion.Euler(0f, yaw, 0f) * key;
            float wishAmount = Mathf.Min(wish.magnitude, 1f);
            Vector3 dir = wishAmount > 1e-4f ? wish.normalized : Vector3.zero;

            StepLunge(dt, jumpHeld, dir, yaw);
            if (_charging) wishAmount *= s.lungeChargeMove;
            wishAmount *= Mathf.Lerp(1f, s.proneSpeed, Prone01);
            StepTurnKick(key, dir, wishAmount, dt);

            Vector3 before = transform.position;
            if (!_climbing && !_lunging && !_charging && Prone01 <= 0f && OnGround && input.y > 0.1f) TryStartClimb(dir);
            if (_climbing) Climb(dir, wishAmount, dt);
            else Crawl(dir, wishAmount, dt);
            Speed = Mathf.Lerp(Speed, (transform.position - before).magnitude / dt, 1f - Mathf.Exp(-10f * dt));
            RecordTrail(before);

            // ---- the body turns after the eyes, a little late (and faces the wall while climbing)
            float targetYaw = _climbing ? Quaternion.LookRotation(_climbDir).eulerAngles.y : yaw;
            float prevYaw = transform.eulerAngles.y;
            // the further behind, the faster it catches up (a quick look round is followed at once), never slower than bodyTurnSpeed
            float behind = Mathf.Abs(Mathf.DeltaAngle(prevYaw, targetYaw));
            float turn = Mathf.Max(s.bodyTurnSpeed * dt, behind * (1f - Mathf.Exp(-dt / Mathf.Max(s.bodyTurnLag, 1e-3f))));
            float bodyYaw = Mathf.MoveTowardsAngle(prevYaw, targetYaw, turn);
            transform.rotation = Quaternion.Euler(0f, bodyYaw, 0f);
            FollowTail(Mathf.DeltaAngle(prevYaw, bodyYaw), dt);
            AlignBody(dt);

            // ---- the crawl wave advances with the distance travelled
            CrawlTime += dt * (s.idleCrawlRate + Speed * s.crawlRatePerSpeed);
            Shader.SetGlobalFloat(TimeId, CrawlTime);
        }

        // ------------------------------------------------------------------ shrink, then stretch forward
        /// <summary>
        /// Pressing Jump on the ground charges (the body shrinks); letting go throws the slug forward the way it is
        /// crawling (or the way it looks), a little up off the ground, faster the longer it was held. A mere press
        /// still shrinks a little (<see cref="SlugMoveSettings.lungeTapCharge"/>) before it stretches: pressed again
        /// and again while crawling, it goes a little faster than crawling, dodging a little as it goes.
        /// </summary>
        void StepLunge(float dt, bool held, Vector3 dir, float yaw)
        {
            var s = settings;
            _cooldown = Mathf.Max(0f, _cooldown - dt);
            if (_charging && Prone01 > 0f) { _charging = false; _charge = 0f; }    // lying down: the shrink is let go of
            bool canCharge = !_climbing && !_lunging && OnGround && _cooldown <= 0f;
            float tapCharge = Mathf.Clamp01(s.lungeTapCharge / Mathf.Max(s.lungeChargeTime, 1e-3f));
            bool tapping = _charging && _charge < tapCharge && Prone01 <= 0f;     // let go early: it shrinks that little first
            _pressed = held && !_charging ? 0.2f : Mathf.Max(0f, _pressed - dt);
            if ((held || tapping || _pressed > 0f) && (_charging || canCharge))
            {
                _pressed = 0f;
                if (!_charging) { _hold = 0f; _pendingKick = 0f; Squeezed?.Invoke(); }
                _charging = true;
                _charge = Mathf.Min(1f, _charge + dt / Mathf.Max(s.lungeChargeTime, 1e-3f));
            }
            else if (_charging)
            {
                Vector3 d = dir != Vector3.zero ? dir : Quaternion.Euler(0f, yaw, 0f) * Vector3.forward;
                float c = ChargePower(_charge);
                // along the ground (up a slope it goes up the slope instead of into it), slower uphill as crawling is;
                // the development fast crawl is for getting about, not for stretching
                Vector3 along = OnGround ? Vector3.ProjectOnPlane(d, _groundNormal).normalized : d;
                _velocity = along * (Mathf.Lerp(s.lungeSpeedMin, s.lungeSpeedMax, c) * speedMultiplier * conditionSpeed * SlopeFactor(d));
                // the head keeps facing the way the eyes look (the eyes sit in it): a stretch along the body makes it
                // long; a stretch to the side flings the tail out the other way instead, trailing behind
                _lungeAlong = Mathf.Abs(Vector3.Dot(d, transform.forward));
                _tailLagVel += Vector3.Dot(d, transform.right) * 420f * Mathf.Lerp(0.5f, 1f, c);
                _vy = s.lungeUp * Mathf.Lerp(0.4f, 1f, c);
                _lunging = _airborne = true;
                _cooldown = s.lungeCooldown;
                // the body stays shrunk a moment (it already moves), then snaps out long
                _heldSquash = SquashTarget(_charge);
                _hold = s.lungeReleaseHold;
                _snap = s.lungeReleaseHold + 0.12f;
                _pendingKick = Mathf.Lerp(0.5f, 1f, c) * Mathf.Lerp(0.3f, 1f, _lungeAlong) * s.lungeStretch * 2f * Mathf.PI * s.lungeSpringHz * 1.8f;
                _charging = false;
                _charge = 0f;
                Lunged?.Invoke(c);
            }

            // the body's length: short while charging (part of it at the press), long in the air, a wobble on landing
            bool holding = _hold > 0f;
            _hold -= dt;
            _snap = Mathf.Max(0f, _snap - dt);
            if (!holding && _pendingKick != 0f) { _stretchV += _pendingKick; _pendingKick = 0f; }
            float target = _charging ? SquashTarget(_charge)
                         : holding ? _heldSquash
                         : _airborne ? s.lungeStretch * 0.6f * Mathf.Lerp(0.3f, 1f, _lungeAlong) : 0f;
            float w = 2f * Mathf.PI * (_charging || _snap > 0f ? Mathf.Max(s.lungeSnapHz, s.lungeSpringHz) : s.lungeSpringHz);
            int n = Mathf.Clamp(Mathf.CeilToInt(dt * 240f), 1, 16);
            for (int i = 0; i < n; i++)
            {
                float h = dt / n;
                _stretchV += (w * w * (target - _stretch) - 2f * s.lungeSpringDamping * w * _stretchV) * h;
                _stretch += _stretchV * h;
            }
            _stretch = Mathf.Clamp(_stretch, -0.45f, 0.6f);
        }

        /// <summary>How short the body is while shrinking: part of it at the press (lungePressSnap), the rest with the charge.</summary>
        float SquashTarget(float charge) =>
            -settings.lungeSquash * Mathf.Lerp(settings.lungePressSnap, 1f, ChargePower(charge));

        /// <summary>
        /// How strong a stretch is after shrinking this long (a part of lungeChargeTime): most of it comes quickly
        /// (lungeQuickPower by lungeQuickTime: slowness is a sin), the rest by lungeChargeTime.
        /// </summary>
        float ChargePower(float charge)
        {
            var s = settings;
            float t = charge * s.lungeChargeTime, quick = Mathf.Clamp(s.lungeQuickTime, 1e-3f, s.lungeChargeTime);
            if (t <= quick) return s.lungeQuickPower * Mathf.Sin(t / quick * Mathf.PI * 0.5f);     // quick at first, easing in
            float rest = Mathf.Max(s.lungeChargeTime - quick, 1e-3f);
            return Mathf.Lerp(s.lungeQuickPower, 1f, Mathf.SmoothStep(0f, 1f, (t - quick) / rest));
        }

        // ------------------------------------------------------------------ turning back (a feint)
        /// <summary>
        /// Putting the move keys the other way while crawling (turnKickAngle or more, the keys themselves: turning the
        /// eyes alone does not count) skips the slow stop: the slug is at once going the new way at full speed, a little
        /// over it (turnKickSpeed, gone in turnKickTime), and the tail is flung out. At most once per turnKickCooldown,
        /// so zigzagging never gets anywhere faster than crawling straight.
        /// </summary>
        void StepTurnKick(Vector3 key, Vector3 dir, float wishAmount, float dt)
        {
            var s = settings;
            _kick = Mathf.Max(0f, _kick - dt / Mathf.Max(s.turnKickTime, 1e-3f));
            _kickCooldown = Mathf.Max(0f, _kickCooldown - dt);
            if (key.sqrMagnitude < 1e-6f)
            {
                _lastKey = (_lastKeyAge += dt) < 0.25f ? _lastKey : Vector3.zero;     // A, let go, D: still a turn
                return;
            }
            Vector3 prevKey = _lastKey;
            _lastKey = key.normalized;
            _lastKeyAge = 0f;
            if (prevKey == Vector3.zero || Vector3.Angle(prevKey, _lastKey) < s.turnKickAngle) return;
            if (_kickCooldown > 0f || _charging || _lunging || _airborne || !OnGround || Prone01 > 0f || aiming > 0f) return;
            float target = s.maxSpeed * SpeedFactor * wishAmount * SlopeFactor(dir);
            // only when it was crawling the old way, not starting from a standstill
            if (_velocity.magnitude < target * 0.4f || Vector3.Angle(_velocity, dir) < s.turnKickAngle * 0.7f) return;
            _kick = 1f;
            _kickCooldown = s.turnKickCooldown;
            _velocity = dir * ((s.maxSpeed + s.turnKickSpeed) * SpeedFactor * wishAmount * SlopeFactor(dir));
            _tailLagVel += Vector3.Dot(dir, transform.right) * s.turnKickTail;
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
                    float sp = _velocity.magnitude, floor = s.maxSpeed * SpeedFactor * wishAmount;
                    float next = Mathf.Max(floor, sp * Mathf.Exp(-dt / Mathf.Max(s.lungeSlideTime, 1e-3f)));
                    _velocity = sp > 1e-5f ? _velocity * (next / sp) : Vector3.zero;
                    if (next <= s.maxSpeed * SpeedFactor * 1.02f) _lunging = false;
                }
            }
            // speed: slow to build up, and a slippery stop (exponential glide + a small constant brake)
            else if (wishAmount > 1e-4f)
            {
                float target = (s.maxSpeed + s.turnKickSpeed * _kick) * SpeedFactor * wishAmount * SlopeFactor(dir);
                float accel = _charging ? s.lungeChargeBrake : s.acceleration;      // shrinking: it all but stops
                _velocity = Vector3.MoveTowards(_velocity, dir * target, accel * devSpeed * dt);
            }
            else
            {
                float sp = _velocity.magnitude;
                float next = Mathf.Max(0f, sp * Mathf.Exp(-dt * devSpeed / Mathf.Max(s.slideTime, 1e-3f)) - s.deceleration * devSpeed * dt);
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
                Landed?.Invoke();
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
            // the ray goes along the input, so it reaches further for a slanted wall (up to about 70 degrees off head-on)
            if (!Physics.Raycast(feet + Vector3.up * (_cc.stepOffset + 0.03f), dir, out var wall, (r + 0.12f) * 3f, Mask, NoTriggers)) return;
            if (Vector3.Angle(wall.normal, Vector3.up) < 60f) return;               // a slope, not a wall
            Vector3 into = -new Vector3(wall.normal.x, 0f, wall.normal.z).normalized;
            if (Vector3.Dot(dir, into) < 0.34f) return;                             // only when heading into it, even at a slant
            if (Vector3.Dot(wall.point - feet, into) > r + 0.12f) return;           // the wall itself is still a little way off

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

        /// <summary>
        /// Up the wall while forward is held (it sticks when let go), then over the edge. Back (S) lets go.
        /// Sideways input (A/D, or forward and sideways together) shuffles along the wall while on it,
        /// as long as the wall and a climbable top go on that way.
        /// </summary>
        void Climb(Vector3 dir, float wishAmount, float dt)
        {
            var s = settings;
            float push = Vector3.Dot(dir, _climbDir) * wishAmount;
            if (push < -0.3f) { _climbing = false; _vy = 0f; return; }
            push = Mathf.Max(0f, push);

            Vector3 feet = transform.position;
            bool onWall = feet.y < _climbTopY + 0.02f;
            Vector3 motion = onWall
                ? Vector3.up * (s.climbSpeed * SpeedFactor * push) + _climbDir * 0.05f     // up, hugging the wall
                : _climbDir * (s.climbSpeed * SpeedFactor * push);                          // over the edge

            Vector3 side = dir * wishAmount - _climbDir * Vector3.Dot(dir * wishAmount, _climbDir);
            side.y = 0f;
            if (onWall && side.sqrMagnitude > 1e-4f)
            {
                Vector3 sideMove = side * (s.climbSpeed * SpeedFactor);
                if (FollowWall(feet + sideMove * dt, dt)) motion += sideMove;
            }
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

        /// <summary>
        /// Is there still wall in front, with a climbable top, if the feet were at <paramref name="feet"/>?
        /// If so, the climb takes on that wall (its facing, slowly, and the height of its top).
        /// </summary>
        bool FollowWall(Vector3 feet, float dt)
        {
            var s = settings;
            float r = _cc.radius;
            Vector3 from = feet;
            from.y = Mathf.Min(feet.y + _cc.stepOffset + 0.03f, _climbTopY - 0.03f);     // below the top edge
            if (!Physics.Raycast(from, _climbDir, out var wall, r + 0.2f, Mask, NoTriggers)) return false;
            if (Vector3.Angle(wall.normal, Vector3.up) < 60f) return false;
            Vector3 into = -new Vector3(wall.normal.x, 0f, wall.normal.z).normalized;
            if (Vector3.Dot(into, _climbDir) < 0.5f) return false;                       // a sharp corner: stop there

            Vector3 probe = wall.point + into * 0.12f;
            probe.y = _climbStartY + s.climbHeight + 0.05f;
            if (Physics.CheckSphere(probe, 0.02f, Mask, NoTriggers)) return false;       // the wall gets too tall
            if (!Physics.Raycast(probe, Vector3.down, out var top, s.climbHeight + 0.05f, Mask, NoTriggers)) return false;
            if (top.point.y < feet.y + 0.02f) return false;                              // the top drops below us
            if (Vector3.Angle(top.normal, Vector3.up) > s.slopeLimit) return false;
            Vector3 c = top.point + into * (r + 0.05f);
            if (Physics.CheckCapsule(c + Vector3.up * (r + 0.03f), c + Vector3.up * (_cc.height - r + 0.03f), r * 0.9f, Mask, NoTriggers)) return false;

            _climbDir = Vector3.Slerp(_climbDir, into, 1f - Mathf.Exp(-8f * dt)).normalized;
            _climbWallPoint = wall.point;
            _climbTopY = top.point.y;
            return true;
        }

        // ------------------------------------------------------------------ ground and body
        /// <summary>
        /// Keeps the path the feet crawled, about a body length back, for the rear to follow. Off the ground (a stretch,
        /// a fall) it is forgotten and the rear stays straight. Shuffling sideways along a wall takes the path along, so
        /// the tail stays below rather than sticking out sideways.
        /// </summary>
        void RecordTrail(Vector3 before)
        {
            Vector3 pos = transform.position;
            if (!_climbing && (!OnGround || _airborne)) { _trail.Clear(); return; }
            if (_trail.Count == 0 || (pos - _trail[0]).sqrMagnitude > 0.25f) { _trail.Clear(); _trail.Add(pos); return; }     // a jump in place (respawn)
            Vector3 contact = pos;
            if (_climbing)
            {
                Vector3 side = pos - before;
                side -= _climbDir * Vector3.Dot(side, _climbDir);
                side.y = 0f;
                for (int i = 1; i < _trail.Count; i++) _trail[i] += side;
                // the feet go up a little way off the wall; the body touches the wall lower down, so the path goes
                // there (slanting in as the slug rises): the tail goes down the wall face and later over the very edge
                float gap = Mathf.Max(0f, Vector3.Dot(_climbWallPoint - pos, _climbDir));
                float w = Mathf.Clamp01((pos.y - _climbStartY) / Mathf.Max(gap, 0.05f));
                contact += (_climbDir + Vector3.down) * (gap * w);
                contact.y = Mathf.Max(contact.y, _climbStartY);
            }
            _trail[0] = pos;
            // backing up over it: the points now in front of the feet go (on a wall the path goes in front on purpose)
            while (_trail.Count >= 3 && (contact - _trail[2]).sqrMagnitude < (_trail[1] - _trail[2]).sqrMagnitude) _trail.RemoveAt(1);
            while (!_climbing && _trail.Count >= 2 && Vector3.Dot(_trail[1] - pos, transform.forward) > 0.005f) _trail.RemoveAt(1);
            if (_trail.Count == 1 || (contact - _trail[1]).sqrMagnitude >= 0.03f * 0.03f) _trail.Insert(1, contact);
            float len = 0f;
            for (int i = 1; i < _trail.Count; i++)
            {
                len += Vector3.Distance(_trail[i - 1], _trail[i]);
                if (len > 1f) { _trail.RemoveRange(i + 1, _trail.Count - i - 1); break; }
            }
        }

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
            body.localRotation = _tilt;
            body.localPosition = _tilt * _bodyBasePos;
            // shrinking makes it short, wide and low; stretching makes it long, thin and low
            float e = 1f + _stretch;
            var sc = _stretch < 0f ? new Vector3(Mathf.Pow(e, -0.55f), Mathf.Pow(e, 0.4f), e)
                                   : new Vector3(Mathf.Pow(e, -0.45f), Mathf.Pow(e, -0.45f), e);
            body.localScale = Vector3.Scale(_bodyBaseScale, sc) * bodyScale;

            // bend the lifted front back by the nose-up pitch, so the upper body stays upright
            if (!_slug) _slug = body.GetComponent<SlugDesign.SlugPop>();
            if (_slug)
            {
                Vector3 f = _tilt * Vector3.forward;
                float pitchUp = Mathf.Atan2(f.y, f.z) * Mathf.Rad2Deg;
                // lying down: the head sinks; looking at the bag: the head turns on the neck
                _slug.SetProne(Prone01 * Prone01 * (3f - 2f * Prone01));
                _slug.SetHeadTwist(twistDegrees);
                _slug.SetUpright(pitchUp * settings.uprightAmount + leanDegrees);
                _slug.SetTurn(_tailLag, transform.position);     // the slug turns about this point
                _slug.SetPath(transform.position, _trail);       // the rear follows the way it came (up a wall, over an edge)
            }
        }
    }
}
