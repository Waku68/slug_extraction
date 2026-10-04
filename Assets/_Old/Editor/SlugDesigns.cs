using System;
using System.Collections.Generic;
using UnityEngine;

namespace SlugDesign
{
    public class EarSpec
    {
        public float t = 0.85f;                 // body position the ear grows from
        public Vector3 d0 = new Vector3(0.35f, 1f, 0.1f);   // start direction (x: outward, y: up, z: forward) in body frame
        public Vector3 d1 = new Vector3(0.5f, 0.8f, -0.3f); // end direction
        public float roll = 0f;                 // rotate the directions around the body axis (deg), used by back tufts
        public bool mirrored = true;            // make a left/right pair
        public float length = 0.14f, baseR = 0.05f, tipR = 0.04f, flat = 1f, bulbR = 0f;
        public int radial = 8;
        public Color? tipColor;                 // colour of the ear (null = appendage colour)
    }

    public class HandSpec
    {
        public float t = 0.78f;
        public Vector3 rayDir = new Vector3(1f, -0.3f, 0.1f);   // from the body axis toward the shoulder
        public Vector3 outDir = new Vector3(0.6f, -0.2f, 0.75f); // direction the arm points when out
        public float armLength = 0.06f, armR = 0.032f, handR = 0.06f;
        public int radial = 10;
    }

    public class FaceSpec
    {
        public float headT = 0.85f;
        public float faceTilt = 0f;     // deg, turns the face from the body tip toward the belly side
        public float eyeYaw = 34f, eyePitch = 8f;
        public Vector3 eyeSize = new Vector3(0.06f, 0.075f, 0.04f);
        public float eyeScaleL = 1f, eyeScaleR = 1f;
        public bool mouth = true;
        public float mouthPitch = -22f, mouthHalfW = 0.05f, mouthDepth = 0.018f, mouthThick = 0.007f;
        public bool cheeks = true;
        public float cheekYaw = 58f, cheekPitch = -14f, cheekSize = 0.045f;
    }

    public class SlugDesignSpec
    {
        public string id, jpName, enName, kind;     // kind: "base" or "inverted"
        public string assumption, inversion;        // for inverted designs
        public string description;
        public BodySpec body;
        public FaceSpec face = new FaceSpec();
        public List<EarSpec> ears = new List<EarSpec>();
        public HandSpec hand = new HandSpec();
        public int appendageRadial = 8;
        public Action<Material, SlugDesignSpec> applyBody;      // sets the material parameters of the body
        public Action<Material, SlugDesignSpec> applyAppendage; // tweaks on top of the body material for ears / hands
        public int patternCells = 8;
        public float stitchDashLen = 0.035f;
        public Color accent = Color.white;
        public Color cheekColor = new Color(1f, 0.55f, 0.65f);
        public bool transparent;
        public Color eyeColor = new Color(0.1f, 0.08f, 0.16f);
    }

    public static class SlugDesigns
    {
        static Color C(string hex) { ColorUtility.TryParseHtmlString(hex, out var c); return c; }

        static void Set(Material m, string prop, Color c) { m.SetColor(prop, c); }

        public static List<SlugDesignSpec> All()
        {
            var list = new List<SlugDesignSpec>();

            // =============================================================== 1. daifuku
            list.Add(new SlugDesignSpec
            {
                id = "01_daifuku", jpName = "大福型", enName = "Daifuku", kind = "base",
                description = "短く丸い、もちっとした体。胴と頭の境目をなくした一つのまんじゅう型で、触角は小さなこぶ耳。",
                body = new BodySpec
                {
                    length = 0.62f, rings = 14, radial = 12, bottomFlat = 0.55f, capTail = 0.24f, capHead = 0.26f, endBias = 0.6f,
                    keys = new[] { new ProfileKey(0f, 0.16f, 0.14f), new ProfileKey(0.25f, 0.27f, 0.25f), new ProfileKey(0.6f, 0.31f, 0.29f), new ProfileKey(0.85f, 0.27f, 0.26f), new ProfileKey(1f, 0.2f, 0.2f) }
                },
                face = new FaceSpec { headT = 0.8f, eyeYaw = 33f, eyePitch = 10f, eyeSize = new Vector3(0.065f, 0.08f, 0.04f), mouthPitch = -24f, mouthHalfW = 0.05f, cheekYaw = 58f, cheekPitch = -14f, cheekSize = 0.05f },
                ears = { new EarSpec { t = 0.8f, d0 = new Vector3(0.45f, 1f, 0.05f), d1 = new Vector3(0.55f, 1f, -0.1f), length = 0.1f, baseR = 0.055f, tipR = 0.05f, radial = 10, tipColor = C("#FFB3C6") } },
                hand = new HandSpec { t = 0.72f, armLength = 0.04f, armR = 0.04f, handR = 0.065f, radial = 12 },
                appendageRadial = 10, patternCells = 8, accent = C("#FFB3C6"), cheekColor = C("#FF8FA8"),
                applyBody = (m, d) =>
                {
                    Set(m, "_BaseColor", C("#FFF3E8")); Set(m, "_ShadeColor", C("#D9B8C8")); Set(m, "_BellyColor", C("#FFC4D2")); m.SetFloat("_BellyAmount", 0.5f);
                    Set(m, "_RimColor", C("#FFFFFF")); m.SetFloat("_RimAmount", 0.35f);
                    m.SetFloat("_FuzzAmount", 0.35f);                 // powdery soft edge, like dusted mochi
                    Set(m, "_OutlineColor", C("#8A5F73")); m.SetFloat("_OutlineWidth", 0.009f);
                    m.SetFloat("_WaveAmp", 0.032f); m.SetFloat("_WaveFreq", 1.2f); m.SetFloat("_WaveSpeed", 0.7f); m.SetFloat("_WaveHeadFade", 0.6f);
                },
                applyAppendage = (m, d) => { Set(m, "_BaseColor", C("#FFC4D2")); }
            });

            // =============================================================== 2. ryusen
            list.Add(new SlugDesignSpec
            {
                id = "02_ryusen", jpName = "流線型", enName = "Streamline", kind = "base",
                description = "細長く低い、速そうなシルエット。尾は先細り、触角は後ろへ流れる細い耳。背中に走るレーシングストライプ。",
                body = new BodySpec
                {
                    length = 1.3f, rings = 22, radial = 10, bottomFlat = 0.45f, capTail = 0.34f, capHead = 0.1f, tailPointy = 0.85f, endBias = 0.45f,
                    keys = new[] { new ProfileKey(0f, 0.04f, 0.035f), new ProfileKey(0.2f, 0.1f, 0.085f), new ProfileKey(0.55f, 0.15f, 0.115f), new ProfileKey(0.85f, 0.145f, 0.115f), new ProfileKey(1f, 0.12f, 0.1f) }
                },
                face = new FaceSpec { headT = 0.9f, eyeYaw = 36f, eyePitch = 10f, eyeSize = new Vector3(0.05f, 0.064f, 0.032f), mouthPitch = -26f, mouthHalfW = 0.035f, mouthDepth = 0.012f, mouthThick = 0.005f, cheeks = false },
                ears = { new EarSpec { t = 0.86f, d0 = new Vector3(0.45f, 1f, -0.1f), d1 = new Vector3(0.45f, 0.25f, -1f), length = 0.3f, baseR = 0.028f, tipR = 0.012f, flat = 0.6f, radial = 8, tipColor = C("#FFF08A") } },
                hand = new HandSpec { t = 0.8f, armLength = 0.04f, armR = 0.024f, handR = 0.045f },
                appendageRadial = 8, patternCells = 1, accent = C("#FFF08A"),
                applyBody = (m, d) =>
                {
                    Set(m, "_BaseColor", C("#2FA8FF")); Set(m, "_ShadeColor", C("#6F7FE0")); Set(m, "_BellyColor", C("#D9F6FF")); m.SetFloat("_BellyAmount", 0.5f);
                    m.SetFloat("_PatternType", 3f); Set(m, "_PatternColor", C("#FFF08A")); m.SetFloat("_PatternSize", 0.14f);
                    m.SetFloat("_Bands", 2f); m.SetFloat("_BandSoftness", 0.03f);
                    Set(m, "_OutlineColor", C("#1A3D78")); m.SetFloat("_OutlineWidth", 0.008f);
                    m.SetFloat("_WaveAmp", 0.03f); m.SetFloat("_WaveFreq", 2.4f); m.SetFloat("_WaveSpeed", 1.6f); m.SetFloat("_WaveHeadFade", 0.78f);
                },
                applyAppendage = (m, d) => { Set(m, "_BaseColor", C("#FFF08A")); }
            });

            // =============================================================== 3. nuigurumi
            list.Add(new SlugDesignSpec
            {
                id = "03_nuigurumi", jpName = "ぬいぐるみ型", enName = "Plush", kind = "base",
                description = "フェルト地の質感に、背中と首まわりの縫い目。ふっくら綿を詰めた体、ボタンの目、パッチ模様。",
                body = new BodySpec
                {
                    length = 0.85f, rings = 16, radial = 12, bottomFlat = 0.6f, capTail = 0.26f, capHead = 0.24f, tailPointy = 0.15f, endBias = 0.6f,
                    keys = new[] { new ProfileKey(0f, 0.12f, 0.11f), new ProfileKey(0.2f, 0.2f, 0.19f), new ProfileKey(0.5f, 0.24f, 0.23f), new ProfileKey(0.8f, 0.245f, 0.235f), new ProfileKey(1f, 0.2f, 0.2f) }
                },
                face = new FaceSpec { headT = 0.82f, eyeYaw = 31f, eyePitch = 10f, eyeSize = new Vector3(0.055f, 0.055f, 0.035f), mouthPitch = -22f, mouthHalfW = 0.045f, cheekYaw = 56f, cheekPitch = -14f, cheekSize = 0.045f },
                ears = { new EarSpec { t = 0.84f, d0 = new Vector3(0.45f, 1f, 0f), d1 = new Vector3(0.6f, 1f, -0.1f), length = 0.17f, baseR = 0.07f, tipR = 0.065f, radial = 10 } },
                hand = new HandSpec { t = 0.74f, armLength = 0.05f, armR = 0.045f, handR = 0.065f, radial = 12 },
                appendageRadial = 10, patternCells = 6, stitchDashLen = 0.04f, accent = C("#8A4B2A"), cheekColor = C("#FF8C7A"),
                applyBody = (m, d) =>
                {
                    Set(m, "_BaseColor", C("#FFC83D")); Set(m, "_ShadeColor", C("#D98A6A")); Set(m, "_BellyColor", C("#FFF0C2")); m.SetFloat("_BellyAmount", 0.45f);
                    m.SetFloat("_PatternType", 1f); Set(m, "_PatternColor", C("#FF9A5C")); m.SetFloat("_PatternSize", 0.4f);
                    m.SetFloat("_FeltAmount", 0.4f); m.SetFloat("_FuzzAmount", 0.8f);
                    Set(m, "_StitchColor", C("#7A3F22")); m.SetFloat("_StitchAmount", 1f); m.SetFloat("_StitchWidth", 0.007f); m.SetFloat("_StitchRingT", 0.72f);
                    Set(m, "_OutlineColor", C("#A8662F")); m.SetFloat("_OutlineWidth", 0.006f);
                    m.SetFloat("_Bands", 3f); m.SetFloat("_BandSoftness", 0.12f);
                    m.SetFloat("_WaveAmp", 0.036f); m.SetFloat("_WaveFreq", 1.1f); m.SetFloat("_WaveSpeed", 0.6f); m.SetFloat("_WaveHeadFade", 0.55f);
                },
                applyAppendage = (m, d) => { m.SetFloat("_StitchRingT", 0f); }
            });

            // =============================================================== 4. clay
            list.Add(new SlugDesignSpec
            {
                id = "04_kurei", jpName = "クレイ型", enName = "Clay", kind = "base",
                description = "手でこねた粘土のでこぼこ。大きなうねりと指でつついたような細かいくぼみ、素朴な点々模様。左右の目の大きさも少し違う。",
                body = new BodySpec
                {
                    length = 0.82f, rings = 17, radial = 12, bottomFlat = 0.55f, capTail = 0.26f, capHead = 0.24f, tailPointy = 0.2f, endBias = 0.55f,
                    keys = new[] { new ProfileKey(0f, 0.1f, 0.09f), new ProfileKey(0.2f, 0.19f, 0.17f), new ProfileKey(0.5f, 0.25f, 0.23f), new ProfileKey(0.8f, 0.23f, 0.22f), new ProfileKey(1f, 0.19f, 0.19f) }
                },
                face = new FaceSpec { headT = 0.82f, eyeYaw = 32f, eyePitch = 9f, eyeSize = new Vector3(0.06f, 0.074f, 0.04f), eyeScaleL = 1.15f, eyeScaleR = 0.95f, mouthPitch = -22f, mouthHalfW = 0.05f, cheekYaw = 57f, cheekPitch = -14f, cheekSize = 0.045f },
                ears = { new EarSpec { t = 0.84f, d0 = new Vector3(0.4f, 1f, 0.1f), d1 = new Vector3(0.65f, 1f, -0.2f), length = 0.14f, baseR = 0.065f, tipR = 0.035f, radial = 9, tipColor = C("#E86A4A") } },
                hand = new HandSpec { t = 0.74f, armLength = 0.05f, armR = 0.04f, handR = 0.062f, radial = 10 },
                appendageRadial = 9, patternCells = 7, accent = C("#E86A4A"), cheekColor = C("#FF9477"),
                applyBody = (m, d) =>
                {
                    Set(m, "_BaseColor", C("#F28A5E")); Set(m, "_ShadeColor", C("#B56A7A")); Set(m, "_BellyColor", C("#FFC9A0")); m.SetFloat("_BellyAmount", 0.45f);
                    m.SetFloat("_PatternType", 1f); Set(m, "_PatternColor", C("#FFE3A3")); m.SetFloat("_PatternSize", 0.26f);
                    m.SetFloat("_BumpAmp", 0.02f); m.SetFloat("_BumpFreq", 4.5f);
                    m.SetFloat("_DentAmp", 0.9f); m.SetFloat("_DentFreq", 85f);
                    Set(m, "_OutlineColor", C("#7A3A3A")); m.SetFloat("_OutlineWidth", 0.009f);
                    m.SetFloat("_WaveAmp", 0.03f); m.SetFloat("_WaveFreq", 1.3f); m.SetFloat("_WaveSpeed", 0.7f); m.SetFloat("_WaveHeadFade", 0.55f);
                },
                applyAppendage = (m, d) => { Set(m, "_BaseColor", C("#E86A4A")); m.SetFloat("_BumpAmp", 0.01f); }
            });

            // =============================================================== 5. gummy
            list.Add(new SlugDesignSpec
            {
                id = "05_gummy", jpName = "グミ型", enName = "Gummy", kind = "base", transparent = true,
                description = "少し半透明でぷるぷる。ふちが濃く中心が透ける色味、砂糖粒のきらめき。テカりではなく透け感で見せる。",
                body = new BodySpec
                {
                    length = 0.72f, rings = 15, radial = 12, bottomFlat = 0.55f, capTail = 0.25f, capHead = 0.25f, endBias = 0.6f,
                    keys = new[] { new ProfileKey(0f, 0.1f, 0.09f), new ProfileKey(0.2f, 0.2f, 0.18f), new ProfileKey(0.5f, 0.25f, 0.23f), new ProfileKey(0.8f, 0.23f, 0.22f), new ProfileKey(1f, 0.18f, 0.18f) }
                },
                face = new FaceSpec { headT = 0.8f, eyeYaw = 33f, eyePitch = 10f, eyeSize = new Vector3(0.062f, 0.078f, 0.04f), mouthPitch = -24f, mouthHalfW = 0.048f, cheekYaw = 57f, cheekPitch = -14f, cheekSize = 0.045f, eyeScaleL = 1f },
                ears = { new EarSpec { t = 0.82f, d0 = new Vector3(0.4f, 1f, 0.05f), d1 = new Vector3(0.6f, 1f, -0.15f), length = 0.14f, baseR = 0.06f, tipR = 0.045f, radial = 10 } },
                hand = new HandSpec { t = 0.72f, armLength = 0.045f, armR = 0.04f, handR = 0.062f, radial = 12 },
                appendageRadial = 10, patternCells = 9, accent = C("#C79BFF"), cheekColor = C("#FF8FCB"),
                applyBody = (m, d) =>
                {
                    Set(m, "_BaseColor", C("#A25CFF")); Set(m, "_ShadeColor", C("#7E55D8")); Set(m, "_BellyColor", C("#C9A0FF")); m.SetFloat("_BellyAmount", 0.5f);
                    m.SetFloat("_PatternType", 4f); Set(m, "_PatternColor", C("#FFFFFF")); m.SetFloat("_PatternSize", 0.3f); m.SetFloat("_SparkleStrength", 1.4f);
                    Set(m, "_RimColor", C("#F0DAFF")); m.SetFloat("_RimAmount", 0.5f); m.SetFloat("_RimPower", 2.5f);
                    m.SetFloat("_Alpha", 0.58f); m.SetFloat("_AlphaEdge", 1.9f);
                    m.SetFloat("_JiggleAmp", 0.007f); m.SetFloat("_JiggleFreq", 10f); m.SetFloat("_JiggleSpeed", 8f);
                    m.SetFloat("_OutlineWidth", 0f);
                    m.SetFloat("_Bands", 3f); m.SetFloat("_BandSoftness", 0.1f);
                    m.SetFloat("_WaveAmp", 0.034f); m.SetFloat("_WaveFreq", 1.2f); m.SetFloat("_WaveSpeed", 0.8f); m.SetFloat("_WaveHeadFade", 0.55f);
                },
                applyAppendage = (m, d) => { m.SetFloat("_PatternType", 0f); }
            });

            // =============================================================== 6. tachiagari (inverted: crawls flat on the ground)
            list.Add(new SlugDesignSpec
            {
                id = "06_tachiagari", jpName = "立ち上がり型", enName = "Rear-up", kind = "inverted",
                assumption = "ナメクジは地面にぺたりと水平に這っている",
                inversion = "上体を高く起こして立つ。尻尾側だけが地面を這い、顔と手が目線の高さに来る。",
                description = "鎌首をもたげたように体を起こし、胸を前に出して立つ。手を使う姿がいちばん見やすい。",
                body = new BodySpec
                {
                    length = 1.1f, rings = 20, radial = 10, bottomFlat = 0.6f, capTail = 0.2f, capHead = 0.2f, tailPointy = 0.3f, endBias = 0.5f, bendDeg = 88f, bendStart = 0.28f,
                    keys = new[] { new ProfileKey(0f, 0.09f, 0.08f), new ProfileKey(0.2f, 0.15f, 0.13f), new ProfileKey(0.5f, 0.17f, 0.16f), new ProfileKey(0.8f, 0.17f, 0.16f), new ProfileKey(1f, 0.16f, 0.16f) }
                },
                face = new FaceSpec { headT = 0.86f, faceTilt = 62f, eyeYaw = 34f, eyePitch = 6f, eyeSize = new Vector3(0.058f, 0.072f, 0.038f), mouthPitch = -24f, mouthHalfW = 0.045f, cheekYaw = 58f, cheekPitch = -14f, cheekSize = 0.042f },
                ears = { new EarSpec { t = 0.9f, d0 = new Vector3(0.5f, 1f, -0.2f), d1 = new Vector3(0.7f, 1f, -0.5f), length = 0.17f, baseR = 0.045f, tipR = 0.03f, radial = 9, tipColor = C("#FFE066") } },
                hand = new HandSpec { t = 0.7f, rayDir = new Vector3(1f, 0.2f, -0.8f), outDir = new Vector3(0.7f, -0.35f, -1f), armLength = 0.07f, armR = 0.035f, handR = 0.058f, radial = 10 },
                appendageRadial = 9, patternCells = 8, accent = C("#FFE066"),
                applyBody = (m, d) =>
                {
                    Set(m, "_BaseColor", C("#52DDA6")); Set(m, "_ShadeColor", C("#4F8FB5")); Set(m, "_BellyColor", C("#E6FFE9")); m.SetFloat("_BellyAmount", 0.5f);
                    m.SetFloat("_PatternType", 2f); Set(m, "_PatternColor", C("#2EBF8C")); m.SetFloat("_PatternSize", 0.38f);
                    Set(m, "_OutlineColor", C("#1F6B63")); m.SetFloat("_OutlineWidth", 0.008f);
                    m.SetFloat("_WaveAmp", 0.034f); m.SetFloat("_WaveFreq", 1.5f); m.SetFloat("_WaveSpeed", 0.9f); m.SetFloat("_WaveHeadFade", 0.4f);
                },
                applyAppendage = (m, d) => { Set(m, "_BaseColor", C("#E6FFE9")); }
            });

            // =============================================================== 7. atamadekkachi (inverted: small head, long trunk)
            list.Add(new SlugDesignSpec
            {
                id = "07_atamadekkachi", jpName = "頭でっかち型", enName = "Big-head", kind = "inverted",
                assumption = "体の大半は長い胴で、頭は先端のちいさなふくらみにすぎない",
                inversion = "頭が体の主役。大きな丸い顔にちいさな胴がくっつく2頭身。目も大きくなる。",
                description = "顔がほとんど全身。キャラとして表情が一番伝わる反面、這う体という印象は薄れる。",
                body = new BodySpec
                {
                    length = 0.95f, rings = 18, radial = 12, bottomFlat = 0.62f, capTail = 0.22f, capHead = 0.2f, tailPointy = 0.2f, endBias = 0.5f,
                    keys = new[] { new ProfileKey(0f, 0.07f, 0.065f), new ProfileKey(0.2f, 0.1f, 0.095f), new ProfileKey(0.45f, 0.12f, 0.115f), new ProfileKey(0.6f, 0.2f, 0.19f), new ProfileKey(0.75f, 0.33f, 0.3f), new ProfileKey(0.9f, 0.34f, 0.31f), new ProfileKey(1f, 0.28f, 0.27f) }
                },
                face = new FaceSpec { headT = 0.78f, eyeYaw = 30f, eyePitch = 5f, eyeSize = new Vector3(0.1f, 0.125f, 0.055f), mouthPitch = -26f, mouthHalfW = 0.075f, mouthDepth = 0.028f, mouthThick = 0.01f, cheekYaw = 58f, cheekPitch = -16f, cheekSize = 0.07f },
                ears = { new EarSpec { t = 0.8f, d0 = new Vector3(0.55f, 1f, 0f), d1 = new Vector3(0.8f, 0.9f, -0.2f), length = 0.24f, baseR = 0.07f, tipR = 0.055f, bulbR = 0.075f, radial = 10, tipColor = C("#FFD84A") } },
                hand = new HandSpec { t = 0.55f, rayDir = new Vector3(1f, -0.25f, 0.3f), outDir = new Vector3(0.55f, -0.15f, 0.8f), armLength = 0.06f, armR = 0.032f, handR = 0.05f, radial = 10 },
                appendageRadial = 10, patternCells = 7, accent = C("#FFD84A"), cheekColor = C("#FF7F6A"),
                applyBody = (m, d) =>
                {
                    Set(m, "_BaseColor", C("#FF9A33")); Set(m, "_ShadeColor", C("#C7645F")); Set(m, "_BellyColor", C("#FFE6B0")); m.SetFloat("_BellyAmount", 0.45f);
                    m.SetFloat("_PatternType", 1f); Set(m, "_PatternColor", C("#FFD84A")); m.SetFloat("_PatternSize", 0.3f);
                    Set(m, "_OutlineColor", C("#8A4A22")); m.SetFloat("_OutlineWidth", 0.009f);
                    m.SetFloat("_WaveAmp", 0.03f); m.SetFloat("_WaveFreq", 1.8f); m.SetFloat("_WaveSpeed", 0.9f); m.SetFloat("_WaveHeadFade", 0.4f);
                },
                applyAppendage = (m, d) => { Set(m, "_BaseColor", C("#FFD84A")); }
            });

            // =============================================================== 8. umiushi (inverted: plain protective colours)
            list.Add(new SlugDesignSpec
            {
                id = "08_umiushi", jpName = "ウミウシ型", enName = "Sea-slug", kind = "inverted",
                assumption = "体の色は地味な単色（茶・灰・黒）で、飾りはない",
                inversion = "ポップな多色。幅広い体のふちにフリルが波打ち、触角は先が丸い花のよう、背中にふさふさの突起。",
                description = "カラナクシのようにウミウシ寄せ。色と模様のパラメータで遊べる幅が最も大きい。",
                body = new BodySpec
                {
                    length = 1.0f, rings = 64, radial = 14, bottomFlat = 0.4f, capTail = 0.2f, capHead = 0.2f, tailPointy = 0.25f, endBias = 0.3f,
                    keys = new[] { new ProfileKey(0f, 0.1f, 0.06f), new ProfileKey(0.2f, 0.24f, 0.09f), new ProfileKey(0.5f, 0.3f, 0.11f), new ProfileKey(0.8f, 0.28f, 0.11f), new ProfileKey(1f, 0.2f, 0.1f) }
                },
                face = new FaceSpec { headT = 0.86f, eyeYaw = 32f, eyePitch = 12f, eyeSize = new Vector3(0.058f, 0.068f, 0.032f), mouthPitch = -28f, mouthHalfW = 0.05f, mouthDepth = 0.014f, mouthThick = 0.006f, cheekYaw = 62f, cheekPitch = -14f, cheekSize = 0.04f },
                ears =
                {
                    new EarSpec { t = 0.8f, d0 = new Vector3(0.3f, 1f, 0.25f), d1 = new Vector3(0.55f, 1f, 0.05f), length = 0.17f, baseR = 0.025f, tipR = 0.02f, bulbR = 0.045f, radial = 9, tipColor = C("#32E6FF") },
                    new EarSpec { t = 0.3f, d0 = new Vector3(0f, 1f, -0.1f), d1 = new Vector3(0f, 1f, -0.4f), roll = -35f, mirrored = false, length = 0.12f, baseR = 0.02f, tipR = 0.015f, bulbR = 0.03f, radial = 8, tipColor = C("#FFE24A") },
                    new EarSpec { t = 0.3f, d0 = new Vector3(0f, 1f, -0.1f), d1 = new Vector3(0f, 1f, -0.4f), roll = 0f, mirrored = false, length = 0.14f, baseR = 0.02f, tipR = 0.015f, bulbR = 0.032f, radial = 8, tipColor = C("#FFE24A") },
                    new EarSpec { t = 0.3f, d0 = new Vector3(0f, 1f, -0.1f), d1 = new Vector3(0f, 1f, -0.4f), roll = 35f, mirrored = false, length = 0.12f, baseR = 0.02f, tipR = 0.015f, bulbR = 0.03f, radial = 8, tipColor = C("#FFE24A") }
                },
                hand = new HandSpec { t = 0.8f, armLength = 0.05f, armR = 0.03f, handR = 0.05f },
                appendageRadial = 9, patternCells = 9, accent = C("#32E6FF"), cheekColor = C("#FF9ACB"),
                applyBody = (m, d) =>
                {
                    Set(m, "_BaseColor", C("#FF5FA2")); Set(m, "_ShadeColor", C("#8A44B8")); Set(m, "_BellyColor", C("#FFE066")); m.SetFloat("_BellyAmount", 0.4f);
                    m.SetFloat("_PatternType", 1f); Set(m, "_PatternColor", C("#32E6FF")); m.SetFloat("_PatternSize", 0.34f);
                    m.SetFloat("_RuffleAmp", 0.016f); m.SetFloat("_RuffleFreq", 10f); m.SetFloat("_RuffleSpeed", 2.5f);
                    Set(m, "_OutlineColor", C("#6B1A55")); m.SetFloat("_OutlineWidth", 0.008f);
                    m.SetFloat("_WaveAmp", 0.02f); m.SetFloat("_WaveFreq", 2f); m.SetFloat("_WaveSpeed", 1f); m.SetFloat("_WaveHeadFade", 0.6f);
                },
                applyAppendage = (m, d) => { Set(m, "_BaseColor", C("#FF8FC1")); m.SetFloat("_RuffleAmp", 0f); }
            });

            // =============================================================== 9. kesshou (inverted: soft smooth surface)
            list.Add(new SlugDesignSpec
            {
                id = "09_kesshou", jpName = "結晶型", enName = "Crystal", kind = "inverted",
                assumption = "体表はやわらかく、なめらかな曲面でできている",
                inversion = "かたい塩の結晶。面を切り出したファセット形状で、光は面ごとに段になる。輪郭だけは丸く保つ。",
                description = "岩塩を磨いたようなカット面。塩がテーマの世界観と相性がよく、粘液のキラキラは結晶のきらめきに置き換わる。",
                body = new BodySpec
                {
                    length = 0.8f, rings = 9, radial = 8, bottomFlat = 0.5f, capTail = 0.26f, capHead = 0.26f, tailPointy = 0.1f, endBias = 0.5f,
                    keys = new[] { new ProfileKey(0f, 0.08f, 0.07f), new ProfileKey(0.2f, 0.17f, 0.15f), new ProfileKey(0.5f, 0.22f, 0.2f), new ProfileKey(0.8f, 0.21f, 0.19f), new ProfileKey(1f, 0.17f, 0.17f) }
                },
                face = new FaceSpec { headT = 0.8f, eyeYaw = 33f, eyePitch = 10f, eyeSize = new Vector3(0.06f, 0.074f, 0.04f), mouthPitch = -22f, mouthHalfW = 0.045f, cheekYaw = 57f, cheekPitch = -14f, cheekSize = 0.04f, cheeks = false },
                ears = { new EarSpec { t = 0.82f, d0 = new Vector3(0.4f, 1f, 0.1f), d1 = new Vector3(0.5f, 1f, -0.1f), length = 0.17f, baseR = 0.06f, tipR = 0.025f, radial = 5, tipColor = C("#E8F7FF") } },
                hand = new HandSpec { t = 0.72f, armLength = 0.045f, armR = 0.036f, handR = 0.056f, radial = 6 },
                appendageRadial = 5, patternCells = 12, accent = C("#E8F7FF"),
                applyBody = (m, d) =>
                {
                    Set(m, "_BaseColor", C("#8FD3FF")); Set(m, "_ShadeColor", C("#6A78D8")); Set(m, "_BellyColor", C("#F2FBFF")); m.SetFloat("_BellyAmount", 0.5f);
                    m.SetFloat("_PatternType", 4f); Set(m, "_PatternColor", C("#FFFFFF")); m.SetFloat("_PatternSize", 0.3f); m.SetFloat("_SparkleStrength", 1.6f);
                    m.SetFloat("_Facet", 1f);
                    m.SetFloat("_Bands", 3f); m.SetFloat("_BandSoftness", 0.02f);
                    Set(m, "_RimColor", C("#FFFFFF")); m.SetFloat("_RimAmount", 0.4f);
                    Set(m, "_OutlineColor", C("#27407A")); m.SetFloat("_OutlineWidth", 0.008f);
                    m.SetFloat("_WaveAmp", 0.02f); m.SetFloat("_WaveFreq", 1.3f); m.SetFloat("_WaveSpeed", 0.8f); m.SetFloat("_WaveHeadFade", 0.55f);
                },
                applyAppendage = (m, d) => { Set(m, "_BaseColor", C("#E8F7FF")); m.SetFloat("_PatternType", 0f); }
            });

            return list;
        }
    }
}
