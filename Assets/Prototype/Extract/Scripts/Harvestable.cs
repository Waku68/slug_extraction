using SlugMove;
using UnityEngine;

namespace SlugExtract
{
    /// <summary>
    /// A node that is struck with the pickaxe (Fortnite-style): every hit puts items straight into the bag,
    /// the node jolts and sheds chips, and the last hit gives a bonus and breaks it.
    /// </summary>
    public class Harvestable : MonoBehaviour, IPickaxeTarget, IPickaxeSurface
    {
        /// <summary>Rock salt is hard (the pickaxe bounces off), crops are soft (it sinks in).</summary>
        public PickaxeMaterial PickaxeMaterial => kind == ItemKind.RockSalt ? PickaxeMaterial.Hard : PickaxeMaterial.Soft;

        public ExtractSettings settings;
        public ItemKind kind;
        [Tooltip("壊れるまでに叩く回数")] public int hits = 4;
        [Tooltip("1 回叩くごとに採れる数")] public int yieldPerHit = 1;
        [Tooltip("最後の一撃で追加で採れる数")] public int breakBonus = 1;

        int _left;
        float _punch;           // 0..1, decays: the jolt of the last hit
        Vector3 _baseScale;
        bool _breaking;
        float _breakT;

        /// <summary>Called after every hit: (item, amount that went into the bag, bag was full).</summary>
        public static event System.Action<ItemKind, int, bool> Harvested;

        void Awake()
        {
            _left = hits;
            _baseScale = transform.localScale;
        }

        public void OnPickaxeHit(GameObject hitter, RaycastHit hit)
        {
            if (_breaking || _left <= 0) return;
            var bag = hitter.GetComponent<SlugBag>();
            bool full = bag != null && !bag.Fits(kind);
            Chips(hit.point, hit.normal, full ? 3 : 6);
            _punch = 1f;
            if (full) { Harvested?.Invoke(kind, 0, true); return; }   // nothing fits: the node is not used up

            _left--;
            int amount = yieldPerHit + (_left == 0 ? breakBonus : 0);
            int added = bag != null ? bag.TryAdd(kind, amount) : 0;
            Harvested?.Invoke(kind, added, added < amount);
            if (_left == 0) _breaking = true;
        }

        void Update()
        {
            if (_breaking)
            {
                _breakT += Time.deltaTime / 0.35f;
                transform.localScale = _baseScale * Mathf.Max(0f, 1f - _breakT * _breakT);
                if (_breakT >= 1f) Destroy(gameObject);
                return;
            }
            if (_punch > 0f)
            {
                _punch = Mathf.Max(0f, _punch - Time.deltaTime * 6f);
                float k = Mathf.Sin(_punch * Mathf.PI * 3f) * _punch;
                transform.localScale = new Vector3(_baseScale.x * (1f + 0.12f * k), _baseScale.y * (1f - 0.1f * k), _baseScale.z * (1f + 0.12f * k));
            }
        }

        void Chips(Vector3 point, Vector3 normal, int count)
            => PickaxeEffects.Chips(point, normal, settings.Def(kind).chipColor, count);
    }
}
