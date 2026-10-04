using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SlugDesign
{
    /// <summary>
    /// v6: dignified long body (gentle S-curve, lifted head, small pointed tail), short conical hands,
    /// and six realistic 3D eyeballs on the same body.
    /// </summary>
    public static class SlugGalleryV6
    {
        public const string Dir = "Assets/Slug/GeneratedV6";
        public const string ScenePath = "Assets/Scenes/SlugGalleryV6.unity";
        const float Spacing = 2.0f;
        const float FrozenTime = 0.7f;       // the membrane eye is part-way through closing at this moment
        const float LabelZ = -2.3f;
        const float RowAspect = 5.2f;
        static readonly Color LabelColor = new Color(0.86f, 0.9f, 0.92f);

        public static readonly (SlugEyeKind kind, string name)[] Eyes =
        {
            (SlugEyeKind.Reptile, "爬虫類風"),
            (SlugEyeKind.Frog, "カエル風"),
            (SlugEyeKind.Octopus, "タコ風"),
            (SlugEyeKind.Fish, "魚風"),
            (SlugEyeKind.Membrane, "瞬膜つき"),
            (SlugEyeKind.LidFolds, "まぶたのひだ"),
        };

        /// <summary>Values shared by the six slugs.</summary>
        public static void ApplyCommon(SlugPop s)
        {
            s.SetAll(1f);
            s.body = 1f;                // the dignified shape
            s.pattern = 0.5f;           // unchanged
            s.color = 1f;               // moss
            s.eyes = 0.4f;              // size of the 3D eyeballs
            s.tentacleLength = 1f;      // unchanged
            s.tentacleThickness = 0.2f; // unchanged
            s.tentacleDirection = 0.5f; // unchanged: straight up
            s.expression = 1f;          // mouth as before
            s.hands = 1f;               // short cone
            s.shading = 1f;             // unchanged
            s.outline = 1f;             // unchanged
        }

        static string OutDir => Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Screenshots", "v6"));
        static Transform _gallery;
        static ParticleSystem _rain;
        static float XAt(int i) => (i - (Eyes.Length - 1) / 2f) * Spacing;

        static TextMeshPro Text(string name, Vector3 pos, string text, float size, TextAlignmentOptions align = TextAlignmentOptions.Top)
        {
            var go = new GameObject(name);
            go.transform.SetParent(_gallery, false);
            go.transform.position = pos;
            var t = go.AddComponent<TextMeshPro>();
            t.font = SlugGallery.GetFont();
            t.alignment = align;
            t.fontSize = size;
            t.textWrappingMode = TextWrappingModes.NoWrap;
            t.overflowMode = TextOverflowModes.Overflow;
            t.rectTransform.sizeDelta = new Vector2(3f, 0.6f);
            t.color = LabelColor;
            t.text = text;
            t.ForceMeshUpdate();
            return t;
        }

        [MenuItem("Slug/V6 Build Scene")]
        public static void BuildScene()
        {
            if (!AssetDatabase.IsValidFolder(Dir)) AssetDatabase.CreateFolder("Assets/Slug", "GeneratedV6");
            if (AssetDatabase.LoadAssetAtPath<GameObject>(SlugGalleryV3.PrefabPath) == null) SlugGalleryV3.BuildPrefab();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            SlugGalleryV2.BuildEnvironment(Dir, out _gallery, out _rain);
            var sh = _rain.shape; sh.scale = new Vector3(30f, 0.2f, 6f);
            var em = _rain.emission; em.rateOverTime = 1700f;

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(SlugGalleryV3.PrefabPath);
            for (int i = 0; i < Eyes.Length; i++)
            {
                var inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                inst.name = $"Slug_Eye{i + 1}";
                inst.transform.SetParent(_gallery, false);
                inst.transform.position = new Vector3(XAt(i), 0f, 0f);
                inst.transform.rotation = Quaternion.Euler(0f, 145f, 0f);
                var s = inst.GetComponent<SlugPop>();
                ApplyCommon(s);
                s.eyeKind = Eyes[i].kind;
                s.Rebuild();
                Text($"Label_{i}", new Vector3(XAt(i), 0.32f, LabelZ), $"<size=70%>目 {i + 1}</size>\n<b>{Eyes[i].name}</b>", 1.5f, TextAlignmentOptions.Bottom);
            }
            Text("Title", new Vector3(0f, 1.5f, -0.2f), "目の比較（体・触角・手・口は共通）", 1.8f);

            var cam = GameObject.Find("Overview Camera").GetComponent<Camera>();
            FrameRow(cam, RowAspect, 14f);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            Debug.Log("[Slug v6] scene saved: " + ScenePath);
        }

        static void FrameRow(Camera cam, float aspect, float elevDeg)
        {
            cam.fieldOfView = 12f;
            cam.nearClipPlane = 0.5f;
            cam.farClipPlane = 140f;
            float width = (Eyes.Length - 1) * Spacing + 2.8f;
            float dist = width / (2f * Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad) * aspect);
            float e = elevDeg * Mathf.Deg2Rad;
            Vector3 target = new Vector3(0f, 0.6f, -1.1f);
            cam.transform.position = target + new Vector3(0f, Mathf.Sin(e), -Mathf.Cos(e)) * dist;
            cam.transform.LookAt(target);
        }

        /// <summary>Four stills tiled into one image (2 x 2).</summary>
        static void Tile(string[] files, string output, int w, int h)
        {
            var sheet = new Texture2D(w * 2, h * 2, TextureFormat.RGB24, false);
            for (int i = 0; i < 4; i++)
            {
                var t = new Texture2D(2, 2);
                t.LoadImage(File.ReadAllBytes(files[i]));
                int x = (i % 2) * w, y = (1 - i / 2) * h;
                sheet.SetPixels(x, y, w, h, t.GetPixels());
                Object.DestroyImmediate(t);
            }
            File.WriteAllBytes(output, sheet.EncodeToPNG());
            Object.DestroyImmediate(sheet);
        }

        /// <summary>Points a camera at the face: both eyes fill the frame.</summary>
        static void AimAtFace(Camera cam, SlugPop s, float dist)
        {
            Vector3 c = s.FaceCenterWorld;
            Vector3 f = s.FaceForwardWorld;
            cam.transform.position = c + f * dist + s.FaceUpWorld * dist * 0.06f;
            cam.transform.LookAt(c);
        }

        [MenuItem("Slug/V6 Capture Screenshots")]
        public static void Capture()
        {
            Directory.CreateDirectory(OutDir);
            foreach (var d in new[] { "by_eye", "closeup", "wave" }) Directory.CreateDirectory(Path.Combine(OutDir, d));
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            _gallery = GameObject.Find("Gallery").transform;
            _rain = GameObject.Find("Rain").GetComponent<ParticleSystem>();
            var overview = GameObject.Find("Overview Camera").GetComponent<Camera>();

            var slugs = new List<SlugPop>(_gallery.GetComponentsInChildren<SlugPop>());
            slugs.Sort((a, b) => a.name.CompareTo(b.name));            // Slug_Eye1 ... Slug_Eye6
            var texts = new List<GameObject>();
            foreach (Transform c in _gallery) if (c.GetComponent<TextMeshPro>() != null) texts.Add(c.gameObject);
            foreach (var s in slugs) s.Rebuild();

            Shader.SetGlobalFloat("_SlugFreeze", 1f);
            Shader.SetGlobalFloat("_SlugTimeValue", FrozenTime);
            Camera cam = null;
            try
            {
                int ow = 4000, oh = Mathf.RoundToInt(4000 / RowAspect);
                var rows = new (string file, float yaw, float elev)[]
                {
                    ("00_overview", 145f, 14f), ("row_angle", 145f, 14f), ("row_side", 90f, 8f), ("row_front", 180f, 10f),
                };
                foreach (var r in rows)
                {
                    foreach (var s in slugs) s.transform.rotation = Quaternion.Euler(0f, r.yaw, 0f);
                    FrameRow(overview, ow / (float)oh, r.elev);
                    _rain.Simulate(2f, true, true, true);
                    SlugScreenshots.Save(overview, Path.Combine(OutDir, r.file + ".png"), ow, oh);
                }

                cam = SlugScreenshots.MakeCamera(overview);
                cam.fieldOfView = 20f;
                const int W = 1280, H = 960;
                var views = new (string name, float yaw, float elev)[] { ("front", 180f, 6f), ("side", 90f, 3f), ("angle", 145f, 16f) };
                foreach (var t in texts) t.SetActive(false);
                for (int i = 0; i < slugs.Count; i++)
                {
                    var s = slugs[i];
                    foreach (var o in slugs) o.gameObject.SetActive(o == s);
                    s.Rebuild();
                    foreach (var v in views)
                    {
                        s.transform.rotation = Quaternion.Euler(0f, v.yaw, 0f);
                        cam.aspect = W / (float)H;
                        SlugScreenshots.Frame(cam, SlugScreenshots.BoundsOf(s.gameObject), v.elev, 1.15f);
                        _rain.Simulate(2f, true, true, true);
                        SlugScreenshots.Save(cam, Path.Combine(OutDir, "by_eye", $"eye{i + 1}_{v.name}.png"), W, H);
                    }

                    // eye close-up: both eyes fill the frame
                    s.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
                    cam.fieldOfView = 16f;
                    cam.aspect = W / (float)H;
                    cam.nearClipPlane = 0.05f;
                    AimAtFace(cam, s, 0.62f);
                    _rain.Clear();
                    SlugScreenshots.Save(cam, Path.Combine(OutDir, "closeup", $"eye{i + 1}_closeup.png"), W, H);
                    cam.fieldOfView = 20f;
                }

                // ---- crawl wave: four stills a quarter of a period apart; the eyes must stay on the skin
                float[] times = { 0f, 0.28f, 0.56f, 0.83f };
                var star = slugs[0];
                foreach (var o in slugs) o.gameObject.SetActive(o == star);
                star.Rebuild();
                foreach (var v in new[] { ("side", 90f, 3f), ("angle", 145f, 16f), ("front34", 165f, 8f) })
                {
                    star.transform.rotation = Quaternion.Euler(0f, v.Item2, 0f);
                    Shader.SetGlobalFloat("_SlugTimeValue", 0f);
                    cam.aspect = W / (float)H;
                    SlugScreenshots.Frame(cam, SlugScreenshots.BoundsOf(star.gameObject), v.Item3, 1.2f);      // framed once, then fixed
                    var files = new string[4];
                    for (int k = 0; k < 4; k++)
                    {
                        Shader.SetGlobalFloat("_SlugTimeValue", times[k]);
                        files[k] = Path.Combine(OutDir, "wave", $"frame{k}_{v.Item1}.png");
                        SlugScreenshots.Save(cam, files[k], W, H);
                    }
                    Tile(files, Path.Combine(OutDir, "wave", $"wave_4frames_{v.Item1}.png"), W, H);
                }

                // close-ups of the face over the same four moments: eye 1 (reptile) and eye 5 (membrane closing)
                foreach (int idx in new[] { 0, 4 })
                {
                    var s = slugs[idx];
                    foreach (var o in slugs) o.gameObject.SetActive(o == s);
                    s.Rebuild();
                    s.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
                    cam.fieldOfView = 16f;
                    cam.aspect = W / (float)H;
                    Shader.SetGlobalFloat("_SlugTimeValue", 0f);
                    AimAtFace(cam, s, 0.62f);                       // aimed once at time 0, then fixed
                    var files = new string[4];
                    for (int k = 0; k < 4; k++)
                    {
                        Shader.SetGlobalFloat("_SlugTimeValue", times[k]);
                        files[k] = Path.Combine(OutDir, "wave", $"frame{k}_face_eye{idx + 1}.png");
                        SlugScreenshots.Save(cam, files[k], W, H);
                    }
                    Tile(files, Path.Combine(OutDir, "wave", $"wave_4frames_face_eye{idx + 1}.png"), W, H);
                    cam.fieldOfView = 20f;
                }
                Shader.SetGlobalFloat("_SlugTimeValue", FrozenTime);

                foreach (var o in slugs) { o.gameObject.SetActive(true); o.Rebuild(); o.transform.rotation = Quaternion.Euler(0f, 145f, 0f); }
                foreach (var t in texts) t.SetActive(true);
            }
            finally
            {
                Shader.SetGlobalFloat("_SlugFreeze", 0f);
                if (cam != null) Object.DestroyImmediate(cam.gameObject);
            }
            FrameRow(overview, RowAspect, 14f);
            EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log("[Slug v6] screenshots written to " + OutDir);
        }
    }
}
