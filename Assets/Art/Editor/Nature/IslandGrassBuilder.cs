using System;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace SlugDesign
{
    /// <summary>
    /// 緑の草 4 種（Nov 3 下書き、1 種 1 案。わくさんのペルソナが監督）。メニュー Slug/Nature/Build Grass で作り直す（Assets/Art/Nature/Grass）。
    /// ケルン風（docs/modeling/README.md 8章、手本は MassBushBuilder）：草を 1 本ずつ描かず塊で見せ、輪郭からだけ葉先が出る。陰は青み、段 3 つ。
    /// 草はかじれるのでなめらか、風で揺れる。すり抜ける（当たり判定なし）。砂漠の背の高い枯れ草とシダは UnderstoryBuilder。
    /// ナメクジの目の高さ 0.47 m は人の約 1/3.6。
    /// - A 草原の草むら（花の草原の隠れる物）：縦長の噴水の形。主役の株 高さ 0.6 m（伏せると隠れて、目は出る。人なら 2.2 m）・幅 0.6 m と、子 2 つ。
    ///   真ん中の葉は立ち、外の葉は長い弧で外へ垂れる（丸いドームに短い葉を刺すとウニやサボテンに見える）
    /// - B 森の下草：幅広の葉 7 枚の株 高さ 0.3 m・幅 0.55 m（人なら 1.1 m）と、子 1 つ。暗い緑。飾り（隠れるのはクワズイモとシダ）
    /// - C 水辺のアシ（川べり・塩田の流れ込み）：下は濃い葉の噴水 0.7 m、上に茎 8 本 1.25〜1.7 m（ナメクジの 3 倍、人なら 6 m）と穂。葉は横に張らず下へしなる
    /// - D 足元の芝のむら（見るだけ。敷く物）：1.2 × 0.9 m・高さ 0.05 m、輪郭線なし、ふちは欠けて地面に溶ける。区画で色だけ替える
    ///   （D 草原の黄緑、D_Forest 森の濃い緑。砂漠と溶岩には敷かない）
    /// 色：草むら #7E9A4E・外の葉 #88A456（葉先を明るくしすぎると毛やトゲに見える）、下草 #566E42・葉柄 #5E6E40、アシ #8A9A5A・下の葉 #5E6A40・穂 #A89878、
    /// 芝 #7E9050・葉先 #88985A（森は #566A40・#627848）。
    /// </summary>
    public static class IslandGrassBuilder
    {
        const string Dir = "Assets/Art/Nature/Grass";
        const int MEADOW = 0, MEADOWTIP = 1, FOREST = 2, STALK = 3, REED = 4, REEDBASE = 5, PLUME = 6, LAWN = 7, LAWNTIP = 8;
        static Material[] _m;

        [MenuItem("Slug/Nature/Build Grass")]
        public static void Build()
        {
            _m = PropKit.Mats(Dir, "Grass",
                new PropKit.Spec("Meadow", "#7E9A4E", false, 0.06f).Mottle("#8AA458", 0.2f, 3f),
                new PropKit.Spec("MeadowTip", "#88A456", false, 0.1f),
                new PropKit.Spec("Forest", "#566E42", false, 0.05f),
                new PropKit.Spec("Stalk", "#5E6E40", false, 0.03f),
                new PropKit.Spec("Reed", "#8A9A5A", false, 0.12f),
                new PropKit.Spec("ReedBase", "#5E6A40", false, 0.02f),
                new PropKit.Spec("Plume", "#A89878", false, 0.15f),
                new PropKit.Spec("Lawn", "#7E9050", false, 0.02f),
                new PropKit.Spec("LawnTip", "#88985A", false, 0.04f));
            foreach (var x in _m) PropKit.CairnPaint(x);
            foreach (int i in new[] { LAWN, LAWNTIP }) _m[i].SetFloat("_OutlineWidth", 0f);   // the lawn melts into the ground: no outline
            foreach (int i in new[] { MEADOW, MEADOWTIP, REED, REEDBASE }) _m[i].SetFloat("_OutlineWidth", 0.004f);   // thin blades get a thin line
            var log = new StringBuilder("Grass built:");
            log.Append(' ').Append(Meadow()).Append(' ').Append(ForestFloor()).Append(' ').Append(Reed()).Append(' ').Append(Lawn());
            AssetDatabase.SaveAssets();
            Debug.Log(log);
        }

        static NatureMeshKit Kit() => new NatureMeshKit(_m.Length);
        static string Save(string name, NatureMeshKit kit) => PropKit.Save(Dir, name, kit, _m, 0);

        /// <summary>
        /// 噴水の形の草の株：根元から長い葉が上へ伸び、外ほど寝て、先は長い弧で外へ垂れる（真ん中は立ち、外は垂れる）。
        /// 株ぜんたいを 1 つの縦長のドームとして光らせる（葉 1 本ずつの陰にしない）。
        /// </summary>
        static void Fountain(NatureMeshKit kit, Vector3 at, float height, float spread, int blades, float width, int seed, int sub = MEADOW, int rimSub = MEADOWTIP)
        {
            int from = kit.VertexCount;
            PropKit.Lump(kit, at, new Vector3(spread * 0.22f, height * 0.06f, spread * 0.22f), 4, 10, sub, seed, 0.15f, 2f, at.y);
            kit.Paint(from, (p, uv) => new Color(0f, 0f, 0f, 1f));
            for (int i = 0; i < blades; i++)
            {
                float az = PropKit.R(seed + 1, i, 0f, Mathf.PI * 2f), outer = Mathf.Sqrt(PropKit.H(seed + 2, i));   // 0 the middle .. 1 the rim
                var outDir = new Vector3(Mathf.Cos(az), 0f, Mathf.Sin(az));
                var root = at + outDir * (spread * 0.12f * outer);
                float rise = Mathf.Lerp(86f, 70f, outer) * Mathf.Deg2Rad, droop = Mathf.Lerp(0.15f, 0.5f, outer) * PropKit.R(seed + 3, i, 0.85f, 1.15f);
                float len = height * Mathf.Lerp(1.15f, 0.8f, outer) * PropKit.R(seed + 4, i, 0.85f, 1.1f);
                Func<float, Vector3> path = q => root + Vector3.up * (len * (Mathf.Sin(rise) * q - droop * q * q))
                                                      + outDir * (len * (Mathf.Cos(rise) * q + 0.3f * droop * q * q));
                int bf = kit.VertexCount;
                // pointed tips (a blunt end with an outline reads as a cut straw); the blade's face turned outward
                kit.Blade(path, q => width * Mathf.Pow(Mathf.Max(0f, 1f - q), 0.7f) * Mathf.Min(1f, 0.55f + q * 3f), 8, 0.004f, 0.15f, outer > 0.75f ? rimSub : sub,
                          Vector3.Cross(Vector3.up, outDir));
                int ii = i;
                kit.Paint(bf, (p, uv) => new Color(uv.y * 0.8f, uv.y * 0.4f, PropKit.H(seed + 5, ii), 1f));
            }
            kit.Mass(from, at + Vector3.up * (height * 0.45f), new Vector3(spread * 0.6f, height * 0.6f, spread * 0.6f), 0.75f);
        }

        // A: the meadow tuft - a tall fountain of grass (0.6 m, 0.5 m across) and two small ones
        static string Meadow()
        {
            var kit = Kit();
            const int seed = 2100;
            Fountain(kit, Vector3.zero, 0.6f, 0.5f, 40, 0.032f, seed);
            Fountain(kit, new Vector3(0.45f, 0f, 0.2f), 0.36f, 0.32f, 18, 0.026f, seed + 20);
            Fountain(kit, new Vector3(-0.38f, 0f, 0.3f), 0.26f, 0.26f, 14, 0.024f, seed + 40);
            return Save("Grass_A", kit);
        }

        /// <summary>幅広の葉の株：葉柄が根元から斜め上へ出て、先に広い葉が外向きにやや下がって付く。株ぜんたいを 1 つのドームとして光らせる。</summary>
        static void BroadLeaves(NatureMeshKit kit, Vector3 at, int leaves, float len, float width, float height, int seed)
        {
            int from = kit.VertexCount;
            for (int i = 0; i < leaves; i++)
            {
                var d = PropKit.Dir(360f * i / leaves + PropKit.R(seed, i, -18f, 18f));
                float hh = height * PropKit.R(seed + 1, i, 0.6f, 1f);
                var stalkTop = at + d * (len * 0.45f) + Vector3.up * hh;
                PropKit.Pipe(kit, t => at + d * (len * 0.45f * t * t) + Vector3.up * (hh * Mathf.Sin(t * Mathf.PI * 0.5f)), (t, a) => Mathf.Lerp(0.009f, 0.006f, t), 5, 5, STALK, -1, STALK);
                // the blade: out and a little down from the stalk top, broad in the middle, a soft point
                var dir = (d + Vector3.down * PropKit.R(seed + 2, i, 0.1f, 0.35f)).normalized;
                float l = len * PropKit.R(seed + 3, i, 0.85f, 1.1f);
                Func<float, Vector3> path = s => stalkTop + dir * (l * s) - Vector3.up * (0.25f * l * s * s);
                int bf = kit.VertexCount;
                kit.Blade(path, s => width * Mathf.Pow(Mathf.Max(0f, Mathf.Sin(Mathf.PI * Mathf.Lerp(0.06f, 1f, s))), 0.75f), 8, 0.006f, 0.2f, FOREST);
                int ii = i;
                kit.Paint(bf, (p, uv) => new Color(uv.y * 0.6f, uv.y * 0.4f, PropKit.H(seed + 4, ii), 1f));
            }
            kit.Mass(from, at + Vector3.up * (height * 0.5f), new Vector3(len * 1.3f, height, len * 1.3f), 0.7f);
        }

        // B: the forest floor - broad dark leaves on stalks, a hero clump and a small one
        static string ForestFloor()
        {
            var kit = Kit();
            const int seed = 2200;
            BroadLeaves(kit, Vector3.zero, 7, 0.24f, 0.1f, 0.3f, seed);
            BroadLeaves(kit, new Vector3(0.45f, 0f, -0.2f), 4, 0.15f, 0.065f, 0.18f, seed + 20);
            return Save("Grass_B", kit);
        }

        // C: reeds by the water - a dense dark mass of leaves below, and above it only a few bare stems with plumes
        // (leaves sticking straight out of thin stems read as bamboo or a fish bone)
        static string Reed()
        {
            var kit = Kit();
            const int seed = 2300;
            Fountain(kit, Vector3.zero, 0.7f, 0.6f, 46, 0.032f, seed + 50, REEDBASE, REEDBASE);
            Fountain(kit, new Vector3(0.34f, 0f, -0.14f), 0.42f, 0.4f, 20, 0.028f, seed + 70, REEDBASE, REEDBASE);
            for (int i = 0; i < 8; i++)
            {
                float az = PropKit.R(seed + 1, i, 0f, Mathf.PI * 2f), rr = Mathf.Sqrt(PropKit.H(seed + 2, i)) * 0.16f;
                var b = new Vector3(Mathf.Cos(az) * rr, 0.3f, Mathf.Sin(az) * rr);
                var lean = new Vector3(b.x, 0f, b.z) * 0.6f + PropKit.Dir(30f) * 0.12f;
                float tall = PropKit.R(seed + 3, i, 0.95f, 1.4f), sway = PropKit.H(seed + 4, i);
                Func<float, Vector3> stem = t => b + Vector3.up * (tall * t) + lean * (t * t);
                int sf = kit.VertexCount;
                PropKit.Pipe(kit, stem, (t, a) => Mathf.Lerp(0.014f, 0.008f, t), 8, 5, REED, -1, REED);
                kit.Paint(sf, (p, uv) => new Color(Mathf.Clamp01(p.y / 1.7f) * 0.8f, 0.2f, sway, 1f));
                // one leaf leaving the stem just above the mass and bending down under its own weight
                {
                    var at = stem(0.25f);
                    var od = PropKit.Dir(PropKit.R(seed + 5, i, 0f, 360f));
                    float l = PropKit.R(seed + 6, i, 0.4f, 0.55f);
                    Func<float, Vector3> leaf = s => at + (Vector3.up * 0.8f + od * 0.6f).normalized * (l * s) + od * (0.2f * l * s * s) - Vector3.up * (0.75f * l * s * s);
                    int lf = kit.VertexCount;
                    kit.Blade(leaf, s => 0.022f * Mathf.Pow(Mathf.Max(0f, 1f - s), 0.5f) * Mathf.Min(1f, 0.4f + s * 5f), 7, 0.003f, 0.15f, REED);
                    kit.Paint(lf, (p, uv) => new Color(Mathf.Clamp01(p.y / 1.7f) * 0.8f, uv.y * 0.6f, sway, 1f));
                }
                if (i != 3)
                {
                    var top = stem(1f);
                    var d = (stem(1f) - stem(0.96f)).normalized;
                    int pf = kit.VertexCount;
                    PropKit.Pipe(kit, t => top + d * (0.2f * t) + lean.normalized * (0.05f * t * t), (t, a) => 0.035f * Mathf.Sin(Mathf.PI * Mathf.Clamp(t, 0.06f, 1f)), 7, 6, PLUME, PLUME, PLUME);
                    kit.Paint(pf, (p, uv) => new Color(0.85f, 0.4f, sway, 1f));
                }
            }
            return Save("Grass_C", kit);
        }

        // D: a low lawn patch to lay on the ground - a flat ragged mass with tiny blade tips over it, no outline,
        // its rim breaking up into small crumbs so it melts into the ground (a hard dark edge reads as a mat laid down)
        static string Lawn()
        {
            var kit = Kit();
            const int seed = 2400;
            int from = kit.VertexCount;
            PropKit.Lump(kit, Vector3.zero, new Vector3(0.6f, 0.05f, 0.45f), 6, 32, LAWN, seed, 0.12f, 1.2f, 0f, null, 0f, 1f,
                n => 0.3f * PropKit.Noise(n * 4f, seed + 1) + 0.12f * PropKit.Noise(n * 11f, seed + 2));
            for (int i = 0; i < 9; i++)
            {
                float az = PropKit.R(seed + 5, i, 0f, Mathf.PI * 2f), rr = PropKit.R(seed + 6, i, 0.9f, 1.15f), cr = PropKit.R(seed + 7, i, 0.05f, 0.11f);
                PropKit.Lump(kit, new Vector3(Mathf.Cos(az) * 0.6f * rr, 0f, Mathf.Sin(az) * 0.45f * rr), new Vector3(cr, cr * 0.3f, cr * 0.8f), 4, 10, LAWN, seed + 10 + i, 0.25f, 2f, 0f);
            }
            kit.Paint(from, (p, uv) => new Color(0f, 0f, 0f, 1f));
            kit.Mass(from, new Vector3(0f, -0.1f, 0f), new Vector3(0.7f, 0.2f, 0.55f), 0.8f);
            int bf = kit.VertexCount;
            for (int i = 0; i < 70; i++)
            {
                float az = PropKit.R(seed + 2, i, 0f, Mathf.PI * 2f), rr = Mathf.Pow(PropKit.H(seed + 3, i), 0.4f) * 0.92f;
                var p0 = new Vector3(Mathf.Cos(az) * 0.6f * rr, 0f, Mathf.Sin(az) * 0.45f * rr);
                p0.y = 0.05f * Mathf.Sqrt(Mathf.Max(0f, 1f - rr * rr)) - 0.01f;
                var outward = new Vector3(p0.x, 0f, p0.z).normalized;
                float len = PropKit.R(seed + 4, i, 0.025f, 0.05f);
                var dir = (Vector3.up + outward * (0.3f + rr * 0.6f)).normalized;
                kit.Blade(q => p0 + dir * (len * q) + outward * (0.3f * len * q * q), q => 0.012f * Mathf.Pow(Mathf.Max(0f, 1f - q), 0.6f), 3, 0.003f, 0.1f, LAWNTIP);
            }
            kit.Paint(bf, (p, uv) => new Color(uv.y * 0.3f, uv.y * 0.3f, 0.5f, 1f));
            kit.Mass(bf, new Vector3(0f, -0.1f, 0f), new Vector3(0.7f, 0.25f, 0.55f), 0.6f);
            string s = Save("Grass_D", kit);

            // the forest's lawn: the same patch in a deeper green
            var forest = PropKit.Mats(Dir, "Grass",
                new PropKit.Spec("LawnForest", "#566A40", false, 0.02f),
                new PropKit.Spec("LawnForestTip", "#627848", false, 0.04f));
            foreach (var x in forest) { PropKit.CairnPaint(x); x.SetFloat("_OutlineWidth", 0f); }
            var mats = (Material[])_m.Clone();
            mats[LAWN] = forest[0];
            mats[LAWNTIP] = forest[1];
            NatureAssets.SavePrefab($"{Dir}/Grass_D_Forest.prefab", AssetDatabase.LoadAssetAtPath<Mesh>($"{Dir}/Grass_D_Mesh.asset"), mats, null);
            return s;
        }
    }
}
