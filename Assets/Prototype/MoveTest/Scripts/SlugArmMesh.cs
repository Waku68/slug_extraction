using UnityEngine;

namespace SlugMove
{
    /// <summary>
    /// A stretchy slug arm: a tapering tube along a cubic curve from the root on the body to the hand, rebuilt every
    /// frame, with a fist (a squashed ball) closed around the handle. Points are given in this object's local space.
    /// </summary>
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class SlugArmMesh : MonoBehaviour
    {
        const int Segments = 14, Sides = 8;

        Mesh _mesh;
        Vector3[] _v, _n;
        Transform _fist;

        public MeshRenderer Renderer { get; private set; }
        public MeshRenderer FistRenderer { get; private set; }

        public static SlugArmMesh Create(string name, Transform parent, Material material, Mesh sphere)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>();
            var arm = go.AddComponent<SlugArmMesh>();
            arm.Renderer = go.GetComponent<MeshRenderer>();
            arm.Renderer.sharedMaterial = material;
            arm.Init();

            var fist = new GameObject(name + "_Fist");
            fist.transform.SetParent(go.transform, false);
            fist.AddComponent<MeshFilter>().sharedMesh = sphere;
            arm.FistRenderer = fist.AddComponent<MeshRenderer>();
            arm.FistRenderer.sharedMaterial = material;
            arm._fist = fist.transform;
            return arm;
        }

        void Init()
        {
            _mesh = new Mesh { name = "SlugArm" };
            _mesh.MarkDynamic();
            int rings = Segments + 1;
            _v = new Vector3[rings * Sides];
            _n = new Vector3[rings * Sides];
            var uv = new Vector2[_v.Length];
            var tri = new int[Segments * Sides * 6];
            int k = 0;
            for (int i = 0; i < rings; i++)
            for (int j = 0; j < Sides; j++)
                uv[i * Sides + j] = new Vector2(j / (float)Sides, i / (float)Segments);
            for (int i = 0; i < Segments; i++)
            for (int j = 0; j < Sides; j++)
            {
                int a0 = i * Sides + j, b0 = i * Sides + (j + 1) % Sides;
                int a1 = a0 + Sides, b1 = b0 + Sides;
                tri[k++] = a0; tri[k++] = b0; tri[k++] = a1;      // outward, clockwise seen from outside
                tri[k++] = b0; tri[k++] = b1; tri[k++] = a1;
            }
            _mesh.vertices = _v;
            _mesh.normals = _n;
            _mesh.uv = uv;
            _mesh.triangles = tri;
            GetComponent<MeshFilter>().sharedMesh = _mesh;
        }

        public void SetVisible(bool visible, bool shadowOnly)
        {
            var mode = shadowOnly ? UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly : UnityEngine.Rendering.ShadowCastingMode.On;
            foreach (var r in new[] { Renderer, FistRenderer })
            {
                r.enabled = visible;
                r.shadowCastingMode = mode;
            }
        }

        /// <summary>
        /// Builds the arm from <paramref name="root"/> (leaving along <paramref name="rootDir"/>) to <paramref name="hand"/>
        /// (arriving along <paramref name="handDir"/>), closing the fist around a handle running along <paramref name="handleDir"/>.
        /// </summary>
        public void Build(Vector3 root, Vector3 rootDir, Vector3 hand, Vector3 handDir, Vector3 handleDir,
                          float rootRadius, float handRadius, Vector3 fistSize)
        {
            float len = Vector3.Distance(root, hand);
            rootDir = rootDir.sqrMagnitude > 1e-8f ? rootDir.normalized : (hand - root).normalized;
            handDir = handDir.sqrMagnitude > 1e-8f ? handDir.normalized : (hand - root).normalized;
            Vector3 p0 = root, p1 = root + rootDir * (len * 0.3f), p2 = hand - handDir * (len * 0.25f), p3 = hand;

            // a frame carried along the curve without twisting
            Vector3 tan = Tangent(p0, p1, p2, p3, 0f);
            Vector3 nrm = Vector3.Cross(tan, Mathf.Abs(tan.y) < 0.9f ? Vector3.up : Vector3.right).normalized;
            for (int i = 0; i <= Segments; i++)
            {
                float t = i / (float)Segments;
                Vector3 c = Bezier(p0, p1, p2, p3, t);
                if (i > 0)
                {
                    tan = Tangent(p0, p1, p2, p3, t);
                    nrm = (nrm - tan * Vector3.Dot(nrm, tan)).normalized;
                }
                Vector3 bin = Vector3.Cross(tan, nrm);
                // thick at the root, thinner at the wrist, a slight swell in the middle like a soft limb
                float r = Mathf.Lerp(rootRadius, handRadius, t) * (1f + 0.12f * Mathf.Sin(Mathf.PI * t));
                for (int j = 0; j < Sides; j++)
                {
                    float a = j / (float)Sides * Mathf.PI * 2f;
                    Vector3 d = nrm * Mathf.Cos(a) + bin * Mathf.Sin(a);
                    _v[i * Sides + j] = c + d * r;
                    _n[i * Sides + j] = d;
                }
            }
            _mesh.vertices = _v;
            _mesh.normals = _n;
            _mesh.RecalculateBounds();

            // the fist: wider across the arm than along the handle
            Vector3 up = handleDir.sqrMagnitude > 1e-8f ? handleDir.normalized : Vector3.up;
            Vector3 fwd = Vector3.ProjectOnPlane(handDir, up);
            if (fwd.sqrMagnitude < 1e-6f) fwd = Vector3.ProjectOnPlane(Vector3.forward, up);
            _fist.localPosition = hand;
            _fist.localRotation = Quaternion.LookRotation(fwd.normalized, up);
            _fist.localScale = fistSize;
        }

        static Vector3 Bezier(Vector3 a, Vector3 b, Vector3 c, Vector3 d, float t)
        {
            float u = 1f - t;
            return u * u * u * a + 3f * u * u * t * b + 3f * u * t * t * c + t * t * t * d;
        }

        static Vector3 Tangent(Vector3 a, Vector3 b, Vector3 c, Vector3 d, float t)
        {
            float u = 1f - t;
            Vector3 v = 3f * u * u * (b - a) + 6f * u * t * (c - b) + 3f * t * t * (d - c);
            return v.sqrMagnitude > 1e-10f ? v.normalized : (d - a).normalized;
        }

        void OnDestroy()
        {
            if (_mesh) Destroy(_mesh);
        }
    }
}
