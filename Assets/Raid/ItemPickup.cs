using System.Collections.Generic;
using SlugMove;
using UnityEngine;

namespace SlugExtract
{
    /// <summary>
    /// Something lying on the ground that a slug can pick up (E): an item dropped or thrown from a bag, or spilled when a
    /// bag broke. A small physical thing (a pink chunk of rock salt, a rain turnip with its leaves) that tumbles and
    /// settles. It never blocks a shot or the aim (Ignore Raycast).
    /// </summary>
    public class ItemPickup : MonoBehaviour
    {
        public ItemKind kind;

        /// <summary>Every pickup lying about (to find the nearest without a physics query).</summary>
        public static readonly List<ItemPickup> All = new List<ItemPickup>();

        static Mesh _chunkMesh, _turnipMesh, _leafMesh;
        static readonly Dictionary<Color, Material> Mats = new Dictionary<Color, Material>();

        void OnEnable() => All.Add(this);
        void OnDisable() => All.Remove(this);

        /// <summary>Makes a pickup at <paramref name="position"/>, thrown with <paramref name="velocity"/>. It does not collide with <paramref name="ignore"/>.</summary>
        public static ItemPickup Spawn(ExtractSettings settings, ItemKind kind, Vector3 position, Vector3 velocity, Collider ignore = null)
        {
            EnsureMeshes();
            var go = new GameObject("Pickup_" + kind) { layer = 2 };
            go.transform.position = position;
            go.transform.rotation = Random.rotation;
            var def = settings.Def(kind);
            if (kind == ItemKind.RockSalt)
            {
                Part(go.transform, _chunkMesh, Mat(def.chipColor), Vector3.zero, Vector3.one);
            }
            else
            {
                Part(go.transform, _turnipMesh, Mat(new Color(0.86f, 0.8f, 0.9f)), Vector3.zero, Vector3.one);
                Part(go.transform, _leafMesh, Mat(def.chipColor), new Vector3(0f, 0.03f, 0f), Vector3.one);
            }
            var col = go.AddComponent<SphereCollider>();
            col.radius = kind == ItemKind.RockSalt ? 0.03f : 0.035f;
            var rb = go.AddComponent<Rigidbody>();
            rb.mass = Mathf.Max(0.05f, def.weight * 0.2f);
            rb.linearDamping = 0.2f;
            rb.angularDamping = 0.6f;
            rb.linearVelocity = velocity;
            rb.angularVelocity = Random.insideUnitSphere * 6f;
            if (ignore) Physics.IgnoreCollision(col, ignore);
            var p = go.AddComponent<ItemPickup>();
            p.kind = kind;
            return p;
        }

        /// <summary>The nearest pickup within <paramref name="reach"/> of <paramref name="from"/>, or null.</summary>
        public static ItemPickup Nearest(Vector3 from, float reach)
        {
            ItemPickup best = null;
            float bestD = reach;
            foreach (var p in All)
            {
                if (!p) continue;
                float d = Vector3.Distance(from, p.transform.position);
                if (d < bestD) { bestD = d; best = p; }
            }
            return best;
        }

        static void Part(Transform parent, Mesh mesh, Material mat, Vector3 pos, Vector3 scale)
        {
            var go = new GameObject("Mesh") { layer = 2 };
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localScale = scale;
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = mat;
        }

        static Material Mat(Color c)
        {
            if (Mats.TryGetValue(c, out var m) && m) return m;
            m = new Material(Shader.Find("Universal Render Pipeline/Lit")) { hideFlags = HideFlags.DontSave };
            m.SetColor("_BaseColor", c);
            m.SetFloat("_Smoothness", 0.35f);
            Mats[c] = m;
            return m;
        }

        static void EnsureMeshes()
        {
            if (_chunkMesh != null) return;
            // a faceted lump of rock salt
            _chunkMesh = new ProfileMesh { uvScale = 1f }
                .Extrude(new List<Vector2> { new Vector2(-0.03f, -0.016f), new Vector2(0f, -0.028f), new Vector2(0.028f, -0.014f),
                                             new Vector2(0.024f, 0.02f), new Vector2(-0.006f, 0.028f), new Vector2(-0.028f, 0.012f) }, 0.022f, 0.009f)
                .ToMesh("SaltChunk");
            // a round turnip with a little root, and two leaves
            _turnipMesh = new ProfileMesh { uvScale = 1f }
                .Tube(new Vector3(0f, -0.034f, 0f), new Vector3(0f, -0.02f, 0f), 0.004f, 8, 0.02f)
                .Tube(new Vector3(0f, -0.02f, 0f), new Vector3(0f, 0.01f, 0f), 0.02f, 10, 0.032f)
                .Tube(new Vector3(0f, 0.01f, 0f), new Vector3(0f, 0.026f, 0f), 0.032f, 10, 0.01f)
                .ToMesh("Turnip");
            _leafMesh = new ProfileMesh { uvScale = 1f }
                .Flat(new List<Vector2> { new Vector2(0f, 0f), new Vector2(0.014f, 0.03f), new Vector2(0f, 0.06f), new Vector2(-0.012f, 0.03f) },
                      Vector3.zero, new Vector3(1f, 0f, 0.3f).normalized, Vector3.up, true)
                .Flat(new List<Vector2> { new Vector2(0f, 0f), new Vector2(0.012f, 0.026f), new Vector2(0f, 0.052f), new Vector2(-0.01f, 0.026f) },
                      Vector3.zero, new Vector3(-0.3f, 0f, 1f).normalized, new Vector3(0.3f, 1f, 0f).normalized, true)
                .ToMesh("TurnipLeaves");
        }
    }
}
