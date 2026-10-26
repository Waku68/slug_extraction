using SlugDesign;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SlugMove
{
    /// <summary>
    /// Builds the EquipTest scene for comparing worn gear: three side by side (a slug with the helmet, the shell plate
    /// armour and a bag; a snail scav with its shell; a slug with only a bag), a camera in front, and labels.
    /// </summary>
    public static class EquipTestBuilder
    {
        const string ScenePath = "Assets/Scenes/Tests/EquipTest.unity";

        [MenuItem("Slug/EquipTest Build Scene")]
        public static void Build()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var scene = MoveTestBuilder.NewScene(new Vector2(12f, 12f));
            MoveTestBuilder.Box("Floor", new Vector3(0f, -0.05f, 0f), new Vector3(12f, 0.1f, 12f), MoveTestBuilder._floorMat);

            var variants = new (string label, bool helmet, SlugEquipment.Armor armor, bool bag)[]
            {
                ("ナメクジ：ヘルメット＋殻板＋バッグ", true, SlugEquipment.Armor.ShellPlate, true),
                ("カタツムリ（スカブ）：殻", false, SlugEquipment.Armor.SnailShell, false),
                ("ナメクジ：バッグだけ", false, SlugEquipment.Armor.None, true),
            };
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(SlugLookbook.PrefabPath);
            for (int i = 0; i < variants.Length; i++)
            {
                var v = variants[i];
                var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                go.name = "Slug_" + v.armor + (v.helmet ? "_Helmet" : "");
                go.transform.position = new Vector3((i - 1) * 1.2f, 0f, 0f);
                go.transform.rotation = Quaternion.Euler(0f, 180f, 0f);         // facing the camera
                var slug = go.GetComponent<SlugPop>();
                SlugLookbook.ApplyCurrent(slug);
                slug.handsExtend = 0f;
                PrefabUtility.RecordPrefabInstancePropertyModifications(slug);
                var gear = go.AddComponent<SlugEquipment>();
                gear.slug = slug;
                gear.helmet = v.helmet;
                gear.armor = v.armor;
                gear.bag = v.bag;
                slug.Rebuild();
                MoveTestBuilder.Label(v.label, new Vector3((i - 1) * 1.2f, 0.95f + 0.15f * (i % 2), 0f), 1.0f);
            }

            var cam = new GameObject("Main Camera") { tag = "MainCamera" }.AddComponent<Camera>();
            cam.fieldOfView = 40f;
            cam.nearClipPlane = 0.02f;
            cam.transform.position = new Vector3(0f, 0.75f, -3.6f);
            cam.transform.LookAt(new Vector3(0f, 0.35f, 0f));

            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            Debug.Log("[EquipTest] scene saved: " + ScenePath);
        }
    }
}
