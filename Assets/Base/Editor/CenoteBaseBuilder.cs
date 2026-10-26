using SlugDesign;
using SlugMove;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SlugBase
{
    /// <summary>
    /// Builds the CenoteBase scene: the cenote cave (Blender/Base/cenote_base_gen.py), the furniture of the ten rooms
    /// at the anchors in CenoteBase_Shell (cenote_furniture_gen.py), the simple colliders (*_COL), and the slug in
    /// third person over the right shoulder with the bag on its back (V: first person, behind). Menu: Slug/CenoteBase Build Scene. F6 in play steps every room through S1 → S2 → S3.
    /// The FBX are exported Z-up and imported with Bake Axis Conversion: Blender (x, y, z) is Unity (x, z, y).
    /// </summary>
    public static class CenoteBaseBuilder
    {
        const string ScenePath = "Assets/Scenes/Game/CenoteBase.unity";
        const string BaseDir = "Assets/Art/Models/Base/CenoteBase/";
        const string FurnDir = "Assets/Art/Models/Base/CenoteFurniture/";

        static readonly string[] Visual =
        {
            "CenoteBase_Shell", "CenoteBase_ShellOuter", "CenoteBase_Floor", "CenoteBase_Ramp", "CenoteBase_Gallery",
            "CenoteBase_GalleryRail", "CenoteBase_Pier", "CenoteBase_Boat", "CenoteBase_Deck", "CenoteBase_DeckFrame", "CenoteBase_Lift",
        };   // CenoteBase_Observatory is not placed: the deck furniture (CenoteFurniture_Deck) has the observatory roof
        static readonly string[] Colliders =
        {
            "CenoteBase_Shell_COL", "CenoteBase_Floor_COL", "CenoteBase_Ramp_COL", "CenoteBase_Gallery_COL",
            "CenoteBase_Pier_COL", "CenoteBase_Deck_COL", "CenoteBase_Rail_COL",
        };
        static readonly string[] Rooms = { "MapTable", "Nursery", "Deck", "Pier", "Trade", "Dye", "Collection", "Workshop", "Drying", "Refinery" };
        static readonly string[] Loose = { "CaveLamps", "BirdBasket", "Nest" };
        // where the loose pieces sit (they are exported at the origin): the bird's basket (d 2.4 m) on the landing ring in
        // the east of the deck (Blender (4.4, -8.5, 4.6)), the nest on the cliff above the sea mouth (Blender (-2.5, -6.5, 7.5))
        static readonly Vector3 BasketAt = new Vector3(4.4f, 4.6f, -8.5f);
        static readonly Vector3 NestAt = new Vector3(-2.5f, 7.5f, -6.5f);

        // the slug starts on the hall floor south of the map table, looking at it; the third-person camera 1.8 m behind
        // stays clear of the lift's low board (its frame has no collider)
        static readonly Vector3 Spawn = new Vector3(-1.3f, 2.0f, -1.9f);
        const float SpawnYaw = -12f;

        [MenuItem("Slug/CenoteBase Build Scene")]
        public static void Build()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            SetupLight();

            var root = new GameObject("CenoteBase").transform;
            var visual = new GameObject("Visual").transform; visual.SetParent(root, false);
            var cols = new GameObject("Colliders").transform; cols.SetParent(root, false);
            Transform shell = null;
            foreach (var name in Visual)
            {
                var go = Place(BaseDir + name + ".fbx", visual);
                if (name == "CenoteBase_Shell") shell = go.transform;
            }
            foreach (var name in Colliders) PlaceCollider(BaseDir + name + ".fbx", cols);
            BuildLift(root);
            BuildSea(root);

            var furn = new GameObject("CenoteFurniture").transform;
            foreach (var room in Rooms)
            {
                var anchor = shell.Find("CenoteBase_Anchor_" + room);
                var go = Place(FurnDir + "CenoteFurniture_" + room + ".fbx", furn);
                if (!go || !anchor) { Debug.LogWarning("[CenoteBase] missing room or anchor: " + room); continue; }
                go.transform.SetPositionAndRotation(anchor.position, anchor.rotation);
                go.AddComponent<CenoteRoom>().stage = 1;
                AddFurnitureColliders(go);
            }
            foreach (var name in Loose)
            {
                var go = Place(FurnDir + "CenoteFurniture_" + name + ".fbx", furn);
                if (!go) continue;
                if (name == "BirdBasket") { go.transform.position = BasketAt; AddBasketCollider(go); }
                if (name == "Nest") go.transform.position = NestAt;
            }

            BuildMapChart(furn);
            BuildMusic(root);

            var player = MoveTestBuilder.BuildPlayer(MoveTestBuilder.LoadSettings(), Spawn);
            player.transform.rotation = Quaternion.Euler(0f, SpawnYaw, 0f);
            var rig = player.GetComponent<SlugCameraRig>();
            rig.thirdPerson = true; rig.shoulder = true;   // the base is seen over the right shoulder (V: first person, behind)
            // the bag on the back, as when coming home from a raid (Q looks at it, G takes the top thing out)
            var bag = player.AddComponent<SlugExtract.SlugBag>();
            bag.settings = AssetDatabase.LoadAssetAtPath<SlugExtract.ExtractSettings>(MoveTestBuilder.TuningDir + "/ExtractSettings.asset");
            bag.controller = player.GetComponent<SlugMoveController>();
            SlugExtract.ExtractTestBuilder.FitBag(player, bag);
            var dev = new GameObject("CenoteBaseDev").AddComponent<CenoteBaseDev>();
            dev.player = player.transform; dev.spawn = Spawn; dev.spawnYaw = SpawnYaw;

            EditorSceneManager.SaveScene(scene, ScenePath);
            MoveTestBuilder.AddToBuildSettings(ScenePath);
            AssetDatabase.SaveAssets();
            Debug.Log("[CenoteBase] scene saved: " + ScenePath);
        }

        static GameObject Place(string path, Transform parent)
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (!asset) { Debug.LogWarning("[CenoteBase] missing " + path); return null; }
            return (GameObject)PrefabUtility.InstantiatePrefab(asset, parent);
        }

        // the lift's two boards hang at 4:45 and 5:39 on the clock, radius 2.85 (cenote_base_gen.py LIFT_*): Blender (x, y) below
        static readonly Vector3[] LiftSpots = { new Vector3(2.05f, 0f, -1.99f), new Vector3(0.61f, 0f, -2.47f) };

        /// <summary>The lift: two boards (board 0 starts up at the gallery) with a box to stand on, and their vertical ropes.</summary>
        static void BuildLift(Transform parent)
        {
            var root = new GameObject("CenoteLift").transform;
            root.SetParent(parent, false);
            var lift = root.gameObject.AddComponent<CenoteLift>();
            var boardAsset = AssetDatabase.LoadAssetAtPath<GameObject>(BaseDir + "CenoteBase_LiftBoard.fbx");
            var ropeAsset = AssetDatabase.LoadAssetAtPath<GameObject>(BaseDir + "CenoteBase_LiftRope.fbx");
            for (int i = 0; i < 2; i++)
            {
                var b = (GameObject)PrefabUtility.InstantiatePrefab(boardAsset, root);
                b.name = "LiftBoard_" + i;
                b.transform.position = LiftSpots[i] + Vector3.up * (i == 0 ? lift.highY : lift.lowY);
                var box = b.AddComponent<BoxCollider>();
                box.center = new Vector3(0f, 0f, 0f);
                box.size = new Vector3(1.0f, 0.1f, 1.0f);
                var rb = b.AddComponent<Rigidbody>();
                rb.isKinematic = true;
                lift.boards[i] = b.transform;
                var r = (GameObject)PrefabUtility.InstantiatePrefab(ropeAsset, root);
                r.name = "LiftRope_" + i;
                lift.ropes[i] = r.transform;
            }
            lift.upper = 0;
        }

        /// <summary>The *_COL meshes as static mesh colliders, without renderers.</summary>
        static void PlaceCollider(string path, Transform parent)
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (!asset) { Debug.LogWarning("[CenoteBase] missing " + path); return; }
            foreach (var mf in asset.GetComponentsInChildren<MeshFilter>(true))
            {
                var go = new GameObject(asset.name) { isStatic = true };
                go.transform.SetParent(parent, false);
                var t = mf.transform;
                go.transform.SetPositionAndRotation(t.position, t.rotation);
                go.transform.localScale = t.lossyScale;
                go.AddComponent<MeshCollider>().sharedMesh = mf.sharedMesh;
            }
        }

        /// <summary>
        /// Furniture collision, kept simple: the rock-carved parts (&lt;Room&gt;_Carved*) as mesh colliders, and the map
        /// table as one box from the floor to its top (the slug can climb on it). The rest has none for now.
        /// </summary>
        static void AddFurnitureColliders(GameObject room)
        {
            foreach (var mf in room.GetComponentsInChildren<MeshFilter>(true))
            {
                if (mf.name.Contains("_Carved")) mf.gameObject.AddComponent<MeshCollider>().sharedMesh = mf.sharedMesh;
                if (mf.name == "MapTable_Top")
                {
                    var b = mf.sharedMesh.bounds;
                    var box = mf.gameObject.AddComponent<BoxCollider>();
                    float top = b.max.y;
                    box.center = new Vector3(b.center.x, top * 0.5f, b.center.z);
                    box.size = new Vector3(b.size.x * 0.9f, top, b.size.z * 0.9f);
                }
            }
        }

        /// <summary>
        /// The bird's basket: a floor the slugs crawl onto (0.4 m up, lower than the 0.45 m they can climb) and a low
        /// wall round it, so they can climb in and out from any side. The wall is a thick rim (0.24 m): climbing needs a
        /// ledge on top to crawl onto before going over.
        /// </summary>
        static void AddBasketCollider(GameObject basket)
        {
            var floor = basket.AddComponent<BoxCollider>();
            floor.center = new Vector3(0f, 0.03f, 0f);
            floor.size = new Vector3(2.1f, 0.06f, 2.1f);
            const int n = 18;
            for (int i = 0; i < n; i++)
            {
                float a = i * Mathf.PI * 2f / n;
                var seg = new GameObject("BasketWall_" + i);
                seg.transform.SetParent(basket.transform, false);
                seg.transform.localPosition = new Vector3(Mathf.Cos(a) * 1.08f, 0.2f, Mathf.Sin(a) * 1.08f);
                seg.transform.localRotation = Quaternion.Euler(0f, -a * Mathf.Rad2Deg, 0f);
                seg.AddComponent<BoxCollider>().size = new Vector3(0.24f, 0.4f, 0.40f);
            }
        }

        const string MapTexPath = "Assets/Art/Models/Base/Textures/CenoteBase_MapChart.png";
        const string MapMatPath = "Assets/Art/Models/Base/Materials/CenoteBase_MapChart.mat";
        const string MapMeshPath = "Assets/Base/Generated/CenoteBase_MapChartDisc.asset";
        const string MusicPath = "Assets/Audio/BGM/base_music1.wav";

        /// <summary>
        /// The island chart on the map table: a flat disc (r 1.0 m) just above the table top, with the old pirate-style
        /// picture drawn by Blender/Base/cenote_map_gen.py (north up = +Z).
        /// </summary>
        static void BuildMapChart(Transform parent)
        {
            var imp = (TextureImporter)AssetImporter.GetAtPath(MapTexPath);
            if (imp == null) { Debug.LogWarning("[CenoteBase] missing " + MapTexPath); return; }
            if (imp.wrapMode != TextureWrapMode.Clamp || imp.anisoLevel != 8)
            {
                imp.wrapMode = TextureWrapMode.Clamp; imp.anisoLevel = 8; imp.mipmapEnabled = true; imp.sRGBTexture = true;
                imp.SaveAndReimport();
            }
            var mat = WorldLook.GetOrCreate(MapMatPath);
            WorldLook.Apply(mat, Color.white, rim: 0.05f);
            mat.SetFloat("_Facet", 0f);
            mat.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(MapTexPath));
            EditorUtility.SetDirty(mat);

            MoveTestBuilder.EnsureFolder("Assets/Base/Generated");
            const int n = 72;
            var v = new Vector3[n + 1]; var uv = new Vector2[n + 1]; var t = new int[n * 3];
            v[0] = Vector3.zero; uv[0] = new Vector2(0.5f, 0.5f);
            for (int i = 0; i < n; i++)
            {
                float a = i * Mathf.PI * 2f / n;
                v[i + 1] = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                uv[i + 1] = new Vector2(0.5f + 0.5f * Mathf.Cos(a), 0.5f + 0.5f * Mathf.Sin(a));
                t[i * 3] = 0; t[i * 3 + 1] = (i + 1) % n + 1; t[i * 3 + 2] = i + 1;
            }
            var mesh = new Mesh { name = "CenoteBase_MapChartDisc", vertices = v, uv = uv, triangles = t };
            mesh.RecalculateNormals(); mesh.RecalculateBounds();
            if (AssetDatabase.LoadAssetAtPath<Mesh>(MapMeshPath)) AssetDatabase.DeleteAsset(MapMeshPath);
            AssetDatabase.CreateAsset(mesh, MapMeshPath);

            var go = new GameObject("MapChart");
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(-1.8f, 2.0f + 0.386f, 0.5f);     // the map table anchor + its top
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = mat;
        }

        /// <summary>The base's music (Assets/Audio/BGM/base_music1.wav, わくさん's), looping, not placed in space.</summary>
        static void BuildMusic(Transform parent)
        {
            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(MusicPath);
            if (!clip) { Debug.LogWarning("[CenoteBase] missing " + MusicPath); return; }
            var imp = (AudioImporter)AssetImporter.GetAtPath(MusicPath);
            var ss = imp.defaultSampleSettings;
            if (ss.loadType != AudioClipLoadType.Streaming) { ss.loadType = AudioClipLoadType.Streaming; imp.defaultSampleSettings = ss; imp.SaveAndReimport(); }
            var go = new GameObject("BaseMusic");
            go.transform.SetParent(parent, false);
            var src = go.AddComponent<AudioSource>();
            src.clip = clip; src.loop = true; src.playOnAwake = true; src.spatialBlend = 0f; src.volume = 0.45f;
        }

        /// <summary>A light from high above that falls through the skylight onto the pool, and a soft ambient for the cave.</summary>
        static void SetupLight()
        {
            var light = new GameObject("Directional Light").AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.25f;
            light.color = new Color(1f, 0.96f, 0.88f);
            light.shadows = LightShadows.Soft;
            light.transform.rotation = Quaternion.Euler(78f, 30f, 0f);

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.62f, 0.66f, 0.72f);
            RenderSettings.ambientEquatorColor = new Color(0.50f, 0.50f, 0.52f);
            RenderSettings.ambientGroundColor = new Color(0.34f, 0.33f, 0.31f);
        }

        /// <summary>The sea around the island (sea level is y = 0), the same water colour as the pool.</summary>
        static void BuildSea(Transform parent)
        {
            var sea = GameObject.CreatePrimitive(PrimitiveType.Plane);
            sea.name = "Sea";
            Object.DestroyImmediate(sea.GetComponent<Collider>());
            sea.transform.SetParent(parent, false);
            sea.transform.localScale = new Vector3(30f, 1f, 30f);
            var water = AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Models/Base/Materials/CenoteBase_Water.mat");
            if (water) sea.GetComponent<Renderer>().sharedMaterial = water;
        }
    }
}
