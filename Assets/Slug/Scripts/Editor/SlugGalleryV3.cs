using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace SlugDesign
{
    /// <summary>
    /// v3: nine pop levels (1.0 ... 0.0 in steps of 0.125) of the rear-up slug, side by side in the dark rain set.
    /// Every slug is a <see cref="SlugPop"/>; the slider is the only difference between them.
    /// </summary>
    public static class SlugGalleryV3
    {
        public const string Dir = "Assets/Slug/GeneratedV3";
        public const string PrefabPath = "Assets/Slug/Prefabs/Slug_Pop.prefab";
        public const string ScenePath = "Assets/Scenes/SlugGalleryV3.unity";
        const int Count = 9;
        const float Spacing = 2.0f;
        const float FrozenTime = 1.3f;
        const float RowAspect = 6.0f;
        const float LabelZ = -1.3f;
        static readonly Color LabelColor = new Color(0.86f, 0.9f, 0.92f);

        static string OutDir => Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Screenshots", "v3"));
        static float PopAt(int i) => 1f - i / (float)(Count - 1);
        static float XAt(int i) => (i - (Count - 1) / 2f) * Spacing;

        static Transform _gallery;
        static ParticleSystem _rain;

        // ------------------------------------------------------------------ prefab
        [MenuItem("Slug/V3 Build Prefab")]
        public static void BuildPrefab()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Slug/Prefabs")) AssetDatabase.CreateFolder("Assets/Slug", "Prefabs");
            var go = new GameObject("Slug_Pop");
            var c = go.AddComponent<SlugPop>();
            c.shader = Shader.Find(SlugBuilder.ShaderName);
            PrefabUtility.SaveAsPrefabAsset(go, PrefabPath);
            Object.DestroyImmediate(go);
            Debug.Log("[Slug v3] prefab saved: " + PrefabPath);
        }

        // ------------------------------------------------------------------ scene
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

        [MenuItem("Slug/V3 Build Scene")]
        public static void BuildScene()
        {
            if (!AssetDatabase.IsValidFolder(Dir)) AssetDatabase.CreateFolder("Assets/Slug", "GeneratedV3");
            if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) == null) BuildPrefab();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            SlugGalleryV2.BuildEnvironment(Dir, out _gallery, out _rain);

            // the row is wider than the v2 set: widen the rain
            var sh = _rain.shape; sh.scale = new Vector3(30f, 0.2f, 6f);
            var em = _rain.emission; em.rateOverTime = 1800f;

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            for (int i = 0; i < Count; i++)
            {
                var inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                inst.name = $"Pop_{PopAt(i):0.000}";
                inst.transform.SetParent(_gallery, false);
                inst.transform.position = new Vector3(XAt(i), 0f, 0f);
                inst.transform.rotation = Quaternion.Euler(0f, 145f, 0f);
                var c = inst.GetComponent<SlugPop>();
                c.pop = PopAt(i);
                c.Rebuild();
                Text($"Label_{i}", new Vector3(XAt(i), 0.01f, LabelZ), $"{PopAt(i):0.000}", 1.8f);
            }
            Text("Title", new Vector3(0f, 1.75f, -0.2f), "ポップ度  1.0 → 0.0（左から右へ 0.125 ずつ）", 2.4f);

            var cam = GameObject.Find("Overview Camera").GetComponent<Camera>();
            FrameRow(cam, RowAspect, 14f);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            Debug.Log("[Slug v3] scene saved: " + ScenePath);
        }

        static void FrameRow(Camera cam, float aspect, float elevDeg)
        {
            cam.fieldOfView = 6f;
            cam.nearClipPlane = 0.5f;
            cam.farClipPlane = 120f;
            float width = (Count - 1) * Spacing + 2.8f;
            float dist = width / (2f * Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad) * aspect);
            float e = elevDeg * Mathf.Deg2Rad;
            Vector3 target = new Vector3(0f, 0.55f, -0.4f);
            cam.transform.position = target + new Vector3(0f, Mathf.Sin(e), -Mathf.Cos(e)) * dist;
            cam.transform.LookAt(target);
        }

        // ------------------------------------------------------------------ screenshots
        [MenuItem("Slug/V3 Capture Screenshots")]
        public static void Capture()
        {
            Directory.CreateDirectory(OutDir);
            Directory.CreateDirectory(Path.Combine(OutDir, "by_pop"));
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            _gallery = GameObject.Find("Gallery").transform;
            _rain = GameObject.Find("Rain").GetComponent<ParticleSystem>();
            var overview = GameObject.Find("Overview Camera").GetComponent<Camera>();

            var slugs = new List<SlugPop>(_gallery.GetComponentsInChildren<SlugPop>());
            slugs.Sort((a, b) => b.pop.CompareTo(a.pop));
            var texts = new List<GameObject>();
            foreach (Transform c in _gallery) if (c.GetComponent<TextMeshPro>() != null) texts.Add(c.gameObject);
            foreach (var s in slugs) s.Rebuild();

            Shader.SetGlobalFloat("_SlugFreeze", 1f);
            Shader.SetGlobalFloat("_SlugTimeValue", FrozenTime);
            Camera cam = null;
            try
            {
                int ow = 4096, oh = Mathf.RoundToInt(4096 / RowAspect);
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

                // individual shots of every pop level
                cam = SlugScreenshots.MakeCamera(overview);
                cam.fieldOfView = 20f;
                const int W = 1280, H = 960;
                var views = new (string name, float yaw, float elev)[] { ("front", 180f, 6f), ("side", 90f, 3f), ("angle", 145f, 20f) };
                foreach (var t in texts) t.SetActive(false);
                foreach (var s in slugs)
                {
                    foreach (var o in slugs) o.gameObject.SetActive(o == s);
                    s.Rebuild();   // OnEnable defers the rebuild, so do it now
                    foreach (var v in views)
                    {
                        s.transform.rotation = Quaternion.Euler(0f, v.yaw, 0f);
                        cam.aspect = W / (float)H;
                        SlugScreenshots.Frame(cam, SlugScreenshots.BoundsOf(s.gameObject), v.elev, 1.2f);
                        _rain.Simulate(2f, true, true, true);
                        SlugScreenshots.Save(cam, Path.Combine(OutDir, "by_pop", $"pop_{s.pop:0.000}_{v.name}.png"), W, H);
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
            Debug.Log("[Slug v3] screenshots written to " + OutDir);
        }
    }
}
