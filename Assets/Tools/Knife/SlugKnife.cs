using UnityEngine;

namespace SlugMove
{
    /// <summary>
    /// A knife for the hip: a wooden handle with a steel pommel and a short guard, and a leaf-shaped steel blade with a
    /// bright bevelled edge. Built from code. Local space: the origin is the middle of the grip (where the fist closes),
    /// the blade runs along +Z, its back (spine) up +Y and the edge down. <see cref="Sheath"/> makes the leather sheath
    /// it rides in on the belt.
    /// </summary>
    public class SlugKnife : MonoBehaviour
    {
        /// <summary>The point of the blade, and the middle of the blade (local space).</summary>
        public static readonly Vector3 TipLocal = new Vector3(0f, 0f, 0.152f), BladeMidLocal = new Vector3(0f, 0f, 0.085f);
        /// <summary>How far the blade reaches past the grip (m).</summary>
        public const float Reach = 0.152f;

        static Material _wood, _steel, _edge, _leather;
        static Mesh _handleMesh, _steelMesh, _bladeMesh, _edgeMesh, _sheathMesh;

        public static SlugKnife Create(string name, Transform parent, bool castShadows)
        {
            EnsureAssets();
            var root = new GameObject(name);
            root.transform.SetParent(parent, false);
            var knife = root.AddComponent<SlugKnife>();
            Part(root.transform, "Handle", _handleMesh, _wood, castShadows);
            Part(root.transform, "Steel", _steelMesh, _steel, castShadows);
            Part(root.transform, "Blade", _bladeMesh, _steel, castShadows);
            Part(root.transform, "Edge", _edgeMesh, _edge, castShadows);
            return knife;
        }

        /// <summary>The leather sheath over the blade (a child of <paramref name="parent"/>, in the knife's own space).</summary>
        public static GameObject Sheath(Transform parent, bool castShadows)
        {
            EnsureAssets();
            return Part(parent, "Sheath", _sheathMesh, _leather, castShadows).gameObject;
        }

        static Transform Part(Transform parent, string name, Mesh mesh, Material mat, bool castShadows)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = mat;
            if (!castShadows) r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return go.transform;
        }

        static void EnsureAssets()
        {
            if (_bladeMesh != null) return;
            var lit = Shader.Find("Universal Render Pipeline/Lit");
            _wood = Mat(lit, new Color(0.62f, 0.36f, 0.19f), 0f, 0.3f);
            _steel = Mat(lit, new Color(0.3f, 0.31f, 0.33f), 0.35f, 0.4f);
            _edge = Mat(lit, new Color(0.78f, 0.8f, 0.83f), 0.5f, 0.55f);
            _leather = Mat(lit, new Color(0.36f, 0.24f, 0.15f), 0f, 0.2f);

            // the handle: a slightly swelling grip, chunky and toy-like
            _handleMesh = new ProfileMesh { uvScale = 1f }
                .Extrude(Zy(-0.052f, -0.009f, -0.03f, -0.012f, 0.0f, -0.011f, 0.011f, -0.009f, 0.011f, 0.009f, 0.0f, 0.011f,
                            -0.03f, 0.012f, -0.052f, 0.009f), 0.0095f, 0.004f)
                .ToMesh("KnifeHandle");

            // the pommel and the guard
            var steel = new ProfileMesh();
            steel.Tube(new Vector3(0f, 0f, -0.061f), new Vector3(0f, 0f, -0.051f), 0.0115f, 10, 0.0105f);
            steel.Extrude(Zy(0.011f, -0.016f, 0.018f, -0.016f, 0.018f, 0.015f, 0.011f, 0.015f), 0.012f, 0.002f);
            _steelMesh = steel.ToMesh("KnifeSteel");

            // the blade: a straight back, a belly curving up to the point; the edge is a brighter strip along the belly
            _bladeMesh = new ProfileMesh()
                .Extrude(Zy(0.018f, 0.0105f, 0.112f, 0.0095f, 0.152f, 0.0f, 0.132f, -0.0075f, 0.095f, -0.0105f, 0.04f, -0.0115f,
                            0.018f, -0.0095f), 0.0021f, 0.0012f)
                .ToMesh("KnifeBlade");
            _edgeMesh = new ProfileMesh()
                .Extrude(Zy(0.024f, -0.0098f, 0.04f, -0.0113f, 0.095f, -0.0103f, 0.132f, -0.0073f, 0.149f, -0.0008f, 0.128f, -0.0045f,
                            0.093f, -0.0068f, 0.04f, -0.0075f, 0.024f, -0.0065f), 0.0024f, 0.0006f)
                .ToMesh("KnifeEdge");

            // the sheath: a leather pouch over the blade, with a stitched welt (a thicker rim) along the edge side
            var sheath = new ProfileMesh();
            sheath.Extrude(Zy(0.016f, 0.0145f, 0.118f, 0.0135f, 0.165f, 0.0f, 0.14f, -0.011f, 0.095f, -0.0155f, 0.016f, -0.0145f), 0.0065f, 0.0025f);
            sheath.Extrude(Zy(0.02f, -0.012f, 0.095f, -0.0135f, 0.135f, -0.009f, 0.14f, -0.0135f, 0.095f, -0.0185f, 0.02f, -0.0175f), 0.0075f, 0.0025f);
            _sheathMesh = sheath.ToMesh("KnifeSheath");
        }

        static System.Collections.Generic.List<Vector2> Zy(params float[] zy)
        {
            var p = new System.Collections.Generic.List<Vector2>(zy.Length / 2);
            for (int i = 0; i + 1 < zy.Length; i += 2) p.Add(new Vector2(zy[i], zy[i + 1]));
            return p;
        }

        static Material Mat(Shader s, Color c, float metallic, float smooth)
        {
            var m = new Material(s) { hideFlags = HideFlags.DontSave };
            m.SetColor("_BaseColor", c);
            m.SetFloat("_Metallic", metallic);
            m.SetFloat("_Smoothness", smooth);
            return m;
        }
    }
}
