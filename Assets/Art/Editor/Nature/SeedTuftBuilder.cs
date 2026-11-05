using UnityEditor;
using UnityEngine;

namespace SlugDesign
{
    /// <summary>
    /// 種の房（Nov 3 下書き、わくさんのペルソナが監督）。花の草原で乾季の後半に実る。手で摘む。拠点の苗床に植える。触るとカラカラ鳴る（音はわくさん）。
    /// 形：枯れた茎の先が重さでうつむき、丸いさやが房になって垂れる。乾いて硬いので茎とさやはカクカク（かじれない）、枯れ葉だけなめらか。
    /// メニュー Slug/Nature/Build Seed Tuft（Assets/Art/Nature/SeedTuft）。大きさは本物寄り。ナメクジの目の高さ 0.47 m は人の約 1/3.6。
    /// - A：茎 3 本の株。主役の茎 高さ 45 cm（人なら 1.6 m）に さや 6 個の房、子の茎 34 cm に 4 個・27 cm に 3 個。茎の太さ 1.8 cm（ポップに太く）、
    ///   さやは直径 4〜5 cm、先が暗い。根元に短い枯れ葉 3 枚が垂れる（長い葉を地面に広げるとタコの脚に見えた）
    /// 色：茎 #B09A6A、さや #C4A464・さやの先 #8E7448、枯れ葉 #BCA878。当たり判定なし。
    /// </summary>
    public static class SeedTuftBuilder
    {
        const string Dir = "Assets/Art/Nature/SeedTuft";
        const int STEM = 0, POD = 1, PODTIP = 2, LEAF = 3;
        const float OutlineWidth = 0.003f;

        [MenuItem("Slug/Nature/Build Seed Tuft")]
        public static void Build()
        {
            System.IO.Directory.CreateDirectory(Dir);
            var mats = new[]
            {
                NatureAssets.Mat($"{Dir}/SeedTuft_Stem.mat", "#B09A6A", false, 0.04f),
                NatureAssets.Mat($"{Dir}/SeedTuft_Pod.mat", "#C4A464", false, 0.04f),
                NatureAssets.Mat($"{Dir}/SeedTuft_PodTip.mat", "#8E7448", false, 0.04f),
                NatureAssets.Mat($"{Dir}/SeedTuft_Leaf.mat", "#BCA878", false, 0.04f),
            };
            for (int i = 0; i < mats.Length; i++) { mats[i].SetFloat("_Facet", i == LEAF ? 0f : 1f); mats[i].SetFloat("_OutlineWidth", OutlineWidth); }
            var kit = new NatureMeshKit(4);
            const int seed = 4300;
            // a few short dry leaves hanging down round the foot (long ones spread on the ground read as octopus legs)
            for (int i = 0; i < 3; i++)
                FlowerKit.StrapLeaf(kit, LEAF, Vector3.up * 0.04f, i * 2.1f + 0.3f, 20f, PropKit.R(seed + 1, i, 0.06f, 0.08f), 0.012f, 0.9f, 0.003f, 6, seed + i);
            Stalk(kit, PropKit.Dir(30f), 0.45f, 6, 1.5f, seed + 10);
            Stalk(kit, PropKit.Dir(200f), 0.34f, 4, 1.35f, seed + 30);
            Stalk(kit, PropKit.Dir(300f), 0.27f, 3, 1.25f, seed + 50);
            string name = "SeedTuft_A";
            var mesh = NatureAssets.SaveMesh(kit.ToMesh(name), $"{Dir}/{name}_Mesh.asset");
            NatureAssets.SavePrefab($"{Dir}/{name}.prefab", mesh, mats, null);
            AssetDatabase.SaveAssets();
            Debug.Log($"Seed tuft built: {name} {kit.TriangleCount}");
        }

        /// <summary>枯れた茎 1 本：まっすぐ立って、先が重さでうつむく。うつむいた先の 3 割に、短い柄でさやが房に下がる。</summary>
        static void Stalk(NatureMeshKit kit, Vector3 toward, float len, int pods, float podScale, int seed)
        {
            var c = FlowerKit.Curve(new Vector3(0f, 0.004f, 0f), Vector3.up, toward, len, t => t < 0.55f ? 6f * t : 3.3f + 120f * Mathf.Pow((t - 0.55f) / 0.45f, 1.6f), out _);
            int from = kit.VertexCount;
            PropKit.Pipe(kit, c, (t, a) => Mathf.Lerp(0.009f, 0.006f, t), 16, 6, STEM, STEM, STEM);
            kit.Paint(from, (p, uv) => new Color(uv.y * 0.7f, 0f, NatureAssets.Hash(seed, 7), 1f));
            for (int i = 0; i < pods; i++)
            {
                float t = Mathf.Lerp(0.72f, 0.99f, (i + 0.5f) / pods);
                var at = c(t);
                var side = PropKit.Dir(PropKit.R(seed + 1, i, 0f, 360f));
                float stalk = PropKit.R(seed + 2, i, 0.012f, 0.03f), pr = PropKit.R(seed + 3, i, 0.013f, 0.016f) * podScale;
                var pod = at + side * (stalk * 0.6f) + Vector3.down * (stalk + pr * 0.9f);
                int pf = kit.VertexCount;
                PropKit.Stick(kit, at, pod + Vector3.up * pr * 0.8f, 0.0028f, 0.0022f, 5, STEM);
                kit.Ball(pod, new Vector3(pr, pr * 1.2f, pr), 4, POD);
                kit.Ball(pod + Vector3.down * (pr * 1.05f), new Vector3(pr * 0.45f, pr * 0.35f, pr * 0.45f), 3, PODTIP);
                kit.Paint(pf, (p, uv) => new Color(0.75f, 0.2f, NatureAssets.Hash(seed, 7), 1f));
            }
        }
    }
}
