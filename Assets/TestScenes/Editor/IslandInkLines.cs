using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace SlugExtract
{
    /// <summary>
    /// Thin ink lines on ridges and edges over the whole picture (docs/ideas/cairn_terrain_ideas.md, H): adds a full-screen
    /// pass with Slug/InkLines to the renderer (Assets/Settings/UniversalRP_Renderer), or switches it off. It works in every
    /// scene, so Off is there to compare and to take it back out if it gets noisy.
    /// </summary>
    public static class IslandInkLines
    {
        const string RendererPath = "Assets/Settings/UniversalRP_Renderer.asset";
        const string MatPath = "Assets/Settings/InkLines.mat";
        const string FeatureName = "InkLines";

        [MenuItem("Slug/IslandTest 800 m/Cairn Try/Ink Lines On")] public static string On() => Set(true);
        [MenuItem("Slug/IslandTest 800 m/Cairn Try/Ink Lines Off")] public static string Off() => Set(false);

        static string Set(bool on)
        {
            var data = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(RendererPath);
            if (!data) return "no renderer at " + RendererPath;
            var feature = data.rendererFeatures.OfType<FullScreenPassRendererFeature>().FirstOrDefault(f => f && f.name == FeatureName);
            if (!feature)
            {
                if (!on) return "no ink lines to switch off";
                var mat = AssetDatabase.LoadAssetAtPath<Material>(MatPath);
                if (!mat) { mat = new Material(Shader.Find("Slug/InkLines")); AssetDatabase.CreateAsset(mat, MatPath); }
                feature = ScriptableObject.CreateInstance<FullScreenPassRendererFeature>();
                feature.name = FeatureName;
                feature.passMaterial = mat;
                feature.injectionPoint = FullScreenPassRendererFeature.InjectionPoint.BeforeRenderingPostProcessing;
                feature.requirements = ScriptableRenderPassInput.Depth;
                feature.fetchColorBuffer = true;
                AssetDatabase.AddObjectToAsset(feature, data);
                AssetDatabase.TryGetGUIDAndLocalFileIdentifier(feature, out _, out long id);
                var so = new SerializedObject(data);
                var list = so.FindProperty("m_RendererFeatures");
                var map = so.FindProperty("m_RendererFeatureMap");
                list.arraySize++;
                list.GetArrayElementAtIndex(list.arraySize - 1).objectReferenceValue = feature;
                map.arraySize++;
                map.GetArrayElementAtIndex(map.arraySize - 1).longValue = id;
                so.ApplyModifiedProperties();
            }
            feature.SetActive(on);
            EditorUtility.SetDirty(feature);
            EditorUtility.SetDirty(data);
            AssetDatabase.SaveAssets();
            return on ? "ink lines on" : "ink lines off";
        }
    }
}
