using System;
using System.Collections.Generic;
using UnityEngine;

namespace SlugDesign
{
    /// <summary>
    /// 精製所の一角（BaseRefineryBuilder の続き）その 1：部屋の殻・丸い石の壁・岩棚・アーチ・木組み（Nov 5 作り直し）。
    /// 参考の絵（夕方の工房）と見比べた 11 項目のうち：
    /// 1 壁を埋める（左に階段と踊り場、2 階の壺の棚、奥に壺の横穴と工房のアーチ、右に滝）、2 天井を低く見せる（手前の太い梁・羽根車・吊った灯・干し草）、
    /// 3 手前をふさぐ（左の大岩・葉。その 2）、4 床を埋める（その 2）、5 階を 3 段に（踊り場 1.2・2 階 2.8・右の 3 階 3.75 で天窓へ）、
    /// 6 壁は大きな丸い石を積む（岩棚の下）、7 木組みはつぎはぎ（幅のちがう板・ずらした継ぎ目・上に打った当て板・継いだ柱と鉄の帯・腕木・ふぞろいな手すり）。
    /// 向き：+Z が奥、-Z がカメラ側、床の上が y 0。ナメクジは体長 1.1 m・目の高さ 0.47 m（人の約 1/3.6）。
    /// 部屋：x -3.4〜3.4、奥の壁 z 3.0、天井 4.6（手前は下がる）。右の x 1.9 から先は石の床（滝の水路）。
    /// </summary>
    public static partial class BaseRefineryBuilder
    {
        const int W = 0, WD = 1, ST = 2, GR = 3, CL = 4, SA = 5, RP = 6, IR = 7, GL = 8, LF = 9, CLOTH = 10,
                  HR = 11, HO = 12, HY = 13, WT = 14, SKY = 15, GZ = 16, DK = 17, SUBS = 18;
        const float X0 = -3.4f, X1 = 3.4f, ZB = 3.0f, ZF = -2.4f, Ceil = 4.6f;
        const float LedgeY0 = 2.05f, LedgeY1 = 2.45f, GalY = 2.8f, GalZ = 2.2f, G3Y = 3.75f, G3X = 2.5f, PlankX1 = 1.9f;
        const float LandY = 1.2f, StairX = -2.82f, StairW = 0.8f;
        const float BeamZ = 0.5f, BeamY = 3.55f, PlatY = 0.88f;

        struct ArchSpec { public Vector3 c; public float w, spring, depth; }
        static readonly ArchSpec JarArch = new ArchSpec { c = new Vector3(-1.55f, 0f, ZB), w = 1.5f, spring = 1.15f, depth = 0.6f };
        static readonly ArchSpec ShopArch = new ArchSpec { c = new Vector3(1.55f, 0f, ZB), w = 2.3f, spring = 0.85f, depth = 1.0f };
        static readonly Rect SkyHole = Rect.MinMaxRect(1.7f, 0.7f, 3.1f, 2.1f);   // 天井の穴（x, z）

        static Quaternion Q(float x, float y, float z) => Quaternion.Euler(x, y, z);
        static float Hs(int s, int i) => PropKit.H(s, i);
        static float Rs(int s, int i, float a, float b) => PropKit.R(s, i, a, b);

        static List<Vector2> P(params float[] xy)
        {
            var l = new List<Vector2>();
            for (int i = 0; i + 1 < xy.Length; i += 2) l.Add(new Vector2(xy[i], xy[i + 1]));
            return l;
        }

        /// <summary>天井の高さ：奥は 4.6、手前ほど下がって 3.7（のぞきこむ洞窟の口）。</summary>
        static float CeilY(float z) => Ceil - 0.9f * Mathf.SmoothStep(0f, 1f, (1.0f - z) / 1.4f);

        static bool InArch(ArchSpec a, Vector3 p, float margin)
        {
            float hw = a.w * 0.5f + margin, dx = p.x - a.c.x;
            return Mathf.Abs(dx) < hw && p.y < a.spring + margin + Mathf.Sqrt(Mathf.Max(0f, hw * hw - dx * dx));
        }

        static bool InFallHole(Vector3 p) => p.y > 2.2f && p.y < 2.75f && p.z > 0.95f && p.z < 1.55f;

        // ───────── 床 ─────────

        /// <summary>床：左は板（幅 0.18〜0.3 m、2〜3 枚に継いで継ぎ目をずらす、ところどころ当て板）、右の x 1.9 から先は石の床と水路。</summary>
        static void Floor()
        {
            Part("Floor", m =>
            {
                Deck(m, X0, PlankX1, ZF, ZB + 0.1f, 0f, 1, true);
                m.RoundBox(WD, new Vector3(0f, -0.16f, 0.3f), new Vector3(7.2f, 0.08f, 6f), Quaternion.identity, 0.01f, 1);
                // 右の石の床（水路の所をあける）
                int k = 0;
                for (float z = ZF; z < ZB; z += 0.55f)
                    for (float x = PlankX1; x < X1; x += 0.5f, k++)
                    {
                        var c = new Vector3(x + 0.25f, -0.07f, z + 0.27f);
                        if (c.x > 2.82f && c.x < 3.28f && c.z < 1.0f) continue;
                        if (c.x > 2.5f && c.z > 0.6f && c.z < 1.95f) continue;
                        m.RoundBox(ST, c + new Vector3(Rs(k, 1, -0.03f, 0.03f), Rs(k, 2, -0.015f, 0.015f), 0f), new Vector3(0.47f, 0.14f, 0.52f), Q(0f, Rs(k, 3, -5f, 5f), 0f), 0.06f, 2, 0.015f, k);
                    }
                m.RoundBox(ST, new Vector3(3.05f, -0.2f, 0f), new Vector3(0.6f, 0.1f, 4.2f), Quaternion.identity, 0.03f, 1);
            }, false, true, true);
        }

        /// <summary>
        /// 板の床：x0〜x1・z0〜z1 の上の面が y。alongZ なら板は z に沿う（でなければ x に沿う）。
        /// 幅はふぞろい、1 列を 2〜3 枚に継いで継ぎ目をずらし、ところどころ上に当て板を打つ（つぎはぎ）。
        /// </summary>
        static void Deck(BaseMesh m, float x0, float x1, float z0, float z1, float y, int seed, bool alongZ)
        {
            float a0 = alongZ ? x0 : z0, a1 = alongZ ? x1 : z1;   // 幅の向き
            float l0 = alongZ ? z0 : x0, l1 = alongZ ? z1 : x1;   // 長さの向き
            float a = a0; int k = 0;
            while (a < a1 - 0.05f)
            {
                float w = Mathf.Min(Rs(seed, k * 7, 0.18f, 0.3f), a1 - a);
                float l = l0; int piece = 0;
                while (l < l1 - 0.05f)
                {
                    float len = Mathf.Min(Rs(seed, k * 13 + piece, 0.9f, 2.4f), l1 - l);
                    if (l1 - (l + len) < 0.4f) len = l1 - l;
                    float ca = a + w * 0.5f, cl = l + len * 0.5f;
                    var c = alongZ ? new Vector3(ca, y - 0.055f, cl) : new Vector3(cl, y - 0.055f, ca);
                    c.y += Rs(seed, k * 17 + piece, -0.01f, 0.012f);
                    var size = alongZ ? new Vector3(w - 0.018f, 0.11f, len - 0.012f) : new Vector3(len - 0.012f, 0.11f, w - 0.018f);
                    m.RoundBox(W, c, size, Q(Rs(seed, k * 19 + piece, -0.5f, 0.5f), Rs(seed, k * 23 + piece, -0.4f, 0.4f), Rs(seed, k * 29 + piece, -0.6f, 0.6f)), 0.035f, 2, 0.006f, seed * 100 + k * 10 + piece);
                    l += len; piece++;
                }
                // 当て板：ときどき、2〜3 列をまたいで横に打つ
                if (Hs(seed, k * 31) < 0.12f && a + 0.6f < a1)
                {
                    float at = Rs(seed, k * 37, l0 + 0.3f, l1 - 0.3f);
                    var c = alongZ ? new Vector3(a + 0.3f, y + 0.02f, at) : new Vector3(at, y + 0.02f, a + 0.3f);
                    var size = alongZ ? new Vector3(0.62f, 0.05f, 0.16f) : new Vector3(0.16f, 0.05f, 0.62f);
                    m.RoundBox(WD, c, size, Q(0f, Rs(seed, k * 41, -6f, 6f), 0f), 0.02f, 2, 0.004f, seed + k);
                }
                a += w; k++;
            }
        }

        // ───────── 部屋の殻 ─────────

        /// <summary>岩の面：格子 P(u, v) を盛り上げて、skip の所を抜く。inward(p) は部屋の中を向く向き（法線は保存のときに計算し直す）。</summary>
        static void RockSheet(BaseMesh m, Func<float, float, Vector3> P, Func<Vector3, Vector3> inward, int nu, int nv, Func<Vector3, bool> skip, int seed, int mat = DK)
        {
            var idx = new int[(nu + 1) * (nv + 1)];
            for (int j = 0; j <= nv; j++)
                for (int i = 0; i <= nu; i++)
                {
                    var p = P(i / (float)nu, j / (float)nv);
                    var inn = inward(p);
                    float bump = 0.2f * PropKit.Fbm(p * 0.9f, seed) + 0.05f * PropKit.Noise(p * 3.1f, seed + 1);
                    p += inn * bump;
                    var uv = Mathf.Abs(inn.y) > 0.5f ? new Vector2(p.x, p.z) : Mathf.Abs(inn.x) > 0.5f ? new Vector2(p.z, p.y) : new Vector2(p.x, p.y);
                    idx[j * (nu + 1) + i] = m.Add(p, inn, uv);
                }
            for (int j = 0; j < nv; j++)
                for (int i = 0; i < nu; i++)
                {
                    int a = idx[j * (nu + 1) + i], b = a + 1, c = a + nu + 2, d = a + nu + 1;
                    if (skip != null && skip(P((i + 0.5f) / nu, (j + 0.5f) / nv))) continue;
                    m.Quad(mat, a, b, c, d);
                }
        }

        static void Shell()
        {
            Part("Shell", m =>
            {
                RockSheet(m, (u, v) => new Vector3(Mathf.Lerp(X0 - 0.3f, X1 + 0.3f, u), Mathf.Lerp(-0.2f, Ceil + 0.1f, v), ZB + 0.15f), _ => Vector3.back, 40, 28,
                          p => InArch(JarArch, p, -0.02f) || InArch(ShopArch, p, -0.02f), 3);
                RockSheet(m, (u, v) => new Vector3(X0 - 0.12f, Mathf.Lerp(-0.2f, Ceil + 0.1f, v), Mathf.Lerp(ZF, ZB + 0.3f, u)), _ => Vector3.right, 30, 28, null, 4);
                RockSheet(m, (u, v) => new Vector3(X1 + 0.12f, Mathf.Lerp(-0.2f, Ceil + 0.1f, v), Mathf.Lerp(ZF, ZB + 0.3f, u)), _ => Vector3.left, 30, 28, InFallHole, 5);
                RockSheet(m, (u, v) => { float z = Mathf.Lerp(ZF, ZB + 0.3f, v); return new Vector3(Mathf.Lerp(X0 - 0.3f, X1 + 0.3f, u), CeilY(z), z); }, _ => Vector3.down, 40, 32,
                          p => SkyHole.Contains(new Vector2(p.x, p.z)), 6);
                // 天窓の縦穴（短い筒）と、その上の空
                RockSheet(m, (u, v) =>
                {
                    float t = u * 4f; int side = Mathf.Min(3, (int)t); float f = t - side;
                    var c = new[] { new Vector2(SkyHole.xMin, SkyHole.yMin), new Vector2(SkyHole.xMax, SkyHole.yMin), new Vector2(SkyHole.xMax, SkyHole.yMax), new Vector2(SkyHole.xMin, SkyHole.yMax) };
                    var q = Vector2.Lerp(c[side], c[(side + 1) % 4], f);
                    return new Vector3(q.x, Mathf.Lerp(Ceil - 0.1f, Ceil + 0.9f, v), q.y);
                }, p => (new Vector3(SkyHole.center.x, p.y, SkyHole.center.y) - p).normalized, 24, 4, null, 7);
                ArchInterior(m, JarArch, 8);
                ArchInterior(m, ShopArch, 9);
                // 滝の出る穴（右の壁の奥の小さな洞）
                RockSheet(m, (u, v) => new Vector3(X1 + 0.5f, Mathf.Lerp(2.1f, 2.85f, v), Mathf.Lerp(0.85f, 1.65f, u)), _ => Vector3.left, 6, 4, null, 10);
            }, true);
            // 空（天窓の上。光る色の板）
            Part("Sky", m =>
            {
                int a = m.Add(new Vector3(SkyHole.xMin - 1f, Ceil + 0.9f, SkyHole.yMin - 1f), Vector3.down, Vector2.zero);
                int b = m.Add(new Vector3(SkyHole.xMax + 1f, Ceil + 0.9f, SkyHole.yMin - 1f), Vector3.down, Vector2.zero);
                int c = m.Add(new Vector3(SkyHole.xMax + 1f, Ceil + 0.9f, SkyHole.yMax + 1f), Vector3.down, Vector2.zero);
                int d = m.Add(new Vector3(SkyHole.xMin - 1f, Ceil + 0.9f, SkyHole.yMax + 1f), Vector3.down, Vector2.zero);
                m.Quad(SKY, a, b, c, d);
            }, false, false);
        }

        /// <summary>アーチの横穴の中：上が丸い筒、奥の壁、石の床。</summary>
        static void ArchInterior(BaseMesh m, ArchSpec a, int seed)
        {
            float hw = a.w * 0.5f;
            Func<float, Vector3> arch = t =>
            {
                if (t < 0.2f) return new Vector3(hw, Mathf.Lerp(0f, a.spring, t / 0.2f), 0f);
                if (t > 0.8f) return new Vector3(-hw, Mathf.Lerp(a.spring, 0f, (t - 0.8f) / 0.2f), 0f);
                float b = Mathf.PI * (t - 0.2f) / 0.6f;
                return new Vector3(hw * Mathf.Cos(b), a.spring + hw * Mathf.Sin(b), 0f);
            };
            RockSheet(m, (u, v) => a.c + arch(u) + Vector3.forward * (v * (a.depth + 0.1f) - 0.05f),
                      p => (new Vector3(a.c.x, Mathf.Min(p.y, a.spring), p.z) - p).normalized, 30, 6, null, seed);
            RockSheet(m, (u, v) => a.c + new Vector3(Mathf.Lerp(-hw, hw, u), Mathf.Lerp(-0.05f, a.spring + hw, v), a.depth + 0.05f), _ => Vector3.back, 16, 16, null, seed + 1);
        }

        // ───────── 丸い石の壁・アーチの石・岩棚 ─────────

        static void Stones()
        {
            Part("Stones", m =>
            {
                Func<Vector3, bool> backSkip = p => InArch(JarArch, p, 0.32f) || InArch(ShopArch, p, 0.32f);
                StoneWall(m, new Vector3(X0, 0f, ZB), Vector3.right, Vector3.back, X1 - X0, 0f, LedgeY0, 11, backSkip);
                StoneWall(m, new Vector3(X0, 0f, ZB - 0.1f), Vector3.back, Vector3.right, 3.2f, 0f, 2.7f, 12, null);
                StoneWall(m, new Vector3(X1, 0f, -1.4f), Vector3.forward, Vector3.left, ZB + 1.4f, 0f, 2.2f, 13, p => InFallHole(p) || (p.z > 0.75f && p.z < 1.85f && (p.y < 1.25f || (p.z > 0.85f && p.z < 1.65f))));   // 落ち水の通り道はあける
                ArchStones(m, JarArch, 21);
                ArchStones(m, ShopArch, 22);
                // 岩棚：奥の壁ぞいに大きな平たい石を並べる（2 階の床を受ける。上と前につたがかぶさる）
                float x = -2.5f; int k = 0;
                while (x < X1 - 0.1f)
                {
                    float len = Mathf.Min(Rs(31, k, 1.1f, 1.7f), X1 - x);
                    var c = new Vector3(x + len * 0.5f, (LedgeY0 + LedgeY1) * 0.5f + Rs(32, k, -0.03f, 0.03f), ZB - 0.33f + Rs(33, k, -0.04f, 0.04f));
                    m.RoundBox(ST, c, new Vector3(len + 0.12f, LedgeY1 - LedgeY0 + Rs(34, k, 0f, 0.08f), 0.78f), Q(Rs(35, k, -2f, 2f), Rs(36, k, -3f, 3f), Rs(37, k, -2f, 2f)), 0.17f, 3, 0.05f, 300 + k);
                    x += len; k++;
                }
            }, false, true, true);
        }

        /// <summary>
        /// 丸い石を積んだ壁：o から right の向きへ len、高さ y0〜y1。outN は部屋の中の向き。
        /// 段の高さ 0.32〜0.5 m、石の長さ 0.45〜0.85 m、段ごとに継ぎ目をずらす。角を大きく丸め（短い辺の 4 割）、少し出入りさせる。
        /// </summary>
        static void StoneWall(BaseMesh m, Vector3 o, Vector3 right, Vector3 outN, float len, float y0, float y1, int seed, Func<Vector3, bool> skip)
        {
            float y = y0; int row = 0;
            var rot0 = Quaternion.LookRotation(-outN, Vector3.up);
            while (y < y1 - 0.12f)
            {
                float h = Mathf.Min(Rs(seed, row * 31, 0.32f, 0.5f), y1 - y);
                float s = -Rs(seed, row * 31 + 1, 0f, 0.4f);
                int k = 0;
                while (s < len)
                {
                    float l = Rs(seed, row * 131 + k, 0.45f, 0.85f);
                    float a = Mathf.Max(0f, s), b = Mathf.Min(len, s + l);
                    if (b - a > 0.15f)
                    {
                        var c = o + right * ((a + b) * 0.5f) + Vector3.up * (y + h * 0.5f) + outN * (0.1f + Rs(seed, row * 57 + k, -0.05f, 0.08f));
                        if (skip == null || !skip(c))
                        {
                            var rot = rot0 * Q(Rs(seed, row * 61 + k, -3f, 3f), Rs(seed, row * 67 + k, -4f, 4f), Rs(seed, row * 71 + k, -4f, 4f));
                            float sx = b - a - 0.07f, sy = h - 0.06f;
                            m.RoundBox(ST, c, new Vector3(sx, sy, 0.36f), rot, Mathf.Min(sx, sy) * 0.26f, 3, 0.035f, seed * 1000 + row * 50 + k);
                        }
                    }
                    s += l; k++;
                }
                y += h; row++;
            }
        }

        /// <summary>アーチのふちの石：両側の柱石と、半円の輪石。</summary>
        static void ArchStones(BaseMesh m, ArchSpec a, int seed)
        {
            float hw = a.w * 0.5f, r = hw + 0.18f;
            int n = Mathf.Max(7, Mathf.RoundToInt(Mathf.PI * r / 0.38f));
            for (int i = 0; i < n; i++)
            {
                float b = Mathf.PI * (i + 0.5f) / n;
                var c = a.c + new Vector3(Mathf.Cos(b) * r, a.spring + Mathf.Sin(b) * r, -0.12f);
                m.RoundBox(ST, c, new Vector3(0.34f, Mathf.PI * r / n - 0.03f, 0.4f), Q(0f, 0f, b * Mathf.Rad2Deg), 0.1f, 3, 0.02f, seed * 100 + i);
            }
            for (int s = -1; s <= 1; s += 2)
            {
                float y = 0f; int i = 0;
                while (y < a.spring - 0.05f)
                {
                    float h = Mathf.Min(Rs(seed, i + s * 10, 0.36f, 0.5f), a.spring - y);
                    m.RoundBox(ST, a.c + new Vector3(s * (hw + 0.18f), y + h * 0.5f, -0.12f), new Vector3(0.36f + Rs(seed, i + 40, 0f, 0.1f), h - 0.03f, 0.4f),
                               Q(0f, 0f, Rs(seed, i + s * 20, -3f, 3f)), 0.11f, 3, 0.02f, seed * 100 + 50 + i + s * 7);
                    y += h; i++;
                }
            }
        }

        // ───────── 木組み（つぎはぎ） ─────────

        /// <summary>
        /// 角柱：足元 b から高さ h、太さ s。高い柱は途中で継ぎ、継ぎ目に鉄の帯と添え木を当てる。上の端の前後に腕木。
        /// </summary>
        static void Post(BaseMesh m, Vector3 b, float h, float s, int seed, bool brackets = true)
        {
            float split = h > 2.2f ? Rs(seed, 1, 0.4f, 0.6f) * h : h;
            m.RoundBox(WD, b + Vector3.up * (split * 0.5f), new Vector3(s, split, s), Q(0f, Rs(seed, 2, -3f, 3f), Rs(seed, 3, -0.8f, 0.8f)), s * 0.22f, 2, 0.01f, seed);
            if (split < h)
            {
                m.RoundBox(WD, b + Vector3.up * ((split + h) * 0.5f), new Vector3(s * 0.94f, h - split, s * 0.94f), Q(0f, Rs(seed, 4, -3f, 3f), Rs(seed, 5, -0.8f, 0.8f)), s * 0.22f, 2, 0.01f, seed + 1);
                m.RoundBox(W, b + Vector3.up * split + Vector3.right * (s * 0.55f), new Vector3(0.06f, 0.6f, s * 0.8f), Quaternion.identity, 0.02f, 2, 0.004f, seed + 2);
                foreach (float dy in new[] { -0.18f, 0.18f })
                    m.RoundBox(IR, b + Vector3.up * (split + dy), new Vector3(s + 0.03f, 0.05f, s + 0.03f), Quaternion.identity, 0.015f, 2);
            }
            m.RoundBox(IR, b + Vector3.up * 0.25f, new Vector3(s + 0.03f, 0.05f, s + 0.03f), Quaternion.identity, 0.015f, 2);
            if (!brackets) return;
            foreach (float d in new[] { -1f, 1f })
            {
                var a = b + new Vector3(d * s * 0.5f, h - 0.55f, 0f); var e = b + new Vector3(d * (s * 0.5f + 0.4f), h - 0.06f, 0f);
                m.RoundBox(WD, (a + e) * 0.5f, new Vector3((e - a).magnitude, 0.12f, s * 0.7f), Quaternion.FromToRotation(Vector3.right, e - a), 0.035f, 2, 0.004f, seed + 5);
            }
        }

        /// <summary>梁：a から b、断面 w×h。両端は少し突き出し、木栓の頭を 2 つずつ。</summary>
        static void Beam(BaseMesh m, Vector3 a, Vector3 b, float w, float h, int seed)
        {
            var dir = (b - a).normalized;
            var rot = Quaternion.LookRotation(dir, Mathf.Abs(dir.y) > 0.9f ? Vector3.forward : Vector3.up);
            m.RoundBox(WD, (a + b) * 0.5f, new Vector3(w, h, (b - a).magnitude + 0.2f), rot * Q(0f, 0f, Rs(seed, 1, -1f, 1f)), Mathf.Min(w, h) * 0.2f, 2, 0.012f, seed);
            foreach (var e in new[] { a, b })
                foreach (float d in new[] { -0.08f, 0.08f })
                    m.Lathe(WD, e + rot * new Vector3(w * 0.5f, d, 0f), rot * Q(0f, 0f, -90f), P(0f, 0f, 0.03f, 0f, 0.032f, 0.02f, 0.025f, 0.03f, 0f, 0.03f), 8);
        }

        /// <summary>
        /// 手すり：a から b の床の上（y は a, b の y）。柱は 0.5〜0.8 m のふぞろいな間、頭に笠木。上の横木は柱ごとに継いで少しずつずれる。ときどき斜めの筋かい。
        /// </summary>
        static void Railing(BaseMesh m, Vector3 a, Vector3 b, float height, int seed)
        {
            float len = (b - a).magnitude;
            var dir = (b - a) / len;
            var flat = new Vector3(dir.x, 0f, dir.z).normalized;
            float yaw = Mathf.Atan2(flat.x, flat.z) * Mathf.Rad2Deg;
            var posts = new List<float> { 0f };
            float t = 0f; int k = 0;
            while (t < len - 0.3f) { t += Rs(seed, k++, 0.5f, 0.8f); posts.Add(Mathf.Min(t, len)); }
            if (posts[posts.Count - 1] < len - 0.01f) posts.Add(len);
            for (int i = 0; i < posts.Count; i++)
            {
                var p = a + dir * posts[i];
                float hh = height + Rs(seed, 100 + i, -0.03f, 0.04f);
                m.RoundBox(W, p + Vector3.up * (hh * 0.5f), new Vector3(0.12f, hh, 0.12f), Q(Rs(seed, 200 + i, -2f, 2f), yaw, Rs(seed, 300 + i, -2f, 2f)), 0.035f, 2, 0.004f, seed + i);
                m.RoundBox(W, p + Vector3.up * (hh + 0.03f), new Vector3(0.17f, 0.06f, 0.17f), Q(0f, yaw + Rs(seed, 400 + i, -6f, 6f), 0f), 0.025f, 2);
                if (i == 0) continue;
                var q = a + dir * posts[i - 1];
                float y1 = height - 0.06f + Rs(seed, 500 + i, -0.02f, 0.02f), y2 = height * 0.45f;
                var r1a = q + Vector3.up * y1; var r1b = p + Vector3.up * (y1 + Rs(seed, 600 + i, -0.02f, 0.02f));
                m.RoundBox(W, (r1a + r1b) * 0.5f, new Vector3(0.13f, 0.1f, (r1b - r1a).magnitude + 0.08f), Quaternion.LookRotation(r1b - r1a), 0.035f, 2, 0.004f, seed + 50 + i);
                var r2a = q + Vector3.up * y2; var r2b = p + Vector3.up * y2;
                m.RoundBox(W, (r2a + r2b) * 0.5f, new Vector3(0.08f, 0.08f, (r2b - r2a).magnitude), Quaternion.LookRotation(r2b - r2a), 0.025f, 2);
                if (Hs(seed, 700 + i) < 0.3f)
                {
                    var d1 = q + Vector3.up * 0.08f; var d2 = p + Vector3.up * (y2 - 0.04f);
                    m.RoundBox(W, (d1 + d2) * 0.5f, new Vector3(0.07f, 0.07f, (d2 - d1).magnitude), Quaternion.LookRotation(d2 - d1), 0.02f, 2);
                }
            }
        }

        /// <summary>階段：足元 start（中心線）から dir へ count 段（1 段 rise・run）、幅 width。けた 2 本、開いた側（openSide）に手すり。</summary>
        static void Stairs(BaseMesh m, Vector3 start, Vector3 dir, float width, int count, float rise, float run, Vector3 openSide, int seed)
        {
            var side = Vector3.Cross(Vector3.up, dir).normalized;
            var rot = Quaternion.LookRotation(dir);
            for (int i = 0; i < count; i++)
            {
                var c = start + dir * (run * (i + 0.5f)) + Vector3.up * (rise * (i + 1) - 0.04f);
                m.RoundBox(W, c, new Vector3(width + Rs(seed, i, -0.04f, 0.04f), 0.08f, run + 0.05f), rot * Q(0f, Rs(seed, i + 30, -2f, 2f), Rs(seed, i + 60, -1f, 1f)), 0.03f, 2, 0.004f, seed + i);
            }
            var end = start + dir * (run * count) + Vector3.up * (rise * count);
            foreach (float s in new[] { -0.5f, 0.5f })
            {
                var a = start + side * (s * (width + 0.06f)) - Vector3.up * 0.05f; var b = end + side * (s * (width + 0.06f)) - Vector3.up * 0.12f;
                m.RoundBox(WD, (a + b) * 0.5f, new Vector3(0.09f, 0.3f, (b - a).magnitude + 0.1f), Quaternion.LookRotation(b - a), 0.035f, 2, 0.008f, seed + 90);
            }
            var rs = openSide.normalized * (width * 0.5f + 0.08f);
            for (int i = 0; i <= count; i += 3)
            {
                var p = start + dir * (run * i) + Vector3.up * (rise * i) + rs;
                m.RoundBox(W, p + Vector3.up * 0.4f, new Vector3(0.1f, 0.8f, 0.1f), rot, 0.03f, 2);
            }
            var ra = start + rs + Vector3.up * 0.78f; var rb = end + rs + Vector3.up * 0.78f;
            m.RoundBox(W, (ra + rb) * 0.5f, new Vector3(0.11f, 0.09f, (rb - ra).magnitude + 0.1f), Quaternion.LookRotation(rb - ra), 0.03f, 2, 0.004f, seed + 95);
        }

        static void Wood()
        {
            Part("Wood", m =>
            {
                // 左：階段 2 本と踊り場（y 1.2）
                Stairs(m, new Vector3(StairX, 0f, -1.4f), Vector3.forward, StairW, 8, 0.15f, 0.175f, Vector3.right, 401);
                Deck(m, X0, StairX + StairW * 0.5f + 0.05f, 0f, 0.8f, LandY, 402, false);
                foreach (float z in new[] { 0.05f, 0.75f }) Post(m, new Vector3(StairX + 0.45f, 0f, z), LandY - 0.11f, 0.18f, 403 + (int)(z * 10), false);
                Beam(m, new Vector3(X0, LandY - 0.2f, 0.78f), new Vector3(StairX + 0.5f, LandY - 0.2f, 0.78f), 0.16f, 0.18f, 405);
                Stairs(m, new Vector3(StairX, LandY, 0.8f), Vector3.forward, StairW, 10, 0.16f, 0.14f, Vector3.right, 406);

                // 2 階（y 2.8）：奥の岩棚の上。根太の木片で受け、前のふちに手すり
                Deck(m, X0, X1, GalZ, ZB + 0.05f, GalY, 410, false);
                for (float x = X0 + 0.3f; x < X1; x += 0.7f)
                    m.RoundBox(WD, new Vector3(x, (LedgeY1 + GalY - 0.11f) * 0.5f, 2.62f), new Vector3(0.18f, GalY - 0.11f - LedgeY1 + 0.02f, 0.85f), Q(0f, Rs(411, (int)(x * 10), -3f, 3f), 0f), 0.04f, 2, 0.006f, 411);
                Beam(m, new Vector3(X0, GalY - 0.2f, GalZ + 0.08f), new Vector3(X1, GalY - 0.2f, GalZ + 0.08f), 0.2f, 0.22f, 412);
                Railing(m, new Vector3(StairX + 0.5f, GalY, GalZ + 0.07f), new Vector3(G3X - 0.08f, GalY, GalZ + 0.07f), 0.62f, 413);

                // 手前の太い梁（y 3.55、z 0.5。天井を低く見せる）と、それを受ける右の柱・左の腕木
                Beam(m, new Vector3(X0, BeamY, BeamZ), new Vector3(X1, BeamY, BeamZ), 0.34f, 0.4f, 420);
                Post(m, new Vector3(X1 - 0.1f, 0f, BeamZ), BeamY - 0.2f, 0.26f, 421);
                var wa = new Vector3(X0, BeamY - 0.85f, BeamZ); var wb = new Vector3(X0 + 0.7f, BeamY - 0.2f, BeamZ);
                m.RoundBox(WD, (wa + wb) * 0.5f, new Vector3(0.16f, 0.16f, (wb - wa).magnitude), Quaternion.LookRotation(wb - wa), 0.04f, 2, 0.006f, 422);
                // 天井の梁（z 1.6）と、羽根車の柱からの筋かい
                Beam(m, new Vector3(X0, 4.43f, 1.6f), new Vector3(X1, 4.43f, 1.6f), 0.28f, 0.3f, 425);
                foreach (float d in new[] { -1f, 1f })
                {
                    var a = new Vector3(0.2f + d * 0.15f, 3.35f, 1.95f); var b = new Vector3(0.2f + d * 1.05f, 4.3f, 1.6f);
                    m.RoundBox(WD, (a + b) * 0.5f, new Vector3(0.14f, 0.14f, (b - a).magnitude), Quaternion.LookRotation(b - a), 0.035f, 2, 0.006f, 426);
                }

                // 右の 3 階（y 3.75、天窓へ）：右の壁ぞい。梁の柱と、奥の柱・壁の腕木で受ける
                Deck(m, G3X, X1, -0.3f, ZB + 0.05f, G3Y, 430, true);
                Beam(m, new Vector3(G3X + 0.1f, G3Y - 0.2f, -0.3f), new Vector3(G3X + 0.1f, G3Y - 0.2f, ZB), 0.2f, 0.22f, 431);
                Post(m, new Vector3(G3X + 0.1f, 0f, 2.25f), GalY - 0.11f, 0.24f, 432, false);
                Post(m, new Vector3(G3X + 0.1f, GalY, 2.6f), G3Y - GalY - 0.11f, 0.2f, 433);
                Railing(m, new Vector3(G3X + 0.06f, G3Y, -0.25f), new Vector3(G3X + 0.06f, G3Y, 2.15f), 0.48f, 434);
                // 2 階 → 3 階のはしご
                var lf = new Vector3(3.0f, GalY, 2.75f); var lt = new Vector3(3.0f, G3Y + 0.5f, 2.3f);
                foreach (float s in new[] { -0.24f, 0.24f }) m.Tube(W, q => Vector3.Lerp(lf, lt, q) + Vector3.right * s, _ => 0.05f, 3, 10);
                for (int i = 1; i <= 5; i++) { var p = Vector3.Lerp(lf, lt, i / 6.5f); m.Stick(W, p + Vector3.left * 0.26f, p + Vector3.right * 0.26f, 0.035f, 10); }
            }, false, true, true);
        }
    }
}
