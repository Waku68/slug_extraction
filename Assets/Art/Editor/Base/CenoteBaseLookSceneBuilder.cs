using System.IO;
using slug = SlugBase;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SlugDesign
{
    public static partial class CenoteBaseCodeBuilder
    {
        static readonly string[] CaveVisualNames =
        {
            "CenoteBase_Shell", "CenoteBase_ShellOuter", "CenoteBase_Floor", "CenoteBase_Ramp",
            "CenoteBase_Gallery", "CenoteBase_GalleryRail", "CenoteBase_Pier", "CenoteBase_Boat",
            "CenoteBase_Deck", "CenoteBase_DeckFrame", "CenoteBase_Lift",
        };

        static readonly string[] CaveColliderNames =
        {
            "CenoteBase_Shell_COL", "CenoteBase_Floor_COL", "CenoteBase_Ramp_COL", "CenoteBase_Gallery_COL",
            "CenoteBase_Pier_COL", "CenoteBase_Deck_COL", "CenoteBase_Rail_COL",
        };

        static readonly string[] RoomNames =
        {
            "MapTable", "Nursery", "Deck", "Pier", "Trade", "Dye", "Collection", "Workshop", "Drying", "Refinery"
        };

        static readonly Vector3 BasketPos = new Vector3(4.4f, 4.6f, -8.5f);
        static readonly Vector3 NestPos   = new Vector3(-2.5f, 7.5f, -6.5f);
        static readonly Vector3 BirdDeckPos = new Vector3(3.1f, 4.6f, -6.6f);

        [MenuItem("Slug/Base/Build Cenote Base (Code)")]
        public static void BuildAll()
        {
            var mats = BuildAllMaterials();
            BuildCavePrefabs(mats);
            BuildFurniturePrefabs(mats);
            AssetDatabase.SaveAssets();
            BuildLookScene(mats);
            Debug.Log("[CenoteBaseCodeBuilder] Built all cave, furniture, basket, nest prefabs and CenoteBaseLook.unity");
        }

        public static void BuildLookScene(Material[] mats = null)
        {
            if (mats == null) mats = BuildAllMaterials();
            Directory.CreateDirectory(Path.GetDirectoryName(PreviewScenePath));
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // 太陽（天窓から池に差し込む光）と環境光
            var sunGo = new GameObject("Directional Light");
            var sun = sunGo.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.intensity = 1.30f;
            sun.color = new Color(1f, 0.96f, 0.88f);
            sun.shadows = LightShadows.Soft;
            sunGo.transform.rotation = Quaternion.Euler(72f, 25f, 0f);

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.66f, 0.70f, 0.76f);
            RenderSettings.ambientEquatorColor = new Color(0.54f, 0.53f, 0.55f);
            RenderSettings.ambientGroundColor = new Color(0.38f, 0.36f, 0.34f);

            var root = new GameObject("CenoteBase").transform;
            var visual = new GameObject("Visual").transform; visual.SetParent(root, false);
            var cols = new GameObject("Colliders").transform; cols.SetParent(root, false);

            Transform shell = null;
            foreach (var name in CaveVisualNames)
            {
                var go = InstantiateAsset($"{BaseDir}/{name}.prefab", visual);
                if (name == "CenoteBase_Shell" && go) shell = go.transform;
            }
            foreach (var name in CaveColliderNames)
                PlaceColliderMesh($"{BaseDir}/{name}.prefab", cols);

            // つるべリフト（CenoteLift）
            BuildLiftInScene(root);

            // 海面（Y = 0）
            var sea = GameObject.CreatePrimitive(PrimitiveType.Plane);
            sea.name = "Sea";
            Object.DestroyImmediate(sea.GetComponent<Collider>());
            sea.transform.SetParent(root, false);
            sea.transform.localScale = new Vector3(30f, 1f, 30f);
            sea.GetComponent<Renderer>().sharedMaterial = mats[M_WATER];

            // 10部屋の家具と段階（見栄え確認のため初期 stage = 3 に設定、F6やインスペクタで1〜3切替可）
            var furn = new GameObject("CenoteFurniture").transform;
            foreach (var room in RoomNames)
            {
                var anchor = shell ? shell.Find("CenoteBase_Anchor_" + room) : null;
                var go = InstantiateAsset($"{FurnDir}/CenoteFurniture_{room}.prefab", furn);
                if (!go || !anchor) continue;
                go.transform.SetPositionAndRotation(anchor.position, anchor.rotation);
                var cr = go.AddComponent<slug.CenoteRoom>();
                cr.SetStage(3);
                AddRoomColliders(go);
            }

            // 洞窟ランプ・四角い鳥籠・鳥の巣
            InstantiateAsset($"{FurnDir}/CenoteFurniture_CaveLamps.prefab", furn);
            var basket = InstantiateAsset($"{FurnDir}/CenoteFurniture_BirdBasket.prefab", furn);
            if (basket) { basket.transform.position = BasketPos; AddSquareBasketColliders(basket); }
            var nest = InstantiateAsset($"{FurnDir}/CenoteFurniture_Nest.prefab", furn);
            if (nest) nest.transform.position = NestPos;

            // 温かいオイルランプの点光源
            AddWarmLight(furn, "LampLight_Table",   new Vector3(-1.8f, 3.4f,  0.5f), 5.5f, 1.4f);
            AddWarmLight(furn, "LampLight_Pool",    new Vector3( 1.8f, 3.8f,  0.2f), 6.0f, 1.2f);
            AddWarmLight(furn, "LampLight_Gallery", new Vector3( 0.0f, 5.8f,  3.6f), 6.5f, 1.3f);
            AddWarmLight(furn, "LampLight_South",   new Vector3( 1.8f, 5.6f, -3.2f), 6.0f, 1.2f);
            AddWarmLight(furn, "LampLight_Pier",    new Vector3(-2.5f, 3.4f, -6.5f), 6.0f, 1.3f);

            // 新しいコード版の鳥（デッキに立つ相棒）
            var birdAsset = AssetDatabase.LoadAssetAtPath<GameObject>($"{CarrierBirdBuilder.Dir}/CarrierBird.prefab");
            if (birdAsset)
            {
                var birdGo = (GameObject)PrefabUtility.InstantiatePrefab(birdAsset);
                birdGo.name = "CarrierBird";
                birdGo.transform.SetPositionAndRotation(BirdDeckPos, Quaternion.Euler(0f, 155f, 0f));
            }

            // 比較用ナメクジカプセル（体長 1.1m × 高さ 0.5m）：地図テーブル、坂、回廊、桟橋、デッキの籠の中
            var slugRoot = new GameObject("SlugProxies").transform;
            var slugMat = WorldLook.GetOrCreate($"{MatDir}/PreviewSlugProxy.mat");
            ColorUtility.TryParseHtmlString("#7BA368", out var sCol);
            WorldLook.Apply(slugMat, sCol, 0.2f, 0f);
            slugMat.SetFloat("_Facet", 0f);

            CarrierBirdBuilder.MakeSlugProxy("Slug_AtMapTable", new Vector3(-1.5f, 2.00f, -1.2f), slugMat).transform.SetParent(slugRoot, false);
            CarrierBirdBuilder.MakeSlugProxy("Slug_OnRamp",     ClockPos(9.4f, 4.15f, 3.25f),     slugMat).transform.SetParent(slugRoot, false);
            CarrierBirdBuilder.MakeSlugProxy("Slug_OnGallery",  ClockPos(12.2f, 3.65f, 4.60f),    slugMat).transform.SetParent(slugRoot, false);
            CarrierBirdBuilder.MakeSlugProxy("Slug_OnPier",     new Vector3(-2.5f, 2.00f, -8.5f), slugMat).transform.SetParent(slugRoot, false);
            CarrierBirdBuilder.MakeSlugProxy("Slug_InBasket",   BasketPos + new Vector3(0f, 0.08f, 0f), slugMat).transform.SetParent(slugRoot, false);

            // メインカメラ（1階の地図テーブル手前、ナメクジの目の高さ 0.47m）
            var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
            var cam = camGo.AddComponent<Camera>();
            cam.fieldOfView = 52f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.66f, 0.76f, 0.84f);
            camGo.transform.position = new Vector3(-2.8f, 2.47f, -1.6f);
            camGo.transform.LookAt(new Vector3(0.8f, 3.0f, 1.2f));

            EditorSceneManager.SaveScene(scene, PreviewScenePath);
        }

        static GameObject InstantiateAsset(string path, Transform parent)
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (!asset) return null;
            return (GameObject)PrefabUtility.InstantiatePrefab(asset, parent);
        }

        static void PlaceColliderMesh(string path, Transform parent)
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (!asset) return;
            foreach (var mf in asset.GetComponentsInChildren<MeshFilter>(true))
            {
                var go = new GameObject(asset.name) { isStatic = true };
                go.transform.SetParent(parent, false);
                go.transform.SetPositionAndRotation(mf.transform.position, mf.transform.rotation);
                go.transform.localScale = mf.transform.lossyScale;
                go.AddComponent<MeshCollider>().sharedMesh = mf.sharedMesh;
            }
        }

        static void BuildLiftInScene(Transform parent)
        {
            Vector3[] spots = { new Vector3(2.05f, 0f, -1.99f), new Vector3(0.61f, 0f, -2.47f) };
            var root = new GameObject("CenoteLift").transform;
            root.SetParent(parent, false);
            var lift = root.gameObject.AddComponent<slug.CenoteLift>();
            var boardAsset = AssetDatabase.LoadAssetAtPath<GameObject>($"{BaseDir}/CenoteBase_LiftBoard.prefab");
            var ropeAsset  = AssetDatabase.LoadAssetAtPath<GameObject>($"{BaseDir}/CenoteBase_LiftRope.prefab");
            if (!boardAsset || !ropeAsset) return;
            for (int i = 0; i < 2; i++)
            {
                var b = (GameObject)PrefabUtility.InstantiatePrefab(boardAsset, root);
                b.name = "LiftBoard_" + i;
                b.transform.position = spots[i] + Vector3.up * (i == 0 ? lift.highY : lift.lowY);
                var box = b.AddComponent<BoxCollider>();
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

        static void AddRoomColliders(GameObject room)
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

        static void AddSquareBasketColliders(GameObject basket)
        {
            var floor = basket.AddComponent<BoxCollider>();
            floor.center = new Vector3(0f, 0.04f, 0f);
            floor.size = new Vector3(2.4f, 0.08f, 2.4f);
            Vector3[] c = { new Vector3(0f, 0.2f, -1.2f), new Vector3(0f, 0.2f, 1.2f), new Vector3(-1.2f, 0.2f, 0f), new Vector3(1.2f, 0.2f, 0f) };
            Vector3[] s = { new Vector3(2.4f, 0.4f, 0.24f), new Vector3(2.4f, 0.4f, 0.24f), new Vector3(0.24f, 0.4f, 2.4f), new Vector3(0.24f, 0.4f, 2.4f) };
            for (int i = 0; i < 4; i++)
            {
                var w = new GameObject("BasketWall_" + i);
                w.transform.SetParent(basket.transform, false);
                w.transform.localPosition = c[i];
                w.AddComponent<BoxCollider>().size = s[i];
            }
        }

        static void AddWarmLight(Transform parent, string name, Vector3 pos, float range, float intensity)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            var l = go.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = new Color(1.0f, 0.76f, 0.42f);
            l.range = range;
            l.intensity = intensity;
        }
    }
}
