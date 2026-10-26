using SlugMove;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace SlugBase
{
    /// <summary>
    /// The departure from the cenote base: the slug crawls into the bird's basket on the deck, the bird picks the basket
    /// up and flies out to sea, seen from behind the bird (third person), toward a far model of the island. The island
    /// scene loads while it flies; when it is ready (and the flight has lasted long enough) the bird flies into a cloud
    /// (the screen goes white) and the island scene opens.
    ///
    /// Steps and numbers (Oct 20; slug eye 0.47 m, about 1/3.6 of a human):
    /// - Board: stay inside the basket (within 1.0 m of its middle) for 2.5 s. The bird calls when the slug gets in.
    ///   F7 skips this (puts the slug in the basket and leaves at once).
    /// - Pick up: the bird opens its wings and hops from its spot on the deck onto the basket's ring (1.8 s).
    /// - Lift: up 7 m in 3 s, flapping fast (heavy). The camera moves from the slug's eyes to behind the bird (2 s).
    /// - Cruise: speeds up to 18 m/s (about 65 km/h), climbs at 14 m/s to 170 m, turns at most 6°/s toward the island.
    ///   Within 650 m of the island's middle it circles round it (island on the right) until the island scene is loaded.
    ///   For a human that is like looking down from about 600 m.
    /// - The island model sits about 1 km south-south-west of the base (its middle at (-450, 0, -950)).
    /// - Load: at least 10 s of flight, then 1.2 s into white, and the island scene opens; white fades out there (1.2 s).
    /// Camera: 11 m behind and 6 m above the bird, looking 14° down, so the bird and the basket sit low on the screen
    /// and the island above them. It turns 60% of the way from the bird's heading toward the island (at most 70°), so
    /// the island stays in view while the bird circles. The mouse turns it round the bird (it drifts back after 2 s).
    /// </summary>
    public class CarrierFlight : MonoBehaviour
    {
        public CarrierBirdMotion bird;
        public Transform basket;
        public SlugMoveController player;
        public SlugCameraRig rig;
        public Camera cam;
        [Tooltip("飛ぶ間、カメラの下についてくる海")] public Transform sea;
        [Tooltip("遠くに見える島の模型（ふだんは隠しておく）")] public GameObject islandModel;

        [Header("乗る")]
        [Tooltip("籠の中にこれだけいると出発（秒）")] public float boardDelay = 2.5f;
        [Tooltip("籠の真ん中からこの距離の中なら、乗っている（m）")] public float basketRadius = 1.0f;
        [Tooltip("籠の原点（床）から、綱が集まる輪までの高さ（m）。0 なら形から測る")] public float ringHeight = 0f;

        [Header("持ち上げる")]
        [Tooltip("鳥がデッキから籠の輪へ移る（秒）")] public float pickupTime = 1.8f;
        [Tooltip("持ち上げる高さ（m）")] public float liftHeight = 7f;
        [Tooltip("持ち上げる時間（秒）")] public float liftTime = 3f;
        [Tooltip("持ち上げる間の羽ばたき（1秒の回数）")] public float liftFlapRate = 1.7f;

        [Header("飛ぶ")]
        [Tooltip("速さ（m/s）")] public float cruiseSpeed = 18f;
        [Tooltip("速さが上がりきるまで（秒）")] public float speedUpTime = 4f;
        [Tooltip("飛ぶ高さ（m、海面から）")] public float cruiseAltitude = 170f;
        [Tooltip("上る速さ（m/s）")] public float climbRate = 14f;
        [Tooltip("曲がる速さの最大（度/秒）")] public float turnRate = 6f;
        [Tooltip("曲がるときの体の傾き（1度/秒あたりの度）")] public float bankPerTurn = 3f;
        [Tooltip("飛ぶ間、体を前へ倒す（度）")] public float bodyPitch = 18f;
        [Tooltip("島の模型の真ん中（ワールド）")] public Vector3 islandCenter = new Vector3(-450f, 0f, -950f);
        [Tooltip("島の真ん中からこの距離に来たら、まわりを回る（m）")] public float circleRadius = 650f;
        [Tooltip("羽ばたく（秒）・すべる（秒）")] public Vector2 flapGlide = new Vector2(2.5f, 3f);

        [Header("カメラ")]
        [Tooltip("鳥の後ろ（m）")] public float camBack = 11f;
        [Tooltip("鳥の上（m）")] public float camUp = 6f;
        [Tooltip("見下ろす角度（度）")] public float camPitch = 14f;
        [Tooltip("鳥の向きから島のほうへ回す割合（0〜1）と最大（度）")] public Vector2 camToIsland = new Vector2(0.6f, 70f);
        [Tooltip("ナメクジの目から鳥の後ろへ移る（秒）")] public float camBlend = 2f;
        [Tooltip("視野（度）")] public float camFov = 55f;
        [Tooltip("マウスで回せる左右・上下（度）")] public Vector2 orbitLimit = new Vector2(120f, 35f);

        [Header("読み込み")]
        [Tooltip("行き先の島のシーン")] public string islandScene = "IslandTest_800";
        [Tooltip("少なくともこれだけ飛ぶ（秒）")] public float minFlight = 10f;
        [Tooltip("白くなる・白から戻る（秒）")] public float fadeTime = 1.2f;

        enum Step { Wait, Board, Pickup, Lift, Cruise, Fade }
        Step _step = Step.Wait;
        float _t, _flightTime, _fade;
        AsyncOperation _load;

        Vector3 _birdFrom, _hover, _liftFrom;
        Quaternion _birdFromRot;
        float _heading, _speed, _climb, _bank, _flapPhase, _flapAmp = 1f, _flapClock;
        Vector3 _bob, _bobPrev;            // the basket's floor, swinging under the bird's feet like a pendulum
        float _basketYaw0, _heading0;

        Vector3 _camFromPos; Quaternion _camFromRot;
        float _camT, _orbitYaw, _orbitPitch, _orbitIdle, _viewYaw;
        float _baseFlap, _baseFlapAngle;

        void Start()
        {
            if (islandModel) islandModel.SetActive(false);
            if (ringHeight <= 0f && basket)
            {
                bool any = false; float top = 0f;
                foreach (var r in basket.GetComponentsInChildren<Renderer>()) { top = any ? Mathf.Max(top, r.bounds.max.y) : r.bounds.max.y; any = true; }
                ringHeight = any ? top - basket.position.y : 1.45f;
            }
            if (bird) { _baseFlap = bird.flapRate; _baseFlapAngle = bird.flapAngle; }
        }

        void Update()
        {
            float dt = Time.deltaTime;
            var kb = Keyboard.current;
            switch (_step)
            {
                case Step.Wait:
                    if (kb != null && kb.f7Key.wasPressedThisFrame) { PutInBasket(); BeginPickup(); break; }
                    if (InBasket()) { _step = Step.Board; _t = 0f; bird.Call(); }
                    break;
                case Step.Board:
                    if (!InBasket()) { _step = Step.Wait; break; }
                    _t += dt;
                    if (_t >= boardDelay) BeginPickup();
                    break;
                case Step.Pickup: StepPickup(dt); break;
                case Step.Lift: StepLift(dt); break;
                case Step.Cruise:
                case Step.Fade: StepCruise(dt); break;
            }
        }

        void LateUpdate()
        {
            if (_step >= Step.Lift) StepCamera(Time.deltaTime);
            if (_step >= Step.Lift && sea && cam) sea.position = new Vector3(cam.transform.position.x, sea.position.y, cam.transform.position.z);
        }

        bool InBasket()
        {
            if (!player || !basket) return false;
            Vector3 d = player.transform.position - basket.position;
            return new Vector2(d.x, d.z).magnitude < basketRadius && Mathf.Abs(d.y) < 0.6f;
        }

        void PutInBasket()
        {
            var cc = player.GetComponent<CharacterController>();
            if (cc) cc.enabled = false;
            player.transform.position = basket.position + Vector3.up * 0.05f;
            if (cc) cc.enabled = true;
        }

        // ---- pick up: the bird hops onto the basket's ring

        void BeginPickup()
        {
            _step = Step.Pickup; _t = 0f;
            player.inputEnabled = false;
            _birdFrom = bird.transform.position;
            _birdFromRot = bird.transform.rotation;
            _hover = basket.position + Vector3.up * ringHeight;
            _heading = 180f;                       // the sea is to the south
            bird.autoFlap = false;
            bird.SetFlying(true);
            bird.Call();
            // load the island while the bird takes off and flies
            Application.backgroundLoadingPriority = ThreadPriority.Low;
            _load = SceneManager.LoadSceneAsync(islandScene, LoadSceneMode.Single);
            if (_load != null) _load.allowSceneActivation = false;
        }

        void StepPickup(float dt)
        {
            _t += dt / Mathf.Max(0.1f, pickupTime);
            float k = Smooth(Mathf.Clamp01(_t));
            Vector3 p = Vector3.Lerp(_birdFrom, _hover, k) + Vector3.up * (Mathf.Sin(k * Mathf.PI) * 1.6f);
            bird.transform.SetPositionAndRotation(p, Quaternion.Slerp(_birdFromRot, Quaternion.Euler(0f, _heading, 0f), k));
            Flap(dt, liftFlapRate, 1f);
            if (_t >= 1f) BeginLift();
        }

        // ---- lift: up off the deck, the slug rides in the basket; the camera goes behind the bird

        void BeginLift()
        {
            _step = Step.Lift; _t = 0f;
            var cc = player.GetComponent<CharacterController>();
            if (cc) cc.enabled = false;
            player.driven = true;
            player.transform.SetParent(basket, true);
            _bob = _bobPrev = basket.position;
            _basketYaw0 = basket.eulerAngles.y;
            _heading0 = _heading;
            _liftFrom = bird.transform.position;

            if (rig) rig.enabled = false;
            _camFromPos = cam.transform.position;
            _camFromRot = cam.transform.rotation;
            _camT = 0f;
            _viewYaw = _heading;
            if (sea)
            {
                // a sea that reaches the horizon (it follows the camera); no outline on such a big plane
                sea.localScale = new Vector3(4000f, 1f, 4000f);
                var r = sea.GetComponent<Renderer>();
                if (r) { r.material.SetFloat("_OutlineWidth", 0f); r.material.SetFloat("_RimAmount", 0f); }   // no bright band far off
            }
            if (islandModel) islandModel.SetActive(true);
        }

        void StepLift(float dt)
        {
            _t += dt / Mathf.Max(0.1f, liftTime);
            float k = Smooth(Mathf.Clamp01(_t));
            Vector3 p = _liftFrom + Vector3.up * (liftHeight * k) + Vector3.back * (3f * k * k);
            float sway = Mathf.Sin(Time.time * liftFlapRate * Mathf.PI * 2f) * 0.12f;   // each beat lifts a little
            bird.transform.SetPositionAndRotation(p + Vector3.up * sway, Quaternion.Euler(bodyPitch * k * 0.5f, _heading, 0f));
            Flap(dt, liftFlapRate, 1f);
            HangBasket(dt);
            _flightTime += dt;
            if (_t >= 1f) { _step = Step.Cruise; _speed = 3f * 2f / liftTime; _climb = liftHeight / liftTime; }
        }

        // ---- cruise: out over the sea toward the island, round it until the island scene is ready

        void StepCruise(float dt)
        {
            _flightTime += dt;
            var bt = bird.transform;
            Vector3 pos = bt.position;

            _speed = Mathf.MoveTowards(_speed, cruiseSpeed, cruiseSpeed / Mathf.Max(0.1f, speedUpTime) * dt);
            float altLeft = cruiseAltitude - pos.y;
            _climb = Mathf.MoveTowards(_climb, Mathf.Clamp(altLeft * 0.4f, -climbRate, climbRate), 4f * dt);

            // head for the island; close to it, fly round it (keep it on the right)
            Vector3 toC = islandCenter - pos; toC.y = 0f;
            float dist = toC.magnitude;
            Vector3 want = toC.normalized;
            if (dist < circleRadius * 1.15f)
            {
                Vector3 tangent = Vector3.Cross(Vector3.up, toC.normalized) * -1f;   // clockwise seen from above
                want = (tangent + toC.normalized * ((dist - circleRadius) / circleRadius) * 2f).normalized;
            }
            float wantYaw = Mathf.Atan2(want.x, want.z) * Mathf.Rad2Deg;
            float turn = Mathf.Clamp(Mathf.DeltaAngle(_heading, wantYaw) * 0.5f, -turnRate, turnRate);
            _heading += turn * dt;
            _bank = Mathf.Lerp(_bank, -turn * bankPerTurn, 1f - Mathf.Exp(-2f * dt));

            Vector3 fwd = Quaternion.Euler(0f, _heading, 0f) * Vector3.forward;
            pos += fwd * (_speed * dt) + Vector3.up * (_climb * dt);
            float pitch = bodyPitch - Mathf.Clamp(_climb, -6f, 12f) * 0.8f;   // nose up while climbing
            bt.SetPositionAndRotation(pos, Quaternion.Euler(pitch, _heading, _bank));

            // flap while climbing; up high, flap a while and glide a while
            bool climbing = _climb > 3f;
            _flapClock += dt;
            float cycle = flapGlide.x + flapGlide.y;
            bool gliding = !climbing && (_flapClock % cycle) > flapGlide.x;
            Flap(dt, climbing ? _baseFlap * 1.25f : _baseFlap, gliding ? 0.08f : 1f);
            HangBasket(dt);

            if (_step == Step.Cruise && _flightTime >= minFlight && (_load == null || _load.progress >= 0.9f))
            {
                _step = Step.Fade; _fade = 0f;
            }
            if (_step == Step.Fade)
            {
                _fade = Mathf.Min(1f, _fade + dt / Mathf.Max(0.05f, fadeTime));
                if (_fade >= 1f && _load != null && !_load.allowSceneActivation)
                {
                    var go = new GameObject("FlightFadeIn");
                    DontDestroyOnLoad(go);
                    go.AddComponent<FlightFadeIn>().time = fadeTime;
                    Application.backgroundLoadingPriority = ThreadPriority.Normal;
                    _load.allowSceneActivation = true;
                }
            }
        }

        void Flap(float dt, float rate, float amp)
        {
            _flapAmp = Mathf.MoveTowards(_flapAmp, amp, dt * 2f);
            // a gliding wing stops near level instead of freezing mid-beat
            float r = rate * Mathf.Lerp(0.4f, 1f, _flapAmp);
            _flapPhase = (_flapPhase + dt * r) % 1f;
            bird.flapAngle = _baseFlapAngle * _flapAmp;
            bird.Flap(_flapPhase);
            bird.flapAngle = _baseFlapAngle;
        }

        /// <summary>The basket hangs from the ring at the bird's feet and swings behind when the bird speeds up or turns.</summary>
        void HangBasket(float dt)
        {
            Vector3 pivot = bird.transform.position;
            Vector3 next = _bob + (_bob - _bobPrev) * 0.96f + Physics.gravity * (dt * dt);
            _bobPrev = _bob;
            Vector3 d = next - pivot;
            if (d.sqrMagnitude < 1e-6f) d = Vector3.down;
            _bob = pivot + d.normalized * ringHeight;
            // keep the swing small (the ropes are short and stiff)
            Vector3 hang = _bob - pivot;
            float ang = Vector3.Angle(Vector3.down, hang);
            if (ang > 20f) _bob = pivot + Vector3.Slerp(Vector3.down, hang.normalized, 20f / ang) * ringHeight;

            Quaternion yaw = Quaternion.Euler(0f, _basketYaw0 + (_heading - _heading0), 0f);
            Quaternion tilt = Quaternion.FromToRotation(Vector3.up, (pivot - _bob).normalized);
            basket.SetPositionAndRotation(_bob, tilt * yaw);
        }

        // ---- camera: behind the bird, the mouse turns it round

        void StepCamera(float dt)
        {
            var bt = bird.transform;
            Vector3 pivot = bt.position + Vector3.up * 0.6f;   // between the bird and the basket
            // turn the view part of the way toward the island
            Vector3 toIsland = islandCenter - bt.position;
            float islandYaw = Mathf.Atan2(toIsland.x, toIsland.z) * Mathf.Rad2Deg;
            float wantView = _heading + Mathf.Clamp(Mathf.DeltaAngle(_heading, islandYaw) * camToIsland.x, -camToIsland.y, camToIsland.y);
            _viewYaw = Mathf.LerpAngle(_viewYaw, wantView, 1f - Mathf.Exp(-1.5f * dt));
            Quaternion head = Quaternion.Euler(0f, _viewYaw, 0f);

            var mouse = Mouse.current;
            Vector2 md = mouse != null ? mouse.delta.ReadValue() * 0.08f : Vector2.zero;
            if (md.sqrMagnitude > 0.0001f) _orbitIdle = 0f; else _orbitIdle += dt;
            _orbitYaw = Mathf.Clamp(_orbitYaw + md.x, -orbitLimit.x, orbitLimit.x);
            _orbitPitch = Mathf.Clamp(_orbitPitch - md.y, -orbitLimit.y * 0.5f, orbitLimit.y);
            if (_orbitIdle > 2f)
            {
                float back = 1f - Mathf.Exp(-1.2f * dt);
                _orbitYaw = Mathf.Lerp(_orbitYaw, 0f, back);
                _orbitPitch = Mathf.Lerp(_orbitPitch, 0f, back);
            }

            Quaternion orbit = head * Quaternion.Euler(_orbitPitch, _orbitYaw, 0f);
            Vector3 camPos = pivot + orbit * new Vector3(0f, camUp, -camBack);
            Quaternion camRot = head * Quaternion.Euler(camPitch + _orbitPitch, _orbitYaw, 0f);

            _camT = Mathf.Min(1f, _camT + dt / Mathf.Max(0.05f, camBlend));
            float k = Smooth(_camT);
            cam.transform.SetPositionAndRotation(Vector3.Lerp(_camFromPos, camPos, k), Quaternion.Slerp(_camFromRot, camRot, k));
            cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, camFov, k);
            if (_camT > 0.3f) { cam.nearClipPlane = 0.3f; cam.farClipPlane = 25000f; }   // the sea reaches 20 km out
        }

        static float Smooth(float x) => x * x * (3f - 2f * x);

        void OnGUI()
        {
            if (_step != Step.Fade) return;
            DrawWhite(_fade);
        }

        internal static void DrawWhite(float a)
        {
            GUI.color = new Color(0.93f, 0.95f, 0.97f, a);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = Color.white;
        }
    }

    /// <summary>Carried into the island scene: the white of the cloud fades out, then it goes away.</summary>
    public class FlightFadeIn : MonoBehaviour
    {
        public float time = 1.2f;
        float _a = 1f, _wait = 0.15f;

        void Update()
        {
            if (_wait > 0f) { _wait -= Time.unscaledDeltaTime; return; }   // the first frames of a new scene can stall
            _a -= Time.unscaledDeltaTime / Mathf.Max(0.05f, time);
            if (_a <= 0f) Destroy(gameObject);
        }

        void OnGUI() => CarrierFlight.DrawWhite(Mathf.Clamp01(_a));
    }
}
