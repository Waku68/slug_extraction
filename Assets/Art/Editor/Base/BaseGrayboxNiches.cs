using UnityEngine;

namespace SlugDesign
{
    /// <summary>
    /// 拠点の灰色の箱：洞窟の壁を凹ませた横穴（Nov 3「木の板で場所を作るんじゃなくて、洞窟を凹ませて場所を確保するのあり」）と、
    /// 生きている感じ（苔・シダ・天窓から垂れるつる・池の小エビ・天井のアナツバメ）。
    /// 横穴：上が丸いアーチの口、奥は平らな壁。床は岩（その設備の板と同じ高さ）。
    /// - 細工場 172°（幅 2.6・高さ 2.2・奥行き 1.6、中段）、精製所の塩の壺の棚とアモレ 220°（2.4・1.6・1.3、下段）
    /// - 飾り枠 304°・図鑑 317°（1.3・1.7・0.7、下段、① の橋を渡った東の壁）、カタツムリの寝床 335°（1.8・1.3・1.4、下段の東、① の横の湿った所）
    /// </summary>
    public static partial class BaseGrayboxBuilder
    {
        struct NicheSpec { public string name; public float deg, w, h, depth, floor; }

        static NicheSpec[] NicheList => new[]
        {
            new NicheSpec { name = "Workshop", deg = 172f, w = 2.6f, h = 2.2f, depth = 1.6f, floor = MDeck },
            new NicheSpec { name = "Salt", deg = 220f, w = 2.4f, h = 1.6f, depth = 1.3f, floor = LDeck },
            new NicheSpec { name = "Kazari", deg = 304f, w = 1.3f, h = 1.7f, depth = 0.7f, floor = LDeck },
            new NicheSpec { name = "Zukan", deg = 317f, w = 1.3f, h = 1.7f, depth = 0.7f, floor = LDeck },
            new NicheSpec { name = "SnailBed", deg = 335f, w = 1.8f, h = 1.3f, depth = 1.4f, floor = LDeck - 0.05f },
        };

        /// <summary>広間の殻の四角が、どれかの横穴の口に入るか（殻を抜く）。</summary>
        static bool InNiche(Vector3 p)
        {
            float a = Mathf.Atan2(p.z, p.x) * Mathf.Rad2Deg; if (a < 0f) a += 360f;
            foreach (var n in NicheList)
            {
                float half = n.w * 0.5f / HallR * Mathf.Rad2Deg;
                if (Mathf.Abs(Mathf.DeltaAngle(a, n.deg)) < half && p.y > n.floor - 0.3f && p.y < n.floor + n.h) return true;
            }
            return false;
        }

        /// <summary>横穴の殻（アーチの筒＋奥の壁）と岩の床。中から見える内向きの面。</summary>
        static void Niches()
        {
            var g = Group("Niches");
            foreach (var n in NicheList)
            {
                var o = Polar(n.deg, 1f, 0f);                 // 外向き
                var side = Vector3.Cross(Vector3.up, o);       // 口の横向き
                var mouth = Polar(n.deg, HallR - 0.4f, n.floor);
                float len = n.depth + 0.4f;
                System.Func<float, Vector3> arch = u => side * (-Mathf.Cos(Mathf.PI * u) * n.w * 0.5f)
                                                      + Vector3.up * (n.h * Mathf.Pow(Mathf.Max(0f, Mathf.Sin(Mathf.PI * u)), 0.5f) - 0.15f * (1f - Mathf.Sin(Mathf.PI * u)));
                Grid(g, "Shell_Niche_" + n.name, (u, v) => mouth + arch(u) + o * (len * v), 14, 4, null, p =>
                {
                    float t = Vector3.Dot(p - mouth, o);
                    return mouth + o * t + Vector3.up * n.h * 0.4f - p;
                }, _rock);
                var back = mouth + o * len;
                Grid(g, "Shell_Niche_" + n.name + "_Back", (u, v) => back + arch(u) * v + Vector3.up * n.h * 0.35f * (1f - v), 14, 3, null, _ => -o, _rock);
                Prim(PrimitiveType.Cube, g, "NicheFloor_" + n.name, mouth + o * (len * 0.5f) + Vector3.down * 0.15f, new Vector3(n.w, 0.3f, len), _rock,
                     Mathf.Atan2(o.x, o.z) * Mathf.Rad2Deg);
            }
        }

        // ───────── 生きている感じ ─────────

        static void Life()
        {
            var g = Group("Life");
            // 苔：池のふち（北側）、水路の落ち口、濡れた岩
            foreach (float a in new[] { 25f, 60f, 95f, 130f, 165f })
            {
                var p = new Vector3(SkyC.x, 0f, SkyC.y) + Polar(a, PoolR + 0.25f, 0f);
                Moss(g, p, 0.55f);
            }
            foreach (var q in new[] { new Vector2(2.6f, 0.75f), new Vector2(2.95f, -2.45f), new Vector2(4.5f, 0.6f), new Vector2(-3.6f, 0.75f),
                                      new Vector2(-0.2f, -2.9f), new Vector2(4.4f, -2.2f), new Vector2(-4.6f, -2.0f) })
                Moss(g, new Vector3(q.x, 0f, q.y), 0.45f);
            // シダ：岩の割れ目と段のふち
            foreach (var q in new[] { new Vector2(3.5f, -1.9f), new Vector2(-4.4f, 2.2f), new Vector2(4.7f, 2.6f), new Vector2(1.2f, 4.9f),
                                      new Vector2(-1.9f, -2.8f), new Vector2(3.3f, 0.6f) })
                Fern(g, new Vector3(q.x, HallGround(q.x, q.y), q.y));
            // 天窓のふちから垂れるつる
            for (int i = 0; i < 10; i++)
            {
                float a = i * 36f + 8f;
                var top = new Vector3(SkyC.x, 0f, SkyC.y) + Polar(a, SkyR - 0.05f, 0f);
                float r = new Vector2(top.x, top.z).magnitude;
                float dome = FloorY + WallH + (HallH - WallH) * Mathf.Sqrt(Mathf.Max(0f, 1f - r * r / (HallR * HallR)));
                top.y = dome - 0.05f;
                Rod(g, "Vine", top, top + Vector3.down * (1.2f + 1.3f * PropKit.H(i, 41)) + Polar(a, 0.15f, 0f), 0.025f, _moss);
            }
            // 池の赤い小エビと、天井近くのアナツバメ
            for (int i = 0; i < 7; i++)
            {
                var p = new Vector3(SkyC.x, UpY - 0.18f, SkyC.y) + Polar(i * 51f, 0.4f + 1.1f * PropKit.H(i, 42), 0f);
                Prim(PrimitiveType.Sphere, g, "Shrimp", p, new Vector3(0.05f, 0.03f, 0.08f), _life, i * 51f, null, false);
            }
            foreach (var p in new[] { new Vector3(-1.5f, 8.0f, 0.5f), new Vector3(1.8f, 7.6f, -1.2f), new Vector3(0.2f, 8.3f, -2.4f) })
            {
                Prim(PrimitiveType.Cube, g, "SwiftletWing", p, new Vector3(0.36f, 0.02f, 0.08f), _wood, 0f, new Vector3(0f, 30f, 12f), false);
                Prim(PrimitiveType.Sphere, g, "SwiftletBody", p, new Vector3(0.06f, 0.05f, 0.12f), _wood, 30f, null, false);
            }
        }

        static void Moss(Transform g, Vector3 p, float size)
        {
            p.y = HallGround(p.x, p.z);
            Prim(PrimitiveType.Sphere, g, "Moss", p + Vector3.up * 0.02f, new Vector3(size, size * 0.22f, size * 0.8f), _moss, p.x * 40f, null, false);
        }

        static void Fern(Transform g, Vector3 p)
        {
            for (int k = 0; k < 6; k++)
            {
                float yaw = k * 60f + p.x * 17f;
                var dir = Quaternion.Euler(0f, yaw, 0f) * Vector3.forward;
                Prim(PrimitiveType.Cube, g, "FernLeaf", p + dir * 0.18f + Vector3.up * 0.12f, new Vector3(0.1f, 0.02f, 0.42f), _moss, 0f, new Vector3(-35f, yaw, 0f), false);
            }
        }
    }
}
