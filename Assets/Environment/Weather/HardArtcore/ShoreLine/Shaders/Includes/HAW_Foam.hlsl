#ifndef HAW_FOAM_INCLUDED
#define HAW_FOAM_INCLUDED

// Part of the include contract: parameter-driven, never reads water material properties.

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"

// Broken foam line where the scene touches the water. 1 at contact, fading out by `width` meters of depth.
half HAW_IntersectionFoam(float waterDepth, float2 uvWS, TEXTURE2D_PARAM(foamTex, foamSampler),
                          float width, float softness, float tiling, float2 scroll)
{
    half noise = SAMPLE_TEXTURE2D(foamTex, foamSampler, uvWS * tiling + scroll).r;
    half s = max(softness, 0.01);
    // Threshold runs from -s at contact (all noise passes) to 1+s at `width` (nothing passes), so the foam
    // fades out smoothly at its outer edge instead of stopping in a hard line.
    half threshold = lerp(-s, 1.0 + s, saturate(waterDepth / max(width, 1e-3)));
    return smoothstep(threshold - s, threshold + s, noise);
}

// Noise foam on the open surface, boosted on wave crests. crestFactor is 0..1 (1 = top of the highest crest).
half HAW_SurfaceFoam(float2 uvWS, half crestFactor, TEXTURE2D_PARAM(foamTex, foamSampler),
                     float amount, float crest, float tiling, float2 scroll)
{
    half noise = SAMPLE_TEXTURE2D(foamTex, foamSampler, uvWS * (tiling * 0.5) - scroll * 0.7).r;
    half threshold = 1.0 - saturate(amount + crestFactor * crest);
    return smoothstep(threshold, threshold + 0.1, noise) * step(1e-4, 1.0 - threshold);
}

// Foam bands that travel toward the shore, using water depth as a distance proxy.
// Bands live where waterDepth < range; `width` is the band thickness as a fraction of the band spacing (0..1).
half HAW_ShoreWaves(float waterDepth, float2 uvWS, half noise, float count, float speed, float width, float range, float time)
{
    float d = waterDepth / max(range, 1e-3);
    if (d >= 1.0)
        return 0.0;
    // frac grows with time, so a band of constant value moves toward smaller depth (the shore).
    float phase = frac(d * max(count, 1.0) + time * speed);
    half band = 1.0 - saturate(abs(phase - 0.5) / max(width * 0.5, 1e-3));
    return saturate(band - noise * 0.5) * (1.0 - d);
}

#endif
