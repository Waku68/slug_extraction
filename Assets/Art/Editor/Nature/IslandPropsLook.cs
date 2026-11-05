using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SlugDesign
{
    /// <summary>
    /// 島の小物の下書き（Nov 2）を 1 つのシーンに並べる：メニュー Slug/Nature/Props/Build Island Props Look → Assets/Scenes/Preview/IslandPropsLook.unity。
    /// 1 行が 1 つの物で、左から 名札・大きさの見本のナメクジ（長さ 1.1 m・高さ 0.5 m のカプセル）・A・B・C。組ごとに奥へ並ぶ。
    /// 開いているシーンには触らない（足して作って、保存して閉じる。Unity をほかのセッションと分け合っても壊さない）。
    /// このシーンそのものが開いていれば、その場で作り直す（カメラと光はそのまま。保存していない変更があれば作らない）。
    /// 作る前に、各ビルダー（Slug/Nature/Props/Build 〜）を一度走らせておく。
    /// </summary>
    public static class IslandPropsLook
    {
        const string ScenePath = "Assets/Scenes/Preview/IslandPropsLook.unity";

        static readonly (string group, string dir, (string file, string label)[] items)[] Groups =
        {
            ("岩と土", "Assets/Art/Nature/Rocks", new[] { ("Rock", "岩"), ("Talus", "落石のすそ"), ("LavaRock", "溶岩の岩"), ("SaltCrystal", "塩の結晶の群れ"), ("TermiteMound", "シロアリ塚") }),
            ("木", "Assets/Art/Nature/Wood", new[] { ("Stump", "切り株"), ("Driftwood", "流木の山"), ("FallenLog", "倒れた大木"), ("RootPlate", "根の円盤"), ("DeadBranch", "枯れ枝の骨") }),
            ("足元", "Assets/Art/Nature/GroundLitter", new[] { ("FallenLeaves", "落ち葉"), ("Twigs", "小枝"), ("Pebbles", "小石・軽石"), ("MossPatch", "苔のむら"), ("SmallMushrooms", "小さいきのこ") }),
            ("浜", "Assets/Art/Nature/BeachLitter", new[] { ("Shells", "貝殻"), ("CrabMolt", "カニの抜け殻"), ("Seaweed", "打ち上げられた海藻") }),
            ("草と茂み", "Assets/Art/Nature/Understory", new[] { ("FernClump", "シダの株"), ("LowBush", "低い茂み"), ("DryGrass", "背の高い枯れ草") }),
            ("ケルン風の試し", "Assets/Art/Nature/MassBush", new[] { ("MassBush", "塊の茂み（A 主役・B 子）") }),
            ("ヤシ", "Assets/Art/Nature/Palm", new[] { ("Palm", "立っているヤシ（A 曲がり・B まっすぐ高い）") }),
            ("ヤシの残り", "Assets/Art/Nature/PalmRemains", new[] { ("DeadPalm", "立ち枯れのヤシ"), ("FallenFrond", "落ちたヤシの葉"), ("Coconut", "ヤシの実") }),
            ("ナメクジの暮らしの跡", "Assets/Art/Nature/SlugTraces", new[] { ("RopeRail", "綱の手すり"), ("RestShelter", "休み場の跡"), ("Campfire", "焚き火の跡"), ("DryingRack", "古い干し台"),
                                                                       ("Basket", "捨てられた籠"), ("DigMark", "古い掘り跡"), ("OldChannel", "乾いた昔の水路") }),
            ("珍しい拾い物（Nov 4）", "Assets/Art/Nature/RareFinds", new[] { ("BottleShip", "ボトルシップ（浜）"), ("SaltFish", "塩の魚（岩塩の洞窟。B 透けるに決定）") }),
            ("森の木（Nov 3）","Assets/Art/Nature/ForestTree", new[] { ("ForestTree", "森の木（A 板根・B 段・C 傘・D ねじれ）") }),
            ("緑の草（Nov 3）", "Assets/Art/Nature/Grass", new[] { ("Grass", "緑の草（A 草むら・B 下草・C アシ・D 芝）") }),
            ("採る植物の残り（Nov 3）", "Assets/Art/Nature", new[] { ("SplashMoss/SplashMoss", "しぶき苔（A 濡れた・B 縮んだ）"), ("Hibana/Hibana", "ヒバナ（A 咲いた・B 芽）"),
                                                             ("SeedTuft/SeedTuft", "種の房"), ("StormFruit/StormFruit", "嵐の実（A 丸ごと・B 割った 1/4）") }),
            ("森の木・芝の色違い（Nov 3）", "Assets/Art/Nature", new[] { ("ForestTree/ForestTree_B|ForestTree/ForestTree_B_Dry", "段の木（雨季・乾季）"),
                                                             ("Grass/Grass_D|Grass/Grass_D_Forest", "芝のむら（草原・森）") }),
            ("植物の下書き（Oct 26〜27）", "Assets/Art/Nature", new[]
            {
                ("Kuwazuimo/Kuwazuimo", "クワズイモ（なめらか）"),
                ("Kuwazuimo/Kuwazuimo_A_Facet|Kuwazuimo/Kuwazuimo_B_Facet|Kuwazuimo/Kuwazuimo_C_Facet", "クワズイモ（カクカク）"),
                ("Hego/Hego", "ヘゴ（なめらか）"),
                ("Hego/Hego_A_Facet|Hego/Hego_B_Facet|Hego/Hego_C_Facet", "ヘゴ（カクカク）"),
                ("DesertRosette/DesertRosette", "砂漠の株（なめらか）"),
                ("DesertRosette/DesertRosette_A_Facet|DesertRosette/DesertRosette_B_Facet|DesertRosette/DesertRosette_C_Facet", "砂漠の株（カクカク）"),
                ("FairyRingMushroom/FairyRingMushroom", "菌輪のキノコ（なめらか）"),
                ("FairyRingMushroom/FairyRingMushroom_A_Facet|FairyRingMushroom/FairyRingMushroom_B_Facet|FairyRingMushroom/FairyRingMushroom_C_Facet", "菌輪のキノコ（カクカク）"),
            }),
            ("採る植物（Oct 27）", "Assets/Art/Nature", new[]
            {
                ("Tsuyuri/Tsuyuri_E_Bud|Tsuyuri/Tsuyuri_E|Tsuyuri/Tsuyuri_E_Picked|Tsuyuri/Tsuyuri_Item|Tsuyuri/Tsuyuri_Bulb", "ツユリ（E に決定：つぼみ・咲いた・摘んだあと・手に持つ花・球根）"),
                ("Tsuyuri/Tsuyuri_A|Tsuyuri/Tsuyuri_B|Tsuyuri/Tsuyuri_C|Tsuyuri/Tsuyuri_D|Tsuyuri/Tsuyuri_F", "ツユリ（選ばなかった案）"),
                ("Tsukishiro/Tsukishiro", "ツキシロ（B 月下美人に決定）"),
                ("Hideribeni/Hideribeni", "ヒデリベニ（A こんもりに決定、雨季の緑）"),
                ("Hideribeni/Hideribeni_A_Red|Hideribeni/Hideribeni_B_Red|Hideribeni/Hideribeni_C_Red", "ヒデリベニ（乾季の赤）"),
            }),
            ("石積みの道しるべ（Argon、Oct 25）", "Assets/Art/Models/Props/Cairn", new[] { ("Cairn", "ケルン（A 立つ・B 低い・C 崩れた）") }),
        };

        [MenuItem("Slug/Nature/Props/Build Island Props Look")]
        public static void Build()
        {
            var active = EditorSceneManager.GetActiveScene();
            // if the look scene is the one open right now, rebuild it in place (keep its camera and light where they are);
            // otherwise make it beside whatever is open, save it and close it again
            var open = UnityEngine.SceneManagement.SceneManager.GetSceneByPath(ScenePath);
            bool inPlace = open.IsValid() && open.isLoaded;
            if (inPlace && open.isDirty) { Debug.LogWarning($"{ScenePath} is open with unsaved changes; save or close it, then build again."); return; }
            var scene = inPlace ? open : EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Additive);
            if (inPlace)
                foreach (var go in scene.GetRootGameObjects())
                    if (!go.TryGetComponent<Camera>(out _) && !go.TryGetComponent<Light>(out _)) Object.DestroyImmediate(go);
            EditorSceneManager.SetActiveScene(scene);   // new objects land here, not in the scene someone else has open
            try
            {
                var font = AssetDatabase.LoadAssetAtPath<TMPro.TMP_FontAsset>("Assets/Fonts/NotoSansJP SDF.asset");
                var groundMat = new Material(AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Nature/Rocks/Rocks_Gravel.mat"));
                groundMat.SetColor("_BaseColor", new Color(0.66f, 0.62f, 0.54f));
                groundMat.SetFloat("_OutlineWidth", 0f);
                groundMat.SetFloat("_Facet", 0f);
                Directory.CreateDirectory("Assets/Art/Nature/Look");
                var slugMat = new Material(groundMat);
                slugMat.SetColor("_BaseColor", new Color(0.55f, 0.6f, 0.45f));
                groundMat = SaveMat(groundMat, "Assets/Art/Nature/Look/IslandPropsLook_Ground.mat");
                slugMat = SaveMat(slugMat, "Assets/Art/Nature/Look/IslandPropsLook_Slug.mat");

                float z = 0f, maxX = 0f;
                var root = new GameObject("島の小物（下書き Nov 2）");
                Move(root, scene);
                foreach (var g in Groups)
                {
                    var groupGo = new GameObject(g.group);
                    groupGo.transform.SetParent(root.transform, false);
                    Label(groupGo.transform, g.group, new Vector3(-3.2f, 0.05f, z), 1.1f, font);
                    z += 1.6f;
                    foreach (var (file, label) in g.items)
                    {
                        var row = new GameObject(label);
                        row.transform.SetParent(groupGo.transform, false);
                        // measure the row first: its depth is the deepest of A, B, C
                        // A, B, C (and D where a row has four kinds); or the prefab names listed with "|" (each named by what follows its first "_")
                        var names = file.Contains("|") ? file.Split('|') : new[] { file + "_A", file + "_B", file + "_C", file + "_D" };
                        var prefabs = new GameObject[names.Length];
                        float depth = 0.5f;
                        for (int v = 0; v < names.Length; v++)
                        {
                            prefabs[v] = AssetDatabase.LoadAssetAtPath<GameObject>($"{g.dir}/{names[v]}.prefab");
                            if (prefabs[v] != null) depth = Mathf.Max(depth, Measure(prefabs[v]).size.z);
                        }
                        float zc = z + depth * 0.5f;
                        Label(row.transform, label, new Vector3(-3.2f, 0.05f, zc - 0.2f), 0.45f, font);
                        var slug = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                        slug.name = "大きさの見本（ナメクジ 1.1 m）";
                        slug.transform.SetParent(row.transform, false);
                        slug.transform.SetPositionAndRotation(new Vector3(-0.85f, 0.25f, zc), Quaternion.Euler(0f, 0f, 90f));
                        slug.transform.localScale = new Vector3(0.5f, 0.55f, 0.5f);
                        slug.GetComponent<Renderer>().sharedMaterial = slugMat;
                        Object.DestroyImmediate(slug.GetComponent<Collider>());
                        float x = 0.3f;
                        for (int v = 0; v < names.Length; v++)
                        {
                            if (prefabs[v] == null) continue;
                            string tag = file.Contains("|") ? names[v].Substring(names[v].IndexOf('_') + 1) : ((char)('A' + v)).ToString();
                            var b = Measure(prefabs[v]);
                            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefabs[v], scene);
                            go.transform.SetParent(row.transform, false);
                            go.transform.position = new Vector3(x - b.min.x, 0f, zc - b.center.z);
                            Label(row.transform, tag, new Vector3(x + b.size.x * 0.5f, 0.02f, zc - depth * 0.5f - 0.35f), 0.3f, font);
                            x += b.size.x + 1.0f;
                        }
                        maxX = Mathf.Max(maxX, x);
                        z += depth + 1.2f;
                    }
                    z += 1.5f;
                }

                var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
                ground.name = "地面";
                Move(ground, scene);
                ground.transform.position = new Vector3(maxX * 0.5f - 2f, 0f, z * 0.5f);
                ground.transform.localScale = new Vector3((maxX + 10f) / 10f, 1f, (z + 10f) / 10f);
                ground.GetComponent<Renderer>().sharedMaterial = groundMat;

                // the camera at slug eye height at the near end, looking down the first rows
                foreach (var go in inPlace ? new GameObject[0] : scene.GetRootGameObjects())
                {
                    if (go.TryGetComponent<Camera>(out var cam))
                    {
                        cam.transform.SetPositionAndRotation(new Vector3(-2.5f, 0.47f, -3f), Quaternion.Euler(4f, 35f, 0f));
                        cam.nearClipPlane = 0.02f;
                    }
                    if (go.TryGetComponent<Light>(out var sun)) sun.transform.rotation = Quaternion.Euler(50f, -35f, 0f);
                }
                EditorSceneManager.SaveScene(scene, ScenePath);
                Debug.Log($"Island props look saved: {ScenePath} ({z:0} m deep, {maxX:0} m wide)");
            }
            finally
            {
                if (!inPlace) EditorSceneManager.CloseScene(scene, true);
                if (active.IsValid()) EditorSceneManager.SetActiveScene(active);
            }
        }

        static Material SaveMat(Material m, string path)
        {
            var old = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (old == null) { AssetDatabase.CreateAsset(m, path); return m; }
            old.shader = m.shader;
            old.CopyPropertiesFromMaterial(m);
            return old;
        }

        static Bounds Measure(GameObject prefab)
        {
            var mf = prefab.GetComponent<MeshFilter>();
            return mf != null && mf.sharedMesh != null ? mf.sharedMesh.bounds : new Bounds(Vector3.zero, Vector3.one);
        }

        static void Move(GameObject go, UnityEngine.SceneManagement.Scene scene) =>
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go, scene);

        /// <summary>地面に寝かせた名札（上から読める向き。ナメクジの目からは斜めに見える）。</summary>
        static void Label(Transform parent, string text, Vector3 at, float size, TMPro.TMP_FontAsset font)
        {
            var go = new GameObject($"名札 {text}");
            go.transform.SetParent(parent, false);
            go.transform.SetPositionAndRotation(at, Quaternion.Euler(60f, 0f, 0f));
            var t = go.AddComponent<TMPro.TextMeshPro>();
            if (font != null) t.font = font;
            t.text = text;
            t.fontSize = size * 10f;
            t.color = new Color(0.25f, 0.22f, 0.18f);
            t.alignment = TMPro.TextAlignmentOptions.Center;
            t.rectTransform.sizeDelta = new Vector2(Mathf.Max(3f, text.Length * size), size * 1.6f);
            t.textWrappingMode = TMPro.TextWrappingModes.NoWrap;
        }
    }
}
