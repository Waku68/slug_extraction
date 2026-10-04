using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace SlugDesign
{
    /// <summary>
    /// v2: same shapes, calmer colours and eyes, shown against a dark rainy background.
    /// Looks = 3 camouflage palettes (moss / earth / grey) x eye styles (subdued / dot / sleepy).
    /// </summary>
    public static class SlugGalleryV2
    {
        public const string Dir = "Assets/Slug/GeneratedV2";
        public const string ScenePath = "Assets/Scenes/SlugGalleryV2.unity";
        const float FrozenTime = 1.3f;
        static readonly Color LabelColor = new Color(0.86f, 0.9f, 0.92f);

        static string OutDir => Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Screenshots", "v2"));
        static string PrefabPath(string look, string id) => $"{Dir}/{look}/Slug_{id}.prefab";

        // ------------------------------------------------------------------ variants (prefabs)
        [MenuItem("Slug/V2 Build Variants")]
        public static void BuildVariants()
        {
            if (AssetDatabase.IsValidFolder(Dir)) AssetDatabase.DeleteAsset(Dir);
            AssetDatabase.CreateFolder("Assets/Slug", "GeneratedV2");
            AssetDatabase.CreateFolder(Dir, "Common");
            var sphere = SlugMeshFactory.Sphere("Sphere", 1f, 14, 11);
            AssetDatabase.CreateAsset(sphere, Dir + "/Common/Sphere.asset");

            foreach (var look in SlugLook.All())
            {
                AssetDatabase.CreateFolder(Dir, look.key);
                foreach (var spec in SlugDesigns.All())
                {
                    var go = SlugBuilder.Build(spec, sphere, $"{Dir}/{look.key}/{spec.id}", look);
                    PrefabUtility.SaveAsPrefabAsset(go, PrefabPath(look.key, spec.id));
                    Object.DestroyImmediate(go);
                }
            }
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[Slug v2] built {SlugLook.All().Count} looks x {SlugDesigns.All().Count} designs");
        }

        // ------------------------------------------------------------------ environment (dark rain)
        static Transform _gallery;
        static ParticleSystem _rain;

        static void Environment() { BuildEnvironment(Dir, out _gallery, out _rain); }

        /// <summary>Dark rainy set (light, ground, rain, camera). Shared with the v3 comparison.</summary>
        internal static void BuildEnvironment(string dir, out Transform gallery, out ParticleSystem rain)
        {
            RenderSettings.skybox = null;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.42f, 0.5f, 0.6f);
            RenderSettings.ambientEquatorColor = new Color(0.34f, 0.4f, 0.47f);
            RenderSettings.ambientGroundColor = new Color(0.2f, 0.23f, 0.26f);

            var lightGo = new GameObject("Directional Light");
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(0.78f, 0.87f, 1f);      // cold overcast light
            light.intensity = 0.95f;
            light.shadows = LightShadows.Soft;
            light.shadowStrength = 0.6f;
            lightGo.transform.rotation = Quaternion.Euler(52f, -28f, 0f);

            var shader = Shader.Find(SlugBuilder.ShaderName);
            var groundMat = new Material(shader) { name = "RainGround" };
            groundMat.SetColor("_BaseColor", new Color(0.3f, 0.35f, 0.35f));
            groundMat.SetColor("_ShadeColor", new Color(0.5f, 0.55f, 0.65f));
            groundMat.SetFloat("_OutlineWidth", 0f);
            groundMat.SetFloat("_WaveAmp", 0f);
            groundMat.SetFloat("_RimAmount", 0f);
            groundMat.SetFloat("_Bands", 2f);
            AssetDatabase.CreateAsset(groundMat, dir + "/RainGround.mat");
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            Object.DestroyImmediate(ground.GetComponent<Collider>());
            ground.transform.localScale = new Vector3(60f, 1f, 30f);
            ground.GetComponent<MeshRenderer>().sharedMaterial = groundMat;

            // rain: thin stretched streaks, pre-simulated before every shot
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
            var em = ps.emission; em.rateOverTime = 1100f;
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Box; sh.scale = new Vector3(18f, 0.2f, 6f);
            var vel = ps.velocityOverLifetime; vel.enabled = true; vel.space = ParticleSystemSimulationSpace.World;
            vel.x = 0.8f; vel.y = -8f; vel.z = 0f;
            ps.useAutoRandomSeed = false; ps.randomSeed = 11;
            var pr = rainGo.GetComponent<ParticleSystemRenderer>();
            pr.renderMode = ParticleSystemRenderMode.Stretch;
            pr.velocityScale = 0.03f; pr.lengthScale = 1f; pr.cameraVelocityScale = 0f;
            var rainMat = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit")) { name = "Rain" };
            rainMat.SetFloat("_Surface", 1f);
            rainMat.SetFloat("_Blend", 0f);
            rainMat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            rainMat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            rainMat.SetFloat("_ZWrite", 0f);
            rainMat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            rainMat.SetOverrideTag("RenderType", "Transparent");
            rainMat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            AssetDatabase.CreateAsset(rainMat, dir + "/Rain.mat");
            pr.sharedMaterial = rainMat;
            rain = ps;

            var camGo = new GameObject("Overview Camera");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.1f, 0.13f, 0.17f);
            camGo.AddComponent<UniversalAdditionalCameraData>();
            SlugGallery.FrameOverview(cam, 9);

            gallery = new GameObject("Gallery").transform;
        }

        static void RainNow()
        {
            if (_rain == null) return;
            _rain.Simulate(2f, true, true, true);
        }

        // ------------------------------------------------------------------ placement
        static TextMeshPro Text(string name, Vector3 pos, string text, float size)
        {
            var go = new GameObject(name);
            go.transform.SetParent(_gallery, false);
            go.transform.position = pos;
            var t = go.AddComponent<TextMeshPro>();
            t.font = SlugGallery.GetFont();
            t.alignment = TextAlignmentOptions.Top;
            t.fontSize = size;
            t.textWrappingMode = TextWrappingModes.NoWrap;
            t.overflowMode = TextOverflowModes.Overflow;
            t.rectTransform.sizeDelta = new Vector2(3f, 0.6f);
            t.color = LabelColor;
            t.text = text;
            t.ForceMeshUpdate();
            return t;
        }

        static void ClearGallery()
        {
            for (int i = _gallery.childCount - 1; i >= 0; i--) Object.DestroyImmediate(_gallery.GetChild(i).gameObject);
        }

        static GameObject AddSlug(string look, SlugDesignSpec d, Vector3 pos, float yaw)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath(look, d.id));
            var inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            inst.transform.SetParent(_gallery, false);
            inst.transform.position = pos;
            inst.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            return inst;
        }

        /// <summary>All nine designs in one row.</summary>
        static void PlaceRow(string look, string title)
        {
            ClearGallery();
            var designs = SlugDesigns.All();
            for (int i = 0; i < designs.Count; i++)
            {
                float x = (i - (designs.Count - 1) / 2f) * SlugGallery.Spacing;
                AddSlug(look, designs[i], new Vector3(x, 0f, 0f), SlugGallery.OverviewYaw);
                string tag = designs[i].kind == "base" ? "" : "<size=60%>反転案</size>\n";
                Text("Label_" + designs[i].id, new Vector3(x, 0.01f, -1.0f), $"{tag}<b>{i + 1}. {designs[i].jpName}</b>", 1.1f);
            }
            if (!string.IsNullOrEmpty(title)) Text("Title", new Vector3(0f, 1.55f, -0.2f), title, 2.6f);
        }

        /// <summary>One design, several looks side by side.</summary>
        static void PlaceSheet(SlugDesignSpec d, string[] looks, string[] labels, float yaw, string title)
        {
            ClearGallery();
            float maxRx = 0f;
            foreach (var k in d.body.keys) maxRx = Mathf.Max(maxRx, k.rx);
            float L = d.body.length;
            bool front = Mathf.Approximately(yaw, 180f);
            float spacing = front ? Mathf.Max(0.95f, maxRx * 2f + 0.5f) : L * 0.75f + 0.55f;
            float labelZ = -(L * (front ? 0.5f : 0.42f) + 0.9f);
            for (int i = 0; i < looks.Length; i++)
            {
                float x = (i - (looks.Length - 1) / 2f) * spacing;
                AddSlug(looks[i], d, new Vector3(x, 0f, 0f), yaw);
                Text("Label_" + looks[i], new Vector3(x, 0.01f, labelZ), labels[i], 1.2f);
            }
            Text("Title", new Vector3(0f, d.id.StartsWith("06") ? 1.45f : 0.95f, 0f), title, 1.6f);
        }

        // ------------------------------------------------------------------ scene
        [MenuItem("Slug/V2 Build Scene")]
        public static void BuildScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            Environment();
            PlaceRow("moss_subdued", "v2  苔系 × 控えめな目");
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            Debug.Log("[Slug v2] scene saved: " + ScenePath);
        }

        // ------------------------------------------------------------------ screenshots
        [MenuItem("Slug/V2 Capture Screenshots")]
        public static void Capture()
        {
            Directory.CreateDirectory(OutDir);
            Directory.CreateDirectory(Path.Combine(OutDir, "by_design"));
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            _gallery = GameObject.Find("Gallery").transform;
            _rain = GameObject.Find("Rain").GetComponent<ParticleSystem>();
            var overview = GameObject.Find("Overview Camera").GetComponent<Camera>();

            Shader.SetGlobalFloat("_SlugFreeze", 1f);
            Shader.SetGlobalFloat("_SlugTimeValue", FrozenTime);
            Camera cam = null;
            try
            {
                int ow = 4096, oh = Mathf.RoundToInt(4096 / SlugGallery.OverviewAspect);
                var rows = new (string look, string file, string title)[]
                {
                    ("moss_subdued", "palette_moss", "配色：苔系（moss）"),
                    ("earth_subdued", "palette_earth", "配色：土系（earth）"),
                    ("gray_subdued", "palette_gray", "配色：灰系（gray）"),
                    ("moss_subdued", "eyes_subdued", "目：今より控えめ（苔系）"),
                    ("moss_dot", "eyes_dot", "目：点目（苔系）"),
                    ("moss_sleepy", "eyes_sleepy", "目：眠たそうなまぶた付き（苔系）"),
                    ("pop_subdued", "rare_pop", "レア色の例（旧ポップ配色 × 控えめな目）"),
                };
                foreach (var r in rows)
                {
                    PlaceRow(r.look, r.title);
                    RainNow();
                    SlugScreenshots.Save(overview, Path.Combine(OutDir, $"overview_{r.file}.png"), ow, oh);
                }

                cam = SlugScreenshots.MakeCamera(overview);
                cam.fieldOfView = 20f;
                const int W = 1920, H = 1080;
                foreach (var d in SlugDesigns.All())
                {
                    string name = $"{d.id}";
                    // palettes: 3/4 view from above
                    PlaceSheet(d, new[] { "moss_subdued", "earth_subdued", "gray_subdued" },
                               new[] { "苔系", "土系", "灰系" }, 145f, $"{d.jpName}　配色");
                    cam.aspect = W / (float)H;
                    SlugScreenshots.Frame(cam, SlugScreenshots.BoundsOf(_gallery.gameObject), 18f, 1.12f);
                    RainNow();
                    SlugScreenshots.Save(cam, Path.Combine(OutDir, "by_design", $"{name}_palettes.png"), W, H);

                    // eyes: front view, close
                    PlaceSheet(d, new[] { "moss_subdued", "moss_dot", "moss_sleepy" },
                               new[] { "控えめ", "点目", "眠たそう" }, 180f, $"{d.jpName}　目");
                    cam.aspect = W / (float)H;
                    SlugScreenshots.Frame(cam, SlugScreenshots.BoundsOf(_gallery.gameObject), 10f, 1.12f);
                    RainNow();
                    SlugScreenshots.Save(cam, Path.Combine(OutDir, "by_design", $"{name}_eyes.png"), W, H);
                }
            }
            finally
            {
                Shader.SetGlobalFloat("_SlugFreeze", 0f);
                if (cam != null) Object.DestroyImmediate(cam.gameObject);
            }
            PlaceRow("moss_subdued", "v2  苔系 × 控えめな目");
            EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log("[Slug v2] screenshots written to " + OutDir);
        }
    }
}
