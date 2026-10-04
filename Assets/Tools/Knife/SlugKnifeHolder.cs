using SlugDesign;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SlugMove
{
    /// <summary>
    /// The slug's knife, held in the right hand on a stretchy arm; the left hand waits empty in front of the chest.
    /// Attack stabs (hold to keep stabbing): the hand draws back a little, the arm shoots the blade at the point under the
    /// crosshair (as far as it reaches), holds a moment and comes back. The blade's point is swept against the world
    /// every small step and stops where it really touches something: a slug's part (or a practice target) is struck
    /// like a round would strike it (<see cref="IShotTarget"/>), anything else throws chips and kicks the hand back.
    ///
    /// Drawn twice over like the other tools: the body's knife and arms (seen by every camera except the player's own
    /// in first person) and a first-person copy low and right in view, drawn shrunk toward the eye; its point lands on
    /// the same spot on screen. Taken in hand, it comes out of its sheath on the hip (<see cref="SlugToolRack"/>).
    /// Runs after the camera rig so the first-person copy follows this frame's camera.
    /// </summary>
    [DefaultExecutionOrder(100)]
    public class SlugKnifeHolder : MonoBehaviour
    {
        public enum Phase { Ready, Windup, Thrust, Hold, Recover }

        public KnifeSettings knife;
        public SlugMoveController controller;
        public SlugCameraRig cameraRig;
        public SlugPop slug;
        [Tooltip("false の間は突けない（試合の終了後など）")] public bool inputEnabled = true;
        /// <summary>How long the next draw takes (set by the loadout before enabling; 0 or less = at once).</summary>
        [System.NonSerialized] public float drawTime = -1f;
        /// <summary>Holds the button without the mouse (tests and tools).</summary>
        [System.NonSerialized] public bool forceAttack;

        /// <summary>Every stab that lands on something (target is null for plain ground and walls).</summary>
        public event System.Action<RaycastHit, IShotTarget> Struck;

        public Phase State => _phase;
        /// <summary>0..1: how far the knife has been taken in hand (1 = ready).</summary>
        public float DrawProgress => _draw;
        public Vector3 TipWorld { get; private set; }

        const float SubStep = 1f / 240f;

        InputAction _attack;
        SlugKnife _tool, _viewTool;
        SlugArmSet _arms;
        SlugToolRack _rack;
        Renderer[] _viewRenderers = new Renderer[0], _bodyRenderers = new Renderer[0];
        bool _ready;
        float _draw = 1f;

        Phase _phase;
        float _t, _stop;
        // the stab, in the slug's own space (the root: ground position, yaw only): where it aims, how far out it got
        Vector3 _targetLocal;
        float _ext, _holdExt, _bounce;
        bool _hitThisStab;

        Transform Root => controller.transform;

        void OnEnable()
        {
            _attack = InputSystem.actions != null ? InputSystem.actions.FindAction("Player/Attack") : null;
            _attack?.Enable();
            UnityEngine.Rendering.RenderPipelineManager.beginCameraRendering += OnBeginCamera;
            _draw = drawTime > 0f ? 0f : 1f;
            _phase = Phase.Ready;
            _ext = 0f;
            if (_ready) Show(true);
        }

        void OnDisable()
        {
            UnityEngine.Rendering.RenderPipelineManager.beginCameraRendering -= OnBeginCamera;
            if (!_ready) return;
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

        void OnBeginCamera(UnityEngine.Rendering.ScriptableRenderContext context, Camera camera)
        {
            if (!_ready) return;
            bool own = camera == cameraRig.cam;
            foreach (var r in _viewRenderers) if (r) r.enabled = own;
            var mode = own && !cameraRig.thirdPerson ? UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly : UnityEngine.Rendering.ShadowCastingMode.On;
            foreach (var r in _bodyRenderers) if (r) r.shadowCastingMode = mode;
        }

        void Start()
        {
            _rack = GetComponent<SlugToolRack>();
            _tool = SlugKnife.Create("Knife", null, true);
            _viewTool = SlugKnife.Create("ViewKnife", cameraRig.cam.transform, false);
            _arms = SlugArmSet.Create("Knife", slug, cameraRig.cam.transform, knife.fpScale);
            foreach (var c in _tool.GetComponentsInChildren<Transform>(true)) c.gameObject.layer = SlugToolHolder.BodyToolLayer;
            var views = new System.Collections.Generic.List<Renderer>(_viewTool.GetComponentsInChildren<Renderer>(true));
            views.AddRange(_arms.ViewRenderers);
            _viewRenderers = views.ToArray();
            var bodies = new System.Collections.Generic.List<Renderer>(_tool.GetComponentsInChildren<Renderer>(true));
            bodies.AddRange(_arms.BodyRenderers);
            _bodyRenderers = bodies.ToArray();
            _ready = true;
            if (!enabled) Show(false);
        }

        void OnDestroy()
        {
            if (_tool) Destroy(_tool.gameObject);
            _arms?.Destroy();
        }

        // ------------------------------------------------------------------ update: input and the stab
        void Update()
        {
            if (!_ready) return;
            if (_draw < 1f) _draw = Mathf.MoveTowards(_draw, 1f, Time.deltaTime / Mathf.Max(drawTime, 1e-3f));
            bool canUse = inputEnabled && _draw >= 1f && !controller.Climbing && (Cursor.lockState == CursorLockMode.Locked || forceAttack);
            bool held = canUse && (forceAttack || (_attack != null && _attack.IsPressed()));
            if (held && (_phase == Phase.Ready || (_phase == Phase.Recover && _t / Mathf.Max(knife.recoverTime, 1e-3f) >= knife.chainAt)))
                BeginStab();
            Simulate(Time.deltaTime);
        }

        void BeginStab()
        {
            var s = knife;
            Vector3 eye = cameraRig.thirdPerson ? slug.FaceCenterWorld : cameraRig.cam.transform.position;
            Vector3 dir = Quaternion.Euler(cameraRig.Pitch, cameraRig.Yaw, 0f) * Vector3.forward;
            Vector3 shoulder = slug.HandRootWorld(1f);
            float range = Vector3.Distance(eye, shoulder) + s.reach;
            Vector3 target = RaycastOthers(eye, dir, range, out var hit) ? hit.point - hit.normal * s.pierceDepth : eye + dir * range;
            // never further than the arm reaches
            Vector3 off = target - shoulder;
            if (off.magnitude > s.reach) target = shoulder + off.normalized * s.reach;
            _targetLocal = Root.InverseTransformPoint(target);
            _phase = Phase.Windup;
            _t = 0f;
            _hitThisStab = false;
            _bounce = 0f;
        }

        /// <summary>Advances the stab by <paramref name="dt"/>, sweeping the point of the blade against the world.</summary>
        public void Simulate(float dt)
        {
            var s = knife;
            int n = Mathf.Clamp(Mathf.CeilToInt(dt / SubStep), 1, 32);
            float h = dt / n;
            for (int i = 0; i < n; i++)
            {
                if (_stop > 0f) { _stop -= h; continue; }      // the hit stop: everything holds still
                Vector3 before = BodyPose().Point(SlugKnife.TipLocal);
                _t += h;
                switch (_phase)
                {
                    case Phase.Windup:
                        if (_t >= s.windupTime) Next(Phase.Thrust);
                        break;
                    case Phase.Thrust:
                        _ext = Ease(_t / Mathf.Max(s.thrustTime, 1e-3f));
                        Vector3 after = BodyPose().Point(SlugKnife.TipLocal);
                        if (!_hitThisStab && Sweep(before, after, out var hit)) { Land(hit, after - before); break; }
                        if (_t >= s.thrustTime) { _holdExt = 1f; Next(Phase.Hold); }
                        break;
                    case Phase.Hold:
                        if (_t >= s.holdTime) Next(Phase.Recover);
                        break;
                    case Phase.Recover:
                        if (_t >= s.recoverTime) { _phase = Phase.Ready; _t = 0f; _ext = 0f; }
                        break;
                }
                _bounce = Mathf.MoveTowards(_bounce, 0f, h * 0.4f);
            }
            float lean = _phase == Phase.Thrust ? Ease(_t / Mathf.Max(s.thrustTime, 1e-3f))
                       : _phase == Phase.Hold ? 1f
                       : _phase == Phase.Recover ? 1f - Ease(_t / Mathf.Max(s.recoverTime, 1e-3f)) : 0f;
            controller.leanDegrees = s.lean * lean;
        }

        void Next(Phase p)
        {
            _phase = p;
            _t = 0f;
        }

        bool Sweep(Vector3 a, Vector3 b, out RaycastHit best)
        {
            best = default;
            Vector3 d = b - a;
            float len = d.magnitude;
            if (len < 1e-6f) return false;
            float bestDist = float.MaxValue;
            foreach (var h in Physics.SphereCastAll(a, knife.tipRadius, d / len, len, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            {
                if (IsOwn(h.collider) || (h.distance <= 0f && h.point == Vector3.zero) || h.distance >= bestDist) continue;
                best = h;
                bestDist = h.distance;
            }
            return bestDist < float.MaxValue;
        }

        void Land(RaycastHit hit, Vector3 travel)
        {
            var s = knife;
            _hitThisStab = true;
            _holdExt = _ext;
            Next(Phase.Hold);
            _stop = s.hitStop;
            var target = hit.collider.GetComponentInParent<IShotTarget>();
            Vector3 velocity = travel.normalized * 6f;
            if (target != null) target.OnShot(hit, velocity);
            else
            {
                // a hard thing: chips fly and the hand is kicked back a little
                PickaxeEffects.Chips(hit.point, hit.normal, new Color(0.6f, 0.6f, 0.6f), 3, 0.6f);
                _bounce = s.bounce;
            }
            cameraRig.Shake(s.shake * (target != null ? 0.7f : 1f), s.shakeDuration);
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

        static float Ease(float x)
        {
            x = Mathf.Clamp01(x);
            return x * x * (3f - 2f * x);
        }

        // ------------------------------------------------------------------ poses
        /// <summary>
        /// Where the hand is on the stab's line: drawn back from <paramref name="hand"/> (ready), out toward
        /// <paramref name="full"/> (the point on the target), held where it stopped (short of the target when it hit
        /// something), and eased back to ready.
        /// </summary>
        Vector3 HandOnLine(Vector3 hand, Vector3 full, Vector3 dir)
        {
            var s = knife;
            Vector3 back = hand - dir * s.windupPull;
            switch (_phase)
            {
                case Phase.Windup: return Vector3.Lerp(hand, back, Ease(_t / Mathf.Max(s.windupTime, 1e-3f)));
                case Phase.Thrust: return Vector3.Lerp(back, full, _ext);
                case Phase.Hold: return Vector3.Lerp(back, full, _holdExt);
                case Phase.Recover: return Vector3.Lerp(Vector3.Lerp(back, full, _holdExt), hand, Ease(_t / Mathf.Max(s.recoverTime, 1e-3f)));
                default: return hand;
            }
        }

        /// <summary>0..1: how far the blade has turned from its ready angle onto the stab's line.</summary>
        float Turn()
        {
            var s = knife;
            switch (_phase)
            {
                case Phase.Windup: return Ease(_t / Mathf.Max(s.windupTime, 1e-3f));
                case Phase.Thrust: case Phase.Hold: return 1f;
                case Phase.Recover: return 1f - Ease(_t / Mathf.Max(s.recoverTime, 1e-3f));
                default: return 0f;
            }
        }

        /// <summary>The body's knife (world): ready in front of the chest, drawn back, or out along the line to the target.</summary>
        ToolPose BodyPose()
        {
            var s = knife;
            Quaternion body = Root.rotation;
            Vector3 hand = slug.HandRootWorld(1f) + body * s.readyHand;
            Vector3 readyDir = (body * s.readyDirection).normalized;
            Quaternion ready = Quaternion.LookRotation(readyDir, body * Vector3.up);
            if (_phase == Phase.Ready) return new ToolPose { position = hand, rotation = ready };
            Vector3 target = Root.TransformPoint(_targetLocal);
            Vector3 line = target - hand;
            Vector3 dir = line.sqrMagnitude > 1e-8f ? line.normalized : readyDir;
            Quaternion stab = Quaternion.LookRotation(dir, body * Vector3.up);
            Vector3 full = target - dir * SlugKnife.Reach;          // the hand where the point reaches the target
            Vector3 pos = HandOnLine(hand, full, dir) - dir * _bounce;
            return new ToolPose { position = pos, rotation = Quaternion.Slerp(ready, stab, Turn()) };
        }

        // ------------------------------------------------------------------ late update: show it
        void LateUpdate()
        {
            if (!_ready) return;
            var s = knife;
            Quaternion body = Root.rotation;
            ToolPose pose = BodyPose();

            // taken in hand: the right hand goes down to the sheath on the hip and draws the knife
            var ls = _rack && _rack.loadout ? _rack.loadout.settings : null;
            ToolPose stow = default;
            bool drawing = ls && _draw < 1f && _rack.TryStowPose(this, out stow);
            float reachPart = ls ? ls.reachPart : 0.4f;
            float dReach = drawing ? Ease(_draw / Mathf.Max(reachPart, 1e-3f)) : 1f;
            float dPull = drawing ? Ease((_draw - reachPart) / Mathf.Max(1f - reachPart, 1e-3f)) : 1f;
            if (drawing) pose = new ToolPose { position = Vector3.Lerp(stow.position, pose.position, dPull), rotation = Quaternion.Slerp(stow.rotation, pose.rotation, dPull) };

            TipWorld = pose.Point(SlugKnife.TipLocal);
            _tool.transform.SetPositionAndRotation(pose.position, pose.rotation);
            Vector3 restR = slug.HandRootWorld(1f) + body * new Vector3(-0.01f, -0.01f, 0.05f);
            Vector3 grip = drawing ? Vector3.Lerp(restR, pose.position, dReach) : pose.position;
            Vector3 handle = pose.rotation * Vector3.forward;
            SlugArmSet.Build(_arms.BodyR, slug.HandRootWorld(1f), slug.HandRootNormalWorld(1f), grip, handle,
                             s.armRadiusRoot, s.armRadiusHand, s.fistSize, 1f);
            SlugArmSet.Build(_arms.BodyL, slug.HandRootWorld(-1f), slug.HandRootNormalWorld(-1f), slug.HandRootWorld(-1f) + body * s.leftHand,
                             body * Vector3.right, s.armRadiusRoot, s.armRadiusHand, s.fistSize, 1f);

            // ---- first person: ready low and right; the stab goes out along the line from there to the target as it
            //      is seen (shrunk toward the eye, so the drawn point lands on the target on screen)
            var cam = cameraRig.cam;
            bool firstPerson = !cameraRig.thirdPerson;
            _viewTool.gameObject.SetActive(firstPerson);
            _arms.ViewR.gameObject.SetActive(firstPerson);
            _arms.ViewL.gameObject.SetActive(false);         // the empty left hand stays out of view
            if (!firstPerson) return;
            Quaternion toCam = Quaternion.Inverse(cam.transform.rotation);
            Vector3 eye = cam.transform.position;
            float sc = s.fpScale;
            Vector3 handV = s.fpReady;
            Quaternion readyV = Quaternion.LookRotation(s.fpReadyDirection.normalized, Vector3.up) * Quaternion.Euler(0f, 0f, s.fpReadyRoll);
            Quaternion rotV = readyV;
            if (_phase != Phase.Ready)
            {
                Vector3 targetV = toCam * (Root.TransformPoint(_targetLocal) - eye);
                Vector3 line = targetV - handV;
                Vector3 dir = line.sqrMagnitude > 1e-8f ? line.normalized : Vector3.forward;
                Quaternion stabV = Quaternion.LookRotation(dir, Vector3.up);
                Vector3 full = targetV - dir * SlugKnife.Reach;
                handV = HandOnLine(handV, full, dir) - dir * _bounce;
                rotV = Quaternion.Slerp(readyV, stabV, Turn());
            }
            if (drawing)
            {
                // up from the lower right, out of the sheath
                Quaternion fromRot = Quaternion.Euler(ls.fpHipTurn) * rotV;
                handV = Vector3.Lerp(ls.fpHipFrom, handV, dPull);
                rotV = Quaternion.Slerp(fromRot, rotV, dPull);
            }
            var t = _viewTool.transform;
            t.localPosition = handV * sc;
            t.localRotation = rotV;
            t.localScale = Vector3.one * sc;
            Vector3 rootV = toCam * (slug.HandRootWorld(1f) - eye) * sc;
            Vector3 gripV = drawing ? Vector3.Lerp(toCam * (restR - eye) * sc, handV * sc, dReach) : handV * sc;
            float thick = sc * s.fpArmThickness;
            SlugArmSet.Build(_arms.ViewR, rootV, toCam * slug.HandRootNormalWorld(1f), gripV, rotV * Vector3.forward,
                             s.armRadiusRoot * thick, s.armRadiusHand * thick, s.fistSize * sc, sc);
        }
    }
}
