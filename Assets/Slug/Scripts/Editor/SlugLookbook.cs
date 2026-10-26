using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace SlugDesign
{
    /// <summary>
    /// The slug as it is now, in one place: the prefab every scene uses (<see cref="PrefabPath"/>), the decided look
    /// (<see cref="ApplyCurrent"/>), and a dark rainy scene with screenshots (whole body, face close-ups, the crawl in
    /// frozen stills) to check it. The earlier comparison galleries (v1 ... v7) were folded into this; their history is
    /// in docs/character/.
    /// </summary>
    public static class SlugLookbook
    {
        public const string PrefabPath = "Assets/Slug/Prefabs/Slug_Pop.prefab";
        public const string Dir = "Assets/Slug/Lookbook";
        public const string ScenePath = "Assets/Scenes/Preview/SlugLookbook.unity";
        const string ShaderName = "Slug/Toon";
        const float FrozenTime = 0.0f;

        static string OutDir => Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Screenshots", "Character"));
        static ParticleSystem _rain;

        /// <summary>The slug as it is now (decided values; the rest are the current values).</summary>
        public static void ApplyCurrent(SlugPop s)
        {
            s.SetAll(1f);
            s.bodyStyle = SlugBodyStyle.V4Round;   // body and face back to the v4 middle (0.5) slug
            s.body = 1f;
            s.pattern = 0.5f;
            s.color = 1f;               // moss
            s.eyes = 0.25f;
            s.eyeKind = SlugEyeKind.None;   // the flat painted face
            s.tentacleLength = 1f;
            s.tentacleThickness = 0.5f;
            s.tentacleDirection = 0.5f; // straight up, with a gentle S and a soft sway
            s.expression = 0.5f;
            s.hands = 1f;               // short cone
            s.handT = 0.8f;             // up on the lifted chest, to hold a gun / axe / pickaxe in front of it
            s.handRaise = 0f;
            s.uprightPivotT = 0.65f;    // on slopes the neck and head stand up over the chest
            s.handReach = 1f;           // a little longer, toward the front of the chest: holds a tool in both hands
            s.feet = true;              // the same cones low on the sides, where the belly meets the ground
            s.footT = 0.3f;
            s.bellyLength = 0.55f;      // a shorter belly behind the lifted chest (the front is unchanged)
            s.soleSquare = 0.75f;       // the belly sits flat on the ground with a clear edge
            s.footFrill = 0f;           // (the rippling, pleated foot fringe was tried and turned off)
            s.shading = 1f;
            s.outline = 1f;
        }

        // ------------------------------------------------------------------ prefab
        [MenuItem("Slug/Lookbook Build Prefab")]
        public static void BuildPrefab()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Slug/Prefabs")) AssetDatabase.CreateFolder("Assets/Slug", "Prefabs");
            var go = new GameObject("Slug_Pop");
            var c = go.AddComponent<SlugPop>();
            c.shader = Shader.Find(ShaderName);
            PrefabUtility.SaveAsPrefabAsset(go, PrefabPath);
            Object.DestroyImmediate(go);
            Debug.Log("[Slug] prefab saved: " + PrefabPath);
        }

        // ------------------------------------------------------------------ scene
        [MenuItem("Slug/Lookbook Build Scene")]
        public static void BuildScene()
        {
            if (!AssetDatabase.IsValidFolder(Dir)) AssetDatabase.CreateFolder("Assets/Slug", "Lookbook");
            if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) == null) BuildPrefab();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            BuildEnvironment(out var gallery, out _rain);

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            var inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            inst.name = "Slug";
            inst.transform.SetParent(gallery, false);
            inst.transform.rotation = Quaternion.Euler(0f, 145f, 0f);
            var s = inst.GetComponent<SlugPop>();
            ApplyCurrent(s);
            s.Rebuild();

            var cam = GameObject.Find("Overview Camera").GetComponent<Camera>();
            cam.fieldOfView = 20f;
            Frame(cam, BoundsOf(inst), 16f, 1.2f);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            Debug.Log("[Slug] lookbook scene saved: " + ScenePath);
        }

        /// <summary>Dark rainy set: cold overcast light, ground, rain streaks (pre-simulated before every shot), camera.</summary>
        static void BuildEnvironment(out Transform gallery, out ParticleSystem rain)
        {
            RenderSettings.skybox = null;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.42f, 0.5f, 0.6f);
            RenderSettings.ambientEquatorColor = new Color(0.34f, 0.4f, 0.47f);
            RenderSettings.ambientGroundColor = new Color(0.2f, 0.23f, 0.26f);

            var lightGo = new GameObject("Directional Light");
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(0.78f, 0.87f, 1f);
            light.intensity = 0.95f;
            light.shadows = LightShadows.Soft;
            light.shadowStrength = 0.6f;
            lightGo.transform.rotation = Quaternion.Euler(52f, -28f, 0f);

            var groundMat = new Material(Shader.Find(ShaderName)) { name = "RainGround" };
            groundMat.SetColor("_BaseColor", new Color(0.3f, 0.35f, 0.35f));
            groundMat.SetColor("_ShadeColor", new Color(0.5f, 0.55f, 0.65f));
            groundMat.SetFloat("_OutlineWidth", 0f);
            groundMat.SetFloat("_WaveAmp", 0f);
            groundMat.SetFloat("_RimAmount", 0f);
            groundMat.SetFloat("_Bands", 2f);
            AssetDatabase.CreateAsset(groundMat, Dir + "/RainGround.mat");
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            Object.DestroyImmediate(ground.GetComponent<Collider>());
            ground.transform.localScale = new Vector3(60f, 1f, 30f);
            ground.GetComponent<MeshRenderer>().sharedMaterial = groundMat;

            var rainGo = new GameObject("Rain");
            rainGo.transform.position = new Vector3(0f, 4.2f, 1.6f);
            var ps = rainGo.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.duration = 1f; main.loop = true; main.playOnAwake = false;
            main.startLifetime = 0.75f; main.startSpeed = 0f;
            main.startSize = 0.0035f;
            main.startColor = new Color(0.8f, 0.88f, 1f, 0.16f);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 8000;
            var em = ps.emission; em.rateOverTime = 900f;
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Box; sh.scale = new Vector3(14f, 0.2f, 6f);
            var vel = ps.velocityOverLifetime; vel.enabled = true; vel.space = ParticleSystemSimulationSpace.World;
            vel.x = 0.8f; vel.y = -8f; vel.z = 0f;
            ps.useAutoRandomSeed = false; ps.randomSeed = 11;
            var pr = rainGo.GetComponent<ParticleSystemRenderer>();
            pr.renderMode = ParticleSystemRenderMode.Stretch;
            pr.velocityScale = 0.03f; pr.lengthScale = 1f; pr.cameraVelocityScale = 0f;
            var rainMat = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit")) { name = "Rain" };
            rainMat.SetFloat("_Surface", 1f);
            rainMat.SetFloat("_Blend", 0f);
            rainMat.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            rainMat.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            rainMat.SetFloat("_ZWrite", 0f);
            rainMat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            rainMat.SetOverrideTag("RenderType", "Transparent");
            rainMat.renderQueue = (int)RenderQueue.Transparent;
            AssetDatabase.CreateAsset(rainMat, Dir + "/Rain.mat");
            pr.sharedMaterial = rainMat;
            rain = ps;

            var camGo = new GameObject("Overview Camera");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.1f, 0.13f, 0.17f);
            camGo.AddComponent<UniversalAdditionalCameraData>();

            gallery = new GameObject("Gallery").transform;
        }

        // ------------------------------------------------------------------ screenshots
        [MenuItem("Slug/Lookbook Capture Screenshots")]
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
                cam = MakeCamera(overview);
                cam.fieldOfView = 20f;
                cam.aspect = W / (float)H;

                // ---- overall views
                var views = new (string name, float yaw, float elev)[] { ("front", 180f, 6f), ("side", 90f, 3f), ("angle", 145f, 16f) };
                foreach (var v in views)
                {
                    s.transform.rotation = Quaternion.Euler(0f, v.yaw, 0f);
                    Frame(cam, BoundsOf(s.gameObject), v.elev, 1.15f);
                    _rain.Simulate(2f, true, true, true);
                    Save(cam, Path.Combine(OutDir, $"view_{v.name}.png"), W, H);
                }
                s.transform.rotation = Quaternion.Euler(0f, 145f, 0f);
                Frame(cam, BoundsOf(s.gameObject), 16f, 1.5f);
                _rain.Simulate(2f, true, true, true);
                Save(cam, Path.Combine(OutDir, "00_overview.png"), W, H);

                // ---- face close-ups: front and two three-quarter views
                cam.fieldOfView = 16f;
                cam.nearClipPlane = 0.05f;
                _rain.Clear();
                foreach (var f in new[] { ("front", 180f), ("left34", 150f), ("right34", 210f) })
                {
                    s.transform.rotation = Quaternion.Euler(0f, f.Item2, 0f);
                    AimAtFace(cam, s, 0.62f);
                    Save(cam, Path.Combine(OutDir, "face", $"closeup_{f.Item1}.png"), W, H);
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
                    if (a.kind == "body") Frame(cam, BoundsOf(s.gameObject), a.elev, 1.45f);
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
                        Save(cam, files[k], W, H);
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
                            Save(cam, eight[k], W / 2, H / 2);
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
            Debug.Log("[Slug] lookbook screenshots written to " + OutDir);
        }

        // ------------------------------------------------------------------ helpers
        static void Save(Camera cam, string path, int w, int h)
        {
            var rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB) { antiAliasing = 4 };
            cam.targetTexture = rt;
            cam.Render();
            var prev = RenderTexture.active;
            RenderTexture.active = rt;
            var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
            tex.Apply();
            RenderTexture.active = prev;
            cam.targetTexture = null;
            cam.ResetAspect();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            rt.Release();
            Object.DestroyImmediate(rt);
        }

        static Bounds BoundsOf(GameObject go)
        {
            // Renderer.bounds can be stale right after activating / rotating, so rebuild from mesh corners.
            bool first = true;
            Bounds b = default;
            foreach (var mf in go.GetComponentsInChildren<MeshFilter>())
            {
                if (!mf.gameObject.activeInHierarchy || mf.sharedMesh == null) continue;
                var lb = mf.sharedMesh.bounds;
                var m = mf.transform.localToWorldMatrix;
                for (int i = 0; i < 8; i++)
                {
                    var c = lb.center + Vector3.Scale(lb.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                    var w = m.MultiplyPoint3x4(c);
                    if (first) { b = new Bounds(w, Vector3.zero); first = false; } else b.Encapsulate(w);
                }
            }
            return b;
        }

        static void Frame(Camera cam, Bounds b, float elevDeg, float margin = 1.25f)
        {
            float half = Mathf.Tan(cam.fieldOfView * Mathf.Deg2Rad * 0.5f);
            float dist = Mathf.Max(b.extents.y / half, b.extents.x / (half * cam.aspect)) * margin + b.extents.z;
            float e = elevDeg * Mathf.Deg2Rad;
            cam.transform.position = b.center + new Vector3(0f, Mathf.Sin(e), -Mathf.Cos(e)) * dist;
            cam.transform.LookAt(b.center);
        }

        static Camera MakeCamera(Camera like)
        {
            var go = new GameObject("Capture Camera");
            var cam = go.AddComponent<Camera>();
            cam.CopyFrom(like);
            cam.fieldOfView = 24f;
            cam.nearClipPlane = 0.05f;
            go.AddComponent<UniversalAdditionalCameraData>();
            return cam;
        }

        static void AimAtFace(Camera cam, SlugPop s, float dist)
        {
            Vector3 c = s.FaceCenterWorld;
            cam.transform.position = c + s.FaceForwardWorld * dist + s.FaceUpWorld * dist * 0.05f;
            cam.transform.LookAt(c);
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
    }
}
