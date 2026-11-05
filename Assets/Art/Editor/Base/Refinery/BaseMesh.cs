using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace SlugDesign
{
    /// <summary>
    /// 拠点の本気の小物（Nov 4、精製所の一角から）のメッシュを組む道具。島の NatureMeshKit と違い、法線と UV を自分で持つ
    /// （テクスチャを貼るため。UV は m 単位で、木目は U の向き）。
    /// - RoundBox：角の丸い箱（板・梁・箱・延べ棒）。面は平ら、角だけ丸い。wob で手作りのゆがみ
    /// - Lathe：回転体（壺・臼の石・鉢・灯の笠）。輪郭 (半径, 高さ) を下から上へ
    /// - Tube：中心線に沿った筒（縄・軸・つるの茎）
    /// - Leaf：つたの葉 1 枚（ハート形、少し折れる。両面のマテリアルで）
    /// 面の向きは三角ごとに頂点の法線へそろえる（巻き順を気にしなくていい）。
    /// </summary>
    public class BaseMesh
    {
        readonly List<Vector3> _v = new List<Vector3>();
        readonly List<Vector3> _n = new List<Vector3>();
        readonly List<Vector2> _uv = new List<Vector2>();
        readonly List<int>[] _t;

        public BaseMesh(int submeshes)
        {
            _t = new List<int>[submeshes];
            for (int i = 0; i < submeshes; i++) _t[i] = new List<int>();
        }

        public int Count => _v.Count;

        public int Add(Vector3 p, Vector3 n, Vector2 uv) { _v.Add(p); _n.Add(n); _uv.Add(uv); return _v.Count - 1; }

        public void Tri(int sub, int a, int b, int c)
        {
            var f = Vector3.Cross(_v[b] - _v[a], _v[c] - _v[a]);
            if (Vector3.Dot(f, _n[a] + _n[b] + _n[c]) < 0f) { int t = b; b = c; c = t; }
            _t[sub].Add(a); _t[sub].Add(b); _t[sub].Add(c);
        }

        public void Quad(int sub, int a, int b, int c, int d) { Tri(sub, a, b, c); Tri(sub, a, c, d); }

        static float N3(Vector3 p, int seed) => PropKit.Noise(p, seed);

        // ───────── 角の丸い箱 ─────────

        /// <summary>
        /// 角の丸い箱：真ん中 c、大きさ size、向き rot、角の丸み r（m）。seg は丸みの刻み。
        /// wob は面のゆがみ（m）、uvOff は貼り方のずらし（同じ木目が並ばないように）。
        /// </summary>
        public void RoundBox(int sub, Vector3 c, Vector3 size, Quaternion rot, float r, int seg = 3, float wob = 0f, int seed = 0, Vector2 uvOff = default)
        {
            var half = size * 0.5f;
            r = Mathf.Min(r, Mathf.Min(half.x, Mathf.Min(half.y, half.z)) * 0.95f);
            var inner = half - Vector3.one * r;
            for (int axis = 0; axis < 3; axis++)
                for (int s = -1; s <= 1; s += 2)
                {
                    int bAx = (axis + 1) % 3, cAx = (axis + 2) % 3;
                    var bs = Samples(half[bAx], r, seg);
                    var cs = Samples(half[cAx], r, seg);
                    bool bLong = half[bAx] >= half[cAx];
                    int start = _v.Count;
                    for (int j = 0; j < cs.Count; j++)
                        for (int i = 0; i < bs.Count; i++)
                        {
                            var p = Vector3.zero;
                            p[axis] = s * half[axis]; p[bAx] = bs[i]; p[cAx] = cs[j];
                            var cl = new Vector3(Mathf.Clamp(p.x, -inner.x, inner.x), Mathf.Clamp(p.y, -inner.y, inner.y), Mathf.Clamp(p.z, -inner.z, inner.z));
                            var d = p - cl;
                            var n = d.sqrMagnitude > 1e-10f ? d.normalized : FaceN(axis, s);
                            var q = cl + n * r;
                            if (wob > 0f) q += n * (wob * N3(q * 2.3f + Vector3.one * seed, seed));
                            var uv = bLong ? new Vector2(bs[i], cs[j]) : new Vector2(cs[j], bs[i]);
                            Add(c + rot * q, rot * n, uv + uvOff + new Vector2(axis * 0.37f + s * 0.11f, axis * 0.53f));
                        }
                    int w = bs.Count;
                    for (int j = 0; j < cs.Count - 1; j++)
                        for (int i = 0; i < w - 1; i++)
                            Quad(sub, start + j * w + i, start + j * w + i + 1, start + (j + 1) * w + i + 1, start + (j + 1) * w + i);
                }
        }

        static Vector3 FaceN(int axis, int s) { var n = Vector3.zero; n[axis] = s; return n; }

        /// <summary>丸みのぶんだけ角へ寄せた刻み（-h〜h）。丸みは角度が等しくなるように、真ん中は 0.3 m おき。</summary>
        static List<float> Samples(float h, float r, int seg)
        {
            float inner = h - r;
            var o = new List<float>();
            for (int j = seg; j >= 1; j--) o.Add(-(inner + r * Mathf.Tan(Mathf.PI * 0.25f * j / seg)));
            int mid = Mathf.Max(1, Mathf.CeilToInt(2f * inner / 0.3f));
            for (int k = 0; k <= mid; k++) o.Add(Mathf.Lerp(-inner, inner, k / (float)mid));
            for (int j = 1; j <= seg; j++) o.Add(inner + r * Mathf.Tan(Mathf.PI * 0.25f * j / seg));
            return o;
        }

        // ───────── 回転体 ─────────

        /// <summary>
        /// 回転体：足元 c、向き rot、輪郭 prof（x＝半径、y＝高さ、下から上）、まわりの刻み segs。
        /// wob は輪郭のゆがみ（手びねりの壺・削った石）。UV は U＝まわり（m）、V＝輪郭に沿って（m）。
        /// </summary>
        public void Lathe(int sub, Vector3 c, Quaternion rot, IList<Vector2> prof, int segs, float wob = 0f, int seed = 0, float squash = 1f)
        {
            int rows = prof.Count;
            var arc = new float[rows];
            for (int k = 1; k < rows; k++) arc[k] = arc[k - 1] + (prof[k] - prof[k - 1]).magnitude;
            float maxR = 0f; foreach (var p in prof) maxR = Mathf.Max(maxR, p.x);
            int start = _v.Count;
            for (int k = 0; k < rows; k++)
            {
                var tan = prof[Mathf.Min(rows - 1, k + 1)] - prof[Mathf.Max(0, k - 1)];
                var n2 = new Vector2(tan.y, -tan.x).normalized;
                for (int j = 0; j <= segs; j++)
                {
                    float a = Mathf.PI * 2f * j / segs;
                    var dir = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a) * squash);
                    float rr = prof[k].x;
                    if (wob > 0f && rr > 1e-4f) rr *= 1f + wob * N3(new Vector3(Mathf.Cos(a) * 2f, prof[k].y * 3f, Mathf.Sin(a) * 2f), seed);
                    var pos = dir * rr + Vector3.up * prof[k].y;
                    var n = new Vector3(dir.x * n2.x, n2.y, dir.z * n2.x).normalized;
                    Add(c + rot * pos, rot * n, new Vector2(a * maxR, arc[k]));
                }
            }
            int w = segs + 1;
            for (int k = 0; k < rows - 1; k++)
                for (int j = 0; j < segs; j++)
                    Quad(sub, start + k * w + j, start + (k + 1) * w + j, start + (k + 1) * w + j + 1, start + k * w + j + 1);
        }

        // ───────── 筒 ─────────

        /// <summary>中心線 path(t) に沿った筒（半径 radius(t)）。ends で両端にふた。UV は U＝長さ（m）、V＝まわり（m）。</summary>
        public void Tube(int sub, Func<float, Vector3> path, Func<float, float> radius, int rows, int sides, bool ends = true)
        {
            var whole = (path(1f) - path(0f)).normalized;
            var refUp = Mathf.Abs(Vector3.Dot(whole, Vector3.up)) > 0.7f ? Vector3.forward : Vector3.up;
            int start = _v.Count;
            float len = 0f; Vector3 prev = path(0f);
            for (int k = 0; k <= rows; k++)
            {
                float t = k / (float)rows;
                var p = path(t);
                len += (p - prev).magnitude; prev = p;
                var tan = path(Mathf.Min(1f, t + 0.01f)) - path(Mathf.Max(0f, t - 0.01f));
                tan = tan.sqrMagnitude < 1e-10f ? whole : tan.normalized;
                var side = Vector3.Cross(tan, refUp);
                if (side.sqrMagnitude < 1e-6f) side = Vector3.Cross(tan, Vector3.right);
                side.Normalize();
                var up = Vector3.Cross(side, tan);
                float rr = radius(t);
                for (int j = 0; j <= sides; j++)
                {
                    float a = Mathf.PI * 2f * j / sides;
                    var n = side * Mathf.Cos(a) + up * Mathf.Sin(a);
                    Add(p + n * rr, n, new Vector2(len, a * rr));
                }
            }
            int w = sides + 1;
            for (int k = 0; k < rows; k++)
                for (int j = 0; j < sides; j++)
                    Quad(sub, start + k * w + j, start + (k + 1) * w + j, start + (k + 1) * w + j + 1, start + k * w + j + 1);
            if (!ends) return;
            foreach (int k in new[] { 0, rows })
            {
                float t = k / (float)rows;
                var tan = (path(Mathf.Min(1f, t + 0.01f)) - path(Mathf.Max(0f, t - 0.01f))).normalized * (k == 0 ? -1f : 1f);
                int ci = Add(path(t), tan, Vector2.zero);
                int ring = _v.Count;
                for (int j = 0; j <= sides; j++) { var src = _v[start + k * w + j]; Add(src, tan, new Vector2((src - path(t)).x, (src - path(t)).z)); }
                for (int j = 0; j < sides; j++) Tri(sub, ci, ring + j, ring + j + 1);
            }
        }

        public void Stick(int sub, Vector3 a, Vector3 b, float r, int sides = 8) => Tube(sub, t => Vector3.Lerp(a, b, t), _ => r, 1, sides);

        // ───────── 葉 ─────────

        /// <summary>つたの葉 1 枚：付け根 root から向き dir へ長さ len、幅 w。葉の表は up の側。真ん中で少し折れる。</summary>
        public void Leaf(int sub, Vector3 root, Vector3 dir, Vector3 up, float len, float w)
        {
            dir.Normalize();
            var side = Vector3.Cross(up, dir).normalized;
            up = Vector3.Cross(dir, side).normalized;
            var n = up;
            int a = Add(root, n, new Vector2(0f, 0.5f));
            int l = Add(root + dir * (len * 0.45f) + side * w * 0.5f - up * (len * 0.06f), (n - side * 0.3f).normalized, new Vector2(0.45f, 0f));
            int t = Add(root + dir * len - up * (len * 0.08f), n, new Vector2(1f, 0.5f));
            int r = Add(root + dir * (len * 0.45f) - side * w * 0.5f - up * (len * 0.06f), (n + side * 0.3f).normalized, new Vector2(0.45f, 1f));
            int m = Add(root + dir * (len * 0.45f) + up * (len * 0.03f), n, new Vector2(0.45f, 0.5f));
            Tri(sub, a, l, m); Tri(sub, l, t, m); Tri(sub, t, r, m); Tri(sub, r, a, m);
        }

        // ───────── 保存 ─────────

        /// <summary>recalcNormals なら法線を形から計算し直す（岩の面のでこぼこを光に出す）。</summary>
        public Mesh ToMesh(string name, bool recalcNormals = false)
        {
            var mesh = new Mesh { name = name, indexFormat = _v.Count > 65000 ? IndexFormat.UInt32 : IndexFormat.UInt16 };
            mesh.SetVertices(_v);
            mesh.SetNormals(_n);
            mesh.SetUVs(0, _uv);
            mesh.subMeshCount = _t.Length;
            for (int i = 0; i < _t.Length; i++) mesh.SetTriangles(_t[i], i);
            if (recalcNormals) mesh.RecalculateNormals();
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();
            return mesh;
        }

        public int Triangles { get { int n = 0; foreach (var t in _t) n += t.Count / 3; return n; } }
    }
}
