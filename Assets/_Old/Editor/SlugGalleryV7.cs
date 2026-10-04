using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SlugDesign
{
    /// <summary>
    /// v7: a gorilla-like flat face (small round eyes, lighter muzzle with nostrils, straight mouth), a shorter body with the
    /// head lifted more, tentacles with a gentle S and sway, and a crawl that stretches / shortens the whole body
    /// without ever turning the tail inside out.
    /// </summary>
    public static class SlugGalleryV7
    {
        public const string Dir = "Assets/Slug/GeneratedV7";
        public const string ScenePath = "Assets/Scenes/SlugGalleryV7.unity";
        const float FrozenTime = 0.0f;

        /// <summary>The slug as it is now (decided values; the rest are the current values).</summary>
        public static void ApplyCurrent(SlugPop s)
        {
            s.SetAll(1f);
            s.bodyStyle = SlugBodyStyle.V4Round;   // body and face back to the v4 middle (0.5) slug
            s.body = 1f;
            s.pattern = 0.5f;           // unchanged
            s.color = 1f;               // moss
            s.eyes = 0.25f;             // v4: eye 0.25
            s.eyeKind = SlugEyeKind.None;   // the flat painted face
            s.tentacleLength = 1f;      // unchanged
            s.tentacleThickness = 0.5f; // back to the v4 value (0.5)
            s.tentacleDirection = 0.5f; // unchanged: straight up (now with a gentle S and a soft sway)
            s.expression = 0.5f;       // face: back to the v4 value (0.5)
            s.hands = 1f;               // short cone, unchanged
            s.handT = 0.8f;           // up on the lifted chest, to hold a gun / axe / pickaxe in front of it
            s.handRaise = 0f;
            s.uprightPivotT = 0.65f;    // on slopes the neck and head stand up over the chest
            s.handReach = 1f;          // a little longer, toward the front of the chest: holds a tool in both hands
            s.feet = true;              // the same cones low on the sides, where the belly meets the ground
            s.footT = 0.3f;
            s.bellyLength = 0.55f;      // a shorter belly behind the lifted chest (the front is unchanged)
            s.soleSquare = 0.75f;       // the belly sits flat on the ground with a clear edge
            s.footFrill = 0f;           // (the rippling, pleated foot fringe was tried and turned off)
            s.shading = 1f;             // unchanged
            s.outline = 1f;             // unchanged
        }

        static string OutDir => Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Screenshots", "v7"));
        static ParticleSystem _rain;

        [MenuItem("Slug/V7 Build Scene")]
        public static void BuildScene()
        {
            if (!AssetDatabase.IsValidFolder(Dir)) AssetDatabase.CreateFolder("Assets/Slug", "GeneratedV7");
            if (AssetDatabase.LoadAssetAtPath<GameObject>(SlugGalleryV3.PrefabPath) == null) SlugGalleryV3.BuildPrefab();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            SlugGalleryV2.BuildEnvironment(Dir, out var gallery, out _rain);
            var sh = _rain.shape; sh.scale = new Vector3(14f, 0.2f, 6f);
            var em = _rain.emission; em.rateOverTime = 900f;

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(SlugGalleryV3.PrefabPath);
            var inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            inst.name = "Slug";
            inst.transform.SetParent(gallery, false);
            inst.transform.rotation = Quaternion.Euler(0f, 145f, 0f);
            var s = inst.GetComponent<SlugPop>();
            ApplyCurrent(s);
            s.Rebuild();

            var cam = GameObject.Find("Overview Camera").GetComponent<Camera>();
            cam.fieldOfView = 20f;
            SlugScreenshots.Frame(cam, SlugScreenshots.BoundsOf(inst), 16f, 1.2f);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            Debug.Log("[Slug v7] scene saved: " + ScenePath);
        }

        /// <summary>Tiles stills into one image (cols x rows).</summary>
        static void Tile(string[] files, int cols, string output, int w, int h)
        {
            int rows = Mathf.CeilToInt(files.Length / (float)cols);
            var sheet = new Texture2D(w * cols, h * rows, TextureFormat.RGB24, false);
            for (int i = 0; i < files.Length; i++)
            {
                var t = new Texture2D(2, 2);
                t.LoadImage(File.ReadAllBytes(files[i]));
                sheet.SetPixels((i % cols) * w, (rows - 1 - i / cols) * h, w, h, t.GetPixels());
                Object.DestroyImmediate(t);
            }
            File.WriteAllBytes(output, sheet.EncodeToPNG());
            Object.DestroyImmediate(sheet);
        }

        static void AimAtFace(Camera cam, SlugPop s, float dist)
        {
            Vector3 c = s.FaceCenterWorld;
            cam.transform.position = c + s.FaceForwardWorld * dist + s.FaceUpWorld * dist * 0.05f;
            cam.transform.LookAt(c);
        }

        [MenuItem("Slug/V7 Capture Screenshots")]
        public static void Capture()
        {
            Directory.CreateDirectory(OutDir);
            foreach (var d in new[] { "face", "wave" }) Directory.CreateDirectory(Path.Combine(OutDir, d));
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var gallery = GameObject.Find("Gallery").transform;
            _rain = GameObject.Find("Rain").GetComponent<ParticleSystem>();
            var overview = GameObject.Find("Overview Camera").GetComponent<Camera>();
            var s = gallery.GetComponentInChildren<SlugPop>();
            s.Rebuild();

            const int W = 1280, H = 960;
            Shader.SetGlobalFloat("_SlugFreeze", 1f);
            Shader.SetGlobalFloat("_SlugTimeValue", FrozenTime);
            Camera cam = null;
            try
            {
                cam = SlugScreenshots.MakeCamera(overview);
                cam.fieldOfView = 20f;
                cam.aspect = W / (float)H;

                // ---- overall views
                var views = new (string name, float yaw, float elev)[] { ("front", 180f, 6f), ("side", 90f, 3f), ("angle", 145f, 16f) };
                foreach (var v in views)
                {
                    s.transform.rotation = Quaternion.Euler(0f, v.yaw, 0f);
                    SlugScreenshots.Frame(cam, SlugScreenshots.BoundsOf(s.gameObject), v.elev, 1.15f);
                    _rain.Simulate(2f, true, true, true);
                    SlugScreenshots.Save(cam, Path.Combine(OutDir, $"view_{v.name}.png"), W, H);
                }
                s.transform.rotation = Quaternion.Euler(0f, 145f, 0f);
                SlugScreenshots.Frame(cam, SlugScreenshots.BoundsOf(s.gameObject), 16f, 1.5f);
                _rain.Simulate(2f, true, true, true);
                SlugScreenshots.Save(cam, Path.Combine(OutDir, "00_overview.png"), W, H);

                // ---- face close-ups: front and two three-quarter views
                cam.fieldOfView = 16f;
                cam.nearClipPlane = 0.05f;
                _rain.Clear();
                foreach (var f in new[] { ("front", 180f), ("left34", 150f), ("right34", 210f) })
                {
                    s.transform.rotation = Quaternion.Euler(0f, f.Item2, 0f);
                    AimAtFace(cam, s, 0.62f);
                    SlugScreenshots.Save(cam, Path.Combine(OutDir, "face", $"closeup_{f.Item1}.png"), W, H);
                }

                // ---- crawl: four stills a quarter of a period apart. The length breathes with sin(2 pi speed t):
                //      the first still is the most stretched moment, the third the most shortened one.
                float period = 1f / 0.8f;
                float[] times = { period * 0.25f, period * 0.5f, period * 0.75f, period * 1.0f };
                var angles = new (string name, float yaw, float elev, string kind)[]
                {
                    ("side", 90f, 3f, "body"), ("angle", 145f, 16f, "body"), ("face", 180f, 6f, "face"), ("tail", 90f, 4f, "tail"),
                };
                foreach (var a in angles)
                {
                    s.transform.rotation = Quaternion.Euler(0f, a.yaw, 0f);
                    cam.fieldOfView = a.kind == "body" ? 20f : 16f;
                    Shader.SetGlobalFloat("_SlugTimeValue", 0f);
                    // the camera is placed once (at rest) and then kept fixed for the four stills
                    if (a.kind == "body") SlugScreenshots.Frame(cam, SlugScreenshots.BoundsOf(s.gameObject), a.elev, 1.45f);
                    else if (a.kind == "face") AimAtFace(cam, s, 0.62f);
                    else
                    {
                        Vector3 tail = s.TailWorld;
                        Vector3 look = tail + s.transform.forward * 0.12f + Vector3.up * 0.05f;
                        cam.transform.position = look + new Vector3(0f, 0.06f, -0.75f);
                        cam.transform.LookAt(look);
                    }
                    var files = new string[4];
                    for (int k = 0; k < 4; k++)
                    {
                        Shader.SetGlobalFloat("_SlugTimeValue", times[k]);
                        files[k] = Path.Combine(OutDir, "wave", $"frame{k}_{a.name}.png");
                        SlugScreenshots.Save(cam, files[k], W, H);
                    }
                    Tile(files, 2, Path.Combine(OutDir, "wave", $"wave_4frames_{a.name}.png"), W, H);

                    if (a.kind == "tail")
                    {
                        // the whole period in 8 steps: the tail must never flip or swell
                        var eight = new string[8];
                        for (int k = 0; k < 8; k++)
                        {
                            Shader.SetGlobalFloat("_SlugTimeValue", period * (k + 1) / 8f);
                            eight[k] = Path.Combine(OutDir, "wave", $"tail8_{k}.png");
                            SlugScreenshots.Save(cam, eight[k], W / 2, H / 2);
                        }
                        Tile(eight, 4, Path.Combine(OutDir, "wave", "tail_8frames.png"), W / 2, H / 2);
                        foreach (var f in eight) File.Delete(f);
                    }
                }
                Shader.SetGlobalFloat("_SlugTimeValue", FrozenTime);
                s.transform.rotation = Quaternion.Euler(0f, 145f, 0f);
            }
            finally
            {
                Shader.SetGlobalFloat("_SlugFreeze", 0f);
                if (cam != null) Object.DestroyImmediate(cam.gameObject);
            }
            EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log("[Slug v7] screenshots written to " + OutDir);
        }
    }
}
