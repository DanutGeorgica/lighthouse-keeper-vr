#ifndef HAW_SURFACE_EFFECTS_INCLUDED
#define HAW_SURFACE_EFFECTS_INCLUDED

// Surface Effects buffer, part of the add-on contract. Set by ShoreLineFeature:
//   _HAW_SurfaceEffects        RGBA half: R = height offset (m), G/B = -dh/dx, -dh/dz (normal tilt, added to the
//                              normal as (G, 0, B)), A = foam 0..1. RGB from all sources is summed, A is the max.
//   _HAW_SurfaceEffectsRect    (minX, minZ, 1/size, size) of the world area the buffer covers
//   _HAW_SurfaceEffectsEnabled 1 while this camera's renderer drew the buffer (sources registered), else 0 (read it with UNITY_BRANCH)

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"

TEXTURE2D(_HAW_SurfaceEffects);
SAMPLER(sampler_HAW_SurfaceEffects);
float4 _HAW_SurfaceEffectsRect;
float _HAW_SurfaceEffectsEnabled;

float2 HAW_SurfaceEffectsUV(float3 positionWS)
{
    return (positionWS.xz - _HAW_SurfaceEffectsRect.xy) * _HAW_SurfaceEffectsRect.z;
}

// 1 inside the buffer, fading to 0 over the outer 10% so effects never smear past its edge.
float HAW_SurfaceEffectsEdgeFade(float2 uv)
{
    float2 edge = min(uv, 1.0 - uv);
    return saturate(min(edge.x, edge.y) / 0.1);
}

// Fragment-stage sample. Uses LOD 0 so it is safe inside a uniform branch (no derivatives needed).
float4 HAW_SampleSurfaceEffects(float3 positionWS)
{
    float2 uv = HAW_SurfaceEffectsUV(positionWS);
    return SAMPLE_TEXTURE2D_LOD(_HAW_SurfaceEffects, sampler_HAW_SurfaceEffects, uv, 0) * HAW_SurfaceEffectsEdgeFade(uv);
}

// Vertex-stage sample.
float4 HAW_SampleSurfaceEffectsLOD(float3 positionWS)
{
    return HAW_SampleSurfaceEffects(positionWS);
}

#endif
