#ifndef HAW_CAUSTICS_INCLUDED
#define HAW_CAUSTICS_INCLUDED

// Part of the include contract: parameter-driven, never reads water material properties.

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"

// Caustic light on the scene under the water: two scrolling samples combined with min, projected straight down
// at the scene position's XZ and faded out by water depth. Returns light intensity (multiply by the light color).
half3 HAW_Caustics(float3 sceneWS, float waterDepth, TEXTURE2D_PARAM(tex, smp),
                   float tiling, float speed, float strength, float maxDepth, float time)
{
    float2 uv = sceneWS.xz * tiling;
    half a = SAMPLE_TEXTURE2D(tex, smp, uv + time * speed * float2(1.0, 0.4)).r;
    half b = SAMPLE_TEXTURE2D(tex, smp, uv * 0.8 - time * speed * float2(0.5, 1.0)).r;
    half fade = 1.0 - saturate(waterDepth / max(maxDepth, 1e-3));
    return (min(a, b) * strength * fade).xxx;
}

#endif
