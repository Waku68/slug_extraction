// A painted sky for the island (Cairn look, docs/ideas/cairn_terrain_ideas.md, C): a soft gradient from the horizon
// (the colour the far land melts into, so the air's layers end in the sky) up to a deeper top, a warm glow round the sun,
// a band of big cumulus standing on the horizon and a few loose clouds overhead. No textures: the clouds are noise cut
// into three flat tones (lit, middle, cool shade), like the plant masses. WeatherPresenter sets the colours and the
// cloud cover from the island's clock and weather. Used as the skybox.
Shader "Slug/PaintedSky"
{
    Properties
    {
        _SkyTop("Top", Color) = (0.3, 0.5, 0.8, 1)
        _SkyHorizon("Horizon (= the air)", Color) = (0.66, 0.78, 0.92, 1)
        _SunDir("Towards The Sun", Vector) = (0, 0.5, 1, 0)
        _SunColor("Sun", Color) = (1, 0.95, 0.85, 1)
        _CloudLit("Cloud Lit", Color) = (1, 0.98, 0.94, 1)
        _CloudShade("Cloud Shade", Color) = (0.62, 0.7, 0.84, 1)
        _Cover("Cloud Cover", Range(0, 1)) = 0.35
        _Drift("Drift", Float) = 0
    }

    SubShader
    {
        Tags { "Queue" = "Background" "RenderType" = "Background" "PreviewType" = "Skybox" "RenderPipeline" = "UniversalPipeline" }
        Cull Off ZWrite Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _SkyTop, _SkyHorizon, _SunColor, _CloudLit, _CloudShade;
                float4 _SunDir;
                float _Cover, _Drift;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings { float4 positionCS : SV_POSITION; float3 dir : TEXCOORD0; };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.dir = IN.positionOS.xyz;
                return OUT;
            }

            float H21(float2 p) { float3 p3 = frac(p.xyx * 0.1031); p3 += dot(p3, p3.yzx + 33.33); return frac((p3.x + p3.y) * p3.z); }
            float N2(float2 p)
            {
                float2 i = floor(p), f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                return lerp(lerp(H21(i), H21(i + float2(1, 0)), f.x), lerp(H21(i + float2(0, 1)), H21(i + float2(1, 1)), f.x), f.y);
            }
            float Fbm(float2 p)
            {
                float s = 0, a = 0.5;
                for (int k = 0; k < 5; k++) { s += a * N2(p); p = p * 2.03 + 17.1; a *= 0.5; }
                return s;
            }
            // three flat tones, crisp steps (like the toon bands)
            half3 Tones(float l)
            {
                float t = smoothstep(0.30, 0.34, l) * 0.5 + smoothstep(0.62, 0.66, l) * 0.5;
                half3 mid = lerp(_CloudShade.rgb, _CloudLit.rgb, 0.55);
                return t < 0.5 ? lerp(_CloudShade.rgb, mid, t * 2.0) : lerp(mid, _CloudLit.rgb, (t - 0.5) * 2.0);
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float3 d = normalize(IN.dir);
                float3 s = normalize(_SunDir.xyz);
                float e = d.y;

                // gradient: the horizon is the air, the top is deeper; below the horizon the air's colour
                half3 col = lerp(_SkyHorizon.rgb, _SkyTop.rgb, pow(saturate(e), 0.55));
                col = e < 0.0 ? _SkyHorizon.rgb * (1.0 - 0.06 * saturate(-e * 4.0)) : col;
                float sd = saturate(dot(d, s));
                col += _SunColor.rgb * (pow(sd, 18.0) * 0.35 + pow(sd, 4.0) * 0.12) * saturate(s.y * 4.0 + 0.6);

                float az = atan2(d.x, d.z);
                float el = asin(clamp(e, -1.0, 1.0));
                float aa = max(fwidth(el), 1e-4) * 1.5;

                // ---- cumulus standing on the horizon ----
                float cover = saturate(_Cover);
                float mass = Fbm(float2(az * 1.3 + _Drift * 0.05, 3.1));
                float top = (0.02 + 0.22 * mass * mass) * (0.45 + cover);
                float billow = Fbm(float2(az * 9.0 + _Drift * 0.2, el * 12.0));
                float edge = top + 0.06 * (billow - 0.5) - el;
                float band = smoothstep(-aa, aa, edge) * smoothstep(-0.06, 0.0, el);
                if (band > 0.0)
                {
                    float hc = saturate(el / max(top, 0.01));                  // 0 bottom .. 1 top of the cloud
                    float sunSide = dot(normalize(float2(d.x, d.z) + 1e-5), normalize(float2(s.x, s.z) + 1e-5));
                    float l = 0.25 + 0.55 * hc + 0.35 * (billow - 0.5) + 0.12 * sunSide * saturate(s.y * 3.0);
                    half3 cc = Tones(l);
                    cc = lerp(cc, _SkyHorizon.rgb, 0.22);                       // far: a little into the air
                    col = lerp(col, cc, band);
                }

                // ---- loose clouds overhead (a cloud layer seen in perspective) ----
                if (e > 0.02)
                {
                    // puffy heaps: a large shape whose edge is bitten by small rounds
                    float2 p = d.xz / (e + 0.15) * 2.4 + float2(_Drift * 0.01, _Drift * 0.004);
                    float2 w = float2(N2(p * 0.5 + 3.3), N2(p * 0.5 + 9.1)) * 0.5;
                    float baseN = Fbm(p * 0.55 + w);
                    float cut = lerp(0.7, 0.4, cover);
                    // only the rim is bitten (no holes inside)
                    float rim = 1.0 - smoothstep(cut, cut + 0.1, baseN);
                    float n = baseN - (1.0 - N2(p * 2.6 + w * 2.0)) * 0.16 * rim;
                    float m = smoothstep(cut, cut + 0.015, n) * smoothstep(0.03, 0.14, e);
                    if (m > 0.0)
                    {
                        // lit unless this part of the heap faces away from the sun; a shade rim on the far side
                        float baseS = Fbm((p - normalize(s.xz + 1e-5) * 0.45) * 0.55 + w);
                        float l = 0.72 + (baseN - baseS) * 3.0 - rim * 0.25;
                        col = lerp(col, lerp(Tones(l), _SkyHorizon.rgb, saturate(0.35 - e)), m);
                    }
                }
                return half4(col, 1.0);
            }
            ENDHLSL
        }
    }
    FallBack Off
}
