// ADR-0003 "retro lens": 90s camcorder treatment for the PiP viewfinder only.
// Used by Codex's CamcorderLens via Graphics.Blit(source, dest, material, 0). Property contract:
// _MainTex, _SourceSize (w, h, 1/w, 1/h), _TimeSeconds, _ScanlineStrength, _GrainStrength,
// _ChromaPixels, _BarrelStrength. WebGL-safe: single pass, no compute.
Shader "Hidden/Doomsday/CamcorderLens"
{
    Properties
    {
        _MainTex("Source", 2D) = "white" {}
        _SourceSize("Source Size", Vector) = (320, 240, 0.003125, 0.0041667)
        _TimeSeconds("Time", Float) = 0
        _ScanlineStrength("Scanline Strength", Range(0, 0.4)) = 0.12
        _GrainStrength("Grain Strength", Range(0, 0.2)) = 0.045
        _ChromaPixels("Chroma Bleed (px)", Range(0, 3)) = 0.8
        _BarrelStrength("Barrel", Range(0, 0.1)) = 0.025
        _Warmth("Warmth", Range(0, 0.3)) = 0.08
        _Desaturate("Desaturate", Range(0, 0.5)) = 0.12
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }
        ZWrite Off ZTest Always Cull Off Blend Off

        Pass
        {
            Name "CamcorderLens"

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            float4 _SourceSize;
            float _TimeSeconds;
            half _ScanlineStrength, _GrainStrength, _ChromaPixels, _BarrelStrength, _Warmth, _Desaturate;

            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; };

            Varyings Vert(Attributes v)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                o.uv = v.uv;
                return o;
            }

            float Hash(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            half4 Frag(Varyings i) : SV_Target
            {
                // Barrel distortion around the center; outside the lens reads as black bezel.
                float2 c = i.uv - 0.5;
                float2 uv = 0.5 + c * (1.0 + _BarrelStrength * dot(c, c) * 4.0);
                if (any(uv < 0.0) || any(uv > 1.0)) return half4(0, 0, 0, 1);

                // Horizontal chroma bleed, like composite video.
                float2 shift = float2(_ChromaPixels * _SourceSize.z, 0);
                half r = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + shift).r;
                half2 gb = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv).gb;
                half b = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv - shift).b;
                half3 col = half3(r, gb.x, b);
                col.b = lerp(col.b, gb.y, 0.5);

                // Slightly washed, warm consumer-tape color.
                half luma = dot(col, half3(0.299, 0.587, 0.114));
                col = lerp(col, luma.xxx, _Desaturate);
                col *= half3(1.0 + _Warmth, 1.0, 1.0 - _Warmth);

                // Scanlines at source resolution (every other line darker).
                half scan = frac(uv.y * _SourceSize.y * 0.5) < 0.5 ? 1.0 : 0.0;
                col *= 1.0 - _ScanlineStrength * scan;

                // Animated grain.
                half grain = Hash(floor(uv * _SourceSize.xy) + frac(_TimeSeconds * 24.0) * 97.0) - 0.5;
                col += grain * _GrainStrength * 2.0;

                // Soft lens vignette.
                col *= 1.0 - dot(c, c) * 0.9;
                return half4(saturate(col), 1);
            }
            ENDHLSL
        }
    }
}
