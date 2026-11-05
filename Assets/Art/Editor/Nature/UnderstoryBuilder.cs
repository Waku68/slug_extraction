using System.Text;
using UnityEditor;
using UnityEngine;

namespace SlugDesign
{
    /// <summary>
    /// 塊の層の草と茂み（Nov 2、ケルン風に作り直し。docs/modeling/README.md 8章、手本は MassBushBuilder）。
    /// メニュー Slug/Nature/Props/Build Understory で作り直す（Assets/Art/Nature/Understory）。
    /// 葉を 1 枚ずつ描かず大きな塊・大きな色の面で見せ、陰は塊の丸みで付ける（NatureMeshKit.Mass）。輪郭だけ葉先や切れ込みが出る。
    /// 明るい所は暖かく、陰は冷たく（青み）、段は 3 つでくっきり。群れには主役 1 つと小さい子。
    /// 葉と草はかじれるのでなめらか（風で揺れる）、茂みの枝はカクカク。草と茂みはすり抜ける（当たり判定なし）。
    /// ナメクジの目の高さ 0.47 m は人の約 1/3.6。
    /// - FernClump シダ：A 株 1 つ、葉 7 枚・0.85 m が弓なり。B 主役の株（0.7 m）と小さい子 2 つ。C 立ち気味の若い株、ぜんまい 3 本
    /// - LowBush 低い茂み：A ドームの主役 幅 1.2 m・高さ 0.75 m（人なら 2.7 m）、塊 5 つ。B 横に広い 幅 2 m・高さ 0.5 m、塊 6 つ。
    ///   C 背高 1.2 m、赤い実が房で
    /// - DryGrass 背の高い枯れ草（砂漠・サバナ）：A 主役の株 0.9 m と子 2 つ。B まばらな背高 1.2 m、穂つき。C 風になびいた株 3 つ
    /// 色：シダ #7E9A50・ぜんまい #9A9258、茂みの葉 #6E8448・葉先 #8A9E58、枝 #5E5040、実 #B04838、枯れ草 #C8B07A・根元 #A08A5A、穂 #D8C890。
    /// 陰の色は青みの緑 (0.5, 0.62, 0.74)、枯れ草だけ少し灰青 (0.6, 0.64, 0.74)。
    /// </summary>
    public static class UnderstoryBuilder
    {
        const string Dir = "Assets/Art/Nature/Understory";
        const int FERN = 0, CURL = 1, BUSH = 2, TIP = 3, TWIG = 4, BERRY = 5, GRASS = 6, GRASSBASE = 7, SEED = 8;
        static Material[] _m;

        [MenuItem("Slug/Nature/Props/Build Understory")]
        public static void Build()
        {
            _m = PropKit.Mats(Dir, "Understory",
                new PropKit.Spec("Fern", "#7E9A50", false, 0.1f, true).Mottle("#8AA458", 0.25f, 2.5f),
                new PropKit.Spec("Curl", "#9A9258", false, 0.02f),
                new PropKit.Spec("Bush", "#6E8448", false, 0.03f).Mottle("#7A9050", 0.3f, 3f),
                new PropKit.Spec("Tip", "#8A9E58", false, 0.05f),
                new PropKit.Spec("Twig", "#5E5040", true),
                new PropKit.Spec("Berry", "#B04838", false, 0.02f),
                new PropKit.Spec("Grass", "#C8B07A", false, 0.12f, true).Mottle("#D6C08A", 0.25f, 3f),
                new PropKit.Spec("GrassBase", "#A08A5A", false, 0.02f),
                new PropKit.Spec("Seed", "#D8C890", false, 0.12f));
            foreach (int i in new[] { FERN, CURL, BUSH, TIP, BERRY }) PropKit.CairnPaint(_m[i]);
            foreach (int i in new[] { GRASS, GRASSBASE, SEED }) PropKit.CairnPaint(_m[i], "#4A3E2A", 0.6f, 0.64f, 0.74f);
            var log = new StringBuilder("Understory built:");
            for (int v = 0; v < 3; v++) log.Append(' ').Append(Fern(v)).Append(' ').Append(Bush(v)).Append(' ').Append(Grass(v));
            AssetDatabase.SaveAssets();
            Debug.Log(log);
        }

        static string Abc(int v) => ((char)('A' + v)).ToString();
        static NatureMeshKit Kit() => new NatureMeshKit(_m.Length);

        /// <summary>
        /// シダの株 1 つ：根元 at から葉 fronds 枚が放射に出て弓なりに垂れる。葉は大きな色の面（ふちだけ丸い切れ込み 6 つ）。
        /// 株ぜんたいを 1 つのドームとして光らせる。
        /// </summary>
        static void FernClump(NatureMeshKit kit, Vector3 at, int fronds, float len, float width, float p0, float p1, float droop, int curls, int seed)
        {
            int from = kit.VertexCount;
            for (int i = 0; i < fronds; i++)
            {
                float yaw = 360f * i / fronds + PropKit.R(seed, i, -15f, 15f);
                float pitch = Mathf.Deg2Rad * PropKit.R(seed + 1, i, p0, p1), l = len * PropKit.R(seed + 2, i, 0.85f, 1.1f);
                var h = PropKit.Dir(yaw);
                var d0 = h * Mathf.Cos(pitch) + Vector3.up * Mathf.Sin(pitch);
                Vector3 root = at + h * 0.03f + Vector3.up * 0.02f;
                System.Func<float, Vector3> path = s => root + d0 * (l * s) - Vector3.up * (droop * l * s * s);
                System.Func<float, float> wid = s =>
                {
                    float body = Mathf.Pow(Mathf.Max(0f, Mathf.Sin(Mathf.PI * Mathf.Min(1f, s))), 0.6f);
                    float lobe = 0.62f + 0.38f * Mathf.Pow(Mathf.Abs(Mathf.Sin(Mathf.PI * 6f * s)), 0.5f);
                    return width * body * (s < 0.15f ? 0.1f + 6f * s : lobe);
                };
                int bf = kit.VertexCount;
                kit.Blade(path, wid, 30, 0.012f, 0.25f, FERN);
                int ii = i;
                kit.Paint(bf, (p, uv) => new Color(uv.y, uv.y * uv.y * 0.5f, PropKit.H(seed + 3, ii), 1f));
            }
            for (int c = 0; c < curls; c++)
            {
                var fwd = PropKit.Dir(c * 120f + 30f);
                float stalk = len * (0.32f + 0.06f * c), r0 = len * 0.08f;
                System.Func<float, Vector3> curl = t =>
                {
                    if (t < 0.45f) return at + fwd * 0.02f + Vector3.up * (stalk * t / 0.45f);
                    float u = (t - 0.45f) / 0.55f, th = u * Mathf.PI * 1.7f, r = r0 * (1f - 0.65f * u);
                    Vector3 tip = at + fwd * 0.02f + Vector3.up * stalk, centre = tip + fwd * r0;
                    return centre + (-fwd * Mathf.Cos(th) + Vector3.up * Mathf.Sin(th)) * r;
                };
                PropKit.Pipe(kit, curl, (t, a) => Mathf.Lerp(0.016f, 0.011f, t), 12, 7, CURL, CURL, -1);
                kit.Ball(curl(1f), Vector3.one * 0.016f, 4, CURL);
            }
            // the whole clump lights as one dome
            float spread = len * 0.8f;
            kit.Mass(from, at + Vector3.up * (len * 0.25f), new Vector3(spread, len * 0.6f, spread), 0.7f);
        }

        static string Fern(int v)
        {
            var kit = Kit();
            if (v == 0) FernClump(kit, Vector3.zero, 7, 0.85f, 0.17f, 48f, 66f, 0.75f, 0, 500);
            else if (v == 1)
            {
                FernClump(kit, Vector3.zero, 6, 0.7f, 0.16f, 40f, 60f, 0.6f, 1, 530);
                FernClump(kit, new Vector3(0.62f, 0f, 0.25f), 4, 0.36f, 0.1f, 40f, 62f, 0.5f, 0, 540);
                FernClump(kit, new Vector3(-0.5f, 0f, 0.45f), 4, 0.3f, 0.09f, 44f, 64f, 0.5f, 0, 550);
            }
            else FernClump(kit, Vector3.zero, 5, 0.6f, 0.15f, 66f, 80f, 0.4f, 3, 560);
            return PropKit.Save(Dir, $"FernClump_{Abc(v)}", kit, _m, 0);
        }

        static string Bush(int v)
        {
            var kit = Kit();
            float w = v == 0 ? 1.2f : v == 1 ? 2.0f : 0.8f, h = v == 0 ? 0.75f : v == 1 ? 0.5f : 1.2f;
            int seed = 540 + v * 40;
            float floor = h * 0.12f, tipLen = v == 1 ? 0.13f : 0.12f;
            var cs = new System.Collections.Generic.List<(Vector3 c, Vector3 r)>();
            if (v == 0)
            {
                // the hero dome: one big middle, three round it, one on top
                cs.Add((new Vector3(0f, h * 0.46f, 0f), new Vector3(w * 0.36f, h * 0.4f, w * 0.34f)));
                for (int i = 0; i < 3; i++)
                {
                    var d = PropKit.Dir(120f * i + PropKit.R(seed, i, -25f, 25f)); float rr = w * PropKit.R(seed + 1, i, 0.2f, 0.25f);
                    cs.Add((d * (w * 0.24f) + Vector3.up * (h * PropKit.R(seed + 2, i, 0.36f, 0.48f)), new Vector3(rr, rr * 0.85f, rr)));
                }
                cs.Add((new Vector3(w * 0.06f, h * 0.7f, -w * 0.04f), new Vector3(w * 0.21f, h * 0.27f, w * 0.2f)));
            }
            else if (v == 1)
            {
                // spreading low: a bigger mass a little off-centre, the rest trailing out to both sides, lower toward the ends
                cs.Add((new Vector3(-w * 0.08f, h * 0.5f, 0f), new Vector3(w * 0.22f, h * 0.45f, w * 0.2f)));
                for (int i = 0; i < 5; i++)
                {
                    float x = Mathf.Lerp(-w * 0.42f, w * 0.42f, i / 4f); if (Mathf.Abs(x + w * 0.08f) < w * 0.12f) x += w * 0.18f;
                    float rr = w * PropKit.R(seed + 3, i, 0.12f, 0.16f) * (1f - 0.35f * Mathf.Abs(x) / w);
                    cs.Add((new Vector3(x, h * (0.42f - 0.15f * Mathf.Abs(x) / w), PropKit.R(seed + 4, i, -0.15f, 0.15f) * w), new Vector3(rr * 1.2f, rr * 0.8f, rr)));
                }
            }
            else
            {
                // tall: one teardrop of overlapping masses - a broad low body, two shoulders, a smaller crown, staggered so it isn't a stack
                cs.Add((new Vector3(0f, h * 0.38f, 0f), new Vector3(w * 0.5f, h * 0.34f, w * 0.46f)));
                cs.Add((new Vector3(w * 0.16f, h * 0.6f, w * 0.05f), new Vector3(w * 0.36f, h * 0.26f, w * 0.34f)));
                cs.Add((new Vector3(-w * 0.14f, h * 0.66f, -w * 0.08f), new Vector3(w * 0.32f, h * 0.24f, w * 0.3f)));
                cs.Add((new Vector3(w * 0.03f, h * 0.84f, 0f), new Vector3(w * 0.26f, h * 0.18f, w * 0.25f)));
            }
            for (int k = 0; k < cs.Count; k++)
            {
                var (c, r) = cs[k];
                PropKit.LeafMass(kit, c, r, seed + 10 + k * 7, BUSH, TIP, k == 0 ? 16 : 12, tipLen * Mathf.Clamp(r.x / 0.3f, 0.6f, 1.2f), floor, h);
            }
            // berries (C): small bunches of three sitting in the surface on the lit side
            if (v == 2)
                for (int i = 0; i < 7; i++)
                {
                    var (c, r) = cs[i % cs.Count];
                    float az = PropKit.R(seed + 40, i, -1.2f, 1.6f), el = PropKit.R(seed + 41, i, -0.2f, 0.7f);
                    var n = new Vector3(Mathf.Cos(el) * Mathf.Cos(az), Mathf.Sin(el), Mathf.Cos(el) * Mathf.Sin(az));
                    var p = c + Vector3.Scale(n, r) * 0.98f;
                    for (int b = 0; b < 3; b++)
                        kit.Ball(p + new Vector3(PropKit.R(seed + 42, i * 3 + b, -0.03f, 0.03f), -0.02f * b, PropKit.R(seed + 43, i * 3 + b, -0.03f, 0.03f)), Vector3.one * 0.032f, 4, BERRY);
                }
            // the whole bush leans toward one dome, so the masses read as one bush in the light
            kit.Mass(0, new Vector3(0f, h * 0.45f, 0f), new Vector3(w * 0.5f, h * 0.55f, w * 0.5f), 0.45f);
            // a few short crooked stems from the ground up into the masses
            for (int i = 0; i < (v == 1 ? 5 : 4); i++)
            {
                var d = PropKit.Dir(i * 100f + PropKit.R(seed + 30, i, -20f, 20f));
                PropKit.Branch(kit, d * (v == 1 ? w * 0.2f * (i - 2) / 2f : 0f), d * 0.3f + Vector3.up, v == 2 ? h * 0.2f : h * 0.38f, v == 2 ? 0.035f : 0.025f, 1, seed + 31 + i, 5, TWIG, v == 2 ? 15f : 30f);
            }
            return PropKit.Save(Dir, $"LowBush_{Abc(v)}", kit, _m, 0);
        }

        static string Grass(int v)
        {
            var kit = Kit();
            int seed = 580 + v * 40;
            if (v == 0)
            {
                PropKit.GrassTuft(kit, Vector3.zero, 0.14f, 0.9f, 26, seed, GRASSBASE, GRASS, default, 0.04f);
                PropKit.GrassTuft(kit, new Vector3(0.42f, 0f, 0.18f), 0.07f, 0.45f, 12, seed + 10, GRASSBASE, GRASS, default, 0.03f);
                PropKit.GrassTuft(kit, new Vector3(-0.3f, 0f, 0.36f), 0.06f, 0.35f, 10, seed + 20, GRASSBASE, GRASS, default, 0.028f);
            }
            else if (v == 1)
            {
                PropKit.GrassTuft(kit, Vector3.zero, 0.12f, 1.2f, 16, seed, GRASSBASE, GRASS, default, 0.035f);
                PropKit.GrassTuft(kit, new Vector3(0.36f, 0f, -0.2f), 0.07f, 0.6f, 9, seed + 10, GRASSBASE, GRASS, default, 0.03f);
                // seed heads: thin stalks rising past the leaves, each with a soft spike that leans over
                for (int i = 0; i < 6; i++)
                {
                    var b = new Vector3(PropKit.R(seed + 30, i, -0.06f, 0.06f), 0.05f, PropKit.R(seed + 31, i, -0.06f, 0.06f));
                    var lean = PropKit.Dir(PropKit.R(seed + 32, i, 0f, 360f)) * PropKit.R(seed + 33, i, 0.08f, 0.2f);
                    float H = PropKit.R(seed + 34, i, 1.15f, 1.45f);
                    System.Func<float, Vector3> stalk = t => b + Vector3.up * (H * t) + lean * (t * t);
                    int from = kit.VertexCount;
                    PropKit.Pipe(kit, stalk, (t, a) => 0.005f, 8, 4, SEED, -1, -1);
                    var top = stalk(1f); var dir = (stalk(1f) - stalk(0.95f)).normalized;
                    PropKit.Pipe(kit, t => top + dir * (0.13f * t) + lean.normalized * (0.03f * t * t), (t, a) => 0.018f * Mathf.Sin(Mathf.PI * Mathf.Clamp(t, 0.08f, 1f)), 6, 6, SEED, SEED, SEED);
                    kit.Paint(from, (p, uv) => new Color(Mathf.Clamp01(p.y / 1.4f), 0.5f, 0.5f, 1f));
                }
            }
            else
            {
                var wind = PropKit.Dir(20f);
                PropKit.GrassTuft(kit, Vector3.zero, 0.13f, 0.85f, 22, seed, GRASSBASE, GRASS, wind * 0.45f, 0.04f);
                PropKit.GrassTuft(kit, new Vector3(-0.45f, 0f, 0.2f), 0.09f, 0.6f, 14, seed + 10, GRASSBASE, GRASS, wind * 0.32f, 0.034f);
                PropKit.GrassTuft(kit, new Vector3(0.2f, 0f, -0.45f), 0.06f, 0.4f, 10, seed + 20, GRASSBASE, GRASS, wind * 0.22f, 0.03f);
            }
            return PropKit.Save(Dir, $"DryGrass_{Abc(v)}", kit, _m, 0);
        }
    }
}
