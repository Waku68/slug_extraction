// Distant rain curtain / column hanging under a moving rain cloud (RainCloudVisuals, docs/ideas/wind_ideas.md #6):
// soft vertical streaks of rain falling from the cloud's base to the ground, fading out at top and bottom and when the
// camera walks inside the column (where the close-up rain particles take over).
Shader "Slug/RainColumn"
{
    Properties
    {
        _Color ("Color", Color) = (0.68, 0.75, 0.84, 0.38)
    }
    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent-10" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" }
        Pass
        {
            Name "RainColumn"
            Tags { "LightMode" = "UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
                half4  color      : COLOR;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
                half4  color      : COLOR;
                float3 positionWS : TEXCOORD1;
            };

            Varyings vert(Attributes v)
            {
                Varyings o;
                o.positionWS = TransformObjectToWorld(v.positionOS.xyz);
                o.positionCS = TransformWorldToHClip(o.positionWS);
                o.uv = v.uv;
                o.color = v.color;
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                // uv.x: 0..1 around/across the curtain, uv.y: 0 at ground .. 1 at cloud base
                float across = 1.0 - abs(i.uv.x * 2.0 - 1.0);
                float side = smoothstep(0.0, 0.45, across);
                float vertFade = smoothstep(0.0, 0.22, i.uv.y) * smoothstep(1.0, 0.72, i.uv.y);

                // Falling rain streaks inside the curtain
                float t = _Time.y;
                float s1 = 0.5 + 0.5 * sin(i.uv.x * 28.0 + i.color.g * 31.0);
                float s2 = 0.5 + 0.5 * sin(i.uv.x * 67.0 - i.color.g * 19.0 + i.uv.y * 6.0 + t * 5.5);
                float fall = 0.72 + 0.28 * (s1 * 0.5 + s2 * 0.5);

                // Fade out when the camera is very close (close-up rain particles take over inside the cloud)
                float dist = distance(i.positionWS, _WorldSpaceCameraPos);
                float distFade = smoothstep(22.0, 65.0, dist) * (1.0 - smoothstep(900.0, 1400.0, dist));

                half a = saturate(i.color.a * _Color.a * side * vertFade * fall * distFade);
                return half4(_Color.rgb, a);
            }
            ENDHLSL
        }
    }
}
