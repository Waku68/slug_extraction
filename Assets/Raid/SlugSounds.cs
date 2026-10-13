using System.Collections.Generic;
using SlugMove;
using UnityEngine;

namespace SlugExtract
{
    /// <summary>
    /// Plays the sound effects of the player's slug from the events the tools, the bag and the world already send
    /// (pickaxe, guns, knife, harvesting, picking, the bag, being hit, switching tools). What plays is in the
    /// <see cref="SoundBank"/>; sounds of the slug's own doings come from its body, hits from where they land.
    /// </summary>
    public class SlugSounds : MonoBehaviour
    {
        public SoundBank bank;
        [Tooltip("ほかのナメクジ（ボット、あとでほかの人）の音：その場所から聞こえ、遠くまで届く（銃声は遠く、ほかは近く）")] public bool other;

        AudioSource _source;
        SlugBag _bag;
        SlugToolHolder _pickaxe;
        SlugGunHolder[] _guns = new SlugGunHolder[0];
        System.Action<Vector3, Vector3>[] _fired = new System.Action<Vector3, Vector3>[0];
        SlugKnifeHolder _knife;
        SlugLoadout _loadout;
        SlugHitboxes _hitboxes;
        SlugMoveController _move;
        static readonly Dictionary<(float, float), AnimationCurve> _fades = new Dictionary<(float, float), AnimationCurve>();

        void Awake()
        {
            _source = gameObject.AddComponent<AudioSource>();
            _source.playOnAwake = false;
            _source.spatialBlend = other ? 1f : 0.3f;     // one's own doings sound close, a little placed
            var root = transform.root;
            _bag = root.GetComponentInChildren<SlugBag>(true);
            _pickaxe = root.GetComponentInChildren<SlugToolHolder>(true);
            _guns = root.GetComponentsInChildren<SlugGunHolder>(true);
            MakeFired();
            _knife = root.GetComponentInChildren<SlugKnifeHolder>(true);
            _loadout = root.GetComponentInChildren<SlugLoadout>(true);
            _hitboxes = root.GetComponentInChildren<SlugHitboxes>(true);
            _move = root.GetComponentInChildren<SlugMoveController>(true);
        }

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
            if (_hitboxes) _hitboxes.Hit += OnHurt;
            if (_move) { _move.Lunged += OnLunge; _move.Landed += OnLand; }
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
            if (_hitboxes) _hitboxes.Hit -= OnHurt;
            if (_move) { _move.Lunged -= OnLunge; _move.Landed -= OnLand; }
            Harvestable.Harvested -= OnHarvested;
            Pickable.Crushed -= OnCrushed;
            GroundBag.Burst -= OnGroundBurst;
        }

        // ------------------------------------------------------------------ what plays when
        void OnBag(string what, ItemKind kind)
        {
            string key = what == "pick" ? "pick_pluck" : what == "swap" ? "bag_up" : "bag_" + what;
            Play(key + "_" + kind, key);
        }

        void OnSwing() => Play("pickaxe_swing");

        void OnPickaxe(RaycastHit hit, IPickaxeTarget target)
        {
            var surface = hit.collider ? hit.collider.GetComponentInParent<IPickaxeSurface>() : null;
            var m = surface != null ? surface.PickaxeMaterial : PickaxeMaterial.Default;
            PlayAt(m == PickaxeMaterial.Hard ? "pickaxe_hit_salt" : m == PickaxeMaterial.Soft ? "pickaxe_hit_soft" : "pickaxe_hit_default", hit.point);
        }

        void OnHarvested(GameObject who, ItemKind kind, int amount, bool full)
        {
            if (amount <= 0 || !who || who.transform.root != transform.root) return;      // someone else's pickaxe
            Play("harvest_pop");
            if (kind == ItemKind.RockSalt) Play("harvest_stage");   // the salt mushroom changes shape when one comes out
        }

        void OnCrushed(Vector3 at) => PlayAt("crush_squish", at);

        void OnGroundBurst(Vector3 at) => PlayAt("bag_burst", at);

        void OnFired(SlugGunHolder gun)
        {
            string key = "gun_fire_" + gun.gun.kind.ToString().ToLowerInvariant();
            if (other) PlayAt(key, transform.position + Vector3.up * 0.4f, 10f, 400f);    // a shot is heard far off
            else Play(key);
        }

        void OnRoundHit(RaycastHit hit)
        {
            bool slug = hit.collider && hit.collider.GetComponentInParent<SlugHitbox>() != null;
            PlayAt(slug ? "round_hit_slug" : "round_hit_world", hit.point);
        }

        void OnCue(string cue) => Play("gun_reload_" + cue);

        void OnKnife(RaycastHit hit, IShotTarget target) => PlayAt("knife_stab", hit.point);

        void OnSwitch() => Play("tool_switch");

        void OnHurt(SlugHitbox box, RaycastHit hit) => Play("slug_hurt");

        /// <summary>The stretch (bigger the longer it was charged), and what is in the bag clattering, louder the heavier.</summary>
        void OnLunge(float charge)
        {
            PlayScaled(Mathf.Lerp(0.6f, 1f, charge), "slug_lunge");
            float heavy = _bag && _bag.settings ? _bag.Weight / (2f * Mathf.Max(_bag.settings.weightThreshold, 1e-3f)) : 0f;
            if (heavy > 0.05f) PlayScaled(Mathf.Clamp01(heavy), "bag_rattle");
        }

        void OnLand() => Play("slug_land");

        // ------------------------------------------------------------------ playing
        /// <summary>From the slug's body. The first key that has clips plays (a version for one item, then the plain one).</summary>
        void Play(params string[] keys) => PlayScaled(1f, keys);

        /// <summary><see cref="Play"/> at a part of its loudness.</summary>
        void PlayScaled(float loudness, params string[] keys)
        {
            var e = Pick(keys);
            if (e == null) return;
            if (other) { PlayEntry(e, transform.position + Vector3.up * 0.3f, 2f, 60f, loudness); return; }
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
