using SlugMove;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SlugExtract
{
    /// <summary>
    /// Builds the SwingTest scene for tuning the pickaxe swing: the slug in the middle of a small floor, surrounded by
    /// things to hit that never break (rock salt on the ground and on a ledge, a crop, a wall), open air for misses,
    /// and the debug tools (slow motion, the head's path, markers).
    /// </summary>
    public static class SwingTestBuilder
    {
        const string ScenePath = "Assets/Scenes/Tests/SwingTest.unity";

        [MenuItem("Slug/SwingTest Build Scene")]
        public static void Build()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var moveSettings = MoveTestBuilder.LoadSettings();
            var scene = MoveTestBuilder.NewScene(new Vector2(20f, 20f));
            ExtractTestBuilder.PrepareAssets();
            var map = MoveTestBuilder._map;
            MoveTestBuilder.Box("Floor", new Vector3(0f, -0.05f, 0f), new Vector3(20f, 0.1f, 20f), MoveTestBuilder._floorMat);

            // in front: rock salt on the ground (hard), within reach of a blow
            Endless(ExtractTestBuilder.SaltRock(map, new Vector3(0f, 0f, 0.9f), 1f));
            MoveTestBuilder.Label("岩塩（硬い：跳ね返る）", new Vector3(0f, 0.85f, 1.3f), 1.4f);
            // front left: a crop (soft)
            Endless(ExtractTestBuilder.Crop(map, new Vector3(-0.75f, 0f, 0.6f)));
            MoveTestBuilder.Label("作物（柔らかい：めり込む）", new Vector3(-1.0f, 0.85f, 0.9f), 1.4f);
            // front right: rock salt up on a ledge at head height
            MoveTestBuilder.Box("Ledge", new Vector3(1.05f, 0.225f, 0.8f), new Vector3(0.8f, 0.45f, 0.8f), MoveTestBuilder._darkMat);
            Endless(ExtractTestBuilder.SaltRock(map, new Vector3(1.05f, 0.45f, 0.8f), 0.8f));
            MoveTestBuilder.Label("高い所の岩塩", new Vector3(1.2f, 1.25f, 1.1f), 1.4f);
            // left: a wall (hard)
            var wall = MoveTestBuilder.Box("Wall", new Vector3(-1.05f, 0.5f, -0.3f), new Vector3(0.2f, 1f, 1.6f), MoveTestBuilder._darkMat);
            wall.AddComponent<PickaxeSurface>().material = PickaxeMaterial.Hard;
            MoveTestBuilder.Label("壁（硬い）", new Vector3(-1.05f, 1.25f, -0.3f), 1.4f);
            // right: nothing (miss) / behind: bare ground
            MoveTestBuilder.Label("右：何もない（空振り）", new Vector3(2.2f, 0.9f, -0.2f), 1.4f);
            MoveTestBuilder.Label("後ろ：地面（ふつう）", new Vector3(0f, 0.9f, -2.2f), 1.4f);

            var player = MoveTestBuilder.BuildPlayer(moveSettings, Vector3.zero);
            player.GetComponent<SlugMoveHud>().enabled = false;          // the debug readout takes its place
            var debug = new GameObject("SwingDebug").AddComponent<SwingDebug>();
            debug.holder = player.GetComponent<SlugToolHolder>();

            EditorSceneManager.SaveScene(scene, ScenePath);
            MoveTestBuilder.AddToBuildSettings(ScenePath);
            AssetDatabase.SaveAssets();
            Debug.Log("[SwingTest] scene saved: " + ScenePath);
        }

        /// <summary>A node for practice: it never runs out.</summary>
        static void Endless(Harvestable h)
        {
            h.hits = 100000;
            h.yieldPerHit = 1;      // something comes out every hit (a rain turnip pops its planted one first)
            h.breakBonus = 0;
        }
    }
}
