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

                float loose = 1.0 - hard;
                float edge = 1.0 - abs(across);
                float core = pow(saturate(edge), 1.6);

                // Flame streaks: long stripes running along the cloth, their lateral position bent by a slow
                // domain warp so they ripple and lick like the tongues of a fire.
                float warp = (CV_Fbm(float2(along * 3.0 - t * 0.5, seed)) - 0.5) * 1.4;
                float stripeCoord = across * 1.7 + warp + seed;
                float stripes = CV_Fbm(float2(stripeCoord * 1.8, along * 1.4 - t * 0.8));
                float fine = CV_ValueNoise(float2(stripeCoord * 5.0, along * 2.5 - t * 1.3));
                float streak = saturate(stripes * 0.8 + fine * 0.3 + 0.12);

                // Ragged, flickering edges, stretched along the flow.
                float edgeNoise = CV_Fbm(float2(along * 6.0 - t * 0.9, seed + across * 0.4));
                // Even a hardened weapon keeps smoky, wandering edges — it is still the veil.
                float soft = lerp(_EdgeSoftness, 0.24, hard);
                float alpha = smoothstep(0.0, soft, edge - (edgeNoise - 0.5) * lerp(0.7, 0.34, hard));

                // Translucent gaps between the streaks: the flame is layered, you see one tongue through another.
                alpha *= lerp(1.0, lerp(0.75, 1.0, smoothstep(0.25, 0.65, streak)), loose * (1.0 - core * 0.6));

                // Tail dissolves into smoke; a weapon's tip smoulders away more subtly.
                // (the strand's end curl lives at along 0.75..0.9, so erosion only frays its very tip)
                float erode = smoothstep(lerp(0.8, 0.84, hard), 1.0, along + (streak - 0.5) * 0.3) * _Erosion * lerp(1.0, 0.5, hard);
                alpha *= 1.0 - erode;

                // Palette of a fire seen in red ink: near-black wine → deep crimson → red → hot bright red.
                half3 wine = _Deep.rgb * 0.55;
                half3 col = lerp(wine, _Deep.rgb, smoothstep(0.05, 0.3, streak));
                col = lerp(col, _Mid.rgb, smoothstep(0.28, 0.6, streak));
                col = lerp(col, _Glow.rgb, smoothstep(0.62, 0.95, streak) * (0.45 + core * 0.55));
                // Edges sink into darker wine, the body is lit from within.
                col *= lerp(0.7, 1.0, saturate(core * 1.4));
                // A weapon glows more evenly along its blade.
                col = lerp(col, lerp(_Mid.rgb, _Glow.rgb, core * 0.6 * (0.6 + streak * 0.6)), hard * 0.4);

                col *= i.color.rgb;
                return half4(col, saturate(alpha * i.color.a));
            }
            ENDHLSL
        }
    }
}
