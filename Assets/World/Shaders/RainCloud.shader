// Cairn / Mathieu Bablet style bande-dessinee cumulonimbus cloud shader (RainCloudVisuals):
//  - Broad, monumental gouache color zones (4 crisp bands: sunlit cream crown, soft ivory body, alpine slate-blue
//    shadow, and deep indigo-slate anvil base)
//  - Crisp comic pen outlines on outer silhouette & major overlapping domes (SlugOutline pass)
//  - Delicate, understated internal ink accents along the main shadow terminator
Shader "Slug/RainCloud"
{
    Properties
    {
        _LitColor("Crown Lit Color", Color) = (0.99, 0.97, 0.92, 1)
        _MidLightColor("Body Mid-Light Color", Color) = (0.90, 0.93, 0.97, 1)
        _MidShadeColor("Lobe Shade Color", Color) = (0.62, 0.72, 0.86, 1)
        _BaseShadeColor("Anvil Base Shade Color", Color) = (0.44, 0.55, 0.71, 1)
        _RimColor("Sun Rim Color", Color) = (1.0, 0.98, 0.92, 1)
        _InkColor("Ink Line Color", Color) = (0.25, 0.34, 0.49, 1)
        _SkyHorizonColor("Sky Horizon Color", Color) = (0.66, 0.78, 0.92, 1)
        _SunDir("Towards Sun", Vector) = (0.4, 0.6, 0.7, 0)
        _OutlineWidth("Outline Width (screen-scaled)", Range(0.0, 0.02)) = 0.0025
        _InkStrength("Internal Ink Strength", Range(0.0, 1.0)) = 0.55
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry+10" }

        // ---------------------------------------------------------------- main painted cloud pass
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            Cull Back
            ZWrite On
            ZTest LEqual

            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _LitColor;
                half4 _MidLightColor;
                half4 _MidShadeColor;
                half4 _BaseShadeColor;
                half4 _RimColor;
                half4 _InkColor;
                half4 _SkyHorizonColor;
                float4 _SunDir;
                float _OutlineWidth;
                float _InkStrength;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0; // x: knuckle ridge (0 crease..1 dome), y: signed puff-seam distance
                float3 uv2        : TEXCOORD2; // macro cloud dome normal in object space
                half4  color      : COLOR;     // r: local puff Y (0 bottom..1 top), g: whole-cloud height (0 base..1 crown), b: flat-base blend
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS   : TEXCOORD1;
                float3 macroNWS   : TEXCOORD2;
                float2 uv         : TEXCOORD3;
                float3 positionOS : TEXCOORD4;
                half4  color      : COLOR;
            };

            float Hash31(float3 p)
            {
                p = frac(p * 0.1031);
                p += dot(p, p.yzx + 33.33);
                return frac((p.x + p.y) * p.z);
            }

            float Noise3(float3 p)
            {
                float3 i = floor(p);
                float3 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                float n000 = Hash31(i + float3(0, 0, 0)), n100 = Hash31(i + float3(1, 0, 0));
                float n010 = Hash31(i + float3(0, 1, 0)), n110 = Hash31(i + float3(1, 1, 0));
                float n001 = Hash31(i + float3(0, 0, 1)), n101 = Hash31(i + float3(1, 0, 1));
                float n011 = Hash31(i + float3(0, 1, 1)), n111 = Hash31(i + float3(1, 1, 1));
                float nx00 = lerp(n000, n100, f.x), nx10 = lerp(n010, n110, f.x);
                float nx01 = lerp(n001, n101, f.x), nx11 = lerp(n011, n111, f.x);
                return lerp(lerp(nx00, nx10, f.y), lerp(nx01, nx11, f.y), f.z);
            }

            // Broad, sweeping gouache billow relief in object space so tonal band edges curve like hand-painted cumulus
            float BillowRelief(float3 posOS)
            {
                float3 q = posOS * 2.6;
                float n1 = abs(Noise3(q) * 2.0 - 1.0);
                float n2 = abs(Noise3(q * 2.1 + 7.7) * 2.0 - 1.0);
                return n1 * 0.65 + n2 * 0.35;
            }

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionWS = TransformObjectToWorld(IN.positionOS.xyz);
                OUT.positionCS = TransformWorldToHClip(OUT.positionWS);
                OUT.normalWS = TransformObjectToWorldNormal(IN.normalOS);
                OUT.macroNWS = TransformObjectToWorldNormal(IN.uv2);
                OUT.uv = IN.uv;
                OUT.positionOS = IN.positionOS.xyz;
                OUT.color = IN.color;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float3 localN = normalize(IN.normalWS);
                float3 macroN = normalize(IN.macroNWS);
                float3 viewDir = normalize(GetWorldSpaceViewDir(IN.positionWS));

                // Translucent sky-cloud key light: ensures upper & sunward domes gleam in warm light from any angle
                float3 rawSun = normalize(_SunDir.xyz);
                float3 viewRight = normalize(cross(float3(0.0, 1.0, 0.0), viewDir) + 1e-4);
                float sideSign = dot(rawSun, viewRight) >= 0.0 ? 1.0 : -1.0;
                float3 viewKey = normalize(viewDir * 0.52 + viewRight * (0.48 * sideSign) + float3(0.0, 0.72, 0.0));
                float3 keyDir = normalize(lerp(rawSun, viewKey, 0.68));

                float puffY = saturate(IN.color.r);      // 0 at bottom of this puff .. 1 at top of this puff
                float height01 = saturate(IN.color.g);   // 0 at flat cloud base .. 1 at highest summit crown
                float isBase = saturate(IN.color.b);     // 1 on the flat horizontal condensation base
                float knuckle = saturate(IN.uv.x);
                float seamDist = IN.uv.y;

                // Blend local puff normal with macro cloud normal so adjacent domes fuse into monumental Cairn masses
                float3 N = normalize(lerp(localN, macroN, lerp(0.32, 0.88, isBase)));

                float billow = BillowRelief(IN.positionOS) * (1.0 - isBase * 0.85);
                float ndlLocal = dot(N, keyDir) * 0.5 + 0.5;
                float ndlMacro = dot(macroN, keyDir) * 0.5 + 0.5;
                float valleyAO = lerp(smoothstep(-0.02, 0.16, seamDist), 1.0, isBase);

                // Generous sunlit upper zones on every major dome, with crisp slate-blue shadow cups underneath
                float domeUp = saturate(N.y * 0.5 + 0.5);
                float lit = ndlLocal * 0.38
                          + domeUp * 0.22
                          + puffY * 0.22
                          + height01 * 0.18
                          + ndlMacro * 0.10
                          + (knuckle - 0.36) * 0.14 * (1.0 - isBase)
                          + (billow - 0.42) * 0.18;
                lit *= lerp(0.80, 1.05, valleyAO);

                // Flat horizontal anvil underside stays in the cool shadow bands
                float underFlat = saturate(max(isBase, smoothstep(-0.12, -0.55, localN.y) * (1.0 - height01 * 0.65)));
                lit = lerp(lit, min(lit, 0.35) * 0.75 + 0.08 * billow, underFlat);

                // 4 crisp painted gouache bands (0: anvil base, 1: lobe shadow crescent, 2: mid-light ivory, 3: sunlit crown)
                float aa = clamp(fwidth(lit) * 1.35, 0.009, 0.038);
                float b1 = smoothstep(0.34 - aa, 0.34 + aa, lit);
                float b2 = smoothstep(0.49 - aa, 0.49 + aa, lit);
                float b3 = smoothstep(0.63 - aa, 0.63 + aa, lit);

                half3 col = _BaseShadeColor.rgb;
                col = lerp(col, _MidShadeColor.rgb, b1);
                col = lerp(col, _MidLightColor.rgb, b2);
                col = lerp(col, _LitColor.rgb, b3);

                // Crisp sunlit silver-gold rim on the upper boiling edges
                float ndv = saturate(dot(N, viewDir));
                float sunRim = pow(1.0 - ndv, 2.4) * smoothstep(0.30, 0.80, ndlLocal) * smoothstep(0.15, 0.50, height01);
                col = lerp(col, _RimColor.rgb, smoothstep(0.25, 0.33, sunRim) * 0.65);

                // ---- Subtle Bande Dessinee Internal Ink Accents ----
                float upperMask = smoothstep(0.08, 0.22, height01) * (1.0 - underFlat);
                float strokeBreak = smoothstep(0.38, 0.72, Noise3(IN.positionOS * 4.5 + 3.1));

                // 1) Delicate ink crease only in deep valleys between major lobes (mostly in the mid/shadow zone)
                float seamAA = max(fwidth(seamDist), 0.008);
                float seamLine = (1.0 - smoothstep(0.002, 0.024 + seamAA, max(0.0, seamDist)))
                               * step(-0.012, seamDist)
                               * upperMask * lerp(0.85, 0.30, b3);

                // 2) Broken pen accent along the main light-to-shadow terminator (lit == 0.49)
                float termEdge = 1.0 - abs(lit - 0.49) / max(aa * 1.35, 0.011);
                float termLine = saturate(termEdge) * strokeBreak * upperMask * 0.50;

                float dist = distance(IN.positionWS, _WorldSpaceCameraPos);
                float inkDistFade = 1.0 - smoothstep(480.0, 980.0, dist);
                float ink = saturate(max(seamLine, termLine)) * _InkStrength * inkDistFade;
                col = lerp(col, _InkColor.rgb, ink);

                // Gentle sky-altitude atmospheric perspective
                float skyHaze = smoothstep(360.0, 1150.0, dist) * 0.28;
                col = lerp(col, _SkyHorizonColor.rgb, skyHaze);

                return half4(col, 1.0);
            }
            ENDHLSL
        }

        // ---------------------------------------------------------------- inverted-hull ink outline (silhouette + major overlapping domes)
        Pass
        {
            Name "Outline"
            Tags { "LightMode" = "SlugOutline" }

            Cull Front
            ZWrite On
            ZTest LEqual

            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _LitColor;
                half4 _MidLightColor;
                half4 _MidShadeColor;
                half4 _BaseShadeColor;
                half4 _RimColor;
                half4 _InkColor;
                half4 _SkyHorizonColor;
                float4 _SunDir;
                float _OutlineWidth;
                float _InkStrength;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float3 uv2        : TEXCOORD2;
                half4  color      : COLOR;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float  baseMask   : TEXCOORD1;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                float isBase = saturate(IN.color.b);
                float3 nOS = normalize(lerp(IN.normalOS, IN.uv2, lerp(0.28, 0.94, isBase)));

                float3 posWS = TransformObjectToWorld(IN.positionOS.xyz);
                float3 normWS = normalize(TransformObjectToWorldNormal(nOS));
                float3 toCam = _WorldSpaceCameraPos - posWS;
                float dist = length(toCam);
                float3 viewDir = toCam / max(dist, 1e-3);

                float width = clamp(dist * _OutlineWidth, 0.14, 1.95) * lerp(1.0, 0.42, isBase);
                float3 sideN = normWS - viewDir * dot(normWS, viewDir);
                // Generous depth bias away from camera so only the outer cloud silhouette and major front-to-back
                // tier overlaps draw pen lines (prevents small internal fish-scale rings)
                float depthBias = clamp(dist * 0.022, 1.2, 7.5);
                posWS += sideN * width - viewDir * depthBias;

                OUT.positionWS = posWS;
                OUT.positionCS = TransformWorldToHClip(posWS);
                OUT.baseMask = isBase * saturate(-normWS.y);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                clip(_OutlineWidth - 1e-5);
                clip(0.55 - IN.baseMask);
                float dist = distance(IN.positionWS, _WorldSpaceCameraPos);
                float skyHaze = smoothstep(360.0, 1150.0, dist) * 0.35;
                half3 col = lerp(_InkColor.rgb, _SkyHorizonColor.rgb, skyHaze);
                return half4(col, 1.0);
            }
            ENDHLSL
        }

        // ---------------------------------------------------------------- depth only
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }

            ZWrite On
            ColorMask R
            Cull Back

            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings { float4 positionCS : SV_POSITION; };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                return OUT;
            }

            half frag(Varyings IN) : SV_Target { return IN.positionCS.z; }
            ENDHLSL
        }
    }
    FallBack Off
}
