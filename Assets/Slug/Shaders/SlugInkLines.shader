// Thin ink lines on the near side of ridges and edges, over the whole picture (the ground too), the way a comic artist
// draws the outline of rock and hill (Cairn look, docs/ideas/cairn_terrain_ideas.md, H). Drawn by a full-screen pass
// (IslandInkLines adds it to the renderer). It reads only the depth: the inverse of the depth changes evenly across a
// flat surface, so where it bends (a ridge, the edge of a rock in front of something far) a line appears. Lines fade in
// the island's air like everything else, and leave the sky alone.
Shader "Slug/InkLines"
{
    Properties
    {
        _Strength("Strength", Range(0, 1)) = 0.55
        _Threshold("Bend Needed", Range(0.005, 0.5)) = 0.06
        _InkColor("Ink (multiplies the picture)", Color) = (0.32, 0.3, 0.36, 1)
        _FadeEnd("Gone By (m)", Float) = 400
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" }
        ZWrite Off ZTest Always Cull Off Blend Off

        Pass
        {
            Name "InkLines"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "SlugAir.hlsl"

            float _Strength;
            float _Threshold;
            float4 _InkColor;
            float _FadeEnd;

            float InvDepth(float2 uv)
            {
                float raw = SampleSceneDepth(uv);
                return 1.0 / max(LinearEyeDepth(raw, _ZBufferParams), 1e-4);
            }

            half4 frag(Varyings i) : SV_Target
            {
                float2 uv = i.texcoord;
                half4 c = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv);
                float raw = SampleSceneDepth(uv);
                #if UNITY_REVERSED_Z
                    if (raw <= 1e-6) return c;          // the sky
                #else
                    if (raw >= 1.0 - 1e-6) return c;
                #endif
                float2 px = 1.0 / _ScreenParams.xy;
                float i0 = InvDepth(uv);
                float il = InvDepth(uv - float2(px.x, 0)), ir = InvDepth(uv + float2(px.x, 0));
                float id = InvDepth(uv - float2(0, px.y)), iu = InvDepth(uv + float2(0, px.y));
                // bending toward the eye (a ridge, a near edge): the middle is nearer than its neighbours' line
                float bx = (i0 - 0.5 * (il + ir)) / i0;
                float by = (i0 - 0.5 * (id + iu)) / i0;
                float bend = max(bx, by);
                float edge = smoothstep(_Threshold, _Threshold * 1.8, bend);

                float d = 1.0 / i0;
                float fade = 1.0 - smoothstep(_FadeEnd * 0.4, _FadeEnd, d);
                if (_SlugAir.w > 0.5) { float h = d * _SlugAir.x; fade *= exp(-h * h); }
                c.rgb = lerp(c.rgb, c.rgb * _InkColor.rgb, edge * fade * _Strength);
                return c;
            }
            ENDHLSL
        }
    }
}
