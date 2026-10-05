using System.Collections.Generic;
using UnityEngine;

namespace SlugExtract
{
    /// <summary>
    /// A bag set down on the ground, its mouth open (its top things show in it). Taking things out of it and putting
    /// them in is quick (<see cref="ExtractSettings.groundTransferTime"/>), for anyone: its owner sorting it, or someone
    /// emptying a fallen slug's bag into their own (set their own down beside it, and both ways are quick). Anyone
    /// without a bag on the back can shoulder it. A fallen slug's bag lies here for a while
    /// (<see cref="ExtractSettings.droppedLifetime"/>), then rain and drying take it.
    /// </summary>
    public class GroundBag : MonoBehaviour
    {
        public ExtractSettings settings;
        public BagKind kind;
        public float durability;
        /// <summary>The slug that set it down (null for a fallen slug's bag).</summary>
        public SlugBag owner;
        [Tooltip("消えるまでの時間（秒、0 = 消えない）")] public float lifetime;

        /// <summary>What is in it, in the order it went in (the last is on top).</summary>
        public readonly List<ItemKind> items = new List<ItemKind>();

        /// <summary>Every bag lying on the ground.</summary>
        public static readonly List<GroundBag> All = new List<GroundBag>();

        float _age;
        Transform _look;
        float _height = 0.3f;
        static readonly Dictionary<Color, Material> Mats = new Dictionary<Color, Material>();

        void OnEnable() => All.Add(this);
        void OnDisable() => All.Remove(this);

        public BagDef Def => settings.Bag(kind);
        public bool Empty => items.Count == 0;
        public Vector3 MouthWorld => transform.position + Vector3.up * _height;

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
                                       Vector3 at, Quaternion rotation, SlugBag owner, float lifetime)
        {
            // the ground below, skipping the slugs themselves (the one setting it down stands right there)
            float best = float.MaxValue;
            foreach (var hit in Physics.RaycastAll(at + Vector3.up * 1f, Vector3.down, 3f, ~(1 << 2), QueryTriggerInteraction.Ignore))
            {
                if (hit.collider is CharacterController || hit.collider.GetComponentInParent<SlugBag>()) continue;
                if (hit.distance < best) { best = hit.distance; at = hit.point; }
            }
            var go = new GameObject("GroundBag_" + kind);
            go.layer = 2;
            go.transform.SetPositionAndRotation(at, rotation);
            var g = go.AddComponent<GroundBag>();
            g.settings = settings; g.kind = kind; g.durability = durability; g.owner = owner; g.lifetime = lifetime;
            g.items.AddRange(items);
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

        // ------------------------------------------------------------------ the look: a soft sack, swollen by its bulk, mouth open
        void Build()
        {
            if (_look) Destroy(_look.gameObject);
            _look = new GameObject("Look").transform;
            _look.SetParent(transform, false);
            var d = Def;
            float swell = Mathf.Min(1f + Volume * settings.swellPerVolume * d.swell, settings.swellVisualMax);
            float w = 0.34f * Mathf.Sqrt(swell), h = 0.26f * swell;
            _height = h * 0.95f;
            Part(PrimitiveType.Sphere, d.color, new Vector3(0f, h * 0.5f, 0f), new Vector3(w, h, w * 0.85f));
            Part(PrimitiveType.Cylinder, d.flapColor, new Vector3(0f, h * 0.93f, 0f), new Vector3(w * 0.62f, 0.012f, w * 0.55f));
            // the lid flopped open over the side
            var lid = Part(PrimitiveType.Cube, d.flapColor, new Vector3(0f, h * 0.7f, -w * 0.5f), new Vector3(w * 0.6f, 0.02f, w * 0.45f));
            lid.localRotation = Quaternion.Euler(-60f, 0f, 0f);
            // the top things show in the mouth
            for (int i = 0; i < Mathf.Min(2, items.Count); i++)
            {
                var k = items[items.Count - 1 - i];
                var v = ItemPickup.Visual(settings, k, _look, true);
                v.transform.localPosition = new Vector3(i == 0 ? 0.03f : -0.07f, h * 0.95f + 0.02f, i == 0 ? 0.02f : -0.03f);
                v.transform.localRotation = Quaternion.Euler(20f + 30f * i, 40f * i, 10f);
                v.transform.localScale = Vector3.one * (settings.Def(k).size * 0.8f * ItemPickup.Scale(k));
            }
        }

        Transform Part(PrimitiveType type, Color c, Vector3 pos, Vector3 scale)
        {
            var go = GameObject.CreatePrimitive(type);
            Destroy(go.GetComponent<Collider>());
            go.layer = 2;
            go.transform.SetParent(_look, false);
            go.transform.localPosition = pos;
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = Mat(c);
            return go.transform;
        }

        static Material Mat(Color c)
        {
            if (Mats.TryGetValue(c, out var m) && m) return m;
            var shader = Shader.Find("Slug/Toon") ?? Shader.Find("Universal Render Pipeline/Lit");
            m = new Material(shader) { name = "GroundBag", hideFlags = HideFlags.DontSave };
            m.SetColor("_BaseColor", c);
            if (m.HasProperty("_WaveAmp")) { m.SetFloat("_WaveAmp", 0f); m.SetFloat("_Stretch", 0f); m.SetFloat("_OutlineWidth", 0.006f); m.SetColor("_OutlineColor", c * 0.45f); }
            Mats[c] = m;
            return m;
        }
    }
}
