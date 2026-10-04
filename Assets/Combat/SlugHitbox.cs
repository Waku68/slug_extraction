using UnityEngine;

namespace SlugMove
{
    /// <summary>One part of a slug a shot can hit. Says which part, and what armour (if any) covers it.</summary>
    public class SlugHitbox : MonoBehaviour, IShotTarget
    {
        public SlugHitboxes owner;
        public string part;
        /// <summary>The armour covering this part (null when bare).</summary>
        public string armour;
        public bool Armoured => !string.IsNullOrEmpty(armour);
        /// <summary>How much of a round's damage this part takes (the tentacles more, armour less).</summary>
        public float multiplier = 1f;

        public void OnShot(RaycastHit hit, Vector3 velocity)
        {
            if (owner) owner.Report(this, hit);
        }
    }
}
