using SlugDesign;
using SlugMove;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SlugBase
{
    /// <summary>
    /// The departure flight of the cenote base (<see cref="CarrierFlight"/>):
    /// - Bake Island Model: a far model of IslandTest_800 made from its terrain (161 x 161 points over 960 m, one point
    ///   every 6 m, about 51,000 triangles), coloured from the terrain's paint (a 256 px map of the layers' colours).
    ///   Bake again after rebuilding the island.
    /// - Add Departure: puts the island model (hidden until the flight) and the CarrierFlight object into the open
    ///   CenoteBase scene, wired to the bird, the basket, the slug, the camera and the sea.
    /// </summary>
    public static class CarrierFlightSetup
    {
        const string TerrainPath = "Assets/TestScenes/Generated/IslandTest_800_Terrain.asset";
        const string MeshPath = "Assets/Base/Generated/IslandModel_800.asset";
        const string TexPath = "Assets/Base/Generated/IslandModel_800_Color.png";
        const string MatPath = "Assets/Base/Generated/IslandModel_800.mat";
        const int Grid = 161, TexRes = 256;
        const float TerrainBottom = -20f;   // IslandTest800Builder: the terrain sits at (-Size/2, Bottom, -Size/2)

        [MenuItem("Slug/CenoteBase Bake Island Model")]
        public static void Bake()
        {
            var td = AssetDatabase.LoadAssetAtPath<TerrainData>(TerrainPath);
            if (!td) { Debug.LogError("[CarrierFlight] no terrain at " + TerrainPath + " (build IslandTest_800 first)"); return; }
            Vector3 size = td.size;

            // ---- shape: the island's middle at the origin, the sea at y = 0
            var v = new Vector3[Grid * Grid];
            var uv = new Vector2[v.Length];
            for (int z = 0; z < Grid; z++)
                for (int x = 0; x < Grid; x++)
                {
                    float u = x / (Grid - 1f), w = z / (Grid - 1f);
                    float h = td.GetInterpolatedHeight(u, w) + TerrainBottom;
                    v[z * Grid + x] = new Vector3((u - 0.5f) * size.x, h, (w - 0.5f) * size.z);
                    uv[z * Grid + x] = new Vector2(u, w);
                }
            var t = new int[(Grid - 1) * (Grid - 1) * 6];
            int i = 0;
            for (int z = 0; z < Grid - 1; z++)
                for (int x = 0; x < Grid - 1; x++)
                {
                    int a = z * Grid + x, b = a + 1, c = a + Grid, d = c + 1;
                    t[i++] = a; t[i++] = c; t[i++] = b;
                    t[i++] = b; t[i++] = c; t[i++] = d;
                }
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(MeshPath);
            if (!mesh) { mesh = new Mesh(); AssetDatabase.CreateAsset(mesh, MeshPath); }
            mesh.Clear();
            mesh.name = "IslandModel_800";
            mesh.vertices = v; mesh.uv = uv; mesh.triangles = t;
            mesh.RecalculateNormals(); mesh.RecalculateBounds();
            EditorUtility.SetDirty(mesh);

            // ---- colour: each layer's colour, mixed by the paint
            var layers = td.terrainLayers;
            var col = new Color[layers.Length];
            for (int l = 0; l < layers.Length; l++) col[l] = Average(layers[l]);
            float[,,] alpha = td.GetAlphamaps(0, 0, td.alphamapWidth, td.alphamapHeight);
            var px = new Color[TexRes * TexRes];
            for (int y = 0; y < TexRes; y++)
                for (int x = 0; x < TexRes; x++)
                {
                    int ax = Mathf.Min(td.alphamapWidth - 1, x * td.alphamapWidth / TexRes);
                    int ay = Mathf.Min(td.alphamapHeight - 1, y * td.alphamapHeight / TexRes);
                    Color c = Color.black;
                    for (int l = 0; l < layers.Length; l++) c += col[l] * alpha[ay, ax, l];   // alphamaps are [y, x, layer]
                    c.a = 1f;
                    px[y * TexRes + x] = c;
                }
            var tex = new Texture2D(TexRes, TexRes, TextureFormat.RGBA32, false);
            tex.SetPixels(px); tex.Apply();
            System.IO.File.WriteAllBytes(TexPath, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(TexPath);
            var imp = (TextureImporter)AssetImporter.GetAtPath(TexPath);
            imp.wrapMode = TextureWrapMode.Clamp; imp.mipmapEnabled = true; imp.sRGBTexture = true;
            imp.SaveAndReimport();

            var mat = WorldLook.GetOrCreate(MatPath);
            WorldLook.Apply(mat, Color.white, rim: 0.05f);
            mat.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(TexPath));
            mat.SetFloat("_OutlineWidth", 0f);    // a 960 m model: an outline would be metres wide
            EditorUtility.SetDirty(mat);
            AssetDatabase.SaveAssets();
            Debug.Log($"[CarrierFlight] island model: {v.Length} points, {t.Length / 3} triangles");
        }

        static Color Average(TerrainLayer layer)
        {
            var tex = layer.diffuseTexture;
            if (!tex || !tex.isReadable) return new Color(0.5f, 0.5f, 0.5f);
            Color sum = Color.black;
            var p = tex.GetPixels();
            foreach (var c in p) sum += c;
            sum /= Mathf.Max(1, p.Length);
            sum.a = 1f;
            return sum * (Color)layer.diffuseRemapMax;
        }

        [MenuItem("Slug/CenoteBase Add Departure (open scene)")]
        public static void AddDeparture()
        {
            var bird = Object.FindAnyObjectByType<CarrierBirdMotion>();
            var basket = GameObject.Find("CenoteFurniture/CenoteFurniture_BirdBasket");
            var player = Object.FindAnyObjectByType<SlugMoveController>();
            var sea = GameObject.Find("CenoteBase/Sea");
            if (!bird || !basket || !player) { Debug.LogError("[CarrierFlight] open CenoteBase (it needs the bird, the basket and the slug)"); return; }
            if (!AssetDatabase.LoadAssetAtPath<Mesh>(MeshPath)) Bake();

            var old = Object.FindAnyObjectByType<CarrierFlight>();
            if (old) Object.DestroyImmediate(old.gameObject);
            var oldModel = GameObject.Find("IslandModel");
            if (oldModel) Object.DestroyImmediate(oldModel);

            var go = new GameObject("CarrierFlight");
            var flight = go.AddComponent<CarrierFlight>();

            var model = new GameObject("IslandModel");
            model.AddComponent<MeshFilter>().sharedMesh = AssetDatabase.LoadAssetAtPath<Mesh>(MeshPath);
            var mr = model.AddComponent<MeshRenderer>();
            mr.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(MatPath);
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            model.transform.position = flight.islandCenter;
            model.SetActive(false);

            flight.bird = bird;
            flight.basket = basket.transform;
            flight.player = player;
            flight.rig = player.GetComponent<SlugCameraRig>();
            flight.cam = flight.rig && flight.rig.cam ? flight.rig.cam : Camera.main;
            flight.sea = sea ? sea.transform : null;
            flight.islandModel = model;

            EditorSceneManager.MarkSceneDirty(go.scene);
            Selection.activeGameObject = go;
            Debug.Log("[CarrierFlight] added to " + go.scene.name + " (save the scene to keep it)");
        }
    }
}
