// URP (HLSL) toon shader for the slug character candidates.
//  - flat toon bands, matte (no specular / no wet shine)
//  - bone-less crawl: vertex wave travelling tail -> head
//  - parameters for felt / stitches / clay bumps / gummy translucency / frills / facets
Shader "Slug/Toon"
{
    Properties
    {
        [Header(Color)]
        _BaseColor("Base Color", Color) = (1, 0.72, 0.8, 1)
        _BaseMap("Base Map (optional, multiplies the base color; uv0)", 2D) = "white" {}
        _ShadeColor("Shade Tint (multiplied in shadow)", Color) = (0.72, 0.6, 0.78, 1)
        _BellyColor("Belly Color", Color) = (1, 0.9, 0.85, 1)
        _BellyAmount("Belly Width (0 = off)", Range(0, 1)) = 0

        [Header(Pattern)]
        _PatternType("Pattern 0 none 1 spots 2 rings 3 stripes 4 sparkle", Range(0, 4)) = 0
        _PatternColor("Pattern Color", Color) = (1, 1, 1, 1)
        _PatternScale("Pattern Cells (around, along)", Vector) = (10, 10, 0, 0)
        _PatternSize("Pattern Size", Range(0, 1)) = 0.3

        [Header(Toon Lighting)]
        _Bands("Light Bands", Range(1, 5)) = 3
        _BandSoftness("Band Softness", Range(0.001, 0.5)) = 0.06
        _AmbientStrength("Ambient Strength", Range(0, 1)) = 0.35
        _RimColor("Rim Color", Color) = (1, 1, 1, 1)
        _RimPower("Rim Power", Range(0.5, 8)) = 3
        _RimAmount("Rim Amount", Range(0, 1)) = 0.25

        [Header(Outline)]
        _OutlineColor("Outline Color", Color) = (0.25, 0.15, 0.25, 1)
        _OutlineWidth("Outline Width (0 = off)", Range(0, 0.05)) = 0.01

        [Header(Crawl Wave)]
        _BodyLength("Body Length", Float) = 1
        _WaveAmp("Wave Amplitude", Range(0, 0.15)) = 0.03
        _WaveFreq("Wave Count Along Body", Range(0, 6)) = 1.5
        _WaveSpeed("Wave Speed", Range(-4, 4)) = 0.8
        _WavePhase("Wave Phase", Range(0, 6.2832)) = 0
        _WaveHeadFade("Wave Fade Start (0 tail - 1 head)", Range(0, 1)) = 0.55

        [Header(Surface Detail)]
        _BumpAmp("Vertex Lumps (clay)", Range(0, 0.1)) = 0
        _BumpFreq("Vertex Lump Frequency", Range(0.5, 20)) = 4
        _DentAmp("Fine Dents (clay)", Range(0, 2)) = 0
        _DentFreq("Fine Dent Frequency", Range(10, 300)) = 90
        _FeltAmount("Felt Grain", Range(0, 1)) = 0
        _FuzzAmount("Felt Fuzz", Range(0, 1)) = 0

        [Header(Stitches)]
        _StitchColor("Stitch Color", Color) = (0.3, 0.2, 0.15, 1)
        _StitchAmount("Stitch Amount", Range(0, 1)) = 0
        _StitchDash("Stitch Dashes", Range(2, 80)) = 24
        _StitchWidth("Stitch Width", Range(0.001, 0.03)) = 0.006
        _StitchRingT("Neck Seam Position (0 = off)", Range(0, 1)) = 0

        [Header(Facets)]
        [Toggle] _Facet("Flat Facets", Float) = 0

        [Header(Gummy)]
        _Alpha("Core Opacity (1 = opaque)", Range(0, 1)) = 1
        _AlphaEdge("Edge Opacity Boost", Range(0, 3)) = 1.5
        _JiggleAmp("Jiggle Amplitude", Range(0, 0.05)) = 0
        _JiggleFreq("Jiggle Spatial Freq", Range(1, 30)) = 9
        _JiggleSpeed("Jiggle Speed", Range(0, 30)) = 9

        [Header(Frills)]
        _RuffleAmp("Ruffle Amplitude", Range(0, 0.1)) = 0
        _RuffleFreq("Ruffle Count", Range(1, 40)) = 14
        _RuffleSpeed("Ruffle Speed", Range(-10, 10)) = 2

        [Header(Emission)]
        [HDR] _EmissionColor("Emission", Color) = (0, 0, 0, 1)
        _SparkleStrength("Sparkle Strength", Range(0, 3)) = 1

        [Header(Realism)]
        _ToonSmooth("Smooth Shading (0 stepped toon - 1 gradient)", Range(0, 1)) = 0
        _WrinkleAmount("Skin Grooves", Range(0, 1)) = 0
        _WrinkleScale("Groove Cells (around, along)", Vector) = (18, 18, 0, 0)
        _MottleAmount("Mottling", Range(0, 1)) = 0
        _MottleColor("Mottle Color", Color) = (0.2, 0.19, 0.15, 1)
        _MottleScale("Mottle Scale", Range(1, 30)) = 8
        _SkirtAmp("Foot Fringe Wave", Range(0, 0.03)) = 0
        _SkirtFreq("Foot Fringe Waves Along", Range(1, 60)) = 18
        _SkirtU("Foot Fringe Edge (from sole middle)", Range(0, 0.5)) = 0.14
        _SkirtWidth("Foot Fringe Width", Range(0.005, 0.2)) = 0.06
        _SkirtLines("Foot Fringe Pleat Lines", Range(0, 1)) = 0.35
        _SkirtEnd("Foot Fringe Ends At (t)", Range(0, 1)) = 0.6
        _CamoAmount("Camouflage (worn gear)", Range(0, 1)) = 0
        _CamoColor1("Camo Color 1", Color) = (0.42, 0.29, 0.17, 1)
        _CamoColor2("Camo Color 2", Color) = (0.16, 0.15, 0.11, 1)
        _CamoColor3("Camo Color 3", Color) = (0.64, 0.58, 0.38, 1)
        _CamoScale("Camo Scale", Range(1, 60)) = 14

        [HideInInspector] _EyeC("Eye Centre", Vector) = (0, 0, 0, 0)
        [HideInInspector] _EyeR("Eye Right", Vector) = (1, 0, 0, 0)
        [HideInInspector] _EyeU("Eye Up", Vector) = (0, 1, 0, 0)
        [HideInInspector] _EyeShape("Eye Shape", Vector) = (0, 0.02, 0.02, 0)
        [HideInInspector] _EyeP("Eye Params", Vector) = (0.15, 0.25, 0.003, 0)
        [HideInInspector] _EyeColor("Eye Color", Color) = (0.05, 0.045, 0.05, 1)
        [HideInInspector] _EyeRimColor("Eye Rim Color", Color) = (0.92, 0.92, 0.86, 1)
        [HideInInspector] _MouthC("Mouth Centre", Vector) = (0, 0, 0, 0)
        [HideInInspector] _MouthR("Mouth Right", Vector) = (1, 0, 0, 0)
        [HideInInspector] _MouthU("Mouth Up", Vector) = (0, 1, 0, 0)
        [HideInInspector] _MouthP("Mouth Params", Vector) = (0.04, 0.015, 0.004, 0)
        [HideInInspector] _MouthColor("Mouth Color", Color) = (0.1, 0.08, 0.06, 1)
        [HideInInspector] _EyeKind("Eye Kind", Float) = 0
        [HideInInspector] _EyeFC("Eye Frame Centre", Vector) = (0, 0, 0, 0)
        [HideInInspector] _EyeFX("Eye Frame X", Vector) = (1, 0, 0, 0)
        [HideInInspector] _EyeFY("Eye Frame Y", Vector) = (0, 1, 0, 0)
        [HideInInspector] _EyeFZ("Eye Frame Z", Vector) = (0, 0, 1, 0)
        [HideInInspector] _EyeRad("Eye Radius", Float) = 0.03
        [HideInInspector] _EyeIris1("Iris Color 1", Color) = (0.85, 0.7, 0.2, 1)
        [HideInInspector] _EyeIris2("Iris Color 2", Color) = (0.35, 0.4, 0.15, 1)
        [HideInInspector] _EyeSclera("Sclera Color", Color) = (0.8, 0.82, 0.8, 1)
        [HideInInspector] _Stretch("Stretch", Float) = 0
        [HideInInspector] _TailPivot("Tail Pivot", Vector) = (0, 0, 0, 0)
        [HideInInspector] _RadiusMin("Radius Min", Float) = 0.4
        [HideInInspector] _RadiusMax("Radius Max", Float) = 1.3
        [HideInInspector] _FaceC("Face Centre", Vector) = (0, 0, 0, 0)
        [HideInInspector] _FaceR("Face Right", Vector) = (1, 0, 0, 0)
        [HideInInspector] _FaceU("Face Up", Vector) = (0, 1, 0, 0)
        [HideInInspector] _FaceN("Face Normal", Vector) = (0, 0, 1, 0)
        [HideInInspector] _FaceEye("Face Eye", Vector) = (0.06, 0.04, 0.015, 0)
        [HideInInspector] _FaceMuz("Face Muzzle", Vector) = (-0.05, 0.1, 0.055, 0.006)
        [HideInInspector] _FaceNos("Face Nostrils", Vector) = (0.02, -0.025, 0.005, 0.008)
        [HideInInspector] _FaceMouth("Face Mouth", Vector) = (-0.07, 0.045, 0.0035, 0)
        [HideInInspector] _FaceMuzzleColor("Muzzle Color", Color) = (0.8, 0.82, 0.72, 1)
        [HideInInspector] _FaceInkColor("Face Ink Color", Color) = (0.05, 0.045, 0.05, 1)
        [HideInInspector] _FollowPivot("Follow Pivot", Vector) = (0, 0, 0, 0)
        [HideInInspector] _FollowRadial("Follow Radial", Vector) = (0, 0, 0, 0)
        [HideInInspector] _SwayAmp("Sway Amplitude", Float) = 0
        [HideInInspector] _SwayDir("Sway Direction", Vector) = (0, 0, 1, 0)
        [HideInInspector] _FollowMode("Follow Mode", Float) = 0
        [HideInInspector] _FollowT("Follow T", Float) = 0
        [HideInInspector] _FollowNormal("Follow Normal", Vector) = (0, 1, 0, 0)
        [HideInInspector] _UprightAngle("Upright Angle", Float) = 0
        [HideInInspector] _UprightPivot("Upright Pivot", Vector) = (0, 0, 0, 0)
        [HideInInspector] _UprightRange("Upright Range", Vector) = (0, 1, 0, 0)
        [HideInInspector] _PartOffset("Part Offset", Vector) = (0, 0, 0, 0)
        [HideInInspector] _TurnAngle("Turn Angle", Float) = 0
        [HideInInspector] _TurnPivot("Turn Pivot", Vector) = (0, 0, 0, 0)
        [HideInInspector] _TurnRange("Turn Range", Vector) = (0, 1, 0, 0)
        [HideInInspector] _DropAmount("Drop Amount", Float) = 0
        [HideInInspector] _DropRange("Drop Range", Vector) = (0, 1, 0, 0)
        [HideInInspector] _TwistAngle("Twist Angle", Float) = 0
        [HideInInspector] _TwistPivot("Twist Pivot", Vector) = (0, 0, 0, 0)
        [HideInInspector] _TwistRange("Twist Range", Vector) = (0, 1, 0, 0)
        [HideInInspector] _PathAnchor("Path Anchor", Vector) = (0, 0, 0, 0)
        [HideInInspector] _PathA0("Path Angles 1-4", Vector) = (0, 0, 0, 0)
        [HideInInspector] _PathA1("Path Angles 5-8", Vector) = (0, 0, 0, 0)

        [HideInInspector] _Surface("Surface", Float) = 0
        [HideInInspector] _SrcBlend("Src Blend", Float) = 1
        [HideInInspector] _DstBlend("Dst Blend", Float) = 0
        [HideInInspector] _ZWrite("ZWrite", Float) = 1
        [HideInInspector] _Cull("Cull", Float) = 2
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" }

        // ---------------------------------------------------------------- main toon pass
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            Blend [_SrcBlend] [_DstBlend]
            ZWrite [_ZWrite]
            Cull [_Cull]

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH

            #include "SlugToonInput.hlsl"
            #include "SlugEye.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            // an optional picture on the surface (white = none). Only the base's map on the map table uses it so far
            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float4 tangentOS  : TANGENT;
                float2 uv         : TEXCOORD0;
                float2 uv1        : TEXCOORD1;
                float3 uv2        : TEXCOORD2;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS   : TEXCOORD1;
                float2 uv         : TEXCOORD2;
                float3 positionOS : TEXCOORD3;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                float3 pos = IN.positionOS.xyz;
                float3 nrm = IN.normalOS;
                SlugDeform(pos, nrm, IN.tangentOS.xyz, IN.uv, IN.uv1.x, IN.uv2);
                OUT.positionWS = TransformObjectToWorld(pos);
                OUT.positionCS = TransformWorldToHClip(OUT.positionWS);
                OUT.normalWS = TransformObjectToWorldNormal(nrm);
                OUT.uv = IN.uv;
                OUT.positionOS = IN.positionOS.xyz;
                return OUT;
            }

            // 3D eyeball: procedural iris / pupil, lit like the skin
            half4 EyeFragment(Varyings IN)
            {
                float3 N = normalize(IN.normalWS);
                float glint;
                half3 albedo = EyeSurface(IN.positionOS, glint);

                float4 shadowCoord = float4(0, 0, 0, 0);
                #if defined(_MAIN_LIGHT_SHADOWS) || defined(_MAIN_LIGHT_SHADOWS_CASCADE)
                    shadowCoord = TransformWorldToShadowCoord(IN.positionWS);
                #endif
                Light mainLight = GetMainLight(shadowCoord);
                float ndl = dot(N, mainLight.direction) * 0.5 + 0.5;
                float lit = ndl * lerp(0.35, 1.0, mainLight.shadowAttenuation);
                float x = lit * _Bands;
                float stepped = (floor(x) + smoothstep(0.5 - _BandSoftness, 0.5 + _BandSoftness, frac(x))) / _Bands;
                stepped = lerp(saturate(stepped), smoothstep(0.1, 0.95, lit), 0.5);
                half3 col = albedo * lerp(_ShadeColor.rgb, half3(1, 1, 1), stepped) * mainLight.color;
                col += albedo * SampleSH(N) * _AmbientStrength;
                col = lerp(col, half3(1.0, 0.98, 0.94), glint * 0.85);
                return half4(col, 1.0);
            }

            half4 frag(Varyings IN) : SV_Target
            {
                if (_EyeKind > 0.5) return EyeFragment(IN);
                float3 N = normalize(IN.normalWS);
                if (_Facet > 0.5)
                {
                    float3 nf = normalize(cross(ddx(IN.positionWS), ddy(IN.positionWS)));
                    N = dot(nf, N) < 0 ? -nf : nf;
                }

                // fine clay dents: perturb the normal with a noise gradient
                if (_DentAmp > 0.0)
                {
                    float3 q = IN.positionOS * _DentFreq;
                    float e = 0.4;
                    float n0 = ValueNoise3(q);
                    float3 g = float3(ValueNoise3(q + float3(e, 0, 0)) - n0,
                                      ValueNoise3(q + float3(0, e, 0)) - n0,
                                      ValueNoise3(q + float3(0, 0, e)) - n0) / e;
                    N = normalize(N - TransformObjectToWorldDir(g) * _DentAmp);
                }

                float3 V = normalize(GetWorldSpaceViewDir(IN.positionWS));
                float ndv = saturate(dot(N, V));

                // ---- albedo: base / belly / pattern ----
                float u = IN.uv.x;
                float t = IN.uv.y;
                half3 albedo = _BaseColor.rgb * SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv).rgb;

                float belly = 0.0;
                if (_BellyAmount > 0.0)
                {
                    float d = abs(u - 0.5) * 2.0;
                    belly = 1.0 - smoothstep(_BellyAmount - 0.04, _BellyAmount + 0.04, d);
                }
                albedo = lerp(albedo, _BellyColor.rgb, belly);

                float pat = 0.0;
                float sparkle = 0.0;
                if (_PatternType > 0.5)
                {
                    float2 p = float2(u * _PatternScale.x, t * _PatternScale.y);
                    float2 id = floor(p);
                    float2 f = frac(p);
                    if (_PatternType < 1.5)
                    {
                        float2 c = 0.25 + 0.5 * Hash22(id);
                        float r = _PatternSize * (0.6 + 0.4 * Hash21(id + 7.7));
                        pat = 1.0 - smoothstep(r - 0.04, r, distance(f, c));
                        pat *= step(0.22, Hash21(id + 3.1));
                    }
                    else if (_PatternType < 2.5)
                    {
                        pat = 1.0 - smoothstep(_PatternSize * 0.5 - 0.02, _PatternSize * 0.5 + 0.02, abs(f.y - 0.5));
                    }
                    else if (_PatternType < 3.5)
                    {
                        float x = abs(frac(p.x + 0.5) - 0.5);
                        pat = 1.0 - smoothstep(_PatternSize * 0.5 - 0.01, _PatternSize * 0.5 + 0.01, x);
                    }
                    else
                    {
                        float2 c = 0.2 + 0.6 * Hash22(id);
                        float2 dd = abs(f - c);
                        float diamond = dd.x + dd.y;
                        float on = step(0.55, Hash21(id + 1.3));
                        float twinkle = 0.55 + 0.45 * sin(SlugTime() * 3.0 + Hash21(id) * 40.0);
                        sparkle = on * (1.0 - smoothstep(_PatternSize * 0.5, _PatternSize, diamond)) * twinkle;
                    }
                    if (_PatternType < 3.5) pat *= (1.0 - belly);
                }
                albedo = lerp(albedo, _PatternColor.rgb, pat * _PatternColor.a);

                // ---- natural mottling: soft blotches plus fine speckles ----
                if (_MottleAmount > 0.0)
                {
                    float n1 = ValueNoise3(IN.positionOS * _MottleScale);
                    float n2 = ValueNoise3(IN.positionOS * _MottleScale * 3.1 + 7.3);
                    float blotch = smoothstep(0.5, 0.64, n1 * 0.72 + n2 * 0.28);
                    float speck = smoothstep(0.76, 0.88, ValueNoise3(IN.positionOS * _MottleScale * 9.0));
                    float mm = saturate(blotch * 0.85 + speck * 0.45) * (1.0 - belly * 0.7);
                    albedo = lerp(albedo, _MottleColor.rgb, mm * _MottleAmount * _MottleColor.a);
                }

                // ---- the foot fringe: a little lighter, with thin pleat lines across it
                if (_SkirtAmp > 0.0)
                {
                    float band = SkirtBand(IN.uv);
                    float pleat = 1.0 - smoothstep(0.08, 0.2, abs(frac(t * _SkirtFreq * 2.0) - 0.5));
                    albedo = lerp(albedo, _BellyColor.rgb * 1.05, band * 0.45);
                    albedo *= 1.0 - _SkirtLines * pleat * band;
                }

                // ---- camouflage (worn gear): three layers of blotches with hard, toon-like edges
                if (_CamoAmount > 0.0)
                {
                    float3 q = IN.positionOS * _CamoScale;
                    float a = ValueNoise3(q) * 0.7 + ValueNoise3(q * 2.3 + 5.1) * 0.3;
                    float b = ValueNoise3(q * 1.3 + 17.7) * 0.7 + ValueNoise3(q * 2.9 + 2.3) * 0.3;
                    float c = ValueNoise3(q * 0.8 + 41.3) * 0.75 + ValueNoise3(q * 3.1 + 9.9) * 0.25;
                    half3 camo = albedo;
                    camo = lerp(camo, _CamoColor1.rgb, smoothstep(0.53, 0.55, a));
                    camo = lerp(camo, _CamoColor3.rgb, smoothstep(0.63, 0.65, c) * (1.0 - smoothstep(0.53, 0.55, a)));
                    camo = lerp(camo, _CamoColor2.rgb, smoothstep(0.62, 0.64, b));
                    albedo = lerp(albedo, camo, _CamoAmount);
                }

                // ---- skin grooves: oblique crossing furrows on the back (bump + darkening) ----
                if (_WrinkleAmount > 0.0)
                {
                    float a = u * _WrinkleScale.x + t * _WrinkleScale.y;
                    float b = u * _WrinkleScale.x - t * _WrinkleScale.y;
                    float jit = (ValueNoise3(IN.positionOS * 16.0) - 0.5) * 0.4;
                    float da = abs(frac(a + jit) - 0.5) * 2.0;
                    float db = abs(frac(b - jit) - 0.5) * 2.0;
                    float groove = smoothstep(0.84, 0.99, max(da, db));
                    groove *= 0.35 + 0.65 * smoothstep(0.3, 0.7, ValueNoise3(IN.positionOS * 4.5 + 3.1));   // patchy, not a regular net
                    float dorsal = smoothstep(0.3, 0.6, abs(u - 0.5) * 2.0);
                    groove *= dorsal * (1.0 - smoothstep(0.86, 0.98, t));

                    float h = -groove * 0.0045 * _WrinkleAmount;
                    float3 dpdx = ddx(IN.positionWS);
                    float3 dpdy = ddy(IN.positionWS);
                    float3 r1 = cross(dpdy, N);
                    float3 r2 = cross(N, dpdx);
                    float det = dot(dpdx, r1);
                    float3 grad = sign(det) * (ddx(h) * r1 + ddy(h) * r2);
                    N = normalize(abs(det) * N - grad);
                    albedo *= 1.0 - 0.14 * groove * _WrinkleAmount;
                }

                // ---- felt grain ----
                if (_FeltAmount > 0.0)
                {
                    float n = ValueNoise3(IN.positionOS * 140.0) * 0.6 + ValueNoise3(IN.positionOS * 330.0) * 0.4;
                    albedo *= 1.0 + (n - 0.5) * _FeltAmount;
                }

                // ---- stitches ----
                if (_StitchAmount > 0.0)
                {
                    float du = min(u, 1.0 - u);
                    float dash = smoothstep(0.0, 0.08, frac(t * _StitchDash)) * (1.0 - smoothstep(0.55, 0.62, frac(t * _StitchDash)));
                    float m = (1.0 - smoothstep(_StitchWidth, _StitchWidth * 1.4, du)) * dash;
                    albedo *= 1.0 - 0.14 * _StitchAmount * (1.0 - smoothstep(0.0, _StitchWidth * 5.0, du));
                    if (_StitchRingT > 0.0)
                    {
                        float ringW = _StitchWidth * 1.5;
                        float dr = abs(t - _StitchRingT);
                        float dashA = 1.0 - smoothstep(0.55, 0.62, frac(u * _StitchDash * 0.6));
                        m = max(m, (1.0 - smoothstep(ringW, ringW * 1.4, dr)) * dashA);
                        albedo *= 1.0 - 0.1 * _StitchAmount * (1.0 - smoothstep(0.0, ringW * 4.0, dr));
                    }
                    albedo = lerp(albedo, _StitchColor.rgb, saturate(m) * _StitchAmount);
                }

                // ---- flat face painted on the skin (object space, so it rides the crawl exactly) ----
                //   small round eyes set a little apart, a lighter muzzle ellipse in the lower half with two tiny
                //   nostrils, and a straight one-line mouth
                if (_FaceC.w > 0.5)
                {
                    float3 dv = IN.positionOS - _FaceC.xyz;
                    float facing = step(-0.09, dot(dv, _FaceN.xyz));            // not on the back of the head
                    float2 q = float2(abs(dot(dv, _FaceR.xyz)), dot(dv, _FaceU.xyz));
                    float aa = max(fwidth(q.x) + fwidth(q.y), 2e-4) * 0.75;

                    // lighter muzzle
                    float2 mab = _FaceMuz.yz;
                    float me = (length((q - float2(0.0, _FaceMuz.x)) / mab) - 1.0) * min(mab.x, mab.y);
                    float muz = (1.0 - smoothstep(-aa, aa + _FaceMuz.w, me)) * facing;
                    albedo = lerp(albedo, _FaceMuzzleColor.rgb, muz * _FaceMuzzleColor.a);

                    // two small nostrils inside the muzzle
                    float2 nr = _FaceNos.zw;
                    float ne = (length((q - _FaceNos.xy) / nr) - 1.0) * min(nr.x, nr.y);
                    float nos = (1.0 - smoothstep(-aa, aa, ne)) * facing;

                    // small round eyes; the left one squeezes shut into a short line (looking along a gun)
                    float wink = _FaceEye.w * step(dot(dv, _FaceR.xyz), 0.0);
                    float2 eab = _FaceEye.z * float2(1.0 + 0.25 * wink, 1.0 - 0.8 * wink);
                    float ee = (length((q - _FaceEye.xy) / eab) - 1.0) * min(eab.x, eab.y);
                    float eye = (1.0 - smoothstep(-aa, aa, ee)) * facing;

                    // straight single-line mouth with rounded ends
                    float hl = _FaceMouth.y * _FaceMouth.w;
                    float ms = length(float2(max(q.x - hl, 0.0), q.y - _FaceMouth.x)) - _FaceMouth.z;
                    float mouth = (1.0 - smoothstep(-aa, aa, ms)) * facing * step(0.01, _FaceMouth.w);

                    float ink = saturate(max(max(nos * 0.85, eye), mouth));
                    albedo = lerp(albedo, _FaceInkColor.rgb, ink);
                }

                // ---- toon lighting (main light + SH ambient only) ----
                float4 shadowCoord = float4(0, 0, 0, 0);
                #if defined(_MAIN_LIGHT_SHADOWS) || defined(_MAIN_LIGHT_SHADOWS_CASCADE)
                    shadowCoord = TransformWorldToShadowCoord(IN.positionWS);
                #endif
                Light mainLight = GetMainLight(shadowCoord);

                float ndl = dot(N, mainLight.direction) * 0.5 + 0.5;
                float lit = ndl * lerp(0.35, 1.0, mainLight.shadowAttenuation);
                float x = lit * _Bands;
                float stepped = (floor(x) + smoothstep(0.5 - _BandSoftness, 0.5 + _BandSoftness, frac(x))) / _Bands;
                stepped = saturate(stepped);
                stepped = lerp(stepped, smoothstep(0.1, 0.95, lit), _ToonSmooth);

                half3 tint = lerp(_ShadeColor.rgb, half3(1, 1, 1), stepped);
                half3 col = albedo * tint * mainLight.color;
                col += albedo * SampleSH(N) * _AmbientStrength;

                // soft rim (matte, not a wet highlight)
                float rim = pow(1.0 - ndv, _RimPower);
                col = lerp(col, col + _RimColor.rgb * 0.5, smoothstep(0.5, 0.62, rim) * _RimAmount);
                col += albedo * pow(1.0 - ndv, 2.0) * _FuzzAmount * 0.6;

                col += _EmissionColor.rgb + _PatternColor.rgb * (sparkle * _SparkleStrength);

                half alpha = _BaseColor.a;
                if (_Alpha < 0.999)
                {
                    alpha *= lerp(_Alpha, 1.0, saturate(pow(1.0 - ndv, 2.0) * _AlphaEdge));
                }
                return half4(col, alpha);
            }
            ENDHLSL
        }

        // ---------------------------------------------------------------- inverted-hull outline
        Pass
        {
            Name "Outline"
            Tags { "LightMode" = "SRPDefaultUnlit" }

            Cull Front
            ZWrite On

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex vert
            #pragma fragment frag

            #include "SlugToonInput.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float4 tangentOS  : TANGENT;
                float2 uv         : TEXCOORD0;
                float2 uv1        : TEXCOORD1;
                float3 uv2        : TEXCOORD2;
            };

            struct Varyings { float4 positionCS : SV_POSITION; };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                float3 pos = IN.positionOS.xyz;
                float3 nrm = IN.normalOS;
                SlugDeform(pos, nrm, IN.tangentOS.xyz, IN.uv, IN.uv1.x, IN.uv2);
                pos += nrm * _OutlineWidth;
                OUT.positionCS = TransformObjectToHClip(pos);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                clip(_OutlineWidth - 1e-4);
                return half4(_OutlineColor.rgb, 1);
            }
            ENDHLSL
        }

        // ---------------------------------------------------------------- shadow caster
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull [_Cull]

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW

            #include "SlugToonInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            float3 _LightDirection;
            float3 _LightPosition;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float4 tangentOS  : TANGENT;
                float2 uv         : TEXCOORD0;
                float2 uv1        : TEXCOORD1;
                float3 uv2        : TEXCOORD2;
            };

            struct Varyings { float4 positionCS : SV_POSITION; };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                float3 pos = IN.positionOS.xyz;
                float3 nrm = IN.normalOS;
                SlugDeform(pos, nrm, IN.tangentOS.xyz, IN.uv, IN.uv1.x, IN.uv2);
                float3 positionWS = TransformObjectToWorld(pos);
                float3 normalWS = TransformObjectToWorldNormal(nrm);
                #if _CASTING_PUNCTUAL_LIGHT_SHADOW
                    float3 lightDirectionWS = normalize(_LightPosition - positionWS);
                #else
                    float3 lightDirectionWS = _LightDirection;
                #endif
                float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, lightDirectionWS));
                #if UNITY_REVERSED_Z
                    positionCS.z = min(positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #else
                    positionCS.z = max(positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #endif
                OUT.positionCS = positionCS;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target { return 0; }
            ENDHLSL
        }

        // ---------------------------------------------------------------- depth only
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }

            ZWrite On
            ColorMask R
            Cull [_Cull]

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex vert
            #pragma fragment frag

            #include "SlugToonInput.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float4 tangentOS  : TANGENT;
                float2 uv         : TEXCOORD0;
                float2 uv1        : TEXCOORD1;
                float3 uv2        : TEXCOORD2;
            };

            struct Varyings { float4 positionCS : SV_POSITION; };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                float3 pos = IN.positionOS.xyz;
                float3 nrm = IN.normalOS;
                SlugDeform(pos, nrm, IN.tangentOS.xyz, IN.uv, IN.uv1.x, IN.uv2);
                OUT.positionCS = TransformObjectToHClip(pos);
                return OUT;
            }

            half frag(Varyings IN) : SV_Target { return IN.positionCS.z; }
            ENDHLSL
        }
    }
    FallBack Off
}
