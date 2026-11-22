#ifndef HAW_SURFACE_EFFECTS_SOURCE_INCLUDED
#define HAW_SURFACE_EFFECTS_SOURCE_INCLUDED

// For shaders of WaterEffectSource add-ons (ripples, wakes, rain). Part of the add-on contract.
// Required blend state in the pass:  Blend One One  +  BlendOp Add, Max
// Output: float4(height m, -dh/dx, -dh/dz, foam 0..1). RGB is signed and summed with other sources; A takes the max.

#include "HAW_SurfaceEffects.hlsl"

// Maps a world position into the Surface Effects buffer's clip space (top-down, XZ only).
float4 HAW_WorldToEffectsClip(float3 positionWS)
{
    float2 clipXY = HAW_SurfaceEffectsUV(positionWS) * 2.0 - 1.0;
#if UNITY_UV_STARTS_AT_TOP
    // Raw clip-space writes into a render texture are not flipped by Unity's projection matrix.
    clipXY.y = -clipXY.y;
#endif
    return float4(clipXY, 0.5, 1.0);
}

#endif
