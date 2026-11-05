using System.Collections.Generic;
using UnityEngine;

namespace SlugDesign
{
    /// <summary>
    /// 拠点の灰色の箱：足場と水まわりの手ざわり（Nov 4、わくさん「足場と水まわり」）。色はまだ、形だけ。当たり判定はなし（見た目）。
    /// - 水路が段を落ちる所：細い ②・③ は流木の樋（溝の幅＋6 cm、長さ 0.8 m、下に 2 本の脚）、太い ① は平たい石の落ち口と、下にしぶきの石 3 つ
    ///   落ちる所は水路の線を 5 cm ごとに見て、岩の段の高さが段の真ん中を越える所（段の場所が変わっても付いてくる）
    /// - 池から水路への口 3 つに平たい石のふち。池の南の板の道の池の側（x -1.0〜0.95）と、トンネルの板の道の水路の側に縄の手すり（杭 0.45 m、縄は少したるむ）
    /// - 池の手すりの杭にヤシの実の柄杓。① の流れの中に頭を出す石 4 つ
    /// - 天井の梁の両端：壁に差しこむ所に石のくさびと縄の巻き
    /// 板の床の柱の縄の巻きと、長い床の下の筋かいは BaseGrayboxGround.cs の Planks。
    /// </summary>
    public static partial class BaseGrayboxBuilder
    {
        static void WaterDetails()
        {
            var g = Group("WaterDetails");
            Drops(g);
            PoolLips(g);
            RopeRail(g, new Vector3(-1.0f, UDeck, 2.12f), new Vector3(0.95f, UDeck, 2.12f), 4);
            RopeRail(g, new Vector3(0.78f, TunY + Board, -9.1f), new Vector3(0.78f, TunY + Board, -11.5f), 4);
            Ladle(g, new Vector3(-1.0f, UDeck, 2.12f));   // 手すりの西の端の杭に掛ける
            foreach (var q in new[] { new Vector2(2.9f, -1.3f), new Vector2(2.8f, -2.0f), new Vector2(2.2f, -4.2f), new Vector2(2.05f, -5.4f) })
            {
                float y = TerraceBase(q.x, q.y) - 0.35f * 0.45f;
                Prim(PrimitiveType.Sphere, g, "StreamStone", new Vector3(q.x, y, q.y), new Vector3(0.22f, 0.12f, 0.18f), _rock, q.x * 70f, null, false);
            }
            BeamEnds(g);
        }

        // ───────── 水路が段を落ちる所 ─────────

        static void Drops(Transform g)
        {
            float lowMid = (FloorY + MidY) * 0.5f, midUp = (MidY + UpY) * 0.5f;
            for (int ci = 0; ci < HallChannels.Length; ci++)
            {
                var c = HallChannels[ci];
                var pts = Sample(c.pts, 0.05f);
                for (int i = 0; i < pts.Count - 1; i++)
                {
                    float b0 = TerraceBase(pts[i].x, pts[i].y), b1 = TerraceBase(pts[i + 1].x, pts[i + 1].y);
                    bool cross = (b0 - lowMid) * (b1 - lowMid) < 0f || (b0 - midUp) * (b1 - midUp) < 0f;
                    if (!cross) continue;
                    var dir2 = (pts[i + 1] - pts[i]).normalized;
                    if (b1 > b0) dir2 = -dir2;   // いつも上から下へ
                    var at = pts[i];
                    var hi2 = at - dir2 * 0.4f; var lo2 = at + dir2 * 0.4f;
                    var hi = new Vector3(hi2.x, TerraceBase(hi2.x, hi2.y) - c.depth * 0.45f, hi2.y);
                    var lo = new Vector3(lo2.x, TerraceBase(lo2.x, lo2.y) - c.depth * 0.45f, lo2.y);
                    if (c.hw < 0.3f) Gutter(g, hi + Vector3.up * 0.04f, lo + Vector3.up * 0.04f, c.hw);
                    else Spillway(g, hi, lo, c.hw);
                    i += 10;   // 同じ段で二重に置かない
                }
            }
        }

        static List<Vector2> Sample(Vector2[] line, float step)
        {
            var o = new List<Vector2>();
            for (int i = 0; i < line.Length - 1; i++)
            {
                int n = Mathf.Max(1, Mathf.CeilToInt((line[i + 1] - line[i]).magnitude / step));
                for (int k = 0; k < n; k++) o.Add(Vector2.Lerp(line[i], line[i + 1], k / (float)n));
            }
            o.Add(line[line.Length - 1]);
            return o;
        }

        /// <summary>流木の樋：底と両側の板（U の字）、下の端に 2 本の脚。</summary>
        static void Gutter(Transform g, Vector3 hi, Vector3 lo, float hw)
        {
            var rot = Quaternion.LookRotation(lo - hi);
            var mid = (hi + lo) * 0.5f;
            float len = (lo - hi).magnitude + 0.1f;
            var right = rot * Vector3.right; var up = rot * Vector3.up;
            Prim(PrimitiveType.Cube, g, "Gutter", mid, new Vector3(hw * 2f + 0.06f, 0.03f, len), _wood, 0f, rot.eulerAngles, false);
            foreach (float s in new[] { -1f, 1f })
                Prim(PrimitiveType.Cube, g, "GutterSide", mid + right * s * (hw + 0.03f) + up * 0.05f, new Vector3(0.03f, 0.1f, len), _wood, 0f, rot.eulerAngles, false);
            WaterStrip(g, "GutterWater", new[] { hi + up * 0.03f, lo + up * 0.03f }, hw * 1.6f);
            var foot = lo + (hi - lo).normalized * 0.15f;
            foreach (float s in new[] { -1f, 1f })
                Rod(g, "GutterLeg", foot + right * s * (hw + 0.03f), new Vector3(foot.x, lo.y - 0.3f, foot.z) + right * s * (hw + 0.12f), 0.02f, _wood);
        }

        /// <summary>① の落ち口：両側の平たい石と、底の張り出した石、下にしぶきの石 3 つ。</summary>
        static void Spillway(Transform g, Vector3 hi, Vector3 lo, float hw)
        {
            var flat = lo - hi; flat.y = 0f;
            float yaw = Quaternion.LookRotation(flat).eulerAngles.y;
            var right = Quaternion.Euler(0f, yaw, 0f) * Vector3.right;
            var lip = Vector3.Lerp(hi, lo, 0.35f);
            Prim(PrimitiveType.Cube, g, "SpillLip", new Vector3(lip.x, hi.y - 0.02f, lip.z), new Vector3(hw * 2f + 0.1f, 0.06f, 0.3f), _rock, yaw, null, false);
            foreach (float s in new[] { -1f, 1f })
                Prim(PrimitiveType.Sphere, g, "SpillSideStone", new Vector3(lip.x, hi.y + 0.08f, lip.z) + right * s * (hw + 0.12f), new Vector3(0.3f, 0.2f, 0.36f), _rock, yaw + s * 20f, null, false);
            for (int k = 0; k < 3; k++)
                Prim(PrimitiveType.Sphere, g, "SplashStone", lo + right * ((k - 1) * hw * 0.7f) + flat.normalized * (0.1f + 0.08f * k), new Vector3(0.16f, 0.09f, 0.14f), _rock, k * 60f, null, false);
        }

        // ───────── 池のまわり ─────────

        static void PoolLips(Transform g)
        {
            foreach (var c in HallChannels)
            {
                var a = c.pts[0]; var b = c.pts[1];
                var dir = new Vector3(b.x - a.x, 0f, b.y - a.y).normalized;
                var p = new Vector3(a.x, UpY - 0.08f, a.y);
                float yaw = Quaternion.LookRotation(dir).eulerAngles.y;
                var right = Quaternion.Euler(0f, yaw, 0f) * Vector3.right;
                Prim(PrimitiveType.Cube, g, "PoolLip", p, new Vector3(c.hw * 2f + 0.2f, 0.05f, 0.24f), _rock, yaw, null, false);
                foreach (float s in new[] { -1f, 1f })
                    Prim(PrimitiveType.Sphere, g, "PoolLipStone", p + right * s * (c.hw + 0.16f) + Vector3.up * 0.05f, new Vector3(0.22f, 0.14f, 0.2f), _rock, s * 30f, null, false);
            }
        }

        /// <summary>縄の手すり：a から b へ count 区間。杭 0.45 m、縄は区間の真ん中で 6 cm たるむ。</summary>
        static void RopeRail(Transform g, Vector3 a, Vector3 b, int count)
        {
            const float h = 0.45f;
            for (int i = 0; i <= count; i++)
            {
                var p = Vector3.Lerp(a, b, i / (float)count);
                Rod(g, "RopePost", p + Vector3.down * 0.15f, p + Vector3.up * (h + 0.03f), 0.03f, _wood);
                if (i == count) break;
                var q = Vector3.Lerp(a, b, (i + 1f) / count);
                Vector3 prev = p + Vector3.up * h;
                for (int k = 1; k <= 4; k++)
                {
                    float t = k / 4f;
                    var r = Vector3.Lerp(p, q, t) + Vector3.up * (h - 0.06f * 4f * t * (1f - t));
                    Rod(g, "Rope", prev, r, 0.012f, _wood);
                    prev = r;
                }
            }
        }

        /// <summary>手すりの杭に掛けたヤシの実の柄杓（池の水をすくう。池の側＝北へ下げる）。</summary>
        static void Ladle(Transform g, Vector3 foot)
        {
            var hook = foot + Vector3.up * 0.4f;
            Rod(g, "LadleHook", hook, hook + new Vector3(0f, 0f, 0.08f), 0.01f, _wood);
            var cup = hook + new Vector3(0f, -0.3f, 0.12f);
            Rod(g, "LadleHandle", hook + new Vector3(0f, 0f, 0.08f), cup + Vector3.up * 0.06f, 0.012f, _wood);
            Prim(PrimitiveType.Sphere, g, "LadleCup", cup, new Vector3(0.14f, 0.11f, 0.14f), _wood, 0f, null, false);
        }

        // ───────── 天井の梁の両端 ─────────

        static void BeamEnds(Transform g)
        {
            foreach (var (a, b) in new[] { (230f, 50f), (280f, 100f), (320f, 140f) })
            {
                var pa = Polar(a, BeamR, BeamY); var pb = Polar(b, BeamR, BeamY);
                foreach (var (end, other) in new[] { (pa, pb), (pb, pa) })
                {
                    var dir = (other - end).normalized;
                    Prim(PrimitiveType.Cube, g, "BeamWedge", end + Vector3.down * 0.17f + dir * 0.1f, new Vector3(0.3f, 0.1f, 0.35f), _rock, Quaternion.LookRotation(dir).eulerAngles.y, null, false);
                    Rod(g, "BeamWrap", end + dir * 0.35f, end + dir * 0.45f, 0.15f, _wood);
                }
            }
        }
    }
}
