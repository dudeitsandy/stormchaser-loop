// ADR-0003 "stylized world": banded cel lighting, tinted shadows, rim light.
// Main light only (with shadows) + SH ambient. Opaque, no alpha test, so URP's stock
// ShadowCaster / DepthOnly / DepthNormals passes can be reused (they only touch
// _BaseMap/_BaseColor/_Cutoff under _ALPHATEST_ON).
Shader "Doomsday/ToonLit"
{
    Properties
    {
        [MainColor] _BaseColor("Base Color", Color) = (0.85, 0.3, 0.2, 1)
        [MainTexture] _BaseMap("Base Map", 2D) = "white" {}

        [Header(Bands)]
        _ShadowTint("Shadow Tint", Color) = (0.45, 0.42, 0.62, 1)
        _ShadowThreshold("Shadow Threshold", Range(0, 1)) = 0.35
        _HighlightThreshold("Highlight Threshold", Range(0, 1)) = 0.8
        _HighlightBoost("Highlight Boost", Range(0, 1)) = 0.12
        _BandSoftness("Band Softness", Range(0.001, 0.2)) = 0.03

        [Header(Rim)]
        _RimColor("Rim Color", Color) = (1, 0.95, 0.85, 1)
        _RimPower("Rim Power", Range(0.5, 8)) = 3.5
        _RimStrength("Rim Strength", Range(0, 1)) = 0.35

        [Header(Ambient)]
        _AmbientStrength("Ambient Strength", Range(0, 1)) = 0.35

        // Kept for compatibility with URP's shared passes and material conversion.
        [HideInInspector] _Cutoff("Alpha Cutoff", Range(0, 1)) = 0.5
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST;
            half4 _BaseColor;
            half4 _ShadowTint;
            half _ShadowThreshold;
            half _HighlightThreshold;
            half _HighlightBoost;
            half _BandSoftness;
            half4 _RimColor;
            half _RimPower;
            half _RimStrength;
            half _AmbientStrength;
            half _Cutoff;
        CBUFFER_END

        TEXTURE2D(_BaseMap);
        SAMPLER(sampler_BaseMap);
        ENDHLSL

        Pass
        {
            Name "ToonForward"
            Tags { "LightMode" = "UniversalForward" }
            Cull Back
            ZWrite On

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fog
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float3 normalWS   : TEXCOORD2;
                half   fogFactor  : TEXCOORD3;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            Varyings Vert(Attributes input)
            {
                Varyings o = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, o);
                VertexPositionInputs pos = GetVertexPositionInputs(input.positionOS.xyz);
                o.positionCS = pos.positionCS;
                o.positionWS = pos.positionWS;
                o.normalWS = TransformObjectToWorldNormal(input.normalOS);
                o.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                o.fogFactor = ComputeFogFactor(pos.positionCS.z);
                return o;
            }

            half4 Frag(Varyings i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                half3 albedo = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv).rgb * _BaseColor.rgb;
                float3 n = normalize(i.normalWS);
                float3 v = GetWorldSpaceNormalizeViewDir(i.positionWS);

                Light light = GetMainLight(TransformWorldToShadowCoord(i.positionWS));
                half ndl = saturate(dot(n, light.direction));
                half lightAmount = ndl * light.shadowAttenuation * light.distanceAttenuation;

                // Two hard-ish steps: shadow → lit → highlight.
                half lit = smoothstep(_ShadowThreshold - _BandSoftness, _ShadowThreshold + _BandSoftness, lightAmount);
                half hi  = smoothstep(_HighlightThreshold - _BandSoftness, _HighlightThreshold + _BandSoftness, lightAmount);

                half3 shadowCol = albedo * _ShadowTint.rgb;
                half3 litCol = albedo * lerp(half3(1, 1, 1), light.color, 0.5);
                half3 col = lerp(shadowCol, litCol, lit);
                col += albedo * _HighlightBoost * hi;

                // Ambient from SH keeps the shadow side from going flat.
                col += albedo * SampleSH(n) * _AmbientStrength;

                // Rim only on the lit side, so silhouettes pop against the sky.
                half rim = pow(1.0h - saturate(dot(n, v)), _RimPower);
                col += _RimColor.rgb * rim * _RimStrength * lit;

                col = MixFog(col, i.fogFactor);
                return half4(col, 1);
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
            Cull Back

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment
            #pragma multi_compile_instancing
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            #include "Packages/com.unity.render-pipelines.universal/Shaders/ShadowCasterPass.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            ZWrite On
            ColorMask R
            Cull Back

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex DepthOnlyVertex
            #pragma fragment DepthOnlyFragment
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/Shaders/DepthOnlyPass.hlsl"
            ENDHLSL
        }

        // Feeds _CameraNormalsTexture for the screen-space outline pass.
        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode" = "DepthNormals" }
            ZWrite On
            Cull Back

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex DepthNormalsVertex
            #pragma fragment DepthNormalsFragment
            #pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/Shaders/DepthNormalsPass.hlsl"
            ENDHLSL
        }
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
