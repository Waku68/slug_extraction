using System;
using UnityEditor;
using UnityEngine;

namespace SlugDesign
{
    /// <summary>
    /// ツキシロ（月白。Oct 27 灰色ラフ 3 案）。満月の夜、月が高い 2 時間だけ菌輪の上に咲く幻の花。白くて大きく、かすかに光る、香る。
    /// 花は手で摘む（また咲く）、根株はピッケルで掘る（spec 6章）。メニュー Slug/Nature/Build Tsukishiro（Assets/Art/Nature/Tsukishiro）。
    ///
    /// 大きさは本物寄り（わくさん Oct 27）：高さ 20〜25 cm。珍しさは色でなく形で：花がツユリよりずっと大きい（花の幅 13〜16 cm、人なら 50 cm の花）。
    /// - A 月の盃：地面に寝た葉のロゼットの真ん中から 1 本、上を向いた大きな盃の花（2 重の花びら）。月を受ける形
    /// - B 月下美人：茎が横へ曲がり、横を向いた大輪（細い花びらが 2 重、うしろに反った細いがく）
    /// - C 提灯：茎の先が曲がって、閉じかけた丸い花が垂れる。5 枚の花びらが玉を抱え、中の芯がのぞく（光を灯す形）
    /// Oct 27 わくさん：B 月下美人に決定。
    /// 色（仮）：花びら #EEF0F4（月白）、芯 #F2E6A8、茎 #6E7F55、葉 #5F7046。光は色が決まってから（芯と花びらの内側をかすかに）。
    /// </summary>
    public static class TsukishiroBuilder
    {
        const string Dir = "Assets/Art/Nature/Tsukishiro";
        const int STEM = 0, LEAF = 1, PETAL = 2, HEART = 3;
        const float OutlineWidth = 0.0025f;

        [MenuItem("Slug/Nature/Build Tsukishiro")]
        public static void Build()
        {
            System.IO.Directory.CreateDirectory(Dir);
            var mats = Materials();
            var log = new System.Text.StringBuilder("Tsukishiro built:");
            log.Append(' ').Append(Make("A", MoonCup, mats));
            log.Append(' ').Append(Make("B", NightQueen, mats));
            log.Append(' ').Append(Make("C", Lantern, mats));
            AssetDatabase.SaveAssets();
            Debug.Log(log);
        }

        static string Make(string variant, Action<NatureMeshKit> build, Material[] mats)
        {
            string name = $"Tsukishiro_{variant}";
            var kit = new NatureMeshKit(4);
            build(kit);
            var mesh = NatureAssets.SaveMesh(kit.ToMesh(name), $"{Dir}/{name}_Mesh.asset");
            NatureAssets.SavePrefab($"{Dir}/{name}.prefab", mesh, mats, null);
            return $"{name} {kit.TriangleCount} tris";
        }

        // A: a rosette on the ground, one stout stem, a big upturned double cup
        static void MoonCup(NatureMeshKit kit)
        {
            const int seed = 401;
            for (int i = 0; i < 6; i++)
                FlowerKit.FlatLeaf(kit, LEAF, Vector3.zero, i * 1.047f + 0.2f, 0.13f, 0.042f, 0.03f, 0.005f, 8, seed + i);
            var c = FlowerKit.Curve(Vector3.zero, Vector3.up, Vector3.right, 0.19f, t => 10f * t * t, out var axis);
            FlowerKit.Stem(kit, STEM, c, 0.009f, 0.0075f, 8, seed);
            Vector3 top = c(1f);
            kit.Ball(top - axis * 0.006f, new Vector3(0.016f, 0.014f, 0.016f), 5, STEM);
            FlowerKit.PetalRing(kit, PETAL, top, axis, 6, 0.08f, 0.03f, 52f, 26f, 0f, 0.006f, -0.25f, 7, seed + 10, 0.8f);
            FlowerKit.PetalRing(kit, PETAL, top + axis * 0.006f, axis, 6, 0.062f, 0.024f, 24f, 18f, 0.52f, 0.006f, -0.3f, 7, seed + 20, 0.8f);
            kit.Ball(top + axis * 0.02f, new Vector3(0.016f, 0.014f, 0.016f), 5, HEART);
        }

        // B: the stem leans over to hold a big sideways bloom, thin sepals swept back behind it
        static void NightQueen(NatureMeshKit kit)
        {
            const int seed = 503;
            for (int i = 0; i < 3; i++)
                FlowerKit.StrapLeaf(kit, LEAF, Vector3.zero, i * 2.1f + 1.4f, 38f, 0.2f, 0.022f, 0.4f, 0.006f, 8, seed + i);
            var c = FlowerKit.Curve(Vector3.zero, Vector3.up, Vector3.right, 0.22f, t => 72f * Mathf.Pow(t, 1.6f), out var axis);
            FlowerKit.Stem(kit, STEM, c, 0.008f, 0.0065f, 8, seed);
            Vector3 top = c(1f);
            var flowerAxis = (axis + Vector3.up * 0.35f).normalized;
            kit.Ball(top - flowerAxis * 0.004f, new Vector3(0.012f, 0.012f, 0.012f), 4, STEM);
            FlowerKit.PetalRing(kit, STEM, top, flowerAxis, 8, 0.07f, 0.007f, 85f, 45f, 0.2f, 0.004f, 0f, 6, seed + 5);
            FlowerKit.PetalRing(kit, PETAL, top + flowerAxis * 0.008f, flowerAxis, 9, 0.085f, 0.021f, 58f, 22f, 0f, 0.005f, -0.25f, 7, seed + 10);
            FlowerKit.PetalRing(kit, PETAL, top + flowerAxis * 0.012f, flowerAxis, 7, 0.06f, 0.023f, 30f, 12f, 0.45f, 0.005f, -0.3f, 7, seed + 20, 0.8f);
            kit.Ball(top + flowerAxis * 0.028f, new Vector3(0.012f, 0.012f, 0.012f), 4, HEART);
        }

        // C: a crooked stem hangs a round, half-closed lantern: five petals hug a glowing core
        static void Lantern(NatureMeshKit kit)
        {
            const int seed = 607;
            for (int i = 0; i < 3; i++)
                FlowerKit.FlatLeaf(kit, LEAF, Vector3.zero, i * 2.09f + 0.5f, 0.1f, 0.045f, 0.035f, 0.005f, 8, seed + i);
            var c = FlowerKit.Curve(Vector3.zero, Vector3.up, Vector3.right, 0.27f, t => t < 0.62f ? 5f * t : 3.1f + 140f * Mathf.Pow((t - 0.62f) / 0.38f, 1.3f), out var axis);
            FlowerKit.Stem(kit, STEM, c, 0.0075f, 0.0055f, 8, seed);
            Vector3 top = c(1f);
            kit.Ball(top - axis * 0.004f, new Vector3(0.011f, 0.011f, 0.011f), 4, STEM);
            FlowerKit.PetalRing(kit, PETAL, top, axis, 5, 0.15f, 0.045f, 62f, -102f, 0f, 0.006f, -0.35f, 8, seed + 10, 0.8f);
            kit.Ball(top + axis * 0.055f, new Vector3(0.022f, 0.024f, 0.022f), 5, HEART);
        }

        static Material[] Materials()
        {
            var m = new[]
            {
                NatureAssets.Mat($"{Dir}/Tsukishiro_Stem.mat", "#6E7F55", false, 0.02f),
                NatureAssets.Mat($"{Dir}/Tsukishiro_Leaf.mat", "#5F7046", false, 0.02f),
                NatureAssets.Mat($"{Dir}/Tsukishiro_Petal.mat", "#EEF0F4", false, 0.02f),
                NatureAssets.Mat($"{Dir}/Tsukishiro_Heart.mat", "#F2E6A8", false, 0.02f),
            };
            foreach (var x in m) { x.SetFloat("_Facet", 0f); x.SetFloat("_OutlineWidth", OutlineWidth); }   // 小さな草花は輪郭線を細く
            return m;
        }
    }
}
