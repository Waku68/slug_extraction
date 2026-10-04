using System.Collections.Generic;
using SlugMove;
using UnityEngine;

namespace SlugExtract
{
    /// <summary>What the slug carries: counts per item, total weight and value. The heavier, the slower.</summary>
    public class SlugBag : MonoBehaviour
    {
        public ExtractSettings settings;
        public SlugMoveController controller;

        readonly Dictionary<ItemKind, int> _counts = new Dictionary<ItemKind, int>();

        public float Weight { get; private set; }
        public int Value { get; private set; }
        public float Fill01 => settings.bagCapacity > 0f ? Mathf.Clamp01(Weight / settings.bagCapacity) : 1f;
        public int Count(ItemKind k) => _counts.TryGetValue(k, out var n) ? n : 0;

        /// <summary>Adds up to <paramref name="amount"/> items, as many as still fit. Returns how many went in.</summary>
        public int TryAdd(ItemKind kind, int amount)
        {
            var def = settings.Def(kind);
            int fit = def.weight > 0f ? Mathf.FloorToInt((settings.bagCapacity - Weight + 1e-4f) / def.weight) : amount;
            int n = Mathf.Clamp(amount, 0, fit);
            if (n <= 0) return 0;
            _counts[kind] = Count(kind) + n;
            Recount();
            return n;
        }

        public bool Fits(ItemKind kind) => Weight + settings.Def(kind).weight <= settings.bagCapacity + 1e-4f;

        public void Clear()
        {
            _counts.Clear();
            Recount();
        }

        void Recount()
        {
            Weight = 0f; Value = 0;
            foreach (var kv in _counts)
            {
                var def = settings.Def(kv.Key);
                Weight += def.weight * kv.Value;
                Value += def.value * kv.Value;
            }
        }

        void Update()
        {
            if (controller) controller.speedMultiplier = Mathf.Lerp(1f, settings.speedAtFull, Fill01);
        }
    }
}
