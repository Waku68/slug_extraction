using System.Text;
using UnityEditor;
using UnityEngine;

namespace SlugDesign
{
    /// <summary>
    /// ケルン風の地形の形の試し（docs/ideas/cairn_terrain_ideas.md の J・K・L）。メニュー Slug/Nature/Build Cairn Shapes で作り直す
    /// （Assets/Art/Nature/CairnShapes）。岩はかじれないのでカクカク、上を向いた面にその場所の地面の色が乗る（_TopCoat）。
    /// ナメクジの目の高さ 0.47 m は人の約 1/3.6。当たり判定は形どおり（よじ登れる段が効くように）。
    /// - StepRock 段の岩（J）：高さ 3.1 m（人なら 11 m）の丸い岩の片側に、0.38 m ずつ（人なら 1.4 m）上がる段が 8 つ。ナメクジは
    ///   0.45 m までよじ登れるので、段の側からは頂上まで上がれて、ほかの面（つるっとして急）からは上がれない。段の奥行き 0.6 m
    /// - Kinoko キノコ岩（K）：根元がえぐれた岩。傘は幅 4.2 m、下のふちが地面から 1.35 m、張り出し 1.1 m（日陰と雨宿り）
    /// - Hisashi 庇岩（K）：崖の根元に置く。後ろの塊から屋根の板が 1.3 m 張り出す（下は高さ 1.25 m）
    /// - Needle 針（L）：高さ 8 m（人なら 29 m）の細い岩の柱。ひと言で呼べる形の目印
    /// - Arch アーチ（L）：幅 7 m、くぐれる穴は幅 3.4 m・高さ 3.2 m
    /// - Split 割れ岩（L）：高さ 3.4 m の岩が縦に割れて、すき間 0.64 m（ナメクジが通れる）
    /// 色：灰色の石 #A39C8E（IslandRockBuilder と同じ）、赤い岩 #B27C60（枯れた谷・台地用）。
    /// </summary>
    public static class CairnShapesBuilder
    {
        public const string Dir = "Assets/Art/Nature/CairnShapes";

        [MenuItem("Slug/Nature/Build Cairn Shapes")]
        public static string Build()
        {
            var mats = PropKit.Mats(Dir, "CairnShapes",
                new PropKit.Spec("Stone", "#A39C8E", true).Mottle("#88827A", 0.45f, 2.2f).Camo("#ADA799", "#928C80", "#B9B596", 2.6f, 0.5f),
                new PropKit.Spec("Red", "#B27C60", true).Mottle("#9A6A50", 0.45f, 2.2f).Camo("#BC876A", "#A27056", "#C49A7C", 2.6f, 0.5f));
            foreach (var m in mats) { m.SetFloat("_TopCoat", 0.7f); EditorUtility.SetDirty(m); }
            var stone = new[] { mats[0] };
            var red = new[] { mats[1] };

            var sb = new StringBuilder();
            sb.AppendLine(PropKit.Save(Dir, "StepRock_Red", StepRock(11), red, 3));
            sb.AppendLine(PropKit.Save(Dir, "StepRock_Stone", StepRock(12), stone, 3));
            sb.AppendLine(PropKit.Save(Dir, "Kinoko_Stone", Kinoko(21), stone, 3));
            sb.AppendLine(PropKit.Save(Dir, "Hisashi_Red", Hisashi(31), red, 3));
            sb.AppendLine(PropKit.Save(Dir, "Needle_Stone", Needle(41), stone, 3));
            sb.AppendLine(PropKit.Save(Dir, "Arch_Red", Arch(51), red, 3));
            sb.AppendLine(PropKit.Save(Dir, "Split_Stone", Split(61), stone, 3));
            AssetDatabase.SaveAssets();
            return sb.ToString();
        }

        // a rock lump: few strata, cracks, chips; box > 2 makes it blocky
        static void Rock(NatureMeshKit kit, Vector3 c, Vector3 size, int seed, float box = 3.5f, float layers = 4f, float step = 0.04f,
                         System.Func<Vector3, Vector3> post = null, Quaternion? rot = null, float floor = 0f, float rough = 0.08f)
        {
            PropKit.Lump(kit, c, size, 14, 22, 0, seed, rough, 1.2f, floor, rot, 0f, 1f,
                n => PropKit.Strata(n, seed, layers, step) + PropKit.Cracks(n, seed + 1, 0.05f) + PropKit.Chips(n, seed + 2, 0.08f), post, box);
        }

        // J: a smooth round rock with a stair of blocks up one side (-x)
        static NatureMeshKit StepRock(int seed)
        {
            var kit = new NatureMeshKit(1);
            Rock(kit, new Vector3(0f, 1.55f, 0f), new Vector3(1.7f, 1.55f, 1.5f), seed, 4f, 2f, 0.02f, rough: 0.05f);
            const float rise = 0.38f, tread = 0.6f;
            for (int i = 0; i < 8; i++)
            {
                float top = (i + 1) * rise;
                float x = -4.9f + i * tread;
                float z = 0.15f * Mathf.Sin(i * 1.7f + seed);
                Rock(kit, new Vector3(x, top * 0.5f, z), new Vector3(0.48f, top * 0.5f, 0.75f + 0.08f * Mathf.Cos(i * 2.3f)), seed + 10 + i,
                     6f, 1f, 0.01f, rough: 0.03f, post: p => { if (p.y > top) p.y = top; return p; });
            }
            return kit;
        }

        // K: undercut "mushroom" rock — a narrow stem under a wide cap
        static NatureMeshKit Kinoko(int seed)
        {
            var kit = new NatureMeshKit(1);
            Rock(kit, new Vector3(0f, 0.85f, 0f), new Vector3(0.95f, 0.85f, 0.85f), seed, 3f);
            Rock(kit, new Vector3(0.1f, 2.0f, 0f), new Vector3(2.1f, 0.68f, 1.8f), seed + 1, 3.5f, 4f, 0.05f, floor: float.NaN);
            return kit;
        }

        // K: a roof of rock jutting from the foot of a cliff (the cliff is behind, -z)
        static NatureMeshKit Hisashi(int seed)
        {
            var kit = new NatureMeshKit(1);
            Rock(kit, new Vector3(0f, 1.4f, -0.7f), new Vector3(2.7f, 1.45f, 0.8f), seed, 4f);
            Rock(kit, new Vector3(0f, 1.6f, 0.35f), new Vector3(2.5f, 0.36f, 1.1f), seed + 1, 5f, 2f, 0.03f, floor: float.NaN);
            return kit;
        }

        // L: a tall thin needle of rock
        static NatureMeshKit Needle(int seed)
        {
            var kit = new NatureMeshKit(1);
            float y = 0f;
            for (int i = 0; i < 6; i++)
            {
                float h = 1.45f - i * 0.05f, r = Mathf.Lerp(1.4f, 0.45f, i / 5f);
                var c = new Vector3(0.12f * Mathf.Sin(i * 1.3f + seed), y + h * 0.55f, 0.1f * Mathf.Cos(i * 1.9f));
                Rock(kit, c, new Vector3(r, h * 0.62f, r * 0.9f), seed + i, 3f, 2f, 0.04f, floor: i == 0 ? 0f : float.NaN);
                y += h;
            }
            return kit;
        }

        // L: a natural arch — two legs and a bow of blocks
        static NatureMeshKit Arch(int seed)
        {
            var kit = new NatureMeshKit(1);
            Rock(kit, new Vector3(-2.6f, 1.7f, 0f), new Vector3(0.95f, 1.75f, 0.95f), seed, 3.5f);
            Rock(kit, new Vector3(2.6f, 1.7f, 0f), new Vector3(0.95f, 1.75f, 0.95f), seed + 1, 3.5f);
            for (int k = 0; k <= 8; k++)
            {
                float a = Mathf.PI * k / 8f;
                var c = new Vector3(-2.6f * Mathf.Cos(a), 3.2f + 1.3f * Mathf.Sin(a), 0f);
                Rock(kit, c, new Vector3(0.75f, 0.62f, 0.8f), seed + 5 + k, 3f, 2f, 0.03f, rot: Quaternion.Euler(0f, 0f, a * Mathf.Rad2Deg - 90f), floor: float.NaN);
            }
            return kit;
        }

        // L: a rock split in two, with a gap a slug can walk through
        static NatureMeshKit Split(int seed)
        {
            var kit = new NatureMeshKit(1);
            Rock(kit, new Vector3(-0.95f, 1.7f, 0f), new Vector3(1.25f, 1.7f, 1.3f), seed, 3.5f, post: p => { if (p.x > -0.32f) p.x = -0.32f; return p; });
            Rock(kit, new Vector3(0.95f, 1.6f, 0.05f), new Vector3(1.2f, 1.6f, 1.25f), seed + 1, 3.5f, post: p => { if (p.x < 0.32f) p.x = 0.32f; return p; });
            return kit;
        }
    }
}
