using UnityEngine;

namespace SlugWorld
{
    /// <summary>
    /// A walkway, bridge or ladder laid through its children (the points, in order). Move the points, then
    /// Slug/IslandTest 800 m/Walkways/Rebuild to lay it again (Assets/TestScenes/Editor/IslandWalkways.cs).
    /// The deck's top is at the bottom of each point's cube (or at an empty's position). A point named 切れ目… breaks a
    /// walkway there: the deck stops short of it on both sides and you walk on the ground across the break.
    /// A child named "_built" is the laid thing itself (made again on every rebuild). Points are drawn as yellow balls in
    /// the Scene view (a break red).
    /// </summary>
    public class WalkwayPath : MonoBehaviour
    {
        public enum Kind { Wall, Bridge, Ladder }

        [Tooltip("Wall: a board walkway along a cliff, braced into the rock, a rope rail on the outer side. Bridge: a plank bridge between its two ends, rails on both sides, braced into the cliffs at its ends. Ladder: a small platform braced into the rock at each point, and a ladder leaned up to it (from the ground for the first one, from the platform below for the rest).")]
        public Kind kind = Kind.Wall;
        [Tooltip("Walkable width (m). The slug is 0.36 m wide; 1.2 m is about 4 m for a person.")]
        public float width = 1.2f;
        [Tooltip("Wall: steeper than this between two points is reported (degrees).")]
        public float maxGrade = 28f;

        [HideInInspector, Tooltip("The laid deck's middle line (written by the rebuild, used by the walk test and the walk map). Ladder: foot, platform, foot, platform...")]
        public Vector3[] deckLine;
        [HideInInspector, Tooltip("Where each piece of deckLine starts (a break splits a walkway into pieces).")]
        public int[] sections;

        public const string BuiltName = "_built";
        public const string BreakPrefix = "切れ目";

        public static bool IsBreak(Transform p) => p.name.StartsWith(BreakPrefix);

        /// <summary>The deck's top at a point.</summary>
        public static Vector3 DeckPoint(Transform p)
        {
            var pos = p.position;
            if (p.GetComponent<MeshFilter>()) pos.y -= 0.5f * p.lossyScale.y;
            return pos;
        }

        void OnDrawGizmos()
        {
            Vector3? last = null;
            foreach (Transform c in transform)
            {
                if (c.name == BuiltName) continue;
                var p = DeckPoint(c);
                Gizmos.color = IsBreak(c) ? new Color(1f, 0.25f, 0.2f) : new Color(1f, 0.85f, 0.1f);
                Gizmos.DrawSphere(p, 0.35f);
                Gizmos.color = new Color(1f, 0.85f, 0.1f);
                if (last.HasValue) Gizmos.DrawLine(last.Value, p);
                last = p;
            }
        }
    }
}
