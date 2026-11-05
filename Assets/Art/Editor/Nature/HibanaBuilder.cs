using UnityEditor;
using UnityEngine;

namespace SlugDesign
{
    /// <summary>
    /// ヒバナ（火花。Nov 3 下書き、わくさんのペルソナが監督）。黒い溶岩原の割れ目と溶岩洞の天窓の下に、溶岩原に雨が降った翌日だけ咲く、まれな赤い花。
    /// 手で摘む。染め場の赤。黒い溶岩に赤が 1 点（島でいちばん鮮やかな赤はこれだけ。ほかの木の赤はくすませる）。
    /// 形は火花：雨のあとに咲くレインリリー＋ヒガンバナ。葉はなく、細い茎の先に花が 1 つずつ、細い花びらが反り返り、長いしべが火の粉のように外へ散る。
    /// メニュー Slug/Nature/Build Hibana（Assets/Art/Nature/Hibana）。大きさは本物寄り（ほかの草花と同じ）。ナメクジの目の高さ 0.47 m は人の約 1/3.6。
    /// - A 咲いた株：大きい花 1 つ（茎 26 cm、人なら 95 cm。花の幅 15 cm）と小さい花 2 つ（茎 15〜18 cm、花は 7 割）
    /// - B 芽（雨の日のうちに割れ目から赤い芽だけ出る。「明日来よう」の合図なので、数 m 先から見つかる大きさ）：高さ 11〜16 cm、太さ 2 cm の
    ///   先のとがった赤い芽 3 本を固めて
    /// 色：花びら #C8402F、しべ #D85A38・しべの先 #F0B050、茎 #4E6440、芽 #B4402E、根元 #5E5A48。小さな草花なので輪郭線は細く。当たり判定なし。
    /// </summary>
    public static class HibanaBuilder
    {
        const string Dir = "Assets/Art/Nature/Hibana";
        const int STEM = 0, PETAL = 1, FILAMENT = 2, ANTHER = 3, BUD = 4, NECK = 5;
        const float OutlineWidth = 0.0025f;

        [MenuItem("Slug/Nature/Build Hibana")]
        public static void Build()
        {
            System.IO.Directory.CreateDirectory(Dir);
            var mats = new[]
            {
                NatureAssets.Mat($"{Dir}/Hibana_Stem.mat", "#4E6440", false, 0.03f),
                NatureAssets.Mat($"{Dir}/Hibana_Petal.mat", "#C8402F", false, 0.03f),
                NatureAssets.Mat($"{Dir}/Hibana_Filament.mat", "#D85A38", false, 0.03f),
                NatureAssets.Mat($"{Dir}/Hibana_Anther.mat", "#F0B050", false, 0.03f),
                NatureAssets.Mat($"{Dir}/Hibana_Bud.mat", "#B4402E", false, 0.01f),
                NatureAssets.Mat($"{Dir}/Hibana_Neck.mat", "#5E5A48", false, 0f),
            };
            foreach (var x in mats) { x.SetFloat("_Facet", 0f); x.SetFloat("_OutlineWidth", OutlineWidth); }
            var log = new System.Text.StringBuilder("Hibana built:");
            log.Append(' ').Append(Save("A", Bloom(), mats)).Append(' ').Append(Save("B", Buds(), mats));
            AssetDatabase.SaveAssets();
            Debug.Log(log);
        }

        static string Save(string v, NatureMeshKit kit, Material[] mats)
        {
            string name = $"Hibana_{v}";
            var mesh = NatureAssets.SaveMesh(kit.ToMesh(name), $"{Dir}/{name}_Mesh.asset");
            NatureAssets.SavePrefab($"{Dir}/{name}.prefab", mesh, mats, null);
            return $"{name} {kit.TriangleCount}";
        }

        /// <summary>割れ目から出る根元の首（球根の首。どちらの姿にもある）。</summary>
        static void Neck(NatureMeshKit kit) => kit.Ball(new Vector3(0f, 0.004f, 0f), new Vector3(0.022f, 0.012f, 0.018f), 5, NECK);

        // A: three bare stems, each with one spark of a flower
        static NatureMeshKit Bloom()
        {
            var kit = new NatureMeshKit(6);
            const int seed = 4100;
            Neck(kit);
            for (int f = 0; f < 3; f++)
            {
                // one big bloom and two smaller ones
                float k0 = f == 0 ? 1f : PropKit.R(seed + 3, f, 0.62f, 0.72f);
                var toward = PropKit.Dir(f * 120f + 20f + PropKit.R(seed, f, -20f, 20f));
                float len = (f == 0 ? 0.26f : PropKit.R(seed + 1, f, 0.15f, 0.18f)), bend = PropKit.R(seed + 2, f, 18f, 34f) * (f == 0 ? 0.6f : 1f);
                var c = FlowerKit.Curve(new Vector3(0f, 0.005f, 0f), Vector3.up, toward, len, t => bend * t * t, out var axis);
                FlowerKit.Stem(kit, STEM, c, 0.0042f, 0.0034f, 6, seed + f);
                var top = c(1f);
                var flowerAxis = (axis + Vector3.up * 0.4f).normalized;
                kit.Ball(top - flowerAxis * 0.003f, new Vector3(0.007f, 0.007f, 0.007f), 4, STEM);
                // six narrow petals that open wide and roll back
                FlowerKit.PetalRing(kit, PETAL, top, flowerAxis, 6, 0.06f * k0, 0.0075f, 72f, 75f, f * 0.4f, 0.0025f, -0.35f, 7, seed + 10 + f, 0.5f);
                // six long stamens flung out like sparks, curving up at the ends, a bright bead on each
                FlowerKit.Frame(flowerAxis, out var fa, out var fb);
                for (int k = 0; k < 6; k++)
                {
                    float ang = k * Mathf.PI / 3f + 0.3f + f * 0.4f + PropKit.R(seed + 20 + f, k, -0.2f, 0.2f);
                    var radial = fa * Mathf.Cos(ang) + fb * Mathf.Sin(ang);
                    var outDir = (flowerAxis * Mathf.Cos(0.75f) + radial * Mathf.Sin(0.75f)).normalized;
                    float l = PropKit.R(seed + 30 + f, k, 0.075f, 0.095f) * k0;
                    System.Func<float, Vector3> fil = t => top + outDir * (l * t) + flowerAxis * (0.025f * t * t);
                    int from = kit.VertexCount;
                    PropKit.Pipe(kit, fil, (t, a) => Mathf.Lerp(0.0016f, 0.0011f, t), 6, 4, FILAMENT, -1, -1);
                    kit.Ball(fil(1f), new Vector3(0.0045f, 0.0045f, 0.0045f), 3, ANTHER);
                    kit.Paint(from, (p, uv) => new Color(0.9f, 0.6f, NatureAssets.Hash(seed + 40 + f, 1), 1f));
                }
            }
            return kit;
        }

        // B: the day of the rain - only three red pointed buds out of the crack
        static NatureMeshKit Buds()
        {
            var kit = new NatureMeshKit(6);
            const int seed = 4200;
            Neck(kit);
            // tall enough to be found from a few metres (it is the "come back tomorrow" hint), packed close together
            for (int f = 0; f < 3; f++)
            {
                var toward = PropKit.Dir(f * 120f + 50f);
                float len = f == 0 ? 0.16f : PropKit.R(seed + 1, f, 0.11f, 0.14f);
                var c = FlowerKit.Curve(new Vector3(0f, 0.004f, 0f) + toward * 0.006f, Vector3.up, toward, len, t => 9f * t, out _);
                int from = kit.VertexCount;
                PropKit.Pipe(kit, c, (t, a) => 0.0105f * Mathf.Sqrt(Mathf.Max(0f, Mathf.Sin(Mathf.PI * Mathf.Lerp(0.5f, 1f, t)))) * (t < 0.6f ? 0.75f + 0.4f * t : 1f), 10, 7, BUD, BUD, BUD);
                kit.Paint(from, (p, uv) => new Color(uv.y * 0.3f, 0f, 0.3f * f, 1f));
            }
            return kit;
        }
    }
}
