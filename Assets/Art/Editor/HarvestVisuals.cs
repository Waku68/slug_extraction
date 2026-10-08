using SlugExtract;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SlugDesign
{
    /// <summary>
    /// Puts the Blender models on the things that are harvested: the rain turnip field (one turnip half in a mound
    /// of soil, its leaves spread wide; the same bulb comes out when it is dug, with smaller leaves), the salt mushroom (full → half-mined → stump, one chunk every two hits), the chunk of rock salt,
    /// and ウゴタケ growing on fallen logs (picked by hand). The scene builders call <see cref="ApplyTo"/> on every node
    /// they make; <c>Slug/Update Harvest Visuals (open scene)</c> does the same to a scene already built (it keeps the
    /// nodes where they are, so hand-made changes to the scene stay).
    /// </summary>
    public static class HarvestVisuals
    {
        const string Art = "Assets/Art/Models/";
        const string SettingsPath = "Assets/Tuning/ExtractSettings.asset";

        /// <summary>Where the logs lie in ExtractTest: the far side of the wet field, behind the turnips.</summary>
        public static readonly Vector3[] UgoTakeSpots = { new Vector3(9.5f, 0f, 5.8f), new Vector3(13f, 0f, 6.1f), new Vector3(16.4f, 0f, 5.7f) };

        /// <summary>How high the middle of the rain turnip's bulb sits above the ground, in the model's metres (0.2 × its radius, rainturnip_gen.py).</summary>
        const float BulbAboveGround = 0.027f;

        static GameObject Load(string path) => AssetDatabase.LoadAssetAtPath<GameObject>(Art + path);

        [MenuItem("Slug/Update Harvest Visuals (open scene)")]
        public static void UpdateOpenScene()
        {
            SetUpAssets();
            var settings = AssetDatabase.LoadAssetAtPath<ExtractSettings>(SettingsPath);
            int n = 0;
            foreach (var h in Object.FindObjectsByType<Harvestable>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                ApplyTo(h);
                n++;
            }
            var scene = EditorSceneManager.GetActiveScene();
            if (scene.name == "ExtractTest" && Object.FindObjectsByType<Pickable>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length == 0)
            {
                var parent = GameObject.Find("Map") ? GameObject.Find("Map").transform : null;
                UgoTakeLogs(parent, settings, UgoTakeSpots);
            }
            EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log($"Harvest visuals: {n} nodes updated in {scene.name}");
        }

        /// <summary>Materials (the world look) and the models on the item definitions (the ground, the hands, the bag).</summary>
        public static void SetUpAssets()
        {
            var cap = Mat("UgoTake/UgoTake_Cap", new Color(0.62f, 0.46f, 0.33f), 0.3f, 0.2f);
            var stem = Mat("UgoTake/UgoTake_Stem", new Color(0.9f, 0.86f, 0.76f), 0.2f, 0.1f);
            var gill = Mat("UgoTake/UgoTake_Gill", new Color(0.78f, 0.7f, 0.58f), 0.1f, 0.2f);
            var bark = Mat("UgoTake/UgoTake_Bark", new Color(0.36f, 0.27f, 0.2f), 0.05f, 0.4f, 0.6f);
            var wood = Mat("UgoTake/UgoTake_Wood", new Color(0.66f, 0.54f, 0.38f), 0.05f, 0.2f, 0.3f);
            var moss = Mat("UgoTake/UgoTake_Moss", new Color(0.43f, 0.48f, 0.3f), 0.1f, 0.4f, 0.5f);
            WorldLook.Remap(Art + "UgoTake/UgoTake_Item.fbx", cap, stem, gill);
            WorldLook.Remap(Art + "UgoTake/UgoTake_Log.fbx", bark, wood, moss);

            var s = AssetDatabase.LoadAssetAtPath<ExtractSettings>(SettingsPath);
            if (!s) return;
            s.crop.model = Load("RainTurnip/RainTurnip_Item.fbx");
            s.rockSalt.model = Load("RockSalt/RockSalt_Chunk_A.fbx");
            s.ugoTake.model = Load("UgoTake/UgoTake_Item.fbx");
            s.ugoTakeCrushed.model = s.ugoTake.model;
            EditorUtility.SetDirty(s);
            AssetDatabase.SaveAssets();
        }

        static Material Mat(string name, Color c, float rim, float mottle, float dent = 0.2f)
        {
            var m = WorldLook.GetOrCreate(Art + name + ".mat");
            WorldLook.Apply(m, c, rim, mottle, null, 12f, dent, 70f);
            return m;
        }

        /// <summary>Replaces a node's stand-in shapes with the models (and its colliders with ones that fit them).</summary>
        public static void ApplyTo(Harvestable h)
        {
            // without the models (not exported yet) the stand-in shapes stay
            if (h.kind == ItemKind.RockSalt ? !Load("RockSalt/Mushroom/RockSaltMush_M5_All.fbx") : !Load("RainTurnip/RainTurnip_Node_A.fbx")) return;
            var t = h.transform;
            for (int i = t.childCount - 1; i >= 0; i--) Object.DestroyImmediate(t.GetChild(i).gameObject);
            foreach (var c in h.GetComponents<Collider>()) Object.DestroyImmediate(c);
            var rnd = new System.Random(Mathf.Abs(t.position.GetHashCode()));

            if (h.kind == ItemKind.RockSalt)
            {
                // the salt mushroom, a little bigger or smaller each
                t.localScale = Vector3.one * (0.9f + 0.2f * (float)rnd.NextDouble());
                var stages = new GameObject[3];
                string[] names = { "RockSaltMush_M5_All", "RockSaltMush_M5_All_Half", "RockSaltMush_M5_All_Stump" };
                for (int i = 0; i < 3; i++)
                {
                    stages[i] = Instance(Load("RockSalt/Mushroom/" + names[i] + ".fbx"), t);
                    stages[i].name = i == 0 ? "満タン" : i == 1 ? "掘りかけ" : "切り株";
                    stages[i].SetActive(i == 0);
                }
                h.gameObject.AddComponent<BoxCollider>();
                h.stages = stages;
                h.hits = 6; h.hitsPerYield = 2; h.yieldPerHit = 1; h.breakBonus = 0;
                FitBox(h.GetComponent<BoxCollider>(), stages[0], t);
            }
            else
            {
                // the rain turnip field: a mound of soil with one rain turnip planted in it, its leaves spread wide, the
                // top of the bulb showing. It is scaled like the one in the hands (whose longest side is the item's
                // size), so when it is dug the bulb that comes out is the same bulb, only with smaller leaves.
                t.localScale = Vector3.one;
                Instance(Load(rnd.Next(2) == 0 ? "RainTurnip/RainTurnip_Node_A.fbx" : "RainTurnip/RainTurnip_Node_B.fbx"), t);
                var item = ModelBounds(Load("RainTurnip/RainTurnip_Item.fbx"));
                float s = h.settings.Def(h.kind).size / Mathf.Max(item.size.x, item.size.y, item.size.z, 1e-3f);
                var turnip = Instance(Load("RainTurnip/RainTurnip_Planted.fbx"), t).transform;
                turnip.name = "カブ";
                turnip.localScale = Vector3.one * s;
                turnip.localRotation = Quaternion.Euler((float)rnd.NextDouble() * 14f - 7f, (float)rnd.NextDouble() * 360f, (float)rnd.NextDouble() * 14f - 7f);
                turnip.localPosition = Vector3.up * (BulbAboveGround * s);
                float bottom = 0f;
                foreach (var r in turnip.GetComponentsInChildren<Renderer>()) bottom = Mathf.Min(bottom, r.bounds.min.y - t.position.y);
                var col = h.gameObject.AddComponent<SphereCollider>();
                col.center = new Vector3(0f, 0.2f, 0f);
                col.radius = 0.26f;
                h.stages = new GameObject[0];
                h.planted = turnip;
                h.plantedItemCenter = item.center;
                h.plantedDepth = -bottom;
                h.hits = 2; h.hitsPerYield = 1; h.yieldPerHit = 0; h.breakBonus = 1;   // dug out in two hits, one turnip
            }
            EditorUtility.SetDirty(h);
        }

        /// <summary>Fallen mossy logs with ウゴタケ on top, picked by hand one at a time.</summary>
        public static void UgoTakeLogs(Transform parent, ExtractSettings settings, Vector3[] at)
        {
            var log = Load("UgoTake/UgoTake_Log.fbx");
            var mush = Load("UgoTake/UgoTake_Item.fbx");
            if (!log || !mush) return;
            var rnd = new System.Random(11);
            foreach (var p0 in at)
            {
                var p = p0;
                if (Physics.Raycast(p + Vector3.up * 5f, Vector3.down, out var hit, 20f)) p.y = hit.point.y;
                var root = new GameObject("UgoTake_Log");
                root.transform.SetParent(parent, true);
                root.transform.SetPositionAndRotation(p, Quaternion.Euler(0f, (float)rnd.NextDouble() * 360f, 0f));
                var l = Instance(log, root.transform);
                l.transform.localScale = Vector3.one * 1.3f;
                var box = root.AddComponent<BoxCollider>();
                Harvestable.FitBox(box, l, root.transform, 1f);   // the whole log (the mushrooms stand on its top)

                var cluster = new GameObject("ウゴタケ");
                cluster.transform.SetParent(root.transform, false);
                var pick = cluster.AddComponent<Pickable>();
                pick.settings = settings;
                pick.kind = ItemKind.UgoTake;
                pick.crushedKind = ItemKind.UgoTakeCrushed;
                int count = 3 + rnd.Next(3);
                float top = box.center.y + box.size.y * 0.5f - 0.02f;
                for (int i = 0; i < count; i++)
                {
                    var m = Instance(mush, cluster.transform);
                    float x = Mathf.Lerp(-0.45f, 0.45f, (i + 0.5f) / count) + ((float)rnd.NextDouble() - 0.5f) * 0.08f;
                    m.transform.localPosition = new Vector3(x, top, ((float)rnd.NextDouble() - 0.5f) * 0.1f);
                    m.transform.localRotation = Quaternion.Euler(((float)rnd.NextDouble() - 0.5f) * 20f, (float)rnd.NextDouble() * 360f, ((float)rnd.NextDouble() - 0.5f) * 20f);
                    m.transform.localScale = Vector3.one * (1.1f + 0.4f * (float)rnd.NextDouble());
                    var sc = m.AddComponent<SphereCollider>();
                    sc.center = new Vector3(0f, 0.06f, 0f);
                    sc.radius = 0.05f;
                    pick.parts.Add(m);
                }
                EditorUtility.SetDirty(pick);
            }
        }

        /// <summary>The bounds of a model as it stands at the origin, unturned and unscaled (what ItemPickup centres and sizes).</summary>
        static Bounds ModelBounds(GameObject prefab)
        {
            var m = Object.Instantiate(prefab);
            m.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            m.transform.localScale = Vector3.one;
            var b = new Bounds();
            bool any = false;
            foreach (var r in m.GetComponentsInChildren<Renderer>())
                if (!any) { b = r.bounds; any = true; } else b.Encapsulate(r.bounds);
            Object.DestroyImmediate(m);
            return b;
        }

        static GameObject Instance(GameObject prefab, Transform parent)
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            return go;
        }

        static void FitBox(BoxCollider box, GameObject shown, Transform space) => Harvestable.FitBox(box, shown, space);
    }
}
