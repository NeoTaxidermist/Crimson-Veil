// Ink silhouette: flat value + painterly grain + optional stone cracks; sinks into distance fog and
// ground mist; optional wind sway (weight from uv2.y) and alpha-clipped texture (maple leaves).
Shader "CrimsonVeil/Silhouette"
{
    Properties
    {
        _BaseColor ("Ink Color", Color) = (0.03, 0.03, 0.035, 1)
        [NoScaleOffset] _MainTex ("Shape (alpha)", 2D) = "white" {}
        [Toggle] _AlphaClip ("Alpha Clip", Float) = 0
        _Cutoff ("Cutoff", Range(0, 1)) = 0.5
        _GrainScale ("Ink Grain Scale", Float) = 0.6
        _GrainStrength ("Ink Grain Strength", Range(0, 1)) = 0.35
        _Cracks ("Stone Cracks", Range(0, 1)) = 0
        _CrackScale ("Crack Scale", Float) = 1.2
        _EdgeLight ("Top Edge Light", Range(0, 1)) = 0.08
        _FormShade ("Form Shading (moonlight)", Range(0, 1)) = 0
        _FormLift ("Moonlit Form (light added on near-black ink)", Range(0, 0.3)) = 0
        _Relief ("Stone Relief (moonlit tops, shaded sides)", Range(0, 1.5)) = 0
        _VertexTint ("Vertex Tone (stones / mortar)", Range(0, 1)) = 0
        _RimLight ("Rim Light", Range(0, 1)) = 0
        _RimColor ("Rim Color", Color) = (0.62, 0.62, 0.66, 1)
        _RimWidth ("Rim Width", Range(0.5, 8)) = 3
        _FootShade ("Darker Towards the Feet", Range(0, 1)) = 0
        _FootHeight ("Feet-to-Head Height (m)", Float) = 2
        _MistStrength ("Ground Mist Strength", Range(0, 2)) = 1
        [Toggle] _Wind ("Wind Sway", Float) = 0
        _WindWeight ("Wind Weight", Float) = 1
        [Toggle] _PinTop ("Pin Top (banners)", Float) = 0
        _InkStreaks ("Ink Streaks (the scarf's painted crimson: the hero's collar)", Range(0, 1)) = 0
        _StreakDeep ("Streak Deep", Color) = (0.17, 0.02, 0.02, 1)
        _StreakLight ("Streak Light", Color) = (0.55, 0.07, 0.06, 1)
        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull", Float) = 2
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" }
        Cull [_Cull]

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "CVCommon.hlsl"

        TEXTURE2D(_MainTex);
        SAMPLER(sampler_MainTex);

        CBUFFER_START(UnityPerMaterial)
            half4 _BaseColor;
            float _AlphaClip;
            float _Cutoff;
            float _GrainScale;
            float _GrainStrength;
            float _Cracks;
            float _CrackScale;
            float _EdgeLight;
            float _FormShade;
            float _FormLift;
            float _Relief;
            float _VertexTint;
            float _RimLight;
            half4 _RimColor;
            float _RimWidth;
            float _FootShade;
            float _FootHeight;
            float _MistStrength;
            float _Wind;
            float _WindWeight;
            float _PinTop;
            float _Cull;
            float _InkStreaks;
            half4 _StreakDeep;
            half4 _StreakLight;
        CBUFFER_END

        struct Attributes
        {
            float4 positionOS : POSITION;
            float3 normalOS : NORMAL;
            float2 uv : TEXCOORD0;
            float2 uv2 : TEXCOORD1;
            float4 color : COLOR;
        };

        float3 DisplaceWS(Attributes v)
        {
            float3 ws = TransformObjectToWorld(v.positionOS.xyz);
            if (_Wind > 0.5)
            {
                float w = _PinTop > 0.5 ? (1.0 - v.uv2.y) : v.uv2.y;
                ws += CV_WindOffset(ws, w * w * _WindWeight);
            }
            return ws;
        }

        void ClipShape(float2 uv)
        {
            if (_AlphaClip > 0.5)
                clip(SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv).a - _Cutoff);
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
                float2 uv : TEXCOORD3;
                float4 tint : TEXCOORD4;
                float3 positionOS : TEXCOORD5;
            };

            Varyings vert(Attributes v)
            {
                Varyings o;
                o.positionWS = DisplaceWS(v);
                o.positionCS = TransformWorldToHClip(o.positionWS);
                o.normalWS = TransformObjectToWorldNormal(v.normalOS);
                o.fogFactor = ComputeFogFactor(o.positionCS.z);
                o.uv = v.uv;
                o.tint = v.color;
                o.positionOS = v.positionOS.xyz;
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                ClipShape(i.uv);
                float2 p = i.positionWS.xy * _GrainScale + i.positionWS.z * 0.37;
                float grain = CV_Fbm(p) + 0.35 * CV_Fbm(p * 6.0);
                half3 col = _BaseColor.rgb * (1.0 + (grain - 0.65) * _GrainStrength * 2.0);
                // Per-vertex tone (masonry): each dressed stone its own shade, the mortar behind them darker.
                // Meshes without vertex colours read as white here, i.e. unchanged.
                col *= lerp(1.0, i.tint.rgb, _VertexTint);

                // The hero's collar painted like his scarf: broad strokes of deep and bright crimson running round the
                // neck (in the body's own space, so they stay on the cloth), not one flat red.
                if (_InkStreaks > 0.001)
                {
                    float3 q = i.positionOS;
                    float around = atan2(q.z, q.x);
                    float warp = (CV_Fbm(float2(around * 1.3, q.y * 6.0)) - 0.5) * 1.2;
                    float s = CV_Fbm(float2(around * 1.6 + warp, q.y * 26.0 + warp * 2.0));
                    half3 ink = lerp(_StreakDeep.rgb, _BaseColor.rgb, smoothstep(0.3, 0.55, s));
                    ink = lerp(ink, _StreakLight.rgb, smoothstep(0.6, 0.85, s) * 0.85);
                    col = lerp(col, ink * (1.0 + (grain - 0.65) * _GrainStrength), _InkStreaks);
                }

                if (_Cracks > 0.001)
                {
                    float2 cp = i.positionWS.xy * _CrackScale + i.positionWS.z * 0.21;
                    float edge = CV_VoronoiEdge(cp + CV_Fbm(cp * 0.7) * 0.6);
                    float crack = 1.0 - smoothstep(0.0, 0.06, edge);
                    // cracks read as lighter lines on dark stone and darker on light stone
                    col = lerp(col, col * 0.55 + 0.035, crack * _Cracks);
                }

                // Only (near-)level tops get the edge light; chamfers and slopes are left to _Relief.
                float up = saturate(i.normalWS.y);
                col += up * up * up * up * _EdgeLight;

                // Creatures: modelled by the moon (from above, slightly behind the camera), darker towards the
                // feet, and a pale rim along the outline so they never melt into a dark background.
                float3 n = normalize(i.normalWS);
                if (_FormShade > 0.001)
                {
                    float3 moon = normalize(float3(0.35, 0.85, -0.4));
                    float lit = saturate(dot(n, moon) * 0.5 + 0.5);
                    col *= lerp(1.0, lerp(0.55, 1.9, lit), _FormShade);
                }
                // The hero: still ink-black, but the moon catches the planes turned up towards it (shoulders, the
                // top of the head and arms, the folds of the tunic) - a few percent of cool grey, added rather than
                // multiplied (on near-black a multiply changes nothing), so the body reads as a 3D form.
                if (_FormLift > 0.0001)
                {
                    float3 moon = normalize(float3(0.35, 0.85, -0.4));
                    float lit = saturate(dot(n, moon));
                    float wrap = saturate(dot(n, moon) * 0.5 + 0.5);
                    col += _FormLift * (lit * lit * 0.8 + wrap * wrap * 0.2) * float3(0.82, 0.85, 0.95);
                }
                // Environment relief: faces towards the camera keep the layer's flat value; stone tops, ledges
                // and the upper sides of carved blocks catch the moon, undersides and left sides fall into shade,
                // so masonry joints, voussoirs and strata read inside the silhouette.
                if (_Relief > 0.001)
                {
                    float3 moon = normalize(float3(0.35, 0.85, -0.4));
                    float d = dot(n, moon) - dot(float3(0, 0, -1), moon);
                    col *= 1.0 + _Relief * (d > 0 ? d * 1.1 : d * 0.75);
                    col += _Relief * max(d, 0) * 0.07; // so carved tops still glint on near-black rock
                }
                if (_FootShade > 0.001)
                {
                    float h = saturate((i.positionWS.y - UNITY_MATRIX_M._m13) / max(0.1, _FootHeight));
                    col *= lerp(1.0, lerp(0.55, 1.15, h), _FootShade);
                }
                if (_RimLight > 0.001)
                {
                    float3 view = normalize(GetWorldSpaceViewDir(i.positionWS));
                    float rim = pow(saturate(1.0 - abs(dot(n, view))), _RimWidth);
                    col = lerp(col, _RimColor.rgb, rim * _RimLight);
                }
                // (the collar painted like the scarf takes no fog either: the scarf has none, and beside it the fog's
                // grey made the collar read as another cloth)
                col = lerp(MixFog(col, i.fogFactor), col, step(0.5, _InkStreaks));
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

            struct V2F { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; };

            V2F vert(Attributes v)
            {
                V2F o;
                o.positionCS = TransformWorldToHClip(DisplaceWS(v));
                o.uv = v.uv;
                return o;
            }

            half frag(V2F i) : SV_Target
            {
                ClipShape(i.uv);
                return 0;
            }
            ENDHLSL
        }
    }
}
