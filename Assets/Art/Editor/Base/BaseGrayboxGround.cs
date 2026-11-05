using System.Collections.Generic;
using UnityEngine;

namespace SlugDesign
{
    /// <summary>
    /// 拠点の灰色の箱の地面（Nov 3、docs/discussions/base_layout_v2.md）：水が削った岩の段々と、ナメクジが渡した板の足場。
    /// - 岩の床：北（池）が高く南（トンネル）が低い 3 段。下段 y 2.4・中段 3.0・上段 3.6、段のふちは丸い急な崖（よじ登れない 0.6 m）
    ///   表面のこぶ ±0.17 m、くぼみに水たまり。岩の上も歩ける（当たり判定あり）
    /// - 水路 3 本（どれも岩を削った溝）：① 本流 幅 0.8・深さ 0.35（池 → 東の段 → 段の横の滝 → トンネル → 水車）、
    ///   ② 染め水 幅 0.35（池の東 → 染め場の 3 つの浅い池 → 中段で ① へ）、③ 苗床の水 幅 0.35（池の西 → 苗床 → 地図の台の板の下 → 下段で ① へ）
    /// - 板の足場：板の上は岩より 0.25 m 高い平ら（下段 2.65・中段 3.25・上段 3.85）。板のすき間から下の岩と水が見える
    ///   段と段は板の階段（1 段 0.15 m）。当たり判定は見えない坂（階段の見た目と別）
    /// </summary>
    public static partial class BaseGrayboxBuilder
    {
        const float MidY = 3.0f, UpY = 3.6f, Board = 0.25f;
        static float LDeck => FloorY + Board;
        static float MDeck => MidY + Board;
        static float UDeck => UpY + Board;

        static float ZB1(float x) => -2.4f + 0.35f * Mathf.Sin(1.1f * x + 0.3f);   // 下段と中段のさかい
        static float ZB2(float x) => 0.9f + 0.35f * Mathf.Sin(0.8f * x + 1.7f);    // 中段と上段のさかい

        static float StepUp(float d) => Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(d / 0.36f + 0.5f));

        /// <summary>こぶを除いた岩の段の高さ。</summary>
        static float TerraceBase(float x, float z) =>
            FloorY + (MidY - FloorY) * StepUp(z - ZB1(x)) + (UpY - MidY) * StepUp(z - ZB2(x));

        struct Channel { public Vector2[] pts; public float hw, depth; }

        static readonly Channel[] HallChannels =
        {
            // ① 本流：池の南東 → 上段 → 中段（② が合流）→ 下段 → 段の横の滝へ
            new Channel { hw = 0.4f, depth = 0.35f, pts = new[] {
                new Vector2(1.45f, 1.73f), new Vector2(2.0f, 1.1f), new Vector2(2.15f, 0.95f), new Vector2(2.35f, 0.55f), new Vector2(2.9f, -1.0f),
                new Vector2(2.9f, -1.7f), new Vector2(2.85f, -2.3f), new Vector2(2.75f, -2.75f), new Vector2(2.15f, -4.5f), new Vector2(1.95f, -6.25f) } },
            // ② 染め水：池の東 → 染め場の 3 つの浅い池 → 中段へ落ちて ① へ
            new Channel { hw = 0.17f, depth = 0.22f, pts = new[] {
                new Vector2(2.2f, 3.78f), new Vector2(3.0f, 3.0f), new Vector2(3.4f, 2.6f), new Vector2(3.8f, 2.0f), new Vector2(4.1f, 1.4f),
                new Vector2(4.25f, 0.8f), new Vector2(4.2f, 0.35f), new Vector2(3.7f, -0.6f), new Vector2(2.95f, -1.4f) } },
            // ③ 苗床の水：池の西 → 苗床の植え箱の2列の間をまっすぐ → 西で中段へ落ちて地図の台の下 → 下段へ落ちて ① へ
            new Channel { hw = 0.14f, depth = 0.22f, pts = new[] {
                new Vector2(-1.0f, 2.75f), new Vector2(-3.35f, 2.75f), new Vector2(-3.4f, 1.9f), new Vector2(-3.3f, 0.85f), new Vector2(-3.2f, 0.35f),
                new Vector2(-2.4f, -0.4f), new Vector2(-1.2f, -1.6f), new Vector2(-0.6f, -2.4f), new Vector2(-0.45f, -2.75f), new Vector2(0.3f, -3.9f),
                new Vector2(1.2f, -4.9f), new Vector2(1.85f, -5.4f) } },
        };
        static readonly Vector2[] DyeBasins = { new Vector2(3.4f, 2.6f), new Vector2(3.8f, 2.0f), new Vector2(4.1f, 1.4f) };
        static readonly Vector3[] Puddles = { new Vector3(-3.0f, -1.6f, 0.5f), new Vector3(-2.6f, -5.0f, 0.45f), new Vector3(4.0f, -3.2f, 0.45f), new Vector3(-4.4f, 3.4f, 0.45f), new Vector3(1.0f, 5.2f, 0.4f) };
        const float PoolR = 1.75f;
        const float MapR = 2.3f;   // 地図の台の円い板の台座（直径 4.6 m、人なら 17 m。Nov 3「地図のまわりを広く」）
        static readonly Rect NurseryBed = Rect.MinMaxRect(-3.45f, 2.0f, -1.2f, 3.55f);   // 苗床の平らに削った台（上段の西）

        /// <summary>
        /// 洞窟を凹ませて場所を作る所（Nov 3）：ここでは岩をその高さより下まで削る。
        /// 地図の台の台座・中段から上段への階段・池の南の板の道・細工場の前（中段の高さまで）・苗床の台（上段の高さで平ら）。
        /// </summary>
        static float ClearZones(float x, float z, float h)
        {
            var p = new Vector2(x, z);
            if ((p - new Vector2(MapAt.x, MapAt.z)).magnitude < MapR + 0.15f) h = Mathf.Min(h, MDeck - 0.15f);
            if (x > -2.8f && x < -1.6f && z > 0.35f && z < 1.9f) h = Mathf.Min(h, MDeck - 0.15f);
            if (x > -2.75f && x < 1.0f && z > 1.4f && z < 2.2f) h = Mathf.Min(h, UDeck - 0.15f);
            float a = Mathf.Atan2(z, x) * Mathf.Rad2Deg; if (a < 0f) a += 360f;
            if (a > 156f && a < 192f && p.magnitude > 3.45f) h = Mathf.Min(h, MDeck - 0.15f);
            if (NurseryBed.Contains(p)) h = Mathf.Min(h, UpY);
            return h;
        }

        static float DistToLine(Vector2 p, Vector2[] pts)
        {
            float best = float.MaxValue;
            for (int i = 0; i < pts.Length - 1; i++)
            {
                var a = pts[i]; var ab = pts[i + 1] - a;
                float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude);
                best = Mathf.Min(best, (a + ab * t - p).magnitude);
            }
            return best;
        }

        static float Carve(float h, float baseY, float d, float hw, float depth)
        {
            if (d > hw + 0.2f) return h;
            float k = Mathf.Clamp01(d / hw);
            float inner = d < hw ? baseY - depth * (1f - k * k) : baseY;
            return Mathf.Min(h, Mathf.Lerp(inner, h, Mathf.Clamp01((d - hw) / 0.2f)));
        }

        /// <summary>広間の岩の床の高さ（こぶ・水路の溝・池・染め場の池・水たまり込み）。</summary>
        static float HallGround(float x, float z)
        {
            float b = TerraceBase(x, z);
            float h = b + 0.12f * PropKit.Fbm(new Vector3(x * 0.7f, 0f, z * 0.7f), 21) + 0.05f * PropKit.Noise(new Vector3(x * 2.4f, 1f, z * 2.4f), 22);
            var p = new Vector2(x, z);
            foreach (var c in HallChannels) h = Carve(h, b, DistToLine(p, c.pts), c.hw, c.depth);
            float rp = (p - SkyC).magnitude;
            if (rp < PoolR + 0.25f) h = Mathf.Min(h, Mathf.Lerp(UpY - 0.55f * (1f - Mathf.Pow(Mathf.Clamp01(rp / PoolR), 2f)), h, Mathf.Clamp01((rp - PoolR) / 0.25f)));
            foreach (var d in DyeBasins) h = Carve(h, b, (p - d).magnitude, 0.45f, 0.2f);
            foreach (var w in Puddles) h = Carve(h, b, (p - new Vector2(w.x, w.y)).magnitude, w.z, 0.1f);
            return ClearZones(x, z, h);
        }

        /// <summary>トンネルの岩の床（東の水路と、出口の手前で西へ横切る溝、出口へ落ちる溝）。</summary>
        static readonly Vector2[] TunnelChannel = { new Vector2(1.95f, -9.0f), new Vector2(1.95f, -11.9f), new Vector2(1.4f, -12.05f), new Vector2(-2.8f, -12.05f) };
        static readonly Vector2[] Outfall = { new Vector2(1.95f, -11.9f), new Vector2(2.05f, -14.0f) };
        static float TunnelGround(float x, float z)
        {
            float h = TunY + 0.08f * PropKit.Fbm(new Vector3(x * 0.9f, 5f, z * 0.9f), 23);
            var p = new Vector2(x, z);
            h = Carve(h, TunY, DistToLine(p, TunnelChannel), 0.42f, 0.4f);
            return Carve(h, TunY, DistToLine(p, Outfall), 0.3f, 0.4f);
        }

        static void Ground()
        {
            var g = Group("Ground");
            const float half = 6.2f;
            Grid(g, "Ground_Hall", (u, v) =>
            {
                float x = Mathf.Lerp(-half, half, u), z = Mathf.Lerp(-half, half, v);
                return new Vector3(x, HallGround(x, z), z);
            }, 62, 62, p => p.z < -6.3f || (new Vector2(p.x, p.z).magnitude > 6.12f && !(Mathf.Abs(p.x) < 2.7f && p.z < 0f)), _ => Vector3.up, _rock);
            Grid(g, "Ground_Tunnel", (u, v) =>
            {
                float x = Mathf.Lerp(-2.7f, 2.7f, u), z = Mathf.Lerp(MouthZ - 0.1f, -9.0f, v);
                return new Vector3(x, TunnelGround(x, z), z);
            }, 27, 25, null, _ => Vector3.up, _rock);
            Box(g, "StepFallBed", new Vector3(1.45f, TunY - 0.6f, -9.0f), new Vector3(2.65f, TunY - 0.2f, StepTopZ), _rock);

            // 水：池、水路 3 本（溝の深さの半分くらいまで）、段の横の滝、トンネルの水路、水車の穴、出口へ落ちる水
            Cyl(g, "Pool", new Vector3(SkyC.x, UpY - 0.14f, SkyC.y), PoolR + 0.05f, 0.02f, _water, false);
            foreach (var c in HallChannels)
            {
                var pts = new Vector3[c.pts.Length];
                for (int i = 0; i < pts.Length; i++) pts[i] = new Vector3(c.pts[i].x, TerraceBase(c.pts[i].x, c.pts[i].y) - c.depth * 0.45f, c.pts[i].y);
                WaterStrip(g, "Channel", pts, c.hw * 2f);
            }
            foreach (var d in DyeBasins) Cyl(g, "DyePool", new Vector3(d.x, UpY - 0.09f, d.y), 0.42f, 0.02f, _water, false);
            foreach (var w in Puddles) Cyl(g, "Puddle", new Vector3(w.x, TerraceBase(w.x, w.y) - 0.05f, w.y), w.z * 0.75f, 0.02f, _water, false);
            WaterStrip(g, "StepFall", new[] { new Vector3(1.95f, FloorY - 0.15f, StepTopZ), new Vector3(1.95f, TunY - 0.18f, -9.0f) }, 0.8f);
            var tun = new List<Vector3>();
            foreach (var q in TunnelChannel) tun.Add(new Vector3(q.x, TunY - 0.18f, q.y));
            tun.Add(new Vector3(WheelC.x + 0.6f, 0.35f, -12.4f));
            WaterStrip(g, "TunnelChannel", tun.ToArray(), 0.8f);
            WaterStrip(g, "Outfall", new[] { new Vector3(1.95f, TunY - 0.18f, -11.9f), new Vector3(2.05f, TunY - 0.25f, MouthZ), new Vector3(2.2f, 0.02f, MouthZ - 1.2f) }, 0.5f);
            Box(g, "WheelPitWater", new Vector3(-5.1f, 0.25f, -15.6f), new Vector3(-2.6f, 0.32f, -11.4f), _water, false);
        }

        // ───────── 板の足場 ─────────

        /// <summary>
        /// 板の床（真ん中 c は板の上の面）。along の向きに長さ len、幅 w。板は along に直角に並べ、すき間 3 cm、少しずつ傾いて手作りに。
        /// 下に梁 2 本と、四隅から岩へ下りる柱。rails なら長い辺の両側に手すり（橋）。当たり判定は見えない箱 1 つ。
        /// </summary>
        static void Planks(Transform g, string name, Vector3 c, float yawDeg, float len, float w, bool rails = false, float postDepth = 0.9f)
        {
            var t = new GameObject(name).transform;
            t.SetParent(g, false);
            t.SetPositionAndRotation(c, Quaternion.Euler(0f, yawDeg, 0f));
            int n = Mathf.Max(1, Mathf.RoundToInt(len / 0.2f));
            for (int i = 0; i < n; i++)
            {
                float s = -len * 0.5f + (i + 0.5f) * len / n;
                var p = t.TransformPoint(new Vector3(0f, -0.04f + 0.012f * PropKit.Noise(new Vector3(s * 3f, 0f, c.x), 31), s));
                Prim(PrimitiveType.Cube, t, "Plank", p, new Vector3(w * (0.94f + 0.06f * PropKit.H(i, 7)), 0.08f, len / n - 0.03f), _wood,
                     0f, new Vector3(0f, yawDeg + 1.5f * PropKit.Noise(new Vector3(i, 2f, c.z), 32), 0f), false);
            }
            foreach (float side in new[] { -0.38f, 0.38f })
                Rod(t, "Beam", t.TransformPoint(new Vector3(side * w, -0.15f, -len * 0.5f)), t.TransformPoint(new Vector3(side * w, -0.15f, len * 0.5f)), 0.07f, _wood);
            foreach (float sx in new[] { -0.4f, 0.4f })
                foreach (float sz in new[] { -0.45f, 0.45f })
                {
                    var top = t.TransformPoint(new Vector3(sx * w, -0.15f, sz * len));
                    Rod(t, "Post", top, top + Vector3.down * postDepth, 0.06f, _wood);
                    Rod(t, "Lashing", top + Vector3.down * 0.02f, top + Vector3.down * 0.12f, 0.075f, _wood);   // 柱と梁を縛った縄（Nov 4）
                }
            if (len >= 2.5f)   // 長い床は下に筋かい（柱の根元から梁の真ん中へ、両側）
                foreach (float sx in new[] { -0.4f, 0.4f })
                    foreach (float sz in new[] { -0.45f, 0.45f })
                    {
                        var foot = t.TransformPoint(new Vector3(sx * w, -0.15f, sz * len)) + Vector3.down * postDepth * 0.7f;
                        Rod(t, "Brace", foot, t.TransformPoint(new Vector3(sx * w * 0.95f, -0.17f, sz * len * 0.35f)), 0.035f, _wood);
                    }
            if (rails)
                foreach (float side in new[] { -0.5f, 0.5f })
                {
                    var a = t.TransformPoint(new Vector3(side * w, 0f, -len * 0.5f)); var b = t.TransformPoint(new Vector3(side * w, 0f, len * 0.5f));
                    Rod(t, "RailPost", a, a + Vector3.up * 0.5f, 0.03f, _wood);
                    Rod(t, "RailPost", b, b + Vector3.up * 0.5f, 0.03f, _wood);
                    Rod(t, "Rail", a + Vector3.up * 0.5f, b + Vector3.up * 0.5f, 0.025f, _wood);
                }
            var col = new GameObject(name + "_COL").AddComponent<BoxCollider>();
            col.transform.SetParent(t, false);
            col.center = new Vector3(0f, -0.06f, 0f);
            col.size = new Vector3(w, 0.12f, len);
        }

        /// <summary>まっすぐな板の階段：足元 a（下の段の板の上）から向き yaw へ、count 段（1 段 0.15 m・踏み面 0.3 m）。当たり判定は見えない坂。</summary>
        static void StairsStraight(Transform g, string name, Vector3 a, float yawDeg, float w, int count)
        {
            var rot = Quaternion.Euler(0f, yawDeg, 0f);
            var fwd = rot * Vector3.forward;
            for (int i = 0; i < count; i++)
            {
                var p = a + fwd * (0.3f * i + 0.15f) + Vector3.up * (0.15f * (i + 1) - 0.04f);
                Prim(PrimitiveType.Cube, g, name + "_Step", p, new Vector3(w, 0.08f, 0.3f), _wood, yawDeg, null, false);
            }
            var side = rot * Vector3.right;
            var end = a + fwd * (0.3f * count) + Vector3.up * (0.15f * count);
            foreach (float s in new[] { -0.5f, 0.5f }) Rod(g, name + "_Stringer", a + side * s * w + Vector3.down * 0.1f, end + side * s * w + Vector3.down * 0.1f, 0.06f, _wood);
            float run = 0.3f * count, rise = 0.15f * count;
            var ramp = Prim(PrimitiveType.Cube, g, name + "_COL", (a + end) * 0.5f + Vector3.down * 0.05f, new Vector3(w, 0.1f, Mathf.Sqrt(run * run + rise * rise)), _wood,
                            0f, new Vector3(-Mathf.Atan2(rise, run) * Mathf.Rad2Deg, yawDeg, 0f));
            ramp.GetComponent<Renderer>().enabled = false;
        }

        /// <summary>壁ぞいの板の階段（角度 a0 → a1、半径 r0〜r1、高さ y0 → y1）。段の板・両側のけた・床へ下りる柱。当たり判定は見えないらせんの坂。</summary>
        static void StairsArc(Transform g, string name, float a0, float a1, float r0, float r1, float y0, float y1, int count)
        {
            float rm = (r0 + r1) * 0.5f;
            for (int i = 0; i < count; i++)
            {
                float a = Mathf.Lerp(a0, a1, (i + 0.5f) / count);
                float y = Mathf.Lerp(y0, y1, (i + 1f) / count) - 0.04f;
                Prim(PrimitiveType.Cube, g, name + "_Step", Polar(a, rm, y), new Vector3(r1 - r0, 0.08f, 0.32f), _wood, FaceCenter(a) + 90f, null, false);
                if (i % 3 == 1) Rod(g, name + "_Post", Polar(a, r0 + 0.1f, y - 0.1f), Polar(a, r0 + 0.1f, TerraceBase(Polar(a, r0, 0f).x, Polar(a, r0, 0f).z) - 0.2f), 0.06f, _wood);
            }
            for (int i = 0; i < 8; i++)
                foreach (float r in new[] { r0 + 0.05f, r1 - 0.05f })
                {
                    float ta = i / 8f, tb = (i + 1) / 8f;
                    Rod(g, name + "_Stringer", Polar(Mathf.Lerp(a0, a1, ta), r, Mathf.Lerp(y0, y1, ta) - 0.12f), Polar(Mathf.Lerp(a0, a1, tb), r, Mathf.Lerp(y0, y1, tb) - 0.12f), 0.05f, _wood);
                }
            Sector(g, name + "_COL", a0, a1, r0, r1, y0, y1, 0.1f, _wood, false);
        }

        static void Boards()
        {
            var g = Group("Boards");
            // 下段：踊り場（段の上）・精製所の台・飾り枠の台と ① の橋
            Planks(g, "Landing", new Vector3(-0.45f, LDeck, -5.35f), 90f, 3.7f, 1.7f);
            // 精製所の板の床は本物の形（Refinery/BaseRefineryInBase.cs の Base_RefineryDeck、当たり判定つき）
            Planks(g, "KazariDeck", new Vector3(3.2f, LDeck, -4.5f), 0f, 1.2f, 1.2f);
            Planks(g, "Bridge_Lower", new Vector3(2.0f, LDeck, -4.5f), 90f, 1.3f, 1.0f, true);
            Planks(g, "SnailWalk", new Vector3(4.3f, LDeck, -3.95f), 42f, 1.5f, 0.8f);
            // 下段 → 中段の階段、中段：地図の台の広い円い台座（③ が下をくぐる）、細工場への板の道
            StairsStraight(g, "Stairs_LM", new Vector3(-0.3f, LDeck, -4.5f), 0f, 1.2f, 4);
            RoundDeck(g, MapAt, MapR);
            Planks(g, "ToWorkshop", new Vector3(-3.2f, MDeck, -0.75f), 90f, 1.0f, 0.9f);
            Sector(g, "WorkshopDeck", 158f, 188f, 3.6f, 5.9f, MDeck, MDeck, 0.12f, _wood);
            // 中段 → 上段の階段（台座の北西から）、上段：池の南のふちの板の道・① の橋・染め場の台
            StairsStraight(g, "Stairs_MU", new Vector3(-2.2f, MDeck, 0.55f), 0f, 1.0f, 4);
            Planks(g, "PoolWalk", new Vector3(-0.85f, UDeck, 1.8f), 90f, 3.6f, 0.7f);
            Planks(g, "Bridge_Upper", new Vector3(1.6f, UDeck, 1.8f), 90f, 1.3f, 0.7f, true);
            Planks(g, "DyeDeck", new Vector3(2.85f, UDeck, 1.5f), 0f, 1.1f, 1.3f);
            // 2階・3階：西の壁の木の回廊（梁で壁に差しこむ）と板の階段（折り返して木組みが重なる）。3階は細工場の横穴の上
            StairsArc(g, "Stairs_2F", 188f, 226f, 5.0f, 5.8f, MDeck, F2Y, 12);
            Sector(g, "Gallery2F", 226f, 254f, 4.4f, 5.9f, F2Y, F2Y, 0.15f, _wood);
            StairsArc(g, "Stairs_3F", 226f, 200f, 4.2f, 4.95f, F2Y, F3Y, 8);
            Sector(g, "Ledge3F", 146f, 202f, 4.55f, 5.85f, F3Y, F3Y, 0.15f, _wood);
            for (float a = 230f; a <= 252f; a += 11f)
            {
                Rod(g, "GalleryPost", Polar(a, 4.5f, F2Y - 0.15f), Polar(a, 4.5f, FloorY - 0.2f), 0.08f, _wood);
                Rod(g, "GalleryBrace", Polar(a, 4.6f, F2Y - 0.15f), Polar(a, 5.95f, F2Y - 1.3f), 0.06f, _wood);
            }
            for (float a = 150f; a <= 198f; a += 12f) Rod(g, "LedgeBrace", Polar(a, 4.65f, F3Y - 0.15f), Polar(a, 5.95f, F3Y - 1.4f), 0.06f, _wood);
            Railing(g, 4.45f, 228f, 254f, F2Y, 13f);
            Railing(g, 4.6f, 166f, 196f, F3Y, 10f);   // 146°〜165° は縁側の口（BaseGrayboxEngawa.cs）
            // 天井の下の太い梁（灯を吊るす）。縁側の上をあけるため y 7.6（Nov 4）
            foreach (var (a, b) in new[] { (230f, 50f), (280f, 100f), (320f, 140f) })
                Rod(g, "CeilingBeam", Polar(a, BeamR, BeamY), Polar(b, BeamR, BeamY), 0.13f, _wood);

            // トンネル：板の道（東の水路に沿う）・出口の手前の橋・行商人の前の板
            for (int i = 0; i < Steps; i++)
            {
                float z = StepTopZ - StepRun * (i + 0.5f);
                Prim(PrimitiveType.Cube, g, "Step", new Vector3(-0.55f, LDeck - 0.15f * (i + 1) - 0.04f, z), new Vector3(3.9f, 0.08f, StepRun), _wood, 0f, null, false);
            }
            float stepRun = StepRun * Steps;
            var steps = Prim(PrimitiveType.Cube, g, "Steps_COL", new Vector3(-0.55f, (LDeck + TunY + Board) * 0.5f - 0.05f, StepTopZ - stepRun * 0.5f),
                             new Vector3(3.9f, 0.1f, Mathf.Sqrt(stepRun * stepRun + 1.44f)), _wood, 0f, new Vector3(-Mathf.Atan2(1.2f, stepRun) * Mathf.Rad2Deg, 0f, 0f));
            steps.GetComponent<Renderer>().enabled = false;
            Box(g, "StepsFill", new Vector3(-2.5f, TunY - 0.4f, StepTopZ - stepRun), new Vector3(1.4f, TunY + 0.1f, StepTopZ), _rock);
            Planks(g, "TunnelWalk", new Vector3(-0.35f, TunY + Board, -10.27f), 0f, 2.55f, 2.3f, false, 0.6f);
            Planks(g, "Bridge_Tunnel", new Vector3(-0.35f, TunY + Board, -12.05f), 0f, 1.0f, 2.3f, true, 0.6f);
            Planks(g, "MouthWalk", new Vector3(-0.35f, TunY + Board, -13.3f), 0f, 1.5f, 2.3f, false, 0.6f);
            Planks(g, "ShopFloor", new Vector3(-3.35f, TunY + Board, -9.7f), 0f, 1.8f, 1.9f, false, 0.6f);
        }

        static void RoundDeck(Transform g, Vector3 c, float r)
        {
            var t = Group("MapDeck");
            for (float s = -r + 0.1f; s < r; s += 0.2f)
            {
                float len = 2f * Mathf.Sqrt(Mathf.Max(0.01f, r * r - s * s));
                Prim(PrimitiveType.Cube, t, "Plank", c + new Vector3(0f, -0.04f, s), new Vector3(len, 0.08f, 0.17f), _wood, 0f, null, false);
            }
            foreach (float s in new[] { -0.7f, 0.7f }) Rod(t, "Beam", c + new Vector3(-r, -0.15f, s), c + new Vector3(r, -0.15f, s), 0.08f, _wood);
            var col = Cyl(t, "MapDeck_COL", c + Vector3.down * 0.12f, r, 0.12f, _wood);
            col.GetComponent<Renderer>().enabled = false;
        }
    }
}
