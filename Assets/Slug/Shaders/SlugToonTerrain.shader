// The island's ground in the same toon look as the things on it (Slug/Toon): flat colour fields, three light bands with
// a crisp edge, the shade in the colour of the moment, hard shadows, the island's air (haze, valley mist, dawn glow).
// Cairn look (docs/ideas/cairn_terrain_ideas.md):
//  - the colours come from one picture of the island seen from above (_IslandColor, baked from the terrain's layers by
//    IslandToonGround: plant bands on the terrace edges are already in it)
//  - ground steeper than the slug can climb (50°) turns to bare rock: green turns grey, and rock shows its strata as
//    horizontal bands with a dark line under each ledge, so "can walk / can't climb" reads from afar
//  - no painted textures: large soft blotches from world-space noise only
// Use with Terrain.drawInstanced = false (vertex normals). Holes (_TerrainHolesTexture) are cut.
Shader "Slug/ToonTerrain"
{
    Properties
    {
        _IslandColor("Island Colour (from above)", 2D) = "white" {}
        _ShadeColor("Shade Tint", Color) = (0.62, 0.66, 0.8, 1)
        _Bands("Light Bands", Range(1, 5)) = 3
        _BandSoftness("Band Softness", Range(0.001, 0.5)) = 0.05
        _AmbientStrength("Ambient Strength", Range(0, 1)) = 0.31

        [Header(Steep ground)]
        _SteepAngle("Bare Rock From (degrees)", Range(20, 80)) = 34
        _RockColor("Rock Colour (where plants can't hold)", Color) = (0.58, 0.59, 0.58, 1)
        _RockDarken("Bare Ground Darker (pale ground on a cliff)", Range(0.3, 1)) = 0.62
        _RockCool("Cliff Cooler (towards the shade colour)", Range(0, 1)) = 0.35
        _StrataAmount("Strata Bands", Range(0, 0.5)) = 0.18
        _StrataPeriod("Strata Height (m)", Range(0.5, 10)) = 3.4
        _LedgeLine("Dark Line Under Ledges", Range(0, 1)) = 0.42
        _CrackAmount("Rock Cracks & Fissure Shadows", Range(0, 1)) = 0.48

        [Header(Surface)]
        _MottleAmount("Large Blotches", Range(0, 0.3)) = 0.08
        _MottleScale("Blotch Size (1 / m)", Range(0.005, 0.5)) = 0.06
        _PatchAmount("Hard Colour Patches (Cairn gouache)", Range(0, 0.5)) = 0.22
        _SpeckAmount("Near Ground Pebbles & Lichen", Range(0, 0.5)) = 0.18

        // set by the terrain itself
        [HideInInspector] _TerrainHolesTexture("Holes Map (RGB)", 2D) = "white" {}
    }

    HLSLINCLUDE
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
    #include "SlugAir.hlsl"

    CBUFFER_START(UnityPerMaterial)
        float4 _IslandColor_ST;
        half4 _ShadeColor;
        half  _Bands;
        half  _BandSoftness;
        half  _AmbientStrength;
        half  _SteepAngle;
        half4 _RockColor;
        half  _RockDarken;
        half  _RockCool;
        half  _StrataAmount;
        half  _StrataPeriod;
        half  _LedgeLine;
        half  _CrackAmount;
        half  _MottleAmount;
        half  _MottleScale;
        half  _PatchAmount;
        half  _SpeckAmount;
    CBUFFER_END

    TEXTURE2D(_IslandColor);       SAMPLER(sampler_IslandColor);
    TEXTURE2D(_TerrainHolesTexture); SAMPLER(sampler_TerrainHolesTexture);

    float TerrainHash31(float3 p3)
    {
        p3 = frac(p3 * 0.1031);
        p3 += dot(p3, p3.zyx + 31.32);
        return frac((p3.x + p3.y) * p3.z);
    }
    float3 TerrainHash33(float3 p3)
    {
        p3 = frac(p3 * float3(0.1031, 0.1030, 0.0973));
        p3 += dot(p3, p3.yxz + 33.33);
        return frac((p3.xxy + p3.yxx) * p3.zyx);
    }
    float TerrainNoise3(float3 p)
    {
        float3 i = floor(p), f = frac(p);
        f = f * f * (3.0 - 2.0 * f);
        return lerp(lerp(lerp(TerrainHash31(i), TerrainHash31(i + float3(1, 0, 0)), f.x),
                         lerp(TerrainHash31(i + float3(0, 1, 0)), TerrainHash31(i + float3(1, 1, 0)), f.x), f.y),
                    lerp(lerp(TerrainHash31(i + float3(0, 0, 1)), TerrainHash31(i + float3(1, 0, 1)), f.x),
                         lerp(TerrainHash31(i + float3(0, 1, 1)), TerrainHash31(i + float3(1, 1, 1)), f.x), f.y), f.z);
    }
    // Angular Voronoi rock blocks: x = edge distance (f2 - f1, straight angular cracks), y = per-block flat tone hash (0..1)
    float2 TerrainVoronoi3(float3 p)
    {
        float3 n = floor(p);
        float3 f = frac(p);
        float f1 = 8.0, f2 = 8.0, id = 0.0;
        [unroll]
        for (int k = -1; k <= 1; k++)
        [unroll]
        for (int j = -1; j <= 1; j++)
        [unroll]
        for (int i = -1; i <= 1; i++)
        {
            float3 g = float3(i, j, k);
            float3 o = TerrainHash33(n + g);
            float3 r = g + (0.15 + 0.70 * o) - f;
            // Chebyshev-blended metric for more angular, chiseled rock planes
            float d = dot(r, r) * 0.75 + max(abs(r.x), max(abs(r.y), abs(r.z))) * 0.25;
            if (d < f1)
            {
                f2 = f1;
                f1 = d;
                id = o.x;
            }
            else if (d < f2)
            {
                f2 = d;
            }
        }
        return float2(f2 - f1, id);
    }

    void ClipHoles(float2 uv)
    {
        float hole = SAMPLE_TEXTURE2D(_TerrainHolesTexture, sampler_TerrainHolesTexture, uv).r;
        clip(hole == 0.0 ? -1 : 1);
    }
    ENDHLSL

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry-100" "TerrainCompatible" = "True" }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; float2 uv : TEXCOORD0; };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS   : TEXCOORD1;
                float2 uv         : TEXCOORD2;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionWS = TransformObjectToWorld(IN.positionOS.xyz);
                OUT.normalWS = TransformObjectToWorldNormal(IN.normalOS);
                OUT.positionCS = TransformWorldToHClip(OUT.positionWS);
                OUT.uv = IN.uv;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                ClipHoles(IN.uv);
                float3 P = IN.positionWS;
                float3 N = normalize(IN.normalWS);
                half3 albedo = SAMPLE_TEXTURE2D(_IslandColor, sampler_IslandColor, IN.uv).rgb;

                // ---- Walkable ground: Cairn bande-dessinee gouache camo patches + fine speckles (matched to Slug/Toon scale) ----
                float camDist = distance(P, _WorldSpaceCameraPos);
                float detailFade = 1.0 - smoothstep(18.0, 65.0, camDist);
                float speckFade  = 1.0 - smoothstep(9.0, 28.0, camDist);

                // 1) Broad terrain variation (4-6 m) so distant hills and meadows still have painterly colour fields
                float3 qBroad = P * 0.22;
                float bA = TerrainNoise3(qBroad) * 0.7 + TerrainNoise3(qBroad * 2.3 + 5.1) * 0.3;
                float bB = TerrainNoise3(qBroad * 1.3 + 17.7) * 0.7 + TerrainNoise3(qBroad * 2.9 + 2.3) * 0.3;
                half3 groundCol = albedo;
                groundCol = lerp(groundCol, groundCol * half3(1.12, 1.09, 0.92), smoothstep(0.53, 0.56, bA) * _PatchAmount * 0.85);
                groundCol = lerp(groundCol, groundCol * half3(0.80, 0.86, 0.92), smoothstep(0.58, 0.61, bB) * _PatchAmount * 0.85);

                // 2) Slug-scale gouache camo patches (0.4-0.8 m, hard toon edges just like Slug/Toon _CamoAmount on rocks/trees)
                if (detailFade > 0.001 && _PatchAmount > 0.001)
                {
                    float3 qCamo = P * 1.65;
                    float cA = TerrainNoise3(qCamo) * 0.7 + TerrainNoise3(qCamo * 2.3 + 5.1) * 0.3;
                    float cB = TerrainNoise3(qCamo * 1.3 + 17.7) * 0.7 + TerrainNoise3(qCamo * 2.9 + 2.3) * 0.3;
                    float cC = TerrainNoise3(qCamo * 0.85 + 41.3) * 0.75 + TerrainNoise3(qCamo * 3.1 + 9.9) * 0.25;
                    float w1 = smoothstep(0.53, 0.55, cA);
                    float w3 = smoothstep(0.62, 0.64, cC) * (1.0 - w1);
                    float w2 = smoothstep(0.61, 0.63, cB);
                    half3 camo = groundCol;
                    camo = lerp(camo, groundCol * half3(1.15, 1.12, 0.93), w1);
                    camo = lerp(camo, groundCol * half3(1.10, 0.96, 0.82), w3);
                    camo = lerp(camo, groundCol * half3(0.76, 0.81, 0.86), w2);
                    groundCol = lerp(groundCol, camo, _PatchAmount * detailFade);
                }

                // 3) Fine near-ground pebbles, soil grains & lichen speckles (~8-15 cm, matching Slug/Toon _MottleScale * 9)
                if (speckFade > 0.001 && _SpeckAmount > 0.001)
                {
                    float sp1 = TerrainNoise3(P * 6.5);
                    float sp2 = TerrainNoise3(P * 11.5 + 7.3);
                    float lightSpeck = smoothstep(0.68, 0.72, sp1);
                    float darkSpeck  = smoothstep(0.70, 0.74, sp2) * (1.0 - lightSpeck);
                    groundCol = lerp(groundCol, groundCol * half3(1.22, 1.18, 1.04), lightSpeck * _SpeckAmount * speckFade);
                    groundCol = lerp(groundCol, groundCol * half3(0.66, 0.70, 0.76), darkSpeck  * _SpeckAmount * speckFade);
                }

                // ---- Steep ground is bare rock (Cairn: chiseled Voronoi rock blocks, angular fissures, strata & Slug/Toon rock camo) ----
                float n1 = TerrainNoise3(P * 0.12);
                float cosSteep = cos(radians(_SteepAngle));
                float steep = 1.0 - smoothstep(cosSteep - 0.06, cosSteep + 0.05, N.y + (n1 - 0.5) * 0.09);
                float green = saturate((albedo.g - max(albedo.r, albedo.b)) * 8.0);
                half3 rock = lerp(albedo * _RockDarken, _RockColor.rgb, green);
                rock = lerp(rock, rock * _ShadeColor.rgb * 1.15, _RockCool);

                // Strata: horizontal bands + dark shadow line under each ledge
                float warp = (TerrainNoise3(P * float3(0.08, 0.02, 0.08)) - 0.5) * 0.75;
                float sh = P.y / _StrataPeriod + warp;
                float f = frac(sh);
                float band = step(0.5, frac(sh * 0.5 + 0.25 * TerrainNoise3(float3(floor(sh), 0, 0))));
                rock *= lerp(1.0 - _StrataAmount, 1.0 + _StrataAmount * 0.55, band);
                float lineW = 0.045 + fwidth(sh) * 1.5;
                float ledgeShadow = (1.0 - smoothstep(0.0, lineW, f)) * saturate(0.14 / max(fwidth(sh), 1e-4));
                rock = lerp(rock, rock * _ShadeColor.rgb * 0.52, _LedgeLine * ledgeShadow);

                // Slug/Toon-style rock surface camo & speckles so terrain cliffs match placed rocks right next to them
                if (steep > 0.01)
                {
                    if (detailFade > 0.001)
                    {
                        float3 rCamoP = P * 2.1;
                        float rcA = TerrainNoise3(rCamoP) * 0.7 + TerrainNoise3(rCamoP * 2.3 + 5.1) * 0.3;
                        float rcB = TerrainNoise3(rCamoP * 1.3 + 17.7) * 0.7 + TerrainNoise3(rCamoP * 2.9 + 2.3) * 0.3;
                        float rcC = TerrainNoise3(rCamoP * 0.8 + 41.3) * 0.75 + TerrainNoise3(rCamoP * 3.1 + 9.9) * 0.25;
                        half3 rCamo = rock;
                        rCamo = lerp(rCamo, rock * half3(1.14, 1.11, 1.03), smoothstep(0.53, 0.55, rcA));
                        rCamo = lerp(rCamo, rock * half3(1.18, 1.14, 0.98), smoothstep(0.63, 0.65, rcC) * (1.0 - smoothstep(0.53, 0.55, rcA)));
                        rCamo = lerp(rCamo, rock * half3(0.75, 0.77, 0.82), smoothstep(0.60, 0.62, rcB));
                        rock = lerp(rock, rCamo, 0.55 * detailFade);
                    }
                    if (speckFade > 0.001)
                    {
                        float rSp = smoothstep(0.74, 0.80, TerrainNoise3(P * 9.5 + 3.1));
                        rock = lerp(rock, rock * 0.72, rSp * 0.45 * speckFade);
                    }
                }

                // Angular Voronoi rock blocks & straight fissure lines (only on steep rock faces)
                if (_CrackAmount > 0.001 && steep > 0.01)
                {
                    float3 vp = P * float3(0.22, 0.14, 0.22) + float3(warp * 0.35, 0.0, -warp * 0.35);
                    float2 vor1 = TerrainVoronoi3(vp);
                    // Flat per-block tone step + slight facet normal tilt so blocks catch toon light steps crisply
                    float blockStep = floor(vor1.y * 3.0) / 2.0;
                    rock *= lerp(0.88, 1.10, blockStep);
                    float3 blockTilt = (TerrainHash33(floor(vp)) - 0.5) * 0.22 * steep;
                    N = normalize(N + blockTilt);

                    // Straight angular crack along Voronoi block borders
                    float crackGate = smoothstep(0.38, 0.55, TerrainNoise3(P * 0.07 + 3.7));
                    float fw = max(fwidth(vor1.x) * 1.35, 0.018);
                    float crackMain = (1.0 - smoothstep(0.012, 0.012 + fw, vor1.x)) * crackGate;

                    // Finer secondary fractures inside blocks
                    float2 vor2 = TerrainVoronoi3(P * float3(0.65, 0.45, 0.65) + 7.9);
                    float fw2 = max(fwidth(vor2.x) * 1.35, 0.024);
                    float crackFine = (1.0 - smoothstep(0.010, 0.010 + fw2, vor2.x)) * crackGate * 0.65 * (1.0 - smoothstep(25.0, 85.0, camDist));

                    float crack = saturate(max(crackMain, crackFine));
                    rock = lerp(rock, rock * _ShadeColor.rgb * 0.36, crack * _CrackAmount);
                }

                // Lichen / moss / salt-crust patches clinging to upper parts of rock blocks
                float lichenNoise = TerrainNoise3(P * 0.32 + 13.1);
                float lichenMask = smoothstep(0.58, 0.62, lichenNoise) * smoothstep(0.25, 0.65, N.y);
                rock = lerp(rock, groundCol * 0.95, lichenMask * 0.55);

                albedo = lerp(groundCol, rock, steep);

                // ---- large soft blotches ----
                float m = TerrainNoise3(P * _MottleScale) * 0.65 + TerrainNoise3(P * _MottleScale * 2.7 + 7.3) * 0.35;
                albedo *= 1.0 + (m - 0.5) * 2.0 * _MottleAmount;

                // ---- toon light, as Slug/Toon ----
                float4 shadowCoord = TransformWorldToShadowCoord(P);
                Light mainLight = GetMainLight(shadowCoord);
                float shadowAtten = mainLight.shadowAttenuation * SlugCloudShadowAt(P);
                float3 V = normalize(GetWorldSpaceViewDir(P));
                half3 wetSheen;
                SlugWetSurface(albedo, wetSheen, P, N, V, mainLight.direction, mainLight.color, shadowAtten, 1.0);

                float ndl = dot(N, mainLight.direction) * 0.5 + 0.5;
                float lit = ndl * lerp(0.35, 1.0, shadowAtten);
                float x = lit * _Bands;
                float stepped = saturate((floor(x) + smoothstep(0.5 - _BandSoftness, 0.5 + _BandSoftness, frac(x))) / _Bands);
                half3 tint = lerp(SlugShade(_ShadeColor.rgb), half3(1, 1, 1), stepped);
                half3 col = albedo * tint * mainLight.color;
                col += albedo * SampleSH(N) * _AmbientStrength;
                col += SlugGlow(albedo, P, stepped * shadowAtten);
                col += wetSheen;

                // without the island's air (other scenes), the scene's fog
                if (_SlugAir.w < 0.5) col = MixFog(col, ComputeFogFactor(TransformWorldToHClip(P).z));
                return half4(SlugAirLook(col, P, 1.0), 1.0);
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            ZWrite On
            ZTest LEqual
            ColorMask 0

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            float3 _LightDirection;
            float3 _LightPosition;

            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                float3 positionWS = TransformObjectToWorld(IN.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(IN.normalOS);
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
                OUT.uv = IN.uv;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target { ClipHoles(IN.uv); return 0; }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            ZWrite On
            ColorMask R

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex vert
            #pragma fragment frag

            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv;
                return OUT;
            }

            half frag(Varyings IN) : SV_Target { ClipHoles(IN.uv); return IN.positionCS.z; }
            ENDHLSL
        }
    }
    FallBack Off
}
