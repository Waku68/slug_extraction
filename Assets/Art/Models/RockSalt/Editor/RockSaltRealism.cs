using UnityEditor;
using UnityEngine;

namespace SlugDesign
{
    /// <summary>
    /// 岩塩（第1版の形）の質感を、リアル度 0（第1版のポップ）〜 1（リアル）と、
    /// 透明度 0（不透明）〜 1（中心が透ける）で作る。形は同じで、Slug/Toon のマテリアルの値だけを変える。
    /// 透明度は結晶だけ（岩は不透明のまま）。
    /// </summary>
    public static class RockSaltRealism
    {
        const string Dir = "Assets/Art/Models/RockSalt/Realism";
        /// <summary>見比べる段（リアル度・透明度とも 1〜0 を 0.2 ずつ）。</summary>
        public static readonly float[] Levels = { 1f, 0.8f, 0.6f, 0.4f, 0.2f, 0f };

        /// <summary>結晶とも岩とも共通の値（0 と 1 の端）。</summary>
        struct Look
        {
            public Color baseColor, shade, outline, mottle;
            public float outlineWidth, bandSoftness, toonSmooth, ambient, rim;
            public float mottleAmount, mottleScale, dentAmp, dentFreq;
            public float sparkleSize, sparkleCells; // 0 ならキラキラなし
        }

        // 結晶：0 は第1版（くすんだピンク、太い輪郭線、段のはっきりしたトゥーン）、1 は白っぽく透けた本物の岩塩
        static readonly Look CrystalPop = new Look
        {
            baseColor = new Color(0.82f, 0.68f, 0.64f), shade = new Color(0.56f, 0.46f, 0.60f),
            outline = new Color(0.30f, 0.20f, 0.22f), mottle = new Color(0.82f, 0.68f, 0.64f),
            outlineWidth = 0.009f, bandSoftness = 0.06f, toonSmooth = 0f, ambient = 0.25f, rim = 0.25f,
            mottleAmount = 0f, mottleScale = 14f, dentAmp = 0f, dentFreq = 40f,
            sparkleSize = 0.12f, sparkleCells = 14f,
        };
        static readonly Look CrystalReal = new Look
        {
            baseColor = new Color(0.93f, 0.88f, 0.86f), shade = new Color(0.66f, 0.62f, 0.70f),
            outline = new Color(0.55f, 0.48f, 0.48f), mottle = new Color(0.80f, 0.64f, 0.62f),
            outlineWidth = 0.002f, bandSoftness = 0.35f, toonSmooth = 0.85f, ambient = 0.45f, rim = 0.45f,
            mottleAmount = 0.5f, mottleScale = 14f, dentAmp = 0.25f, dentFreq = 40f,
            sparkleSize = 0.03f, sparkleCells = 24f,
        };

        // 岩：0 は第1版（苔色のまだらが強い）、1 は細かい凹みのある暗い岩
        static readonly Look RockPop = new Look
        {
            baseColor = new Color(0.30f, 0.28f, 0.24f), shade = new Color(0.55f, 0.55f, 0.66f),
            outline = new Color(0.16f, 0.15f, 0.12f), mottle = new Color(0.43f, 0.48f, 0.30f),
            outlineWidth = 0.009f, bandSoftness = 0.06f, toonSmooth = 0f, ambient = 0.35f, rim = 0.1f,
            mottleAmount = 0.8f, mottleScale = 6f, dentAmp = 0f, dentFreq = 70f,
        };
        static readonly Look RockReal = new Look
        {
            baseColor = new Color(0.27f, 0.25f, 0.22f), shade = new Color(0.62f, 0.62f, 0.68f),
            outline = new Color(0.10f, 0.09f, 0.08f), mottle = new Color(0.38f, 0.39f, 0.30f),
            outlineWidth = 0.002f, bandSoftness = 0.35f, toonSmooth = 0.85f, ambient = 0.45f, rim = 0.1f,
            mottleAmount = 0.45f, mottleScale = 9f, dentAmp = 1.0f, dentFreq = 70f,
        };

        /// <summary>決めた質感（Oct 5、6×6 の見比べ）：リアル度 0.2（ポップ寄り）、透明なし。</summary>
        public const float ChosenRealism = 0.2f, ChosenTranslucency = 0f;

        /// <summary>決めた質感を、本番のマテリアル（FBX がつながっている RockSalt_Crystal / RockSalt_Rock）に入れる。</summary>
        [MenuItem("Slug/RockSalt Apply Chosen Look")]
        public static void ApplyChosen()
        {
            var (crystal, rock) = Build(ChosenRealism, ChosenTranslucency);
            var mainCrystal = AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Models/RockSalt/RockSalt_Crystal.mat");
            var mainRock = AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Models/RockSalt/RockSalt_Rock.mat");
            mainCrystal.CopyPropertiesFromMaterial(crystal);
            mainRock.CopyPropertiesFromMaterial(rock);
            mainCrystal.renderQueue = -1;
            EditorUtility.SetDirty(mainCrystal); EditorUtility.SetDirty(mainRock);
            AssetDatabase.SaveAssets();
        }

        [MenuItem("Slug/RockSalt Build Realism Materials")]
        public static void BuildAll()
        {
            foreach (var r in Levels)
                foreach (var t in Levels)
                    Build(r, t);
            AssetDatabase.SaveAssets();
        }

        /// <summary>リアル度 r・透明度 t のマテリアル（結晶、岩）を作るか更新する。岩はリアル度だけで決まる。</summary>
        public static (Material crystal, Material rock) Build(float realism, float translucency)
        {
            if (!AssetDatabase.IsValidFolder(Dir))
                AssetDatabase.CreateFolder("Assets/Art/Models/RockSalt", "Realism");
            string r = "R" + Mathf.RoundToInt(realism * 10).ToString("00");
            string t = "T" + Mathf.RoundToInt(translucency * 10).ToString("00");
            var crystal = GetOrCreate("RockSalt_Crystal_" + r + "_" + t);
            var rock = GetOrCreate("RockSalt_Rock_" + r);
            Apply(crystal, CrystalPop, CrystalReal, realism);
            ApplyTranslucency(crystal, translucency);
            Apply(rock, RockPop, RockReal, realism);
            return (crystal, rock);
        }

        /// <summary>
        /// 透明感：中心ほど透けてふちは濃いまま（ガラスや結晶らしく）。光を通しているように、ふちの光を強め影を明るくする。
        /// 輪郭線は内側の殻で描いているので、透けると中から見えてしまう。透明度に合わせて細く薄くする。
        /// </summary>
        static void ApplyTranslucency(Material m, float t)
        {
            if (t <= 0f)
            {
                m.SetFloat("_Alpha", 1f);
                m.SetFloat("_Surface", 0f);
                m.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.One);
                m.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.Zero);
                m.SetFloat("_ZWrite", 1f);
                m.SetOverrideTag("RenderType", "Opaque");
                m.renderQueue = -1;
                return;
            }
            m.SetFloat("_Alpha", Mathf.Lerp(1f, 0.3f, t));
            m.SetFloat("_AlphaEdge", 1.6f);
            m.SetFloat("_Surface", 1f);
            m.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            m.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            m.SetFloat("_ZWrite", 1f); // 結晶どうしが重なっても奥の面が混ざらないように、前の面だけ描く
            m.SetOverrideTag("RenderType", "Transparent");
            m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;

            m.SetFloat("_RimAmount", Mathf.Lerp(m.GetFloat("_RimAmount"), 0.7f, t));
            m.SetFloat("_RimPower", Mathf.Lerp(3f, 2f, t));
            m.SetColor("_ShadeColor", Color.Lerp(m.GetColor("_ShadeColor"), new Color(0.85f, 0.82f, 0.9f), t * 0.6f));
            m.SetFloat("_OutlineWidth", m.GetFloat("_OutlineWidth") * (1f - t));
            m.SetColor("_OutlineColor", Color.Lerp(m.GetColor("_OutlineColor"), m.GetColor("_BaseColor"), t));
            EditorUtility.SetDirty(m);
        }

        static Material GetOrCreate(string name)
        {
            string path = Dir + "/" + name + ".mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null)
            {
                m = new Material(Shader.Find("Slug/Toon"));
                AssetDatabase.CreateAsset(m, path);
            }
            return m;
        }

        static void Apply(Material m, Look a, Look b, float t)
        {
            m.shader = Shader.Find("Slug/Toon");
            // 置いた物なので、這う動きと体の形の変形は全部 0
            m.SetFloat("_WaveAmp", 0f); m.SetFloat("_Stretch", 0f); m.SetFloat("_JiggleAmp", 0f);
            m.SetFloat("_RuffleAmp", 0f); m.SetFloat("_SkirtAmp", 0f); m.SetFloat("_BumpAmp", 0f);
            m.SetFloat("_BellyAmount", 0f);
            m.SetFloat("_Facet", 1f); // 結晶の平らな面は、リアルでも残す
            m.SetFloat("_Bands", 3f);
            m.SetFloat("_RimPower", 3f);

            m.SetColor("_BaseColor", Color.Lerp(a.baseColor, b.baseColor, t));
            m.SetColor("_ShadeColor", Color.Lerp(a.shade, b.shade, t));
            m.SetColor("_OutlineColor", Color.Lerp(a.outline, b.outline, t));
            m.SetFloat("_OutlineWidth", Mathf.Lerp(a.outlineWidth, b.outlineWidth, t));
            m.SetFloat("_BandSoftness", Mathf.Lerp(a.bandSoftness, b.bandSoftness, t));
            m.SetFloat("_ToonSmooth", Mathf.Lerp(a.toonSmooth, b.toonSmooth, t));
            m.SetFloat("_AmbientStrength", Mathf.Lerp(a.ambient, b.ambient, t));
            m.SetFloat("_RimAmount", Mathf.Lerp(a.rim, b.rim, t));
            m.SetColor("_MottleColor", Color.Lerp(a.mottle, b.mottle, t));
            m.SetFloat("_MottleAmount", Mathf.Lerp(a.mottleAmount, b.mottleAmount, t));
            m.SetFloat("_MottleScale", Mathf.Lerp(a.mottleScale, b.mottleScale, t));
            m.SetFloat("_DentAmp", Mathf.Lerp(a.dentAmp, b.dentAmp, t));
            m.SetFloat("_DentFreq", Mathf.Lerp(a.dentFreq, b.dentFreq, t));

            float sparkle = Mathf.Lerp(a.sparkleSize, b.sparkleSize, t);
            m.SetFloat("_PatternType", sparkle > 0f ? 4f : 0f);
            m.SetColor("_PatternColor", new Color(1f, 0.97f, 0.95f));
            float cells = Mathf.Lerp(a.sparkleCells, b.sparkleCells, t);
            m.SetVector("_PatternScale", new Vector4(cells, cells, 0, 0));
            m.SetFloat("_PatternSize", sparkle);
            EditorUtility.SetDirty(m);
        }
    }
}
