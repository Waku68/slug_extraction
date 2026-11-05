using UnityEngine;

namespace SlugMove
{
    public enum SwingPhase { Idle, Windup, Strike, Impact, Follow, Recover }

    /// <summary>How a surface takes a pickaxe blow.</summary>
    public enum PickaxeMaterial { Default, Hard, Soft }

    /// <summary>Something that says what it is made of (salt rock = Hard, crop = Soft). Anything else counts as Default.</summary>
    public interface IPickaxeSurface
    {
        PickaxeMaterial PickaxeMaterial { get; }
    }

    /// <summary>Pose of the pickaxe: the bottom end of the handle and the rotation (handle along +Y, striking point along +Z).</summary>
    public struct ToolPose
    {
        public Vector3 position;
        public Quaternion rotation;

        public static readonly Vector3 GripLowLocal = new Vector3(0f, SlugPickaxe.GripLow, 0f);
        public static readonly Vector3 GripHighLocal = new Vector3(0f, SlugPickaxe.GripHigh, 0f);

        public Vector3 Point(Vector3 local) => position + rotation * local;
        public Vector3 GripLow => Point(GripLowLocal);
        public Vector3 GripHigh => Point(GripHighLocal);

        public static ToolPose AtGrip(Vector3 grip, Quaternion rotation)
            => new ToolPose { position = grip - rotation * GripLowLocal, rotation = rotation };

        /// <summary>Blends two poses about the right-hand grip (the hand moves in a line, the tool turns in it).</summary>
        public static ToolPose Lerp(ToolPose a, ToolPose b, float t)
            => AtGrip(Vector3.LerpUnclamped(a.GripLow, b.GripLow, t), Quaternion.SlerpUnclamped(a.rotation, b.rotation, t));

        public ToolPose Transformed(Transform space)
            => new ToolPose { position = space.TransformPoint(position), rotation = space.rotation * rotation };

        public ToolPose InverseTransformed(Transform space)
            => new ToolPose { position = space.InverseTransformPoint(position), rotation = Quaternion.Inverse(space.rotation) * rotation };
    }

    /// <summary>
    /// The swing of the pickaxe, simulated in the slug's own space (the root: position on the ground, yaw only).
    ///
    /// The tool turns in one plane about a pivot near the right shoulder: the plane holds the aimed point and a
    /// wind-up direction behind, above and to the right of the pivot. Its state is
    /// θ (angle in that plane, 0 = wound up), λ (arm length from the pivot to the right hand) and
    /// ψ (wrist: how far the head lags behind the arm). When the swing starts, λ and the contact angle are solved so
    /// the striking point lands exactly on the aimed point.
    ///
    /// Wind-up and strike follow fixed curves (ease in, so the head is fastest when it lands). From the first contact on,
    /// everything is springs: a hard surface throws the head back (restitution) and keeps it from going in, a soft one
    /// lets it sink and brakes it hard, the arm squashes and wobbles, the handle quivers. Recovery blends back to
    /// the ready pose with a critically damped spring.
    /// </summary>
    public class PickaxeSwing
    {
        /// <summary>Tip speed of a full-power hit (m/s). Shakes and wobbles scale with speed / this.</summary>
        public const float ReferenceSpeed = 25f;

        public PickaxeSettings settings;

        public SwingPhase Phase { get; private set; }
        public float PhaseTime { get; private set; }
        /// <summary>0 = the ready pose (or where the swing started), 1 = the simulated swing.</summary>
        public float Blend => _beta;
        /// <summary>0..1: how much the first-person view shows the true path (1 = the head lands on the aimed point on screen).</summary>
        public float FirstPersonTrue { get; private set; }
        /// <summary>
        /// 0..1: how far the upper hand has slid down the handle toward the lower one. Like a real pickaxe blow, the hands
        /// come together on the way down (1 at contact) and part again on the way back.
        /// </summary>
        public float HandSlide { get; private set; }
        /// <summary>0..1: how wound up the swing is (1 at the top of the wind-up and while the strike starts).</summary>
        public float WindupAmount => Phase == SwingPhase.Windup ? _beta : Phase == SwingPhase.Strike ? 1f : 0f;
        /// <summary>Lean of the upper body (degrees, + = forward).</summary>
        public float Lean => _lean;
        /// <summary>True while the head can hit things (strike and follow-through).</summary>
        public bool Sweeping => (Phase == SwingPhase.Strike && _u >= settings.sweepStart) || Phase == SwingPhase.Follow;
        public bool Frozen => _hitStop > 0f;

        // what the last swing aimed at and how it hit (root space)
        public bool HasTarget { get; private set; }
        public Vector3 TargetLocal { get; private set; }
        public Vector3 PivotLocal => _pivot;
        public float LastImpactSpeed { get; private set; }
        public PickaxeMaterial LastMaterial { get; private set; }
        public bool LastWasHit { get; private set; }

        // the swing plane (root space): pivot, axis, wound-up direction
        Vector3 _pivot, _axis = Vector3.right, _wind = Vector3.up;
        float _theta, _omega, _thetaContact, _thetaHit, _thetaLimit, _strikeFrom;
        float _lambda, _lambdaV, _lambdaContact, _lambdaHit;
        float _psi, _psiV;
        float _beta, _betaV;
        float _lean, _leanV;
        float _u, _hitStop, _restitution;
        PickaxeMaterial _material;
        ToolPose _fromSwing;
        float _fromBlend;
        float _thetaPrev, _lambdaPrev, _psiPrev;

        static Vector3 TipFromGrip => SlugPickaxe.TipLocal - ToolPose.GripLowLocal;

        /// <summary>Distance from the pivot to the striking point.</summary>
        float TipRadius
        {
            get { var g = TipFromGrip; return Mathf.Sqrt((_lambda + g.y) * (_lambda + g.y) + g.z * g.z); }
        }

        /// <summary>Speed of the striking point right now (m/s).</summary>
        public float TipSpeed => Mathf.Abs(_omega) * Mathf.Deg2Rad * TipRadius;

        /// <summary>
        /// How far this swing's hit reaches past the striking point (m), along the handle. 0 when the arms can bring the
        /// head itself to the target; up to hitReach when the target is a little beyond what the arms reach.
        /// </summary>
        public float HitExtension { get; private set; }

        /// <summary>Distance from the pivot that a blow reaches with this arm length and hit extension.</summary>
        public static float Reach(float arm, float extension)
        {
            var g = TipFromGrip;
            float along = arm + g.y + extension;
            return Mathf.Sqrt(along * along + g.z * g.z);
        }

        /// <summary>
        /// Remembers where the tool is right now (as a blend of the ready pose and the previous swing), so the next
        /// wind-up starts from there. Call once before <see cref="Begin"/>. The ready pose itself is given later, so the
        /// same swing can be shown from different ready poses (third person and first person).
        /// </summary>
        public void CaptureStart()
        {
            _fromSwing = SwingPoseLocal();
            _fromBlend = Phase == SwingPhase.Idle ? 0f : Mathf.Clamp01(_beta);
        }

        /// <summary>The swing angle of this swing (degrees, from wound up to the aimed point).</summary>
        public float Span { get; private set; }

        /// <summary>
        /// Starts a swing from the pose the tool has now (root space) toward the aimed point.
        /// <paramref name="spanScale"/> &lt; 1 winds up less high, for a flatter blow (to reach under something in the way).
        /// </summary>
        public void Begin(Vector3 pivotLocal, Vector3 targetLocal, bool hasTarget, float spanScale = 1f)
        {
            var s = settings;
            Span = s.swingSpan * spanScale;
            _pivot = pivotLocal;
            TargetLocal = targetLocal;
            HasTarget = hasTarget;
            LastWasHit = false;

            Vector3 toTarget = targetLocal - pivotLocal;
            float dist = toTarget.magnitude;
            Vector3 tau = dist > 1e-4f ? toTarget / dist : Vector3.forward;
            // a level axis across the aim, tilted about the aim so the wind-up comes from the right
            Vector3 level = Vector3.Cross(Vector3.up, tau);
            if (level.sqrMagnitude < 1e-4f) level = Vector3.right;           // aiming straight up or down
            _axis = Quaternion.AngleAxis(-s.swingSlant, tau) * level.normalized;
            _wind = Quaternion.AngleAxis(-Span, _axis) * tau;

            // contact: the arm length that brings the striking point to the target, and the angle where it lands.
            // The arms stretch no further than maxArm; what is left (up to hitReach) is covered by the hit alone,
            // which reaches past the head along the handle.
            Vector3 g = TipFromGrip;
            float along = Mathf.Sqrt(Mathf.Max(dist * dist - g.z * g.z, 0f)) - g.y;   // arm + extension needed
            _lambdaContact = Mathf.Clamp(along, s.minArm, s.maxArm);
            HitExtension = Mathf.Clamp(along - _lambdaContact, 0f, Mathf.Max(s.hitReach, 0f));
            float lead = Mathf.Atan2(g.z, _lambdaContact + g.y + HitExtension) * Mathf.Rad2Deg;   // the point runs ahead of the arm
            _thetaContact = Span - lead;

            _theta = 0f; _omega = 0f;
            _lambda = s.windupArm; _lambdaV = 0f;
            _psi = s.wristCock; _psiV = 0f;
            _beta = 0f; _betaV = 0f;
            _u = 0f; _hitStop = 0f;
            _thetaPrev = _theta; _lambdaPrev = _lambda; _psiPrev = _psi;
            SetPhase(SwingPhase.Windup);
        }

        /// <summary>Drops the swing at once (the pickaxe is put away): back to the ready pose, nothing pending.</summary>
        public void Cancel()
        {
            SetPhase(SwingPhase.Idle);
            _beta = 0f; _betaV = 0f;
            _lean = 0f; _leanV = 0f;
            _hitStop = 0f;
            FirstPersonTrue = 0f;
            HandSlide = 0f;
        }

        void SetPhase(SwingPhase p)
        {
            Phase = p;
            PhaseTime = 0f;
        }

        static float Smooth(float x)
        {
            x = Mathf.Clamp01(x);
            return x * x * (3f - 2f * x);
        }

        /// <summary>Damped spring toward a target (semi-implicit Euler; stable for the small steps used here).</summary>
        static void Spring(ref float x, ref float v, float target, float hz, float damping, float dt)
        {
            float w = 2f * Mathf.PI * hz;
            v += (w * w * (target - x) - 2f * damping * w * v) * dt;
            x += v * dt;
        }

        public void Step(float dt)
        {
            var s = settings;
            _thetaPrev = _theta; _lambdaPrev = _lambda; _psiPrev = _psi;
            if (Phase == SwingPhase.Idle)
            {
                Spring(ref _lean, ref _leanV, 0f, s.leanSpringHz, s.leanDamping, dt);
                FirstPersonTrue = 0f;
                HandSlide = 0f;
                return;
            }
            if (_hitStop > 0f)
            {
                _hitStop -= dt;      // the blow lands: everything holds still for a moment
                return;
            }
            PhaseTime += dt;

            switch (Phase)
            {
                case SwingPhase.Windup:
                {
                    float tw = Mathf.Max(s.windupTime, 1e-3f), total = tw + Mathf.Max(s.windupHold, 0f);
                    _beta = Smooth(PhaseTime / tw);
                    _theta = -s.anticipation * Smooth(PhaseTime / total);     // keeps pulling back a little while it holds
                    _lambda = s.windupArm;
                    _psi = s.wristCock;
                    _lean = -s.leanBack * Smooth(PhaseTime / tw);
                    _leanV = 0f;
                    FirstPersonTrue = 0f;
                    HandSlide = Mathf.Min(HandSlide, 1f - _beta);     // a chained swing parts the hands again
                    if (PhaseTime >= total)
                    {
                        _strikeFrom = _theta;
                        SetPhase(SwingPhase.Strike);
                    }
                    break;
                }
                case SwingPhase.Strike:
                {
                    float ts = Mathf.Max(s.strikeTime, 1e-3f), p = Mathf.Max(s.strikeEase, 1f);
                    _u = PhaseTime / ts;
                    if (_u < 1f)
                    {
                        float span = _thetaContact - _strikeFrom;
                        _theta = _strikeFrom + span * Mathf.Pow(_u, p);
                        _omega = span * p * Mathf.Pow(_u, p - 1f) / ts;
                        _lambda = Mathf.Lerp(s.windupArm, _lambdaContact, Smooth(_u));
                        _psi = s.wristCock * (1f - _u * _u * _u);              // the wrist lets go late: a whip
                        _lean = Mathf.Lerp(-s.leanBack, s.leanForward, Mathf.Pow(_u, 1.5f));
                        _beta = 1f;
                        FirstPersonTrue = Smooth(_u / s.fpStrikeBlend);
                        HandSlide = Smooth(_u / 0.85f);
                    }
                    else
                    {
                        // reached the aimed point and touched nothing: swing on through
                        _omega = (_thetaContact - _strikeFrom) * p / ts;
                        _lambdaV = 0f; _psiV = 0f; _leanV = 0f;
                        FirstPersonTrue = 1f;
                        HandSlide = 1f;
                        SetPhase(SwingPhase.Follow);
                    }
                    break;
                }
                case SwingPhase.Follow:
                    Spring(ref _theta, ref _omega, _thetaContact + s.followAngle, s.followSpringHz, s.followDamping, dt);
                    Spring(ref _lambda, ref _lambdaV, Mathf.Min(_lambdaContact + 0.05f, s.maxArm), s.armSpringHz, s.armDamping, dt);
                    Spring(ref _psi, ref _psiV, -8f, s.quiverHz, 0.4f, dt);
                    Spring(ref _lean, ref _leanV, s.leanForward * 1.25f, s.leanSpringHz, s.leanDamping, dt);
                    FirstPersonTrue = 1f;
                    if (PhaseTime >= s.followTime) StartRecover();
                    break;

                case SwingPhase.Impact:
                {
                    // the arms keep pressing toward the surface; the surface pushes back
                    float press = _material == PickaxeMaterial.Soft ? _thetaLimit : _thetaHit + 1f;
                    Spring(ref _theta, ref _omega, press, s.impactSpringHz, s.impactDamping, dt);
                    if (_material == PickaxeMaterial.Soft && _theta > _thetaHit)
                        _omega *= Mathf.Exp(-40f * dt);                         // thick and sticky inside
                    if (_theta > _thetaLimit)
                    {
                        _theta = _thetaLimit;
                        if (_omega > 0f) _omega = _material == PickaxeMaterial.Soft ? 0f : -_restitution * _omega;  // a smaller bounce again
                    }
                    Spring(ref _lambda, ref _lambdaV, _lambdaHit, s.armSpringHz, s.armDamping, dt);
                    Spring(ref _psi, ref _psiV, 0f, s.quiverHz, s.quiverDamping, dt);
                    Spring(ref _lean, ref _leanV, s.leanForward * 0.6f, s.leanSpringHz, s.leanDamping, dt);
                    FirstPersonTrue = 1f;
                    if (PhaseTime >= s.impactTime) StartRecover();
                    break;
                }
                case SwingPhase.Recover:
                    Spring(ref _beta, ref _betaV, 0f, s.recoverFrequency, 1f, dt);
                    _omega *= Mathf.Exp(-10f * dt);
                    _theta += _omega * dt;
                    Spring(ref _lambda, ref _lambdaV, _lambdaHit, s.armSpringHz, s.armDamping, dt);
                    Spring(ref _psi, ref _psiV, 0f, s.quiverHz, s.quiverDamping, dt);
                    Spring(ref _lean, ref _leanV, 0f, s.leanSpringHz, s.leanDamping, dt);
                    FirstPersonTrue = Mathf.Clamp01(_beta);
                    HandSlide = Mathf.Clamp01(_beta);
                    if (_beta < 0.005f && Mathf.Abs(_betaV) < 0.05f)
                    {
                        _beta = 0f;
                        SetPhase(SwingPhase.Idle);
                    }
                    break;
            }
        }

        void StartRecover()
        {
            _lambdaHit = _lambda;
            _betaV = 0f;
            SetPhase(SwingPhase.Recover);
        }

        /// <summary>
        /// The head touched something during the last step, at <paramref name="fraction"/> of the way through it.
        /// </summary>
        public void Impact(float fraction, PickaxeMaterial material)
        {
            var s = settings;
            _theta = Mathf.Lerp(_thetaPrev, _theta, fraction);
            _lambda = Mathf.Lerp(_lambdaPrev, _lambda, fraction);
            _psi = Mathf.Lerp(_psiPrev, _psi, fraction);

            float omegaIn = Mathf.Max(_omega, 0f);
            float speed = omegaIn * Mathf.Deg2Rad * TipRadius;
            float k = Mathf.Clamp01(speed / ReferenceSpeed);
            LastImpactSpeed = speed;
            LastMaterial = material;
            LastWasHit = true;

            _material = material;
            _thetaHit = _theta;
            _lambdaHit = _lambda;
            switch (material)
            {
                case PickaxeMaterial.Hard:
                    _restitution = s.hardRestitution; _hitStop = s.hardHitStop; _thetaLimit = _thetaHit;
                    break;
                case PickaxeMaterial.Soft:
                    _restitution = 0f; _hitStop = s.softHitStop;
                    _thetaLimit = _thetaHit + s.softPenetration / Mathf.Max(TipRadius, 0.1f) * Mathf.Rad2Deg;
                    break;
                default:
                    _restitution = s.otherRestitution; _hitStop = s.otherHitStop; _thetaLimit = _thetaHit;
                    break;
            }
            _omega = material == PickaxeMaterial.Soft ? omegaIn * 0.35f : -_restitution * omegaIn;
            _lambdaV = -s.armSquish * k * (material == PickaxeMaterial.Soft ? 0.4f : 1f);
            _psiV = s.handleQuiver * k * (material == PickaxeMaterial.Soft ? 0.4f : 1f);
            _beta = 1f;
            FirstPersonTrue = 1f;
            HandSlide = 1f;
            SetPhase(SwingPhase.Impact);
        }

        /// <summary>The pose the swing state describes (root space), ignoring the blend.</summary>
        public ToolPose SwingPoseLocal() => PoseAt(_theta, _lambda, _psi);

        ToolPose PoseAt(float theta, float lambda, float psi)
        {
            Vector3 radial = Quaternion.AngleAxis(theta, _axis) * _wind;
            Vector3 travel = Quaternion.AngleAxis(theta + 90f, _axis) * _wind;
            Quaternion wrist = Quaternion.AngleAxis(-psi, _axis);
            return ToolPose.AtGrip(_pivot + radial * lambda, Quaternion.LookRotation(wrist * travel, wrist * radial));
        }

        /// <summary>
        /// Where the strike will put the tool at progress <paramref name="u"/> (0 = top of the wind-up, 1 = the aimed point,
        /// beyond 1 = carrying on at the same speed). Used to find in advance what the head will hit.
        /// </summary>
        public ToolPose StrikePoseLocal(float u)
        {
            var s = settings;
            float lambda = Mathf.Lerp(s.windupArm, _lambdaContact, Smooth(u));
            float psi = s.wristCock * (1f - Mathf.Pow(Mathf.Clamp01(u), 3f));
            return PoseAt(StrikeAngle(u), lambda, psi);
        }

        /// <summary>The swing angle at strike progress <paramref name="u"/> (as in <see cref="StrikePoseLocal"/>).</summary>
        public float StrikeAngle(float u)
        {
            var s = settings;
            float p = Mathf.Max(s.strikeEase, 1f), from = -s.anticipation, span = _thetaContact - from;
            return u <= 1f ? from + span * Mathf.Pow(Mathf.Max(u, 0f), p) : _thetaContact + span * p * (u - 1f);
        }

        /// <summary>The pose to show (root space): the swing blended with where it started or the ready pose.</summary>
        public ToolPose PoseLocal(ToolPose restLocal)
        {
            if (Phase == SwingPhase.Idle) return restLocal;
            // a wind-up starts from where the tool was (part way back from the previous swing when chained)
            var from = Phase == SwingPhase.Windup ? ToolPose.Lerp(restLocal, _fromSwing, _fromBlend) : restLocal;
            return ToolPose.Lerp(from, PoseAt(_theta - DrawnWindExtra, _lambda, _psi), Mathf.Clamp01(_beta));
        }

        /// <summary>
        /// How much further back than the swing itself the tool is drawn (degrees): wound deeper over the wind-up and
        /// unwound early in the strike. Only the look: the head's path that is swept for hits is the swing's own.
        /// </summary>
        float DrawnWindExtra
        {
            get
            {
                var s = settings;
                if (Phase == SwingPhase.Windup) return s.windupExtra * Smooth(PhaseTime / Mathf.Max(s.windupTime, 1e-3f));
                if (Phase == SwingPhase.Strike)
                    return s.windupExtra * (1f - Smooth(PhaseTime / Mathf.Max(s.strikeTime * s.windupExtraUnwind, 1e-3f)));
                return 0f;
            }
        }

        /// <summary>
        /// How far round the upper body is turned by the swing (for the body to follow it; nothing here depends on it):
        /// +1 at the top of the wind-up, 0 where the head meets the aimed point, below 0 carried on through. Blended
        /// in and out with the swing.
        /// </summary>
        public float BodyTurn
        {
            get
            {
                if (Phase == SwingPhase.Idle) return 0f;
                float from = -settings.anticipation;
                float p = (_theta - from) / Mathf.Max(_thetaContact - from, 1f);
                return Mathf.Clamp01(_beta) * Mathf.Clamp(1f - p, -1f, 1f);
            }
        }

        // for debug read-outs and tests
        public Vector3 WindLocal => _wind;
        public Vector3 AxisLocal => _axis;
        public float Theta => _theta;
        public float Omega => _omega;
        public float ContactAngle => _thetaContact;
        public float Arm => _lambda;
        public float Wrist => _psi;
    }
}
