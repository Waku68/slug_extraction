using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public static class URPSetup
{
    [MenuItem("Tools/Setup URP")]
    public static void Run()
    {
        const string dir = "Assets/Settings";
        const string path = dir + "/UniversalRP.asset";
        var rp = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(path);
        if (rp == null)
        {
            var data = ScriptableObject.CreateInstance<UniversalRendererData>();
            AssetDatabase.CreateAsset(data, dir + "/UniversalRP_Renderer.asset");
            rp = UniversalRenderPipelineAsset.Create(data);
            AssetDatabase.CreateAsset(rp, path);
        }
        GraphicsSettings.defaultRenderPipeline = rp;
        for (int i = 0; i < QualitySettings.names.Length; i++)
        {
            QualitySettings.SetQualityLevel(i, false);
            QualitySettings.renderPipeline = rp;
        }
        AssetDatabase.SaveAssets();
        EditorUtility.SetDirty(rp);
        AssetDatabase.SaveAssetIfDirty(rp);
        Debug.Log("URP setup done: " + path);
    }
}
