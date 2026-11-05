using System;
using System.Collections.Generic;
using UnityEngine;

namespace SlugDesign
{
    /// <summary>
    /// ヤシの形（Nov 3 わくさん「ずんぐりじゃない方のヤシがいい」→ Oct 20 に Blender で作った 3 案のうち A 曲がり・B まっすぐ高い、Blender/Props/palm_gen.py と同じ数字）。
    /// 立っているヤシ（PalmBuilder）と、立ち枯れ・落ちた葉（PalmRemainsBuilder）が同じ形を使う。
    /// ナメクジの目の高さ 0.47 m は人の約 1/3.6。
    /// - A 曲がり：高さ 8 m（人なら 29 m）、太さ 根元 0.22 m → てっぺん 0.15 m、上へ行くほど弓なり（8 m で 1.8 m ずれる）、葉 10 枚・3.8 m
    /// - B まっすぐ高い：高さ 10 m、太さ 0.21 → 0.15 m、少し S 字、葉 9 枚・3.6 m、葉は高く上がる
    /// 幹の節は 0.45 m ごと：ふくらみ（×1.10）から下がってくびれ（×0.90）、その下に暗い溝の帯。根元は大きく広がる。
    /// てっぺんは葉の付け根のふくらんだ鞘。葉は V 字に折れ、ふちに小葉の切れ込みが並ぶ。
    /// </summary>
    public static class PalmShape
    {
        public struct Palm
        {
            public float h, r0, r1, lean, sCurve, flen, fwid, pitch0, pitch1, droop;
            public int fronds, seed;
        }

        public static readonly Palm A = new Palm { h = 8f, r0 = 0.22f, r1 = 0.15f, lean = 0.22f, sCurve = 0f, fronds = 10, flen = 3.8f, fwid = 0.62f, pitch0 = 22f, pitch1 = 45f, droop = 0.55f, seed = 1 };
        public static readonly Palm B = new Palm { h = 10f, r0 = 0.21f, r1 = 0.15f, lean = 0.05f, sCurve = 0.35f, fronds = 9, flen = 3.6f, fwid = 0.58f, pitch0 = 35f, pitch1 = 62f, droop = 0.45f, seed = 2 };

        const float Step = 0.45f;   // 節の間隔（m）

        /// <summary>幹の中心線（t=0 根元、1 てっぺん。t &lt; 0 は地面の下へまっすぐ）。</summary>
        public static Vector3 Center(Palm p, float t)
        {
            if (t < 0f) return Center(p, 0f) + Vector3.up * (p.h * t);
            float x = p.lean * p.h * Mathf.Pow(t, 1.6f) + p.sCurve * 0.6f * Mathf.Sin(t * Mathf.PI * 2f) * (1f - t * 0.5f);
            return new Vector3(x, p.h * t, 0f);
        }

        /// <summary>幹の太さ（節を入れる前）：根元からてっぺんへ細り、根元は大きく広がる。</summary>
        public static float Radius(Palm p, float t)
        {
            t = Mathf.Max(0f, t);
            return Mathf.Lerp(p.r0, p.r1, t) * (1f + 0.9f * Mathf.Exp(-t * p.h / 0.35f));
        }

        static int Nodes(Palm p) => Mathf.Max(6, (int)(p.h / Step));

        /// <summary>節の倍率：ふくらみ 1.10 → 0.2 でくびれ 0.90 → 0.35 まで暗い溝の帯 → 次のふくらみへゆっくり戻る。</summary>
        public static float NodeScale(Palm p, float t)
        {
            if (t <= 0f) return 1f;
            float f = t * Nodes(p) - Mathf.Floor(t * Nodes(p));
            if (f < 0.2f) return Mathf.Lerp(1.10f, 0.90f, f / 0.2f);
            if (f < 0.35f) return 0.90f;
            return Mathf.Lerp(0.90f, 1.10f, (f - 0.35f) / 0.65f);
        }

        /// <summary>
        /// 幹の t0〜t1 の所を作る。節の行を正確な位置に置き、溝の帯だけ ring のマテリアル。topJag で上の端をぎざぎざに折る
        /// （capTop のマテリアルでふた、-1 でなし）。t0 &gt; 0 なら下の端にもふた（capBottom）。
        /// </summary>
        public static void Trunk(NatureMeshKit kit, Palm p, float t0, float t1, int trunk, int ring, int capBottom, int capTop, float topJag, int seed, int sides = 12)
        {
            int n = Nodes(p);
            var ts = new List<float> { t0 };
            var subs = new List<int> { trunk };
            for (int k = Mathf.FloorToInt(Mathf.Max(0f, t0) * n); k <= n; k++)
                foreach (var (frac, isRing) in new[] { (0f, false), (0.2f, false), (0.35f, true) })
                {
                    float t = (k + frac) / n;
                    if (t <= t0 + 1e-4f || t >= t1 - 1e-4f) continue;
                    ts.Add(t); subs.Add(isRing ? ring : trunk);
                }
            ts.Add(t1);
            float fEnd = t1 * n - Mathf.Floor(t1 * n);
            subs.Add(fEnd >= 0.2f && fEnd < 0.35f ? ring : trunk);
            int rows = ts.Count - 1;
            PropKit.Surface(kit, (u, a) =>
            {
                int i = Mathf.Clamp(Mathf.RoundToInt(u * rows), 0, rows);
                float t = ts[i];
                Vector3 c = Center(p, t);
                Vector3 tan = (Center(p, Mathf.Min(1f, t + 0.01f)) - Center(p, t - 0.01f)).normalized;
                Vector3 side = Vector3.Cross(tan, Vector3.forward).normalized;
                Vector3 up = Vector3.Cross(side, tan).normalized;
                float r = Radius(p, t) * NodeScale(p, t);
                if (topJag > 0f && i == rows)
                    c -= tan * (topJag * Mathf.Pow(Mathf.Abs(Mathf.Sin(a * 2.5f + seed)), 2f) * (0.6f + 0.4f * Mathf.Cos(a)));
                if (topJag > 0f && i == rows) r *= 0.85f;
                return c + (side * Mathf.Cos(a) + up * Mathf.Sin(a)) * r;
            }, rows, sides, trunk, capBottom, capTop, topJag > 0f ? -0.05f : 0f, tm => subs[Mathf.Clamp(Mathf.RoundToInt(tm * rows + 0.5f), 1, rows)]);
        }

        /// <summary>てっぺんの葉の付け根：ふくらんだ鞘（下は幹の色、上は鞘の色）。返り値は葉の出る所。</summary>
        public static Vector3 Crown(NatureMeshKit kit, Palm p, int trunk, int sheath, int sides = 12)
        {
            Vector3 top = Center(p, 1f);
            var rows = new List<Vector3>
            {
                new Vector3(0f, p.r1 * NodeScale(p, 1f), trunk),
                new Vector3(0.001f, p.r1 * 1.05f, trunk),
                new Vector3(0.47f, p.r1 * 1.9f, sheath),
                new Vector3(1f, p.r1 * 0.95f, sheath),
            };
            kit.BeginTube();
            kit.Tube(t => top + Vector3.up * (0.75f * t), t => 1f, rows, sides, -1, sheath);
            return top + Vector3.up * 0.45f;
        }

        /// <summary>葉の中心線：付け根 b から水平の向き yaw（度）へ、仰角 pitch（度）で出て、先が droop で垂れる。</summary>
        public static Func<float, Vector3> FrondPath(Vector3 b, float yawDeg, float pitchDeg, float len, float droop)
        {
            var h = PropKit.Dir(yawDeg);
            float pr = pitchDeg * Mathf.Deg2Rad;
            var d0 = h * Mathf.Cos(pr) + Vector3.up * Mathf.Sin(pr);
            return s => b + d0 * (len * s) + Vector3.down * (droop * len * s * s);
        }

        /// <summary>葉の幅（片側）：付け根と先で細く、先へ行くほど少し狭い。segs ごとの 1 つおきに切れ込み（×0.62）。</summary>
        public static float FrondWidth(float s, float width, int segs)
        {
            float w = width * Mathf.Pow(Mathf.Max(0f, Mathf.Sin(Mathf.PI * Mathf.Min(1f, s * 1.05f))), 0.6f) * (1f - 0.35f * s);
            if (Mathf.RoundToInt(s * segs) % 2 == 1) w *= 0.62f;
            return Mathf.Max(w, 0.02f);
        }

        /// <summary>
        /// 葉 1 枚：中心線 path、幅 width、V 字の折れ fold（かもめの翼のように下へ。枯れ葉は深く）。sideDir で幅の向きを決められる
        /// （幹に沿って垂れる葉は、幹の回りの向きに）。
        /// </summary>
        public static void Frond(NatureMeshKit kit, Func<float, Vector3> path, float width, float fold, int sub, int segs = 14, Vector3 sideDir = default, float thick = 0.03f)
        {
            int from = kit.VertexCount;
            kit.Blade(path, s => FrondWidth(s, width, segs), segs, thick, fold, sub, sideDir);
            kit.Paint(from, (q, uv) => new Color(uv.y * 0.35f, uv.y * 0.3f, 0.3f, 1f));
        }

        /// <summary>実：本物の大きさ（直径 約 28 cm）。葉の付け根の下に房が 2 つ、5 個と 4 個。</summary>
        public static void Nuts(NatureMeshKit kit, Palm p, Vector3 crown, float y0, int sub)
        {
            for (int b = 0; b < 2; b++)
            {
                float a0 = y0 + Mathf.PI * b + 0.6f;
                for (int k = 0; k < (b == 0 ? 5 : 4); k++)
                {
                    float a = a0 + (k - 2) * 0.42f, rr = p.r1 * 1.35f + 0.16f + 0.06f * (k % 2);
                    var c = crown + new Vector3(Mathf.Cos(a) * rr, -0.55f - 0.2f * (k % 2), Mathf.Sin(a) * rr);
                    kit.Ball(c, new Vector3(0.14f, 0.16f, 0.14f), 5, sub);
                }
            }
        }
    }
}
