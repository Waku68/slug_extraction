using System;
using System.Collections.Generic;
using UnityEngine;

namespace SlugDesign
{
    /// <summary>
    /// 島の植物・岩を数式から作るための部品箱（Oct 20 わくさん：Blender を使わず Unity のコードで作る）。
    /// 筒（幹・根・枝）、葉身（厚みのある閉じた板）、玉（実）を 1 つのメッシュに足していき、最後に Mesh にする。
    /// 法線はなめらか（Slug/Toon の輪郭線が割れないように）。カクカクに見せたいときは分割を減らし、マテリアルの _Facet を 1 にする。
    /// 頂点の色は風の揺れの重み（Oct 25）：R＝しなり（根元 0 → 先 1）、G＝葉先の震え、B＝部品ごとの揺れのずれ（0〜1）。
    /// 塗らなかった頂点は黒（揺れない）。部品を足した直後に Paint で塗る。揺れはシェーダーの SlugWind.hlsl。
    /// </summary>
    public class NatureMeshKit
    {
        readonly List<Vector3> _v = new List<Vector3>();
        readonly List<Vector2> _uv = new List<Vector2>();
        readonly List<Color> _col = new List<Color>();
        readonly List<List<int>> _tris = new List<List<int>>();

        public NatureMeshKit(int submeshes)
        {
            for (int i = 0; i < submeshes; i++) _tris.Add(new List<int>());
        }

        public int Vert(Vector3 p, Vector2 uv) { _v.Add(p); _uv.Add(uv); _col.Add(new Color(0f, 0f, 0f, 1f)); return _v.Count - 1; }

        /// <summary>いまの頂点の数（Paint で、ここから先に足した部品だけを塗るのに使う）。</summary>
        public int VertexCount => _v.Count;

        /// <summary>
        /// from 番から後の頂点に、風の重みを塗る。paint(位置, uv)。uv.y は筒なら t、葉身なら軸に沿った s、uv.x は葉身なら 0 左ふち・0.5 軸・1 右ふち。
        /// </summary>
        public void Paint(int from, Func<Vector3, Vector2, Color> paint)
        {
            for (int i = from; i < _v.Count; i++) _col[i] = paint(_v[i], _uv[i]);
        }
        public void Tri(int sub, int a, int b, int c) { var t = _tris[sub]; t.Add(a); t.Add(b); t.Add(c); }
        public void Quad(int sub, int a, int b, int c, int d) { Tri(sub, a, b, c); Tri(sub, a, c, d); }
        void QuadR(int sub, int a, int b, int c, int d) { Quad(sub, d, c, b, a); }   // 葉身は向きが逆に組んであるので裏返す

        /// <summary>
        /// 中心線 center(t) に沿った筒（t=0〜1）。rows に (t, 半径, その行までの帯のマテリアル) を並べる。
        /// 半径は radius(t) × 行の倍率。下と上にふたをする（capBottom / capTop のマテリアル、-1 でなし）。
        /// </summary>
        public void Tube(Func<float, Vector3> center, Func<float, float> radius, IList<Vector3> rows, int sides,
                         int capBottom, int capTop, float twistPerT = 0f)
        {
            int prevStart = -1;
            for (int r = 0; r < rows.Count; r++)
            {
                float t = rows[r].x, scale = rows[r].y; int sub = (int)rows[r].z;
                Vector3 c = center(t);
                Vector3 tan = (center(Mathf.Min(1f, t + 0.01f)) - center(Mathf.Max(0f, t - 0.01f))).normalized;
                Vector3 side = Vector3.Cross(tan, Vector3.forward);
                if (side.sqrMagnitude < 1e-4f) side = Vector3.Cross(tan, Vector3.right);
                side.Normalize();
                Vector3 up = Vector3.Cross(side, tan).normalized;
                float rad = radius(t) * scale;
                int start = _v.Count;
                for (int i = 0; i < sides; i++)
                {
                    float a = twistPerT * t + Mathf.PI * 2f * i / sides;
                    Vert(c + (side * Mathf.Cos(a) + up * Mathf.Sin(a)) * rad, new Vector2(i / (float)sides, t));
                }
                if (prevStart >= 0)
                    for (int i = 0; i < sides; i++)
                    {
                        int j = (i + 1) % sides;
                        Quad(sub, prevStart + i, start + i, start + j, prevStart + j);
                    }
                prevStart = start;
            }
            if (capBottom >= 0) Cap(capBottom, center(rows[0].x), 0, sides, true);
            if (capTop >= 0) Cap(capTop, center(rows[rows.Count - 1].x), prevStart, sides, false);
        }

        void Cap(int sub, Vector3 c, int ringStart, int sides, bool bottom)
        {
            if (ringStart == 0 && bottom) ringStart = FirstRingStart;
            int ci = Vert(c, new Vector2(0.5f, bottom ? 0f : 1f));
            for (int i = 0; i < sides; i++)
            {
                int j = (i + 1) % sides;
                if (bottom) Tri(sub, ci, ringStart + j, ringStart + i);
                else Tri(sub, ci, ringStart + i, ringStart + j);
            }
        }

        /// <summary>Tube の最初の行の頂点番号（Tube を呼ぶ前に BeginTube で覚える）。</summary>
        public int FirstRingStart;
        public void BeginTube() { FirstRingStart = _v.Count; }

        /// <summary>
        /// 厚みのある葉身。path(s) が軸、widthAt(s) が片側の幅、fold が V 字の深さ（幅に対する割合）。
        /// 断面は 左ふち・軸・右ふち の 3 点で、上の面と下の面とふちの厚みを閉じる。
        /// </summary>
        public void Blade(Func<float, Vector3> path, Func<float, float> widthAt, int segs, float thick, float fold, int sub)
        {
            var top = new int[segs + 1, 3];
            var bot = new int[segs + 1, 3];
            for (int i = 0; i <= segs; i++)
            {
                float s = i / (float)segs;
                Vector3 c = path(s);
                Vector3 tan = (path(Mathf.Min(1f, s + 0.02f)) - path(Mathf.Max(0f, s - 0.02f))).normalized;
                Vector3 side = Vector3.Cross(Vector3.up, tan);
                if (side.sqrMagnitude < 1e-4f) side = Vector3.right;
                side.Normalize();
                Vector3 up = Vector3.Cross(tan, side).normalized;
                float w = Mathf.Max(0.01f, widthAt(s));
                Vector3 l = c + side * w - up * (w * fold);
                Vector3 r = c - side * w - up * (w * fold);
                Vector3 m = c + up * (thick * 0.6f);
                top[i, 0] = Vert(l, new Vector2(0, s)); top[i, 1] = Vert(m, new Vector2(0.5f, s)); top[i, 2] = Vert(r, new Vector2(1, s));
                bot[i, 0] = Vert(l - up * thick, new Vector2(0, s)); bot[i, 1] = Vert(m - up * thick * 1.6f, new Vector2(0.5f, s)); bot[i, 2] = Vert(r - up * thick, new Vector2(1, s));
            }
            for (int i = 0; i < segs; i++)
            {
                QuadR(sub, top[i, 0], top[i + 1, 0], top[i + 1, 1], top[i, 1]);
                QuadR(sub, top[i, 1], top[i + 1, 1], top[i + 1, 2], top[i, 2]);
                QuadR(sub, bot[i, 1], bot[i + 1, 1], bot[i + 1, 0], bot[i, 0]);
                QuadR(sub, bot[i, 2], bot[i + 1, 2], bot[i + 1, 1], bot[i, 1]);
                QuadR(sub, top[i, 0], bot[i, 0], bot[i + 1, 0], top[i + 1, 0]);
                QuadR(sub, top[i, 2], top[i + 1, 2], bot[i + 1, 2], bot[i, 2]);
            }
            for (int k = 0; k < 2; k++)
            {
                QuadR(sub, top[0, k + 1], bot[0, k + 1], bot[0, k], top[0, k]);
                QuadR(sub, top[segs, k], bot[segs, k], bot[segs, k + 1], top[segs, k + 1]);
            }
        }

        /// <summary>玉（緯度経度の球）。scale で縦横をつぶせる。</summary>
        public void Ball(Vector3 c, Vector3 scale, int rings, int sub)
        {
            int segs = rings * 2;
            int start = _v.Count;
            for (int y = 0; y <= rings; y++)
            {
                float v = y / (float)rings, phi = Mathf.PI * v;
                for (int x = 0; x <= segs; x++)
                {
                    float u = x / (float)segs, th = Mathf.PI * 2f * u;
                    var n = new Vector3(Mathf.Sin(phi) * Mathf.Cos(th), -Mathf.Cos(phi), Mathf.Sin(phi) * Mathf.Sin(th));
                    Vert(c + Vector3.Scale(n, scale), new Vector2(u, v));
                }
            }
            for (int y = 0; y < rings; y++)
                for (int x = 0; x < segs; x++)
                {
                    int a = start + y * (segs + 1) + x, b = a + segs + 1;
                    Quad(sub, a, b, b + 1, a + 1);
                }
        }

        public int TriangleCount { get { int n = 0; foreach (var t in _tris) n += t.Count / 3; return n; } }

        public Mesh ToMesh(string name)
        {
            var m = new Mesh { name = name };
            if (_v.Count > 65000) m.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            m.SetVertices(_v);
            m.SetUVs(0, _uv);
            m.SetColors(_col);
            m.subMeshCount = _tris.Count;
            for (int i = 0; i < _tris.Count; i++) m.SetTriangles(_tris[i], i);
            m.RecalculateNormals();
            m.RecalculateBounds();
            m.RecalculateTangents();
            return m;
        }
    }
}
