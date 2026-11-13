// Ink silhouette: flat value + painterly grain, sinks into distance fog and ground mist.
Shader "CrimsonVeil/Silhouette"
{
    Properties
    {
        _BaseColor ("Ink Color", Color) = (0.03, 0.03, 0.035, 1)
        _GrainScale ("Ink Grain Scale", Float) = 0.6
        _GrainStrength ("Ink Grain Strength", Range(0, 1)) = 0.35
        _EdgeLight ("Top Edge Light", Range(0, 1)) = 0.08
        _MistStrength ("Ground Mist Strength", Range(0, 2)) = 1
        [Toggle] _Wind ("Wind Sway", Float) = 0
        _WindWeight ("Wind Weight", Float) = 1
        [Toggle] _PinTop ("Pin Top (banners)", Float) = 0
        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull", Float) = 2
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" }
        Cull [_Cull]

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "CVCommon.hlsl"

        CBUFFER_START(UnityPerMaterial)
            half4 _BaseColor;
            float _GrainScale;
            float _GrainStrength;
            float _EdgeLight;
            float _MistStrength;
            float _Wind;
            float _WindWeight;
            float _PinTop;
            float _Cull;
        CBUFFER_END

        struct Attributes
        {
            float4 positionOS : POSITION;
            float3 normalOS : NORMAL;
            float2 uv : TEXCOORD0;
        };

        float3 DisplaceWS(Attributes v)
        {
            float3 ws = TransformObjectToWorld(v.positionOS.xyz);
            if (_Wind > 0.5)
            {
                float w = _PinTop > 0.5 ? (1.0 - v.uv.y) : v.uv.y;
                ws += CV_WindOffset(ws, w * w * _WindWeight);
            }
            return ws;
        }
        ENDHLSL

        Pass
        {
            Name "Forward"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float fogFactor : TEXCOORD2;
            };

            Varyings vert(Attributes v)
            {
                Varyings o;
                o.positionWS = DisplaceWS(v);
                o.positionCS = TransformWorldToHClip(o.positionWS);
                o.normalWS = TransformObjectToWorldNormal(v.normalOS);
                o.fogFactor = ComputeFogFactor(o.positionCS.z);
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                float2 p = i.positionWS.xy * _GrainScale + i.positionWS.z * 0.37;
                float grain = CV_Fbm(p) + 0.35 * CV_Fbm(p * 6.0);
                half3 col = _BaseColor.rgb * (1.0 + (grain - 0.65) * _GrainStrength * 2.0);
                col += saturate(i.normalWS.y) * _EdgeLight;

                col = MixFog(col, i.fogFactor);
                float mist = CV_GroundMist(i.positionWS, _MistStrength);
                col = lerp(col, _CV_MistColor.rgb, mist);
                return half4(col, 1);
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            ColorMask R

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            float4 vert(Attributes v) : SV_POSITION
            {
                return TransformWorldToHClip(DisplaceWS(v));
            }

            half frag() : SV_Target { return 0; }
            ENDHLSL
        }
    }
}
