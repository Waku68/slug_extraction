using SlugMove;
using UnityEngine;

namespace SlugExtract
{
    /// <summary>
    /// Salt hurts: a round that lands on a slug takes health by its gun (type B: the bare body falls after the rifle's 8,
    /// the pistol's 12, the shotgun's 2 with every grain in, the sniper's 2; the sniper's one to the bare head), times the
    /// part it struck (the tentacles 1.4, armour 0.6, a bag on the back by its kind). A knife takes a quarter.
    /// A slug that falls with <see cref="vanishOnFall"/> lets everything drop where it is (its bag, as a bag on the ground,
    /// <see cref="SlugBag"/>) and is gone without a trace; the player's slug stays (the raid ends).
    /// </summary>
    public class SlugDamage : MonoBehaviour
    {
        [Range(0f, 1f), Tooltip("ナイフ1突きで減る体力の割合")] public float knifeFraction = 0.25f;
        [Tooltip("倒れたら持ち物をその場に落として、体は跡形もなく消える（敵・練習用）。プレイヤーは消えない")] public bool vanishOnFall = true;

        SlugHitboxes _hitboxes;
        SlugHealth _health;

        void Awake()
        {
            _hitboxes = GetComponentInChildren<SlugHitboxes>(true);
            _health = GetComponentInChildren<SlugHealth>(true);
        }

        void OnEnable()
        {
            if (_hitboxes) _hitboxes.Hit += OnHit;
            if (_health) _health.Died += OnFell;
        }

        void OnDisable()
        {
            if (_hitboxes) _hitboxes.Hit -= OnHit;
            if (_health) _health.Died -= OnFell;
        }

        void OnHit(SlugHitbox box, RaycastHit hit)
        {
            if (!_health || _health.Dead) return;
            var gun = SaltRound.Striking;
            float max = _health.Max, damage;
            if (gun == null) damage = max * knifeFraction * box.multiplier;
            else if (gun.HeadshotFalls && box.part == "頭" && !box.Armoured) damage = max;
            else damage = max / Mathf.Max(1f, gun.HitsToFall * Mathf.Max(1, gun.pellets)) * box.multiplier;
            _health.TakeDamage(damage, "salt");
        }

        /// <summary>Gone without a trace: a puff of the body's colour, and nothing left but what it carried.</summary>
        void OnFell(string cause)
        {
            if (!vanishOnFall) return;
            Vector3 at = transform.position + Vector3.up * 0.3f;
            PickaxeEffects.Chips(at, Vector3.up, new Color(0.43f, 0.48f, 0.3f), 24, 1.2f);
            // the gear drops to the ground where it fell, the small things beside it (spec 8章, Oct 25)
            var bag = GetComponentInChildren<SlugBag>(true);
            SlugHusk.Leave(gameObject, bag && bag.settings ? bag.settings.droppedLifetime : 2700f);
            Destroy(gameObject);     // at the end of the frame: the bag has dropped by then (SlugBag hears Died too)
        }
    }
}
