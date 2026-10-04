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
        /// <summary>The velocity of the last round that struck this part (how hard it hit).</summary>
        public Vector3 LastVelocity { get; private set; }

        public void OnShot(RaycastHit hit, Vector3 velocity)
        {
            LastVelocity = velocity;
            if (owner) owner.Report(this, hit);
        }
    }
}
