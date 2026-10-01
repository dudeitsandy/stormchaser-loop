// ADR-0003 stylized sky: three-stop vertical gradient + soft sun disc. Assign via RenderSettings.skybox.
Shader "Doomsday/GradientSky"
{
    Properties
    {
        _TopColor("Top", Color) = (0.22, 0.42, 0.78, 1)
        _HorizonColor("Horizon", Color) = (0.95, 0.78, 0.6, 1)
        _BottomColor("Below Horizon", Color) = (0.36, 0.4, 0.35, 1)
        _HorizonSharpness("Horizon Sharpness", Range(0.5, 10)) = 3
        _SunColor("Sun", Color) = (1, 0.93, 0.75, 1)
        _SunSize("Sun Size", Range(0.0005, 0.05)) = 0.006
        _SunGlow("Sun Glow", Range(0, 1)) = 0.35
    }

    SubShader
    {
        Tags { "Queue" = "Background" "RenderType" = "Background" "PreviewType" = "Skybox" "RenderPipeline" = "UniversalPipeline" }
        Cull Off ZWrite Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RealtimeLights.hlsl"

            half4 _TopColor, _HorizonColor, _BottomColor, _SunColor;
            half _HorizonSharpness, _SunSize, _SunGlow;

            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings { float4 positionCS : SV_POSITION; float3 dir : TEXCOORD0; };

            Varyings Vert(Attributes v)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                o.dir = v.positionOS.xyz;
                return o;
            }

            half4 Frag(Varyings i) : SV_Target
            {
                float3 d = normalize(i.dir);
                half up = saturate(d.y);
                half down = saturate(-d.y);
                half3 sky = lerp(_HorizonColor.rgb, _TopColor.rgb, pow(up, 1.0 / _HorizonSharpness));
                sky = lerp(sky, _BottomColor.rgb, saturate(down * _HorizonSharpness));

                float3 sunDir = GetMainLight().direction;
                half sd = saturate(dot(d, sunDir));
                half disc = smoothstep(1.0 - _SunSize, 1.0 - _SunSize * 0.5, sd);
                half glow = pow(sd, 64) * _SunGlow;
                sky += _SunColor.rgb * (disc + glow);
                return half4(sky, 1);
            }
            ENDHLSL
        }
    }
}
