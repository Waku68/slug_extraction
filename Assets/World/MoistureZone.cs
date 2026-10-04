using UnityEngine;

namespace SlugWorld
{
    /// <summary>A stretch of ground that changes the moisture: salt (dries) or water (wets). An axis-aligned box.</summary>
    public class MoistureZone : MonoBehaviour
    {
        public enum Kind { Salt, Water }
        public Kind kind = Kind.Water;
        [Tooltip("広さ（m、中心はこのオブジェクトの位置）")] public Vector3 size = new Vector3(3f, 1f, 3f);

        static readonly System.Collections.Generic.List<MoistureZone> _all = new System.Collections.Generic.List<MoistureZone>();
        void OnEnable() => _all.Add(this);
        void OnDisable() => _all.Remove(this);

        public bool Contains(Vector3 p)
        {
            Vector3 d = p - transform.position;
            return Mathf.Abs(d.x) <= size.x * 0.5f && Mathf.Abs(d.y) <= size.y * 0.5f && Mathf.Abs(d.z) <= size.z * 0.5f;
        }

        /// <summary>Whether <paramref name="p"/> is on a zone of this kind.</summary>
        public static bool Any(Kind kind, Vector3 p)
        {
            foreach (var z in _all) if (z && z.kind == kind && z.Contains(p)) return true;
            return false;
        }

        void OnDrawGizmos()
        {
            Gizmos.color = kind == Kind.Water ? new Color(0.3f, 0.6f, 1f, 0.5f) : new Color(1f, 1f, 1f, 0.5f);
            Gizmos.DrawWireCube(transform.position, size);
        }
    }
}
