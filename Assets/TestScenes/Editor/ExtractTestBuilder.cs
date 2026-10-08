using SlugMove;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SlugExtract
{
    /// <summary>
    /// Builds the ExtractTest scene, the solo "harvest and escape" prototype: a 60 m gray field with a dry salt
    /// flat (rock salt on and around a plateau), a wet field of crops, some obstacles to crawl over, and an exit
    /// marked by a pillar of light. The settings asset is created once and never overwritten.
    /// </summary>
    public static class ExtractTestBuilder
    {
        const string SettingsPath = MoveTestBuilder.TuningDir + "/ExtractSettings.asset";
        const string ScenePath = "Assets/Scenes/ExtractTest.unity";

        static ExtractSettings _settings;
        static Material _salt, _saltDark, _leaf, _root, _dryGround, _wetGround, _exitMat;

        [MenuItem("Slug/ExtractTest Build Scene")]
        public static void Build()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var moveSettings = MoveTestBuilder.LoadSettings();
            var scene = MoveTestBuilder.NewScene(new Vector2(60f, 60f));
            PrepareAssets();
            var map = MoveTestBuilder._map;
            var floor = MoveTestBuilder.Box("Floor", new Vector3(0f, -0.05f, 0f), new Vector3(60f, 0.1f, 60f), MoveTestBuilder._floorMat);

            // ---- dry salt flat in the west: a plateau (45 degree ramps) with the richest salt on top
            MoveTestBuilder.Box("SaltFlat", new Vector3(-15f, 0.005f, 4f), new Vector3(14f, 0.01f, 18f), _dryGround).GetComponent<Collider>().enabled = false;
            float top = MoveTestBuilder.Hill("SaltPlateau", -17f, 45f, 1.0f, z0: 6f, width: 5f, top: 4f);
            foreach (var p in new[] { new Vector3(-10f, 0f, -3f), new Vector3(-12.5f, 0f, 0.5f), new Vector3(-9f, 0f, 3f), new Vector3(-13f, 0f, 9f), new Vector3(-20.5f, 0f, 2f) })
                SaltRock(map, p, 1f);
            SaltRock(map, new Vector3(-17.8f, top, 8.2f), 1.25f);
            SaltRock(map, new Vector3(-16.2f, top, 9.6f), 1.25f);
            MoveTestBuilder.Label("岩塩の採取場（乾いた塩原）", new Vector3(-12f, 2.2f, -5f), 3f);

            // ---- wet field in the east: rows of crops
            MoveTestBuilder.Box("WetField", new Vector3(13f, 0.005f, 0f), new Vector3(12f, 0.01f, 14f), _wetGround).GetComponent<Collider>().enabled = false;
            for (int row = 0; row < 3; row++)
            for (int i = 0; i < 4; i++)
                Crop(map, new Vector3(9f + i * 2.6f + (row % 2) * 1.2f, 0f, -4.5f + row * 4f));
            SlugDesign.HarvestVisuals.UgoTakeLogs(map, _settings, SlugDesign.HarvestVisuals.UgoTakeSpots);   // ウゴタケ on fallen logs, picked by hand
            MoveTestBuilder.Label("雨カブの畑", new Vector3(13f, 2f, -8f), 3f);

            // ---- things in the way: a low wall to climb, rocks, bumpy ground on the road to the exit
            MoveTestBuilder.Box("LowWall", new Vector3(0f, 0.2f, 10f), new Vector3(8f, 0.4f, 0.6f), MoveTestBuilder._darkMat);
            MoveTestBuilder.Box("Rock_A", new Vector3(4f, 0.6f, 4f), new Vector3(1.6f, 1.2f, 1.4f), MoveTestBuilder._darkMat).transform.rotation = Quaternion.Euler(0f, 25f, 0f);
            MoveTestBuilder.Box("Rock_B", new Vector3(-4f, 0.5f, 15f), new Vector3(2.2f, 1.0f, 1.2f), MoveTestBuilder._darkMat).transform.rotation = Quaternion.Euler(0f, -15f, 0f);
            var bumps = new GameObject("Bumpy");
            bumps.transform.SetParent(map, false);
            bumps.transform.position = new Vector3(8f, 0f, 13f);
            var mesh = MoveTestBuilder.BumpMesh(10f, 8f, 0.1f, 0.14f, "Bumpy_Extract", 5f);
            bumps.AddComponent<MeshFilter>().sharedMesh = mesh;
            bumps.AddComponent<MeshRenderer>().sharedMaterial = MoveTestBuilder._grayMat;
            bumps.AddComponent<MeshCollider>().sharedMesh = mesh;

            // ---- the exit: a disc and a pillar of light, far from the start
            var exit = BuildExit(map, new Vector3(14f, 0f, 24f));
            MoveTestBuilder.Label("脱出口", new Vector3(14f, 2.2f, 22.3f), 3f);

            // ---- the player and the raid
            BuildRaidPlayer(moveSettings, new Vector3(0f, 0f, -24f), exit);

            // the island's clock and weather, moisture zones, giant shade, the body's moisture and health
            WeatherSceneSetup.Fit();
            EditorSceneManager.SaveScene(scene, ScenePath);
            MoveTestBuilder.AddToBuildSettings(ScenePath);
            AssetDatabase.SaveAssets();
            Debug.Log("[ExtractTest] scene saved: " + ScenePath);
        }

        /// <summary>The exit: a disc and a pillar of light at <paramref name="pos"/>.</summary>
        internal static Transform BuildExit(Transform map, Vector3 pos)
        {
            var exit = new GameObject("Exit").transform;
            exit.SetParent(map, false);
            exit.position = pos;
            var disc = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            disc.name = "ExitDisc";
            Object.DestroyImmediate(disc.GetComponent<Collider>());
            disc.transform.SetParent(exit, false);
            disc.transform.localScale = new Vector3(_settings.extractRadius * 2f, 0.01f, _settings.extractRadius * 2f);
            disc.transform.localPosition = new Vector3(0f, 0.01f, 0f);
            disc.GetComponent<Renderer>().sharedMaterial = _exitMat;
            var pillar = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pillar.name = "ExitPillar";
            Object.DestroyImmediate(pillar.GetComponent<Collider>());
            pillar.transform.SetParent(exit, false);
            pillar.transform.localScale = new Vector3(0.25f, 6f, 0.25f);
            pillar.transform.localPosition = new Vector3(0f, 6f, 0f);
            pillar.GetComponent<Renderer>().sharedMaterial = _exitMat;
            pillar.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return exit;
        }

        /// <summary>The player (with the bag, the carry, the portrait) and the raid that ends at <paramref name="exit"/>.</summary>
        internal static GameObject BuildRaidPlayer(SlugMoveSettings moveSettings, Vector3 spawn, Transform exit)
        {
            var player = MoveTestBuilder.BuildPlayer(moveSettings, spawn);
            player.GetComponent<SlugMoveHud>().enabled = false;   // the raid has its own HUD
            var bag = player.AddComponent<SlugBag>();
            bag.settings = _settings;
            bag.controller = player.GetComponent<SlugMoveController>();
            FitBag(player, bag);
            var raid = new GameObject("Raid").AddComponent<RaidManager>();
            raid.settings = _settings;
            raid.controller = bag.controller;
            raid.tool = player.GetComponent<SlugToolHolder>();
            raid.guns = player.GetComponents<SlugGunHolder>();
            raid.loadout = player.GetComponent<SlugLoadout>();
            // choose what to carry before setting out (the clock waits)
            var picker = player.GetComponent<LoadoutPicker>();
            picker.openOnStart = true; picker.allowToggle = false; picker.confirmLabel = "出撃する";
            raid.picker = picker;
            raid.bag = bag;
            raid.exitPoint = exit;
            return player;
        }

        /// <summary>
        /// Wires a bag to the slug it rides on: the bag on its back (gear; added, bag only, when the slug wears none), its
        /// hitboxes (rounds in the back wear the bag), and for the player the camera (peeking at the back).
        /// </summary>
        internal static void FitBag(GameObject root, SlugBag bag)
        {
            var slug = root.GetComponentInChildren<SlugDesign.SlugPop>();
            var gear = slug.GetComponent<SlugDesign.SlugEquipment>();
            if (!gear)
            {
                gear = slug.gameObject.AddComponent<SlugDesign.SlugEquipment>();
                gear.slug = slug;
                gear.helmet = false;
                gear.armor = SlugDesign.SlugEquipment.Armor.None;
            }
            gear.bag = true;
            bag.slug = slug;
            bag.gear = gear;
            bag.hitboxes = root.GetComponent<SlugHitboxes>();
            bag.cameraRig = root.GetComponent<SlugCameraRig>();
            // the player carries things in its hands on the way to the bag
            if (bag.controller)
            {
                var carry = root.GetComponent<SlugCarry>();
                if (!carry) carry = root.AddComponent<SlugCarry>();
                carry.bag = bag; carry.controller = bag.controller; carry.cameraRig = bag.cameraRig; carry.slug = slug;
                EditorUtility.SetDirty(carry);
                // and sees itself in the top left corner
                var portrait = root.GetComponent<SelfPortrait>();
                if (!portrait) portrait = root.AddComponent<SelfPortrait>();
                portrait.controller = bag.controller;
                EditorUtility.SetDirty(portrait);
            }
            EditorUtility.SetDirty(bag);
            EditorUtility.SetDirty(gear);
        }

        /// <summary>
        /// Brings the open scene's bags up to date: the player's bag is wired to its gear, hitboxes and camera; the practice
        /// slug (GunTest) gets a bag with things in it, to shoot until it bursts.
        /// </summary>
        [MenuItem("Slug/Update Bag (open scene)")]
        public static void UpdateOpenScene()
        {
            var settings = AssetDatabase.LoadAssetAtPath<ExtractSettings>(SettingsPath);
            foreach (var bag in Object.FindObjectsByType<SlugBag>(FindObjectsInactive.Include))
            {
                if (!bag.settings) bag.settings = settings;
                FitBag(bag.gameObject, bag);
            }
            var dummy = GameObject.Find("練習用ナメクジ");
            if (dummy && settings)
            {
                var bag = dummy.GetComponent<SlugBag>();
                if (!bag)
                {
                    bag = dummy.AddComponent<SlugBag>();
                    bag.settings = settings;
                    bag.startItems = new[] { ItemKind.Crop, ItemKind.RockSalt, ItemKind.Crop, ItemKind.RockSalt, ItemKind.RockSalt, ItemKind.Crop };
                }
                FitBag(dummy, bag);
            }
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
        }

        /// <summary>Loads (or creates once) the raid settings and the materials of the nodes. Call after MoveTestBuilder.NewScene.</summary>
        internal static ExtractSettings PrepareAssets()
        {
            MoveTestBuilder.EnsureFolder(MoveTestBuilder.TuningDir);
            _settings = AssetDatabase.LoadAssetAtPath<ExtractSettings>(SettingsPath);
            if (_settings == null)
            {
                _settings = ScriptableObject.CreateInstance<ExtractSettings>();
                AssetDatabase.CreateAsset(_settings, SettingsPath);
            }
            Materials();
            return _settings;
        }

        static void Materials()
        {
            var lit = Shader.Find("Universal Render Pipeline/Lit");
            _salt = MoveTestBuilder.SaveMaterial("X_RockSalt", lit, new Color(0.96f, 0.88f, 0.9f));
            _saltDark = MoveTestBuilder.SaveMaterial("X_RockSaltBase", lit, new Color(0.78f, 0.7f, 0.68f));
            _leaf = MoveTestBuilder.SaveMaterial("X_Leaf", lit, new Color(0.35f, 0.55f, 0.25f));
            _root = MoveTestBuilder.SaveMaterial("X_Root", lit, new Color(0.62f, 0.38f, 0.62f));
            _dryGround = MoveTestBuilder.SaveMaterial("X_DryGround", lit, new Color(0.8f, 0.77f, 0.7f));
            _wetGround = MoveTestBuilder.SaveMaterial("X_WetGround", lit, new Color(0.36f, 0.4f, 0.33f));
            _exitMat = MoveTestBuilder.SaveMaterial("X_Exit", Shader.Find("Universal Render Pipeline/Unlit"), new Color(0.4f, 0.95f, 1f));
        }

        /// <summary>A cluster of pale pink crystals on a low base. Six hits, the richest node.</summary>
        internal static Harvestable SaltRock(Transform parent, Vector3 pos, float size)
        {
            var root = new GameObject("RockSalt");
            root.transform.SetParent(parent, false);
            root.transform.position = pos;
            root.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
            root.transform.localScale = Vector3.one * size;
            // every crystal keeps its own box collider, so the pickaxe stops on the crystal it really meets
            Part(root.transform, PrimitiveType.Cube, _saltDark, new Vector3(0f, 0.06f, 0f), new Vector3(0.7f, 0.12f, 0.6f), Vector3.zero, true);
            Part(root.transform, PrimitiveType.Cube, _salt, new Vector3(0f, 0.28f, 0f), new Vector3(0.22f, 0.5f, 0.22f), new Vector3(8f, 20f, 6f), true);
            Part(root.transform, PrimitiveType.Cube, _salt, new Vector3(0.17f, 0.2f, 0.08f), new Vector3(0.16f, 0.34f, 0.16f), new Vector3(-10f, 40f, -22f), true);
            Part(root.transform, PrimitiveType.Cube, _salt, new Vector3(-0.15f, 0.18f, -0.06f), new Vector3(0.15f, 0.3f, 0.15f), new Vector3(14f, -30f, 18f), true);
            Part(root.transform, PrimitiveType.Cube, _salt, new Vector3(0.02f, 0.14f, -0.2f), new Vector3(0.12f, 0.22f, 0.12f), new Vector3(-20f, 10f, 4f), true);
            var h = root.AddComponent<Harvestable>();
            h.settings = _settings; h.kind = ItemKind.RockSalt; h.hits = 6; h.yieldPerHit = 1; h.breakBonus = 2;
            SlugDesign.HarvestVisuals.ApplyTo(h);     // the salt mushroom, when its models are there
            return h;
        }

        /// <summary>A leafy plant with a fat purple root showing. Dug out in two hits, one turnip.</summary>
        internal static Harvestable Crop(Transform parent, Vector3 pos)
        {
            var root = new GameObject("Crop");
            root.transform.SetParent(parent, false);
            root.transform.position = pos;
            root.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
            // about the size of the root and the base of the leaves
            var col = root.AddComponent<SphereCollider>();
            col.center = new Vector3(0f, 0.16f, 0f);
            col.radius = 0.2f;
            Part(root.transform, PrimitiveType.Sphere, _root, new Vector3(0f, 0.1f, 0f), new Vector3(0.3f, 0.26f, 0.3f), Vector3.zero);
            for (int i = 0; i < 5; i++)
            {
                float a = i * 72f;
                Part(root.transform, PrimitiveType.Sphere, _leaf, Quaternion.Euler(0f, a, 0f) * new Vector3(0f, 0.32f, 0.1f),
                     new Vector3(0.12f, 0.03f, 0.3f), new Vector3(-35f, a, 0f));
            }
            var h = root.AddComponent<Harvestable>();
            h.settings = _settings; h.kind = ItemKind.Crop; h.hits = 2; h.yieldPerHit = 0; h.breakBonus = 1;
            SlugDesign.HarvestVisuals.ApplyTo(h);     // the rain turnip field, when its models are there
            return h;
        }

        static void Part(Transform parent, PrimitiveType type, Material mat, Vector3 pos, Vector3 scale, Vector3 euler, bool keepCollider = false)
        {
            var go = GameObject.CreatePrimitive(type);
            if (!keepCollider) Object.DestroyImmediate(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localRotation = Quaternion.Euler(euler);
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = mat;
        }
    }
}
