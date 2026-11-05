using System.Text;
using UnityEditor;
using UnityEngine;

namespace SlugDesign
{
    /// <summary>
    /// 足元の層（Nov 2 下書き、3 案ずつ。docs/ideas/map_clutter_ideas.md の A）。メニュー Slug/Nature/Props/Build Ground Litter で作り直す
    /// （Assets/Art/Nature/GroundLitter）。見るだけで当たり判定なし、高さ 15 cm 以下。知っている物なので本物の大きさ。
    /// 落ち葉・苔・きのこはかじれるのでなめらか、小枝・小石・軽石はカクカク。ナメクジの目の高さ 0.47 m は人の約 1/3.6。
    /// - FallenLeaves 落ち葉：A 葉 12 枚（12〜22 cm）が幅 1 m に散る。B 大きな茶色の葉 6 枚（28〜32 cm、人なら 1 m の葉）がよく丸まる。C 20 枚の吹きだまり
    /// - Twigs 小枝：A 6 本（20〜40 cm）が散る。B 二股の枝 1 本 60 cm。C 7 本が重なる
    /// - Pebbles 小石：A 小石 10 個（3〜8 cm）。B 平たい小石 25 個の州。C 軽石 5 個（5〜15 cm、穴だらけで白っぽい。溶岩原）
    /// - MossPatch 苔のむら：A 平たいふとん 幅 0.6 m・厚み 5 cm。B 小さいふとん 5 つ。C ふとんから胞子の柄が 20 本（3〜5 cm）
    /// - SmallMushrooms 小さいきのこ：A 茶色のかさ 5 本の株（高さ 4〜8 cm）。B 白い柄の背高 1 本 12 cm。C 白い小さいの 7 本が小さな輪に（3 cm）
    /// 色：落ち葉 #A0804F・#B8A05A・#7A6440・#7E8A4C、小枝 #8A7660、小石 #A39C8E・#7E786C、軽石 #B8AE9C、苔 #73804C・#8A9A55、
    /// 胞子の柄 #9A5A3A、かさ #A06A48、柄 #E0D4BC、白いかさ #E8E2D2、ひだ #8A7A64。
    /// </summary>
    public static class GroundLitterBuilder
    {
        const string Dir = "Assets/Art/Nature/GroundLitter";
        const int LEAFBROWN = 0, LEAFYELLOW = 1, LEAFDARK = 2, LEAFGREEN = 3, TWIG = 4, PEBBLE = 5, PEBBLEDARK = 6, PUMICE = 7,
                  MOSS = 8, MOSSLIGHT = 9, SPORE = 10, CAP = 11, STEM = 12, WHITECAP = 13, GILL = 14;
        static Material[] _m;

        [MenuItem("Slug/Nature/Props/Build Ground Litter")]
        public static void Build()
        {
            _m = PropKit.Mats(Dir, "Litter",
                new PropKit.Spec("LeafBrown", "#A0804F", false, 0f, true).Mottle("#7A5A36", 0.55f, 22f).Felt(0.3f),
                new PropKit.Spec("LeafYellow", "#B8A05A", false, 0f, true).Mottle("#9A7A3E", 0.5f, 22f).Felt(0.3f),
                new PropKit.Spec("LeafDark", "#7A6440", false, 0f, true).Mottle("#5A4830", 0.5f, 22f).Felt(0.3f),
                new PropKit.Spec("LeafGreen", "#7E8A4C", false, 0f, true).Mottle("#9A8A4A", 0.45f, 18f).Felt(0.25f),
                new PropKit.Spec("Twig", "#8A7660", true).Mottle("#6A5A48", 0.5f, 30f).Dent(0.4f, 120f),
                new PropKit.Spec("Pebble", "#A39C8E", true).Mottle("#86807A", 0.6f, 16f).Camo("#ADA799", "#928C80", "#B4AE9C", 14f, 0.5f).Dent(0.3f, 80f),
                new PropKit.Spec("PebbleDark", "#7E786C", true).Mottle("#66615A", 0.6f, 16f).Camo("#88827A", "#6E695F", "#8E8878", 14f, 0.5f).Dent(0.3f, 80f),
                new PropKit.Spec("Pumice", "#B8AE9C", true).Mottle("#8E8676", 0.7f, 30f).Dent(1.2f, 140f),
                new PropKit.Spec("Moss", "#73804C", false).Mottle("#7E8C52", 0.3f, 5f),
                new PropKit.Spec("MossLight", "#8A9A55", false).Mottle("#96A660", 0.3f, 5f),
                new PropKit.Spec("Spore", "#9A5A3A", false),
                new PropKit.Spec("Cap", "#A06A48", false).Mottle("#C8946A", 0.5f, 60f).Felt(0.2f),
                new PropKit.Spec("Stem", "#E0D4BC", false).Felt(0.3f),
                new PropKit.Spec("WhiteCap", "#E8E2D2", false).Mottle("#CFC2A4", 0.45f, 60f).Felt(0.2f),
                new PropKit.Spec("Gill", "#8A7A64", false));
            // the soft plant parts are painted the Cairn way (README 8): crisp bands, cool shade, no fine grain
            foreach (int i in new[] { LEAFBROWN, LEAFYELLOW, LEAFDARK, LEAFGREEN }) PropKit.CairnPaint(_m[i], "#4A3E2A", 0.6f, 0.62f, 0.72f);
            foreach (int i in new[] { MOSS, MOSSLIGHT, SPORE }) PropKit.CairnPaint(_m[i]);
            var log = new StringBuilder("Ground litter built:");
            for (int v = 0; v < 3; v++)
                log.Append(' ').Append(Leaves(v)).Append(' ').Append(Twigs(v)).Append(' ').Append(Pebbles(v))
                   .Append(' ').Append(Moss(v)).Append(' ').Append(Mushrooms(v));
            AssetDatabase.SaveAssets();
            Debug.Log(log);
        }

        static string Abc(int v) => ((char)('A' + v)).ToString();
        static NatureMeshKit Kit() => new NatureMeshKit(_m.Length);
        static string Save(string name, NatureMeshKit kit) => PropKit.Save(Dir, name, kit, _m, 0);

        static string Leaves(int v)
        {
            var kit = Kit();
            int n = v == 0 ? 12 : v == 1 ? 6 : 20;
            float spread = v == 0 ? 0.5f : v == 1 ? 0.45f : 0.28f;
            int[] cols = v == 1 ? new[] { LEAFBROWN, LEAFDARK } : new[] { LEAFBROWN, LEAFYELLOW, LEAFDARK, LEAFGREEN };
            for (int i = 0; i < n; i++)
            {
                int s = 300 + v * 40;
                float len = v == 1 ? PropKit.R(s, i, 0.28f, 0.32f) : PropKit.R(s, i, 0.12f, 0.22f);
                float a = PropKit.R(s + 1, i, 0f, Mathf.PI * 2f), d = Mathf.Sqrt(PropKit.H(s + 2, i)) * spread;
                // the drift (C) heaps up in the middle
                float y = 0.008f + (v == 2 ? 0.05f * (1f - d / spread) * PropKit.H(s + 3, i) : 0.004f * (i % 3));
                var root = new Vector3(Mathf.Cos(a) * d, y, Mathf.Sin(a) * d);
                var dir = PropKit.Dir(PropKit.R(s + 4, i, 0f, 360f));
                PropKit.FlatLeaf(kit, root - dir * len * 0.5f, dir, len, len * 0.28f, v == 1 ? PropKit.R(s + 5, i, 0.04f, 0.08f) : PropKit.R(s + 5, i, 0.005f, 0.035f),
                    v == 1 ? 0.35f : 0.18f, 10, cols[i % cols.Length], PropKit.R(s + 6, i, -18f, 18f), LEAFDARK);
            }
            return Save($"FallenLeaves_{Abc(v)}", kit);
        }

        static void Twig(NatureMeshKit kit, Vector3 mid, float yaw, float len, float r, int seed, int depth)
        {
            var d = PropKit.Dir(yaw);
            PropKit.Branch(kit, mid - d * (len * 0.5f) + Vector3.up * r, d + Vector3.up * 0.02f, len, r, depth, seed, 5, TWIG, 30f, 0.05f);
        }

        static string Twigs(int v)
        {
            var kit = Kit();
            int from = kit.VertexCount;
            if (v == 0)
                for (int i = 0; i < 6; i++)
                    Twig(kit, new Vector3(PropKit.R(320, i, -0.4f, 0.4f), 0f, PropKit.R(321, i, -0.4f, 0.4f)), PropKit.R(322, i, 0f, 360f), PropKit.R(323, i, 0.2f, 0.4f), PropKit.R(324, i, 0.008f, 0.012f), 325 + i, i % 2);
            else if (v == 1)
                Twig(kit, Vector3.zero, 10f, 0.4f, 0.02f, 331, 2);
            else
                for (int i = 0; i < 7; i++)
                    Twig(kit, new Vector3(PropKit.R(332, i, -0.08f, 0.08f), 0.012f * (i % 3), PropKit.R(333, i, -0.08f, 0.08f)), PropKit.R(334, i, -40f, 40f) + (i % 2) * 70f, PropKit.R(335, i, 0.25f, 0.45f), 0.011f, 336 + i, 0);
            // twigs lie down: press anything that rose too high back toward the ground
            kit.Transform(from, p => new Vector3(p.x, Mathf.Min(p.y, 0.03f + p.y * 0.25f), p.z));
            return Save($"Twigs_{Abc(v)}", kit);
        }

        static string Pebbles(int v)
        {
            var kit = Kit();
            int n = v == 0 ? 10 : v == 1 ? 25 : 5;
            for (int i = 0; i < n; i++)
            {
                int s = 340 + v * 20;
                float a = PropKit.R(s, i, 0f, Mathf.PI * 2f), d = Mathf.Sqrt(PropKit.H(s + 1, i)) * (v == 1 ? 0.6f : v == 0 ? 0.4f : 0.3f);
                float r = v == 2 ? PropKit.R(s + 2, i, 0.025f, 0.075f) : PropKit.R(s + 2, i, 0.015f, 0.04f);
                float flat = v == 1 ? 0.4f : v == 2 ? 0.75f : 0.6f;
                var size = new Vector3(r, r * flat, r * PropKit.R(s + 3, i, 0.7f, 1f));
                int sub = v == 2 ? PUMICE : (i % 3 == 0 ? PEBBLEDARK : PEBBLE);
                System.Func<Vector3, float> pits = null;
                if (v == 2) { int ps = s + i; pits = q => -0.3f * Mathf.Pow(Mathf.Max(0f, PropKit.Noise(q * 6f, ps)), 1.5f) - 0.12f * Mathf.Abs(PropKit.Noise(q * 13f, ps + 1)); }
                int seed = s + 5 + i;
                System.Func<Vector3, float> shape = pits ?? (q => PropKit.Chips(q, seed, 0.18f, 2.5f));
                PropKit.Lump(kit, new Vector3(Mathf.Cos(a) * d, size.y * 0.6f, Mathf.Sin(a) * d), size, v == 2 ? 8 : 5, v == 2 ? 12 : 8, sub, seed,
                    v == 2 ? 0.3f : 0.15f, 1.6f, 0f, Quaternion.Euler(0f, PropKit.R(s + 4, i, 0f, 360f), 0f), 0f, 1f, shape, null, v == 2 ? 2f : 3.2f);
            }
            return Save($"Pebbles_{Abc(v)}", kit);
        }

        /// <summary>苔のふとん：上の面が細かくでこぼこ、明るい色の皮を上にかぶせる。</summary>
        static void Cushion(NatureMeshKit kit, Vector3 c, float rx, float rz, float h, int seed)
        {
            System.Func<Vector3, float> tufts = n => 0.35f * Mathf.Abs(PropKit.Noise(n * 9f, seed + 3)) * Mathf.Max(0f, n.y);
            int from = kit.VertexCount;
            PropKit.Lump(kit, c, new Vector3(rx, h, rz), 8, 28, MOSS, seed, 0.3f, 3f, 0f, null, 0f, 1f, tufts);
            PropKit.Lump(kit, c, new Vector3(rx, h, rz), 6, 28, MOSSLIGHT, seed, 0.3f, 3f, float.NaN, null, 0.82f, 1.06f, tufts);
            kit.Mass(from, c, new Vector3(rx, h * 1.3f, rz), 0.85f);   // the cushion lights as one soft mound
        }

        static string Moss(int v)
        {
            var kit = Kit();
            if (v == 0) Cushion(kit, Vector3.zero, 0.32f, 0.27f, 0.05f, 360);
            else if (v == 1)
                for (int i = 0; i < 5; i++)
                {
                    float a = i * 72f + PropKit.R(361, i, -20f, 20f);
                    Cushion(kit, PropKit.Dir(a) * PropKit.R(362, i, 0.1f, 0.35f), PropKit.R(363, i, 0.06f, 0.12f), PropKit.R(364, i, 0.05f, 0.1f), PropKit.R(365, i, 0.025f, 0.045f), 366 + i);
                }
            else
            {
                Cushion(kit, Vector3.zero, 0.25f, 0.22f, 0.045f, 370);
                for (int i = 0; i < 20; i++)
                {
                    float a = PropKit.R(371, i, 0f, Mathf.PI * 2f), d = Mathf.Sqrt(PropKit.H(372, i)) * 0.8f;
                    var b = new Vector3(Mathf.Cos(a) * d * 0.25f, 0.035f * (1f - d * d), Mathf.Sin(a) * d * 0.22f);
                    float h = PropKit.R(373, i, 0.03f, 0.05f);
                    var top = b + new Vector3(PropKit.R(374, i, -0.008f, 0.008f), h, PropKit.R(375, i, -0.008f, 0.008f));
                    PropKit.Stick(kit, b, top, 0.0025f, 0.002f, 4, SPORE);
                    kit.Ball(top, new Vector3(0.004f, 0.007f, 0.004f), 3, SPORE);
                }
            }
            return Save($"MossPatch_{Abc(v)}", kit);
        }

        /// <summary>きのこ 1 本：根元 at、高さ h、柄の太さ sr、かさの半径 cr・厚み ch、lean だけ傾く。</summary>
        static void Mushroom(NatureMeshKit kit, Vector3 at, float h, float sr, float cr, float ch, Vector3 lean, int capSub)
        {
            Vector3 top = at + Vector3.up * h + lean;
            PropKit.Pipe(kit, t => Vector3.Lerp(at, top, t) - Vector3.up * (0.005f * (1f - t)), (t, a) => sr * (1f + 0.4f * Mathf.Exp(-t * 6f)) * Mathf.Lerp(1f, 0.85f, t), 3, 7, STEM, STEM, -1);
            Vector3 b = top - Vector3.up * (ch * 0.35f);
            // the cap: from the gills under it out to the rim, then up over the dome to the top
            PropKit.Surface(kit, (t, a) =>
            {
                var d = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                if (t < 0.3f) return b + d * Mathf.Lerp(sr, cr, t / 0.3f) + Vector3.up * (ch * 0.08f * t / 0.3f - ch * 0.12f * Mathf.Abs(Mathf.Sin(a * 14f)) * Mathf.Sin(Mathf.PI * t / 0.3f));
                float th = (t - 0.3f) / 0.7f * Mathf.PI * 0.5f;
                return b + d * (cr * Mathf.Cos(th) * (1f + 0.04f * Mathf.Sin(th * 2f))) + Vector3.up * (ch * 0.08f + ch * Mathf.Sin(th));
            }, 9, 56, capSub, -1, -1, 0f, t => t < 0.27f ? GILL : capSub);
        }

        static string Mushrooms(int v)
        {
            var kit = Kit();
            if (v == 0)
                for (int i = 0; i < 5; i++)
                {
                    var at = new Vector3(PropKit.R(380, i, -0.05f, 0.05f), 0f, PropKit.R(381, i, -0.05f, 0.05f));
                    float h = PropKit.R(382, i, 0.04f, 0.08f);
                    Mushroom(kit, at, h, 0.005f, PropKit.R(383, i, 0.015f, 0.03f), 0.015f, at.normalized * 0.012f, CAP);
                }
            else if (v == 1)
                Mushroom(kit, Vector3.zero, 0.12f, 0.008f, 0.035f, 0.028f, new Vector3(0.01f, 0f, 0f), WHITECAP);
            else
                for (int i = 0; i < 7; i++)
                {
                    var at = PropKit.Dir(i * 360f / 7f + PropKit.R(384, i, -10f, 10f)) * 0.15f;
                    Mushroom(kit, at, PropKit.R(385, i, 0.025f, 0.035f), 0.004f, PropKit.R(386, i, 0.012f, 0.018f), 0.01f, Vector3.zero, WHITECAP);
                }
            return Save($"SmallMushrooms_{Abc(v)}", kit);
        }
    }
}
