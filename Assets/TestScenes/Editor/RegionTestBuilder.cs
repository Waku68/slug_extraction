using System.Collections.Generic;
using SlugMove;
using SlugWorld;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SlugExtract
{
    /// <summary>
    /// Builds RegionTest: a gray prototype of a whole region, 800 m across, to crawl through and time
    /// (docs/discussions/map_discussion.md, regions_discussion.md). The "all-in-one" estuary: a forest of giant trees in
    /// the east (crops, puddles), a river winding from east to west with shallow fords and fallen-log bridges, a hill with a
    /// rock-salt cave in the north, and the white salt flat with giant rocks in the west where the river spreads into a
    /// lagoon. The player starts in the east; the exit is past the lagoon in the west. Burrow spots are not placed yet
    /// (they are chosen after crawling the region). Everything is gray boxes and a terrain: a sketch, not the real region.
    /// </summary>
    public static class RegionTestBuilder
    {
        const string ScenePath = "Assets/Scenes/Tests/RegionTest.unity";
        const string TerrainPath = MoveTestBuilder.GenDir + "/RegionTest_Terrain.asset";
        const float Size = 800f, MaxHeight = 60f;
        const int Res = 513;

        static readonly float[] Fords = { 230f, 40f, -125f };
        static readonly float[] Bridges = { 140f, -40f };

        [MenuItem("Slug/RegionTest Build Scene")]
        public static void Build()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var moveSettings = MoveTestBuilder.LoadSettings();
            var scene = MoveTestBuilder.NewScene(new Vector2(60f, 60f));
            ExtractTestBuilder.PrepareAssets();
            var map = MoveTestBuilder._map;
            Random.InitState(4242);

            var terrain = BuildTerrain(map);
            System.Func<float, float, float> ground = (x, z) => terrain.SampleHeight(new Vector3(x, 0f, z));

            var lit = Shader.Find("Universal Render Pipeline/Lit");
            var rockMat = MoveTestBuilder.SaveMaterial("W_GiantRock", lit, new Color(0.36f, 0.35f, 0.34f));
            var bark = MoveTestBuilder.SaveMaterial("W_Bark", lit, new Color(0.33f, 0.25f, 0.18f));
            var leaves = MoveTestBuilder.SaveMaterial("W_Leaves", lit, new Color(0.24f, 0.38f, 0.2f));
            var water = MoveTestBuilder.SaveMaterial("W_Puddle", lit, new Color(0.2f, 0.3f, 0.38f));
            water.SetFloat("_Smoothness", 0.92f);
            var caveMat = MoveTestBuilder.SaveMaterial("W_CaveRock", lit, new Color(0.27f, 0.25f, 0.26f));
            var logMat = MoveTestBuilder.SaveMaterial("W_Log", lit, new Color(0.38f, 0.3f, 0.22f));

            var props = new GameObject("Region").transform;
            props.SetParent(map, false);

            // ---- the river: water along the bed, fords where it is shallow, logs across it
            for (float x = Size * 0.5f; x > -270f; x -= 10f)
            {
                float zc = RiverZ(x);
                float bed = ground(x, zc);
                bool ford = NearFord(x);
                float level = bed + (ford ? 0.25f : 1.2f);
                // the segment's long side along the river (local x), its width across it
                float dx = -10f, dz = RiverZ(x - 5f) - RiverZ(x + 5f);
                float yaw = Mathf.Atan2(-dz, dx) * Mathf.Rad2Deg;
                var seg = WeatherSceneSetup.Prim(props, "River", PrimitiveType.Cube, water, new Vector3(x, level, zc), new Vector3(Mathf.Sqrt(dx * dx + dz * dz) + 1f, 0.04f, 15f), yaw);
                Object.DestroyImmediate(seg.GetComponent<Collider>());
                seg.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                WeatherSceneSetup.Zone(props, "River_Zone", MoistureZone.Kind.Water, new Vector3(x, level, zc), new Vector3(11f, 3f, 13f));
            }
            // the lagoon where it spreads into the salt flat
            {
                Vector3 c = new Vector3(-300f, 0f, RiverZ(-300f));
                c.y = ground(c.x, c.z) + 0.9f;
                var lagoon = WeatherSceneSetup.Prim(props, "Lagoon", PrimitiveType.Cylinder, water, c, new Vector3(64f, 0.02f, 50f), 0f);
                Object.DestroyImmediate(lagoon.GetComponent<Collider>());
                WeatherSceneSetup.Zone(props, "Lagoon_Zone", MoistureZone.Kind.Water, c, new Vector3(56f, 3f, 42f));
            }
            foreach (float bx in Bridges)
            {
                float zc = RiverZ(bx);
                float bank = Mathf.Max(ground(bx, zc - 11f), ground(bx, zc + 11f));
                var log = WeatherSceneSetup.Prim(props, "LogBridge", PrimitiveType.Cylinder, logMat, new Vector3(bx, bank - 0.5f, zc), new Vector3(1.6f, 13f, 1.6f), 0f);
                log.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            }

            // ---- the east: a forest of giant trees, crops in the clearings, puddles
            int trees = 0;
            for (int i = 0; i < 400 && trees < 38; i++)
            {
                float x = Random.Range(110f, 385f), z = Random.Range(-385f, 385f);
                if (Mathf.Abs(z - RiverZ(x)) < 20f) continue;
                if (Vector2.Distance(new Vector2(x, z), new Vector2(370f, RiverZ(370f) + 35f)) < 30f) continue;   // keep the start open
                float h = Random.Range(14f, 22f), r = Random.Range(1.6f, 2.8f), canopy = Random.Range(11f, 17f);
                float y = ground(x, z);
                WeatherSceneSetup.Prim(props, "GiantTree_Trunk", PrimitiveType.Cylinder, bark, new Vector3(x, y + h * 0.5f, z), new Vector3(r, h * 0.5f, r), 0f);
                WeatherSceneSetup.Prim(props, "GiantTree_Canopy", PrimitiveType.Sphere, leaves, new Vector3(x, y + h + canopy * 0.2f, z), new Vector3(canopy, canopy * 0.55f, canopy), 0f);
                trees++;
            }
            for (int i = 0, n = 0; i < 200 && n < 24; i++)
            {
                float x = Random.Range(120f, 380f), z = Random.Range(-360f, 360f);
                if (Mathf.Abs(z - RiverZ(x)) < 16f) continue;
                ExtractTestBuilder.Crop(props, new Vector3(x, ground(x, z), z));
                n++;
            }
            for (int i = 0, n = 0; i < 200 && n < 8; i++)
            {
                float x = Random.Range(120f, 380f), z = Random.Range(-360f, 360f);
                if (Mathf.Abs(z - RiverZ(x)) < 20f) continue;
                Puddle(props, water, new Vector3(x, ground(x, z), z), Random.Range(2.5f, 4.5f));
                n++;
            }

            // ---- the north: a hill with a rock-salt cave (a roofed room of rock, dark inside)
            {
                Vector3 c = new Vector3(-40f, 0f, 262f);
                c.y = ground(c.x, c.z);
                var cave = new GameObject("SaltCave").transform;
                cave.SetParent(props, false);
                cave.position = c;
                float w = 22f, d = 30f, h = 4.5f, t = 2f;
                WeatherSceneSetup.Prim(cave, "Roof", PrimitiveType.Cube, caveMat, c + new Vector3(0f, h + t * 0.5f, 0f), new Vector3(w + t * 2f, t, d + t * 2f), 0f);
                WeatherSceneSetup.Prim(cave, "Wall_W", PrimitiveType.Cube, caveMat, c + new Vector3(-w * 0.5f - t * 0.5f, h * 0.5f, 0f), new Vector3(t, h + 1f, d), 0f);
                WeatherSceneSetup.Prim(cave, "Wall_E", PrimitiveType.Cube, caveMat, c + new Vector3(w * 0.5f + t * 0.5f, h * 0.5f, 0f), new Vector3(t, h + 1f, d), 0f);
                WeatherSceneSetup.Prim(cave, "Wall_N", PrimitiveType.Cube, caveMat, c + new Vector3(0f, h * 0.5f, d * 0.5f + t * 0.5f), new Vector3(w + t * 2f, h + 1f, t), 0f);
                // the mouth faces south, half closed
                WeatherSceneSetup.Prim(cave, "Wall_S_A", PrimitiveType.Cube, caveMat, c + new Vector3(-w * 0.32f, h * 0.5f, -d * 0.5f - t * 0.5f), new Vector3(w * 0.36f, h + 1f, t), 0f);
                WeatherSceneSetup.Prim(cave, "Wall_S_B", PrimitiveType.Cube, caveMat, c + new Vector3(w * 0.32f, h * 0.5f, -d * 0.5f - t * 0.5f), new Vector3(w * 0.36f, h + 1f, t), 0f);
                for (int i = 0; i < 7; i++)
                {
                    Vector3 p = c + new Vector3(Random.Range(-w * 0.4f, w * 0.4f), 0f, Random.Range(-d * 0.3f, d * 0.4f));
                    p.y = ground(p.x, p.z);
                    ExtractTestBuilder.SaltRock(cave, p, Random.Range(1.3f, 1.8f));
                }
                WeatherSceneSetup.Zone(props, "Cave_Salt", MoistureZone.Kind.Salt, c + Vector3.up * 2f, new Vector3(w, 6f, d));
                MoveTestBuilder.Label("岩塩の洞窟（仮）", c + new Vector3(0f, h + 4f, -d * 0.5f), 6f);
            }

            // ---- the west: the white salt flat with giant rocks and rock salt
            for (int i = 0; i < 12; i++)
            {
                float x = Random.Range(-380f, -175f), z = Random.Range(-370f, 370f);
                if (Mathf.Abs(z - RiverZ(x)) < 30f) continue;
                Vector3 s = new Vector3(Random.Range(6f, 14f), Random.Range(5f, 12f), Random.Range(5f, 12f));
                WeatherSceneSetup.Prim(props, "GiantRock", PrimitiveType.Cube, rockMat, new Vector3(x, ground(x, z) + s.y * 0.45f, z), s, Random.Range(0f, 90f));
            }
            for (int i = 0, n = 0; i < 200 && n < 14; i++)
            {
                float x = Random.Range(-370f, -180f), z = Random.Range(-360f, 360f);
                if (Mathf.Abs(z - RiverZ(x)) < 22f) continue;
                ExtractTestBuilder.SaltRock(props, new Vector3(x, ground(x, z), z), Random.Range(1.1f, 1.6f));
                n++;
            }
            WeatherSceneSetup.Zone(props, "SaltFlat_Salt", MoistureZone.Kind.Salt, new Vector3(-290f, 3f, 0f), new Vector3(220f, 12f, 800f));

            // ---- labels, the start and the exit
            MoveTestBuilder.Label("雨の森（東）", new Vector3(300f, ground(300f, -80f) + 8f, -80f), 10f);
            MoveTestBuilder.Label("塩田（西）", new Vector3(-260f, ground(-260f, -120f) + 8f, -120f), 10f);
            MoveTestBuilder.Label("川", new Vector3(60f, ground(60f, RiverZ(60f)) + 5f, RiverZ(60f)), 8f);
            Vector3 exitAt = new Vector3(-370f, 0f, RiverZ(-370f) + 60f);
            exitAt.y = ground(exitAt.x, exitAt.z);
            var exit = ExtractTestBuilder.BuildExit(map, exitAt);
            Vector3 spawn = new Vector3(370f, 0f, RiverZ(370f) + 35f);
            spawn.y = ground(spawn.x, spawn.z) + 0.05f;
            var player = ExtractTestBuilder.BuildRaidPlayer(moveSettings, spawn, exit);
            player.transform.rotation = Quaternion.Euler(0f, -90f, 0f);     // facing west, down the river
            var cam = player.GetComponentInChildren<Camera>();
            if (cam) cam.farClipPlane = 500f;

            // the island's clock and weather, the body's moisture and health (no test-field props here)
            WeatherSceneSetup.Fit(false);
            EditorSceneManager.SaveScene(scene, ScenePath);
            MoveTestBuilder.AddToBuildSettings(ScenePath);
            AssetDatabase.SaveAssets();
            Debug.Log("[RegionTest] scene saved: " + ScenePath);
        }

        /// <summary>Where the river runs (its middle, z) at x: winding from the east edge to the lagoon in the west.</summary>
        static float RiverZ(float x) => -30f + 55f * Mathf.Sin(x / 130f) + 15f * Mathf.Sin(x / 47f + 1.3f);

        static bool NearFord(float x)
        {
            foreach (float f in Fords) if (Mathf.Abs(x - f) < 9f) return true;
            return false;
        }

        static float Smooth(float a, float b, float x) { float t = Mathf.Clamp01((x - a) / (b - a)); return t * t * (3f - 2f * t); }

        /// <summary>The ground's height at a world point (m).</summary>
        static float Height(float x, float z)
        {
            float h = 4f;
            // the east: rolling forest floor
            float east = Smooth(40f, 160f, x);
            h += east * 6f * Mathf.PerlinNoise(x * 0.011f + 3.1f, z * 0.011f + 7.7f);
            // the north: the hill of the salt cave (a broad plateau)
            float dHill = Vector2.Distance(new Vector2(x, z), new Vector2(-40f, 255f));
            h += 20f * (1f - Smooth(55f, 150f, dHill));
            // the west: the salt flat, dead level
            float west = Smooth(-100f, -175f, x);
            h = Mathf.Lerp(h, 3f, west);
            // the river and the lagoon carved in
            float dRiver = Mathf.Abs(z - RiverZ(x));
            float depth = NearFord(x) ? 0.6f : 2.2f;
            if (x > -275f) h -= depth * (1f - Smooth(5f, 12f, dRiver)) * Smooth(-280f, -260f, x);
            float dLagoon = Vector2.Distance(new Vector2((x + 300f) * 0.8f, z - RiverZ(-300f)), Vector2.zero);
            h -= 1.6f * (1f - Smooth(18f, 30f, dLagoon));
            return Mathf.Max(h, 0.5f);
        }

        static Terrain BuildTerrain(Transform map)
        {
            var data = AssetDatabase.LoadAssetAtPath<TerrainData>(TerrainPath);
            if (!data) { data = new TerrainData(); AssetDatabase.CreateAsset(data, TerrainPath); }
            data.heightmapResolution = Res;
            data.size = new Vector3(Size, MaxHeight, Size);
            var heights = new float[Res, Res];
            for (int iz = 0; iz < Res; iz++)
                for (int ix = 0; ix < Res; ix++)
                {
                    float x = -Size * 0.5f + Size * ix / (Res - 1f), z = -Size * 0.5f + Size * iz / (Res - 1f);
                    heights[iz, ix] = Height(x, z) / MaxHeight;
                }
            data.SetHeights(0, 0, heights);

            // painted by kind: forest floor, river banks, salt, rock
            var layers = new[]
            {
                Layer("RT_Forest", new Color(0.42f, 0.47f, 0.38f)),
                Layer("RT_Bank", new Color(0.46f, 0.4f, 0.33f)),
                Layer("RT_Salt", new Color(0.88f, 0.87f, 0.84f)),
                Layer("RT_Rock", new Color(0.45f, 0.44f, 0.43f)),
            };
            data.terrainLayers = layers;
            int a = 256;
            data.alphamapResolution = a;
            var alpha = new float[a, a, layers.Length];
            for (int iz = 0; iz < a; iz++)
                for (int ix = 0; ix < a; ix++)
                {
                    float x = -Size * 0.5f + Size * ix / (a - 1f), z = -Size * 0.5f + Size * iz / (a - 1f);
                    float salt = Smooth(-110f, -170f, x);
                    float rock = 1f - Smooth(70f, 140f, Vector2.Distance(new Vector2(x, z), new Vector2(-40f, 255f)));
                    float bank = (1f - Smooth(8f, 18f, Mathf.Abs(z - RiverZ(x)))) * (1f - salt);
                    float forest = Mathf.Max(0f, 1f - salt - rock - bank);
                    float sum = forest + bank + salt + rock + 1e-4f;
                    alpha[iz, ix, 0] = forest / sum; alpha[iz, ix, 1] = bank / sum; alpha[iz, ix, 2] = salt / sum; alpha[iz, ix, 3] = rock / sum;
                }
            data.SetAlphamaps(0, 0, alpha);
            EditorUtility.SetDirty(data);

            var go = Terrain.CreateTerrainGameObject(data);
            go.name = "Terrain";
            go.transform.SetParent(map, false);
            go.transform.position = new Vector3(-Size * 0.5f, 0f, -Size * 0.5f);
            var terrain = go.GetComponent<Terrain>();
            var matPath = MoveTestBuilder.GenDir + "/RT_TerrainMat.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (!mat) { mat = new Material(Shader.Find("Universal Render Pipeline/Terrain/Lit")); AssetDatabase.CreateAsset(mat, matPath); }
            terrain.materialTemplate = mat;
            terrain.Flush();
            return terrain;
        }

        static TerrainLayer Layer(string name, Color color)
        {
            string texPath = $"{MoveTestBuilder.GenDir}/{name}_Tex.asset", layerPath = $"{MoveTestBuilder.GenDir}/{name}.terrainlayer";
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);
            if (!tex)
            {
                tex = new Texture2D(4, 4, TextureFormat.RGBA32, false);
                AssetDatabase.CreateAsset(tex, texPath);
            }
            var px = new Color[16];
            // (the alpha is read as smoothness by the terrain shader: keep it matte)
            for (int i = 0; i < px.Length; i++) px[i] = new Color(color.r, color.g, color.b, 0f);
            tex.SetPixels(px);
            tex.Apply();
            EditorUtility.SetDirty(tex);
            var layer = AssetDatabase.LoadAssetAtPath<TerrainLayer>(layerPath);
            if (!layer) { layer = new TerrainLayer(); AssetDatabase.CreateAsset(layer, layerPath); }
            layer.diffuseTexture = tex;
            layer.tileSize = new Vector2(4f, 4f);
            layer.smoothness = 0f;
            layer.metallic = 0f;
            EditorUtility.SetDirty(layer);
            return layer;
        }

        static void Puddle(Transform parent, Material water, Vector3 at, float size)
        {
            var p = WeatherSceneSetup.Prim(parent, "Puddle", PrimitiveType.Cylinder, water, at + Vector3.up * 0.03f, new Vector3(size, 0.01f, size), 0f);
            Object.DestroyImmediate(p.GetComponent<Collider>());
            p.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            WeatherSceneSetup.Zone(parent, "Puddle_Zone", MoistureZone.Kind.Water, at + Vector3.up * 0.5f, new Vector3(size, 1.2f, size));
        }
    }
}
