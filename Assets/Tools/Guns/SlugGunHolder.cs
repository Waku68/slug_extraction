using SlugDesign;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

namespace SlugMove
{
    /// <summary>
    /// One of the slug's guns on stretchy arms (an assault rifle, a pump shotgun or a bolt-action sniper, by
    /// <see cref="GunSettings.kind"/>). Carried across the chest; Attack shoulders it (the butt on the right shoulder) and
    /// fires (hold for full auto; a click per shot otherwise, with the pump or bolt worked by hand in between). R reloads
    /// (a whole magazine, or one round at a time until full or until the trigger is pulled). Guns with a scope look
    /// through it on the right button.
    ///
    /// The handling (raise, recoil springs, action, reload, scope) is simulated in <see cref="GunHandling"/>; the model
    /// (<see cref="SlugGunModel"/>) moves its own parts from it and says where the hands go. This component aims it and
    /// draws it twice over, like the pickaxe: the body's gun and arms (seen by every camera except the player's own in
    /// first person), and a first-person copy shouldered low and right in view, drawn shrunk toward the eye so it reaches
    /// far less into walls (and pulled back when one is right in front). Rounds fly from the eye (<see cref="SaltRound"/>);
    /// the eyes climb with every shot and come partly back afterwards.
    /// Runs after the camera rig so the first-person copy follows this frame's camera.
    /// </summary>
    [DefaultExecutionOrder(100)]
    public class SlugGunHolder : MonoBehaviour
    {
        [FormerlySerializedAs("rifle")] public GunSettings gun;
        public SlugMoveController controller;
        public SlugCameraRig cameraRig;
        public SlugPop slug;
        [Tooltip("false の間は撃てない（試合の終了後など）")] public bool inputEnabled = true;
        [Tooltip("弾数と照準を画面に出す")] public bool showHud = true;

        /// <summary>Every round fired (from the eye, direction); a shotgun blast fires several at once.</summary>
        public event System.Action<Vector3, Vector3> Fired;
        /// <summary>Every round that lands on something.</summary>
        public event System.Action<RaycastHit> RoundHit;

        /// <summary>Holds the trigger without the mouse (tests and tools; works with a free cursor too).</summary>
        [System.NonSerialized] public bool forceTrigger;
        /// <summary>Holds the aim button without the mouse (tests and tools).</summary>
        [System.NonSerialized] public bool forceAim;

        public GunHandling Handling => _handling;
        /// <summary>The spread right now (cone half-angle, degrees), crawling and the scope included.</summary>
        public float CurrentSpread => (_handling.Spread + gun.moveSpread * controller.Speed01) * Mathf.Lerp(1f, gun.scopeSpread, _handling.Scope);
        /// <summary>Where the gun points (world): the point under the crosshair, not closer than minAimDistance.</summary>
        public Vector3 AimPoint { get; private set; }
        public Vector3 MuzzleWorld => _bodyMuzzle;
        public bool Ready => _ready;

        const float SubStep = 1f / 240f;
        const float StowPitch = 65f;
        static readonly Vector3 StowOffset = new Vector3(0.02f, -0.12f, -0.03f);

        readonly GunHandling _handling = new GunHandling();
        InputAction _attack;
        SlugGunModel _gun, _viewGun;
        SlugArmSet _arms;
        Light _flashLight;
        Renderer[] _viewRenderers = new Renderer[0], _bodyRenderers = new Renderer[0], _bodyUnshadowed = new Renderer[0];
        bool _ready, _aimInit;
        float _aimYaw, _aimPitch;
        float _recoverPitch, _recoverYaw, _expectedPitch;
        float _flashRoll, _flashScale = 1f, _wallPull;
        Vector3 _bodyMuzzle, _viewMuzzle;
        GUIStyle _style;
        static Texture2D _scopeTex;

        Transform Root => controller.transform;

        void OnEnable()
        {
            _attack = InputSystem.actions != null ? InputSystem.actions.FindAction("Player/Attack") : null;
            _attack?.Enable();
            UnityEngine.Rendering.RenderPipelineManager.beginCameraRendering += OnBeginCamera;
            if (!_ready) return;
            Show(true);
            _handling.Draw();
            _expectedPitch = cameraRig.Pitch;
        }

        /// <summary>Put away (another tool taken in hand): the gun and its arms are hidden, the eyes keep where they are.</summary>
        void OnDisable()
        {
            UnityEngine.Rendering.RenderPipelineManager.beginCameraRendering -= OnBeginCamera;
            if (!_ready) return;
            _recoverPitch = _recoverYaw = 0f;
            if (controller) controller.leanDegrees = 0f;
            if (cameraRig) cameraRig.zoom = 1f;
            Show(false);
        }

        void Show(bool on)
        {
            if (_gun) _gun.gameObject.SetActive(on);
            _arms.ShowBody(on);
            if (on) return;
            if (_viewGun) _viewGun.gameObject.SetActive(false);
            _arms.ShowView(false);
        }

        /// <summary>
        /// The first-person copy is drawn only by the player's own camera. For that camera in first person, the body's gun
        /// and arms only cast their shadow (they would poke into the view), so the shadow on the ground stays; every other
        /// camera (the Scene view, later other players) sees them as usual.
        /// </summary>
        void OnBeginCamera(UnityEngine.Rendering.ScriptableRenderContext context, Camera camera)
        {
            if (!_ready) return;
            bool own = camera == cameraRig.cam;
            foreach (var r in _viewRenderers) if (r) r.enabled = own;
            var mode = own && !cameraRig.thirdPerson ? UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly : UnityEngine.Rendering.ShadowCastingMode.On;
            foreach (var r in _bodyRenderers) if (r) r.shadowCastingMode = mode;
            // parts that cast no shadow (the magazine windows, the flash) are simply not drawn for it
            bool hide = own && !cameraRig.thirdPerson;
            foreach (var r in _bodyUnshadowed) if (r) r.enabled = !hide;
        }

        void Start()
        {
            _handling.settings = gun;
            _handling.Reset();
            _handling.Draw();
            _handling.Cue += OnCue;
            string name = gun.kind.ToString();
            _gun = SlugGunModel.Create(gun.kind, name, null, true);
            _viewGun = SlugGunModel.Create(gun.kind, "View" + name, cameraRig.cam.transform, false);
            _arms = SlugArmSet.Create(name, slug, cameraRig.cam.transform, gun.fpScale);
            foreach (var c in _gun.GetComponentsInChildren<Transform>(true)) c.gameObject.layer = SlugToolHolder.BodyToolLayer;

            var lightGo = new GameObject("MuzzleLight");
            lightGo.transform.SetParent(_gun.Muzzle, false);
            lightGo.transform.localPosition = new Vector3(0f, 0f, 0.05f);
            _flashLight = lightGo.AddComponent<Light>();
            _flashLight.type = LightType.Point;
            _flashLight.range = 1.2f;
            _flashLight.color = new Color(1f, 0.86f, 0.62f);
            _flashLight.shadows = LightShadows.None;
            _flashLight.enabled = false;

            var views = new System.Collections.Generic.List<Renderer>(_viewGun.GetComponentsInChildren<Renderer>(true));
            views.AddRange(_arms.ViewRenderers);
            _viewRenderers = views.ToArray();
            var bodies = new System.Collections.Generic.List<Renderer>();
            var unshadowed = new System.Collections.Generic.List<Renderer>();
            foreach (var r in _gun.GetComponentsInChildren<Renderer>(true))
                (r.shadowCastingMode != UnityEngine.Rendering.ShadowCastingMode.Off ? bodies : unshadowed).Add(r);
            bodies.AddRange(_arms.BodyRenderers);
            _bodyRenderers = bodies.ToArray();
            _bodyUnshadowed = unshadowed.ToArray();
            _expectedPitch = cameraRig.Pitch;
            _ready = true;
        }

        void OnDestroy()
        {
            if (_gun) Destroy(_gun.gameObject);
            _arms?.Destroy();
        }

        // ------------------------------------------------------------------ update: input, firing, recoil
        void Update()
        {
            if (!_ready) return;
            bool canUse = inputEnabled && !controller.Climbing && (Cursor.lockState == CursorLockMode.Locked || forceTrigger || forceAim);
            bool trigger = canUse && (forceTrigger || (_attack != null && _attack.IsPressed()));
            var mouse = Mouse.current;
            bool aim = canUse && (forceAim || (mouse != null && mouse.rightButton.isPressed));
            var kb = Keyboard.current;
            bool reload = canUse && kb != null && kb.rKey.wasPressedThisFrame;
            Simulate(Time.deltaTime, trigger, reload, aim);
        }

        /// <summary>
        /// Advances the handling by <paramref name="dt"/>, firing the rounds that are due. Small fixed steps keep the
        /// stiff recoil springs stable through a long frame, and put each shot at its own moment within the frame.
        /// </summary>
        public void Simulate(float dt, bool trigger, bool reload, bool aim = false)
        {
            _handling.settings = gun;
            if (reload) _handling.StartReload();
            Recover(dt);
            int n = Mathf.Clamp(Mathf.CeilToInt(dt / SubStep), 1, 32);
            for (int i = 0; i < n; i++)
            {
                _handling.Step(dt / n, trigger, aim);
                while (_handling.TryFire(trigger)) FireRound();
            }
            controller.leanDegrees = _handling.Lean;
            // the scope narrows the view (and slows the look to match)
            float zoom = gun.scopeFov > 0f
                ? Mathf.Tan(cameraRig.settings.fieldOfView * 0.5f * Mathf.Deg2Rad) / Mathf.Tan(gun.scopeFov * 0.5f * Mathf.Deg2Rad) : 1f;
            cameraRig.zoom = Mathf.Lerp(1f, zoom, _handling.Scope);
            _expectedPitch = cameraRig.Pitch;
        }

        /// <summary>
        /// After a burst, the eyes come back down part of the way they climbed. What the player already pulled down
        /// with the mouse counts toward it, so pulling against the climb never overshoots.
        /// </summary>
        void Recover(float dt)
        {
            float pulled = cameraRig.Pitch - _expectedPitch;               // + = the player looked down since last time
            if (pulled > 0f) _recoverPitch = Mathf.Max(0f, _recoverPitch - pulled);
            if (_handling.SinceShot < 0.1f || _recoverPitch <= 1e-4f) return;
            float step = Mathf.Min(_recoverPitch, gun.climbRecoverySpeed * dt);
            float yaw = _recoverYaw * step / _recoverPitch;
            cameraRig.AddLook(-yaw, step);
            _recoverPitch -= step;
            _recoverYaw -= yaw;
        }

        void FireRound()
        {
            var s = gun;
            bool firstPerson = !cameraRig.thirdPerson;
            Vector3 eye = firstPerson ? cameraRig.cam.transform.position : slug.FaceCenterWorld;
            Quaternion look = Quaternion.Euler(cameraRig.Pitch, cameraRig.Yaw, 0f);
            // the whole shot strays by the spread; a shotgun's grains also scatter within their own cone
            Vector2 off = Random.insideUnitCircle * CurrentSpread;
            Quaternion shot = look * Quaternion.Euler(-off.y, off.x, 0f);
            for (int i = 0; i < Mathf.Max(1, s.pellets); i++)
            {
                Vector2 scatter = s.pellets > 1 ? Random.insideUnitCircle * s.pelletSpread : Vector2.zero;
                Vector3 dir = shot * Quaternion.Euler(-scatter.y, scatter.x, 0f) * Vector3.forward;
                float speed = s.muzzleSpeed * (s.pellets > 1 ? Random.Range(0.9f, 1.1f) : 1f);
                SaltRound.Fire(eye, dir * speed, firstPerson ? _viewMuzzle : _bodyMuzzle, firstPerson ? s.fpScale : 1f,
                               s, Root, hit => RoundHit?.Invoke(hit));
                Fired?.Invoke(eye, dir);
            }

            // the eyes climb (a little sideways too); part of it comes back after the burst
            float up = s.climbPerShot * Random.Range(0.85f, 1.15f);
            float side = (Random.Range(-1f, 1f) + s.climbYawBias) * s.climbYaw;
            cameraRig.AddLook(side, -up);
            _recoverPitch += up * s.climbRecovery;
            _recoverYaw += side * s.climbRecovery;
            cameraRig.Shake(s.shake, s.shakeDuration);
            _flashRoll = Random.Range(0f, 360f);
            _flashScale = Random.Range(0.8f, 1.25f) * s.flashSize;
        }

        /// <summary>The pump racked back: the spent shell flies out of the port of the gun this player sees.</summary>
        void OnCue(string cue)
        {
            if (cue != "eject" || gun.kind != GunKind.Shotgun || !_ready) return;
            var model = cameraRig.thirdPerson ? _gun : _viewGun;
            if (!model || !model.gameObject.activeInHierarchy) return;
            var t = model.transform;
            float scale = t.lossyScale.x;
            var shell = SlugShotgun.Shell(null, true, true);
            shell.transform.SetPositionAndRotation(t.TransformPoint(SlugShotgun.EjectLocal), t.rotation * Quaternion.Euler(0f, 90f, 0f));
            shell.transform.localScale = Vector3.one * scale;
            var cc = controller.GetComponent<Collider>();
            if (cc) Physics.IgnoreCollision(shell.GetComponent<Collider>(), cc);
            var rb = shell.GetComponent<Rigidbody>();
            rb.linearVelocity = t.rotation * new Vector3(1.5f, 1.2f, -0.3f) * (Random.Range(0.85f, 1.15f) * Mathf.Max(scale, 0.5f));
            rb.angularVelocity = Random.insideUnitSphere * 14f;
            Destroy(shell, 6f);
        }

        bool RaycastOthers(Vector3 origin, Vector3 dir, float range, out RaycastHit best)
        {
            best = default;
            float bestDist = float.MaxValue;
            foreach (var h in Physics.RaycastAll(origin, dir, range, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            {
                if (h.collider.transform.IsChildOf(Root) || h.distance >= bestDist) continue;
                best = h;
                bestDist = h.distance;
            }
            return bestDist < float.MaxValue;
        }

        // ------------------------------------------------------------------ late update: aim and show it
        void LateUpdate()
        {
            if (!_ready) return;
            var s = gun;
            var h = _handling;
            var cam = cameraRig.cam;
            bool firstPerson = !cameraRig.thirdPerson;
            float dt = Time.deltaTime;
            float boreY = _gun.BoreY;

            // ---- the point under the crosshair (a near wall does not turn the gun across the face)
            Vector3 eye = firstPerson ? cam.transform.position : slug.FaceCenterWorld;
            Vector3 look = Quaternion.Euler(cameraRig.Pitch, cameraRig.Yaw, 0f) * Vector3.forward;
            float dist = RaycastOthers(eye, look, s.farAimDistance, out var aimHit) ? Mathf.Max(aimHit.distance, s.minAimDistance) : s.farAimDistance;
            AimPoint = eye + look * dist;

            // ---- the bore (above the butt) points at it, following the eyes a little late
            Quaternion body = Root.rotation;
            Vector3 shoulder = slug.HandRootWorld(1f) + body * s.buttOffset;
            Vector3 want = AimPoint - (shoulder + Quaternion.Euler(_aimPitch, _aimYaw, 0f) * new Vector3(0f, boreY, 0f));
            float wantYaw = Mathf.Atan2(want.x, want.z) * Mathf.Rad2Deg;
            float wantPitch = -Mathf.Atan2(want.y, new Vector2(want.x, want.z).magnitude) * Mathf.Rad2Deg;
            if (!_aimInit) { _aimYaw = wantYaw; _aimPitch = wantPitch; _aimInit = true; }
            float follow = 1f - Mathf.Exp(-2f * Mathf.PI * s.aimLagHz * dt);
            _aimYaw += Mathf.DeltaAngle(_aimYaw, wantYaw) * follow;
            _aimPitch += (wantPitch - _aimPitch) * follow;

            // ---- the pose: shouldered, carried across the chest, tilted for a reload, down out of the way while being
            //      drawn; the kick pushes it back into the shoulder and flips the muzzle
            // (a reload holds it up, tilted, instead of carried: the hands work on it in front of the chest)
            float tilt = h.ReloadTilt, low = (1f - h.Raise) * (1f - tilt), stow = 1f - h.Equip;
            // working a bolt cants the gun a little toward the hand on it
            float cant = gun.action == GunAction.Bolt && h.Cycling ? 10f * Mathf.Sin(Mathf.PI * h.Cycle) : 0f;
            Quaternion HoldTurn(float lowAmount) => Quaternion.Euler(
                s.lowPitch * lowAmount + s.reloadPitch * tilt + StowPitch * stow - h.Pitch,
                -s.lowYaw * lowAmount + h.Yaw,
                s.lowRoll * lowAmount + s.reloadRoll * tilt + h.Roll + cant);
            Vector3 buttShift = s.lowButtOffset * low + s.reloadButtOffset * tilt + StowOffset * stow;   // body space (first person)
            // the body's gun: at the shoulder, blended into the carry across the chest while low
            Quaternion rot = Quaternion.Euler(_aimPitch, _aimYaw, 0f) * HoldTurn(0f);
            Vector3 pos = shoulder + body * (s.reloadButtOffset * tilt) - rot * Vector3.forward * h.Kick;
            if (low > 0f)
            {
                var carry = Carry(body);
                rot = Quaternion.Slerp(rot, carry.rotation, low);
                pos = Vector3.Lerp(pos, carry.position, low);
            }
            pos += body * (StowOffset * stow);
            var pose = new ToolPose { position = pos, rotation = rot };

            float q = h.Squeeze * s.fistSqueeze;
            Vector3 fist = Vector3.Scale(s.fistSize, new Vector3(1f + q * 0.5f, 1f - q, 1f + q * 0.5f));
            float ammo = h.Ammo / (float)Mathf.Max(s.magazineSize, 1);
            bool flash = h.SinceShot < s.flashTime;

            // ---- the body's gun and arms
            _gun.Pose(h, s, out var right, out var left);
            Place(_gun, pose.position, pose.rotation, Vector3.one, ammo, flash, false);
            _flashLight.enabled = flash;
            _flashLight.intensity = s.flashLight;
            _bodyMuzzle = pose.Point(_gun.MuzzleLocal);
            SlugArmSet.Build(_arms.BodyR, slug.HandRootWorld(1f), slug.HandRootNormalWorld(1f), pose.Point(right.point), rot * right.axis,
                             s.armRadiusRoot, s.armRadiusHand, fist, 1f);
            SlugArmSet.Build(_arms.BodyL, slug.HandRootWorld(-1f), slug.HandRootNormalWorld(-1f), pose.Point(left.point), rot * left.axis,
                             s.armRadiusRoot, s.armRadiusHand, fist, 1f);

            // ---- first person: the same handling, but shouldered at a set place low and right in view (the real
            //      shoulder is right under the eye, so the receiver would fill the screen) and aimed at the point under
            //      the crosshair, lagging and kicking exactly as the body's gun. Low and ready it dips less, so it stays
            //      in view. The whole thing is drawn shrunk toward the eye (the same on screen, but reaching far less
            //      into walls) and pulled back when a wall is closer than the muzzle.
            bool scopedIn = h.Scope > 0.97f;
            _viewGun.gameObject.SetActive(firstPerson && !scopedIn);
            _arms.ShowView(firstPerson && !scopedIn);
            if (!firstPerson || scopedIn) { _viewMuzzle = _bodyMuzzle; return; }
            Quaternion toCam = Quaternion.Inverse(cam.transform.rotation);
            Vector3 camPos = cam.transform.position;
            Vector3 upV = toCam * Vector3.up;
            Vector3 aimV = toCam * (AimPoint - camPos);
            Vector3 shoulderV = s.fpShoulder + toCam * (body * buttShift);
            Quaternion wantV = Quaternion.LookRotation(aimV - (s.fpShoulder + upV * boreY), upV);
            Quaternion lag = Quaternion.Euler(_aimPitch, _aimYaw, 0f) * Quaternion.Inverse(Quaternion.Euler(wantPitch, wantYaw, 0f));
            Quaternion rotV = toCam * lag * cam.transform.rotation * wantV * HoldTurn(low * s.fpLowAngles);
            var poseV = new ToolPose { position = shoulderV - rotV * Vector3.forward * h.Kick, rotation = rotV };
            if (tilt > 0f)
            {
                // a reload brings where the rounds go in to the lower middle of the view and turns the gun about it,
                // the muzzle off to the left, so the right side (the magazine, the port, the open bolt) faces the eyes
                Vector3 focus = Vector3.Lerp(poseV.Point(_viewGun.ReloadFocusLocal), s.fpReloadFocus, tilt);
                rotV = Quaternion.Euler(s.fpReloadTurn * tilt) * rotV;
                poseV = new ToolPose { position = focus - rotV * _viewGun.ReloadFocusLocal, rotation = rotV };
            }
            if (h.Scope > 0f)
            {
                // the scope comes up to the eye: straight ahead, still kicked by the recoil
                Quaternion rotA = Quaternion.Euler(-h.Pitch, h.Yaw, h.Roll);
                Vector3 posA = new Vector3(0f, 0f, s.scopeEyeRelief) - rotA * _viewGun.ScopeEyeLocal - rotA * Vector3.forward * h.Kick;
                float w = h.Scope;
                rotV = Quaternion.Slerp(poseV.rotation, rotA, w);
                poseV = new ToolPose { position = Vector3.Lerp(poseV.position, posA, w), rotation = rotV };
            }

            float sc = s.fpScale;
            Vector3 offset = s.fpLowOffset * low;
            Vector3 muzzle = poseV.Point(_viewGun.MuzzleLocal) * sc + offset;
            float reach = muzzle.magnitude + s.fpWallMargin, pull = 0f;
            if (RaycastOthers(camPos, cam.transform.rotation * muzzle.normalized, reach, out var wall)) pull = reach - wall.distance;
            _wallPull = pull > _wallPull ? pull : Mathf.MoveTowards(_wallPull, pull, 0.5f * dt);
            offset += Vector3.back * _wallPull * (1f - h.Scope);
            Vector3 Map(Vector3 local) => poseV.Point(local) * sc + offset;                    // gun space → drawn
            Vector3 MapBody(Vector3 world) => toCam * (world - camPos) * sc;                   // the real body → drawn

            _viewGun.Pose(h, s, out var rightV, out var leftV);
            Place(_viewGun, poseV.position * sc + offset, rotV, Vector3.one * sc, ammo, flash, true);
            _viewMuzzle = cam.transform.TransformPoint(Map(_viewGun.MuzzleLocal));
            float thick = sc * s.fpArmThickness;
            SlugArmSet.Build(_arms.ViewR, MapBody(slug.HandRootWorld(1f)), toCam * slug.HandRootNormalWorld(1f),
                             Map(rightV.point), rotV * rightV.axis, s.armRadiusRoot * thick, s.armRadiusHand * thick, fist * sc, sc);
            SlugArmSet.Build(_arms.ViewL, MapBody(slug.HandRootWorld(-1f)), toCam * slug.HandRootNormalWorld(-1f),
                             Map(leftV.point), rotV * leftV.axis, s.armRadiusRoot * thick, s.armRadiusHand * thick, fist * sc, sc);
        }

        /// <summary>
        /// The carry (what everyone else sees while not firing): like the pickaxe's ready pose, the right hand drawn in
        /// just in front of where the arm grows, and the gun across the chest, the butt out by the right shoulder and the
        /// muzzle down to the lower left. The left hand reaches the gun wherever it is.
        /// </summary>
        ToolPose Carry(Quaternion body)
        {
            var s = gun;
            Vector3 right = slug.HandRootWorld(1f) + body * s.carryRightHand;
            Vector3 dir = s.carryDirection.sqrMagnitude > 1e-6f ? s.carryDirection : new Vector3(-1f, -0.5f, 0.2f);
            Quaternion rot = Quaternion.LookRotation(body * dir, body * s.carryUp);
            return new ToolPose { position = right - rot * _gun.RestGripLocal, rotation = rot };
        }

        void Place(SlugGunModel model, Vector3 pos, Quaternion rot, Vector3 scale, float ammo, bool flash, bool local)
        {
            if (local) { model.transform.localPosition = pos; model.transform.localRotation = rot; }
            else model.transform.SetPositionAndRotation(pos, rot);
            model.transform.localScale = scale;
            model.SetAmmo(ammo);
            model.SetFlash(flash, _flashRoll, _flashScale);
        }

        // ------------------------------------------------------------------ HUD: a crosshair that opens with the spread, the salt left, the scope
        void OnGUI()
        {
            if (!_ready || !showHud) return;
            if (_style == null)
            {
                _style = new GUIStyle(GUI.skin.label) { fontSize = 22, alignment = TextAnchor.MiddleRight, richText = true };
                _style.normal.textColor = Color.white;
            }
            var h = _handling;
            float w = Screen.width, hgt = Screen.height, cx = w * 0.5f, cy = hgt * 0.5f;
            var old = GUI.color;
            if (!cameraRig.thirdPerson && h.Scope > 0.97f) DrawScope(w, hgt, cx, cy - h.Pitch * hgt * 0.01f);
            else if (!cameraRig.thirdPerson)
            {
                // a shotgun shows where its grains will scatter, the others how far a round may stray
                float spread = CurrentSpread + (gun.pellets > 1 ? gun.pelletSpread : 0f);
                float half = cameraRig.cam.fieldOfView * 0.5f * Mathf.Deg2Rad;
                float gap = Mathf.Tan(spread * Mathf.Deg2Rad) / Mathf.Tan(half) * hgt * 0.5f + 3f;
                GUI.color = new Color(1f, 1f, 1f, 0.85f);
                var tex = Texture2D.whiteTexture;
                GUI.DrawTexture(new Rect(cx + gap, cy - 1f, 9f, 2f), tex);
                GUI.DrawTexture(new Rect(cx - gap - 9f, cy - 1f, 9f, 2f), tex);
                GUI.DrawTexture(new Rect(cx - 1f, cy + gap, 2f, 9f), tex);
                GUI.DrawTexture(new Rect(cx - 1f, cy - gap - 9f, 2f, 9f), tex);
                GUI.color = old;
            }
            string ammo = h.Reloading && gun.reloadStyle == ReloadStyle.Magazine ? "<size=18>リロード中…</size>"
                : h.Ammo == 0 ? "<color=#ff9080><b>0</b></color> / " + gun.magazineSize
                : $"<b>{h.Ammo}</b> / {gun.magazineSize}";
            if (h.Reloading && gun.reloadStyle == ReloadStyle.OneByOne) ammo = "<size=16>込めている… </size>" + ammo;
            GUI.Box(new Rect(w - 230, hgt - 70, 220, 56), GUIContent.none);
            GUI.Label(new Rect(w - 225, hgt - 68, 205, 30), ammo, _style);
            GUI.Label(new Rect(w - 225, hgt - 44, 205, 26), $"<size=14>{gun.displayName}・{gun.ammoName}　R リロード</size>", _style);
        }

        /// <summary>Looking through the scope: black all round a round view, with a fine cross.</summary>
        static void DrawScope(float w, float h, float cx, float cy)
        {
            if (_scopeTex == null)
            {
                const int N = 256;
                _scopeTex = new Texture2D(N, N, TextureFormat.RGBA32, false) { hideFlags = HideFlags.DontSave, wrapMode = TextureWrapMode.Clamp };
                for (int y = 0; y < N; y++)
                for (int x = 0; x < N; x++)
                {
                    float dx = (x + 0.5f) / N * 2f - 1f, dy = (y + 0.5f) / N * 2f - 1f, r = Mathf.Sqrt(dx * dx + dy * dy);
                    float a = Mathf.Clamp01((r - 0.94f) / 0.04f);                       // the dark edge of the lens
                    bool cross = (Mathf.Abs(dx) < 0.004f || Mathf.Abs(dy) < 0.004f) && r < 0.9f && r > 0.02f;
                    bool post = (Mathf.Abs(dx) < 0.014f && dy < -0.35f) || (Mathf.Abs(dy) < 0.014f && Mathf.Abs(dx) > 0.35f);
                    _scopeTex.SetPixel(x, y, cross || (post && r < 0.95f) ? new Color(0f, 0f, 0f, 0.9f) : new Color(0f, 0f, 0f, a));
                }
                _scopeTex.Apply();
            }
            float size = h * 0.98f;
            var rect = new Rect(cx - size * 0.5f, cy - size * 0.5f, size, size);
            var old = GUI.color;
            GUI.color = Color.black;
            GUI.DrawTexture(new Rect(0, 0, rect.xMin + 1f, h), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(rect.xMax - 1f, 0, w - rect.xMax + 1f, h), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(0, 0, w, rect.yMin + 1f), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(0, rect.yMax - 1f, w, h - rect.yMax + 1f), Texture2D.whiteTexture);
            GUI.color = Color.white;
            GUI.DrawTexture(rect, _scopeTex);
            GUI.color = old;
        }
    }
}
