using System.Collections.Generic;
using SlugMove;
using UnityEngine;

namespace SlugExtract
{
    /// <summary>
    /// A soft plant (a flower, a mushroom) that is picked by hand: with empty hands, E next to it reaches out and takes
    /// one in the hands over a moment (<see cref="ExtractSettings.pickTime"/>). Struck with the pickaxe instead, it is
    /// crushed: one comes off squashed (<see cref="crushedKind"/>, worth less) and lands on the ground. Each picked or
    /// crushed one disappears from <see cref="parts"/>; when none is left the plant is gone.
    /// </summary>
    public class Pickable : MonoBehaviour, IPickaxeTarget, IPickaxeSurface
    {
        public PickaxeMaterial PickaxeMaterial => PickaxeMaterial.Soft;

        public ExtractSettings settings;
        [Tooltip("手で摘むと手に入る物")] public ItemKind kind = ItemKind.UgoTake;
        [Tooltip("ピッケルで叩くと出てくる物（潰れて値打ちが下がる）")] public ItemKind crushedKind = ItemKind.UgoTakeCrushed;
        [Tooltip("一つずつ摘める物（キノコ1本など）。摘むたびに1つ消える")] public List<GameObject> parts = new List<GameObject>();

        /// <summary>Every plant that can still be picked (to find the nearest without a physics query).</summary>
        public static readonly List<Pickable> All = new List<Pickable>();

        /// <summary>One was crushed by a pickaxe (where), for sounds.</summary>
        public static event System.Action<Vector3> Crushed;

        float _punch;
        Vector3 _baseScale;

        void OnEnable() => All.Add(this);
        void OnDisable() => All.Remove(this);
        void Awake() => _baseScale = transform.localScale;

        public bool HasAny => Next() != null;

        GameObject Next()
        {
            for (int i = parts.Count - 1; i >= 0; i--) if (parts[i] && parts[i].activeSelf) return parts[i];
            return null;
        }

        /// <summary>Where the next one to be picked stands (the hands reach there).</summary>
        public Vector3 NextPoint
        {
            get
            {
                var p = Next();
                if (!p) return transform.position;
                var r = p.GetComponentInChildren<Renderer>();
                return r ? r.bounds.center : p.transform.position;
            }
        }

        /// <summary>Takes one off the plant (it is now in someone's hands). False if none is left.</summary>
        public bool Take()
        {
            var p = Next();
            if (!p) return false;
            p.SetActive(false);
            _punch = 0.6f;
            if (!HasAny) gameObject.SetActive(false);
            return true;
        }

        public void OnPickaxeHit(GameObject hitter, RaycastHit hit)
        {
            if (!HasAny) return;
            PickaxeEffects.Chips(hit.point, hit.normal, settings.Def(kind).chipColor, 8);
            Vector3 from = NextPoint + Vector3.up * 0.05f;
            Take();
            Crushed?.Invoke(from);
            Vector3 toward = Vector3.ProjectOnPlane(hitter.transform.position - transform.position, Vector3.up).normalized;
            Vector3 v = toward * Random.Range(0.6f, 1f) + Vector3.up * Random.Range(1f, 1.4f);
            ItemPickup.Spawn(settings, crushedKind, from, v, hitter.GetComponent<Collider>());
        }

        /// <summary>The nearest plant with something left on it within <paramref name="reach"/>, or null.</summary>
        public static Pickable Nearest(Vector3 from, float reach)
        {
            Pickable best = null;
            float bestD = reach;
            foreach (var p in All)
            {
                if (!p || !p.HasAny) continue;
                float d = Vector3.Distance(from, p.NextPoint);
                if (d < bestD) { bestD = d; best = p; }
            }
            return best;
        }

        void Update()
        {
            if (_punch <= 0f) return;
            _punch = Mathf.Max(0f, _punch - Time.deltaTime * 4f);
            float k = Mathf.Sin(_punch * Mathf.PI * 3f) * _punch;
            transform.localScale = new Vector3(_baseScale.x * (1f + 0.08f * k), _baseScale.y * (1f - 0.12f * k), _baseScale.z * (1f + 0.08f * k));
        }
    }
}
