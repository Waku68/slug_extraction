using System.Collections.Generic;
using SlugDesign;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SlugMove
{
    /// <summary>
    /// Builds the MoveTest scene: a gray test map (slopes, steps, narrow corridors, bumpy ground) and the
    /// main slug driven in first person. The settings asset is created once and never overwritten,
    /// so tuned values survive a rebuild.
    /// </summary>
    public static class MoveTestBuilder
    {
        const string Root = "Assets/Prototype/MoveTest";
        internal const string GenDir = Root + "/Generated";
        const string SettingsPath = Root + "/SlugMoveSettings.asset";
        const string ScenePath = "Assets/Scenes/MoveTest.unity";
        const string FontPath = "Assets/Slug/Fonts/NotoSansJP SDF.asset";

        internal static Material _floorMat, _grayMat, _darkMat;
        internal static Transform _map;

        [MenuItem("Slug/MoveTest Build Scene")]
        public static void Build()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var settings = LoadSettings();
            var scene = NewScene(new Vector2(50f, 40f));
            BuildMap();
            BuildPlayer(settings, new Vector3(0f, 0f, -6f));

            EditorSceneManager.SaveScene(scene, ScenePath);
            AddToBuildSettings(ScenePath);
            AssetDatabase.SaveAssets();
            Debug.Log("[MoveTest] scene saved: " + ScenePath);
        }

        /// <summary>The movement settings asset, created once (never overwritten, so tuned values survive).</summary>
        internal static SlugMoveSettings LoadSettings()
        {
            EnsureFolder(Root); EnsureFolder(GenDir);
            var settings = AssetDatabase.LoadAssetAtPath<SlugMoveSettings>(SettingsPath);
            if (settings == null)
            {
                settings = ScriptableObject.CreateInstance<SlugMoveSettings>();
                AssetDatabase.CreateAsset(settings, SettingsPath);
            }
            return settings;
        }

        /// <summary>The pickaxe swing settings asset, created once (never overwritten).</summary>
        internal static PickaxeSettings LoadPickaxeSettings()
        {
            const string path = Root + "/PickaxeSettings.asset";
            EnsureFolder(Root);
            var settings = AssetDatabase.LoadAssetAtPath<PickaxeSettings>(path);
            if (settings == null)
            {
                settings = ScriptableObject.CreateInstance<PickaxeSettings>();
                AssetDatabase.CreateAsset(settings, path);
            }
            return settings;
        }

        /// <summary>
        /// The settings asset of one gun (RifleSettings / ShotgunSettings / SniperSettings), created once from the kind's
        /// preset and never overwritten afterwards, so tuned values survive.
        /// </summary>
        internal static GunSettings LoadGunSettings(GunKind kind)
        {
            string path = $"{Root}/{kind}Settings.asset";
            EnsureFolder(Root);
            var settings = AssetDatabase.LoadAssetAtPath<GunSettings>(path);
            if (settings == null)
            {
                settings = ScriptableObject.CreateInstance<GunSettings>();
                settings.Preset(kind);
                AssetDatabase.CreateAsset(settings, path);
            }
            return settings;
        }

        /// <summary>An empty scene with a sun, the gray materials (floor grid tiled to <paramref name="floorTiles"/>) and a Map root.</summary>
        internal static UnityEngine.SceneManagement.Scene NewScene(Vector2 floorTiles)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            BuildMaterials(floorTiles);

            var light = new GameObject("Directional Light").AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.2f;
            light.shadows = LightShadows.Soft;
            light.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            _map = new GameObject("Map").transform;
            return scene;
        }

        internal static void AddToBuildSettings(string path)
        {
            var list = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            if (list.Exists(s => s.path == path)) return;
            list.Add(new EditorBuildSettingsScene(path, true));
            EditorBuildSettings.scenes = list.ToArray();
        }

        // ------------------------------------------------------------------ player
        /// <summary>Where the face sits in front of the capsule's axis (m). The slug turns about that axis.</summary>
        const float FaceForward = 0.122f;

        /// <summary>The main slug with the first-person controller, camera, debug HUD and the pickaxe.</summary>
        internal static GameObject BuildPlayer(SlugMoveSettings settings, Vector3 position)
        {
            var player = new GameObject("Player");
            player.transform.position = position;
            var cc = player.AddComponent<CharacterController>();
            cc.radius = 0.18f;   // the body is about 0.33 m wide; a 0.36 m capsule passes a 0.45 m corridor
            cc.height = 0.56f;
            cc.center = new Vector3(0f, 0.28f, 0f);
            cc.skinWidth = 0.03f;
            cc.minMoveDistance = 0f;
            cc.slopeLimit = settings.slopeLimit;
            cc.stepOffset = settings.stepHeight;

            // the capsule sits around the head; the body trails behind it (eyes at about +0.16 m, inside the capsule)
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(SlugGalleryV3.PrefabPath);
            var slugGo = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            slugGo.name = "Body";
            slugGo.transform.SetParent(player.transform, false);
            slugGo.transform.localPosition = new Vector3(0f, -cc.skinWidth, -0.31f);
            var slug = slugGo.GetComponent<SlugPop>();
            SlugGalleryV7.ApplyCurrent(slug);
            PrefabUtility.RecordPrefabInstancePropertyModifications(slug);
            slug.Rebuild();
            // keep the face over the front of the capsule whatever the body length (the body is centred on its footprint)
            float faceZ = player.transform.InverseTransformPoint(slug.FaceCenterWorld).z;
            slugGo.transform.localPosition += new Vector3(0f, 0f, FaceForward - faceZ);

            var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
            var cam = camGo.AddComponent<Camera>();
            cam.nearClipPlane = 0.02f;
            cam.farClipPlane = 200f;
            cam.fieldOfView = settings.fieldOfView;
            camGo.AddComponent<AudioListener>();
            camGo.transform.SetPositionAndRotation(player.transform.position + new Vector3(0f, 0.47f, 0.3f), Quaternion.identity);

            var move = player.AddComponent<SlugMoveController>();
            var rig = player.AddComponent<SlugCameraRig>();
            var hud = player.AddComponent<SlugMoveHud>();
            var tool = player.AddComponent<SlugToolHolder>();
            move.settings = settings; move.cameraRig = rig; move.body = slugGo.transform;
            rig.settings = settings; rig.controller = move; rig.slug = slug; rig.cam = cam;
            hud.controller = move; hud.cameraRig = rig;
            tool.pickaxe = LoadPickaxeSettings(); tool.controller = move; tool.cameraRig = rig; tool.slug = slug;
            // the guns: 1 = pickaxe, 2 / 3 / 4 (or the wheel) = rifle, shotgun, sniper; only the one in hand is enabled
            var kinds = new[] { GunKind.Rifle, GunKind.Shotgun, GunKind.Sniper };
            var guns = new SlugGunHolder[kinds.Length];
            for (int i = 0; i < kinds.Length; i++)
            {
                var gun = guns[i] = player.AddComponent<SlugGunHolder>();
                gun.gun = LoadGunSettings(kinds[i]); gun.controller = move; gun.cameraRig = rig; gun.slug = slug;
                gun.enabled = false;
            }
            var loadout = player.AddComponent<SlugLoadout>();
            loadout.pickaxe = tool; loadout.guns = guns;
            // what a shot can hit, part by part (the movement capsule is not hit)
            player.AddComponent<SlugHitboxes>().slug = slug;
            return player;
        }

        // ------------------------------------------------------------------ map
        static void BuildMap()
        {
            // floor with a 1 m grid so the speed can be read by eye
            var floor = Box("Floor", new Vector3(3f, -0.05f, 6f), new Vector3(50f, 0.1f, 40f), _floorMat);
            floor.GetComponent<Renderer>().sharedMaterial = _floorMat;

            // slopes (each: up ramp, flat top, down ramp). The default limit is 60 degrees.
            Hill("Slope_15", -9f, 15f, 3.0f);
            Hill("Slope_45", -5.5f, 45f, 1.2f);
            Steep("Slope_70", -2f, 70f, 0.5f);
            Label("緩い坂 15°", new Vector3(-9f, 1.6f, -2.3f));
            Label("急な坂 45°", new Vector3(-5.5f, 1.9f, -2.3f));
            Label("急すぎる坂 70°\n（登れない）", new Vector3(-2f, 2.0f, -2.3f));

            // steps of increasing height: up to 0.15 m they are stepped over, up to 0.45 m (head height) climbed.
            // The gaps between them are a body length (1.1 m) plus a little, so the slug can crawl through.
            float[] heights = { 0.05f, 0.10f, 0.20f, 0.30f, 0.45f, 0.60f };
            for (int i = 0; i < heights.Length; i++)
            {
                float x = 1.5f + i * 2.5f, h = heights[i];
                Box($"Step_{h:0.00}", new Vector3(x, h * 0.5f, -1.25f), new Vector3(1.3f, h, 1.5f), _grayMat);
                Label($"段差 {h * 100f:0} cm", new Vector3(x, h + 0.7f, -2.3f), 2f);
            }

            // narrow corridors: 0.45 m (fits) and 0.3 m (narrower than the body)
            Corridor("Corridor_45", 18.5f, 0.45f);
            Corridor("Corridor_30", 20.5f, 0.3f);
            Label("通路 45 cm", new Vector3(18.5f, 1.3f, -2.3f), 2f);
            Label("通路 30 cm", new Vector3(20.5f, 1.3f, -2.3f), 2f);

            // bumpy ground
            var bumps = new GameObject("Bumpy");
            bumps.transform.SetParent(_map, false);
            bumps.transform.position = new Vector3(0f, 0f, 3f);
            var mesh = BumpMesh(8f, 7f, 0.1f, 0.12f);
            bumps.AddComponent<MeshFilter>().sharedMesh = mesh;
            bumps.AddComponent<MeshRenderer>().sharedMaterial = _grayMat;
            bumps.AddComponent<MeshCollider>().sharedMesh = mesh;
            Label("凸凹地面", new Vector3(4f, 0.9f, 2.7f), 2f);
        }

        /// <summary>Up ramp along +z from (x, z0), a flat top, and a down ramp. Returns the top height.</summary>
        internal static float Hill(string name, float x, float deg, float rampLen, float z0 = -2f, float width = 2.4f, float top = 1.5f)
        {
            float h = rampLen * Mathf.Tan(deg * Mathf.Deg2Rad);
            var parent = new GameObject(name).transform;
            parent.SetParent(_map, false);
            Ramp(parent, $"{name}_Up", new Vector3(x, 0f, z0), width, rampLen, h, false);
            Box($"{name}_Top", new Vector3(x, h * 0.5f, z0 + rampLen + top * 0.5f), new Vector3(width, h, top), _grayMat, parent);
            Ramp(parent, $"{name}_Down", new Vector3(x, 0f, z0 + rampLen + top), width, rampLen, h, true);
            return h;
        }

        static void Steep(string name, float x, float deg, float rampLen)
        {
            float h = rampLen * Mathf.Tan(deg * Mathf.Deg2Rad);
            const float width = 2.4f, top = 1.2f, z0 = -2f;
            var parent = new GameObject(name).transform;
            parent.SetParent(_map, false);
            Ramp(parent, $"{name}_Up", new Vector3(x, 0f, z0), width, rampLen, h, false);
            Box($"{name}_Top", new Vector3(x, h * 0.5f, z0 + rampLen + top * 0.5f), new Vector3(width, h, top), _darkMat, parent);
        }

        static void Corridor(string name, float x, float gap)
        {
            const float len = 4f, hgt = 0.8f, thick = 0.2f;
            var parent = new GameObject(name).transform;
            parent.SetParent(_map, false);
            foreach (float side in new[] { -1f, 1f })
                Box($"{name}_Wall", new Vector3(x + side * (gap + thick) * 0.5f, hgt * 0.5f, -2f + len * 0.5f), new Vector3(thick, hgt, len), _darkMat, parent);
        }

        internal static GameObject Box(string name, Vector3 pos, Vector3 size, Material mat, Transform parent = null)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent ? parent : _map, false);
            go.transform.position = pos;
            go.transform.localScale = size;
            go.GetComponent<Renderer>().sharedMaterial = mat;
            return go;
        }

        /// <summary>A wedge going up along +z from <paramref name="start"/> (or down along +z when <paramref name="descending"/>).</summary>
        internal static void Ramp(Transform parent, string name, Vector3 start, float width, float len, float h, bool descending)
        {
            var mesh = WedgeMesh(width, len, h, descending);
            mesh.name = name;
            SaveMesh(mesh, name);
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = start;
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = _grayMat;
            go.AddComponent<MeshCollider>().sharedMesh = mesh;
        }

        static Mesh WedgeMesh(float w, float len, float h, bool descending)
        {
            float x0 = -w * 0.5f, x1 = w * 0.5f;
            float hb = descending ? h : 0f, hf = descending ? 0f : h;   // height at the back (z=0) / front (z=len)
            var v = new List<Vector3>();
            var tri = new List<int>();
            void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d)       // clockwise seen from outside
            {
                int i = v.Count; v.Add(a); v.Add(b); v.Add(c); v.Add(d);
                tri.AddRange(new[] { i, i + 1, i + 2, i, i + 2, i + 3 });
            }
            void Tri(Vector3 a, Vector3 b, Vector3 c)
            {
                int i = v.Count; v.Add(a); v.Add(b); v.Add(c);
                tri.AddRange(new[] { i, i + 1, i + 2 });
            }
            // slope (top)
            Quad(new Vector3(x0, hb, 0f), new Vector3(x0, hf, len), new Vector3(x1, hf, len), new Vector3(x1, hb, 0f));
            // bottom
            Quad(new Vector3(x0, 0f, 0f), new Vector3(x1, 0f, 0f), new Vector3(x1, 0f, len), new Vector3(x0, 0f, len));
            // the tall end
            if (descending) Quad(new Vector3(x0, 0f, 0f), new Vector3(x0, h, 0f), new Vector3(x1, h, 0f), new Vector3(x1, 0f, 0f));
            else Quad(new Vector3(x1, 0f, len), new Vector3(x1, h, len), new Vector3(x0, h, len), new Vector3(x0, 0f, len));
            // sides
            Tri(new Vector3(x0, 0f, 0f), new Vector3(x0, 0f, len), new Vector3(x0, descending ? h : h, descending ? 0f : len));
            Tri(new Vector3(x1, 0f, 0f), new Vector3(x1, descending ? h : h, descending ? 0f : len), new Vector3(x1, 0f, len));
            var m = new Mesh();
            m.SetVertices(v);
            m.SetTriangles(tri, 0);
            m.RecalculateNormals();
            m.RecalculateBounds();
            return m;
        }

        /// <summary>Gentle noise bumps that fade to zero at the edges, so the patch blends into the floor.</summary>
        internal static Mesh BumpMesh(float sizeX, float sizeZ, float cell, float amp, string name = "Bumpy", float seed = 0f)
        {
            int nx = Mathf.RoundToInt(sizeX / cell), nz = Mathf.RoundToInt(sizeZ / cell);
            var v = new Vector3[(nx + 1) * (nz + 1)];
            var uv = new Vector2[v.Length];
            for (int j = 0; j <= nz; j++)
            for (int i = 0; i <= nx; i++)
            {
                float x = i * cell, z = j * cell;
                float n = Mathf.PerlinNoise(x * 0.9f + 13.1f + seed, z * 0.9f + 7.7f) * 0.7f + Mathf.PerlinNoise(x * 2.3f + 3.3f, z * 2.3f + 1.9f + seed) * 0.3f;
                float edge = Mathf.SmoothStep(0f, 1f, Mathf.Min(Mathf.Min(x, sizeX - x), Mathf.Min(z, sizeZ - z)) / 0.8f);
                v[j * (nx + 1) + i] = new Vector3(x - sizeX * 0.5f, n * amp * edge, z);
                uv[j * (nx + 1) + i] = new Vector2(x, z);
            }
            var t = new int[nx * nz * 6];
            int k = 0;
            for (int j = 0; j < nz; j++)
            for (int i = 0; i < nx; i++)
            {
                int a = j * (nx + 1) + i, b = a + 1, c = a + nx + 1, d = c + 1;
                t[k++] = a; t[k++] = c; t[k++] = b;
                t[k++] = b; t[k++] = c; t[k++] = d;
            }
            var m = new Mesh { name = name, indexFormat = UnityEngine.Rendering.IndexFormat.UInt32, vertices = v, uv = uv, triangles = t };
            m.RecalculateNormals();
            m.RecalculateBounds();
            SaveMesh(m, name);
            return m;
        }

        internal static void Label(string text, Vector3 pos, float size = 2.4f)
        {
            var go = new GameObject("Label_" + text.Split('\n')[0]);
            go.transform.SetParent(_map, false);
            go.transform.position = pos;
            var tmp = go.AddComponent<TextMeshPro>();
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
            if (font) tmp.font = font;
            tmp.text = text;
            tmp.fontSize = size;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = new Color(0.15f, 0.15f, 0.15f);
            tmp.rectTransform.sizeDelta = new Vector2(3f, 1f);
        }

        // ------------------------------------------------------------------ assets
        static void BuildMaterials(Vector2 floorTiles)
        {
            var lit = Shader.Find("Universal Render Pipeline/Lit");
            _grayMat = SaveMaterial("Gray", lit, new Color(0.62f, 0.62f, 0.62f));
            _darkMat = SaveMaterial("DarkGray", lit, new Color(0.42f, 0.42f, 0.42f));
            _floorMat = SaveMaterial($"FloorGrid_{floorTiles.x:0}x{floorTiles.y:0}", lit, Color.white);
            _floorMat.SetTexture("_BaseMap", GridTexture());
            _floorMat.SetTextureScale("_BaseMap", floorTiles);   // one tile per metre
            EditorUtility.SetDirty(_floorMat);
        }

        internal static Material SaveMaterial(string name, Shader shader, Color color)
        {
            string path = $"{GenDir}/{name}.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null) { m = new Material(shader); AssetDatabase.CreateAsset(m, path); }
            m.shader = shader;
            m.SetColor("_BaseColor", color);
            m.SetFloat("_Smoothness", 0.1f);
            EditorUtility.SetDirty(m);
            return m;
        }

        /// <summary>Gray tile with a darker line on its edges: one tile = 1 m on the floor.</summary>
        static Texture2D GridTexture()
        {
            string path = $"{GenDir}/Grid.png";
            const int N = 128;
            var tex = new Texture2D(N, N, TextureFormat.RGB24, true);
            var c0 = new Color(0.58f, 0.58f, 0.58f);
            var c1 = new Color(0.46f, 0.46f, 0.46f);
            for (int y = 0; y < N; y++)
            for (int x = 0; x < N; x++)
                tex.SetPixel(x, y, x < 2 || y < 2 ? c1 : c0);
            System.IO.File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path);
            var imp = (TextureImporter)AssetImporter.GetAtPath(path);
            imp.wrapMode = TextureWrapMode.Repeat;
            imp.anisoLevel = 8;
            imp.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        static void SaveMesh(Mesh mesh, string name)
        {
            string path = $"{GenDir}/{name}.asset";
            if (AssetDatabase.LoadAssetAtPath<Mesh>(path) != null) AssetDatabase.DeleteAsset(path);
            AssetDatabase.CreateAsset(mesh, path);
        }

        internal static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            int i = path.LastIndexOf('/');
            EnsureFolder(path.Substring(0, i));
            AssetDatabase.CreateFolder(path.Substring(0, i), path.Substring(i + 1));
        }
    }
}
