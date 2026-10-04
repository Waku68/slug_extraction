using SlugMove;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SlugExtract
{
    /// <summary>
    /// Builds the GunTest scene for tuning the guns: the slug at one end of a long floor with the rifle in hand (the
    /// shotgun and the sniper a key away), targets on posts at 3 to 80 m, a low box to shoot over, a wall and rock salt
    /// close by, a back wall for misses, and the debug readout.
    /// </summary>
    public static class GunTestBuilder
    {
        const string ScenePath = "Assets/Scenes/GunTest.unity";

        [MenuItem("Slug/GunTest Build Scene")]
        public static void Build()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var moveSettings = MoveTestBuilder.LoadSettings();
            var scene = MoveTestBuilder.NewScene(new Vector2(30f, 120f));
            ExtractTestBuilder.PrepareAssets();
            var map = MoveTestBuilder._map;
            MoveTestBuilder.Box("Floor", new Vector3(0f, -0.05f, 52f), new Vector3(30f, 0.1f, 120f), MoveTestBuilder._floorMat);

            var lit = Shader.Find("Universal Render Pipeline/Lit");
            var wood = Glowing(MoveTestBuilder.SaveMaterial("T_TargetWood", lit, new Color(0.86f, 0.74f, 0.55f)));
            var post = MoveTestBuilder.SaveMaterial("T_TargetPost", lit, new Color(0.45f, 0.32f, 0.22f));
            var red = Glowing(MoveTestBuilder.SaveMaterial("T_TargetRed", lit, new Color(0.8f, 0.25f, 0.2f)));
            var white = Glowing(MoveTestBuilder.SaveMaterial("T_TargetWhite", lit, new Color(0.95f, 0.93f, 0.88f)));

            // targets in a loose fan, facing the start; the far ones are bigger
            var targets = new (float x, float z, float r)[]
            {
                (-1.2f, 3f, 0.25f), (1.1f, 6f, 0.25f), (-1.8f, 10f, 0.3f), (2.2f, 18f, 0.4f), (0f, 30f, 0.5f), (-3f, 50f, 0.7f), (3f, 80f, 0.9f),
            };
            foreach (var (x, z, r) in targets)
            {
                var pos = new Vector3(x, 0f, z);
                Vector3 facing = Vector3.ProjectOnPlane(-pos, Vector3.up).normalized;
                ShootingTarget.Create(map, pos, facing, 0.45f + r * 0.6f, r, wood, post, red, white);
                MoveTestBuilder.Label($"{z:0} m", pos + new Vector3(0f, 0.65f + r * 2.2f, 0f), 2f + z * 0.08f);
            }

            // a low box to shoot over, a wall and rock salt close by, a back wall for misses
            MoveTestBuilder.Box("Cover", new Vector3(0.3f, 0.2f, 4.5f), new Vector3(1.4f, 0.4f, 0.3f), MoveTestBuilder._grayMat);
            MoveTestBuilder.Label("低い箱（越しに撃つ）", new Vector3(0.3f, 0.75f, 4.5f), 1.4f);
            var wall = MoveTestBuilder.Box("Wall", new Vector3(-1.4f, 0.5f, 1.2f), new Vector3(0.2f, 1f, 2f), MoveTestBuilder._darkMat);
            wall.AddComponent<PickaxeSurface>().material = PickaxeMaterial.Hard;
            MoveTestBuilder.Label("壁（近くで撃つ）", new Vector3(-1.4f, 1.25f, 1.2f), 1.4f);
            var rock = ExtractTestBuilder.SaltRock(map, new Vector3(1.3f, 0f, 1.6f), 1f);
            rock.hits = 100000;
            rock.breakBonus = 0;
            MoveTestBuilder.Label("岩塩（1 でピッケル）", new Vector3(1.3f, 0.85f, 1.9f), 1.4f);
            var back = MoveTestBuilder.Box("BackWall", new Vector3(0f, 3f, 104f), new Vector3(30f, 6f, 0.5f), MoveTestBuilder._darkMat);
            back.AddComponent<PickaxeSurface>().material = PickaxeMaterial.Hard;

            var player = MoveTestBuilder.BuildPlayer(moveSettings, Vector3.zero);
            player.GetComponent<SlugMoveHud>().enabled = false;          // the debug readout takes its place
            var loadout = player.GetComponent<SlugLoadout>();
            loadout.startWith = 1;
            player.GetComponent<SlugToolHolder>().enabled = false;
            loadout.guns[0].enabled = true;
            // worn gear to try on: the helmet with the tentacles out, the shell plate armour in camouflage, and a bag
            var body = player.GetComponentInChildren<SlugDesign.SlugPop>();
            var gear = body.gameObject.AddComponent<SlugDesign.SlugEquipment>();
            gear.slug = body;
            gear.bag = true;                                          // slugs carry a bag (the shell is the snails' mark)
            // a practice slug in full gear to shoot at: the readout says which part was hit and what covered it
            var dummyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(SlugDesign.SlugLookbook.PrefabPath);
            var dummy = (GameObject)PrefabUtility.InstantiatePrefab(dummyPrefab);
            dummy.name = "練習用ナメクジ";
            dummy.transform.position = new Vector3(2.2f, 0f, 4f);
            dummy.transform.rotation = Quaternion.LookRotation(Vector3.ProjectOnPlane(-dummy.transform.position, Vector3.up), Vector3.up);
            var dummySlug = dummy.GetComponent<SlugDesign.SlugPop>();
            SlugDesign.SlugLookbook.ApplyCurrent(dummySlug);
            dummySlug.handsExtend = 0f;
            PrefabUtility.RecordPrefabInstancePropertyModifications(dummySlug);
            var dummyGear = dummy.AddComponent<SlugDesign.SlugEquipment>();
            dummyGear.slug = dummySlug;
            dummyGear.bag = true;
            dummy.AddComponent<SlugHitboxes>().slug = dummySlug;
            dummySlug.Rebuild();
            MoveTestBuilder.Label("練習用ナメクジ（当たった部位が出る）", new Vector3(2.2f, 0.95f, 4f), 1.4f);

            var debug = new GameObject("GunDebug").AddComponent<GunDebug>();
            debug.loadout = loadout;

            EditorSceneManager.SaveScene(scene, ScenePath);
            MoveTestBuilder.AddToBuildSettings(ScenePath);
            AssetDatabase.SaveAssets();
            Debug.Log("[GunTest] scene saved: " + ScenePath);
        }

        /// <summary>Lets a material flash when hit (the target sets its emission through a property block).</summary>
        static Material Glowing(Material m)
        {
            m.EnableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", Color.black);
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            EditorUtility.SetDirty(m);
            return m;
        }
    }
}
