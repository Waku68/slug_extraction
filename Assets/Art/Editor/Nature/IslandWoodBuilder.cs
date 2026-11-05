using System.Text;
using UnityEditor;
using UnityEngine;

namespace SlugDesign
{
    /// <summary>
    /// 木の小物（Nov 2 下書き、3 案ずつ）。メニュー Slug/Nature/Props/Build Wood で作り直す（Assets/Art/Nature/Wood）。
    /// 乾いた木・流木・土はかじれないのでカクカク、苔・きのこ・朽ちた木の中はなめらか。ナメクジの目の高さ 0.47 m は人の約 1/3.6。
    /// - Stump 切り株：A ずんぐり 直径 0.9 m・高さ 0.5 m、根が 5 本、切り口に年輪。B 折れた株 高さ 1.0 m、ぎざぎざの上。C 朽ちた株 0.6 m、苔と棚のきのこ
    /// - Driftwood 流木の山（浜・川の州）：A 白い流木 5 本が重なる（1.5〜2.5 m）。B ねじれた大きな流木 1 本 3 m、根のこぶ付き。C 小枝の山 8 本（0.6〜1.2 m）
    /// - FallenLog 倒れた大木（幹の太さ 1.5 m、人なら 5 m の壁）：A 長さ 9 m、折れた先と枝の跡。B 長さ 8 m、片方の端が中空で奥 2 m（ナメクジが入れる）。
    ///   C 二つに折れて、上に苔の帯
    /// - RootPlate 根ごと倒れた木の根の円盤：A 直径 5 m が立ち、根 10 本と幹 3 m。B 4 m が 70° に傾き、手前に土の山。C 6 m、長い根が垂れ、石がはさまる
    /// - DeadBranch 枯れ枝の骨（砂漠）：A 地をはう低い骨 幅 1 m。B 寝た枯れ枝 2 m。C 立ち枯れ 1.6 m、曲がった腕 3 本
    /// 色：樹皮 #8A7660、暗い樹皮 #6A5A48、切り口 #C8B08A、年輪 #A08458、白い流木 #C4BBAA、土 #7A6450、苔 #73804C、朽ち木 #6E5A44、
    /// 棚きのこ #C89A60、石 #A39C8E。当たり判定は形どおりのメッシュ（枝先の細い物は見るだけ）。
    /// </summary>
    public static class IslandWoodBuilder
    {
        const string Dir = "Assets/Art/Nature/Wood";
        const int BARK = 0, BARKDARK = 1, CUT = 2, RING = 3, DRIFT = 4, DIRT = 5, MOSS = 6, ROT = 7, FUNGUS = 8, STONE = 9;
        static Material[] _m;

        [MenuItem("Slug/Nature/Props/Build Wood")]
        public static void Build()
        {
            // bark: near-tone patches (a shade darker, a mossy green), darker stains, fine dents; cut wood grainy; driftwood silver-grey and weathered
            _m = PropKit.Mats(Dir, "Wood",
                new PropKit.Spec("Bark", "#8A7660", true).Mottle("#6E5E4C", 0.5f, 5f).Camo("#806E5A", "#6C5C4A", "#8A8C62", 3f, 0.55f).Dent(0.35f, 18f),
                new PropKit.Spec("BarkDark", "#6A5A48", true).Mottle("#554838", 0.5f, 5f).Camo("#62533F", "#544636", "#6E7450", 3f, 0.55f).Dent(0.35f, 18f),
                new PropKit.Spec("Cut", "#C8B08A", true).Mottle("#A88E66", 0.6f, 9f).Felt(0.35f),
                new PropKit.Spec("Ring", "#A08458", true).Felt(0.3f),
                new PropKit.Spec("Driftwood", "#C4BBAA", true).Mottle("#A39A8A", 0.55f, 4f).Felt(0.25f).Dent(0.3f, 25f),
                new PropKit.Spec("Dirt", "#7A6450", true).Mottle("#5E4C3C", 0.7f, 8f).Felt(0.4f).Dent(0.6f, 40f),
                new PropKit.Spec("Moss", "#73804C", false).Mottle("#8E9C58", 0.7f, 14f).Felt(0.5f).Dent(0.5f, 60f),
                new PropKit.Spec("Rot", "#6E5A44", false).Mottle("#4E3E2E", 0.6f, 8f).Felt(0.5f),
                new PropKit.Spec("Fungus", "#C89A60", false).Mottle("#E0BE88", 0.5f, 14f).Dent(0.3f, 40f),
                new PropKit.Spec("Stone", "#A39C8E", true).Mottle("#88827A", 0.5f, 4f).Camo("#ADA799", "#928C80", "#B9B596", 5f, 0.6f).Dent(0.3f, 22f));
            var log = new StringBuilder("Wood built:");
            for (int v = 0; v < 3; v++)
                log.Append(' ').Append(Stump(v)).Append(' ').Append(Driftwood(v)).Append(' ').Append(FallenLog(v))
                   .Append(' ').Append(RootPlate(v)).Append(' ').Append(DeadBranch(v));
            AssetDatabase.SaveAssets();
            Debug.Log(log);
        }

        static string Abc(int v) => ((char)('A' + v)).ToString();
        static NatureMeshKit Kit() => new NatureMeshKit(_m.Length);
        static string Save(string name, NatureMeshKit kit, int collider = 3) => PropKit.Save(Dir, name, kit, _m, collider);

        /// <summary>樹皮：割れた板の筋（PropKit.BarkPlates）。切り株・根の太さに掛ける。</summary>
        static float Bark(float t, float a, float len, int seed, float ridges = 10f) => PropKit.BarkPlates(t, a, len, seed, ridges, 0.14f);

        /// <summary>樹皮の溝だけ暗い色にする（面ごとのマテリアル）。</summary>
        static System.Func<float, float, int> Grooves(float len, int seed, float ridges = 10f, int bark = BARK) =>
            (t, a) => PropKit.BarkGroove(t, a, len, seed, ridges) ? BARKDARK : bark;

        /// <summary>根：根元 at から水平の向き yaw へ、地面にもぐっていく太い根。</summary>
        static void Root(NatureMeshKit kit, Vector3 at, float yaw, float len, float r, int seed, int sub = BARK)
        {
            var d = PropKit.Dir(yaw);
            // the root humps up out of the flare, then dives into the ground and tapers to nothing there
            PropKit.Pipe(kit, t => at + d * (len * t) + Vector3.up * (0.12f * len * Mathf.Sin(Mathf.PI * t * 0.8f) - 1.6f * r * t * t),
                (t, a) => r * Mathf.Lerp(1f, 0.08f, Mathf.Pow(t, 1.3f)) * (sub == BARK ? Bark(t, a, len, seed, 6f) : PropKit.Grain(t, a, len, seed)), 8, 12, sub, -1, sub, 0f, null,
                sub == BARK ? Grooves(len, seed, 6f) : null);
        }

        /// <summary>
        /// 年輪の切り口：明るい面に、少しゆがんだ年輪 6 本、暗い芯、真ん中から外へ走る干割れ 3 本、ふちに暗い樹皮の輪
        /// （少しずつ高さを変えて重ねる）。
        /// </summary>
        static void CutFace(NatureMeshKit kit, Vector3 c, float r, int sides)
        {
            sides = Mathf.Max(sides, 32);
            float Wob(float a, float k) => 1f + 0.06f * Mathf.Sin(a * 2f + k) + 0.03f * Mathf.Sin(a * 5f + k * 2f);
            PropKit.Surface(kit, (t, a) => c + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * (r * (1f - t)) + Vector3.up * 0.004f, 1, sides, CUT);
            for (int k = 1; k <= 6; k++)
            {
                float rr = r * (0.16f + 0.13f * k), w = r * (0.012f + 0.006f * (k % 2));
                int kk = k;
                PropKit.Surface(kit, (t, a) => c + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * ((rr - w * t) * Wob(a, kk * 0.4f)) + Vector3.up * 0.008f, 1, sides, RING);
            }
            PropKit.Surface(kit, (t, a) => c + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * (r * 0.14f * (1f - t) * Wob(a, 0f)) + Vector3.up * 0.01f, 1, 16, RING);
            PropKit.Surface(kit, (t, a) => c + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * (r * (1f - 0.08f * t)) + Vector3.up * 0.012f, 1, sides, BARKDARK);
            for (int i = 0; i < 3; i++)
            {
                // a drying crack: a thin dark wedge from near the middle out toward the rim
                float a0 = i * 2.2f + 0.4f;
                var d = new Vector3(Mathf.Cos(a0), 0f, Mathf.Sin(a0)); var sd = new Vector3(-d.z, 0f, d.x);
                float l = r * (0.55f + 0.15f * i);
                int b = kit.VertexCount;
                kit.Vert(c + d * (r * 0.12f) + Vector3.up * 0.014f, Vector2.zero);
                kit.Vert(c + d * l + sd * (r * 0.035f) + Vector3.up * 0.014f, Vector2.zero);
                kit.Vert(c + d * l - sd * (r * 0.035f) + Vector3.up * 0.014f, Vector2.zero);
                kit.Tri(BARKDARK, b, b + 2, b + 1);
            }
        }

        /// <summary>棚のきのこ：幹の面の点 at から、外の向き out へ張り出す半円の棚。</summary>
        static void Shelf(NatureMeshKit kit, Vector3 at, Vector3 outDir, float r)
        {
            outDir = new Vector3(outDir.x, 0f, outDir.z).normalized;
            var q = Quaternion.LookRotation(outDir);
            PropKit.Surface(kit, (t, a) =>
            {
                float ang = Mathf.Lerp(-Mathf.PI * 0.5f, Mathf.PI * 0.5f, a / (Mathf.PI * 2f));
                var p = new Vector3(Mathf.Sin(ang) * r, 0f, Mathf.Cos(ang) * r * 0.8f) * t;
                float y = Mathf.Sin(a) * r * 0.18f * (1f - t * 0.5f);
                return at + q * (p + Vector3.up * y);
            }, 3, 10, FUNGUS);
        }

        static string Stump(int v)
        {
            var kit = Kit();
            if (v == 0)
            {
                float R = 0.45f, Hh = 0.5f;
                PropKit.Pipe(kit, t => Vector3.up * (Hh * t - 0.05f), (t, a) => R * (1f + 0.35f * Mathf.Exp(-t * 5f)) * Bark(t, a, Hh, 120), 12, 40, BARK, -1, CUT, 0f, null, Grooves(Hh, 120));
                CutFace(kit, Vector3.up * (Hh - 0.05f), R * 0.98f, 40);
                for (int i = 0; i < 5; i++) Root(kit, new Vector3(0f, 0.1f, 0f) + PropKit.Dir(i * 72f + 10f) * R * 0.8f, i * 72f + 10f + PropKit.R(121, i, -12f, 12f), PropKit.R(122, i, 0.6f, 0.9f), 0.16f, 123 + i);
            }
            else if (v == 1)
            {
                // snapped: the top ring's height is jagged around (tall splinters on one side)
                float R = 0.36f, Hh = 1.0f;
                PropKit.Surface(kit, (t, a) =>
                {
                    float top = Hh * (0.65f + 0.35f * Mathf.Pow(Mathf.Abs(Mathf.Sin(a * 2.5f + 0.4f)), 3f) * (0.6f + 0.4f * Mathf.Cos(a)));
                    float y = Mathf.Lerp(-0.05f, top, t);
                    float r = R * (1f + 0.3f * Mathf.Exp(-t * 5f)) * Bark(t, a, Hh, 124) * (1f - 0.35f * Mathf.Pow(Mathf.Max(0f, t - 0.8f) / 0.2f, 2f));
                    return new Vector3(Mathf.Cos(a) * r, y, Mathf.Sin(a) * r);
                }, 20, 40, BARK, -1, CUT, -0.1f, null, Grooves(Hh, 124));
                for (int i = 0; i < 4; i++) Root(kit, new Vector3(0f, 0.1f, 0f) + PropKit.Dir(i * 90f + 40f) * R * 0.8f, i * 90f + 40f, PropKit.R(125, i, 0.5f, 0.75f), 0.13f, 126 + i);
            }
            else
            {
                float R = 0.4f, Hh = 0.6f;
                PropKit.Surface(kit, (t, a) =>
                {
                    float r = R * (1f + 0.25f * Mathf.Exp(-t * 4f)) * Bark(t, a, Hh, 127) * (1f + 0.15f * PropKit.Noise(new Vector3(Mathf.Cos(a) * 2f, t * 3f, Mathf.Sin(a) * 2f), 128));
                    float y = Mathf.Lerp(-0.05f, Hh * (0.85f + 0.15f * Mathf.Sin(a * 3f)), t);
                    return new Vector3(Mathf.Cos(a) * r, y, Mathf.Sin(a) * r);
                }, 16, 40, BARKDARK, -1, ROT, -0.12f, null, Grooves(Hh, 127, 10f, BARKDARK));
                // a cushion of moss over the rotten top, and bracket fungi stepping down one side
                PropKit.Lump(kit, new Vector3(0.05f, Hh * 0.92f, 0f), new Vector3(R * 0.95f, 0.08f, R * 0.85f), 6, 24, MOSS, 129, 0.35f, 4f);
                for (int i = 0; i < 3; i++)
                {
                    float yaw = 150f + i * 22f, y = 0.12f + i * 0.14f;
                    Shelf(kit, PropKit.Dir(yaw) * (R * 1.02f) + Vector3.up * y, PropKit.Dir(yaw), 0.16f - i * 0.03f);
                }
            }
            return Save($"Stump_{Abc(v)}", kit);
        }

        /// <summary>流木 1 本：a から b、太さ r、ゆるく曲がり、両端は丸く削れている。</summary>
        static void DriftLog(NatureMeshKit kit, Vector3 a, Vector3 b, float r, int seed)
        {
            Vector3 side = Vector3.Cross((b - a).normalized, Vector3.up).normalized;
            Vector3 c1 = Vector3.Lerp(a, b, 0.5f) + side * ((b - a).magnitude * PropKit.R(seed, 1, -0.08f, 0.08f));
            float len = (b - a).magnitude;
            PropKit.Pipe(kit, t => (1 - t) * (1 - t) * a + 2 * (1 - t) * t * c1 + t * t * b,
                (t, ang) => r * Mathf.Lerp(1f, 0.75f, t) * Mathf.Sqrt(Mathf.Clamp01(Mathf.Min(t, 1f - t) * len / (r * 1.2f) + 0.25f)) * PropKit.Grain(t, ang, len, seed, 0.6f, 0.1f),
                Mathf.Max(8, (int)(len * 12f)), 14, DRIFT, DRIFT, DRIFT);
            // a knot or two where a branch was
            for (int k = 0; k < (len > 1f ? 2 : 0); k++)
            {
                float t = PropKit.R(seed, 20 + k, 0.25f, 0.75f), ang = PropKit.R(seed, 22 + k, 0f, Mathf.PI * 2f);
                Vector3 p = (1 - t) * (1 - t) * a + 2 * (1 - t) * t * c1 + t * t * b;
                var up = Vector3.Cross(side, (b - a).normalized);
                kit.Ball(p + (side * Mathf.Cos(ang) + up * Mathf.Sin(ang)) * r * 0.85f, Vector3.one * r * 0.35f, 4, DRIFT);
            }
        }

        static string Driftwood(int v)
        {
            var kit = Kit();
            if (v == 0)
            {
                var logs = new[]
                {
                    (new Vector3(-1.2f, 0.15f, -0.3f), new Vector3(1.2f, 0.15f, 0.2f), 0.17f),
                    (new Vector3(-0.9f, 0.12f, 0.6f), new Vector3(0.7f, 0.12f, 0.9f), 0.12f),
                    (new Vector3(-0.6f, 0.38f, -0.6f), new Vector3(0.5f, 0.2f, 0.9f), 0.13f),
                    (new Vector3(0.2f, 0.42f, -0.5f), new Vector3(1.1f, 0.1f, 0.7f), 0.1f),
                    (new Vector3(-1.4f, 0.1f, 0.1f), new Vector3(-0.3f, 0.3f, -0.1f), 0.11f),
                };
                for (int i = 0; i < logs.Length; i++) DriftLog(kit, logs[i].Item1, logs[i].Item2, logs[i].Item3, 130 + i);
            }
            else if (v == 1)
            {
                // one big twisted trunk with its root knob and two broken limbs
                PropKit.Pipe(kit, t => new Vector3(-1.5f + 3f * t, 0.22f + 0.08f * Mathf.Sin(t * 5f), 0.25f * Mathf.Sin(t * 3.5f)),
                    (t, a) => Mathf.Lerp(0.26f, 0.12f, t) * (1f + 0.12f * Mathf.Cos(3f * a + t * 9f)) * Mathf.Sqrt(Mathf.Clamp01((1f - t) * 12f + 0.2f)) * PropKit.Grain(t, a, 3f, 134, 1.5f, 0.12f),
                    40, 18, DRIFT, DRIFT, DRIFT);
                PropKit.Lump(kit, new Vector3(-1.6f, 0.3f, 0f), new Vector3(0.4f, 0.32f, 0.42f), 5, 9, DRIFT, 135, 0.35f, 2.4f, 0f);
                for (int i = 0; i < 4; i++) Root(kit, new Vector3(-1.75f, 0.32f, 0f), 120f + i * 40f, PropKit.R(136, i, 0.4f, 0.7f), 0.07f, 137 + i, DRIFT);
                DriftLog(kit, new Vector3(-0.4f, 0.3f, 0.1f), new Vector3(-0.1f, 0.55f, 0.75f), 0.07f, 141);
                DriftLog(kit, new Vector3(0.6f, 0.25f, -0.2f), new Vector3(1.1f, 0.15f, -0.75f), 0.06f, 142);
            }
            else
            {
                for (int i = 0; i < 8; i++)
                {
                    float yaw = PropKit.R(143, i, 0f, 180f), len = PropKit.R(144, i, 0.6f, 1.2f);
                    var mid = new Vector3(PropKit.R(145, i, -0.3f, 0.3f), 0.04f + 0.05f * (i % 3), PropKit.R(146, i, -0.3f, 0.3f));
                    var d = PropKit.Dir(yaw) * (len * 0.5f) + Vector3.up * PropKit.R(147, i, -0.03f, 0.03f);
                    DriftLog(kit, mid - d, mid + d, PropKit.R(148, i, 0.03f, 0.05f), 150 + i);
                }
            }
            return Save($"Driftwood_{Abc(v)}", kit, v == 2 ? 0 : 3);
        }

        // ───────── 倒れた大木（ナメクジの 8 倍の長さ。形を凝る） ─────────

        /// <summary>幹の端の形：Roots 根元（板根が張り、折れた根が出る）、Snapped 折れた先（ぎざぎざの裂け）、Hollow 中空（洞の口）、Flat のこぎりのように平ら。</summary>
        enum End { Roots, Snapped, Hollow, Flat }

        /// <summary>
        /// 倒れた幹：中心線 path(t)（t=0 が根元）、太さ radius(t)。樹皮は割れた板の筋、根元は板根で花びらのように広がる。
        /// 端 e0（t=0）と e1（t=1）の形を選ぶ。
        /// </summary>
        static void Log(NatureMeshKit kit, System.Func<float, Vector3> path, System.Func<float, float> radius, float len, End e0, End e1, int seed, int sides = 60)
        {
            int rows = Mathf.Max(16, Mathf.RoundToInt(len * 9f));
            float flareLen = 1.1f / len;
            PropKit.Pipe(kit, path, (t, a) =>
            {
                float r = radius(t) * PropKit.BarkPlates(t, a, len, seed, 12f, 0.16f);
                if (e0 == End.Roots)
                {
                    // buttresses: five fins that swell out toward the root end
                    float fin = Mathf.Pow(Mathf.Abs(Mathf.Cos(a * 2.5f + 0.3f)), 3f);
                    r *= 1f + (0.25f + 0.9f * fin) * Mathf.Exp(-t / flareLen);
                }
                if (e1 == End.Snapped && t > 0.97f)
                    r *= 1f - 0.35f * (t - 0.97f) / 0.03f * Mathf.Abs(Mathf.Sin(a * 3.5f + seed));
                return r;
            }, rows, sides, BARK, e0 == End.Hollow ? -1 : CUT, e1 == End.Hollow ? -1 : CUT, e1 == End.Snapped ? -0.15f : 0.02f, null,
                (t, a) => PropKit.BarkGroove(t, a, len, seed, 12f) ? BARKDARK : BARK);
            Vector3 p1 = path(1f), d1 = (path(1f) - path(0.98f)).normalized;
            if (e1 == End.Snapped)
            {
                // long splinters standing out of the break, leaning outward a little
                var side = Vector3.Cross(d1, Vector3.up).normalized; var up = Vector3.Cross(side, d1);
                for (int i = 0; i < 7; i++)
                {
                    float a = PropKit.R(seed + 60, i, 0f, Mathf.PI * 2f), rr = radius(1f) * PropKit.R(seed + 61, i, 0.3f, 0.85f);
                    var off = (side * Mathf.Cos(a) + up * Mathf.Sin(a)) * rr;
                    var dir = (d1 + off.normalized * 0.25f).normalized;
                    float l = PropKit.R(seed + 62, i, 0.25f, 0.8f), w = PropKit.R(seed + 63, i, 0.04f, 0.09f);
                    PropKit.Pipe(kit, t => p1 - d1 * 0.15f + off + dir * (l * t), (t, ang) => w * (1f - t * 0.95f) * (0.6f + 0.4f * Mathf.Abs(Mathf.Cos(ang))), 3, 4, CUT, -1, CUT);
                }
            }
            if (e1 == End.Hollow)
            {
                // the hollow: a ragged rim, then an inner wall of soft rotten wood running 2.2 m in (a slug can crawl inside)
                float ri = radius(1f) * 0.72f;
                var side = Vector3.Cross(d1, Vector3.up).normalized; var up = Vector3.Cross(side, d1);
                PropKit.Surface(kit, (t, a) =>
                {
                    float ro = radius(1f) * PropKit.BarkPlates(1f, a, len, seed, 12f, 0.16f);
                    float rim = Mathf.Lerp(ro, ri * (1f + 0.08f * Mathf.Sin(a * 5f)), t);
                    return p1 - d1 * (0.04f * t * (1f + Mathf.Sin(a * 3f))) + (side * Mathf.Cos(a) + up * Mathf.Sin(a)) * rim;
                }, 2, sides, CUT);
                PropKit.Surface(kit, (t, a) =>
                {
                    float r = ri * (1f + 0.08f * Mathf.Sin(a * 5f)) * Mathf.Lerp(1f, 0.85f, t) * (1f + 0.06f * PropKit.Noise(new Vector3(a, t * 6f, 0f), seed + 70));
                    return p1 - d1 * (0.04f + 2.2f * t) + (side * Mathf.Cos(-a) + up * Mathf.Sin(-a)) * r;
                }, 8, sides, ROT, -1, ROT);
            }
            if (e0 == End.Roots)
            {
                // broken roots out of the buttresses: some dive into the ground, some stick up in the air, snapped
                Vector3 p0 = path(0f), d0 = (path(0.02f) - p0).normalized;
                var side = Vector3.Cross(d0, Vector3.up).normalized; var up = Vector3.Cross(side, d0);
                for (int i = 0; i < 8; i++)
                {
                    float a = i * Mathf.PI * 2f / 8f + PropKit.R(seed + 80, i, -0.2f, 0.2f);
                    var outDir = side * Mathf.Cos(a) + up * Mathf.Sin(a);
                    float l = PropKit.R(seed + 81, i, 0.7f, 1.6f), r0 = radius(0f) * PropKit.R(seed + 82, i, 0.2f, 0.32f);
                    var start = p0 + d0 * 0.25f + outDir * radius(0f) * 1.1f;
                    var dir = (outDir - d0 * 0.6f).normalized;
                    bool down = outDir.y < 0.2f;
                    int ii = i;
                    PropKit.Pipe(kit, t =>
                    {
                        var p = start + dir * (l * t) + Vector3.down * (down ? 0.4f * l * t * t : -0.1f * l * t * t);
                        p.y = Mathf.Max(p.y, r0 * 0.3f * (1f - t));
                        return p;
                    }, (t, ang) => r0 * Mathf.Lerp(1f, 0.35f, t) * PropKit.BarkPlates(t, ang, l, seed + ii, 6f, 0.1f), 6, 9, BARK, -1, CUT, -0.03f);
                }
                // soil still caught between the roots, and a couple of stones held in it
                for (int i = 0; i < 5; i++)
                {
                    float a = PropKit.R(seed + 90, i, 0f, Mathf.PI * 2f);
                    var c = p0 + d0 * 0.15f + (side * Mathf.Cos(a) + up * Mathf.Sin(a)) * radius(0f) * PropKit.R(seed + 91, i, 0.5f, 1.2f);
                    float r = PropKit.R(seed + 92, i, 0.18f, 0.32f);
                    PropKit.Lump(kit, c, new Vector3(r, r * 0.8f, r), 5, 9, i < 3 ? DIRT : STONE, seed + 93 + i, 0.3f, 2f, 0f);
                }
            }
        }

        /// <summary>折れた枝：幹の t の所の向き a（ラジアン、上が π/2）から、付け根がこぶに張って、先は裂けて折れる。</summary>
        static void Limb(NatureMeshKit kit, System.Func<float, Vector3> path, System.Func<float, float> radius, float t, float a, float l, float r, Vector3 bend, int seed)
        {
            Vector3 c = path(t), d = (path(t + 0.01f) - path(t - 0.01f)).normalized;
            var side = Vector3.Cross(d, Vector3.up).normalized; var up = Vector3.Cross(side, d);
            var outDir = side * Mathf.Cos(a) + up * Mathf.Sin(a);
            var dir = (outDir + d * 0.35f).normalized;
            Vector3 start = c + outDir * radius(t) * 0.7f;
            PropKit.Pipe(kit, u => start + dir * (l * u) + bend * (l * u * u), (u, ang) =>
                r * (1f + 0.8f * Mathf.Exp(-u * l / 0.12f)) * Mathf.Lerp(1f, 0.7f, u) * (u > 0.93f ? 1f - 0.4f * Mathf.Abs(Mathf.Sin(ang * 2.5f + seed)) : 1f)
                  * PropKit.BarkPlates(u, ang, l, seed, 8f, 0.06f), Mathf.Max(5, (int)(l * 8f)), 12, BARK, -1, CUT, -0.05f);
        }

        /// <summary>めくれた樹皮：幹の面から、t0〜t1・角度 a0〜a1 の板が、片側のふちから浮き上がる（厚み 3 cm の三日月の断面）。</summary>
        static void BarkSheet(NatureMeshKit kit, System.Func<float, Vector3> path, System.Func<float, float> radius, float t0, float t1, float a0, float a1, float lift)
        {
            PropKit.Surface(kit, (t, k) =>
            {
                float tt = Mathf.Lerp(t0, t1, t);
                Vector3 c = path(tt), d = (path(tt + 0.01f) - path(tt - 0.01f)).normalized;
                var side = Vector3.Cross(d, Vector3.up).normalized; var up = Vector3.Cross(side, d);
                // k runs round a thin crescent: out along the outer face, back along the inner face
                bool outer = k < Mathf.PI;
                float u = outer ? k / Mathf.PI : 2f - k / Mathf.PI;
                float a = Mathf.Lerp(a0, a1, u);
                float r = radius(tt) * 1.04f + lift * u * u * (0.6f + 0.4f * Mathf.Sin(t * Mathf.PI)) + (outer ? 0.03f : 0f);
                return c + (side * Mathf.Cos(a) + up * Mathf.Sin(a)) * r;
            }, 6, 14, BARKDARK, BARKDARK, BARKDARK);
        }

        static string FallenLog(int v)
        {
            var kit = Kit();
            const float R = 0.75f;
            if (v == 0)
            {
                // a whole tree come down: root end with buttresses and torn roots, a long trunk sagging into the ground,
                // broken limbs, one dead branch still reaching up, the top end snapped
                float L = 9f;
                System.Func<float, Vector3> path = t => new Vector3(-L * 0.5f + L * t, R * 0.82f + 0.25f * Mathf.Exp(-t * 6f) - 0.12f * Mathf.Sin(Mathf.PI * t), 0.35f * Mathf.Sin(t * 2.6f));
                System.Func<float, float> rad = t => R * Mathf.Lerp(1.08f, 0.72f, t);
                Log(kit, path, rad, L, End.Roots, End.Snapped, 160);
                Limb(kit, path, rad, 0.42f, 1.9f, 0.7f, 0.17f, Vector3.zero, 161);
                Limb(kit, path, rad, 0.58f, 0.5f, 1.1f, 0.15f, Vector3.down * 0.3f, 162);
                Limb(kit, path, rad, 0.7f, 2.6f, 0.5f, 0.13f, Vector3.zero, 163);
                // the dead branch still standing up from the top side
                Vector3 bp = path(0.8f) + Vector3.up * rad(0.8f) * 0.8f;
                PropKit.Branch(kit, bp, new Vector3(0.35f, 1f, 0.15f), 1.3f, 0.12f, 2, 164, 9, BARK, 32f, 0.05f);
                BarkSheet(kit, path, rad, 0.3f, 0.42f, -0.2f, 0.9f, 0.12f);
            }
            else if (v == 1)
            {
                // an old hollow log: sawn-flat butt, the far end hollow, a long strip of bark peeling off, brackets of fungus marching along
                float L = 8f;
                System.Func<float, Vector3> path = t => new Vector3(-L * 0.5f + L * t, R * 0.85f - 0.05f * Mathf.Sin(Mathf.PI * t), 0.2f * Mathf.Sin(t * 3.2f + 1f));
                System.Func<float, float> rad = t => R * Mathf.Lerp(1.02f, 0.9f, t);
                Log(kit, path, rad, L, End.Flat, End.Hollow, 165);
                BarkSheet(kit, path, rad, 0.3f, 0.5f, 0.4f, 1.6f, 0.32f);
                BarkSheet(kit, path, rad, 0.55f, 0.7f, 2.0f, 2.9f, 0.1f);
                for (int i = 0; i < 6; i++)
                {
                    float t = 0.3f + i * 0.08f;
                    Vector3 c = path(t), d = (path(t + 0.01f) - path(t - 0.01f)).normalized;
                    var outDir = Vector3.Cross(d, Vector3.up).normalized;
                    Shelf(kit, c + outDir * rad(t) * 1.02f + Vector3.up * (0.1f + 0.12f * (i % 2)), outDir, PropKit.R(166, i, 0.13f, 0.22f));
                }
                Limb(kit, path, rad, 0.5f, 1.4f, 0.45f, 0.14f, Vector3.zero, 167);
                Limb(kit, path, rad, 0.22f, 1.75f, 1.2f, 0.2f, new Vector3(0.1f, -0.15f, 0f), 172);
                Limb(kit, path, rad, 0.78f, -0.3f, 0.8f, 0.16f, Vector3.down * 0.4f, 173);
                // one dark knot-hole on the top
                Vector3 kc = path(0.62f) + Vector3.up * rad(0.62f) * 1.0f;
                kit.Ball(kc, new Vector3(0.12f, 0.04f, 0.09f), 5, ROT);
            }
            else
            {
                // snapped in two: a long piece and a short one slewed aside, the breaks splintered,
                // a blanket of moss along the top of the long piece
                float L1 = 5.5f, L2 = 3.2f;
                System.Func<float, Vector3> p1 = t => new Vector3(-L1 + L1 * t, R * 0.85f - 0.1f * Mathf.Sin(Mathf.PI * t), 0.15f * Mathf.Sin(t * 2f));
                System.Func<float, float> r1 = t => R * Mathf.Lerp(1.05f, 0.95f, t);
                Log(kit, p1, r1, L1, End.Roots, End.Snapped, 168);
                // the short piece: its break near the long one's break, its far end sawn flat
                Vector3 brk = new Vector3(0.7f, R * 0.78f, 0.6f), dir = PropKit.Dir(35f);
                System.Func<float, Vector3> p2 = t => brk + dir * (L2 * (1f - t)) + Vector3.down * (0.05f * (1f - t));
                Log(kit, p2, t => R * Mathf.Lerp(0.82f, 0.92f, t), L2, End.Flat, End.Snapped, 169);
                // the moss blanket: a lumpy strip draped over the top of the long piece, wide in the middle
                PropKit.Surface(kit, (t, a) =>
                {
                    float tt = Mathf.Lerp(0.18f, 0.88f, t);
                    Vector3 c = p1(tt);
                    float spread = 1.05f * Mathf.Sin(Mathf.PI * t) + 0.2f;
                    float ang = Mathf.PI * 0.5f - Mathf.Clamp(Mathf.Cos(a) * spread, -1.4f, 1.4f);
                    float r = r1(tt) * (1.06f + 0.05f * Mathf.Max(0f, Mathf.Sin(a)) + 0.03f * PropKit.Noise(new Vector3(tt * 20f, a * 2f, 0f), 170));
                    return c + new Vector3(0f, Mathf.Sin(ang), Mathf.Cos(ang)) * r;
                }, 30, 12, MOSS);
                Limb(kit, p1, r1, 0.35f, 0.2f, 0.6f, 0.15f, Vector3.zero, 171);
            }
            return Save($"FallenLog_{Abc(v)}", kit);
        }

        static string RootPlate(int v)
        {
            var kit = Kit();
            float D = v == 0 ? 5f : v == 1 ? 4f : 6f, tilt = v == 1 ? 20f : 0f;
            float Rp = D * 0.5f;
            var q = Quaternion.Euler(tilt, 0f, 0f);
            Vector3 c = new Vector3(0f, Rp * (v == 1 ? 0.8f : 0.85f), 0f);
            var face = q * Vector3.back;
            // the pit the roots tore out of, in front of the plate: a low bank of loose earth round a scooped floor
            Vector3 pit = new Vector3(0f, 0f, -Rp * 0.55f);
            PropKit.Surface(kit, (t, a) =>
            {
                float r = Rp * 1.05f * t;
                float y = 0.28f * Mathf.Exp(-Mathf.Pow((r - Rp * 0.7f) / (Rp * 0.22f), 2f)) * (1f + 0.4f * PropKit.Noise(new Vector3(Mathf.Cos(a) * 3f, 0f, Mathf.Sin(a) * 3f), 170)) + 0.015f;
                return pit + new Vector3(Mathf.Cos(a) * r, y, -Mathf.Sin(a) * r * 0.7f);
            }, 12, 28, DIRT);
            // the plate of earth: a fat disc standing on its edge, its rim ragged and lobed
            PropKit.Lump(kit, c, new Vector3(Rp, Rp, 0.45f), 16, 32, DIRT, 170 + v, 0.3f, 1.8f, 0f, q, 0f, 1f,
                n =>
                {
                    // an uneven rim: a few big bites and bulges, not a regular star
                    float th = Mathf.Atan2(n.y, n.x);
                    float rim = 0.08f * Mathf.Sin(3f * th + v) + 0.06f * Mathf.Sin(7f * th + 2f * v) + 0.12f * PropKit.Noise(new Vector3(Mathf.Cos(th), Mathf.Sin(th), 0f) * 2.5f, 174 + v);
                    return rim * (1f - Mathf.Abs(n.z)) + PropKit.Cracks(n, 175 + v, 0.06f, 5f);
                });
            // main roots: thick, from the middle where the trunk was, radiating across the face and out past the rim
            for (int i = 0; i < 6; i++)
            {
                float ang = i * Mathf.PI * 2f / 6f + PropKit.R(171, i, -0.3f, 0.3f);
                var radial = q * new Vector3(Mathf.Cos(ang), Mathf.Sin(ang), 0f);
                float reach = Rp * PropKit.R(172, i, 0.95f, v == 2 ? 1.35f : 1.15f), r = 0.2f * D / 5f;
                bool hang = v == 2 && radial.y < 0.1f;
                int ii = i;
                PropKit.Pipe(kit, t =>
                {
                    var p = c + face * (0.42f + 0.05f * Mathf.Sin(t * 5f + ii) + Mathf.Max(0f, t - 0.8f) * 0.8f) + radial * (reach * t);
                    if (hang && t > 0.7f) p += Vector3.down * ((t - 0.7f) * Rp * 0.9f);
                    p.y = Mathf.Max(p.y, r * 0.4f);
                    return p;
                }, (t, a2) => r * Mathf.Lerp(1.4f, 0.25f, t) * Bark(t, a2, reach, 180 + ii, 6f), 12, 12, BARK, BARK, CUT, 0f, null, Grooves(reach, 180 + ii, 6f));
            }
            // side roots: thinner, starting anywhere on the face, wandering outward; the lower ones hang (C)
            int roots = v == 2 ? 20 : 14;
            for (int i = 0; i < roots; i++)
            {
                float ang = PropKit.R(173, i, 0f, Mathf.PI * 2f);
                var radial = q * new Vector3(Mathf.Cos(ang), Mathf.Sin(ang), 0f);
                var across = q * new Vector3(-Mathf.Sin(ang), Mathf.Cos(ang), 0f);
                float r0 = Rp * PropKit.R(174, i, 0.2f, 0.7f), reach = Rp * PropKit.R(175, i, 0.4f, v == 2 ? 1.0f : 0.8f);
                float r = PropKit.R(176, i, 0.04f, 0.08f) * D / 5f, wav = PropKit.R(177, i, -0.25f, 0.25f), ph = PropKit.R(178, i, 0f, 6f);
                bool hanging = v == 2 && radial.y < 0f;
                PropKit.Pipe(kit, t =>
                {
                    float d = r0 + reach * t;
                    var p = c + face * (0.4f + 0.06f * Mathf.Sin(t * 7f + ph) + Mathf.Max(0f, d - Rp) * 0.5f) + radial * d + across * (wav * reach * Mathf.Sin(t * 3f + ph));
                    if (hanging && d > Rp * 0.8f) p += Vector3.down * ((d - Rp * 0.8f) * 1.4f);
                    p.y = Mathf.Max(p.y, r * 0.5f);
                    return p;
                }, (t, a2) => r * Mathf.Lerp(1.2f, 0.2f, t), 8, 7, BARK, BARK, BARK);
            }
            // clods of earth caught on the face between the roots, and stones held in it
            for (int i = 0; i < 9; i++)
            {
                var p = c + q * new Vector3(PropKit.R(186, i, -0.75f, 0.75f) * Rp, PropKit.R(187, i, -0.75f, 0.75f) * Rp, -0.42f);
                float r = PropKit.R(188, i, 0.15f, 0.32f);
                PropKit.Lump(kit, p, new Vector3(r, r, r * 0.6f), 5, 9, i < (v == 2 ? 4 : 6) ? DIRT : STONE, 189 + i, 0.3f, 2f, 0f, q);
            }
            // the trunk lying away from the plate (+Z), its far end snapped, a couple of broken limbs
            float L = v == 1 ? 4f : 5.5f;
            Vector3 t0 = c + q * Vector3.forward * 0.2f;
            System.Func<float, Vector3> path = t => Vector3.Lerp(t0, new Vector3(0.3f, 0.6f, t0.z + L), t) + Vector3.up * (0.15f * Mathf.Sin(Mathf.PI * t));
            System.Func<float, float> rad = t => Mathf.Lerp(0.7f, 0.55f, t) * D / 5f;
            Log(kit, path, rad, L, End.Flat, End.Snapped, 190 + v, 48);
            Limb(kit, path, rad, 0.45f, 1.2f, 0.9f, 0.13f, Vector3.zero, 193 + v);
            Limb(kit, path, rad, 0.7f, -0.1f, 0.7f, 0.11f, Vector3.down * 0.3f, 196 + v);
            return Save($"RootPlate_{Abc(v)}", kit);
        }

        static string DeadBranch(int v)
        {
            var kit = Kit();
            if (v == 0)
                for (int i = 0; i < 4; i++)
                    PropKit.Branch(kit, Vector3.zero, (PropKit.Dir(i * 90f + 20f) + Vector3.up * 0.55f), 0.45f, 0.035f, 2, 190 + i, 6, DRIFT, 40f, 0.25f);
            else if (v == 1)
            {
                PropKit.Branch(kit, new Vector3(-1f, 0.05f, 0f), new Vector3(1f, 0.04f, 0.1f), 1.1f, 0.07f, 2, 195, 7, DRIFT, 32f, 0.04f);
                PropKit.Lump(kit, new Vector3(-1.05f, 0.08f, 0f), new Vector3(0.1f, 0.09f, 0.1f), 3, 6, DRIFT, 196, 0.3f, 2f, 0f);
            }
            else
            {
                PropKit.Pipe(kit, t => new Vector3(0.12f * Mathf.Sin(t * 4f), 1.0f * t, 0.08f * t), (t, a) => Mathf.Lerp(0.09f, 0.06f, t), 5, 7, DRIFT, DRIFT, -1);
                for (int i = 0; i < 3; i++)
                {
                    var at = new Vector3(0.12f * Mathf.Sin((0.55f + i * 0.2f) * 4f), 0.55f + i * 0.22f, 0.04f);
                    PropKit.Branch(kit, at, PropKit.Dir(i * 130f + 30f) + Vector3.up * (0.6f + 0.3f * i), 0.45f - i * 0.08f, 0.05f, 1, 197 + i, 6, DRIFT, 45f, 0.1f);
                }
                PropKit.Branch(kit, new Vector3(0.05f, 0.95f, 0.08f), new Vector3(0.2f, 1f, 0f), 0.35f, 0.055f, 1, 201, 6, DRIFT, 40f);
            }
            return Save($"DeadBranch_{Abc(v)}", kit, 0);
        }
    }
}
