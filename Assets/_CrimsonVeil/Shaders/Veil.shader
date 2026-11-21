// The crimson veil: painterly cloth with shades of red — dark wine edges, a glowing core,
// streaks flowing along the length, ragged soft edges and a tail that dissolves into smoke.
// uv.x = 0 at the neck → 1 at the tip, uv.y = 0..1 across the ribbon.
// uv2.x = hardness (0 cloth → 1 weapon), uv2.y = strand seed. Vertex colour = tint * alpha.
// Normal = the cloth's bright face (VeilRenderer.RibbonFace): with _Volume the side turned away from the camera is
// the dark wine reverse and a soft key light runs across the folds. (Its strands are painted back to front:
// VeilRenderer.PaintOrder.)
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
        _Cloth ("Cloth Look (the scarf painted in red ink)", Range(0, 1)) = 0
        _WeaponInk ("Ink Look on Weapons", Range(0, 1)) = 0.85
        _ClothDeep ("Cloth Wine", Color) = (0.36, 0.0, 0.01, 1)
        _ClothMid ("Cloth Crimson", Color) = (0.8, 0.02, 0.03, 1)
        _ClothLight ("Cloth Light", Color) = (1.0, 0.04, 0.04, 1)
        _Volume ("Volume (two-sided cloth, light across its folds)", Range(0, 1)) = 0
        _BackShade ("Reverse Shade", Range(0, 1)) = 0.6
        _KeyLight ("Key Light Direction (towards the light)", Vector) = (-0.45, 0.8, -0.4, 0)
        _LightAmount ("Light Across the Folds", Range(0, 1)) = 0.45
        _Glint ("Glint on Weapons (their ridge and edges catch the light)", Range(0, 2)) = 0.9
    }
    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" }
        Cull Off

        HLSLINCLUDE
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
                float _WeaponInk;
                half4 _ClothDeep;
                half4 _ClothMid;
                half4 _ClothLight;
                float _Volume;
                float _BackShade;
                float4 _KeyLight;
                float _LightAmount;
                float _Glint;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                half4 color : COLOR;
                float2 uv : TEXCOORD0;
                float2 uv2 : TEXCOORD1;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                half4 color : COLOR;
                float2 uv : TEXCOORD0;
                float2 data : TEXCOORD1;
                float3 normalWS : TEXCOORD2;
                float3 positionWS : TEXCOORD3;
            };

            Varyings vert(Attributes v)
            {
                Varyings o;
                o.positionWS = TransformObjectToWorld(v.positionOS.xyz);
                o.positionCS = TransformWorldToHClip(o.positionWS);
                o.normalWS = TransformObjectToWorldNormal(v.normalOS, false);
                o.color = v.color;
                o.uv = v.uv;
                o.data = v.uv2;
                return o;
            }

            // The cloth's shade from its real orientation (1 for a face turned to the camera): its reverse is deep wine,
            // and a soft key light runs across the bow of the cloth and its twists.
            // (glint: the light caught by a set weapon - the ridge of a blade, the round of a shaft)
            half VolumeShade(Varyings i, out half glint)
            {
                glint = 0.0;
                if (_Volume <= 0.0) return 1.0;
                float3 n = i.normalWS;
                float len2 = dot(n, n);
                if (len2 < 1e-6) return 1.0;
                n *= rsqrt(len2);
                float3 toCam = GetWorldSpaceNormalizeViewDir(i.positionWS);
                float facing = dot(n, toCam);
                float face = smoothstep(-0.12, 0.12, facing);
                half shade = lerp(_BackShade, 1.0, face);
                n = facing < 0.0 ? -n : n;                  // the side we see is lit, front or reverse
                float3 key = normalize(_KeyLight.xyz);
                float lit = saturate(dot(n, key) * 0.5 + 0.5);
                float flat = saturate(dot(toCam, key) * 0.5 + 0.5);   // a face square to the camera keeps its colour
                float hard = saturate(i.data.x);
                // a set weapon is solid: the light models it more firmly than soft cloth
                shade *= 1.0 + _LightAmount * (1.0 + hard) * (lit - flat) * 1.6;
                float3 h = normalize(key + toCam);
                glint = pow(saturate(dot(n, h)), 18.0) * hard * _Glint * _Volume;
                shade *= lerp(0.82, 1.0, saturate(abs(facing) * 3.0));   // the cloth's edge, turned away, a shade deeper
                return lerp(1.0, shade, _Volume);
            }

            half4 Shade(Varyings i)
            {
                half glint;
                half3 vtint = i.color.rgb * VolumeShade(i, glint);
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
                // fine strokes in the curls at the end. No grey smoke. Weapons are the same ink, solid, with a little heat.
                float cloth = _Cloth * lerp(1.0, _WeaponInk, hard);
                if (cloth > 0.0)
                {
                    float brush = CV_ValueNoise(float2(across * 14.0 + seed * 3.0, along * 2.0 - t * 0.15));
                    float rag = CV_Fbm(float2(along * 7.0 - t * 0.3, seed + across * 0.3));
                    float clothAlpha = smoothstep(0.0, 0.12, edge - (rag - 0.5) * 0.3 - (brush - 0.5) * 0.25 * (1.0 - core));
                    // the wash: opaque body, translucent overlapping layers towards the curls
                    // the sheet: opaque crimson at the neck, ~70 % through the middle, a translucent pink-red wash in
                    // the whorls
                    // (the key art: a solid flame body, thinning only into its smoke curls)
                    float wash = lerp(lerp(1.0, 0.8, smoothstep(0.5, 0.92, along)), 1.0, hard);  // a weapon is solid ink
                    clothAlpha *= wash * lerp(0.82, 1.0, smoothstep(0.3, 0.6, streak));
                    clothAlpha *= 1.0 - smoothstep(0.86, 0.9, along + (rag - 0.5) * 0.1);   // stroke ends in a point
                    // dry brush: the bristles leave streaks along the stroke - at its edges and all over the thin,
                    // fast strokes of the curls
                    float dry = CV_ValueNoise(float2(across * 9.0 + seed * 5.0, along * 2.0 + seed));
                    clothAlpha *= lerp(1.0, smoothstep(0.15, 0.45, dry), saturate(pow(1.0 - core, 2.0) * 0.6 + smoothstep(0.6, 0.88, along) * 0.25) * lerp(0.35, 1.0, hard));
                    float sheen = CV_Fbm(float2(across * lerp(1.5, 2.4, hard) + warp * 0.5 + seed, along * 1.5 - t * 0.25));
                    // the sheet's scarf is a sheaf of fine strands running along it: thin darker and brighter
                    // streaks, a little translucent between them
                    float strands = CV_ValueNoise(float2(across * 13.0 + warp * 1.2 + seed * 2.0, along * 0.9 - t * 0.2));
                    float strands2 = CV_ValueNoise(float2(across * 27.0 + warp * 2.0 + seed * 5.0, along * 1.6 - t * 0.3));
                    float sheaf = strands * 0.65 + strands2 * 0.35;
                    // loose: broad strokes of darker crimson through a bright body (the key art), barely a hair of the
                    // sheaf; set into a weapon: the sheaf of fine strands
                    sheen = lerp(saturate(sheen * 1.05 + sheaf * 0.12 - 0.05), saturate(sheen * 0.6 + sheaf * 0.55 - 0.08), hard);
                    clothAlpha *= lerp(lerp(0.95, 1.0, smoothstep(0.25, 0.6, sheaf)), lerp(0.78, 1.0, smoothstep(0.25, 0.6, sheaf)), hard);
                    half3 cc = lerp(_ClothDeep.rgb, _ClothMid.rgb, smoothstep(lerp(0.32, 0.18, hard), lerp(0.56, 0.46, hard), sheen));   // darker crimson strokes
                    cc = lerp(cc, _ClothLight.rgb * 1.15, smoothstep(0.58, 0.82, sheen) * (0.5 + core * 0.5));   // and hot bright ones
                    cc *= lerp(0.72, 1.0, smoothstep(0.0, 0.3, edge));               // brush edges a shade deeper
                    col = lerp(col, cc, cloth);
                    // a weapon's ink still smoulders: a little heat in its core
                    col += _ClothLight.rgb * (0.3 * hard * core) * cloth;
                    alpha = lerp(alpha, clothAlpha, cloth);
                }

                // On the cloth the vertex shade (the ribbon's dark reverse, the darker layers) blends towards the deep
                // wine instead of darkening to black, and a thin wash turns a lighter, clear red - watercolour on
                // paper, never grey.
                float clothV = _Cloth * lerp(1.0, _WeaponInk, hard);
                float shadeV = saturate(max(vtint.r, max(vtint.g, vtint.b)));
                half3 clothCol = lerp(_ClothDeep.rgb, col, smoothstep(0.25, 1.0, shadeV));
                // a thin wash is clear bright red (pink on the paper), never a dark shade seen through
                clothCol = lerp(_ClothLight.rgb, clothCol, smoothstep(0.3, 0.85, alpha * i.color.a));
                col = lerp(col * vtint, clothCol * max(1.0, vtint.r), clothV);
                // the ribbon is painted, not smoke: the layers' soft vertex alpha is lifted on the cloth
                float va = lerp(i.color.a, saturate(i.color.a * 1.6), clothV);
                float outA = saturate(alpha * va);
                // where the dark reverse or a dense dark layer shows it is thick ink: opaque, so it reads as deep
                // wine and never as a grey veil over the background
                float darkInk = (1.0 - smoothstep(0.3, 1.0, shadeV)) * clothV * (1.0 - smoothstep(0.3, 0.55, along));
                outA = lerp(outA, saturate(outA * 2.2), darkInk);
                col += _ClothLight.rgb * glint * 0.8 + glint * 0.25;   // a hot red sheen, white at its heart
                return half4(col, outA);
            }
        ENDHLSL

        Pass
        {
            Name "Forward"
            Tags { "LightMode" = "UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            ZTest LEqual

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            half4 frag(Varyings i) : SV_Target { return Shade(i); }
            ENDHLSL
        }
    }
}
