using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.TextCore.LowLevel;

namespace SlugDesign
{
    /// <summary>Lines the nine slugs up in one scene with name labels, and takes the screenshots.</summary>
    public static class SlugGallery
    {
        public const string ScenePath = "Assets/Scenes/SlugGallery.unity";
        public const string FontPath = "Assets/Slug/Fonts/NotoSansJP SDF.asset";
        public const float Spacing = 1.55f;
        public const float OverviewAspect = 4.8f;
        public const float OverviewYaw = 145f;   // head toward the camera (-Z), turned a little to the right

        // ------------------------------------------------------------------ font (Japanese labels)
        internal static TMP_FontAsset GetFont()
        {
            var existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
            if (existing != null) return existing;
            var src = AssetDatabase.LoadAssetAtPath<Font>("Assets/Slug/Fonts/NotoSansJP.ttf");
            var fa = TMP_FontAsset.CreateFontAsset(src, 72, 6, GlyphRenderMode.SDFAA, 2048, 2048, AtlasPopulationMode.Dynamic, true);
            fa.name = "NotoSansJP SDF";
            AssetDatabase.CreateAsset(fa, FontPath);
            if (fa.atlasTextures != null)
                foreach (var t in fa.atlasTextures) { t.name = "NotoSansJP Atlas"; AssetDatabase.AddObjectToAsset(t, fa); }
            if (fa.material != null) { fa.material.name = "NotoSansJP Material"; AssetDatabase.AddObjectToAsset(fa.material, fa); }
            AssetDatabase.SaveAssets();
            return fa;
        }

        // ------------------------------------------------------------------ URP asset tuning for clean soft shadows
        static void TuneUrp()
        {
            var rp = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>("Assets/Settings/UniversalRP.asset");
            if (rp == null) return;
            rp.shadowDistance = 14f;
            rp.shadowCascadeCount = 1;
            rp.mainLightShadowmapResolution = 2048;
            rp.msaaSampleCount = 4;
            rp.supportsHDR = true;
            EditorUtility.SetDirty(rp);
            AssetDatabase.SaveAssetIfDirty(rp);
        }

        // ------------------------------------------------------------------ scene
        [MenuItem("Slug/Build Gallery Scene")]
        public static void BuildScene()
        {
            TuneUrp();
            var designs = SlugDesigns.All();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            RenderSettings.skybox = null;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.62f, 0.68f, 0.8f);
            RenderSettings.ambientEquatorColor = new Color(0.56f, 0.57f, 0.65f);
            RenderSettings.ambientGroundColor = new Color(0.42f, 0.4f, 0.45f);

            // light: soft, from the camera side (upper right front)
            var lightGo = new GameObject("Directional Light");
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(1f, 0.97f, 0.92f);
            light.intensity = 1.0f;
            light.shadows = LightShadows.Soft;
            light.shadowStrength = 0.75f;
            lightGo.transform.rotation = Quaternion.Euler(48f, -28f, 0f);

            // ground (matte, desaturated: "the world is chic, the characters are pop")
            var shader = Shader.Find(SlugBuilder.ShaderName);
            var groundMat = new Material(shader) { name = "Ground" };
            groundMat.SetColor("_BaseColor", new Color(0.62f, 0.68f, 0.68f));
            groundMat.SetColor("_ShadeColor", new Color(0.62f, 0.64f, 0.72f));
            groundMat.SetFloat("_OutlineWidth", 0f);
            groundMat.SetFloat("_WaveAmp", 0f);
            groundMat.SetFloat("_RimAmount", 0f);
            groundMat.SetFloat("_Bands", 2f);
            if (!AssetDatabase.IsValidFolder(SlugBuilder.Root)) AssetDatabase.CreateFolder("Assets/Slug", "Generated");
            AssetDatabase.CreateAsset(groundMat, SlugBuilder.Root + "/Ground.mat");
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            Object.DestroyImmediate(ground.GetComponent<Collider>());
            ground.transform.localScale = new Vector3(12f, 1f, 6f);
            ground.GetComponent<MeshRenderer>().sharedMaterial = groundMat;

            // slugs
            var gallery = new GameObject("Gallery").transform;
            var font = GetFont();
            int n = designs.Count;
            for (int i = 0; i < n; i++)
            {
                var d = designs[i];
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{SlugBuilder.Root}/Prefabs/Slug_{d.id}.prefab");
                var inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                inst.transform.SetParent(gallery, false);
                inst.transform.position = new Vector3((i - (n - 1) / 2f) * Spacing, 0f, 0f);
                inst.transform.rotation = Quaternion.Euler(0f, OverviewYaw, 0f);

                var label = new GameObject("Label_" + d.id);
                label.transform.SetParent(gallery, false);
                label.transform.position = new Vector3(inst.transform.position.x, 0.01f, -1.0f);
                var tmp = label.AddComponent<TextMeshPro>();
                tmp.font = font;
                tmp.alignment = TextAlignmentOptions.Top;
                tmp.fontSize = 1.15f;
                tmp.overflowMode = TextOverflowModes.Overflow;
                tmp.textWrappingMode = TextWrappingModes.NoWrap;
                tmp.rectTransform.sizeDelta = new Vector2(1.5f, 0.5f);
                tmp.color = d.kind == "base" ? new Color(0.14f, 0.16f, 0.24f) : new Color(0.7f, 0.12f, 0.42f);
                string tag = d.kind == "base" ? "" : "<size=60%>反転案</size>\n";
                tmp.text = $"{tag}<b>{i + 1}. {d.jpName}</b>\n<size=65%>{d.enName}</size>";
            }

            // overview camera
            var camGo = new GameObject("Overview Camera");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.72f, 0.77f, 0.82f);
            camGo.AddComponent<UniversalAdditionalCameraData>();
            FrameOverview(cam, n);

            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            Debug.Log("[Slug] gallery scene saved: " + ScenePath);
        }

        internal static void FrameOverview(Camera cam, int n)
        {
            cam.fieldOfView = 6f;
            cam.nearClipPlane = 0.5f;
            cam.farClipPlane = 80f;
            float width = (n - 1) * Spacing + 1.5f;
            float aspect = OverviewAspect;
            float dist = width / (2f * Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad) * aspect);
            float elev = 16f * Mathf.Deg2Rad;
            Vector3 target = new Vector3(0f, 0.4f, -0.5f);
            cam.transform.position = target + new Vector3(0f, Mathf.Sin(elev), -Mathf.Cos(elev)) * dist;
            cam.transform.LookAt(target);
        }
    }
}
