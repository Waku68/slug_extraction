using System.Collections.Generic;
using UnityEngine;

namespace SlugMove
{
    /// <summary>
    /// A pickaxe in the spirit of Fortnite's default one: a wooden handle with a wrapped grip, a dark collar,
    /// and a curved metal head with two points. Built from code. Local space: the handle runs up +Y from the
    /// bottom end (origin) and the head's points lie along ±Z. Salt melts a slug, so it never digs bare-handed.
    /// </summary>
    public class SlugPickaxe : MonoBehaviour
    {
        public const float HandleLength = 0.5f;
        /// <summary>Where the two hands hold the handle (distance from the bottom end).</summary>
        public const float GripLow = 0.07f, GripHigh = 0.3f;
        const float HeadY = HandleLength - 0.01f, HeadHalf = 0.17f, HeadDroop = 0.06f;
        /// <summary>The striking point (the +Z point of the head) and the middle of the head, in local space.</summary>
        public static readonly Vector3 TipLocal = new Vector3(0f, HeadY - HeadDroop, HeadHalf);
        public static readonly Vector3 HeadLocal = new Vector3(0f, HeadY, 0f);

        static Material _wood, _metal, _wrap;
        static Mesh _headMesh;


        /// <summary>Creates a pickaxe. Its colliders are removed; shadows can be turned off for a first-person copy.</summary>
        public static SlugPickaxe Create(string name, Transform parent, bool castShadows)
        {
            EnsureAssets();
            var root = new GameObject(name);
            root.transform.SetParent(parent, false);
            var pick = root.AddComponent<SlugPickaxe>();

            Piece(root.transform, PrimitiveType.Cylinder, _wood, new Vector3(0f, HandleLength * 0.5f, 0f), new Vector3(0.032f, HandleLength * 0.5f, 0.032f), castShadows);
            Piece(root.transform, PrimitiveType.Cylinder, _wrap, new Vector3(0f, 0.09f, 0f), new Vector3(0.038f, 0.07f, 0.038f), castShadows);
            Piece(root.transform, PrimitiveType.Cylinder, _wrap, new Vector3(0f, HandleLength - 0.02f, 0f), new Vector3(0.05f, 0.035f, 0.05f), castShadows);

            var head = new GameObject("Head");
            head.transform.SetParent(root.transform, false);
            head.transform.localPosition = HeadLocal;
            head.AddComponent<MeshFilter>().sharedMesh = _headMesh;
            var mr = head.AddComponent<MeshRenderer>();
            mr.sharedMaterial = _metal;
            if (!castShadows) mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return pick;
        }

        /// <summary>Shows the pickaxe normally, as a shadow only (first person: the body's own pickaxe), or hides it.</summary>
        public void SetMode(bool visible, bool shadowOnly)
        {
            foreach (var r in GetComponentsInChildren<MeshRenderer>(true))
            {
                r.enabled = visible;
                r.shadowCastingMode = shadowOnly ? UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly : UnityEngine.Rendering.ShadowCastingMode.On;
            }
        }

        static void Piece(Transform parent, PrimitiveType type, Material mat, Vector3 pos, Vector3 scale, bool castShadows)
        {
            var go = GameObject.CreatePrimitive(type);
            DestroyImmediate(go.GetComponent<Collider>());   // never collide with the slug that holds it
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localScale = scale;
            var r = go.GetComponent<MeshRenderer>();
            r.sharedMaterial = mat;
            if (!castShadows) r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        static void EnsureAssets()
        {
            if (_headMesh != null) return;
            var lit = Shader.Find("Universal Render Pipeline/Lit");
            _wood = Mat(lit, new Color(0.55f, 0.36f, 0.2f), 0f, 0.15f);
            _wrap = Mat(lit, new Color(0.2f, 0.16f, 0.14f), 0f, 0.1f);
            _metal = Mat(lit, new Color(0.62f, 0.66f, 0.72f), 0.5f, 0.45f);
            _headMesh = HeadMesh();
        }

        static Material Mat(Shader s, Color c, float metallic, float smooth)
        {
            var m = new Material(s) { hideFlags = HideFlags.DontSave };
            m.SetColor("_BaseColor", c);
            m.SetFloat("_Metallic", metallic);
            m.SetFloat("_Smoothness", smooth);
            return m;
        }

        /// <summary>A curved bar along Z, thick in the middle and pointed at both ends, drooping toward the points.</summary>
        static Mesh HeadMesh()
        {
            const int seg = 16;
            const float half = HeadHalf, droop = HeadDroop;
            var v = new List<Vector3>();
            var tri = new List<int>();
            for (int i = 0; i <= seg; i++)
            {
                float s = Mathf.Lerp(-1f, 1f, i / (float)seg);
                float z = s * half;
                float y = -droop * s * s;
                float w = Mathf.Lerp(0.045f, 0.004f, Mathf.Pow(Mathf.Abs(s), 1.4f));   // half thickness
                float hx = w * 0.75f, hy = w;
                v.Add(new Vector3(-hx, y - hy, z)); v.Add(new Vector3(hx, y - hy, z));
                v.Add(new Vector3(hx, y + hy, z)); v.Add(new Vector3(-hx, y + hy, z));
            }
            for (int i = 0; i < seg; i++)
            {
                int a = i * 4, b = a + 4;
                for (int k = 0; k < 4; k++)
                {
                    int k1 = (k + 1) % 4;
                    tri.AddRange(new[] { a + k, b + k1, b + k, a + k, a + k1, b + k1 });
                }
            }
            // caps
            int e = seg * 4;
            tri.AddRange(new[] { 0, 2, 1, 0, 3, 2 });
            tri.AddRange(new[] { e, e + 1, e + 2, e, e + 2, e + 3 });
            var m = new Mesh { name = "PickaxeHead", hideFlags = HideFlags.DontSave };
            m.SetVertices(v);
            m.SetTriangles(tri, 0);
            m.RecalculateNormals();
            m.RecalculateBounds();
            return m;
        }
    }
}
