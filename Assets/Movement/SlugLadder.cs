using UnityEngine;

namespace SlugMove
{
    /// <summary>
    /// A ladder leaned on the rock (Nov 5 わくさん「岩に掛けたはしご」). A slug that walks into it climbs it
    /// (<see cref="SlugMoveController"/>): toward the rock goes up, away goes down. At the top it steps onto the platform;
    /// from the platform, walking off over the ladder's top climbs down. The BoxCollider (a trigger) is the room in front
    /// of the rungs. foot and top are the line the feet follow, already a body's width out from the rungs; top is a little
    /// above the platform's floor, just off its edge. Made by IslandWalkways (Assets/TestScenes/Editor).
    /// </summary>
    [RequireComponent(typeof(BoxCollider))]
    public class SlugLadder : MonoBehaviour
    {
        [Tooltip("足の通る線の下の端（ワールド座標）")] public Vector3 foot;
        [Tooltip("足の通る線の上の端（台の床より少し上、台の縁のすぐ外）")] public Vector3 top;
        [Tooltip("岩（台）のほうへ向く水平の向き")] public Vector3 into = Vector3.forward;

        public Vector3 Up => (top - foot).normalized;

        /// <summary>The nearest point of the feet's line.</summary>
        public Vector3 Closest(Vector3 p)
        {
            var d = top - foot;
            float t = Mathf.Clamp01(Vector3.Dot(p - foot, d) / Mathf.Max(d.sqrMagnitude, 1e-6f));
            return foot + d * t;
        }

        void Reset() { GetComponent<BoxCollider>().isTrigger = true; }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.6f, 0.1f);
            Gizmos.DrawLine(foot, top);
            Gizmos.DrawLine(top, top + into * 0.5f);
        }
    }
}
