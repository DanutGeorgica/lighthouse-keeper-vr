#ifndef HAW_WAVES_INCLUDED
#define HAW_WAVES_INCLUDED

// GPU mirror of GerstnerWaves.cs. Each wave is (dirX, dirZ, steepness 0..1, wavelength m).
// Any change here must be mirrored in GerstnerWaves.cs (CPU height queries / buoyancy).

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"

#define HAW_GRAVITY 9.8
#define HAW_MIN_WAVELENGTH 0.01
#define HAW_MIN_DIRECTION 1e-4

// Returns the displacement of the surface point that starts at positionWS (XZ used) and its world normal.
float3 HAW_GerstnerOffset(float3 positionWS, float4 waves[4], int waveCount, float speed, float time, out float3 normalWS)
{
    int count = clamp(waveCount, 0, 4);

    // When the total steepness exceeds 1 the surface would fold over itself; scale every wave down.
    float totalSteepness = 0.0;
    for (int s = 0; s < count; s++)
        totalSteepness += length(waves[s].xy) >= HAW_MIN_DIRECTION ? saturate(waves[s].z) : 0.0;
    float steepnessScale = totalSteepness > 1.0 ? 1.0 / totalSteepness : 1.0;

    float3 offset = 0.0;
    float3 tangent = float3(1.0, 0.0, 0.0);
    float3 binormal = float3(0.0, 0.0, 1.0);

    for (int i = 0; i < count; i++)
    {
        float4 w = waves[i];
        float len = length(w.xy);
        if (len < HAW_MIN_DIRECTION)
            continue;
        float2 d = w.xy / len;

        float k = TWO_PI / max(w.w, HAW_MIN_WAVELENGTH);
        float c = sqrt(HAW_GRAVITY / k) * speed;
        float q = saturate(w.z) * steepnessScale;
        float a = q / k;
        float f = k * (dot(d, positionWS.xz) - c * time);
        float sinF, cosF;
        sincos(f, sinF, cosF);

        offset += float3(d.x * a * cosF, a * sinF, d.y * a * cosF);

        tangent += float3(-d.x * d.x * q * sinF, d.x * q * cosF, -d.x * d.y * q * sinF);
        binormal += float3(-d.x * d.y * q * sinF, d.y * q * cosF, -d.y * d.y * q * sinF);
    }

    // At a steepness-1 crest the tangent collapses to zero length; fall back to up instead of returning NaN.
    float3 n = cross(binormal, tangent);
    normalWS = dot(n, n) > 1e-8 ? normalize(n) : float3(0.0, 1.0, 0.0);
    return offset;
}

// Largest possible displacement (mirror of GerstnerWaves.MaxAmplitude). Used to normalize crest height.
float HAW_GerstnerMaxAmplitude(float4 waves[4], int waveCount)
{
    int count = clamp(waveCount, 0, 4);
    float totalSteepness = 0.0;
    for (int s = 0; s < count; s++)
        totalSteepness += length(waves[s].xy) >= HAW_MIN_DIRECTION ? saturate(waves[s].z) : 0.0;
    float steepnessScale = totalSteepness > 1.0 ? 1.0 / totalSteepness : 1.0;

    float sum = 0.0;
    for (int i = 0; i < count; i++)
        if (length(waves[i].xy) >= HAW_MIN_DIRECTION)
            sum += saturate(waves[i].z) * steepnessScale / (TWO_PI / max(waves[i].w, HAW_MIN_WAVELENGTH));
    return sum;
}

// Shallow Water Damping: wave displacement multiplier for water waterDepth meters deep. 0 at the shoreline, easing
// up to 1 at dampingDepth and below (smoothstep). Mirror of GerstnerWaves.ShallowWaterDamping.
float HAW_ShallowWaterDamping(float waterDepth, float dampingDepth)
{
    float t = saturate(waterDepth / max(dampingDepth, 1e-3));
    return t * t * (3.0 - 2.0 * t);
}

#endif
