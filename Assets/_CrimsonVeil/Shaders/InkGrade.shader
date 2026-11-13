// Final grade: everything becomes grey ink except crimson, plus paper grain.
// Used by a Full Screen Pass Renderer Feature (After Post Processing).
Shader "CrimsonVeil/InkGrade"
{
    Properties
    {
        _RedThreshold ("Red Threshold", Range(0, 1)) = 0.12
        _RedBoost ("Red Saturation Boost", Range(0, 2)) = 1.15
        _PaperStrength ("Paper Grain", Range(0, 0.3)) = 0.06
        _PaperScale ("Paper Scale", Float) = 900
        _Contrast ("Grey Contrast", Range(0.5, 2)) = 1.12
        _Toe ("Shadow Crush", Range(0, 0.2)) = 0.03
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }
        ZWrite Off ZTest Always Cull Off Blend Off

        Pass
        {
            Name "InkGrade"
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            #include "CVCommon.hlsl"

            float _RedThreshold;
            float _RedBoost;
            float _PaperStrength;
            float _PaperScale;
            float _Contrast;
            float _Toe;

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float2 uv = input.texcoord;
                half3 c = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv).rgb;

                // Work on perceptual (sRGB-encoded) values so contrast/crush don't flatten the dark greys.
                half lum = dot(c, half3(0.2126, 0.7152, 0.0722));
                half g = LinearToSRGB(lum);
                g = saturate((g - 0.5) * _Contrast + 0.5);
                g = saturate((g - _Toe) / (1.0 - _Toe));
                half grey = SRGBToLinear(g);

                // How "crimson" is this pixel? Red must dominate green and blue.
                half redness = saturate((c.r - max(c.g, c.b)) / max(c.r, 1e-3) - _RedThreshold) / (1.0 - _RedThreshold);
                redness = smoothstep(0.0, 0.6, redness);
                half3 red = lerp(lum.xxx, c, _RedBoost);
                half3 col = lerp(grey.xxx, red, redness);

                // Paper fibres (static, aspect corrected).
                float2 puv = uv * float2(_ScreenParams.x / _ScreenParams.y, 1.0) * _PaperScale;
                float paper = CV_ValueNoise(puv) * 0.6 + CV_ValueNoise(puv * 0.13) * 0.4;
                col = SRGBToLinear(saturate(LinearToSRGB(col) * (1.0 + (paper - 0.5) * _PaperStrength * 2.0)));

                return half4(col, 1);
            }
            ENDHLSL
        }
    }
}
