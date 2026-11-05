using System;
using UnityEditor;
using UnityEngine;

namespace SlugDesign
{
    /// <summary>
    /// ツユリ（Oct 27 灰色ラフ 3 案）。大雨が止んで晴れ間が出たら島時間 1 時間だけ咲く、まれな白い花。花の草原と塩田のふちの湿った窪みに 2〜3 本。
    /// 花は手で摘む（また咲く）、球根はピッケルで掘る（spec 6章）。メニュー Slug/Nature/Build Tsuyuri（Assets/Art/Nature/Tsuyuri）。
    ///
    /// 大きさは本物寄り（わくさん Oct 27）：花まで 17〜24 cm（ナメクジの目の高さ 0.47 m の半分、人なら 60〜85 cm）。ポップは太さで出す。
    /// - A 星：上を向いた 6 枚の星形の花が 2 本（タマスダレ・レインリリー）。細い葉がふさになって弓なりに垂れる
    /// - B うつむき：茎の先が曲がって花が下を向き、花びらが上へ反り返る（カタクリ）。幅広の葉 2 枚が地面に寝る
    /// - C しずく：曲がった茎の先に、閉じたしずく形の花が垂れる（スズランズイセン）。先だけ 6 つの小さな花びらが開く。葉は立つ
    /// Oct 27 わくさん「ツユリなんか微妙」→ 違いそうな所を 3 つ、極端に振った版：
    /// - D 大輪：花の大きさ。星形の花を 2 倍（花びら 11 cm、花の幅 20 cm）にして 1 本だけ。横にまだ閉じたつぼみ（1 時間だけ開く、の差）
    /// - E 水を抱く：ツユリだけの理由。深い盃の花が、雨のしずく（大きな水の玉）を抱えたまま咲く
    /// - F ずんぐり：ポップの寄せ方。太く短い茎（高さ 12 cm）に、ふくらんだ鈴が 3 つ垂れる。葉も短く太い
    /// Oct 27 わくさん：E 水を抱くに決定。状態ごとのプレハブ：
    /// - Tsuyuri_E_Bud：つぼみ（晴れ間の前。花びらが閉じた細いしずく形、長さ 7.5 cm）
    /// - Tsuyuri_E：咲いた花（晴れ間の 1 時間。花の幅 約 12 cm、中に直径 6 cm の水の玉、花びらの外にも小さな水滴）
    /// - Tsuyuri_E_Picked：花を摘んだあと（茎は 13 cm で切れ、葉は残る。また咲くまでの姿）
    /// - Tsuyuri_Item：手に持つ花（茎 6 cm が持ち手、原点は茎の下の端）
    /// - Tsuyuri_Bulb：ピッケルで掘った球根（玉ねぎ形、幅 7.5 cm・高さ 9 cm、下にひげ根。原点は底）
    /// 色（仮）：花びら #E9EEF2（白に少し青）、花の芯 #D9C37A、茎 #7C8A5A、葉 #6F8048、水 #BFD9E6、球根 #C8B48C、ひげ根 #9C8A6C。
    /// </summary>
    public static class TsuyuriBuilder
    {
        const string Dir = "Assets/Art/Nature/Tsuyuri";
        const int STEM = 0, LEAF = 1, PETAL = 2, HEART = 3, WATER = 4, BULB = 5, ROOT = 6;
        const float OutlineWidth = 0.0025f;

        [MenuItem("Slug/Nature/Build Tsuyuri")]
        public static void Build()
        {
            System.IO.Directory.CreateDirectory(Dir);
            var mats = Materials();
            var log = new System.Text.StringBuilder("Tsuyuri built:");
            log.Append(' ').Append(Make("A", Star, mats));
            log.Append(' ').Append(Make("B", Nodding, mats));
            log.Append(' ').Append(Make("C", Drop, mats));
            log.Append(' ').Append(Make("D", BigStar, mats));
            log.Append(' ').Append(Make("E", HoldingWater, mats));
            log.Append(' ').Append(Make("F", Squat, mats));
            log.Append(' ').Append(Make("E_Bud", k => WaterPlant(k, Stage.Bud), mats));
            log.Append(' ').Append(Make("E_Picked", k => WaterPlant(k, Stage.Picked), mats));
            log.Append(' ').Append(Make("Item", Item, mats));
            log.Append(' ').Append(Make("Bulb", Bulb, mats));
            AssetDatabase.SaveAssets();
            Debug.Log(log);
        }

        static string Make(string variant, Action<NatureMeshKit> build, Material[] mats)
        {
            string name = $"Tsuyuri_{variant}";
            var kit = new NatureMeshKit(7);
            build(kit);
            var mesh = NatureAssets.SaveMesh(kit.ToMesh(name), $"{Dir}/{name}_Mesh.asset");
            NatureAssets.SavePrefab($"{Dir}/{name}.prefab", mesh, mats, null);
            return $"{name} {kit.TriangleCount} tris";
        }

        // A: two upright stems, each topped by an upward six-pointed star; a fountain of thin leaves
        static void Star(NatureMeshKit kit)
        {
            const int seed = 101;
            for (int i = 0; i < 7; i++)
                FlowerKit.StrapLeaf(kit, LEAF, Vector3.zero, i * 0.9f + 0.3f, 80f, 0.19f, 0.007f, 0.35f, 0.004f, 7, seed + i);
            var stems = new[] { (new Vector3(-0.012f, 0f, 0.006f), 0.23f, 3.3f), (new Vector3(0.014f, 0f, -0.006f), 0.18f, 0.1f) };
            foreach (var (root, h, lean) in stems)
            {
                var toward = new Vector3(Mathf.Cos(lean), 0f, Mathf.Sin(lean));
                var c = FlowerKit.Curve(root, Vector3.up, toward, h, t => 26f * t, out var axis);
                FlowerKit.Stem(kit, STEM, c, 0.0065f, 0.005f, 7, seed);
                Vector3 top = c(1f);
                kit.Ball(top - axis * 0.004f, new Vector3(0.009f, 0.012f, 0.009f), 4, STEM);   // the swelling under the flower
                FlowerKit.PetalRing(kit, PETAL, top, axis, 6, 0.058f, 0.017f, 58f, 28f, lean, 0.005f, -0.25f, 6, seed + (int)(h * 100));
                kit.Ball(top + axis * 0.01f, new Vector3(0.011f, 0.013f, 0.011f), 4, HEART);
            }
        }

        // B: the stem hooks over so the flower hangs, petals sweeping back up (katakuri); two broad leaves lie on the ground
        static void Nodding(NatureMeshKit kit)
        {
            const int seed = 211;
            FlowerKit.FlatLeaf(kit, LEAF, Vector3.zero, 0.4f, 0.13f, 0.034f, 0.025f, 0.005f, 8, seed);
            FlowerKit.FlatLeaf(kit, LEAF, Vector3.zero, 3.3f, 0.12f, 0.03f, 0.022f, 0.005f, 8, seed + 1);
            var stems = new[] { (new Vector3(-0.01f, 0f, 0f), 0.25f, 3.0f), (new Vector3(0.015f, 0f, 0.01f), 0.22f, 0.2f) };
            foreach (var (root, h, lean) in stems)
            {
                var toward = new Vector3(Mathf.Cos(lean), 0f, Mathf.Sin(lean));
                var c = FlowerKit.Curve(root, Vector3.up, toward, h, t => t < 0.65f ? 6f * t : 3.9f + 125f * Mathf.Pow((t - 0.65f) / 0.35f, 1.4f), out var axis);
                FlowerKit.Stem(kit, STEM, c, 0.006f, 0.0045f, 7, seed);
                Vector3 top = c(1f);
                FlowerKit.PetalRing(kit, PETAL, top, axis, 6, 0.06f, 0.014f, 22f, 115f, lean, 0.005f, -0.2f, 7, seed + (int)(h * 100), 0.5f);
                kit.Ball(top + axis * 0.016f, new Vector3(0.006f, 0.006f, 0.006f) * 1.4f, 4, HEART);
                var ax = axis;
                kit.BeginTube();
                kit.Tube(t => top + ax * (0.032f * t), t => 0.0025f, new[] { new Vector3(0f, 1f, HEART), new Vector3(1f, 1f, HEART) }, 5, -1, HEART);   // stamens
            }
        }

        // C: closed drops hang from crooked stems, only the tip of each flaring into six little points; upright leaves
        static void Drop(NatureMeshKit kit)
        {
            const int seed = 307;
            for (int i = 0; i < 4; i++)
                FlowerKit.StrapLeaf(kit, LEAF, new Vector3(0.008f * (i - 1.5f), 0f, 0f), i * 1.6f + 0.8f, 82f, 0.2f, 0.011f, 0.18f, 0.004f, 7, seed + i);
            var stems = new[] { (new Vector3(0.005f, 0f, 0.008f), 0.24f, 0.2f), (new Vector3(-0.01f, 0f, -0.01f), 0.2f, 3.0f) };
            foreach (var (root, h, lean) in stems)
            {
                var toward = new Vector3(Mathf.Cos(lean), 0f, Mathf.Sin(lean));
                var c = FlowerKit.Curve(root, Vector3.up, toward, h, t => t < 0.7f ? 6f * t : 4.2f + 130f * Mathf.Pow((t - 0.7f) / 0.3f, 1.3f), out var axis);
                FlowerKit.Stem(kit, STEM, c, 0.0055f, 0.004f, 7, seed);
                Bell(kit, c(1f), axis, 0.05f, 0.021f, lean, seed);
            }
        }

        /// <summary>垂れたしずく形の閉じた花（長さ L、太さ R）。先だけ 6 つの小さな花びらが開く。</summary>
        static void Bell(NatureMeshKit kit, Vector3 top, Vector3 ax, float L, float R, float phase, int seed)
        {
            int from = kit.VertexCount;
            var rows = new Vector3[10];
            for (int k = 0; k < rows.Length; k++) rows[k] = new Vector3(k / 9f, 1f, PETAL);
            kit.BeginTube();
            kit.Tube(t => top + ax * (L * t), t => R * Mathf.Pow(Mathf.Max(0f, Mathf.Sin(Mathf.PI * Mathf.Lerp(0.04f, 0.96f, t))), 0.75f) * Mathf.Lerp(0.5f, 1f, t), rows, 12, PETAL, PETAL);
            kit.Paint(from, (p, uv) => new Color(0.9f, 0.2f, NatureAssets.Hash(seed, 5), 1f));
            FlowerKit.PetalRing(kit, PETAL, top + ax * (L * 0.82f), ax, 6, R * 0.76f, R * 0.38f, 62f, 35f, phase, 0.004f, 0f, 4, seed + 3);
        }

        // D: flower size pushed: one big star twice the size, and beside it a bud still shut
        static void BigStar(NatureMeshKit kit)
        {
            const int seed = 401;
            for (int i = 0; i < 6; i++)
                FlowerKit.StrapLeaf(kit, LEAF, Vector3.zero, i * 1.05f + 0.3f, 76f, 0.2f, 0.011f, 0.35f, 0.005f, 7, seed + i);
            var c = FlowerKit.Curve(Vector3.zero, Vector3.up, Vector3.right, 0.2f, t => 18f * t, out var axis);
            FlowerKit.Stem(kit, STEM, c, 0.0085f, 0.007f, 8, seed);
            Vector3 top = c(1f);
            kit.Ball(top - axis * 0.005f, new Vector3(0.014f, 0.016f, 0.014f), 4, STEM);
            FlowerKit.PetalRing(kit, PETAL, top, axis, 6, 0.11f, 0.032f, 64f, 20f, 0f, 0.006f, -0.25f, 7, seed + 10, 0.7f);
            kit.Ball(top + axis * 0.014f, new Vector3(0.02f, 0.02f, 0.02f), 5, HEART);
            // the bud: the same flower an hour before, a closed drop pointing up
            var b = FlowerKit.Curve(new Vector3(-0.012f, 0f, 0.01f), Vector3.up, Vector3.back, 0.14f, t => 12f * t, out var bAxis);
            FlowerKit.Stem(kit, STEM, b, 0.0065f, 0.0055f, 7, seed + 1);
            Vector3 bt = b(1f);
            var rows = new[] { new Vector3(0f, 1f, PETAL), new Vector3(0.3f, 1f, PETAL), new Vector3(0.6f, 1f, PETAL), new Vector3(0.85f, 1f, PETAL), new Vector3(1f, 1f, PETAL) };
            kit.BeginTube();
            kit.Tube(t => bt + bAxis * (0.055f * t), t => 0.016f * Mathf.Pow(Mathf.Max(0.05f, Mathf.Sin(Mathf.PI * Mathf.Lerp(0.15f, 1f, t))), 0.8f), rows, 10, PETAL, PETAL);
        }

        enum Stage { Bud, Open, Picked }

        // E: the reason it is Tsuyuri: a deep cup that blooms still holding a raindrop
        static void HoldingWater(NatureMeshKit kit) => WaterPlant(kit, Stage.Open);

        static void WaterPlant(NatureMeshKit kit, Stage stage)
        {
            const int seed = 503;
            for (int i = 0; i < 5; i++)
                FlowerKit.StrapLeaf(kit, LEAF, Vector3.zero, i * 1.26f + 0.5f, 74f, 0.17f, 0.01f, 0.4f, 0.005f, 7, seed + i);
            if (stage == Stage.Picked)
            {
                // the flower was picked: the stem stands cut short, its end a little swollen
                var cut = FlowerKit.Curve(Vector3.zero, Vector3.up, Vector3.right, 0.13f, t => 8f * t, out _);
                FlowerKit.Stem(kit, STEM, cut, 0.008f, 0.007f, 8, seed);
                kit.Ball(cut(1f), new Vector3(0.008f, 0.005f, 0.008f), 4, STEM);
                return;
            }
            var c = FlowerKit.Curve(Vector3.zero, Vector3.up, Vector3.right, 0.17f, t => 10f * t, out var axis);
            FlowerKit.Stem(kit, STEM, c, 0.008f, 0.0065f, 8, seed);
            Vector3 top = c(1f);
            if (stage == Stage.Bud)
            {
                kit.Ball(top - axis * 0.004f, new Vector3(0.013f, 0.014f, 0.013f), 4, STEM);
                FlowerKit.PetalRing(kit, PETAL, top, axis, 6, 0.075f, 0.024f, 10f, -8f, 0f, 0.006f, -0.4f, 7, seed + 10, 0.9f);
                return;
            }
            WaterCup(kit, top, axis, seed);
        }

        /// <summary>咲いた花：深い盃と、抱えた水の玉と、花びらの外の小さな水滴。</summary>
        static void WaterCup(NatureMeshKit kit, Vector3 top, Vector3 axis, int seed)
        {
            kit.Ball(top - axis * 0.004f, new Vector3(0.013f, 0.014f, 0.013f), 4, STEM);
            FlowerKit.PetalRing(kit, PETAL, top, axis, 6, 0.085f, 0.036f, 42f, -16f, 0f, 0.006f, -0.4f, 7, seed + 10, 0.9f);
            kit.Ball(top + axis * 0.038f, new Vector3(0.03f, 0.027f, 0.03f), 6, WATER);
            // a second, smaller drop beaded on the outside of one petal
            Vector3 radial = Vector3.Cross(axis, Vector3.forward).normalized;
            kit.Ball(top + axis * 0.05f + radial * 0.045f, Vector3.one * 0.008f, 4, WATER);
        }

        // the picked flower in hand: a short stem to hold, the cup still holding its water
        static void Item(NatureMeshKit kit)
        {
            var c = FlowerKit.Curve(Vector3.zero, Vector3.up, Vector3.right, 0.06f, t => 0f, out var axis);
            FlowerKit.Stem(kit, STEM, c, 0.007f, 0.0065f, 8, 509);
            WaterCup(kit, c(1f), axis, 503);
        }

        // the bulb dug up with a pickaxe: an onion shape with a dry neck and a tuft of roots
        static void Bulb(NatureMeshKit kit)
        {
            const int seed = 521;
            const float bottom = 0.018f, H = 0.09f, R = 0.038f;
            var rows = new Vector3[12];
            for (int k = 0; k < rows.Length; k++) rows[k] = new Vector3(k / 11f, 1f, BULB);
            kit.BeginTube();
            kit.Tube(t => Vector3.up * (bottom + H * t), t => R * Mathf.Pow(Mathf.Max(0f, Mathf.Sin(Mathf.PI * Mathf.Lerp(0.1f, 1f, t))), 0.55f) * Mathf.Lerp(1f, 0.3f, t * t),
                     rows, 12, BULB, BULB);
            // the dry neck where the leaves were
            var neck = FlowerKit.Curve(Vector3.up * (bottom + H * 0.95f), Vector3.up, Vector3.right, 0.035f, t => 20f * t, out _);
            FlowerKit.Stem(kit, BULB, neck, 0.007f, 0.004f, 6, seed);
            // roots: short thin hairs from the base, out and down to the ground
            for (int i = 0; i < 9; i++)
            {
                float yaw = i * 0.698f + (NatureAssets.Hash(seed, i) - 0.5f) * 0.4f;
                var h = new Vector3(Mathf.Cos(yaw), 0f, Mathf.Sin(yaw));
                Vector3 start = Vector3.up * (bottom + 0.004f) + h * 0.008f;
                float len = Mathf.Lerp(0.022f, 0.032f, NatureAssets.Hash(seed, i + 20));
                Vector3 P(float t) => start + h * (len * t) - Vector3.up * (bottom * t * t);
                kit.BeginTube();
                kit.Tube(P, t => Mathf.Lerp(0.0025f, 0.0012f, t), new[] { new Vector3(0f, 1f, ROOT), new Vector3(0.5f, 1f, ROOT), new Vector3(1f, 1f, ROOT) }, 5, -1, ROOT);
            }
        }

        // F: pop pushed to squat: a short fat stem, three plump bells hanging round it, short broad leaves
        static void Squat(NatureMeshKit kit)
        {
            const int seed = 607;
            for (int i = 0; i < 5; i++)
                FlowerKit.StrapLeaf(kit, LEAF, Vector3.zero, i * 1.26f + 0.2f, 55f, 0.12f, 0.02f, 0.45f, 0.006f, 7, seed + i);
            for (int i = 0; i < 3; i++)
            {
                float yaw = i * 2.09f + 0.4f, h = 0.13f - 0.015f * i;
                var toward = new Vector3(Mathf.Cos(yaw), 0f, Mathf.Sin(yaw));
                var c = FlowerKit.Curve(Vector3.zero, Vector3.up, toward, h, t => t < 0.5f ? 10f * t : 5f + 120f * Mathf.Pow((t - 0.5f) / 0.5f, 1.3f), out var axis);
                FlowerKit.Stem(kit, STEM, c, 0.012f, 0.008f, 8, seed + i);
                Bell(kit, c(1f), axis, 0.06f, 0.03f, yaw, seed + 10 * i);
            }
        }

        static Material[] Materials()
        {
            var m = new[]
            {
                NatureAssets.Mat($"{Dir}/Tsuyuri_Stem.mat", "#7C8A5A", false, 0.02f),
                NatureAssets.Mat($"{Dir}/Tsuyuri_Leaf.mat", "#6F8048", false, 0.02f),
                NatureAssets.Mat($"{Dir}/Tsuyuri_Petal.mat", "#E9EEF2", false, 0.02f),
                NatureAssets.Mat($"{Dir}/Tsuyuri_Heart.mat", "#D9C37A", false, 0.02f),
                NatureAssets.Mat($"{Dir}/Tsuyuri_Water.mat", "#BFD9E6", false, 0f, 0.5f),
                NatureAssets.Mat($"{Dir}/Tsuyuri_Bulb.mat", "#C8B48C", false, 0f),
                NatureAssets.Mat($"{Dir}/Tsuyuri_Root.mat", "#9C8A6C", false, 0f),
            };
            foreach (var x in m) { x.SetFloat("_Facet", 0f); x.SetFloat("_OutlineWidth", OutlineWidth); }   // 小さな草花は輪郭線を細く
            return m;
        }
    }
}
