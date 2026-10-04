using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SlugDesign
{
    /// <summary>
    /// v4: element-wise sliders. The decided elements are fixed for all three slugs;
    /// the four undecided ones (expression, hands, shading, outline) are set to 1.0 / 0.5 / 0.0 together.
    /// </summary>
    public static class SlugGalleryV4
    {
        public const string Dir = "Assets/Slug/GeneratedV4";
        public const string ScenePath = "Assets/Scenes/SlugGalleryV4.unity";
        const float Spacing = 1.9f;
        const float FrozenTime = 1.3f;
        const float LabelZ = -2.7f;
        const float RowAspect = 2.4f;
        static readonly Color LabelColor = new Color(0.86f, 0.9f, 0.92f);

        /// <summary>Values already decided (shared by all three).</summary>
        public static void ApplyDecided(SlugPop s)
        {
            s.body = 1f;
            s.pattern = 0.5f;
            s.color = 1f;
            s.eyes = 0.25f;
            s.tentacleLength = 1f;
            s.tentacleThickness = 0.5f;
            s.tentacleDirection = 0.5f;
        }

        /// <summary>The four still-undecided elements.</summary>
        public static void ApplyUndecided(SlugPop s, float v)
        {
            s.expression = v;
            s.hands = v;
            s.shading = v;
            s.outline = v;
        }

        static readonly float[] Values = { 1f, 0.5f, 0f };

        static string OutDir => Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Screenshots", "v4"));
        static Transform _gallery;
        static ParticleSystem _rain;
        static float XAt(int i) => (i - (Values.Length - 1) / 2f) * Spacing;

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

        [MenuItem("Slug/V4 Build Scene")]
        public static void BuildScene()
        {
            if (!AssetDatabase.IsValidFolder(Dir)) AssetDatabase.CreateFolder("Assets/Slug", "GeneratedV4");
            if (AssetDatabase.LoadAssetAtPath<GameObject>(SlugGalleryV3.PrefabPath) == null) SlugGalleryV3.BuildPrefab();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            SlugGalleryV2.BuildEnvironment(Dir, out _gallery, out _rain);

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(SlugGalleryV3.PrefabPath);
            for (int i = 0; i < Values.Length; i++)
            {
                var inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                inst.name = $"Slug_{Values[i]:0.0}";
                inst.transform.SetParent(_gallery, false);
                inst.transform.position = new Vector3(XAt(i), 0f, 0f);
                inst.transform.rotation = Quaternion.Euler(0f, 145f, 0f);
                var s = inst.GetComponent<SlugPop>();
                s.SetAll(1f);
                ApplyDecided(s);
                ApplyUndecided(s, Values[i]);
                s.Rebuild();
                Text($"Label_{i}", new Vector3(XAt(i), 0.32f, LabelZ),
                     $"<size=65%>表情・手・陰影・輪郭線</size>\n<b>{Values[i]:0.0}</b>", 1.5f, TextAlignmentOptions.Bottom);
            }
            Text("Title", new Vector3(0f, 1.15f, -0.2f),
                 "共通：体1.0　模様0.5　色1.0　目0.25（ハイライト0.25）　触角 長さ1.0・太さ0.5・向き0.5", 1.2f);

            var cam = GameObject.Find("Overview Camera").GetComponent<Camera>();
            FrameRow(cam, RowAspect, 14f);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            Debug.Log("[Slug v4] scene saved: " + ScenePath);
        }

        static void FrameRow(Camera cam, float aspect, float elevDeg)
        {
            cam.fieldOfView = 12f;
            cam.nearClipPlane = 0.5f;
            cam.farClipPlane = 120f;
            float width = (Values.Length - 1) * Spacing + 1.9f;
            float dist = width / (2f * Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad) * aspect);
            float e = elevDeg * Mathf.Deg2Rad;
            Vector3 target = new Vector3(0f, 0.5f, -1.2f);
            cam.transform.position = target + new Vector3(0f, Mathf.Sin(e), -Mathf.Cos(e)) * dist;
            cam.transform.LookAt(target);
        }

        [MenuItem("Slug/V4 Capture Screenshots")]
        public static void Capture()
        {
            Directory.CreateDirectory(OutDir);
            Directory.CreateDirectory(Path.Combine(OutDir, "by_variant"));
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            _gallery = GameObject.Find("Gallery").transform;
            _rain = GameObject.Find("Rain").GetComponent<ParticleSystem>();
            var overview = GameObject.Find("Overview Camera").GetComponent<Camera>();

            var slugs = new List<SlugPop>(_gallery.GetComponentsInChildren<SlugPop>());
            slugs.Sort((a, b) => b.expression.CompareTo(a.expression));   // 1.0, 0.5, 0.0
            var texts = new List<GameObject>();
            foreach (Transform c in _gallery) if (c.GetComponent<TextMeshPro>() != null) texts.Add(c.gameObject);
            foreach (var s in slugs) s.Rebuild();

            Shader.SetGlobalFloat("_SlugFreeze", 1f);
            Shader.SetGlobalFloat("_SlugTimeValue", FrozenTime);
            Camera cam = null;
            try
            {
                int ow = 2880, oh = Mathf.RoundToInt(2880 / RowAspect);
                var rows = new (string file, float yaw, float elev)[]
                {
                    ("00_overview", 145f, 14f),
                    ("row_angle", 145f, 14f),
                    ("row_side", 90f, 8f),
                    ("row_front", 180f, 10f),
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
                var views = new (string name, float yaw, float elev)[] { ("front", 180f, 6f), ("side", 90f, 3f), ("angle", 145f, 20f) };
                foreach (var t in texts) t.SetActive(false);
                foreach (var s in slugs)
                {
                    foreach (var o in slugs) o.gameObject.SetActive(o == s);
                    s.Rebuild();
                    foreach (var v in views)
                    {
                        s.transform.rotation = Quaternion.Euler(0f, v.yaw, 0f);
                        cam.aspect = W / (float)H;
                        SlugScreenshots.Frame(cam, SlugScreenshots.BoundsOf(s.gameObject), v.elev, 1.2f);
                        _rain.Simulate(2f, true, true, true);
                        SlugScreenshots.Save(cam, Path.Combine(OutDir, "by_variant", $"undecided_{s.expression:0.0}_{v.name}.png"), W, H);
                    }
                }
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
            Debug.Log("[Slug v4] screenshots written to " + OutDir);
        }
    }
}
