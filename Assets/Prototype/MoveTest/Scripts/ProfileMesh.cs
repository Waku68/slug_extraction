using System.Collections.Generic;
using UnityEngine;

namespace SlugMove
{
    /// <summary>
    /// Low-poly solids for tools built from code. The main shape is a side profile (a polygon seen from +X, given as
    /// (z, y) points) pushed out to a thickness along X, its edges bevelled and every face flat-shaded, so a stock or a
    /// receiver reads as a chunky toy-like part. Round parts (barrels) are tubes along any axis.
    /// Triangles are wound from the outward direction each face is given, so profiles may go either way round and
    /// may be concave (a trigger guard).
    /// </summary>
    public class ProfileMesh
    {
        readonly List<Vector3> _v = new List<Vector3>();
        readonly List<Vector3> _n = new List<Vector3>();
        readonly List<Vector2> _uv = new List<Vector2>();
        readonly List<int> _t = new List<int>();

        /// <summary>Texture repeats per metre (the wood grain runs along U = z).</summary>
        public float uvScale = 8f;

        /// <summary>
        /// Adds a solid: <paramref name="profile"/> (z, y) extruded from x = cx - half to cx + half, with its edges
        /// bevelled by <paramref name="bevel"/>.
        /// </summary>
        public ProfileMesh Extrude(IList<Vector2> profile, float half, float bevel, float cx = 0f)
        {
            var p = new List<Vector2>(profile);
            if (SignedArea(p) < 0f) p.Reverse();               // counter-clockwise seen from +X
            int n = p.Count;
            float b = Mathf.Clamp(bevel, 0f, half * 0.9f);
            var inset = b > 0f ? Inset(p, b) : p;
            float xo = half - b;

            // the two flat sides
            var tris = Triangulate(inset);
            foreach (float side in new[] { 1f, -1f })
            {
                var nrm = new Vector3(side, 0f, 0f);
                int start = _v.Count;
                foreach (var q in inset) Add(P(q, cx + side * half), nrm, q);
                for (int i = 0; i + 2 < tris.Count; i += 3) Tri(start + tris[i], start + tris[i + 1], start + tris[i + 2], nrm);
            }
            // the rim all round, and the bevels joining it to the sides
            for (int i = 0; i < n; i++)
            {
                int j = (i + 1) % n;
                Vector2 e = p[j] - p[i];
                if (e.sqrMagnitude < 1e-12f) continue;
                var outward = new Vector3(0f, -e.x, e.y).normalized;       // right of a counter-clockwise edge
                Vector3 a0 = P(p[i], cx + xo), a1 = P(p[j], cx + xo), b0 = P(p[i], cx - xo), b1 = P(p[j], cx - xo);
                Quad(a0, a1, b1, b0, outward, true);
                if (b <= 0f) continue;
                Quad(P(inset[i], cx + half), P(inset[j], cx + half), a1, a0, (outward + Vector3.right).normalized, true);
                Quad(b0, b1, P(inset[j], cx - half), P(inset[i], cx - half), (outward + Vector3.left).normalized, true);
            }
            return this;
        }

        /// <summary>Adds a round tube from <paramref name="a"/> to <paramref name="b"/>, closed at both ends.</summary>
        public ProfileMesh Tube(Vector3 a, Vector3 b, float radius, int sides = 10, float radiusB = -1f)
        {
            if (radiusB < 0f) radiusB = radius;
            Vector3 axis = (b - a).normalized;
            Vector3 u = Vector3.Cross(axis, Mathf.Abs(axis.y) < 0.9f ? Vector3.up : Vector3.right).normalized;
            Vector3 w = Vector3.Cross(axis, u);
            int ring = _v.Count;
            for (int k = 0; k <= sides; k++)
            {
                float ang = k / (float)sides * Mathf.PI * 2f;
                Vector3 d = u * Mathf.Cos(ang) + w * Mathf.Sin(ang);
                Add(a + d * radius, d, new Vector2(k / (float)sides, 0f));
                Add(b + d * radiusB, d, new Vector2(k / (float)sides, 1f));
            }
            for (int k = 0; k < sides; k++)
            {
                int i0 = ring + k * 2, i1 = i0 + 1, i2 = i0 + 2, i3 = i0 + 3;
                Vector3 mid = (_n[i0] + _n[i2]).normalized;
                Tri(i0, i2, i1, mid);
                Tri(i1, i2, i3, mid);
            }
            foreach (var (c, r, nrm) in new[] { (a, radius, -axis), (b, radiusB, axis) })
            {
                int center = _v.Count;
                Add(c, nrm, new Vector2(0.5f, 0.5f));
                for (int k = 0; k <= sides; k++)
                {
                    float ang = k / (float)sides * Mathf.PI * 2f;
                    Add(c + (u * Mathf.Cos(ang) + w * Mathf.Sin(ang)) * r, nrm, new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * 0.5f + Vector2.one * 0.5f);
                }
                for (int k = 0; k < sides; k++) Tri(center, center + 1 + k, center + 2 + k, nrm);
            }
            return this;
        }

        /// <summary>Adds a flat polygon (a star, a window) lying in the plane through <paramref name="origin"/> spanned by <paramref name="right"/> and <paramref name="up"/>.</summary>
        public ProfileMesh Flat(IList<Vector2> polygon, Vector3 origin, Vector3 right, Vector3 up, bool twoSided)
        {
            var p = new List<Vector2>(polygon);
            if (SignedArea(p) < 0f) p.Reverse();
            var tris = Triangulate(p);
            Vector3 nrm = Vector3.Cross(right, up).normalized;
            foreach (float side in twoSided ? new[] { 1f, -1f } : new[] { 1f })
            {
                int start = _v.Count;
                foreach (var q in p) Add(origin + right * q.x + up * q.y, nrm * side, q);
                for (int i = 0; i + 2 < tris.Count; i += 3) Tri(start + tris[i], start + tris[i + 1], start + tris[i + 2], nrm * side);
            }
            return this;
        }

        /// <summary>Moves every vertex added so far by <paramref name="delta"/> (to put a part's pivot where it turns).</summary>
        public ProfileMesh Offset(Vector3 delta)
        {
            for (int i = 0; i < _v.Count; i++) _v[i] += delta;
            return this;
        }

        public Mesh ToMesh(string name)
        {
            var m = new Mesh { name = name, hideFlags = HideFlags.DontSave };
            m.SetVertices(_v);
            m.SetNormals(_n);
            m.SetUVs(0, _uv);
            m.SetTriangles(_t, 0);
            m.RecalculateBounds();
            return m;
        }

        // ------------------------------------------------------------------ building blocks
        static Vector3 P(Vector2 zy, float x) => new Vector3(x, zy.y, zy.x);

        void Add(Vector3 v, Vector3 n, Vector2 uv)
        {
            _v.Add(v);
            _n.Add(n);
            _uv.Add(uv * uvScale);
        }

        /// <summary>A triangle facing <paramref name="outward"/> (a front face's edge cross product points out of it).</summary>
        void Tri(int a, int b, int c, Vector3 outward)
        {
            if (Vector3.Dot(Vector3.Cross(_v[b] - _v[a], _v[c] - _v[a]), outward) < 0f) { int t = b; b = c; c = t; }
            _t.Add(a); _t.Add(b); _t.Add(c);
        }

        /// <summary>A flat-shaded quad; its normal is turned toward <paramref name="outward"/>.</summary>
        void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 outward, bool grainAlongZ)
        {
            Vector3 nrm = Vector3.Cross(c - a, d - b);
            nrm = nrm.sqrMagnitude > 1e-14f ? nrm.normalized : outward;
            if (Vector3.Dot(nrm, outward) < 0f) nrm = -nrm;
            int s = _v.Count;
            foreach (var v in new[] { a, b, c, d })
                Add(v, nrm, grainAlongZ ? new Vector2(v.z, v.x + v.y) : new Vector2(v.x, v.y));
            Tri(s, s + 1, s + 2, nrm);
            Tri(s, s + 2, s + 3, nrm);
        }

        static float SignedArea(List<Vector2> p)
        {
            float a = 0f;
            for (int i = 0; i < p.Count; i++)
            {
                var q = p[i]; var r = p[(i + 1) % p.Count];
                a += q.x * r.y - r.x * q.y;
            }
            return a * 0.5f;
        }

        static float Cross(Vector2 a, Vector2 b) => a.x * b.y - a.y * b.x;

        /// <summary>The polygon moved inward by <paramref name="d"/> (mitred corners; fine for the small bevels used here).</summary>
        static List<Vector2> Inset(List<Vector2> p, float d)
        {
            int n = p.Count;
            var r = new List<Vector2>(n);
            for (int i = 0; i < n; i++)
            {
                Vector2 e1 = (p[i] - p[(i + n - 1) % n]).normalized, e2 = (p[(i + 1) % n] - p[i]).normalized;
                Vector2 n1 = new Vector2(-e1.y, e1.x), n2 = new Vector2(-e2.y, e2.x);     // left = inside
                Vector2 m = (n1 + n2).sqrMagnitude > 1e-8f ? (n1 + n2).normalized : n1;
                r.Add(p[i] + m * (d / Mathf.Max(Vector2.Dot(m, n1), 0.35f)));
            }
            return r;
        }

        /// <summary>Ear clipping of a counter-clockwise simple polygon.</summary>
        static List<int> Triangulate(List<Vector2> p)
        {
            var idx = new List<int>();
            for (int i = 0; i < p.Count; i++) idx.Add(i);
            var tris = new List<int>();
            int guard = 0;
            while (idx.Count > 3 && guard++ < 4096)
            {
                bool clipped = false;
                for (int k = 0; k < idx.Count; k++)
                {
                    int ia = idx[(k + idx.Count - 1) % idx.Count], ib = idx[k], ic = idx[(k + 1) % idx.Count];
                    Vector2 a = p[ia], b = p[ib], c = p[ic];
                    if (Cross(b - a, c - b) <= 1e-10f) continue;              // a reflex corner is not an ear
                    bool inside = false;
                    foreach (int m in idx)
                    {
                        if (m == ia || m == ib || m == ic) continue;
                        if (InTriangle(p[m], a, b, c)) { inside = true; break; }
                    }
                    if (inside) continue;
                    tris.Add(ia); tris.Add(ib); tris.Add(ic);
                    idx.RemoveAt(k);
                    clipped = true;
                    break;
                }
                if (!clipped) break;
            }
            if (idx.Count == 3) { tris.Add(idx[0]); tris.Add(idx[1]); tris.Add(idx[2]); }
            return tris;
        }

        static bool InTriangle(Vector2 q, Vector2 a, Vector2 b, Vector2 c)
            => Cross(b - a, q - a) >= 0f && Cross(c - b, q - b) >= 0f && Cross(a - c, q - c) >= 0f;
    }
}
