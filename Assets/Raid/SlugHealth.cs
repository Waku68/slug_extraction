using SlugWorld;
using UnityEngine;

namespace SlugExtract
{
    /// <summary>
    /// The slug's health (docs/discussions/health_damage_discussion.md): it falls when the body dries out to nothing (and,
    /// later, when salt hits it) and comes back very slowly while the body is moist enough (<see cref="SlugMoisture"/>).
    /// At zero the slug falls: there is no knocked-down state. The player never sees it as a number.
    /// </summary>
    public class SlugHealth : MonoBehaviour
    {
        public ConditionSettings settings;

        public float Current { get; private set; }
        public float Max => settings ? settings.healthMax : 100f;
        public float Fraction => Max > 0f ? Current / Max : 0f;
        public bool Dead { get; private set; }
        /// <summary>What brought the slug down ("dried", "salt", ...).</summary>
        public string Cause { get; private set; }

        /// <summary>Called once when the slug falls, with the cause.</summary>
        public event System.Action<string> Died;

        void Awake() => Current = Max;

        public void TakeDamage(float amount, string cause)
        {
            if (Dead || amount <= 0f) return;
            Current = Mathf.Max(0f, Current - amount);
            if (Current > 0f) return;
            Dead = true;
            Cause = cause;
            Died?.Invoke(cause);
        }

        public void Heal(float amount)
        {
            if (Dead || amount <= 0f) return;
            Current = Mathf.Min(Max, Current + amount);
        }
    }
}
