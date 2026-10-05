using UnityEditor;
using UnityEngine;

namespace SlugDesign
{
    /// <summary>
    /// 世界の物（岩塩、植物など）の質感の基準。形はポップ、質感はリアル度 0.2（岩塩の 6×6 の見比べで決めた。ポップ寄り）。
    /// ナメクジと同じ Slug/Toon を使い、這う動きは 0、面はカクカク（Flat Facets）。輪郭線と段のある陰影はナメクジに近く、少しだけやわらかい。
    /// </summary>
    public static class WorldLook
    {
        public const float Realism = 0.2f;

        // 岩塩の RockSaltRealism の 0（ポップ）と 1（リアル）の端を、リアル度 0.2 で混ぜた値
        public const float OutlineWidth = 0.0076f;
        public const float BandSoftness = 0.118f;
        public const float ToonSmooth = 0.17f;
        public const float Ambient = 0.31f;

        public static Material GetOrCreate(string path)
        {
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null)
            {
                m = new Material(Shader.Find("Slug/Toon"));
                AssetDatabase.CreateAsset(m, path);
            }
            m.shader = Shader.Find("Slug/Toon");
            return m;
        }

        /// <summary>基準の質感を当てる。輪郭線の色は地の色を暗くした色（ナメクジと同じ決まり）。</summary>
        public static void Apply(Material m, Color baseColor, float rim = 0.15f, float mottle = 0f, Color? mottleColor = null,
                                 float mottleScale = 10f, float dent = 0f, float dentFreq = 60f, bool doubleSided = false)
        {
            m.SetFloat("_WaveAmp", 0f); m.SetFloat("_Stretch", 0f); m.SetFloat("_JiggleAmp", 0f);
            m.SetFloat("_RuffleAmp", 0f); m.SetFloat("_SkirtAmp", 0f); m.SetFloat("_BumpAmp", 0f);
            m.SetFloat("_BellyAmount", 0f); m.SetFloat("_PatternType", 0f);
            m.SetFloat("_Facet", 1f);
            m.SetFloat("_Bands", 3f);
            m.SetFloat("_BandSoftness", BandSoftness);
            m.SetFloat("_ToonSmooth", ToonSmooth);
            m.SetFloat("_AmbientStrength", Ambient);
            m.SetFloat("_RimAmount", rim);
            m.SetFloat("_RimPower", 3f);
            m.SetColor("_BaseColor", baseColor);
            m.SetColor("_ShadeColor", new Color(0.64f, 0.62f, 0.70f));
            m.SetColor("_OutlineColor", baseColor * 0.45f);
            m.SetFloat("_OutlineWidth", OutlineWidth);
            m.SetFloat("_MottleAmount", mottle);
            m.SetColor("_MottleColor", mottleColor ?? baseColor * 0.85f);
            m.SetFloat("_MottleScale", mottleScale);
            m.SetFloat("_DentAmp", dent);
            m.SetFloat("_DentFreq", dentFreq);
            m.SetFloat("_Alpha", 1f);
            m.SetFloat("_Cull", doubleSided ? 0f : 2f);
            EditorUtility.SetDirty(m);
        }

        /// <summary>FBX の中のマテリアル名を、作ったマテリアルにつなぎ直す。</summary>
        public static void Remap(string fbxPath, params Material[] mats)
        {
            var imp = (ModelImporter)AssetImporter.GetAtPath(fbxPath);
            imp.importNormals = ModelImporterNormals.Import;
            imp.importAnimation = false; imp.importCameras = false; imp.importLights = false;
            imp.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
            foreach (var m in mats)
                imp.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), m.name), m);
            imp.SaveAndReimport();
        }
    }
}
