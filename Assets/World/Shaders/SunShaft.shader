// A shaft of sunlight standing in the air (SunShafts): additive, soft at the sides, fading in from the ground and out
// toward the sun, with a slow drift of brighter bands like dust turning in the light. The mesh is built in world space by
// SunShafts every frame; the vertex colour carries each shaft's strength.
Shader "Slug/SunShaft"
{
    Properties
    {
        _Color ("Color", Color) = (1, 0.93, 0.7, 1)
        _Intensity ("Intensity", Float) = 1
    }
    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" }
        Pass
        {
            Name "SunShaft"
            Tags { "LightMode" = "UniversalForward" }
            Blend One One
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                half _Intensity;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
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
                // across: soft both sides, a little brighter core
                half across = 1 - abs(i.uv.x * 2 - 1);
                half side = across * across * (3 - 2 * across);
                side *= 0.75 + 0.25 * side;
                // along: in from the ground, out toward the sun
                half along = smoothstep(0, 0.08, i.uv.y) * pow(saturate(1 - i.uv.y), 1.4);
                // dust turning in the light: slow bands drifting up the shaft
                half t = _Time.y;
                half dust = 0.8 + 0.2 * sin(i.uv.y * 23 - t * 0.7 + i.color.g * 40) * sin(i.uv.y * 7 + t * 0.3);
                half a = i.color.a * side * along * dust * _Intensity;
                return half4(_Color.rgb * a, a);
            }
            ENDHLSL
        }
    }
}
