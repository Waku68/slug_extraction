using SlugDesign;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SlugMove
{
    /// <summary>Something the pickaxe can strike (a salt rock, a crop...).</summary>
    public interface IPickaxeTarget
    {
        void OnPickaxeHit(GameObject hitter, RaycastHit hit);
    }

    /// <summary>
    /// The slug's pickaxe, held in both hands on stretchy arms, swung with the Attack button (hold to keep swinging).
    ///
    /// One swing is simulated in the world (<see cref="PickaxeSwing"/>): wind up behind and to the right, bring the head
    /// down on the point under the crosshair, and stop where the head really touches something (the head's path is
    /// swept against the colliders every small step). Third person shows that pickaxe and the arms as they are.
    /// First person draws the same pose shrunk toward the eye (so it never pokes into walls, yet lands on the same
    /// spot on screen) and, while the slug holds still, shifted to a comfortable place in view.
    /// Taken in hand, it comes off the back (<see cref="SlugToolRack"/>): the right hand reaches over the shoulder for it,
    /// brings it round, and the left hand takes the end of the handle; no swing until it is in hand.
    /// Runs after the camera rig so the first-person copy follows this frame's camera.
    /// </summary>
    [DefaultExecutionOrder(100)]
    public class SlugToolHolder : MonoBehaviour
    {
        public PickaxeSettings pickaxe;
        public SlugMoveController controller;
        public SlugCameraRig cameraRig;
        public SlugPop slug;
        [Tooltip("false の間は振れない（試合の終了後など）")] public bool inputEnabled = true;
        /// <summary>How long the next draw takes (set by the loadout before enabling; 0 or less = at once).</summary>
        [System.NonSerialized] public float drawTime = -1f;
        /// <summary>0..1: how far the pickaxe has been taken in hand (1 = ready to swing).</summary>
        public float DrawProgress => _draw;

        /// <summary>Every blow that lands on something (target is null for plain ground and walls).</summary>
        public event System.Action<RaycastHit, IPickaxeTarget> Struck;
        /// <summary>A swing starts (debug drawing).</summary>
        public event System.Action SwingStarted;

        public PickaxeSwing Swing => _swing;
        /// <summary>The striking point in the world, this frame.</summary>
        public Vector3 TipWorld { get; private set; }
        public RaycastHit LastHit { get; private set; }

        const float SubStep = 1f / 240f;
        static readonly float[] SpanScales = { 1f, 0.82f, 0.66f, 0.5f };

        readonly PickaxeSwing _swing = new PickaxeSwing();
        InputAction _attack;
        SlugPickaxe _tool, _viewTool;
        SlugArmSet _arms;
        SlugToolRack _rack;
        Renderer[] _viewRenderers = new Renderer[0], _bodyRenderers = new Renderer[0];
        bool _ready;
        float _draw = 1f;

        /// <summary>
        /// Layer of the tools and arms on the body. For the player's own camera in first person they only cast their
        /// shadow (that camera shows its own first-person copy instead); every other camera sees them as usual.
        /// </summary>
        public const int BodyToolLayer = 31;

        Transform Root => controller.transform;

        void OnEnable()
        {
            _attack = InputSystem.actions != null ? InputSystem.actions.FindAction("Player/Attack") : null;
            _attack?.Enable();
            UnityEngine.Rendering.RenderPipelineManager.beginCameraRendering += OnBeginCamera;
            _draw = drawTime > 0f ? 0f : 1f;
            if (_ready) Show(true);
        }

        /// <summary>Put away (another tool taken in hand): the swing is dropped and the pickaxe and its arms are hidden.</summary>
        void OnDisable()
        {
            UnityEngine.Rendering.RenderPipelineManager.beginCameraRendering -= OnBeginCamera;
            if (!_ready) return;
            _swing.Cancel();
            if (controller) controller.leanDegrees = 0f;
            Show(false);
        }

        void Show(bool on)
        {
            if (_tool) _tool.gameObject.SetActive(on);
            _arms.ShowBody(on);
            if (on) return;
            if (_viewTool) _viewTool.gameObject.SetActive(false);
            _arms.ShowView(false);
        }

        /// <summary>The first-person copy is drawn only by the player's own camera, never by the Scene view or others.</summary>
        void OnBeginCamera(UnityEngine.Rendering.ScriptableRenderContext context, Camera camera)
        {
            if (!_ready) return;
            bool own = camera == cameraRig.cam;
            foreach (var r in _viewRenderers) if (r) r.enabled = own;
            // the body's pickaxe and arms only cast their shadow for the own camera in first person
            var mode = own && cameraRig.OwnToolsHidden ? UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly : UnityEngine.Rendering.ShadowCastingMode.On;
            foreach (var r in _bodyRenderers) if (r) r.shadowCastingMode = mode;
        }

        void Start()
        {
            _swing.settings = pickaxe;
            _rack = GetComponent<SlugToolRack>();
            _tool = SlugPickaxe.Create("Pickaxe", null, true);
            _viewTool = SlugPickaxe.Create("ViewPickaxe", cameraRig.cam.transform, false);

            // the arms replace the short cone hands: they stretch from the same spots on the body
            _arms = SlugArmSet.Create("", slug, cameraRig.cam.transform, pickaxe.fpScale);
            SlugBodyPart.Mark(_tool.gameObject, slug.transform);
            var views = new System.Collections.Generic.List<Renderer>(_viewTool.GetComponentsInChildren<Renderer>(true));
            views.AddRange(_arms.ViewRenderers);
            _viewRenderers = views.ToArray();
            var bodies = new System.Collections.Generic.List<Renderer>(_tool.GetComponentsInChildren<Renderer>(true));
            bodies.AddRange(_arms.BodyRenderers);
            _bodyRenderers = bodies.ToArray();
            _ready = true;
        }

        void OnDestroy()
        {
            if (_tool) Destroy(_tool.gameObject);
            _arms?.Destroy();
        }

        // ------------------------------------------------------------------ update: input and the simulation
        void Update()
        {
            if (!_ready) return;
            _swing.settings = pickaxe;
            if (_draw < 1f) _draw = Mathf.MoveTowards(_draw, 1f, Time.deltaTime / Mathf.Max(drawTime, 1e-3f));
            bool canSwing = inputEnabled && _draw >= 1f && !cameraRig.HandsBusy && !controller.Climbing && Cursor.lockState == CursorLockMode.Locked;
            bool held = canSwing && _attack != null && _attack.IsPressed();
            if (held && (_swing.Phase == SwingPhase.Idle || (_swing.Phase == SwingPhase.Recover && _swing.Blend < pickaxe.chainAt)))
                BeginSwing();
            Simulate(Time.deltaTime);
        }

        /// <summary>Advances the swing by <paramref name="dt"/>, sweeping the head's path against the world.</summary>
        public void Simulate(float dt)
        {
            // small fixed steps: the springs stay stable and the head's path is swept finely enough to catch thin things
            int n = Mathf.Clamp(Mathf.CeilToInt(dt / SubStep), 1, 32);
            float h = dt / n;
            for (int i = 0; i < n; i++)
            {
                bool sweeping = _swing.Sweeping && !_swing.Frozen;
                ToolPose before = sweeping ? _swing.SwingPoseLocal().Transformed(Root) : default;
                _swing.Step(h);
                if (!sweeping || !_swing.Sweeping) continue;
                ToolPose after = _swing.SwingPoseLocal().Transformed(Root);
                if (SweepHead(before, after, out var hit, out float fraction, out var drawn))
                    Land(hit, fraction, drawn);
            }
            controller.leanDegrees = _swing.Lean;
        }

        void BeginSwing()
        {
            var cam = cameraRig.cam.transform;
            Vector3 eye = cameraRig.ShowsBody ? slug.FaceCenterWorld : cam.position;
            // where the eyes look now (the camera transform is from the last frame and carries bob and shake)
            Vector3 dir = Quaternion.Euler(cameraRig.Pitch, cameraRig.Yaw, 0f) * Vector3.forward;
            Vector3 pivot = ShoulderMid() + Root.rotation * pickaxe.pivotOffset;
            float reach = PickaxeSwing.Reach(pickaxe.maxArm, pickaxe.hitReach);

            bool has = RaycastOthers(eye, dir, Vector3.Distance(eye, pivot) + reach, out var hit) && Vector3.Distance(hit.point, pivot) <= reach;
            Vector3 target;
            if (has) target = hit.point;
            else
            {
                target = eye + dir * pickaxe.missDistance;
                Vector3 off = target - pivot;
                if (off.magnitude > reach) target = pivot + off.normalized * reach;
            }
            // The path aims a little inside the surface, so the head meets it going in (not sliding down a wall).
            // An overhead blow can meet something on the way (the top of a tall crystal when the base is aimed at):
            // then wind up less high, for a flatter blow, until the head lands near the aimed point.
            AimPoint = target;
            Vector3 solveFor = has ? target - hit.normal * pickaxe.aimDepth : target;
            Vector3 pivotLocal = Root.InverseTransformPoint(pivot), targetLocal = Root.InverseTransformPoint(solveFor);
            float bestScale = 1f, bestMiss = float.MaxValue;
            _swing.CaptureStart();
            foreach (float spanScale in SpanScales)
            {
                _swing.Begin(pivotLocal, targetLocal, has, spanScale);
                PredictContact();
                if (!has || !HasPredictedContact) { bestScale = spanScale; break; }
                float miss = Vector3.Distance(ExpectedContactWorld, target);
                if (miss < bestMiss) { bestMiss = miss; bestScale = spanScale; }
                if (miss <= pickaxe.aimTolerance) break;
            }
            if (!Mathf.Approximately(bestScale, _swing.Span / Mathf.Max(pickaxe.swingSpan, 1e-3f)))
            {
                _swing.Begin(pivotLocal, targetLocal, has, bestScale);
                PredictContact();
            }
            SwingStarted?.Invoke();
        }

        /// <summary>
        /// Runs the strike ahead of time to find what the head will touch first. The aimed point is on the surface
        /// under the crosshair, but an overhead blow can meet something on the way (the top of a rock aimed at low).
        /// First person draws the swing so that it lands on this point.
        /// </summary>
        void PredictContact()
        {
            HasPredictedContact = false;
            const int steps = 48;
            float uPrev = pickaxe.sweepStart;
            ToolPose prev = _swing.StrikePoseLocal(uPrev).Transformed(Root);
            for (int i = 1; i <= steps; i++)
            {
                float uNext = Mathf.Lerp(pickaxe.sweepStart, 1.4f, i / (float)steps);
                ToolPose next = _swing.StrikePoseLocal(uNext).Transformed(Root);
                if (SweepHead(prev, next, out var hit, out float f, out var drawn))
                {
                    _contactLocal = Root.InverseTransformPoint(hit.point);
                    // where the drawn head will be at that moment (short of the surface when only the hit reaches it)
                    _contactDrawnLocal = Root.InverseTransformPoint(Vector3.Lerp(prev.Point(drawn), next.Point(drawn), f));
                    _contactGripLocal = Root.InverseTransformPoint(Vector3.Lerp(prev.GripLow, next.GripLow, f));
                    _contactTheta = Mathf.Lerp(_swing.StrikeAngle(uPrev), _swing.StrikeAngle(uNext), f);
                    HasPredictedContact = true;
                    return;
                }
                prev = next;
                uPrev = uNext;
            }
        }

        /// <summary>The point under the crosshair when the last swing started (world), or where an empty swing goes.</summary>
        public Vector3 AimPoint { get; private set; }

        /// <summary>Where the head is expected to land (world), or the aimed point when it will touch nothing.</summary>
        public Vector3 ExpectedContactWorld => Root.TransformPoint(HasPredictedContact ? _contactLocal : _swing.TargetLocal);
        public bool HasPredictedContact { get; private set; }
        /// <summary>How far the drawn head stayed from the surface on the last blow (m): 0 when the head itself touched.</summary>
        public float LastGap { get; private set; }
        Vector3 _contactLocal, _contactDrawnLocal, _contactGripLocal;
        float _contactTheta;

        /// <summary>
        /// Sweeps the head from one step to the next: its striking point and its middle, and, when this swing needs it,
        /// a point past the striking point along the handle (the hit reaching further than the head).
        /// <paramref name="drawn"/> is the point of the drawn head that stands for the contact.
        /// </summary>
        bool SweepHead(ToolPose a, ToolPose b, out RaycastHit best, out float fraction, out Vector3 drawn)
        {
            best = default;
            fraction = 1f;
            drawn = SlugPickaxe.TipLocal;
            bool found = false;
            float ext = _swing.HitExtension;
            int count = ext > 1e-4f ? 3 : 2;
            for (int k = 0; k < count; k++)
            {
                Vector3 local = k == 0 ? SlugPickaxe.TipLocal : k == 1 ? SlugPickaxe.HeadLocal : SlugPickaxe.TipLocal + Vector3.up * ext;
                Vector3 p0 = a.Point(local), p1 = b.Point(local);
                Vector3 d = p1 - p0;
                float len = d.magnitude;
                if (len < 1e-6f) continue;
                foreach (var h in Physics.SphereCastAll(p0, pickaxe.tipRadius, d / len, len, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                {
                    if (IsOwn(h.collider) || (h.distance <= 0f && h.point == Vector3.zero)) continue;   // own body, or already touching
                    float f = h.distance / len;
                    if (found && f >= fraction) continue;
                    best = h; fraction = f; found = true;
                    drawn = k == 1 ? SlugPickaxe.HeadLocal : SlugPickaxe.TipLocal;
                }
            }
            return found;
        }

        void Land(RaycastHit hit, float fraction, Vector3 drawn)
        {
            var target = hit.collider.GetComponentInParent<IPickaxeTarget>();
            var surface = hit.collider.GetComponentInParent<IPickaxeSurface>();
            var material = surface != null ? surface.PickaxeMaterial : PickaxeMaterial.Default;
            _swing.Impact(fraction, material);
            LastHit = hit;
            Vector3 point = _swing.SwingPoseLocal().Transformed(Root).Point(drawn);
            LastGap = Mathf.Max(0f, Vector3.Distance(hit.point, point) - pickaxe.tipRadius);

            target?.OnPickaxeHit(gameObject, hit);
            if (target == null) PickaxeEffects.Chips(hit.point, hit.normal, new Color(0.55f, 0.55f, 0.55f), 4, 0.8f);
            float power = Mathf.Clamp01(_swing.LastImpactSpeed / PickaxeSwing.ReferenceSpeed);
            cameraRig.Shake(pickaxe.shake * power * (material == PickaxeMaterial.Soft ? 0.45f : 1f), pickaxe.shakeDuration);
            Struck?.Invoke(hit, target);
        }

        bool RaycastOthers(Vector3 origin, Vector3 dir, float range, out RaycastHit best)
        {
            best = default;
            float bestDist = float.MaxValue;
            foreach (var h in Physics.RaycastAll(origin, dir, range, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            {
                if (IsOwn(h.collider) || h.distance >= bestDist) continue;
                best = h;
                bestDist = h.distance;
            }
            return bestDist < float.MaxValue;
        }

        bool IsOwn(Collider c) => c.transform.IsChildOf(Root);

        Vector3 ShoulderMid() => (slug.HandRootWorld(-1f) + slug.HandRootWorld(1f)) * 0.5f;

        /// <summary>
        /// First-person ready pose (only for the player's own first-person view): carried by the shoulders, turned with
        /// the yaw and part of the camera pitch, so it sits on the right side of the view.
        /// </summary>
        ToolPose RestFirstPerson()
        {
            Quaternion aim = Root.rotation * Quaternion.Euler(cameraRig.Pitch * pickaxe.aimPitchFollow, 0f, 0f);
            Vector3 handle = (aim * pickaxe.restHandleDir).normalized;
            Vector3 head = Vector3.ProjectOnPlane(aim * pickaxe.restHeadDir, handle);
            if (head.sqrMagnitude < 1e-6f) head = Vector3.ProjectOnPlane(aim * Vector3.forward, handle);
            return ToolPose.AtGrip(ShoulderMid() + aim * pickaxe.restGrip, Quaternion.LookRotation(head.normalized, handle));
        }

        /// <summary>
        /// The body's ready pose (what everyone else sees, in any view): both hands just in front of where the arms grow
        /// (the arms drawn in), the handle running between them across the chest, rising to the right so the head sits
        /// above the right hand.
        /// </summary>
        ToolPose RestThirdPerson(out float upperHand)
        {
            var s = pickaxe;
            Quaternion body = Root.rotation;
            Vector3 left = slug.HandRootWorld(-1f) + body * s.tpLeftHand;
            Vector3 right = slug.HandRootWorld(1f) + body * s.tpRightHand;
            Vector3 along = right - left;
            float dist = along.magnitude;
            Vector3 handle = dist > 1e-4f ? along / dist : body * Vector3.right;
            upperHand = Mathf.Clamp(SlugPickaxe.GripLow + dist, SlugPickaxe.GripLow + s.handsTogetherGap, SlugPickaxe.HandleLength - 0.06f);
            Vector3 head = Vector3.ProjectOnPlane(body * s.tpHeadDir, handle);
            if (head.sqrMagnitude < 1e-6f) head = Vector3.ProjectOnPlane(body * Vector3.forward, handle);
            return ToolPose.AtGrip(left, Quaternion.LookRotation(head.normalized, handle));
        }

        // ------------------------------------------------------------------ late update: show it
        void LateUpdate()
        {
            if (!_ready) return;
            var s = pickaxe;
            bool firstPerson = !cameraRig.ShowsBody;
            float together = SlugPickaxe.GripLow + s.handsTogetherGap;

            // ---- the body's pickaxe and arms: always the third-person ready pose, seen by every camera except the
            //      player's own in first person (that one shows the first-person copy below)
            ToolPose bodyRest = RestThirdPerson(out float tpUpper);
            ToolPose pose = _swing.PoseLocal(bodyRest.InverseTransformed(Root)).Transformed(Root);
            // taken in hand: the right hand reaches it on the back, then brings it round to the ready pose
            var ls = _rack && _rack.loadout ? _rack.loadout.settings : null;
            ToolPose stow = default;
            bool drawing = ls && _draw < 1f && _rack.TryStowPose(this, out stow);
            float reachPart = ls ? ls.reachPart : 0.4f;
            float dReach = drawing ? Ease(_draw / Mathf.Max(reachPart, 1e-3f)) : 1f;
            float dPull = drawing ? Ease((_draw - reachPart) / Mathf.Max(1f - reachPart, 1e-3f)) : 1f;
            if (drawing) pose = ToolPose.Lerp(stow, pose, dPull);
            TipWorld = pose.Point(SlugPickaxe.TipLocal);
            _tool.transform.SetPositionAndRotation(pose.position, pose.rotation);
            _tool.SetMode(true, false);
            Vector3 handle = pose.rotation * Vector3.up;
            // a right-handed hold: the left hand at the end of the handle, the right hand up by the head.
            // The right hand slides down to the left one on the blow and back up afterwards.
            Vector3 rightGrip = pose.Point(new Vector3(0f, Mathf.Lerp(tpUpper, together, _swing.HandSlide), 0f));
            Vector3 leftGripBody = pose.GripLow;
            if (drawing)
            {
                Quaternion body = Root.rotation;
                rightGrip = Vector3.Lerp(slug.HandRootWorld(1f) + body * new Vector3(-0.01f, -0.01f, 0.05f), rightGrip, dReach);
                leftGripBody = Vector3.Lerp(slug.HandRootWorld(-1f) + body * new Vector3(0.01f, -0.01f, 0.05f), leftGripBody, dPull);
            }
            BuildArm(_arms.BodyR, slug.HandRootWorld(1f), slug.HandRootNormalWorld(1f), rightGrip, handle, 1f, 1f);
            BuildArm(_arms.BodyL, slug.HandRootWorld(-1f), slug.HandRootNormalWorld(-1f), leftGripBody, handle, 1f, 1f);
            var cam = cameraRig.cam;

            // ---- first person: the same swing from the first-person ready pose, shrunk toward the eye and
            //      (while ready) shifted into view
            _viewTool.gameObject.SetActive(firstPerson);
            _arms.ShowView(firstPerson);
            if (!firstPerson) return;
            pose = _swing.PoseLocal(RestFirstPerson().InverseTransformed(Root)).Transformed(Root);
            handle = pose.rotation * Vector3.up;
            rightGrip = pose.Point(new Vector3(0f, Mathf.Lerp(s.restUpperHand, together, _swing.HandSlide), 0f));
            Vector3 leftGrip = pose.GripLow;
            // While ready: shrunk toward the eye and shifted into a comfortable place in view.
            // While striking: drawn about the point it will hit, so the head lands right on it (in 3D, not only on
            // screen), turned and shrunk so that at that moment the hands are at the lower right of the view.
            // (The upper body lunges forward on the blow, so the real hands end up right in front of the eyes.)
            Quaternion toCam = Quaternion.Inverse(cam.transform.rotation);
            Vector3 eye = cam.transform.position;
            float w = _swing.FirstPersonTrue;
            Vector3 contact = toCam * (ExpectedContactWorld - eye);
            float strikeScale = s.fpStrikeScale;
            Quaternion strikeTurn = Quaternion.identity;
            if (HasPredictedContact)
            {
                // from the point of the drawn head that lands on the contact (short of it when only the hit reaches)
                // to the hands: turned and scaled so the hands end up at the lower edge of the view
                Vector3 toGrip = toCam * Root.TransformVector(_contactGripLocal - _contactDrawnLocal);
                Vector3 hands = toCam * (cameraRig.cam.ViewportToWorldPoint(s.fpStrikeHands) - eye) - contact;
                if (toGrip.sqrMagnitude > 1e-6f && hands.sqrMagnitude > 1e-6f)
                {
                    strikeTurn = Quaternion.FromToRotation(toGrip, hands);
                    strikeScale = Mathf.Clamp(hands.magnitude / toGrip.magnitude, 0.25f, 1.3f);
                }
            }
            float scale = Mathf.Lerp(s.fpScale, strikeScale, w);
            Quaternion turn = Quaternion.Slerp(Quaternion.identity, strikeTurn, w);
            Vector3 center = w * contact;
            Vector3 offset = (1f - w) * (s.fpIdleOffset + _swing.WindupAmount * s.fpWindupOffset);
            // when only the hit reaches the surface, the drawn head stops short of it: close that gap on screen
            if (HasPredictedContact)
                offset += w * scale * (turn * (toCam * Root.TransformVector(_contactLocal - _contactDrawnLocal)));
            Vector3 MapBase(Vector3 world) => center + turn * (scale * (toCam * (world - eye) - center)) + offset;

            // The swing turns about the shoulders, just behind the eyes: drawn as it is, the head would come down from
            // behind the camera and only show up on the target. Turn it about the hands instead (at the lower edge of
            // the view), so the blow is seen falling onto the target; still far from it, the pickaxe is held raised
            // over the view rather than going on back past the eyes.
            Quaternion fix = Quaternion.identity;
            Vector3 fixPivot = Vector3.zero, fixMove = Vector3.zero;
            if (HasPredictedContact && w > 0f)
            {
                float real = _swing.Theta - _contactTheta, drawn = real, limit = Mathf.Max(s.fpRaiseLimit, 1f);
                if (drawn < 0f) drawn = -limit * Tanh(-drawn / limit);
                Vector3 axis = turn * (toCam * Root.TransformDirection(_swing.AxisLocal));
                Vector3 p = MapBase(Root.TransformPoint(_swing.PivotLocal)), hands = MapBase(Root.TransformPoint(_contactGripLocal));
                fix = Quaternion.AngleAxis(w * (drawn - real), axis);
                fixPivot = p;
                fixMove = w * (hands + Quaternion.AngleAxis(drawn, axis) * (p - hands) - p);
            }
            Vector3 Map(Vector3 world) => fixPivot + fix * (MapBase(world) - fixPivot) + fixMove;

            _viewTool.transform.localPosition = Map(pose.position);
            _viewTool.transform.localRotation = fix * turn * toCam * pose.rotation;
            _viewTool.transform.localScale = Vector3.one * scale;
            Vector3 viewHandle = fix * (turn * (toCam * handle));
            // the first-person arms come up from below the bottom of the screen
            Vector3 up = new Vector3(0f, 1f, 0.4f).normalized;
            Vector3 rightV = Map(rightGrip), leftV = Map(leftGrip);
            if (drawing)
            {
                // off the back: it comes down into view from the upper right, turned head-up, about the left hand's grip
                var t = _viewTool.transform;
                Quaternion r1 = t.localRotation, r0 = Quaternion.Euler(ls.fpBackTurn) * r1;
                Vector3 g = ToolPose.GripLowLocal * scale;
                Quaternion r = Quaternion.Slerp(r0, r1, dPull);
                Vector3 grip = Vector3.Lerp(ls.fpBackFrom * scale, t.localPosition + r1 * g, dPull);
                t.localRotation = r;
                t.localPosition = grip - r * g;
                viewHandle = r * Vector3.up;
                float upper = Mathf.Lerp(s.restUpperHand, together, _swing.HandSlide);
                rightV = Vector3.Lerp(s.fpArmAnchor + up * 0.03f, t.localPosition + r * new Vector3(0f, upper * scale, 0f), dReach);
                leftV = Vector3.Lerp(s.fpArmAnchorLeft + up * 0.03f, grip, dPull);
            }
            BuildArm(_arms.ViewR, s.fpArmAnchor, up, rightV, viewHandle, scale * s.fpArmThickness, scale);
            BuildArm(_arms.ViewL, s.fpArmAnchorLeft, up, leftV, viewHandle, scale * s.fpArmThickness, scale);
        }

        static float Ease(float x)
        {
            x = Mathf.Clamp01(x);
            return x * x * (3f - 2f * x);
        }

        static float Tanh(float x)
        {
            float e = Mathf.Exp(-2f * Mathf.Abs(x));
            return Mathf.Sign(x) * (1f - e) / (1f + e);
        }

        /// <summary>An arm from its root to a grip: it leaves the body along the skin normal and wraps the handle from the side.</summary>
        void BuildArm(SlugArmMesh arm, Vector3 root, Vector3 rootNormal, Vector3 grip, Vector3 handle, float thickness, float fistScale)
        {
            var s = pickaxe;
            SlugArmSet.Build(arm, root, rootNormal, grip, handle, s.armRadiusRoot * thickness, s.armRadiusHand * thickness,
                             s.fistSize * fistScale, fistScale);
        }
    }
}
