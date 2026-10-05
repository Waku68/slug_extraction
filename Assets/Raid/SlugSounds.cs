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

        AudioSource _source;
        SlugBag _bag;
        SlugToolHolder _pickaxe;
        SlugGunHolder[] _guns = new SlugGunHolder[0];
        System.Action<Vector3, Vector3>[] _fired = new System.Action<Vector3, Vector3>[0];
        SlugKnifeHolder _knife;
        SlugLoadout _loadout;
        SlugHitboxes _hitboxes;

        void Awake()
        {
            _source = gameObject.AddComponent<AudioSource>();
            _source.playOnAwake = false;
            _source.spatialBlend = 0.3f;     // one's own doings sound close, a little placed
            var root = transform.root;
            _bag = root.GetComponentInChildren<SlugBag>(true);
            _pickaxe = root.GetComponentInChildren<SlugToolHolder>(true);
            _guns = root.GetComponentsInChildren<SlugGunHolder>(true);
            _fired = new System.Action<Vector3, Vector3>[_guns.Length];
            for (int i = 0; i < _guns.Length; i++) { var gun = _guns[i]; _fired[i] = (eye, dir) => OnFired(gun); }
            _knife = root.GetComponentInChildren<SlugKnifeHolder>(true);
            _loadout = root.GetComponentInChildren<SlugLoadout>(true);
            _hitboxes = root.GetComponentInChildren<SlugHitboxes>(true);
        }

        void OnEnable()
        {
            if (_bag) _bag.Did += OnBag;
            if (_pickaxe) { _pickaxe.SwingStarted += OnSwing; _pickaxe.Struck += OnPickaxe; }
            for (int i = 0; i < _guns.Length; i++) { _guns[i].Fired += _fired[i]; _guns[i].RoundHit += OnRoundHit; _guns[i].Cue += OnCue; }
            if (_knife) _knife.Struck += OnKnife;
            if (_loadout) _loadout.Changed += OnSwitch;
            if (_hitboxes) _hitboxes.Hit += OnHurt;
            Harvestable.Harvested += OnHarvested;
            Pickable.Crushed += OnCrushed;
        }

        void OnDisable()
        {
            if (_bag) _bag.Did -= OnBag;
            if (_pickaxe) { _pickaxe.SwingStarted -= OnSwing; _pickaxe.Struck -= OnPickaxe; }
            for (int i = 0; i < _guns.Length; i++) { _guns[i].Fired -= _fired[i]; _guns[i].RoundHit -= OnRoundHit; _guns[i].Cue -= OnCue; }
            if (_knife) _knife.Struck -= OnKnife;
            if (_loadout) _loadout.Changed -= OnSwitch;
            if (_hitboxes) _hitboxes.Hit -= OnHurt;
            Harvestable.Harvested -= OnHarvested;
            Pickable.Crushed -= OnCrushed;
        }

        // ------------------------------------------------------------------ what plays when
        void OnBag(string what, ItemKind kind)
        {
            string key = what switch { "pick" => "pick_pluck", "eco" => "bag_eco_open", _ => "bag_" + what };
            Play(key + "_" + kind, key);
        }

        void OnSwing() => Play("pickaxe_swing");

        void OnPickaxe(RaycastHit hit, IPickaxeTarget target)
        {
            var surface = hit.collider ? hit.collider.GetComponentInParent<IPickaxeSurface>() : null;
            var m = surface != null ? surface.PickaxeMaterial : PickaxeMaterial.Default;
            PlayAt(m == PickaxeMaterial.Hard ? "pickaxe_hit_salt" : m == PickaxeMaterial.Soft ? "pickaxe_hit_soft" : "pickaxe_hit_default", hit.point);
        }

        void OnHarvested(ItemKind kind, int amount, bool full)
        {
            if (amount <= 0) return;
            Play("harvest_pop");
            if (kind == ItemKind.RockSalt) Play("harvest_stage");   // the salt mushroom changes shape when one comes out
        }

        void OnCrushed(Vector3 at) => PlayAt("crush_squish", at);

        void OnFired(SlugGunHolder gun) => Play("gun_fire_" + gun.gun.kind.ToString().ToLowerInvariant());

        void OnRoundHit(RaycastHit hit)
        {
            bool slug = hit.collider && hit.collider.GetComponentInParent<SlugHitbox>() != null;
            PlayAt(slug ? "round_hit_slug" : "round_hit_world", hit.point);
        }

        void OnCue(string cue) => Play("gun_reload_" + cue);

        void OnKnife(RaycastHit hit, IShotTarget target) => PlayAt("knife_stab", hit.point);

        void OnSwitch() => Play("tool_switch");

        void OnHurt(SlugHitbox box, RaycastHit hit) => Play("slug_hurt");

        // ------------------------------------------------------------------ playing
        /// <summary>From the slug's body. The first key that has clips plays (a version for one item, then the plain one).</summary>
        void Play(params string[] keys)
        {
            var e = Pick(keys);
            if (e == null) return;
            _source.pitch = 1f + Random.Range(-e.pitchJitter, e.pitchJitter);
            _source.PlayOneShot(e.clips[Random.Range(0, e.clips.Length)], e.volume * bank.masterVolume);
        }

        /// <summary>From a place in the world (where a hit lands).</summary>
        void PlayAt(string key, Vector3 at)
        {
            var e = Pick(key);
            if (e == null) return;
            var go = new GameObject("Sound_" + key);
            go.transform.position = at;
            var s = go.AddComponent<AudioSource>();
            s.spatialBlend = 1f;
            s.minDistance = 1.5f;
            s.maxDistance = 40f;
            s.pitch = 1f + Random.Range(-e.pitchJitter, e.pitchJitter);
            s.clip = e.clips[Random.Range(0, e.clips.Length)];
            s.volume = e.volume * bank.masterVolume;
            s.Play();
            Destroy(go, s.clip.length / Mathf.Max(s.pitch, 0.1f) + 0.1f);
        }

        SoundBank.Entry Pick(params string[] keys)
        {
            if (!bank) return null;
            foreach (var k in keys) { var e = bank.Find(k); if (e != null) return e; }
            return null;
        }
    }
}
