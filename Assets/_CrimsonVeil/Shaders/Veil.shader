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
        _Cloth ("Cloth Look (loose scarf as in the concept)", Range(0, 1)) = 0
        _ClothDeep ("Cloth Wine", Color) = (0.42, 0.03, 0.04, 1)
        _ClothMid ("Cloth Crimson", Color) = (0.85, 0.06, 0.06, 1)
        _ClothLight ("Cloth Light", Color) = (1.0, 0.3, 0.25, 1)
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
                float _Cloth;
                half4 _ClothDeep;
                half4 _ClothMid;
                half4 _ClothLight;
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
                float erode = smoothstep(lerp(0.8, 0.84, hard), 1.0, along + (streak - 0.5) * 0.3) * _Erosion * lerp(1.0, 0.15, hard); // weapons keep their ends (a bow ends in its lower limb)
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

                // The loose scarf as in the key art (_Cloth): a ribbon painted in red ink - saturated crimson with a
                // lighter sheen along it and darker wine where the layers of the wash overlap; firm dry-brush edges
                // (streaks running along the stroke), nearly opaque in the body, thinning to a translucent wash and
                // fine strokes in the curls at the end. No grey smoke. Weapons (hard) keep the fire look.
                float cloth = _Cloth * loose;
                if (cloth > 0.0)
                {
                    float brush = CV_ValueNoise(float2(across * 14.0 + seed * 3.0, along * 2.0 - t * 0.15));
                    float rag = CV_Fbm(float2(along * 7.0 - t * 0.3, seed + across * 0.3));
                    float clothAlpha = smoothstep(0.0, 0.12, edge - (rag - 0.5) * 0.3 - (brush - 0.5) * 0.25 * (1.0 - core));
                    // the wash: opaque body, translucent overlapping layers towards the curls
                    float wash = lerp(1.0, 0.7, smoothstep(0.45, 0.9, along));
                    clothAlpha *= wash * lerp(0.82, 1.0, smoothstep(0.3, 0.6, streak));
                    clothAlpha *= 1.0 - smoothstep(0.86, 0.9, along + (rag - 0.5) * 0.1);   // stroke ends in a point
                    float sheen = CV_Fbm(float2(across * 2.4 + warp * 0.5 + seed, along * 1.5 - t * 0.25));
                    half3 cc = lerp(_ClothDeep.rgb, _ClothMid.rgb, smoothstep(0.18, 0.42, sheen));
                    cc = lerp(cc, _ClothLight.rgb, smoothstep(0.62, 0.85, sheen) * (0.35 + core * 0.65));
                    cc *= lerp(0.72, 1.0, smoothstep(0.0, 0.3, edge));               // brush edges a shade deeper
                    col = lerp(col, cc, cloth);
                    alpha = lerp(alpha, clothAlpha, cloth);
                }

                // On the cloth the vertex shade (the ribbon's dark reverse, the darker layers) blends towards the deep
                // wine instead of darkening to black, and a thin wash turns a lighter, clear red - watercolour on
                // paper, never grey.
                float clothV = _Cloth * loose;
                float shadeV = saturate(max(i.color.r, max(i.color.g, i.color.b)));
                half3 clothCol = lerp(_ClothDeep.rgb, col, smoothstep(0.25, 1.0, shadeV));
                clothCol = lerp(_ClothMid.rgb, clothCol, smoothstep(0.1, 0.4, alpha * i.color.a));
                col = lerp(col * i.color.rgb, clothCol * max(1.0, i.color.r), clothV);
                // the ribbon is painted, not smoke: the layers' soft vertex alpha is lifted on the cloth
                float va = lerp(i.color.a, saturate(i.color.a * 1.6), clothV);
                return half4(col, saturate(alpha * va));
            }
            ENDHLSL
        }
    }
}
