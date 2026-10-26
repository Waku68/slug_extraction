using UnityEngine;

namespace SlugMove
{
    /// <summary>
    /// How a gun is handled, as a handful of springs and timers (no world queries; <see cref="SlugGunHolder"/> turns it
    /// into poses and each <see cref="SlugGunModel"/> moves its own parts from it):
    ///  - raise: carried (0) or at the shoulder (1); the trigger (or the scope) raises it, it stays up a moment after the
    ///    last shot and while the action is worked;
    ///  - every shot: the gun is pushed back into the shoulder, the muzzle flips up with a little random yaw and roll,
    ///    the soft upper body rocks back, the fists squeeze, the spread grows;
    ///  - the action: a pump or a bolt worked by hand after every shot (no shot until it is done);
    ///  - reloading: a whole magazine (pulled out, taken away, brought back full), one round at a time (open, insert
    ///    rounds, close; pulling the trigger stops loading after the round in hand), or salt poured in from the top
    ///    (open, pour, close; it fills at a steady rate and the trigger stops it at once);
    ///  - scope: eased in and out on the aim button; equip: taken in hand over a given time (longer from the back).
    /// Kicks are velocity impulses into damped springs, so a burst piles up and settles on its own.
    /// </summary>
    public class GunHandling
    {
        public enum Stage { None, Magazine, Open, Insert, Pour, Close }

        public GunSettings settings;

        /// <summary>Moments a sound or an effect would go with: "eject" (pump / bolt pulled back), "seat" (closed / magazine
        /// seated), "insert" (a round goes in), "open" (ready to load).</summary>
        public event System.Action<string> Cue;

        public int Ammo { get; private set; }
        public int ShotsInBurst { get; private set; }
        /// <summary>0 = carried, 1 = at the shoulder (eased).</summary>
        public float Raise => Smooth(_raise);
        /// <summary>0 = just taken in hand (still down out of the way), 1 = ready.</summary>
        public float Equip => Smooth(_equip);
        /// <summary>0..1 through taking it in hand, not eased (the holder splits it into reaching for it and bringing it up).</summary>
        public float EquipLinear => _equip;
        /// <summary>0..1: how far the scope (or the iron sights) is up to the eye (eased).</summary>
        public float Scope => Smooth(_scope);
        /// <summary>The gun has iron sights to look along on the aim button (set by the holder from the model).</summary>
        public bool sights;
        public bool Reloading => _stage != Stage.None;
        public Stage ReloadStage => _stage;
        /// <summary>0..1 through the current reload stage (the whole magazine change, or one open / insert / close).</summary>
        public float StageProgress
        {
            get
            {
                var s = settings;
                switch (_stage)
                {
                    case Stage.Magazine: return Mathf.Clamp01(_stageT / Mathf.Max(s.reloadTime, 0.05f));
                    case Stage.Open: return Mathf.Clamp01(_stageT / Mathf.Max(s.reloadOpenTime, 0.05f));
                    case Stage.Insert: return Mathf.Clamp01(_stageT / Mathf.Max(s.insertTime, 0.05f));
                    case Stage.Pour: return Mathf.Repeat(_stageT / 0.45f, 1f);      // free-running: the pouring hand's little shake
                    case Stage.Close: return Mathf.Clamp01(_stageT / Mathf.Max(s.reloadCloseTime, 0.05f));
                    default: return 0f;
                }
            }
        }
        /// <summary>Kept for the magazine reload: 0..1 through it.</summary>
        public float ReloadProgress => _stage == Stage.Magazine ? StageProgress : 0f;
        /// <summary>True while the pump or bolt is being worked after a shot.</summary>
        public bool Cycling => _cycling;
        /// <summary>0..1 through working the pump or bolt (0 before it starts).</summary>
        public float Cycle => _cycling ? Mathf.Clamp01(_cycleT / Mathf.Max(settings.cycleTime, 0.05f)) : 0f;
        /// <summary>The current spread (cone half-angle, degrees) without movement or the scope.</summary>
        public float Spread => settings.spreadBase + _bloom;
        public float SinceShot => _sinceShot;

        // recoil state: kick (m, back along the bore), pitch (deg, + = muzzle up), yaw, roll, lean (deg, + = forward)
        public float Kick => _kick;
        public float Pitch => _pitch;
        public float Yaw => _yaw;
        public float Roll => _roll;
        public float Lean => _lean;
        /// <summary>0..1: how hard the fists are squeezing right now (a pulse on every shot).</summary>
        public float Squeeze => _squeeze;

        const float CycleDelay = 0.08f;      // the action is worked a moment after the shot, once the kick has landed
        const float ClickBuffer = 0.25f;     // a click this early before the gun is ready still fires when it is

        float _raise, _equip = 1f, _scope, _equipTime = -1f;
        float _kick, _kickV, _pitch, _pitchV, _yaw, _yawV, _roll, _rollV, _lean, _leanV;
        float _squeeze, _bloom, _cool, _sinceShot = 99f, _queued;
        bool _prevTrigger, _cycling, _ejected, _cycleSeated;
        float _cycleT;
        Stage _stage;
        float _stageT;
        bool _seated, _inserted, _stopLoading;
        float _poured;

        public void Reset()
        {
            Ammo = settings.magazineSize;
            _raise = 0f; _equip = 1f; _scope = 0f;
            _kick = _kickV = _pitch = _pitchV = _yaw = _yawV = _roll = _rollV = _lean = _leanV = 0f;
            _squeeze = _bloom = _cool = _queued = 0f;
            _sinceShot = 99f;
            _cycling = false;
            _stage = Stage.None;
            ShotsInBurst = 0;
        }

        /// <summary>
        /// Takes the gun in hand over <paramref name="time"/> seconds (negative = the gun's own equip time, 0 = at once);
        /// a reload in progress starts over, a half-worked action finishes.
        /// </summary>
        public void Draw(float time = -1f)
        {
            _equipTime = time;
            _equip = time == 0f ? 1f : 0f;
            _raise = 0f;
            _scope = 0f;
            if (_stage == Stage.Magazine || _stage == Stage.Open) _stageT = 0f;
        }

        static float Smooth(float x)
        {
            x = Mathf.Clamp01(x);
            return x * x * (3f - 2f * x);
        }

        static void Spring(ref float x, ref float v, float target, float hz, float damping, float dt)
        {
            float w = 2f * Mathf.PI * hz;
            v += (w * w * (target - x) - 2f * damping * w * v) * dt;
            x += v * dt;
        }

        /// <summary>The velocity that makes a spring of <paramref name="hz"/> swing out by about <paramref name="peak"/>.</summary>
        static float Impulse(float peak, float hz) => peak * 2f * Mathf.PI * hz * 2.2f;

        /// <summary>
        /// Advances everything by <paramref name="dt"/>. <paramref name="trigger"/> = the fire button is held,
        /// <paramref name="aim"/> = the aim button is held (only guns with a scope or iron sights use it).
        /// </summary>
        public void Step(float dt, bool trigger, bool aim)
        {
            var s = settings;
            _sinceShot += dt;
            _equip = Mathf.MoveTowards(_equip, 1f, dt / Mathf.Max(_equipTime > 0f ? _equipTime : s.equipTime, 1e-3f));

            bool pressed = trigger && !_prevTrigger;
            _prevTrigger = trigger;
            _queued = pressed ? ClickBuffer : Mathf.Max(0f, _queued - dt);
            if (!trigger) ShotsInBurst = 0;

            // the trigger with an empty gun: reload. During a one-at-a-time reload: stop after the round in hand.
            if (pressed && Ammo <= 0 && !Reloading && _equip >= 1f) StartReload();
            if (pressed && (_stage == Stage.Open || _stage == Stage.Insert || _stage == Stage.Pour) && Ammo > 0) _stopLoading = true;

            bool hasScope = s.scopeFov > 0f;
            bool scoped = aim && (hasScope || sights) && !Reloading && _equip >= 1f;
            bool up = !Reloading && _equip >= 1f && (trigger || scoped || _sinceShot < s.holdTime || _cycling);
            _raise = Mathf.MoveTowards(_raise, up ? 1f : 0f, dt / Mathf.Max(up ? s.raiseTime : s.lowerTime, 1e-3f));
            _scope = Mathf.MoveTowards(_scope, scoped && _raise > 0.5f ? 1f : 0f, dt / Mathf.Max(hasScope ? s.scopeTime : s.sightTime, 1e-3f));
            _cool = Mathf.Max(0f, _cool - dt);

            if (_cycling) StepCycle(dt);
            if (Reloading) StepReload(dt);

            float hz = s.kickSpringHz, d = s.kickDamping;
            Spring(ref _kick, ref _kickV, 0f, hz, d, dt);
            Spring(ref _pitch, ref _pitchV, 0f, hz, d, dt);
            Spring(ref _yaw, ref _yawV, 0f, hz, d, dt);
            Spring(ref _roll, ref _rollV, 0f, hz, d, dt);
            Spring(ref _lean, ref _leanV, s.readyLean * Raise, s.leanSpringHz, s.leanDamping, dt);
            _squeeze *= Mathf.Exp(-25f * dt);
            if (_sinceShot > 0.12f) _bloom = Mathf.Max(0f, _bloom - s.spreadRecovery * dt);
        }

        public bool CanFire => !Reloading && !_cycling && Ammo > 0 && _equip >= 1f && _raise >= settings.fireAtRaise;

        /// <summary>
        /// Fires one round if the trigger asks for it (held for full auto, a fresh click otherwise), the gun is up and the
        /// next round is due. Call in a loop after <see cref="Step"/>.
        /// </summary>
        public bool TryFire(bool trigger)
        {
            var s = settings;
            bool want = s.fireMode == FireMode.Auto ? trigger : _queued > 0f;
            if (!want || !CanFire || _cool > 0f) return false;
            _cool = 60f / Mathf.Max(s.fireRate, 1f);
            _queued = 0f;
            Ammo--;
            ShotsInBurst++;
            _sinceShot = 0f;

            float hz = s.kickSpringHz;
            _kickV += Impulse(s.kickBack, hz);
            _pitchV += Impulse(s.kickPitch * Random.Range(0.8f, 1.2f), hz);
            _yawV += Impulse(s.kickYaw * Random.Range(-1f, 1f), hz);
            _rollV += Impulse(s.kickRoll * Random.Range(-1f, 1f), hz);
            _leanV -= Impulse(s.shoulderLean, s.leanSpringHz);
            _squeeze = 1f;
            _bloom = Mathf.Min(_bloom + s.spreadPerShot, s.spreadMax);
            if (s.action != GunAction.None)
            {
                _cycling = true;
                _cycleT = -CycleDelay;
                _ejected = _cycleSeated = false;
            }
            return true;
        }

        // ------------------------------------------------------------------ the pump / bolt after a shot
        void StepCycle(float dt)
        {
            _cycleT += dt;
            float u = Cycle;
            if (u >= 0.4f && !_ejected) { _ejected = true; Cue?.Invoke("eject"); }
            if (u >= 0.75f && !_cycleSeated) { _cycleSeated = true; Cue?.Invoke("seat"); }
            if (_cycleT >= settings.cycleTime) _cycling = false;
        }

        // ------------------------------------------------------------------ reload
        // magazine, u: 0 tilt the gun → 0.18 pull the magazine → 0.36 take it away (refilled while away) → 0.6 bring it
        //    back → 0.78 seat it with a jolt → 1 back to ready
        const float PullAt = 0.18f, AwayAt = 0.36f, BackAt = 0.6f, SeatAt = 0.78f;
        /// <summary>One round at a time: the moment within an insert when the round goes in.</summary>
        public const float InsertAt = 0.72f;

        public void StartReload()
        {
            if (Reloading || Ammo >= settings.magazineSize) return;
            _cycling = false;
            _stage = settings.reloadStyle == ReloadStyle.Magazine ? Stage.Magazine : Stage.Open;
            _stageT = 0f;
            _seated = _inserted = _stopLoading = false;
            _poured = 0f;
        }

        void StepReload(float dt)
        {
            var s = settings;
            _stageT += dt;
            float u = StageProgress;
            switch (_stage)
            {
                case Stage.Magazine:
                    if (u >= 0.5f && Ammo < s.magazineSize) Ammo = s.magazineSize;      // the magazine comes back full of salt
                    if (u >= SeatAt && !_seated) { _seated = true; Jolt(); Cue?.Invoke("seat"); }
                    if (u >= 1f) Finish();
                    break;
                case Stage.Open:
                    if (u >= 1f)
                    {
                        Next(s.reloadStyle == ReloadStyle.Pour ? (_stopLoading ? Stage.Close : Stage.Pour) : Stage.Insert);
                        Cue?.Invoke("open");
                    }
                    break;
                case Stage.Pour:
                    // the salt runs in at a steady rate; the trigger stops it at once
                    _poured += s.pourRate * dt;
                    while (_poured >= 1f && Ammo < s.magazineSize) { Ammo++; _poured -= 1f; }
                    if (Ammo >= s.magazineSize || _stopLoading) Next(Stage.Close);
                    break;
                case Stage.Insert:
                    // stopping before the hand has reached the gun drops this round
                    if (_stopLoading && u < 0.35f) { Next(Stage.Close); break; }
                    if (u >= InsertAt && !_inserted)
                    {
                        _inserted = true;
                        Ammo = Mathf.Min(Ammo + 1, s.magazineSize);
                        _squeeze = 0.4f;
                        Cue?.Invoke("insert");
                    }
                    if (u >= 1f)
                    {
                        if (Ammo >= s.magazineSize || _stopLoading) Next(Stage.Close);
                        else { _stageT = 0f; _inserted = false; }
                    }
                    break;
                case Stage.Close:
                    if (u >= 0.6f && !_seated) { _seated = true; Jolt(); Cue?.Invoke("seat"); }
                    if (u >= 1f) Finish();
                    break;
            }
        }

        void Next(Stage stage)
        {
            _stage = stage;
            _stageT = 0f;
            _inserted = false;
        }

        void Finish()
        {
            _stage = Stage.None;
            _bloom = 0f;
        }

        /// <summary>Click: the gun jolts in the hands.</summary>
        void Jolt()
        {
            var s = settings;
            _kickV += Impulse(s.kickBack * 0.3f, s.kickSpringHz);
            _pitchV -= Impulse(s.kickPitch * 0.3f, s.kickSpringHz);
            _squeeze = 0.6f;
        }

        /// <summary>0..1: how far the gun is held tilted for the reload.</summary>
        public float ReloadTilt
        {
            get
            {
                float u = StageProgress;
                switch (_stage)
                {
                    case Stage.Magazine: return Smooth(u / 0.15f) * (1f - Smooth((u - 0.82f) / 0.18f));
                    case Stage.Open: return Smooth(u / 0.6f);
                    case Stage.Insert: case Stage.Pour: return 1f;
                    case Stage.Close: return 1f - Smooth((u - 0.3f) / 0.7f);
                    default: return 0f;
                }
            }
        }

        /// <summary>0..1: how far the magazine is pulled out along its own curve.</summary>
        public float MagazineOut
        {
            get
            {
                if (_stage != Stage.Magazine) return 0f;
                float u = StageProgress;
                return Smooth((u - PullAt) / (AwayAt - PullAt) * 1.4f) * (1f - Smooth((u - BackAt) / (SeatAt - BackAt)));
            }
        }

        /// <summary>0..1: how far the right hand has taken the magazine away (to refill it).</summary>
        public float MagazineAway
        {
            get
            {
                if (_stage != Stage.Magazine) return 0f;
                float u = StageProgress;
                return Smooth((u - (PullAt + AwayAt) * 0.5f) / (AwayAt - PullAt)) * (1f - Smooth((u - (AwayAt + BackAt) * 0.5f) / (BackAt - AwayAt) * 1.2f));
            }
        }
    }
}
