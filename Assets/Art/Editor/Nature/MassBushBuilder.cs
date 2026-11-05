using System.Text;
using UnityEditor;
using UnityEngine;

namespace SlugDesign
{
    /// <summary>
    /// ケルン風の低い茂み（Nov 2 試し）。メニュー Slug/Nature/Props/Build Mass Bush で作り直す（Assets/Art/Nature/MassBush）。
    /// 考え方（docs/modeling/README.md 8章）：葉を 1 枚ずつ描かず、大きな塊 3〜5 個で見せる。陰は塊の丸みで付け（NatureMeshKit.Mass）、
    /// 塊の中は描き込まない。輪郭からだけ葉先を少し飛び出させる（切り絵のふち）。明るい所は黄みの緑、陰は青みの緑で、段は 3 つ。
    /// 群れは主役 1 つ（A）と小さい子（B）。前の下書き（UnderstoryBuilder の LowBush）と同じ大きさで比べる。
    /// - A 主役：幅 1.2 m・高さ 0.75 m（ナメクジの目より上。人なら 2.7 m の茂み）、塊 5 個、葉先は塊ごとに 16 枚まで
    /// - B 子：幅 0.6 m・高さ 0.42 m（ナメクジの目の高さくらい。人なら 1.5 m）、塊 3 個、葉先は塊ごとに 12 枚まで
    /// 色：葉 #6E8448（陰は青み (0.5, 0.62, 0.74)）、葉先 #8A9E58、枝 #5E5040（硬いのでカクカク）。葉と葉先はかじれるのでなめらか、風で揺れる。
    /// </summary>
    public static class MassBushBuilder
    {
        const string Dir = "Assets/Art/Nature/MassBush";
        const int LEAF = 0, TIP = 1, TWIG = 2;
        static Material[] _m;

        [MenuItem("Slug/Nature/Props/Build Mass Bush")]
        public static void Build()
        {
            _m = PropKit.Mats(Dir, "MassBush",
                new PropKit.Spec("Leaf", "#6E8448", false, 0.03f).Mottle("#7A9050", 0.3f, 3f),
                new PropKit.Spec("Tip", "#8A9E58", false, 0.05f),
                new PropKit.Spec("Twig", "#5E5040", true));
            // painted zones: crisp bands, a cool blue-green shade, a dark green outline
            foreach (var m in new[] { _m[LEAF], _m[TIP] })
            {
                m.SetColor("_ShadeColor", new Color(0.5f, 0.62f, 0.74f));
                m.SetColor("_OutlineColor", PropKit.Hex("#2E3E2C"));
                m.SetFloat("_BandSoftness", 0.04f);
                m.SetFloat("_ToonSmooth", 0.05f);
                EditorUtility.SetDirty(m);
            }
            var log = new StringBuilder("Mass bush built:");
            log.Append(' ').Append(Bush(0)).Append(' ').Append(Bush(1));
            AssetDatabase.SaveAssets();
            Debug.Log(log);
        }

        static string Bush(int v)
        {
            var kit = new NatureMeshKit(_m.Length);
            float w = v == 0 ? 1.2f : 0.6f, h = v == 0 ? 0.75f : 0.42f;
            int seed = 700 + v * 40, tipsPer = v == 0 ? 16 : 12;
            float floor = h * 0.14f;   // the masses sit a little off the ground: the stems show under them

            // the masses: one big middle, the rest around and on top (upper ones smaller)
            var cs = new System.Collections.Generic.List<(Vector3 c, Vector3 r)>();
            cs.Add((new Vector3(0f, h * 0.46f, 0f), new Vector3(w * 0.36f, h * 0.4f, w * 0.34f)));
            int around = v == 0 ? 3 : 2;
            for (int i = 0; i < around; i++)
            {
                var d = PropKit.Dir(360f * i / around + PropKit.R(seed, i, -25f, 25f));
                float rr = w * PropKit.R(seed + 1, i, 0.2f, 0.25f);
                cs.Add((d * (w * 0.24f) + Vector3.up * (h * PropKit.R(seed + 2, i, 0.36f, 0.48f)), new Vector3(rr, rr * 0.85f, rr)));
            }
            if (v == 0) cs.Add((new Vector3(w * 0.06f, h * 0.7f, -w * 0.04f), new Vector3(w * 0.21f, h * 0.27f, w * 0.2f)));

            for (int k = 0; k < cs.Count; k++)
            {
                var (c, r) = cs[k];
                int ks = seed + 10 + k * 7;
                var rot = Quaternion.Euler(0f, ks * 37f, 0f);
                // the mass: a smooth squashed ball with a few soft scallops (only the outline shows them)
                int from = kit.VertexCount;
                System.Func<Vector3, float> scallops = n => 0.1f * Mathf.Abs(PropKit.Noise(n * 6f, ks + 1));
                PropKit.Lump(kit, c, r, 10, 18, LEAF, ks, 0.1f, 1.4f, floor, rot, 0f, 1f, scallops);
                kit.Paint(from, (p, uv) => new Color(Mathf.Clamp01(p.y / h) * 0.5f, 0.3f, PropKit.H(ks, 3), 1f));
                kit.Mass(from, c, r, 1f);                 // the mass lights as one ball

                // leaf tips: short pointed blades out of the upper and outer side, so the outline reads leafy
                int tipFrom = kit.VertexCount;
                for (int i = 0; i < tipsPer; i++)
                {
                    float az = PropKit.R(ks + 2, i, 0f, Mathf.PI * 2f), el = Mathf.Asin(PropKit.R(ks + 3, i, -0.15f, 0.85f));
                    var n = new Vector3(Mathf.Cos(el) * Mathf.Cos(az), Mathf.Sin(el), Mathf.Cos(el) * Mathf.Sin(az));
                    // half sunk in the mass and pointing up and out, so they overlap like shingles and only their rounded ends show
                    var root = c + Vector3.Scale(n, r) * 0.82f;
                    if (root.y < floor + 0.04f) continue;
                    var dir = (n * 0.7f + Vector3.up * 0.6f).normalized;
                    float len = (v == 0 ? 0.12f : 0.085f) * PropKit.R(ks + 4, i, 0.8f, 1.2f), wid = len * 0.42f;
                    System.Func<float, Vector3> path = s => root + dir * (len * s) - Vector3.up * (0.2f * len * s * s);
                    int bf = kit.VertexCount;
                    kit.Blade(path, s => wid * Mathf.Pow(Mathf.Max(0f, Mathf.Sin(Mathf.PI * Mathf.Min(1f, s * 0.9f + 0.1f))), 0.6f), 3, 0.006f, 0.15f, TIP);
                    int ii = i;
                    kit.Paint(bf, (p, uv) => new Color(Mathf.Clamp01(p.y / h) * 0.5f + 0.1f, uv.y, PropKit.H(ks + 5, ii), 1f));
                }
                kit.Mass(tipFrom, c, r, 0.8f);            // and the tips with it (a little of their own lean left)
            }
            // then the whole bush leans towards one dome, so the masses read as one bush in the light
            kit.Mass(0, new Vector3(0f, h * 0.45f, 0f), new Vector3(w * 0.5f, h * 0.55f, w * 0.5f), 0.45f);

            // a few short crooked stems from the ground up into the masses
            for (int i = 0; i < (v == 0 ? 4 : 3); i++)
            {
                var d = PropKit.Dir(i * 100f + PropKit.R(seed + 30, i, -20f, 20f));
                PropKit.Branch(kit, Vector3.zero, d * 0.5f + Vector3.up, h * 0.38f, v == 0 ? 0.025f : 0.018f, 1, seed + 31 + i, 5, TWIG, 30f);
            }
            return PropKit.Save(Dir, $"MassBush_{(char)('A' + v)}", kit, _m, 0);
        }
    }
}
