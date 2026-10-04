using SlugDesign;
using SlugMove;
using SlugWorld;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SlugExtract
{
    /// <summary>
    /// Puts the island's clock and weather into the open scene (ExtractTest): the weather presenter, the moisture zones
    /// (the salt flat dries, puddles wet), a giant tree and giant rocks to throw shade, and the player's moisture and health.
    /// Safe to run again: the weather props are made anew, the settings assets are only created once.
    /// </summary>
    public static class WeatherSceneSetup
    {
        const string IslandPath = MoveTestBuilder.TuningDir + "/IslandSettings.asset";
        const string ConditionPath = MoveTestBuilder.TuningDir + "/ConditionSettings.asset";

        [MenuItem("Slug/Update Weather (open scene)")]
        public static void UpdateOpenScene()
        {
            Fit();
            var scene = EditorSceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[Weather] fitted: " + scene.path);
        }

        public static IslandSettings LoadIsland()
        {
            var s = AssetDatabase.LoadAssetAtPath<IslandSettings>(IslandPath);
            if (!s) { s = ScriptableObject.CreateInstance<IslandSettings>(); AssetDatabase.CreateAsset(s, IslandPath); }
            return s;
        }

        public static ConditionSettings LoadCondition()
        {
            var s = AssetDatabase.LoadAssetAtPath<ConditionSettings>(ConditionPath);
            if (!s) { s = ScriptableObject.CreateInstance<ConditionSettings>(); AssetDatabase.CreateAsset(s, ConditionPath); }
            return s;
        }

        /// <summary>Fits the weather into the open scene (does not save).</summary>
        public static void Fit()
        {
            var island = LoadIsland();
            var condition = LoadCondition();

            // the presenter
            var presenter = Object.FindAnyObjectByType<WeatherPresenter>();
            if (!presenter) presenter = new GameObject("Island").AddComponent<WeatherPresenter>();
            presenter.settings = island;
            presenter.region = IslandRegion.TestField;
            foreach (var l in Object.FindObjectsByType<Light>())
                if (l.type == LightType.Directional) { presenter.sun = l; break; }

            // the props, made anew
            var old = presenter.transform.Find("WeatherProps");
            if (old) Object.DestroyImmediate(old.gameObject);
            var props = new GameObject("WeatherProps").transform;
            props.SetParent(presenter.transform, false);
            var rock = MoveTestBuilder.SaveMaterial("W_GiantRock", Shader.Find("Universal Render Pipeline/Lit"), new Color(0.36f, 0.35f, 0.34f));
            var bark = MoveTestBuilder.SaveMaterial("W_Bark", Shader.Find("Universal Render Pipeline/Lit"), new Color(0.33f, 0.25f, 0.18f));
            var leaves = MoveTestBuilder.SaveMaterial("W_Leaves", Shader.Find("Universal Render Pipeline/Lit"), new Color(0.24f, 0.38f, 0.2f));
            var puddle = MoveTestBuilder.SaveMaterial("W_Puddle", Shader.Find("Universal Render Pipeline/Lit"), new Color(0.2f, 0.3f, 0.38f));
            puddle.SetFloat("_Smoothness", 0.92f);

            // a giant tree near the start, a giant rock beside and in the salt flat: shade that moves with the sun
            Prim(props, "GiantTree_Trunk", PrimitiveType.Cylinder, bark, new Vector3(-3f, 7f, -8f), new Vector3(2.4f, 7f, 2.4f), 0f);
            Prim(props, "GiantTree_Canopy", PrimitiveType.Sphere, leaves, new Vector3(-3f, 15.5f, -8f), new Vector3(13f, 7f, 13f), 0f);
            Prim(props, "GiantRock_A", PrimitiveType.Cube, rock, new Vector3(-26f, 3.5f, 12f), new Vector3(8f, 7f, 6f), 20f);
            Prim(props, "GiantRock_B", PrimitiveType.Cube, rock, new Vector3(-19f, 3f, -2f), new Vector3(5f, 6f, 4f), -12f);

            // the salt flat dries the body
            Zone(props, "Zone_SaltFlat", MoistureZone.Kind.Salt, new Vector3(-15f, 0.5f, 4f), new Vector3(14f, 2f, 18f));

            // puddles: one near the start, one in the field, one where the stream meets the salt flat
            foreach (var (name, at, size) in new[]
            {
                ("Puddle_Start", new Vector3(3f, 0f, -17f), 3.2f),
                ("Puddle_Field", new Vector3(12f, 0f, -9f), 3.6f),
                ("Puddle_SaltEdge", new Vector3(-7f, 0f, 13f), 2.6f),
            })
            {
                var p = Prim(props, name, PrimitiveType.Cylinder, puddle, at + Vector3.up * 0.012f, new Vector3(size, 0.006f, size), 0f);
                Object.DestroyImmediate(p.GetComponent<Collider>());
                p.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                Zone(props, name + "_Zone", MoistureZone.Kind.Water, at + Vector3.up * 0.5f, new Vector3(size, 1.2f, size));
            }

            // the player's body: moisture and health
            foreach (var bag in Object.FindObjectsByType<SlugBag>(FindObjectsInactive.Include))
            {
                if (!bag.controller) continue;
                var root = bag.gameObject;
                var health = root.GetComponent<SlugHealth>();
                if (!health) health = root.AddComponent<SlugHealth>();
                health.settings = condition;
                var moisture = root.GetComponent<SlugMoisture>();
                if (!moisture) moisture = root.AddComponent<SlugMoisture>();
                moisture.settings = condition;
                moisture.controller = bag.controller;
                moisture.cameraRig = root.GetComponent<SlugCameraRig>();
                moisture.slug = root.GetComponentInChildren<SlugPop>();
                moisture.health = health;
                moisture.portrait = root.GetComponent<SelfPortrait>();
                EditorUtility.SetDirty(health);
                EditorUtility.SetDirty(moisture);
                foreach (var raid in Object.FindObjectsByType<RaidManager>())
                {
                    raid.health = health;
                    EditorUtility.SetDirty(raid);
                }
            }
            EditorUtility.SetDirty(presenter);
        }

        static GameObject Prim(Transform parent, string name, PrimitiveType type, Material mat, Vector3 pos, Vector3 scale, float yaw)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.SetPositionAndRotation(pos, Quaternion.Euler(0f, yaw, 0f));
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = mat;
            return go;
        }

        static void Zone(Transform parent, string name, MoistureZone.Kind kind, Vector3 center, Vector3 size)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = center;
            var z = go.AddComponent<MoistureZone>();
            z.kind = kind;
            z.size = size;
        }
    }
}
