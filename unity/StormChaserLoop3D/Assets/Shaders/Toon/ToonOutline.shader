// ADR-0003 outline: screen-space edge detection on depth + normals.
// Driven by URP's built-in FullScreenPassRendererFeature (Render Graph), injected before
// post-processing with Depth + Normal requirements. _BlitTexture is the camera color.
Shader "Hidden/Doomsday/ToonOutline"
{
    Properties
    {
        _OutlineColor("Outline Color", Color) = (0.08, 0.07, 0.12, 1)
        _Thickness("Thickness (px)", Range(0.5, 4)) = 1.5
        _DepthThreshold("Depth Threshold", Range(0.001, 0.5)) = 0.06
        _NormalThreshold("Normal Threshold", Range(0.05, 1)) = 0.4
        _FadeStart("Fade Start (m)", Float) = 60
        _FadeEnd("Fade End (m)", Float) = 140
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }
        ZWrite Off ZTest Always Cull Off Blend Off

        Pass
        {
            Name "ToonOutline"

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareNormalsTexture.hlsl"

            half4 _OutlineColor;
            float _Thickness;
            float _DepthThreshold;
            float _NormalThreshold;
            float _FadeStart;
            float _FadeEnd;

            float EyeDepth(float2 uv)
            {
                return LinearEyeDepth(SampleSceneDepth(uv), _ZBufferParams);
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float2 uv = input.texcoord;
                half4 color = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv);

                float2 px = _Thickness / _ScreenParams.xy;
                float2 o0 = float2(px.x, 0), o1 = float2(-px.x, 0), o2 = float2(0, px.y), o3 = float2(0, -px.y);

                float dc = EyeDepth(uv);
                float d0 = EyeDepth(uv + o0), d1 = EyeDepth(uv + o1), d2 = EyeDepth(uv + o2), d3 = EyeDepth(uv + o3);
                // Relative depth jump, so the threshold works near and far.
                float dMax = max(max(abs(d0 - dc), abs(d1 - dc)), max(abs(d2 - dc), abs(d3 - dc)));
                float depthEdge = step(_DepthThreshold, dMax / max(dc, 0.001));

                float3 nc = SampleSceneNormals(uv);
                float nMin = min(min(dot(nc, SampleSceneNormals(uv + o0)), dot(nc, SampleSceneNormals(uv + o1))),
                                 min(dot(nc, SampleSceneNormals(uv + o2)), dot(nc, SampleSceneNormals(uv + o3))));
                float normalEdge = step(_NormalThreshold, 1.0 - nMin);

                float nearest = min(dc, min(min(d0, d1), min(d2, d3)));
                float fade = 1.0 - saturate((nearest - _FadeStart) / max(_FadeEnd - _FadeStart, 0.001));

                float edge = saturate(max(depthEdge, normalEdge)) * fade * _OutlineColor.a;
                return half4(lerp(color.rgb, _OutlineColor.rgb, edge), color.a);
            }
            ENDHLSL
        }
    }
}
