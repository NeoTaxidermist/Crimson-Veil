// The crimson veil: painterly cloth with shades of red — dark wine edges, a glowing core,
// streaks flowing along the length, ragged soft edges and a tail that dissolves into smoke.
// uv.x = 0 at the neck → 1 at the tip, uv.y = 0..1 across the ribbon.
// uv2.x = hardness (0 cloth → 1 weapon), uv2.y = strand seed. Vertex colour = tint * alpha.
Shader "CrimsonVeil/Veil"
{
    Properties
    {
        _Deep ("Deep Colour", Color) = (0.28, 0.0, 0.02, 1)
        _Mid ("Mid Colour", Color) = (0.85, 0.02, 0.04, 1)
        _Glow ("Core Glow (HDR)", Color) = (1.8, 0.12, 0.1, 1)
        _FlowSpeed ("Flow Speed", Float) = 1.4
        _EdgeSoftness ("Edge Softness", Range(0.05, 1)) = 0.45
        _Erosion ("Tail Erosion", Range(0, 1)) = 0.75
    }
    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            Name "Forward"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "CVCommon.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Deep;
                half4 _Mid;
                half4 _Glow;
                float _FlowSpeed;
                float _EdgeSoftness;
                float _Erosion;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                half4 color : COLOR;
                float2 uv : TEXCOORD0;
                float2 uv2 : TEXCOORD1;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                half4 color : COLOR;
                float2 uv : TEXCOORD0;
                float2 data : TEXCOORD1;
            };

            Varyings vert(Attributes v)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                o.color = v.color;
                o.uv = v.uv;
                o.data = v.uv2;
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                float along = i.uv.x;
                float across = i.uv.y * 2.0 - 1.0;           // -1 .. 1
                float hard = saturate(i.data.x);
                float seed = i.data.y * 17.31;
                float t = _Time.y * _FlowSpeed;

                // Streaks flowing from neck to tip, stretched along the cloth.
                float2 sp = float2(along * 7.0 - t, across * 1.6 + seed);
                float streak = CV_Fbm(sp) * 0.65 + CV_ValueNoise(float2(along * 22.0 - t * 2.3, across * 4.0 + seed)) * 0.35;

                // Ragged soft edges: the edge line wanders with noise.
                float edgeNoise = CV_Fbm(float2(along * 9.0 - t * 0.7, seed + across * 0.3));
                float edge = 1.0 - abs(across);
                // Even a hardened weapon keeps smoky, wandering edges — it is still the veil.
                float soft = lerp(_EdgeSoftness, 0.24, hard);
                float alpha = smoothstep(0.0, soft, edge - (edgeNoise - 0.5) * lerp(0.55, 0.34, hard));

                // Tail dissolves into smoke; a weapon's tip smoulders away more subtly.
                float erode = smoothstep(lerp(0.55, 0.78, hard), 1.0, along + (streak - 0.5) * 0.5) * _Erosion * lerp(1.0, 0.5, hard);
                alpha *= 1.0 - erode;

                // Shades: deep wine at the edges and in streak troughs, bright glowing core.
                float core = pow(saturate(edge), 1.6);
                half3 col = lerp(_Deep.rgb, _Mid.rgb, saturate(core * 0.9 + streak * 0.5));
                col = lerp(col, _Glow.rgb, saturate(core * (0.35 + hard * 0.55) * (0.6 + streak * 0.6)));
                // Thin dark folds running along the cloth.
                float fold = smoothstep(0.08, 0.0, abs(frac(across * 1.5 + streak * 0.8 + seed) - 0.5) - 0.42);
                col *= 1.0 - fold * 0.35 * (1.0 - hard);

                col *= i.color.rgb;
                return half4(col, saturate(alpha * i.color.a));
            }
            ENDHLSL
        }
    }
}
