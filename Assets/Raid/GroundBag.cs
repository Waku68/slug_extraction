using System.Collections.Generic;
using SlugDesign;
using SlugMove;
using UnityEngine;

namespace SlugExtract
{
    /// <summary>
    /// A bag set down on the ground, its lid flopped open (its top things show in the mouth). It is the same bag as on
    /// the back (<see cref="SlugEquipment.BuildLooseBag"/>), swollen by what is in it. Taking things out of it and putting
    /// them in is quick (<see cref="ExtractSettings.groundTransferTime"/>), for anyone: its owner sorting it, or someone
    /// emptying a fallen slug's bag into their own (set their own down beside it, and both ways are quick). Anyone
    /// without a bag on the back can shoulder it. Rounds and the knife wear it down as on the back; at zero it bursts and
    /// everything in it spills. A fallen slug's bag lies here for a while (<see cref="ExtractSettings.droppedLifetime"/>),
    /// then rain and drying take it.
    /// </summary>
    public class GroundBag : MonoBehaviour, IShotTarget
    {
        public ExtractSettings settings;
        public BagKind kind;
        public float durability;
        /// <summary>The slug that set it down (null for a fallen slug's bag).</summary>
        public SlugBag owner;
        [Tooltip("消えるまでの時間（秒、0 = 消えない）")] public float lifetime;
        [Tooltip("バッグの大きさ（幅、高さ、厚み の半分, m）。背中のバッグと同じ")] public Vector3 bagSize = new Vector3(0.11f, 0.12f, 0.065f);

        /// <summary>What is in it, in the order it went in (the last is on top).</summary>
        public readonly List<ItemKind> items = new List<ItemKind>();

        /// <summary>Every bag lying on the ground.</summary>
        public static readonly List<GroundBag> All = new List<GroundBag>();
        /// <summary>A bag on the ground burst (where), for sounds.</summary>
        public static event System.Action<Vector3> Burst;

        float _age;
        bool _burst;
        Transform _look;
        Vector3 _half = new Vector3(0.11f, 0.12f, 0.065f);
        BoxCollider _box;
        static readonly Dictionary<Color, Material> Mats = new Dictionary<Color, Material>();

        void OnEnable() => All.Add(this);
        void OnDisable() => All.Remove(this);

        public BagDef Def => settings.Bag(kind);
        public bool Empty => items.Count == 0;
        Vector3 _mouth = new Vector3(0f, 0.24f, 0f);
        public Vector3 MouthWorld => transform.TransformPoint(_mouth);

        float Volume { get { float v = 0f; foreach (var i in items) v += settings.Def(i).volume; return v; } }

        public bool Fits(ItemKind k) => Def.capacity <= 0f || Volume + settings.Def(k).volume <= Def.capacity + 1e-4f;

        public void Add(ItemKind k) { items.Add(k); Build(); }

        public ItemKind TakeTop()
        {
            var k = items[items.Count - 1];
            items.RemoveAt(items.Count - 1);
            Build();
            return k;
        }

        /// <summary>Sets a bag down on the ground in front of <paramref name="at"/> (settled on whatever is below).</summary>
        public static GroundBag Create(ExtractSettings settings, BagKind kind, float durability, IEnumerable<ItemKind> items,
                                       Vector3 at, Quaternion rotation, SlugBag owner, float lifetime, Vector3? bagSize = null)
        {
            // the ground below, skipping the slugs themselves (the one setting it down stands right there)
            float best = float.MaxValue;
            foreach (var hit in Physics.RaycastAll(at + Vector3.up * 1f, Vector3.down, 3f, ~(1 << 2), QueryTriggerInteraction.Ignore))
            {
                if (hit.collider is CharacterController || hit.collider.GetComponentInParent<SlugBag>() || hit.collider.GetComponentInParent<GroundBag>()) continue;
                if (hit.distance < best) { best = hit.distance; at = hit.point; }
            }
            var go = new GameObject("GroundBag_" + kind);
            go.transform.SetPositionAndRotation(at, rotation);
            var g = go.AddComponent<GroundBag>();
            g.settings = settings; g.kind = kind; g.durability = durability; g.owner = owner; g.lifetime = lifetime;
            if (bagSize.HasValue) g.bagSize = bagSize.Value;
            g.items.AddRange(items);
            g._box = go.AddComponent<BoxCollider>();       // rounds and the knife meet it
            g.Build();
            return g;
        }

        /// <summary>The nearest bag on the ground within <paramref name="reach"/> of <paramref name="from"/> (optionally only one's own, or not one's own).</summary>
        public static GroundBag Nearest(Vector3 from, float reach, SlugBag own = null, bool? ownOnly = null)
        {
            GroundBag best = null;
            float bestD = reach;
            foreach (var g in All)
            {
                if (!g) continue;
                if (ownOnly == true && g.owner != own) continue;
                if (ownOnly == false && g.owner == own) continue;
                float d = Vector3.Distance(from, g.transform.position + Vector3.up * 0.15f);
                if (d < bestD) { bestD = d; best = g; }
            }
            return best;
        }

        void Update()
        {
            if (lifetime <= 0f) return;
            _age += Time.deltaTime;
            if (_age >= lifetime) Destroy(gameObject);
        }

        // ------------------------------------------------------------------ rounds and the knife
        /// <summary>Worn down like the bag on the back (by how fast the round came); at zero it bursts.</summary>
        public void OnShot(RaycastHit hit, Vector3 velocity)
        {
            if (_burst) return;
            float power = velocity.magnitude / 140f;
            durability -= settings.hitDamage * Mathf.Clamp(power, 0.3f, 3f);
            if (durability <= 0f) BurstOpen(hit.point);
        }

        /// <summary>Everything in it spills over the ground, and the bag is gone.</summary>
        void BurstOpen(Vector3 at)
        {
            _burst = true;
            foreach (var k in items)
            {
                Vector3 v = Random.insideUnitSphere * 1.4f + Vector3.up * Random.Range(1.2f, 2.2f);
                ItemPickup.Spawn(settings, k, MouthWorld + Random.insideUnitSphere * 0.06f, v);
            }
            items.Clear();
            Burst?.Invoke(at);
            Destroy(gameObject);
        }

        // ------------------------------------------------------------------ the look: the bag of the back, lid open, swollen by its bulk
        void Build()
        {
            if (_look) Destroy(_look.gameObject);
            _look = new GameObject("Look").transform;
            _look.SetParent(transform, false);
            var d = Def;
            float swell = settings.Swell(d, Volume);
            if (kind == BagKind.Eco) swell *= 0.75f;
            _half = SlugEquipment.BuildLooseBag(_look, bagSize, swell, Mat(d.color), Mat(d.flapColor), Mat(new Color(0.07f, 0.055f, 0.045f)),
                                                Mat(new Color(0.22f, 0.16f, 0.11f)), Mat(SlugEquipment.BuckleColor), out _mouth);
            if (_box)
            {
                _box.center = new Vector3(0f, _half.y, 0f);
                _box.size = _half * 2f;
            }
            // the top things show in the mouth, half sunk in it
            for (int i = 0; i < Mathf.Min(2, items.Count); i++)
            {
                var k = items[items.Count - 1 - i];
                var v = ItemPickup.Visual(settings, k, _look, true);
                float size = Mathf.Min(_half.x, _half.z, 0.08f) * (i == 0 ? 1.5f : 1.2f);
                v.transform.localPosition = _mouth + new Vector3((i == 0 ? 0.25f : -0.35f) * Mathf.Min(_half.x, 0.2f), 0f, (i == 0 ? 0.1f : -0.2f) * size);
                v.transform.localRotation = Quaternion.Euler(20f + 30f * i, 40f * i, 10f);
                v.transform.localScale = Vector3.one * (size * ItemPickup.Scale(k));
            }
        }

        /// <summary>The same look as the gear on a slug (Slug/Toon, still: no crawl).</summary>
        static Material Mat(Color c)
        {
            if (Mats.TryGetValue(c, out var m) && m) return m;
            var shader = Shader.Find("Slug/Toon") ?? Shader.Find("Universal Render Pipeline/Lit");
            m = new Material(shader) { name = "GroundBag", hideFlags = HideFlags.DontSave };
            m.SetColor("_BaseColor", c);
            if (m.HasProperty("_WaveAmp"))
            {
                m.SetFloat("_WaveAmp", 0f); m.SetFloat("_Stretch", 0f);
                m.SetColor("_ShadeColor", new Color(0.66f, 0.66f, 0.72f));
                m.SetColor("_OutlineColor", c * 0.4f);
                m.SetColor("_RimColor", Color.Lerp(c, Color.white, 0.35f));
                m.SetFloat("_OutlineWidth", 0.004f);
                if (m.HasProperty("_IslandLook")) m.SetFloat("_IslandLook", 0f);
            }
            Mats[c] = m;
            return m;
        }
    }
}
