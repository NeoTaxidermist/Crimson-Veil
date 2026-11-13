#ifndef CRIMSON_VEIL_COMMON_INCLUDED
#define CRIMSON_VEIL_COMMON_INCLUDED

// Global atmosphere, set by AtmosphereController.
float4 _CV_MistColor;     // rgb mist colour
float4 _CV_MistParams;    // x = mist top (world Y), y = fade height, z = distance where mist reaches full strength, w = global strength
float4 _CV_Wind;          // x = strength, y = frequency, z = gust scale, w = unused

float CV_Hash21(float2 p)
{
    p = frac(p * float2(123.34, 456.21));
    p += dot(p, p + 45.32);
    return frac(p.x * p.y);
}

float CV_ValueNoise(float2 p)
{
    float2 i = floor(p);
    float2 f = frac(p);
    float a = CV_Hash21(i);
    float b = CV_Hash21(i + float2(1, 0));
    float c = CV_Hash21(i + float2(0, 1));
    float d = CV_Hash21(i + float2(1, 1));
    float2 u = f * f * (3.0 - 2.0 * f);
    return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
}

// Distance to the nearest Voronoi cell border (0 on the border) — used for stone cracks.
float CV_VoronoiEdge(float2 p)
{
    float2 g = floor(p);
    float2 f = frac(p);
    float d1 = 8.0, d2 = 8.0;
    [unroll] for (int y = -1; y <= 1; y++)
    [unroll] for (int x = -1; x <= 1; x++)
    {
        float2 o = float2(x, y);
        float2 r = o + float2(CV_Hash21(g + o), CV_Hash21(g + o + 17.3)) - f;
        float d = dot(r, r);
        if (d < d1) { d2 = d1; d1 = d; }
        else if (d < d2) { d2 = d; }
    }
    return sqrt(d2) - sqrt(d1);
}

float CV_Fbm(float2 p)
{
    float v = 0.0;
    float a = 0.5;
    [unroll] for (int i = 0; i < 5; i++)
    {
        v += a * CV_ValueNoise(p);
        p = p * 2.03 + float2(17.1, 9.7);
        a *= 0.5;
    }
    return v;
}

// Mist that pools near the ground and grows with distance from the camera.
float CV_GroundMist(float3 positionWS, float strength)
{
    float top = _CV_MistParams.x;
    float fade = max(0.01, _CV_MistParams.y);
    float dist = distance(positionWS, _WorldSpaceCameraPos);
    float heightTerm = saturate((top - positionWS.y) / fade);
    float distTerm = saturate(dist / max(0.01, _CV_MistParams.z));
    float wobble = CV_Fbm(positionWS.xz * 0.08 + float2(_Time.y * 0.03, 0)) * 0.5 + 0.75;
    return saturate(heightTerm * distTerm * wobble * strength * _CV_MistParams.w);
}

float3 CV_WindOffset(float3 positionWS, float weight)
{
    float t = _Time.y * _CV_Wind.y;
    float gust = CV_ValueNoise(positionWS.xz * _CV_Wind.z + float2(t * 0.35, 0)) * 1.4;
    float sway = sin(t + positionWS.x * 0.7 + positionWS.z * 0.3) * 0.6 + gust;
    return float3(sway, 0, sin(t * 0.7 + positionWS.x) * 0.25) * _CV_Wind.x * weight;
}

#endif
