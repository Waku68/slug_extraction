using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace SlugDesign
{
    /// <summary>
    /// ナメクジの暮らしの跡（Nov 2 下書き、3 案ずつ。docs/ideas/map_clutter_ideas.md の D。人の跡は出さない）。
    /// メニュー Slug/Nature/Props/Build Slug Traces で作り直す（Assets/Art/Nature/SlugTraces）。
    /// 流木・石・殻のかけら・炭・塩の岩はかじれないのでカクカク、縄・編んだ籠・灰・苔はなめらか。ナメクジの目の高さ 0.47 m は人の約 1/3.6。
    /// - RopeRail 綱の手すり（山のつづら折り）：流木の杭 高さ 0.5 m（ナメクジの目の少し上）＋ヤシ縄。A 杭 3 本・1.5 m おき。B つづら折りの角で杭 5 本。C 真ん中の杭が倒れ、縄が地面に垂れる
    /// - RestShelter 休み場の跡（山の平場）：流木の骨組みに殻のかけらを瓦のように葺いた屋根、幅 1.8 m・高さ 0.9 m（ナメクジ 1〜2 匹）。
    ///   A 片側の屋根が残り、もう片側は落ちてかけらが散る。B 岩に立てかけた片屋根、半分残る。C すっかり崩れて、骨組みとかけらの山
    /// - Campfire 焚き火の跡：石の輪 直径 0.6 m、灰と炭。A きれいな輪。B 石が散り、三脚の棒が倒れ、割れた殻の椀。C 古い跡（石が減り、苔）
    /// - DryingRack 古い干し台（塩田のふち）：流木の台 長さ 1.6 m・高さ 0.75 m、桟と縄。A 立っていて、桟がところどころ抜ける。B 片側の脚が折れて傾く。C 崩れて地面に散る
    /// - Basket 捨てられた籠：編んだ籠 直径 0.65 m・高さ 0.5 m。A 立っていて、横に破れ穴、切れた背負い紐。B 横に倒れ、口から中身（塩の粒）がこぼれる。C つぶれて破れた籠
    /// - DigMark 古い掘り跡（岩塩の山）：A つるはしの溝が残る塩の岩の面 1.4 m と、足元のかけら。B かけらの山と、折れたつるはしの柄。C 浅い掘り穴（土手の輪 直径 2.4 m）
    /// - OldChannel 乾いた昔の水路（地形に置いてつなぐ部品、長さ 3 m・幅 0.6 m）：石の壁 2 段と、底に白い塩の筋。A まっすぐ。B 曲がり（90°）。C 崩れて石が落ち、砂が入る
    /// 荒れ方（Nov 2 わくさん「もっと荒廃させて」、無難の案）：形は残して何だったかは分かるまま、もっと崩し（傾く・抜ける・倒れる）、
    /// 地面に沈めて半分埋め、根元に土が吹きだまり、苔が乗り、草が中を突き抜けて生え、杭や脚にはつるが巻く（Ruin・Vine）。
    /// 草・苔・つるはケルン風（README 8章）。
    /// 色：流木 #C4BBAA、縄 #B8A272、殻のかけら #D8CDB8・暗いかけら #A89A84、石 #A39C8E・#7E786C、炭 #3A3430、灰 #9A948C、苔 #73804C、
    /// 籠 #B09A68・#8A7650、塩の粒 #E9E3DA、塩の岩 #CFC6B8、土 #9A8670、草 #8A9A58・根元 #6E7E48、つる #5E6A3C・つるの葉 #7E9650。
    /// </summary>
    public static class SlugTraceBuilder
    {
        const string Dir = "Assets/Art/Nature/SlugTraces";
        const int WOOD = 0, ROPE = 1, SHARD = 2, SHARDDARK = 3, STONE = 4, STONEDARK = 5, CHAR = 6, ASH = 7, MOSS = 8,
                  WEAVE = 9, WEAVEDARK = 10, SALT = 11, SALTROCK = 12, DIRT = 13, GRASS = 14, GRASSBASE = 15, VINE = 16, VINELEAF = 17;
        static Material[] _m;

        [MenuItem("Slug/Nature/Props/Build Slug Traces")]
        public static void Build()
        {
            _m = PropKit.Mats(Dir, "Traces",
                new PropKit.Spec("Driftwood", "#C4BBAA", true).Mottle("#A39A8A", 0.55f, 6f).Felt(0.25f).Dent(0.3f, 40f),
                new PropKit.Spec("Rope", "#B8A272", false).Mottle("#9A8458", 0.4f, 40f).Felt(0.5f),
                new PropKit.Spec("Shard", "#D8CDB8", true, 0f, true).Mottle("#B8AA90", 0.55f, 12f).Camo("#E2D8C4", "#C8BCA4", "#D0B8A4", 9f, 0.5f).Dent(0.3f, 60f),
                new PropKit.Spec("ShardDark", "#A89A84", true, 0f, true).Mottle("#8A7E6A", 0.55f, 12f).Dent(0.3f, 60f),
                new PropKit.Spec("Stone", "#A39C8E", true).Mottle("#88827A", 0.5f, 6f).Camo("#ADA799", "#928C80", "#B9B596", 7f, 0.6f).Dent(0.3f, 30f),
                new PropKit.Spec("StoneDark", "#7E786C", true).Mottle("#68635A", 0.5f, 6f).Camo("#88827A", "#6E695F", "#959276", 7f, 0.6f).Dent(0.3f, 30f),
                new PropKit.Spec("Char", "#3A3430", true).Mottle("#24201E", 0.6f, 30f).Dent(1.0f, 90f),
                new PropKit.Spec("Ash", "#9A948C", false).Mottle("#B4AEA6", 0.6f, 25f).Felt(0.6f),
                new PropKit.Spec("Moss", "#73804C", false).Mottle("#7E8C52", 0.3f, 5f),
                new PropKit.Spec("Weave", "#B09A68", false, 0f, true).Mottle("#94804E", 0.45f, 20f).Felt(0.5f),
                new PropKit.Spec("WeaveDark", "#8A7650", false, 0f, true).Mottle("#6E5C3C", 0.45f, 20f).Felt(0.5f),
                new PropKit.Spec("Salt", "#E9E3DA", true).Mottle("#D2CABC", 0.5f, 20f).Felt(0.3f),
                new PropKit.Spec("SaltRock", "#CFC6B8", true).Mottle("#B0A696", 0.55f, 6f).Camo("#D8D0C4", "#BCB2A2", "#C8C0B0", 6f, 0.5f).Dent(0.35f, 30f),
                new PropKit.Spec("Dirt", "#9A8670", true).Mottle("#7E6C58", 0.65f, 10f).Felt(0.4f).Dent(0.6f, 50f),
                new PropKit.Spec("Grass", "#8A9A58", false, 0.1f, true).Mottle("#9AAA62", 0.25f, 3f),
                new PropKit.Spec("GrassBase", "#6E7E48", false, 0.02f),
                new PropKit.Spec("Vine", "#5E6A3C", false, 0.01f),
                new PropKit.Spec("VineLeaf", "#7E9650", false, 0.04f, true));
            foreach (int i in new[] { MOSS, GRASS, GRASSBASE, VINE, VINELEAF }) PropKit.CairnPaint(_m[i]);
            var log = new StringBuilder("Slug traces built:");
            for (int v = 0; v < 3; v++)
                log.Append(' ').Append(RopeRail(v)).Append(' ').Append(Shelter(v)).Append(' ').Append(Campfire(v)).Append(' ').Append(Rack(v))
                   .Append(' ').Append(Basket(v)).Append(' ').Append(DigMark(v)).Append(' ').Append(Channel(v));
            AssetDatabase.SaveAssets();
            Debug.Log(log);
        }

        static string Abc(int v) => ((char)('A' + v)).ToString();
        static NatureMeshKit Kit() => new NatureMeshKit(_m.Length);
        static string Save(string name, NatureMeshKit kit, int collider = 3) => PropKit.Save(Dir, name, kit, _m, collider);

        /// <summary>p を pivot のまわりに axis で deg 度回す（骨組みを傾ける・倒す）。</summary>
        static Vector3 Lean(Vector3 p, Vector3 pivot, Vector3 axis, float deg) => pivot + Quaternion.AngleAxis(deg, axis) * (p - pivot);

        /// <summary>
        /// 荒れた跡にする：from 番から後に作った物を、ところどころ sink だけ地面に沈め（半分埋まる）、そのあと
        /// 物の上に苔のかたまり moss 個、根元に土の吹きだまり drifts 個、まわりと中に草の株 tufts 個を足す（spread だけ外まで）。
        /// 苔は物の高い所の頂点から選ぶ（作ったばかりの頂点だけを見る）。
        /// </summary>
        static void Ruin(NatureMeshKit kit, int from, int seed, float sink, int moss, int tufts, int drifts, float spread = 0.3f)
        {
            int to = kit.VertexCount;
            Vector3 mn = Vector3.one * 1e5f, mx = -Vector3.one * 1e5f;
            for (int i = from; i < to; i++) { var p = kit.Vertex(i); mn = Vector3.Min(mn, p); mx = Vector3.Max(mx, p); }
            // settle: everything sinks a patchy amount into the ground
            kit.Transform(from, p => p - Vector3.up * (sink * (0.55f + 0.45f * PropKit.Noise(new Vector3(p.x * 1.3f, 0f, p.z * 1.3f), seed))));
            // moss on the high places of the thing (picked among its own vertices)
            int placed = 0;
            for (int k = 0; k < moss * 6 && placed < moss; k++)
            {
                var p = kit.Vertex(from + (int)(PropKit.H(seed + 1, k) * (to - from - 1)));
                if (p.y < 0.04f) continue;
                float r = PropKit.R(seed + 2, k, 0.06f, 0.14f);
                int mf = kit.VertexCount;
                var c = p + Vector3.up * r * 0.15f;
                PropKit.Lump(kit, c, new Vector3(r, r * 0.4f, r * 0.85f), 4, 10, MOSS, seed + 10 + k, 0.35f, 3f, float.NaN, Quaternion.Euler(0f, k * 47f, 0f));
                kit.Mass(mf, c, new Vector3(r, r * 0.6f, r), 0.8f);
                placed++;
            }
            // drifts of soil heaped against the base
            for (int k = 0; k < drifts; k++)
            {
                float ex = Mathf.Lerp(mn.x, mx.x, PropKit.H(seed + 3, k)), ez = Mathf.Lerp(mn.z, mx.z, PropKit.H(seed + 4, k));
                var r = new Vector3(PropKit.R(seed + 5, k, 0.22f, 0.5f), PropKit.R(seed + 6, k, 0.03f, 0.08f), PropKit.R(seed + 7, k, 0.18f, 0.4f));
                PropKit.Lump(kit, new Vector3(ex, 0f, ez), r, 5, 12, DIRT, seed + 30 + k, 0.25f, 2f, 0f, Quaternion.Euler(0f, k * 61f, 0f));
            }
            // grass growing up round it and through it
            for (int k = 0; k < tufts; k++)
            {
                float ex = Mathf.Lerp(mn.x - spread, mx.x + spread, PropKit.H(seed + 8, k)), ez = Mathf.Lerp(mn.z - spread, mx.z + spread, PropKit.H(seed + 9, k));
                float h = PropKit.R(seed + 11, k, 0.14f, 0.42f);
                PropKit.GrassTuft(kit, new Vector3(ex, 0f, ez), h * 0.22f, h, 6 + (int)(h * 18f), seed + 50 + k * 13, GRASSBASE, GRASS, default, 0.022f + h * 0.03f);
            }
        }

        /// <summary>つる：a（根元）から b へ、太さ poleR の棒に turns 回巻きついて登り、丸い葉が並ぶ。</summary>
        static void Vine(NatureMeshKit kit, Vector3 a, Vector3 b, float poleR, int seed, float turns = 2f)
        {
            Vector3 axis = b - a, dir = axis.normalized;
            Vector3 side = Vector3.Cross(dir, Mathf.Abs(dir.y) > 0.8f ? Vector3.forward : Vector3.up).normalized, up = Vector3.Cross(side, dir);
            float ph = PropKit.R(seed, 0, 0f, 6f), rr = poleR + 0.012f;
            System.Func<float, Vector3> path = t =>
            {
                float th = ph + t * turns * Mathf.PI * 2f;
                return a + axis * t + (side * Mathf.Cos(th) + up * Mathf.Sin(th)) * rr;
            };
            PropKit.Pipe(kit, path, (t, ang) => Mathf.Lerp(0.009f, 0.004f, t), Mathf.Max(16, (int)(turns * 14f)), 5, VINE, VINE, VINE);
            int n = Mathf.Max(4, (int)(axis.magnitude * 14f));
            for (int i = 0; i < n; i++)
            {
                float t = (i + 0.5f) / n;
                var p = path(t);
                var outDir = (p - (a + axis * t)).normalized;
                var ld = (outDir + Vector3.up * 0.4f).normalized;
                float l = PropKit.R(seed + 1, i, 0.035f, 0.055f) * (1f - 0.4f * t);
                int bf = kit.VertexCount;
                kit.Blade(q => p + ld * (l * q) - Vector3.up * (0.3f * l * q * q), q => l * 0.45f * Mathf.Pow(Mathf.Max(0f, Mathf.Sin(Mathf.PI * Mathf.Min(1f, q * 0.9f + 0.1f))), 0.6f), 3, 0.004f, 0.2f, VINELEAF);
                int ii = i;
                kit.Paint(bf, (q, uv) => new Color(0.2f + 0.3f * uv.y, uv.y, PropKit.H(seed + 2, ii), 1f));
            }
        }

        /// <summary>流木の棒：a から b、太さ r。少し曲がり、太さがゆれる。</summary>
        static void Pole(NatureMeshKit kit, Vector3 a, Vector3 b, float r, int seed)
        {
            Vector3 d = b - a, side = Vector3.Cross(d.normalized, Mathf.Abs(d.normalized.y) > 0.8f ? Vector3.forward : Vector3.up).normalized;
            Vector3 mid = Vector3.Lerp(a, b, 0.5f) + side * (d.magnitude * PropKit.R(seed, 0, -0.04f, 0.04f));
            PropKit.Pipe(kit, t => (1 - t) * (1 - t) * a + 2 * (1 - t) * t * mid + t * t * b,
                (t, ang) => r * (1f + 0.12f * PropKit.Noise(new Vector3(t * d.magnitude * 3f, Mathf.Cos(ang), Mathf.Sin(ang)), seed)) * PropKit.Grain(t, ang, d.magnitude, seed, 0.4f, 0.12f),
                Mathf.Max(6, (int)(d.magnitude * 14f)), 10, WOOD, WOOD, WOOD, r * 0.4f);
        }

        /// <summary>縄のたるみ：p0 から p1 へ、真ん中で sag だけ下がる（地面より下には行かない）。</summary>
        static void Rope(NatureMeshKit kit, Vector3 p0, Vector3 p1, float sag, float r = 0.014f)
        {
            PropKit.Pipe(kit, t =>
            {
                var p = Vector3.Lerp(p0, p1, t) - Vector3.up * (sag * 4f * t * (1f - t));
                p.y = Mathf.Max(p.y, r);
                return p;
            }, (t, a) => r * (1f + 0.3f * Mathf.Abs(Mathf.Sin(a * 1.5f + t * (p1 - p0).magnitude * 70f))), Mathf.Max(12, (int)((p1 - p0).magnitude * 50f)), 9, ROPE, ROPE, ROPE);
        }

        /// <summary>棒に巻いた縄の輪（at の高さで、棒の太さ r のまわりに 2 周）。</summary>
        static void Lashing(NatureMeshKit kit, Vector3 at, float r)
        {
            for (int k = 0; k < 2; k++)
            {
                Vector3 c = at + Vector3.up * (k * 0.025f);
                PropKit.Pipe(kit, t => c + new Vector3(Mathf.Cos(t * Mathf.PI * 2f), 0.01f * t, Mathf.Sin(t * Mathf.PI * 2f)) * (r + 0.01f), (t, a) => 0.011f, 10, 5, ROPE);
            }
        }

        static void Stake(NatureMeshKit kit, Vector3 foot, float h, float lean, float yaw, int seed, out Vector3 top)
        {
            top = foot + Vector3.up * h + PropKit.Dir(yaw) * lean;
            Pole(kit, foot - Vector3.up * 0.08f, top, 0.04f, seed);
            Lashing(kit, top - Vector3.up * 0.08f, 0.04f);
        }

        static string RopeRail(int v)
        {
            var kit = Kit();
            var tops = new List<Vector3>();
            if (v == 0)
                for (int i = 0; i < 3; i++) { Stake(kit, new Vector3(-1.5f + 1.5f * i, 0f, PropKit.R(700, i, -0.05f, 0.05f)), PropKit.R(701, i, 0.42f, 0.54f), PropKit.R(709, i, 0.1f, 0.2f), PropKit.R(702, i, 0f, 360f), 703 + i, out var t); tops.Add(t); }
            else if (v == 1)
            {
                // the outside of a switchback corner: the rail comes in along X and leaves back along -X, a little higher
                var feet = new[] { new Vector3(-1.6f, 0f, -0.6f), new Vector3(-0.3f, 0f, -0.75f), new Vector3(0.6f, 0f, -0.2f), new Vector3(0.4f, 0f, 0.7f), new Vector3(-0.8f, 0f, 1.0f) };
                for (int i = 0; i < feet.Length; i++)
                {
                    if (i == 3) { Pole(kit, feet[i] + Vector3.up * 0.04f, feet[i] + new Vector3(0.45f, 0.04f, 0.15f), 0.04f, 718); tops.Add(feet[i] + new Vector3(0.45f, 0.06f, 0.15f)); continue; }   // this one fell
                    Stake(kit, feet[i], PropKit.R(704, i, 0.4f, 0.56f), PropKit.R(719, i, 0.08f, 0.2f), PropKit.R(705, i, 0f, 360f), 706 + i, out var t); tops.Add(t);
                }
            }
            else
            {
                Stake(kit, new Vector3(-1.5f, 0f, 0f), 0.5f, 0.04f, 30f, 711, out var t0);
                Stake(kit, new Vector3(1.5f, 0f, 0.05f), 0.48f, 0.12f, 200f, 712, out var t2);
                // the middle stake has fallen over beside its hole; the rope drapes to the ground and the far end is frayed loose
                Pole(kit, new Vector3(0.05f, 0.04f, 0.1f), new Vector3(0.45f, 0.04f, 0.5f), 0.04f, 713);
                PropKit.Lump(kit, new Vector3(0f, 0f, 0f), new Vector3(0.08f, 0.03f, 0.08f), 2, 8, STONEDARK, 714, 0.3f, 2f, 0f);
                Rope(kit, t0 - Vector3.up * 0.06f, new Vector3(0.3f, 0.02f, 0.25f), 0.25f);
                Rope(kit, new Vector3(0.3f, 0.02f, 0.25f), new Vector3(0.9f, 0.015f, 0.4f), 0f);
                Rope(kit, t2 - Vector3.up * 0.06f, t2 + new Vector3(-0.08f, -0.3f, 0.05f), 0f);
            }
            for (int i = 0; i + 1 < tops.Count; i++) Rope(kit, tops[i] - Vector3.up * 0.06f, tops[i + 1] - Vector3.up * 0.06f, 0.24f);
            var vineFoot = v == 2 ? new Vector3(-1.5f, 0f, 0f) : tops.Count > 0 ? new Vector3(tops[0].x, 0f, tops[0].z) : Vector3.zero;
            Vine(kit, vineFoot, vineFoot + Vector3.up * 0.4f, 0.045f, 715);
            Ruin(kit, 0, 716 + v, 0.06f, 6, v == 1 ? 14 : 10, 2);
            return Save($"RopeRail_{Abc(v)}", kit);
        }

        /// <summary>殻のかけら 1 枚：少し反った薄い板（幅 w・長さ l）。at に、向き q で。</summary>
        static void Shard(NatureMeshKit kit, Vector3 at, Quaternion q, float w, float l, int seed)
        {
            int from = kit.VertexCount;
            PropKit.Lump(kit, Vector3.zero, new Vector3(w, 0.012f, l), 3, 9, PropKit.H(seed, 0) < 0.3f ? SHARDDARK : SHARD, seed, 0.35f, 2.5f, float.NaN, null, 0f, 1f,
                n => 0.4f * PropKit.Noise(new Vector3(n.x, 0f, n.z) * 2.2f, seed + 3) * (1f - Mathf.Abs(n.y)), null, 3f);
            kit.Transform(from, p => at + q * (p + Vector3.up * (0.6f * (p.x * p.x + p.z * p.z) / Mathf.Max(w, l))));
        }

        /// <summary>
        /// 屋根の面に殻のかけらを瓦のように葺く：軒 eave から棟 ridge へ、幅 width（X に沿って）。keep(u, v) が false の所は抜ける（落ちた）。
        /// </summary>
        static void Shingles(NatureMeshKit kit, Vector3 eaveMid, Vector3 ridgeMid, float width, System.Func<float, float, bool> keep, int seed)
        {
            Vector3 up = ridgeMid - eaveMid, slope = up.normalized;
            Vector3 across = Vector3.right, normal = Vector3.Cross(across, slope).normalized;
            if (normal.y < 0f) normal = -normal;
            int cols = Mathf.RoundToInt(width / 0.15f), rows = Mathf.Max(2, Mathf.RoundToInt(up.magnitude / 0.12f));
            var q = Quaternion.LookRotation(slope, normal);
            for (int r = 0; r < rows; r++)
                for (int c = 0; c < cols; c++)
                {
                    float u = (c + 0.5f + (r % 2) * 0.5f) / cols - 0.5f, vv = (r + 0.5f) / rows;
                    if (Mathf.Abs(u) > 0.5f || !keep(u + 0.5f, vv)) continue;
                    int s = seed + r * 31 + c;
                    var p = eaveMid + across * (u * width) + up * vv + normal * (0.05f + 0.008f * (rows - r));
                    Shard(kit, p, q * Quaternion.Euler(PropKit.R(s, 1, -10f, 0f), PropKit.R(s, 2, -12f, 12f), PropKit.R(s, 3, -6f, 6f)), PropKit.R(s, 4, 0.08f, 0.1f), PropKit.R(s, 5, 0.09f, 0.12f), s);
                }
        }

        /// <summary>落ちたかけらを地面に散らす（真ん中 c、広さ rx・rz、n 枚、山になる heap）。</summary>
        static void FallenShards(NatureMeshKit kit, Vector3 c, float rx, float rz, int n, int seed, float heap = 0f)
        {
            for (int i = 0; i < n; i++)
            {
                float a = PropKit.R(seed, i, 0f, Mathf.PI * 2f), d = Mathf.Sqrt(PropKit.H(seed + 1, i));
                float y = 0.015f + heap * (1f - d) * PropKit.H(seed + 2, i);
                var p = c + new Vector3(Mathf.Cos(a) * d * rx, y, Mathf.Sin(a) * d * rz);
                Shard(kit, p, Quaternion.Euler(PropKit.R(seed + 3, i, -25f, 25f), PropKit.R(seed + 4, i, 0f, 360f), PropKit.R(seed + 5, i, -25f, 25f)), PropKit.R(seed + 6, i, 0.09f, 0.14f), PropKit.R(seed + 7, i, 0.1f, 0.16f), seed + 50 + i);
            }
        }

        static string Shelter(int v)
        {
            var kit = Kit();
            const float W = 1.8f, H = 0.85f, D = 1.0f;
            if (v == 0)
            {
                // two A-frames, a ridge pole; the -Z side still shingled, the +Z side kept only its top rows
                for (int s = -1; s <= 1; s += 2)
                {
                    Vector3 apex = new Vector3(s * W * 0.45f, H, 0f);
                    Pole(kit, new Vector3(s * W * 0.45f, -0.05f, D), apex + Vector3.up * 0.06f + Vector3.back * 0.04f, 0.035f, 720 + s);
                    Pole(kit, new Vector3(s * W * 0.45f, -0.05f, -D), apex + Vector3.up * 0.06f + Vector3.forward * 0.04f, 0.035f, 722 + s);
                    Lashing(kit, apex - Vector3.up * 0.03f, 0.04f);
                }
                Pole(kit, new Vector3(-W * 0.55f, H + 0.03f, 0f), new Vector3(W * 0.55f, H + 0.01f, 0f), 0.035f, 724);
                Pole(kit, new Vector3(-W * 0.5f, 0.3f, D * 0.67f), new Vector3(W * 0.5f, 0.32f, D * 0.67f), 0.025f, 725);   // a bare purlin where the shards fell
                // the shingled side has lost whole patches (a hole through the middle, the lower corner gone)
                Shingles(kit, new Vector3(0f, 0.02f, -D), new Vector3(0f, H, 0f), W, (u, vv) =>
                    PropKit.H(726, (int)(u * 50f + vv * 7f)) > 0.3f && !(Mathf.Abs(u - 0.55f) < 0.18f && vv > 0.3f && vv < 0.75f) && !(u > 0.75f && vv < 0.45f), 727);
                Shingles(kit, new Vector3(0f, 0.02f, D), new Vector3(0f, H, 0f), W, (u, vv) => vv > 0.75f && u > 0.35f, 728);
                // the whole frame has racked over to one side and sags toward the back
                kit.Transform(0, p => Lean(Lean(p, Vector3.zero, Vector3.forward, -18f), new Vector3(0f, 0f, -D), Vector3.right, 9f));
                FallenShards(kit, new Vector3(0f, 0f, D * 1.1f), 0.9f, 0.4f, 22, 729, 0.05f);
                FallenShards(kit, new Vector3(0.3f, 0f, -D * 1.2f), 0.7f, 0.4f, 18, 738, 0.06f);
                Vine(kit, new Vector3(-W * 0.45f, 0f, -D), Lean(new Vector3(-W * 0.45f, H * 0.7f, -D * 0.3f), Vector3.zero, Vector3.forward, -18f), 0.035f, 739, 2.5f);
            }
            else if (v == 1)
            {
                // a lean-to against a rock: the roof rises from the ground at +Z to two posts at the back
                PropKit.Lump(kit, new Vector3(0f, 0.45f, -0.95f), new Vector3(1.3f, 0.8f, 0.5f), 6, 12, STONEDARK, 730, 0.2f, 1.4f, 0f);
                for (int s = -1; s <= 1; s += 2)
                {
                    Pole(kit, new Vector3(s * W * 0.42f, -0.05f, -0.35f), new Vector3(s * W * 0.42f, H, -0.38f), 0.04f, 731 + s);
                    Pole(kit, new Vector3(s * W * 0.42f, -0.02f, D + 0.1f), new Vector3(s * W * 0.42f, H + 0.02f, -0.38f), 0.03f, 733 + s);
                }
                Pole(kit, new Vector3(-W * 0.52f, H + 0.02f, -0.38f), new Vector3(W * 0.52f, H, -0.38f), 0.035f, 735);
                Shingles(kit, new Vector3(0f, 0.04f, D + 0.1f), new Vector3(0f, H + 0.02f, -0.38f), W * 0.92f, (u, vv) => u < 0.4f && vv > 0.25f, 736);
                // one back post has given way: that side of the roof has dropped
                kit.Transform(0, p => p.x > 0.1f && p.z > -0.42f ? p - Vector3.up * (0.25f * Mathf.Clamp01((p.y - 0.05f) / H)) : p);
                FallenShards(kit, new Vector3(0.5f, 0f, 0.2f), 0.6f, 0.55f, 18, 737, 0.08f);
                Vine(kit, new Vector3(-W * 0.42f, 0f, -0.35f), new Vector3(-W * 0.42f, H * 0.85f, -0.38f), 0.04f, 734, 3f);
            }
            else
            {
                // collapsed: the ridge pole lies slantwise, the A-frame legs lie or lean, the shards in heaps
                Pole(kit, new Vector3(-0.9f, 0.04f, -0.2f), new Vector3(0.8f, 0.35f, 0.25f), 0.035f, 740);
                Pole(kit, new Vector3(0.75f, -0.03f, -0.55f), new Vector3(0.85f, 0.45f, 0.2f), 0.035f, 741);
                Pole(kit, new Vector3(0.9f, -0.03f, 0.7f), new Vector3(0.82f, 0.42f, 0.28f), 0.035f, 742);
                Pole(kit, new Vector3(-0.85f, 0.04f, 0.5f), new Vector3(-0.3f, 0.04f, 1.0f), 0.035f, 743);
                Pole(kit, new Vector3(-1.1f, 0.04f, -0.7f), new Vector3(-0.5f, 0.04f, -0.9f), 0.033f, 744);
                FallenShards(kit, new Vector3(-0.1f, 0f, 0.1f), 0.7f, 0.5f, 22, 745, 0.14f);
                FallenShards(kit, new Vector3(0.6f, 0f, -0.6f), 0.35f, 0.3f, 8, 746, 0.08f);
            }
            Ruin(kit, 0, 747 + v, 0.07f, v == 2 ? 14 : 10, v == 2 ? 18 : 13, 3, 0.4f);
            return Save($"RestShelter_{Abc(v)}", kit);
        }

        static string Campfire(int v)
        {
            var kit = Kit();
            int stones = v == 2 ? 5 : 8;
            for (int i = 0; i < stones; i++)
            {
                float a = 360f * i / 8f + PropKit.R(750 + v, i, -8f, 8f);
                float d = 0.3f + (v == 1 && i % 3 == 0 ? PropKit.R(753, i, 0.15f, 0.35f) : 0f);
                float r = PropKit.R(754, i, 0.07f, 0.11f);
                var c = PropKit.Dir(a) * d + Vector3.up * r * 0.45f;
                PropKit.Lump(kit, c, new Vector3(r, r * 0.75f, r * 0.85f), 3, 7, i % 3 == 1 ? STONEDARK : STONE, 755 + i, 0.25f, 1.6f, 0f, Quaternion.Euler(0f, a, 0f));
                if (v == 2 && i % 2 == 0) PropKit.Lump(kit, c, new Vector3(r, r * 0.75f, r * 0.85f), 2, 9, MOSS, 755 + i, 0.25f, 1.6f, float.NaN, Quaternion.Euler(0f, a, 0f), 0.7f, 1.07f);
            }
            // ash, and charred sticks lying in it
            PropKit.Lump(kit, Vector3.zero, new Vector3(0.24f, 0.03f, 0.22f), 3, 12, ASH, 760, 0.3f, 2.5f, 0f);
            int sticks = v == 2 ? 2 : 5;
            for (int i = 0; i < sticks; i++)
            {
                float a = PropKit.R(761, i, 0f, 360f), l = PropKit.R(762, i, 0.1f, 0.2f);
                var mid = new Vector3(PropKit.R(763, i, -0.08f, 0.08f), 0.03f + 0.012f * (i % 2), PropKit.R(764, i, -0.08f, 0.08f));
                PropKit.Stick(kit, mid - PropKit.Dir(a) * l * 0.5f, mid + PropKit.Dir(a) * l * 0.5f, 0.017f, 0.012f, 5, CHAR);
            }
            if (v == 1)
            {
                // the tripod that held a pot has fallen across the ring, and a cracked shell bowl lies on its side
                Pole(kit, new Vector3(-0.55f, 0.03f, -0.2f), new Vector3(0.35f, 0.07f, 0.15f), 0.022f, 765);
                Pole(kit, new Vector3(-0.5f, 0.03f, 0.25f), new Vector3(0.4f, 0.03f, -0.05f), 0.022f, 766);
                Pole(kit, new Vector3(0.45f, 0.03f, 0.45f), new Vector3(-0.2f, 0.03f, 0.55f), 0.022f, 767);
                int from = kit.VertexCount;
                PropKit.Lump(kit, Vector3.zero, new Vector3(0.12f, 0.08f, 0.12f), 4, 10, SHARD, 768, 0.05f, 1.5f, float.NaN, null, 0.5f, 1f, n => n.x > 0.6f && n.z > 0f ? -0.5f : 0f);
                kit.Transform(from, p => new Vector3(0.55f, 0.1f, -0.35f) + Quaternion.Euler(0f, 30f, 100f) * p);
            }
            Ruin(kit, 0, 769 + v, 0.045f, v == 2 ? 6 : 4, v == 2 ? 10 : 7, 1, 0.25f);
            return Save($"Campfire_{Abc(v)}", kit, 0);
        }

        static string Rack(int v)
        {
            var kit = Kit();
            const float L = 1.6f, Hh = 0.75f, Dd = 0.5f;
            if (v < 2)
            {
                // trestles at each end (two splayed legs + a crossbar); two long rails; slats across
                float hl = Hh, hr = v == 1 ? 0.18f : Hh;
                for (int e = -1; e <= 1; e += 2)
                {
                    float x = e * L * 0.42f, h = e < 0 ? hl : hr;
                    if (v == 1 && e > 0)
                    {
                        // broken legs: short stubs, the snapped tops lying beside
                        Pole(kit, new Vector3(x, -0.03f, Dd * 0.45f), new Vector3(x, 0.22f, Dd * 0.3f), 0.03f, 770);
                        Pole(kit, new Vector3(x, -0.03f, -Dd * 0.45f), new Vector3(x, 0.15f, -Dd * 0.35f), 0.03f, 771);
                        Pole(kit, new Vector3(x + 0.15f, 0.03f, Dd * 0.6f), new Vector3(x + 0.5f, 0.03f, Dd * 0.9f), 0.03f, 772);
                    }
                    else
                    {
                        Pole(kit, new Vector3(x, -0.03f, Dd * 0.45f), new Vector3(x, h + 0.05f, -Dd * 0.05f), 0.03f, 773 + e);
                        Pole(kit, new Vector3(x, -0.03f, -Dd * 0.45f), new Vector3(x, h + 0.05f, Dd * 0.05f), 0.03f, 775 + e);
                        Lashing(kit, new Vector3(x, h - 0.02f, 0f), 0.03f);
                    }
                }
                for (int s = -1; s <= 1; s += 2)
                    Pole(kit, new Vector3(-L * 0.5f, hl, s * Dd * 0.3f), new Vector3(L * 0.5f, hr, s * Dd * 0.3f), 0.028f, 777 + s);
                int slats = 12;
                for (int i = 0; i < slats; i++)
                {
                    if (i == 3 || i == 5 || i == 8 || i == 9 || (v == 1 && i > 7)) continue;
                    float x = -L * 0.45f + L * 0.9f * i / (slats - 1), h = Mathf.Lerp(hl, hr, (x + L * 0.5f) / L) + 0.035f;
                    float skew = i == 6 ? 18f : PropKit.R(779, i, -3f, 3f);
                    var d = Quaternion.Euler(0f, skew, 0f) * Vector3.forward * (Dd * 0.42f);
                    Pole(kit, new Vector3(x, h, 0f) - d, new Vector3(x, h, 0f) + d, 0.016f, 780 + i);
                }
                Vine(kit, new Vector3(-L * 0.42f, 0f, Dd * 0.45f), new Vector3(-L * 0.42f, Hh * 0.8f, Dd * 0.05f), 0.03f, 798, 2.5f);
                if (v == 0)
                    // racked over: the whole frame (vine and all) leans sideways and the far end has sunk
                    kit.Transform(0, p => Lean(Lean(p, Vector3.zero, Vector3.right, 14f), new Vector3(-L * 0.42f, 0f, 0f), Vector3.forward, -5f));
                if (v == 1)
                    for (int i = 0; i < 3; i++)
                    {
                        var c = new Vector3(L * 0.55f + 0.1f * i, 0.02f, PropKit.R(795, i, -0.3f, 0.3f));
                        var d = PropKit.Dir(PropKit.R(796, i, 60f, 120f)) * 0.22f;
                        Pole(kit, c - d, c + d, 0.016f, 797 + i);
                    }
            }
            else
            {
                // collapsed flat: rails, legs and slats scattered where they fell
                for (int i = 0; i < 12; i++)
                {
                    bool longPiece = i < 2;
                    float len = longPiece ? L : i < 6 ? 0.8f : 0.42f, r = longPiece ? 0.028f : i < 6 ? 0.03f : 0.016f;
                    var c = new Vector3(PropKit.R(800, i, -0.6f, 0.6f), 0.02f + 0.03f * (i % 3), PropKit.R(801, i, -0.5f, 0.5f));
                    var d = PropKit.Dir((longPiece ? 5f + 20f * i : PropKit.R(802, i, 0f, 180f))) * (len * 0.5f);
                    Pole(kit, c - d, c + d, r, 803 + i);
                }
                Rope(kit, new Vector3(-0.4f, 0.02f, 0.3f), new Vector3(0.1f, 0.02f, 0.5f), 0f);
            }
            Ruin(kit, 0, 799 + v, 0.06f, v == 2 ? 10 : 8, v == 2 ? 16 : 12, 2, 0.35f);
            return Save($"DryingRack_{Abc(v)}", kit);
        }

        /// <summary>
        /// 編んだ籠（直径 2R・高さ H、少し四角い）：横の帯 rows 段は内外に波打って編み目に見える。hole(段, 角度) が true の所は破れて帯が切れる。
        /// </summary>
        static void WovenBasket(NatureMeshKit kit, float R, float H, int rows, System.Func<int, float, bool> hole, int seed)
        {
            const int ribs = 14;
            float Square(float a) => 1f / Mathf.Pow(Mathf.Pow(Mathf.Abs(Mathf.Cos(a)), 3f) + Mathf.Pow(Mathf.Abs(Mathf.Sin(a)), 3f), 1f / 3f) * 0.92f;
            float Rad(float y) => R * Mathf.Lerp(0.85f, 1f, Mathf.Sqrt(Mathf.Clamp01(y / H)));
            // the ribs (stakes) the bands weave around
            for (int i = 0; i < ribs; i++)
            {
                float a = Mathf.PI * 2f * i / ribs;
                var d = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * Square(a);
                PropKit.Stick(kit, d * Rad(0.02f) + Vector3.up * 0.02f, d * Rad(H) + Vector3.up * (H + 0.03f), 0.012f, 0.01f, 4, WEAVEDARK);
            }
            for (int r = 0; r < rows; r++)
            {
                float y = 0.04f + (H - 0.06f) * r / (rows - 1);
                int seg = 0;
                // split the ring into runs between holes
                var runs = new List<Vector2>();
                float start = -1f;
                for (int k = 0; k <= 56; k++)
                {
                    float a = Mathf.PI * 2f * k / 56f;
                    bool open = k < 56 && !hole(r, a);
                    if (open && start < 0f) start = a;
                    if ((!open || k == 56) && start >= 0f) { runs.Add(new Vector2(start, a)); start = -1f; }
                }
                foreach (var run in runs)
                {
                    int rr = r, sg = seg++;
                    float a0 = run.x, a1 = run.y;
                    PropKit.Pipe(kit, t =>
                    {
                        float a = Mathf.Lerp(a0, a1, t);
                        float weave = 1f + 0.035f * Mathf.Sin(a * ribs + rr * Mathf.PI);
                        return new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * (Rad(y) * Square(a) * weave) + Vector3.up * y;
                    }, (t, ang) => 1f / Mathf.Sqrt(Mathf.Pow(Mathf.Cos(ang) / 0.012f, 2f) + Mathf.Pow(Mathf.Sin(ang) / 0.024f, 2f)),
                        Mathf.Max(2, Mathf.RoundToInt((a1 - a0) * 9f)), 6, (r + sg) % 3 == 0 ? WEAVEDARK : WEAVE, WEAVE, WEAVE);
                }
            }
            // a thick rim and a woven floor
            PropKit.Pipe(kit, t => new Vector3(Mathf.Cos(t * Mathf.PI * 2f), 0f, Mathf.Sin(t * Mathf.PI * 2f)) * (Rad(H) * Square(t * Mathf.PI * 2f)) + Vector3.up * (H + 0.02f), (t, a) => 0.028f, 40, 6, WEAVEDARK);
            PropKit.Lump(kit, Vector3.up * 0.02f, new Vector3(R * 0.85f, 0.025f, R * 0.85f), 2, 16, WEAVE, seed, 0.05f, 2f);
        }

        static string Basket(int v)
        {
            var kit = Kit();
            const float R = 0.32f, H = 0.5f;
            int from = kit.VertexCount;
            if (v == 0)
            {
                WovenBasket(kit, R, H, 9, (r, a) => r >= 3 && r <= 5 && a > 0.4f && a < 1.0f, 810);
                // the carrying strap, snapped: one end tied on, hanging down the side
                var tie = new Vector3(-R * 0.95f, H, 0f);
                Rope(kit, tie, tie + new Vector3(-0.08f, -0.3f, 0.1f), 0f, 0.016f);
                Rope(kit, new Vector3(R * 0.95f, H, 0f), new Vector3(R * 1.3f, 0.015f, 0.2f), 0.05f, 0.016f);
            }
            else if (v == 1)
            {
                WovenBasket(kit, R, H, 9, (r, a) => r == 8 && a > 2.2f && a < 3.0f, 811);
                // tipped on its side, the mouth toward +X
                kit.Transform(from, p => Quaternion.Euler(0f, 0f, -90f) * p + new Vector3(-H * 0.5f, R * 0.95f, 0f));
                for (int i = 0; i < 9; i++)
                {
                    float r = PropKit.R(812, i, 0.03f, 0.06f);
                    var c = new Vector3(PropKit.R(813, i, 0.05f, 0.45f), r * 0.5f, PropKit.R(814, i, -0.18f, 0.18f));
                    PropKit.Lump(kit, c, new Vector3(r, r * 0.8f, r), 2, 6, SALT, 815 + i, 0.25f, 1.6f, 0f);
                }
            }
            else
            {
                WovenBasket(kit, R, H, 9, (r, a) => (r % 3 == 1 && a > 3.5f && a < 4.6f) || (r > 5 && a > 0.3f && a < 1.4f), 816);
                // crushed: squashed down and pushed over, the top skewed
                kit.Transform(from, p =>
                {
                    float k = p.y / (H + 0.04f);
                    return new Vector3(p.x * (1.1f + 0.25f * k) + 0.18f * k, p.y * 0.42f, p.z * (1.15f - 0.2f * k)) + Vector3.up * 0.015f * PropKit.Noise(p * 9f, 817);
                });
            }
            if (v == 0)
                // the rim has sagged in on one side
                kit.Transform(0, p => p.y > 0.25f ? p - Vector3.up * (0.12f * Mathf.Max(0f, p.x / 0.32f) * (p.y - 0.25f) / 0.25f) : p);
            Ruin(kit, 0, 818 + v, v == 1 ? 0.14f : 0.1f, 5, 9, 2, 0.2f);
            return Save($"Basket_{Abc(v)}", kit, 3);
        }

        static string DigMark(int v)
        {
            var kit = Kit();
            if (v == 0)
            {
                // a salt-rock face (toward -Z) scored with pick strokes, chips at its foot
                PropKit.Lump(kit, new Vector3(0f, 0.5f, 0.2f), new Vector3(0.8f, 0.65f, 0.6f), 14, 24, SALTROCK, 820, 0.22f, 1.3f, 0f, null, 0f, 1f, n =>
                {
                    if (n.z > -0.2f) return 0f;
                    // the worked face is cut back flat, then scored by slanting pick strokes
                    float g = Mathf.Abs(Mathf.Sin((n.x * 1.0f + n.y * 0.45f) * 13f));
                    return -0.5f * Mathf.Max(0f, -n.z - 0.2f) - 0.12f * Mathf.Pow(g, 4f) * Mathf.Min(1f, (-n.z - 0.2f) * 4f);
                });
                for (int i = 0; i < 10; i++)
                {
                    float r = PropKit.R(821, i, 0.04f, 0.1f);
                    var c = new Vector3(PropKit.R(822, i, -0.5f, 0.5f), r * 0.4f, PropKit.R(823, i, -0.65f, -0.3f));
                    PropKit.Lump(kit, c, new Vector3(r, r * 0.6f, r * 0.8f), 2, 6, SALTROCK, 824 + i, 0.3f, 1.6f, 0f, Quaternion.Euler(0f, PropKit.R(825, i, 0f, 360f), 0f));
                }
            }
            else if (v == 1)
            {
                PropKit.Lump(kit, Vector3.zero, new Vector3(0.6f, 0.3f, 0.5f), 4, 12, DIRT, 830, 0.2f, 1.6f, 0f);
                for (int i = 0; i < 14; i++)
                {
                    float a = PropKit.R(831, i, 0f, Mathf.PI * 2f), d = Mathf.Sqrt(PropKit.H(832, i)) * 0.85f, r = PropKit.R(833, i, 0.05f, 0.13f);
                    var c = new Vector3(Mathf.Cos(a) * d * 0.6f, 0.3f * Mathf.Sqrt(Mathf.Max(0f, 1f - d * d)) + r * 0.2f, Mathf.Sin(a) * d * 0.5f);
                    PropKit.Lump(kit, c, new Vector3(r, r * 0.65f, r * 0.85f), 2, 6, SALTROCK, 834 + i, 0.3f, 1.6f, 0f, Quaternion.Euler(0f, a * 57f, 0f));
                }
                // a snapped pick handle: the head end stuck in the heap, the other half lying apart
                Pole(kit, new Vector3(0.15f, 0.15f, 0.05f), new Vector3(0.35f, 0.5f, 0.15f), 0.025f, 850);
                Pole(kit, new Vector3(0.75f, 0.025f, 0.3f), new Vector3(1.05f, 0.025f, 0.05f), 0.025f, 851);
            }
            else
            {
                // a shallow pit: a ring bank of spoil with a low dished floor inside, chips around the rim
                PropKit.Surface(kit, (t, a) =>
                {
                    float r = 1.4f * t;
                    float y = 0.25f * Mathf.Exp(-Mathf.Pow((r - 0.85f) / 0.28f, 2f)) * (1f + 0.25f * PropKit.Noise(new Vector3(Mathf.Cos(a) * 2f, 0f, Mathf.Sin(a) * 2f), 860)) + 0.015f;
                    return new Vector3(Mathf.Cos(a) * r, y, Mathf.Sin(a) * r);
                }, 10, 24, DIRT);
                for (int i = 0; i < 12; i++)
                {
                    float a = PropKit.R(861, i, 0f, Mathf.PI * 2f), d = PropKit.R(862, i, 0.7f, 1.2f), r = PropKit.R(863, i, 0.04f, 0.1f);
                    float y = 0.25f * Mathf.Exp(-Mathf.Pow((d - 0.85f) / 0.28f, 2f)) + 0.01f;
                    PropKit.Lump(kit, new Vector3(Mathf.Cos(a) * d, y + r * 0.2f, Mathf.Sin(a) * d), new Vector3(r, r * 0.6f, r * 0.8f), 2, 6, SALTROCK, 864 + i, 0.3f, 1.6f);
                }
            }
            Ruin(kit, 0, 865 + v, 0.03f, 10, 12, 2, 0.3f);
            return Save($"DigMark_{Abc(v)}", kit);
        }

        static string Channel(int v)
        {
            var kit = Kit();
            // the path of the channel's middle line, 3 m long: straight along X (A, C) or a quarter circle of radius 1.9 m (B)
            System.Func<float, Vector3> mid = v == 1
                ? (System.Func<float, Vector3>)(s => new Vector3(-1.9f + 1.9f * Mathf.Sin(s * Mathf.PI * 0.5f), 0f, 1.9f - 1.9f * Mathf.Cos(s * Mathf.PI * 0.5f)) + new Vector3(0f, 0f, -0.95f))
                : s => new Vector3(-1.5f + 3f * s, 0f, 0f);
            const float halfW = 0.3f;
            for (int side = -1; side <= 1; side += 2)
                for (int course = 0; course < 2; course++)
                    for (int i = 0; i < 13; i++)
                    {
                        float s = (i + 0.5f * course + 0.25f) / 13f;
                        if (s > 1f) continue;
                        int seed = 870 + side * 100 + course * 30 + i;
                        // every channel has lost some of its top course; C has also caved in on one side
                        bool fallen = course == 1 && PropKit.H(seed, 9) < (v == 2 ? 0.75f : 0.35f) || (v == 2 && side > 0 && i >= 4 && i <= 8);
                        Vector3 p = mid(s), tan = (mid(Mathf.Min(1f, s + 0.01f)) - mid(Mathf.Max(0f, s - 0.01f))).normalized;
                        Vector3 n = Vector3.Cross(Vector3.up, tan) * side;
                        float r = PropKit.R(seed, 1, 0.1f, 0.14f);
                        Vector3 c = p + n * (halfW + 0.06f) + Vector3.up * (0.06f + course * 0.12f);
                        var rot = Quaternion.LookRotation(tan) * Quaternion.Euler(PropKit.R(seed, 2, -5f, 5f), PropKit.R(seed, 3, -8f, 8f), 0f);
                        if (fallen)
                        {
                            // tumbled into the bed
                            c = p + n * PropKit.R(seed, 4, -0.1f, 0.2f) + tan * PropKit.R(seed, 5, -0.1f, 0.1f) + Vector3.up * 0.05f;
                            rot = Quaternion.Euler(PropKit.R(seed, 6, -30f, 30f), PropKit.R(seed, 7, 0f, 360f), PropKit.R(seed, 8, -30f, 30f));
                        }
                        PropKit.Lump(kit, c, new Vector3(0.08f, 0.065f, r), 3, 7, course == 0 ? STONEDARK : STONE, seed, 0.2f, 1.5f, 0f, rot);
                    }
            // the bed: a white crust of salt along the middle (what the last water left)
            kit.Blade(s => mid(s) + Vector3.up * 0.006f, s => halfW * 0.7f * (1f + 0.15f * Mathf.Sin(s * 17f)), 30, 0.004f, -0.05f, SALT);
            if (v == 2) PropKit.Lump(kit, mid(0.3f), new Vector3(0.45f, 0.12f, 0.28f), 3, 10, DIRT, 990, 0.2f, 1.6f, 0f);
            Ruin(kit, 0, 991 + v, 0.05f, 10, 16, 3, 0.15f);
            return Save($"OldChannel_{Abc(v)}", kit);
        }
    }
}
