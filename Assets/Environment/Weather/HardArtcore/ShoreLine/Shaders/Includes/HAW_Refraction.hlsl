#ifndef HAW_REFRACTION_INCLUDED
#define HAW_REFRACTION_INCLUDED

// Part of the include contract: parameter-driven, never reads water material properties.

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"
#include "HAW_Depth.hlsl"

// Set per camera by ShoreLineFeature: 1 when the camera opaque texture exists this frame, else 0.
// Read it with UNITY_BRANCH and fall back to alpha blending when 0.
float _HAW_OpaqueTextureAvailable;

// Scene color behind the water, distorted by the surface normal. If the distorted sample lands on something in
// front of the water surface (a pier, a boat hull), the undistorted UV is used so foreground objects don't leak in.
// surfaceEyeDepth: linear eye depth of the water surface at this pixel.
half3 HAW_Refraction(float2 screenUV, half3 normalWS, float strength, float surfaceEyeDepth)
{
    float2 refractedUV = screenUV + normalWS.xz * strength;
    float sceneEyeDepth = HAW_LinearEyeDepth(SampleSceneDepth(refractedUV));
    float2 uv = sceneEyeDepth < surfaceEyeDepth ? screenUV : refractedUV;
    return SampleSceneColor(uv);
}

#endif
