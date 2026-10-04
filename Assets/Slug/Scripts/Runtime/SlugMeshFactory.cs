using System;
using System.Collections.Generic;
using UnityEngine;

namespace SlugDesign
{
    /// <summary>One cross-section of a lofted tube.</summary>
    public struct Ring
    {
        public Vector3 center, right, up, forward;
        public float rx, ry;
        public float t; // 0..1 along the tube (written to uv.y)
        public float keel; // dorsal ridge strength (raises the very top of the back)
        public float flare; // widens the lower sides so the base spreads out on the ground
    }

    /// <summary>Key of the body thickness profile (t: 0 tail -> 1 head).</summary>
    [Serializable]
    public struct ProfileKey
    {
        public float t, rx, ry;
        public ProfileKey(float t, float rx, float ry) { this.t = t; this.rx = rx; this.ry = ry; }
    }

    [Serializable]
    public class BodySpec
    {
        public float length = 1f;
        public int rings = 14;
        public int radial = 10;
        public float bottomFlat = 0.5f;     // squash factor of the lower half (flat sole)
        public ProfileKey[] keys;
        public float capTail = 0.2f;        // t-range of the rounded tail end
        public float capHead = 0.2f;        // t-range of the rounded head end
        public float tailPointy = 0f;       // 0 round tail ... 1 pointed tail
        public float tailPow = 1f;          // pointed tail profile: 1 = straight cone, >1 = concave (a thin small point)
        public float endBias = 0.6f;        // ring spacing: 0 uniform ... 1 dense at both ends
        public float bendDeg = 0f;          // spine rises by this angle toward the head
        public float bendStart = 0.4f;
        public float rearScale = 1f;        // the spine behind rearEnd is this much shorter (a shorter belly, same front)
        public float rearEnd = 0.55f;
        public float sDeg = 0f;            // gentle S-curve of the spine (side view), amplitude in degrees
        public float sCycles = 1f;          // number of S periods along the body
        public float keel = 0f;             // dorsal ridge along the back
        public float keelHead = 0f;         // 0 = ridge on the rear half, 1 = ridge on the head region
        public float baseFlare = 0f;        // spreads the lower sides outward (manju-like base)
        public float soleSquare = 0f;       // 0 = round lower half ... 1 = a flat sole with a defined edge (the foot's rim)
    }

    /// <summary>Procedural mesh builder: every part of the slug is a "loft" of elliptical rings.</summary>
    public static class SlugMeshFactory
    {
        // ------------------------------------------------------------------ generic loft
        /// <summary>
        /// A point of the cross-section at angle th (0 = the top of the back): the lower half is squashed by bottomFlat
        /// and, with soleSquare, squared off into a flat sole with rounded edges (a superellipse).
        /// </summary>
        public static Vector2 Section(float th, Ring r, float bottomFlat, float soleSquare)
        {
            float sx = Mathf.Sin(th), cy = Mathf.Cos(th);
            if (cy >= 0f) return new Vector2(sx * r.rx, cy * r.ry * (1f + r.keel * Mathf.Pow(cy, 6f)));
            float e = Mathf.Lerp(1f, 0.4f, soleSquare);
            float px = Mathf.Sign(sx) * Mathf.Pow(Mathf.Abs(sx), e), py = -Mathf.Pow(-cy, e);
            return new Vector2(px * r.rx * (1f + r.flare * -cy), py * r.ry * bottomFlat);
        }

        public static Mesh Loft(string name, IList<Ring> rings, int radial, float bottomFlat = 1f, float soleSquare = 0f)
        {
            int n = rings.Count;
            int cols = radial + 1;
            var verts = new Vector3[n * cols];
            var uvs = new Vector2[n * cols];
            var tans = new Vector4[n * cols];
            var ringRad = new List<Vector2>(n * cols);
            var ringPivot = new List<Vector3>(n * cols);

            for (int i = 0; i < n; i++)
            {
                Ring r = rings[i];
                for (int k = 0; k < cols; k++)
                {
                    float th = Mathf.PI * 2f * k / radial;
                    Vector2 xy = Section(th, r, bottomFlat, soleSquare);
                    int v = i * cols + k;
                    verts[v] = r.center + r.right * xy.x + r.up * xy.y;
                    uvs[v] = new Vector2((float)k / radial, r.t);
                    tans[v] = new Vector4(r.forward.x, r.forward.y, r.forward.z, 1f);
                    ringRad.Add(new Vector2(Mathf.Max((r.rx + r.ry) * 0.5f, 1e-5f), 0f));
                    ringPivot.Add(r.center - r.up * (r.ry * bottomFlat));   // sole-line point under the ring centre
                }
            }

            // analytic-ish smooth normals from finite differences (wraps around the ring)
            var norms = new Vector3[n * cols];
            for (int i = 0; i < n; i++)
            {
                int i0 = Mathf.Max(i - 1, 0), i1 = Mathf.Min(i + 1, n - 1);
                for (int k = 0; k < cols; k++)
                {
                    int ka = (k - 1 + radial) % radial, kb = (k + 1) % radial;
                    Vector3 dT = verts[i1 * cols + k] - verts[i0 * cols + k];
                    Vector3 dTh = verts[i * cols + kb] - verts[i * cols + ka];
                    Vector3 nn = Vector3.Cross(dT, dTh);
                    if (nn.sqrMagnitude < 1e-12f)
                        nn = (i == 0) ? -rings[i].forward : (i == n - 1 ? rings[i].forward : rings[i].up);
                    norms[i * cols + k] = nn.normalized;
                }
            }

            var tris = new List<int>(n * radial * 6);
            for (int i = 0; i < n - 1; i++)
            {
                for (int k = 0; k < radial; k++)
                {
                    int a = i * cols + k, b = (i + 1) * cols + k, c = i * cols + k + 1, d = (i + 1) * cols + k + 1;
                    tris.Add(a); tris.Add(b); tris.Add(c);
                    tris.Add(c); tris.Add(b); tris.Add(d);
                }
            }

            var mesh = new Mesh { name = name };
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            mesh.vertices = verts;
            mesh.normals = norms;
            mesh.tangents = tans;
            mesh.uv = uvs;
            mesh.SetUVs(1, ringRad);
            mesh.SetUVs(2, ringPivot);
            mesh.triangles = tris.ToArray();
            mesh.RecalculateBounds();
            return mesh;
        }

        // ------------------------------------------------------------------ simple shapes
        /// <summary>Unit-ish sphere (radius r) centred on the origin, axis = +Z.</summary>
        public static Mesh Sphere(string name, float r, int radial, int rings)
        {
            var list = new List<Ring>();
            for (int i = 0; i < rings; i++)
            {
                float s = (float)i / (rings - 1);
                float a = Mathf.PI * s;
                list.Add(new Ring
                {
                    center = new Vector3(0, 0, -r * Mathf.Cos(a)),
                    right = Vector3.right, up = Vector3.up, forward = Vector3.forward,
                    rx = r * Mathf.Sin(a), ry = r * Mathf.Sin(a), t = s
                });
            }
            return Loft(name, list, radial);
        }

        /// <summary>
        /// Eyelid: the part of a unit sphere above the horizontal plane y = cut (open edge at the bottom).
        /// Scaled slightly larger than the eye and parented to it, it reads as a half-closed lid.
        /// </summary>
        public static Mesh Lid(string name, float cut, int radial, int rings)
        {
            float th0 = Mathf.Acos(Mathf.Clamp(cut, -0.99f, 0.99f));
            var list = new List<Ring>();
            for (int i = 0; i < rings; i++)
            {
                float s = (float)i / (rings - 1);
                float th = th0 * (1f - s);
                list.Add(new Ring
                {
                    center = new Vector3(0, Mathf.Cos(th), 0),
                    forward = Vector3.up, up = Vector3.forward, right = Vector3.left,
                    rx = Mathf.Sin(th), ry = Mathf.Sin(th), t = s
                });
            }
            return Loft(name, list, radial);
        }

        /// <summary>Tube along a polyline with per-parameter radii (rounded by the radius functions).</summary>
        public static Mesh PathLoft(string name, IList<Vector3> pts, Func<float, float> rx, Func<float, float> ry,
                                    Vector3 upHint, int radial)
        {
            int n = pts.Count;
            var list = new List<Ring>();
            for (int i = 0; i < n; i++)
            {
                float s = (float)i / (n - 1);
                Vector3 f = (pts[Mathf.Min(i + 1, n - 1)] - pts[Mathf.Max(i - 1, 0)]).normalized;
                Vector3 u = (upHint - f * Vector3.Dot(upHint, f));
                if (u.sqrMagnitude < 1e-6f) u = Vector3.Cross(f, Vector3.right);
                u.Normalize();
                Vector3 r = Vector3.Cross(u, f).normalized;
                list.Add(new Ring { center = pts[i], forward = f, up = u, right = r, rx = rx(s), ry = ry(s), t = s });
            }
            return Loft(name, list, radial);
        }

        static float RoundCap(float x) { x = Mathf.Clamp01(x); return Mathf.Sqrt(Mathf.Max(0f, 1f - (1f - x) * (1f - x))); }

        // ------------------------------------------------------------------ ears / antennae-as-ears
        public static Mesh Ear(string name, Vector3 root, Vector3 dirStart, Vector3 dirEnd, Vector3 upHint,
                               float length, float baseR, float tipR, float flat, float bulbR, int radial,
                               Vector3 wiggleAxis = default, float wiggle = 0f, bool pointed = false)
        {
            const int M = 14;
            var pts = new List<Vector3>();
            Vector3 p = root;
            for (int i = 0; i < M; i++)
            {
                // a gentle S: sideways offset sin(2 pi s), zero at the root and at the tip
                float sw = (float)i / (M - 1);
                pts.Add(p + wiggleAxis * (wiggle * Mathf.Sin(2f * Mathf.PI * sw)));
                float s = (float)i / (M - 1);
                Vector3 d = Vector3.Slerp(dirStart.normalized, dirEnd.normalized, s);
                p += d * (length / (M - 1));
            }
            Func<float, float> rad = s =>
            {
                float r = Mathf.Lerp(baseR, tipR, Mathf.SmoothStep(0f, 1f, s));
                if (bulbR > 0f) r = Mathf.Max(r, bulbR * Mathf.Exp(-Mathf.Pow((s - 0.86f) / 0.14f, 2f)));
                float cap = pointed ? Mathf.Clamp01((1f - s) / 0.5f) : RoundCap((1f - s) / 0.16f);   // pointed: a natural taper to a tip
                float rootCap = Mathf.Clamp01(s / 0.04f);       // closes the buried root end
                return r * cap * Mathf.Lerp(0.6f, 1f, rootCap);
            };
            return PathLoft(name, pts, s => rad(s) * flat, s => rad(s), upHint, radial);
        }

        // ------------------------------------------------------------------ hand: stubby arm + round mitt (no fingers)
        public static Mesh Hand(string name, float armLength, float armR, float handR, int radial)
        {
            const int M = 16;
            float zc = armLength + handR * 0.7f;
            float total = zc + handR;
            var list = new List<Ring>();
            for (int i = 0; i < M; i++)
            {
                float s = (float)i / (M - 1);
                float z = s * total;
                float arm = z <= zc ? armR * Mathf.Lerp(1.0f, 0.85f, Mathf.Clamp01(z / Mathf.Max(armLength, 1e-3f))) : 0f;
                float dz = z - zc;
                float sph = Mathf.Abs(dz) < handR ? Mathf.Sqrt(handR * handR - dz * dz) : 0f;
                float r = Mathf.Max(arm, sph);
                if (i == M - 1) r = 0f;
                list.Add(new Ring
                {
                    center = new Vector3(0, 0, z), right = Vector3.right, up = Vector3.up, forward = Vector3.forward,
                    rx = r, ry = r, t = s
                });
            }
            return Loft(name, list, radial);
        }

        // ------------------------------------------------------------------ hand: tapering cone, slightly curved, no fingers
        /// <summary>Thick at the root, thin at the tip; the tip bends down a little (local +Z forward, +Y up).</summary>
        public static Mesh ConeHand(string name, float length, float baseR, float tipR, float curve, int radial)
        {
            const int M = 12;
            var pts = new List<Vector3>();
            for (int i = 0; i < M; i++)
            {
                float s = (float)i / (M - 1);
                pts.Add(new Vector3(0f, -curve * length * s * s, s * length));
            }
            Func<float, float> rad = s =>
            {
                float r = Mathf.Lerp(baseR, tipR, Mathf.Pow(s, 0.9f));
                return r * RoundCap((1f - s) / 0.1f);      // rounded tip
            };
            return PathLoft(name, pts, rad, rad, Vector3.up, radial);
        }

        // ------------------------------------------------------------------ bake a unit mesh into body space
        /// <summary>
        /// Maps a unit mesh (sphere / lid cap) through the frame (centre, X, Y, Z) with per-axis radii.
        /// The result lives in the same space as the body, so it can ride the body wave as a rigid part.
        /// A mirrored frame (left-handed) automatically gets its triangle winding reversed.
        /// </summary>
        public static Mesh BakeEllipsoid(Mesh unit, string name, Vector3 c, Vector3 X, Vector3 Y, Vector3 Z, float sx, float sy, float sz)
        {
            var uv = unit.vertices;
            var un = unit.normals;
            var verts = new Vector3[uv.Length];
            var norms = new Vector3[uv.Length];
            for (int i = 0; i < uv.Length; i++)
            {
                verts[i] = c + X * (uv[i].x * sx) + Y * (uv[i].y * sy) + Z * (uv[i].z * sz);
                norms[i] = (X * (un[i].x / sx) + Y * (un[i].y / sy) + Z * (un[i].z / sz)).normalized;
            }
            var tris = unit.triangles;
            if (Vector3.Dot(Vector3.Cross(X, Y), Z) < 0f)
                for (int i = 0; i < tris.Length; i += 3) { int t = tris[i + 1]; tris[i + 1] = tris[i + 2]; tris[i + 2] = t; }
            var m = new Mesh { name = name };
            m.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            m.vertices = verts;
            m.normals = norms;
            m.uv = unit.uv;
            var tan = new Vector4[uv.Length];
            for (int i = 0; i < tan.Length; i++) tan[i] = new Vector4(X.x, X.y, X.z, 1f);
            m.tangents = tan;
            m.triangles = tris;
            m.RecalculateBounds();
            return m;
        }

        // ------------------------------------------------------------------ mouth: thin smile tube in face space (x right, y up, z out)
        public static Mesh Smile(string name, float halfWidth, float depth, float thickness, int radial)
        {
            const int M = 12;
            var pts = new List<Vector3>();
            for (int i = 0; i < M; i++)
            {
                float s = (float)i / (M - 1) * 2f - 1f;
                pts.Add(new Vector3(s * halfWidth, -depth * (1f - s * s), 0f));
            }
            return PathLoft(name, pts, s => thickness * RoundCap(Mathf.Min(s, 1f - s) / 0.15f),
                            s => thickness * RoundCap(Mathf.Min(s, 1f - s) / 0.15f), Vector3.forward, radial);
        }

        // ------------------------------------------------------------------ body
        public class BodyShape
        {
            public BodySpec spec;
            public Mesh mesh;
            public Vector3[] verts;
            public int[] tris;
            public Vector3[] norms;
            public Vector2[] uvs;
            public float yShift;

            /// <summary>Angle of the spine above horizontal at t (radians): head lift plus a gentle S-curve.</summary>
            public float Phi(float t)
            {
                float a = 0f;
                if (spec.bendDeg != 0f)
                    a += spec.bendDeg * Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(spec.bendStart, 1f, t));
                if (spec.sDeg != 0f)
                    a += spec.sDeg * Mathf.Sin(2f * Mathf.PI * spec.sCycles * t);
                return a * Mathf.Deg2Rad;
            }

            public float Profile(float t, bool x)
            {
                var k = spec.keys;
                int last = k.Length - 1;
                if (t <= k[0].t) return x ? k[0].rx : k[0].ry;
                if (t >= k[last].t) return x ? k[last].rx : k[last].ry;
                int i = 0;
                while (i < last - 1 && t > k[i + 1].t) i++;
                Func<int, float> y = j => x ? k[j].rx : k[j].ry;
                float t0 = k[i].t, t1 = k[i + 1].t, h = t1 - t0, u = (t - t0) / h;
                float m0 = i > 0 ? (y(i + 1) - y(i - 1)) / (k[i + 1].t - k[i - 1].t) : (y(i + 1) - y(i)) / h;
                float m1 = i + 1 < last ? (y(i + 2) - y(i)) / (k[i + 2].t - k[i].t) : (y(i + 1) - y(i)) / h;
                float u2 = u * u, u3 = u2 * u;
                return (2 * u3 - 3 * u2 + 1) * y(i) + (u3 - 2 * u2 + u) * h * m0 + (-2 * u3 + 3 * u2) * y(i + 1) + (u3 - u2) * h * m1;
            }

            float Cap(float t)
            {
                float tail = RoundCap(t / spec.capTail);
                tail = Mathf.Lerp(tail, Mathf.Pow(Mathf.Clamp01(t / spec.capTail), spec.tailPow), spec.tailPointy);
                float head = RoundCap((1f - t) / spec.capHead);
                return tail * head;
            }

            /// <summary>Pivot (sole-line point) and radius of the ring at t. Used by parts that follow the crawl.</summary>
            public void RingInfo(float t, out Vector3 pivot, out float radius)
            {
                Ring f = FrameAt(t);
                pivot = f.center - f.up * (f.ry * spec.bottomFlat);
                radius = Mathf.Max((f.rx + f.ry) * 0.5f, 1e-5f);
            }

            /// <summary>Length of the spine per unit t at t: shorter toward the tail when the belly is shortened.</summary>
            float StepLength(float t)
            {
                if (spec.rearScale >= 1f) return spec.length;
                return spec.length * Mathf.Lerp(spec.rearScale, 1f, Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(spec.rearEnd - 0.2f, spec.rearEnd, t)));
            }

            public Vector3 SpinePoint(float t)
            {
                int steps = Mathf.Max(1, Mathf.CeilToInt(t * 240f));
                float dt = t / steps;
                Vector3 p = Vector3.zero;
                for (int s = 0; s < steps; s++)
                {
                    float ts = (s + 0.5f) * dt;
                    float ph = Phi(ts);
                    p += new Vector3(0, Mathf.Sin(ph), Mathf.Cos(ph)) * (dt * StepLength(ts));
                }
                p.y += yShift;
                return p;
            }

            /// <summary>Full frame of the body at t (after the ground shift).</summary>
            public Ring FrameAt(float t)
            {
                float ph = Phi(t);
                Vector3 f = new Vector3(0, Mathf.Sin(ph), Mathf.Cos(ph));
                Vector3 u = new Vector3(0, Mathf.Cos(ph), -Mathf.Sin(ph));
                return new Ring
                {
                    center = SpinePoint(t), forward = f, up = u, right = Vector3.Cross(u, f),
                    rx = Profile(t, true) * Cap(t), ry = Profile(t, false) * Cap(t), t = t,
                    keel = spec.keel * Mathf.Lerp(1f - Mathf.SmoothStep(0.3f, 0.75f, t), Mathf.SmoothStep(0.4f, 0.8f, t) * (1f - Mathf.SmoothStep(0.9f, 1f, t)), spec.keelHead),
                    flare = spec.baseFlare
                };
            }

            public bool Raycast(Vector3 o, Vector3 d, out Vector3 hit, out Vector3 normal) => RaycastT(o, d, out hit, out normal, out _);

            /// <summary>Raycast that also returns the position along the body (0 tail ... 1 head) of the hit point.</summary>
            public bool RaycastT(Vector3 o, Vector3 d, out Vector3 hit, out Vector3 normal, out float t)
            {
                float best = float.MaxValue;
                hit = o; normal = Vector3.up; t = 0f;
                for (int i = 0; i < tris.Length; i += 3)
                {
                    Vector3 a = verts[tris[i]], b = verts[tris[i + 1]], c = verts[tris[i + 2]];
                    Vector3 e1 = b - a, e2 = c - a, pv = Vector3.Cross(d, e2);
                    float det = Vector3.Dot(e1, pv);
                    if (Mathf.Abs(det) < 1e-9f) continue;
                    float inv = 1f / det;
                    Vector3 tv = o - a;
                    float u = Vector3.Dot(tv, pv) * inv;
                    if (u < 0f || u > 1f) continue;
                    Vector3 qv = Vector3.Cross(tv, e1);
                    float v = Vector3.Dot(d, qv) * inv;
                    if (v < 0f || u + v > 1f) continue;
                    float dist = Vector3.Dot(e2, qv) * inv;
                    if (dist > 1e-5f && dist < best)
                    {
                        best = dist;
                        hit = o + d * dist;
                        normal = (norms[tris[i]] * (1f - u - v) + norms[tris[i + 1]] * u + norms[tris[i + 2]] * v).normalized;
                        t = uvs[tris[i]].y * (1f - u - v) + uvs[tris[i + 1]].y * u + uvs[tris[i + 2]].y * v;
                    }
                }
                return best < float.MaxValue;
            }
        }

        public static BodyShape Body(string name, BodySpec spec)
        {
            var shape = new BodyShape { spec = spec };
            int n = spec.rings;

            // two passes: first find the ground shift, then build the final rings
            shape.yShift = 0f;
            var rings = new List<Ring>();
            for (int pass = 0; pass < 2; pass++)
            {
                rings.Clear();
                float minY = float.MaxValue;
                for (int i = 0; i < n; i++)
                {
                    float s = (float)i / (n - 1);
                    float t = Mathf.Lerp(s, 0.5f - 0.5f * Mathf.Cos(Mathf.PI * s), spec.endBias);
                    Ring r = shape.FrameAt(t);
                    rings.Add(r);
                    for (int k = 0; k < spec.radial; k++)
                    {
                        float th = Mathf.PI * 2f * k / spec.radial;
                        Vector2 xy = Section(th, r, spec.bottomFlat, spec.soleSquare);
                        Vector3 p = r.center + r.right * xy.x + r.up * xy.y;
                        minY = Mathf.Min(minY, p.y);
                    }
                }
                if (pass == 0) shape.yShift = -minY;
            }

            shape.mesh = Loft(name, rings, spec.radial, spec.bottomFlat, spec.soleSquare);
            shape.verts = shape.mesh.vertices;
            shape.norms = shape.mesh.normals;
            shape.uvs = shape.mesh.uv;
            shape.tris = shape.mesh.triangles;
            return shape;
        }
    }
}
