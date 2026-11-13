// Soft drifting mist band / light shaft / waterfall card. One shader, three modes.
Shader "CrimsonVeil/Mist"
{
    Properties
    {
        [KeywordEnum(Mist, Shaft, Waterfall)] _Mode ("Mode", Float) = 0
        _Color ("Color", Color) = (0.8, 0.8, 0.8, 1)
        _Opacity ("Opacity", Range(0, 1)) = 0.6
        _NoiseScale ("Noise Scale", Vector) = (2, 1, 0, 0)
        _Scroll ("Scroll Speed", Vector) = (0.03, 0, 0, 0)
        _EdgeSoftness ("Edge Softness (x,y)", Vector) = (0.25, 0.45, 0, 0)
        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend ("Src Blend", Float) = 5
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend ("Dst Blend", Float) = 10
    }
    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" }
        Blend [_SrcBlend] [_DstBlend]
        ZWrite Off
        Cull Off

        Pass
        {
            Name "Forward"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma shader_feature_local _MODE_MIST _MODE_SHAFT _MODE_WATERFALL
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "CVCommon.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                float _Opacity;
                float4 _NoiseScale;
                float4 _Scroll;
                float4 _EdgeSoftness;
                float _SrcBlend;
                float _DstBlend;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; float3 positionWS : TEXCOORD1; };

            Varyings vert(Attributes v)
            {
                Varyings o;
                o.positionWS = TransformObjectToWorld(v.positionOS.xyz);
                o.positionCS = TransformWorldToHClip(o.positionWS);
                o.uv = v.uv;
                return o;
            }

            float EdgeMask(float2 uv)
            {
                float2 s = max(_EdgeSoftness.xy, 0.001);
                float x = smoothstep(0, s.x, uv.x) * smoothstep(0, s.x, 1 - uv.x);
                float y = smoothstep(0, s.y, uv.y) * smoothstep(0, s.y, 1 - uv.y);
                return x * y;
            }

            half4 frag(Varyings i) : SV_Target
            {
                float2 uv = i.uv;
                float2 offset = i.positionWS.xz * 0.013;
                float a;
                half3 col = _Color.rgb;

            #if defined(_MODE_WATERFALL)
                // Vertical streaks rushing down, foam at the bottom.
                float2 p = float2(uv.x * _NoiseScale.x, uv.y * _NoiseScale.y + _Time.y * _Scroll.y) + offset;
                float streak = CV_Fbm(float2(p.x, p.y * 0.15)) * 0.6 + CV_ValueNoise(float2(p.x * 3.0, p.y)) * 0.4;
                float foam = smoothstep(0.25, 0.0, uv.y) * CV_Fbm(uv * 8.0 + _Time.y * 0.4);
                a = saturate(smoothstep(0.35, 0.75, streak) + foam) * EdgeMask(float2(uv.x, max(uv.y, 0.5)));
                col *= 0.85 + 0.3 * streak;
            #elif defined(_MODE_SHAFT)
                // Light shaft: bright at the top, dusty and fading downwards.
                float dust = CV_Fbm(uv * _NoiseScale.xy + _Time.y * _Scroll.xy + offset);
                a = EdgeMask(uv) * pow(saturate(uv.y), 0.7) * (0.6 + 0.6 * dust);
            #else
                // Drifting mist band.
                float2 p = uv * _NoiseScale.xy + _Time.y * _Scroll.xy + offset;
                float n = CV_Fbm(p + CV_Fbm(p * 0.7 + _Time.y * 0.02) * 0.8);
                a = smoothstep(0.25, 0.85, n) * EdgeMask(uv);
            #endif

                return half4(col, saturate(a * _Opacity));
            }
            ENDHLSL
        }
    }
}
