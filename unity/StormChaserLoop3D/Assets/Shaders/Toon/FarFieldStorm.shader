// Far-field storm cards (storm-director.md Visual/Audio: wall cloud, anvils, funnel silhouette at >= 800 m).
// Claude, 2026-10-02, for Codex's StormVisualSpike / far-field cells.
// URP forward, unlit, alpha-blended, double-sided, WebGL 2 safe (target 2.0, one texture sample).
// Deliberately FOG-EXEMPT: no multi_compile_fog and no MixFog, so ADR-0004's ~250 m fog cannot erase the
// cell. The horizon read comes from _HorizonColor/_HorizonBlend instead, which the caller drives by distance.
// ZWrite Off, ZTest LEqual: terrain and the opaque distant silhouette still occlude it. Plain forward draw,
// no custom renderer pass, so it runs unchanged under Render Graph.
// Contract with Codex: _BaseMap (+ _BaseMap_ST), _BaseColor (tint + alpha), _HorizonColor (RGB),
// _HorizonBlend 0-1 (lerps RGB only, alpha untouched), vertex colour multiplies.
// Variants: instancing only.
Shader "Doomsday/FarFieldStorm"
{
    Properties
    {
        [Header(Card)]
        [MainTexture] _BaseMap("Base Map", 2D) = "white" {}
        [MainColor] _BaseColor("Base Color", Color) = (1, 1, 1, 1)
        [Header(Horizon)]
        _HorizonColor("Horizon Color", Color) = (0.62, 0.68, 0.74, 1)
        _HorizonBlend("Horizon Blend", Range(0, 1)) = 0.25
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        ZTest LEqual
        Cull Off

        Pass
        {
            Name "FarFieldStorm"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor;
                half4 _HorizonColor;
                half _HorizonBlend;
            CBUFFER_END
            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
            };

            Varyings Vert(Attributes v)
            {
                Varyings o;
                UNITY_SETUP_INSTANCE_ID(v);
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                o.uv = TRANSFORM_TEX(v.uv, _BaseMap);
                o.color = v.color;
                return o;
            }

            half4 Frag(Varyings i) : SV_Target
            {
                half4 c = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv) * _BaseColor * i.color;
                // Aerial perspective without fog: pull RGB toward the horizon colour, keep the card's alpha.
                c.rgb = lerp(c.rgb, _HorizonColor.rgb, saturate(_HorizonBlend));
                return c;
            }
            ENDHLSL
        }
    }
}
