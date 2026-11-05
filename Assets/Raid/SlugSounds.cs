using System.Collections.Generic;
using SlugMove;
using UnityEngine;

namespace SlugExtract
{
    /// <summary>
    /// Plays the sound effects of the player's slug from the events the tools, the bag and the world already send
    /// (pickaxe, guns, knife, harvesting, picking, the bag, switching tools). Being hit makes no voice (Oct 27 わく): the
    /// shooter's round_hit_slug, heard where it lands, is the sound of it. What plays is in the
    /// <see cref="SoundBank"/>; sounds of the slug's own doings come from its body, hits from where they land.
    /// </summary>
    public class SlugSounds : MonoBehaviour
    {
        public SoundBank bank;
        [Tooltip("ほかのナメクジ（ボット、あとでほかの人）の音：その場所から聞こえ、遠くまで届く（銃声は遠く、ほかは近く）")] public bool other;
        [Tooltip("掘る音（ピッケル）が届く距離（m）。ふだんの移動で 10 秒で着く所まで。近いほど大きい（spec 13章 Oct 25）")] public float digHeard = 18f;
        [Tooltip("摘む音（キノコ・花を手で）が届く距離（m）。3 秒で着く所まで")] public float pickHeard = 5f;
        [Tooltip("ほかのナメクジの足音が届く距離（m）。音の地図の「這う」の 8 m")] public float stepHeard = 8f;
        [Tooltip("自分の足音の大きさ（1 = ほかの音と同じ）。1歩ごとに鳴るので小さめ")] public float ownStepLoudness = 0.22f;
        [Tooltip("ほかのナメクジ（ボット）の足音の大きさ（1 = ほかの音と同じ）")] public float otherStepLoudness = 0.45f;
        [Tooltip("ほかの人の銃声が遠くの音（gun_fire_far）に替わる距離（m）")] public float farShotFrom = 60f;
        [Tooltip("耳もとを弾がかすめたと聞こえる、弾の通り道と頭の近さ（m）")] public float flybyNear = 1.5f;

        AudioSource _source;
        AudioSource _squeeze;       // the shrink: plays while Jump is held, cut short on letting go
        SlugBag _bag;
        SlugToolHolder _pickaxe;
        SlugGunHolder[] _guns = new SlugGunHolder[0];
        System.Action<Vector3, Vector3>[] _fired = new System.Action<Vector3, Vector3>[0];
        SlugKnifeHolder _knife;
        SlugLoadout _loadout;
        SlugMoveController _move;
        static readonly Dictionary<(float, float), AnimationCurve> _fades = new Dictionary<(float, float), AnimationCurve>();

        void Awake()
        {
            _source = gameObject.AddComponent<AudioSource>();
            _source.playOnAwake = false;
            _source.spatialBlend = other ? 1f : 0.3f;     // one's own doings sound close, a little placed
            _squeeze = gameObject.AddComponent<AudioSource>();
            _squeeze.playOnAwake = false;
            _squeeze.spatialBlend = _source.spatialBlend;
            if (other)
            {
                _squeeze.rolloffMode = AudioRolloffMode.Custom;
                _squeeze.SetCustomCurve(AudioSourceCurveType.CustomRolloff, Fade(2f, 60f));
                _squeeze.maxDistance = 60f;
            }
            var root = transform.root;
            _bag = root.GetComponentInChildren<SlugBag>(true);
            _pickaxe = root.GetComponentInChildren<SlugToolHolder>(true);
            _guns = root.GetComponentsInChildren<SlugGunHolder>(true);
            MakeFired();
            _knife = root.GetComponentInChildren<SlugKnifeHolder>(true);
            _loadout = root.GetComponentInChildren<SlugLoadout>(true);
            _move = root.GetComponentInChildren<SlugMoveController>(true);
            _moisture = root.GetComponentInChildren<SlugMoisture>(true);
            _body = root.GetComponentInChildren<SlugDesign.DinoSlugBody>(true);
            _health = root.GetComponentInChildren<SlugHealth>(true);
        }

        SlugMoisture _moisture;
        SlugDesign.DinoSlugBody _body;
        SlugHealth _health;
        readonly Dictionary<SlugGunHolder, int> _firedFrame = new Dictionary<SlugGunHolder, int>();
        float _proneT, _dangerT, _flybyT;
        bool _wasClimbing, _wasDrinking;

        /// <summary>One handler per gun (made again after a script reload in play, which keeps the guns but not these).</summary>
        void MakeFired()
        {
            _fired = new System.Action<Vector3, Vector3>[_guns.Length];
            for (int i = 0; i < _guns.Length; i++) { var gun = _guns[i]; _fired[i] = (eye, dir) => OnFired(gun); }
        }

        void OnEnable()
        {
            if (_fired == null || _fired.Length != _guns.Length) MakeFired();
            if (_bag) _bag.Did += OnBag;
            if (_pickaxe) { _pickaxe.SwingStarted += OnSwing; _pickaxe.Struck += OnPickaxe; }
            for (int i = 0; i < _guns.Length; i++) { _guns[i].Fired += _fired[i]; _guns[i].RoundHit += OnRoundHit; _guns[i].Cue += OnCue; }
            if (_knife) _knife.Struck += OnKnife;
            if (_loadout) _loadout.Changed += OnSwitch;
            if (_move) { _move.Squeezed += OnSqueeze; _move.Lunged += OnLunge; _move.Landed += OnLand; }
            if (_moisture) { _moisture.DryingStarted += OnDrying; _moisture.WettingStarted += OnWetting; _moisture.StageChanged += OnDryStage; }
            if (_body) _body.Stepped += OnStep;
            if (_health) _health.Died += OnDied;
            if (!other) SlugGunHolder.AnyFired += OnAnyFired;
            Harvestable.Harvested += OnHarvested;
            Pickable.Crushed += OnCrushed;
            GroundBag.Burst += OnGroundBurst;
        }

        void OnDisable()
        {
            if (_bag) _bag.Did -= OnBag;
            if (_pickaxe) { _pickaxe.SwingStarted -= OnSwing; _pickaxe.Struck -= OnPickaxe; }
            for (int i = 0; i < _guns.Length && i < _fired.Length; i++) { _guns[i].Fired -= _fired[i]; _guns[i].RoundHit -= OnRoundHit; _guns[i].Cue -= OnCue; }
            if (_knife) _knife.Struck -= OnKnife;
            if (_loadout) _loadout.Changed -= OnSwitch;
            if (_move) { _move.Squeezed -= OnSqueeze; _move.Lunged -= OnLunge; _move.Landed -= OnLand; }
            if (_moisture) { _moisture.DryingStarted -= OnDrying; _moisture.WettingStarted -= OnWetting; _moisture.StageChanged -= OnDryStage; }
            if (_body) _body.Stepped -= OnStep;
            if (_health) _health.Died -= OnDied;
            SlugGunHolder.AnyFired -= OnAnyFired;
            Harvestable.Harvested -= OnHarvested;
            Pickable.Crushed -= OnCrushed;
            GroundBag.Burst -= OnGroundBurst;
        }

        // ------------------------------------------------------------------ what plays when
        void OnBag(string what, ItemKind kind)
        {
            string key = what == "pick" ? "pick_pluck" : what == "swap" ? "bag_up" : "bag_" + what;
            // someone else picking by hand: only close by
            if (other && what == "pick") { var e = Pick(key + "_" + kind, key); if (e != null) PlayEntry(e, transform.position + Vector3.up * 0.3f, 1f, pickHeard * RainHush); return; }
            Play(key + "_" + kind, key);
        }

        void OnSwing() => Play("pickaxe_swing");

        void OnPickaxe(RaycastHit hit, IPickaxeTarget target)
        {
            var surface = hit.collider ? hit.collider.GetComponentInParent<IPickaxeSurface>() : null;
            var m = surface != null ? surface.PickaxeMaterial : PickaxeMaterial.Default;
            PlayAt(m == PickaxeMaterial.Hard ? "pickaxe_hit_salt" : m == PickaxeMaterial.Soft ? "pickaxe_hit_soft" : "pickaxe_hit_default", hit.point, 1.5f, digHeard * (other ? RainHush : 1f));
        }

        void OnHarvested(GameObject who, ItemKind kind, int amount, bool full)
        {
            if (amount <= 0 || !who || who.transform.root != transform.root) return;      // someone else's pickaxe
            Play("harvest_pop");
            if (kind == ItemKind.RockSalt) Play("harvest_stage");   // the salt mushroom changes shape when one comes out
        }

        void OnCrushed(Vector3 at) => PlayAt("crush_squish", at);

        void OnGroundBurst(Vector3 at) => PlayAt("bag_burst", at);

        /// <summary>
        /// One shot (a shotgun's grains all come in the same frame: one sound). Someone else's is heard far off, by the gun
        /// (the sound map: pistol 250 m, rifle and shotgun 300 m, sniper 500 m), and past farShotFrom it is the far sound
        /// (a dull "don" and an echo). One's own says when the magazine runs low ("カラ") and when it is shot empty.
        /// </summary>
        void OnFired(SlugGunHolder gun)
        {
            if (_firedFrame.TryGetValue(gun, out int frame) && frame == Time.frameCount) return;
            _firedFrame[gun] = Time.frameCount;
            var kind = gun.gun.kind;
            string key = "gun_fire_" + kind.ToString().ToLowerInvariant();
            if (other)
            {
                float far = kind == GunKind.Sniper ? 500f : kind == GunKind.Pistol ? 250f : 300f;
                float near = kind == GunKind.Sniper ? 40f : 25f;
                Vector3 at = transform.position + Vector3.up * 0.4f;
                var listener = Listener;
                bool distant = listener && Vector3.Distance(listener.position, at) > farShotFrom && Pick("gun_fire_far") != null;
                PlayAt(distant ? "gun_fire_far" : key, at, near, far);
                return;
            }
            Play(key);
            var h = gun.Handling;
            if (h.Unlimited) return;
            if (h.Ammo <= 0) Play("gun_last_round");
            else if (h.Ammo <= gun.gun.magazineSize / 4) Play("gun_low_ammo");
        }

        /// <summary>Where it lands: the body, a helmet ("カン"), a shell plate, or the world. The shooter also hears that
        /// it hit a slug, in the ear ("ぷちっ"; on a tentacle, the weak spot, higher).</summary>
        void OnRoundHit(RaycastHit hit)
        {
            var box = hit.collider ? hit.collider.GetComponentInParent<SlugHitbox>() : null;
            string key = !box ? "round_hit_world" : box.armour == "ヘルメット" ? "round_hit_helmet" : box.armour == "殻板" ? "round_hit_shell" : "round_hit_slug";
            var e = Pick(key, box ? "round_hit_slug" : key);
            if (e != null) PlayEntry(e, hit.point, 1.5f, 40f);
            if (box && !other && box.transform.root != transform.root) Play(box.part == "触角" ? "hit_confirm_weak" : "hit_confirm", "hit_confirm");
        }

        /// <summary>Someone else's round passing close by the head: "シュッ" from where it went by (the player only).</summary>
        void OnAnyFired(SlugGunHolder gun, Vector3 eye, Vector3 dir)
        {
            if (!gun || gun.transform.root == transform.root || Time.time < _flybyT) return;
            Vector3 head = transform.root.position + Vector3.up * 0.45f;
            float along = Vector3.Dot(head - eye, dir);
            float range = gun.gun.muzzleSpeed * gun.gun.roundLife;
            if (along < 3f || along > range) return;
            Vector3 by = eye + dir * along;
            if (Vector3.Distance(by, head) > flybyNear) return;
            _flybyT = Time.time + 0.12f;
            PlayAt("round_flyby", by, 1f, 8f);
        }

        void OnDied(string cause) => PlayAt("slug_fall", transform.root.position, 2f, 40f);

        /// <summary>A foot comes down: the ground under it (puddle, salt, grass, rock, soil), or a dried-out "かさ".</summary>
        void OnStep(Vector3 at)
        {
            if (_move && _move.Prone01 > 0.5f) return;              // lying down: the belly's own sound
            string ground = StepGround(at);
            string key = ground != "puddle" && _moisture && _moisture.Stage >= 2 ? "slug_step_dry" : "slug_step_" + ground;
            var e = Pick(key, "slug_step_soil");
            if (e == null) return;
            if (other) { PlayEntry(e, at, 1f, stepHeard * RainHush, otherStepLoudness); return; }
            _source.pitch = 1f + Random.Range(-e.pitchJitter, e.pitchJitter);
            _source.PlayOneShot(e.clips[Random.Range(0, e.clips.Length)], e.volume * bank.masterVolume * ownStepLoudness);
        }

        /// <summary>The footstep's ground: water and salt by their zones, then the terrain layer's name; off the terrain
        /// (rocks, caves, the base's floors) rock.</summary>
        static string StepGround(Vector3 p)
        {
            if (SlugWorld.MoistureZone.Any(SlugWorld.MoistureZone.Kind.Water, p)) return "puddle";
            if (SlugWorld.MoistureZone.Any(SlugWorld.MoistureZone.Kind.Salt, p)) return "salt";
            string n = SlugWorld.GroundKinds.LayerNameAt(p);
            if (n == null) return "rock";
            if (n.Contains("salt")) return "salt";
            if (n.Contains("water") || n.Contains("puddle")) return "puddle";
            if (n.Contains("grass") || n.Contains("forest") || n.Contains("moss") || n.Contains("meadow")) return "grass";
            if (n.Contains("rock") || n.Contains("stone") || n.Contains("lava") || n.Contains("gravel") || n.Contains("cliff") || n.Contains("canyon")) return "rock";
            return "soil";
        }

        /// <summary>What hears: the audio listener (the player's camera).</summary>
        static Transform Listener
        {
            get
            {
                var l = FindAnyObjectByType<AudioListener>();
                return l ? l.transform : null;
            }
        }

        void OnCue(string cue) => Play("gun_reload_" + cue);

        void OnKnife(RaycastHit hit, IShotTarget target) => PlayAt("knife_stab", hit.point);

        void OnSwitch() => Play("tool_switch");

        /// <summary>The stretch (bigger the longer it was charged), and what is in the bag clattering, louder the heavier.</summary>
        void OnLunge(float charge)
        {
            PlayScaled(Mathf.Lerp(0.6f, 1f, charge), "slug_lunge");
            float heavy = _bag && _bag.settings ? _bag.Weight / (2f * Mathf.Max(_bag.settings.weightThreshold, 1e-3f)) : 0f;
            if (heavy > 0.05f) PlayScaled(Mathf.Clamp01(heavy), "bag_rattle");
        }

        void OnLand() => Play("slug_land");

        /// <summary>Rain drowns another slug's small sounds (its moving, digging, picking; not its shots): heard 40% as far
        /// in a downpour (spec 4章).</summary>
        static float RainHush => SlugWorld.WeatherPresenter.Current ? 1f - 0.6f * SlugWorld.WeatherPresenter.Current.Weather.rain : 1f;

        // moisture (spec 4章): said once at the moment it changes. Silent until the bank has clips for these keys.
        // Out into the sun / onto salt makes no sound since Nov 5 (わくさん: it felt wrong); the tentacles still flinch
        void OnDrying() { }
        void OnWetting() => Play("moisture_squelch");
        void OnDryStage(int now, int was) => Play(now > was ? "moisture_stage_drier_" + now : "moisture_stage_wetter_" + now, now > was ? "moisture_stage_drier" : "moisture_stage_wetter");

        /// <summary>The press: a squeak, then the body tightening as long as it is held (cut short on letting go).</summary>
        void OnSqueeze()
        {
            var e = Pick("slug_squeeze");
            if (e == null) return;
            _squeeze.Stop();
            _squeeze.clip = e.clips[Random.Range(0, e.clips.Length)];
            _squeeze.pitch = 1f + Random.Range(-e.pitchJitter, e.pitchJitter);
            _squeeze.volume = e.volume * bank.masterVolume;
            _squeeze.Play();
        }

        void Update()
        {
            // let go (or lay down): the squeeze dies away in a few hundredths of a second instead of clicking off
            if (_squeeze.isPlaying && _move && !_move.Charging)
            {
                _squeeze.volume = Mathf.MoveTowards(_squeeze.volume, 0f, Time.deltaTime / 0.04f);
                if (_squeeze.volume <= 0f) _squeeze.Stop();
            }
            Body();
        }

        /// <summary>The sounds that go on while something lasts: crawling on the belly, climbing up a ledge, drying
        /// dangerously ("ぴしっ" now and then), drinking a moss ball. No breathing when tired or weak since Nov 5 (わくさん:
        /// it felt wrong).</summary>
        void Body()
        {
            float t = Time.time;
            if (_move)
            {
                if (_move.Climbing && !_wasClimbing) Play("slug_climb");
                _wasClimbing = _move.Climbing;
                if (_move.Prone01 > 0.9f && _move.Speed > 0.1f && t >= _proneT) { Play("slug_prone"); _proneT = t + 0.9f; }
            }
            if (other) return;
            if (_moisture)
            {
                if (_moisture.Stage >= 3 && t >= _dangerT) { Play("slug_dry_danger"); _dangerT = t + Random.Range(2.5f, 4f); }
                bool drinking = _moisture.Using > 0f;
                if (drinking && !_wasDrinking) Play("moss_drink");
                _wasDrinking = drinking;
            }
        }

        // ------------------------------------------------------------------ playing
        /// <summary>From the slug's body. The first key that has clips plays (a version for one item, then the plain one).</summary>
        void Play(params string[] keys) => PlayScaled(1f, keys);

        /// <summary><see cref="Play"/> at a part of its loudness.</summary>
        void PlayScaled(float loudness, params string[] keys)
        {
            var e = Pick(keys);
            if (e == null) return;
            if (other) { PlayEntry(e, transform.position + Vector3.up * 0.3f, 2f, 60f * RainHush, loudness); return; }
            _source.pitch = 1f + Random.Range(-e.pitchJitter, e.pitchJitter);
            _source.PlayOneShot(e.clips[Random.Range(0, e.clips.Length)], e.volume * bank.masterVolume * loudness);
        }

        /// <summary>From a place in the world (where a hit lands).</summary>
        void PlayAt(string key, Vector3 at, float near = 1.5f, float far = 40f)
        {
            var e = Pick(key);
            if (e != null) PlayEntry(e, at, near, far);
        }

        /// <summary>One clip of <paramref name="e"/> from a place, full loud within <paramref name="near"/> m, fading to <paramref name="far"/>.</summary>
        void PlayEntry(SoundBank.Entry e, Vector3 at, float near, float far, float loudness = 1f)
        {
            var go = new GameObject("Sound_" + e.key);
            go.transform.position = at;
            var s = go.AddComponent<AudioSource>();
            s.spatialBlend = 1f;
            s.rolloffMode = AudioRolloffMode.Custom;     // the plain fall-off never reaches silence: still heard from anywhere
            s.SetCustomCurve(AudioSourceCurveType.CustomRolloff, Fade(near, far));
            s.maxDistance = far;
            s.pitch = 1f + Random.Range(-e.pitchJitter, e.pitchJitter);
            s.clip = e.clips[Random.Range(0, e.clips.Length)];
            s.volume = e.volume * bank.masterVolume * loudness;
            s.Play();
            Destroy(go, s.clip.length / Mathf.Max(s.pitch, 0.1f) + 0.1f);
        }

        /// <summary>
        /// Loudness by distance (as a part of <paramref name="far"/>): full within <paramref name="near"/>, then falling
        /// as near / distance like a real sound, and eased down to nothing at <paramref name="far"/>.
        /// </summary>
        static AnimationCurve Fade(float near, float far)
        {
            if (_fades.TryGetValue((near, far), out var c)) return c;
            c = new AnimationCurve();
            float n = Mathf.Clamp01(near / far);
            c.AddKey(0f, 1f);
            for (int i = 0; i <= 16; i++)
            {
                float x = Mathf.Lerp(n, 1f, Mathf.Pow(i / 16f, 2f));     // closer keys near the source, where it changes fast
                float v = Mathf.Min(1f, n / Mathf.Max(x, 1e-4f)) * (1f - x * x);
                c.AddKey(x, v);
            }
            for (int i = 0; i < c.length; i++) c.SmoothTangents(i, 0f);
            for (int i = 0; i < 2; i++) { var k = c[i]; k.inTangent = k.outTangent = 0f; c.MoveKey(i, k); }    // flat (not over full) up to near
            _fades[(near, far)] = c;
            return c;
        }

        SoundBank.Entry Pick(params string[] keys)
        {
            if (!bank) return null;
            foreach (var k in keys) { var e = bank.Find(k); if (e != null) return e; }
            return null;
        }
    }
}
