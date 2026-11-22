#ifndef HAW_DEPTH_INCLUDED
#define HAW_DEPTH_INCLUDED

// Part of the HardArtcore ShoreLine include contract: functions take their inputs as parameters
// and never read water material properties, so add-on shaders can reuse them.

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

// Linear eye depth from a raw depth-texture sample, for perspective and orthographic cameras.
float HAW_LinearEyeDepth(float rawDepth)
{
    if (unity_OrthoParams.w > 0.5)
    {
    #if UNITY_REVERSED_Z
        float depth01 = 1.0 - rawDepth;
    #else
        float depth01 = rawDepth;
    #endif
        return lerp(_ProjectionParams.y, _ProjectionParams.z, depth01);
    }
    return LinearEyeDepth(rawDepth, _ZBufferParams);
}

// World position of the scene surface behind a screen pixel. Works for perspective and orthographic cameras.
float3 HAW_ReconstructWorldPos(float2 screenUV, float rawDepth)
{
#if !UNITY_REVERSED_Z
    // OpenGL-style APIs (GLES3): the depth texture stores [0,1] but clip space Z is [-1,1].
    rawDepth = lerp(UNITY_NEAR_CLIP_VALUE, 1.0, rawDepth);
#endif
    return ComputeWorldSpacePosition(screenUV, rawDepth, UNITY_MATRIX_I_VP);
}

// True when the depth texture holds the cleared far plane (nothing was drawn behind the water, e.g. open sea
// or sky). Water there must be treated as infinitely deep, or foam and shore bands appear near the horizon.
bool HAW_IsFarPlane(float rawDepth)
{
#if UNITY_REVERSED_Z
    return rawDepth <= 1e-7;
#else
    return rawDepth >= 1.0 - 1e-7;
#endif
}

// Vertical water depth between the water surface and the scene behind it. View independent,
// so it is identical for perspective and orthographic cameras.
float HAW_WaterDepth(float3 surfaceWS, float3 sceneWS)
{
    return max(surfaceWS.y - sceneWS.y, 0.0);
}

// Anything closer than this in front of a water vertex is taken as the ground the vertex is buried in (beach sand).
// Farther occluders (a mast, a hill between camera and sea) are unrelated to the water depth and are ignored.
#define HAW_VERTEX_OCCLUDER_RANGE 2.0

// Vertex stage: vertical water depth under a water vertex at its rest position, read from the camera depth texture
// with an explicit LOD (legal in a vertex shader on every API and URP version). Returns 1e4 (deep) where the floor
// can't be known: behind the camera, at the cleared far plane, or behind an unrelated occluder. Off-screen vertices
// read the nearest screen-edge pixel, so the result stays continuous at the screen edges.
float HAW_VertexWaterDepth(float3 positionWS)
{
    float4 positionCS = TransformWorldToHClip(positionWS);
    if (positionCS.w <= 1e-5)
        return 1e4;
    float2 screenUV = saturate(ComputeScreenPos(positionCS).xy / positionCS.w);
    float2 uv = ClampAndScaleUVForBilinear(UnityStereoTransformScreenSpaceTex(screenUV), _CameraDepthTexture_TexelSize.xy);
    float rawDepth = SAMPLE_TEXTURE2D_X_LOD(_CameraDepthTexture, sampler_PointClamp, uv, 0).r;
    if (HAW_IsFarPlane(rawDepth))
        return 1e4;
    float vertexEyeDepth = -TransformWorldToView(positionWS).z;
    if (HAW_LinearEyeDepth(rawDepth) < vertexEyeDepth - HAW_VERTEX_OCCLUDER_RANGE)
        return 1e4;
    return HAW_WaterDepth(positionWS, HAW_ReconstructWorldPos(screenUV, rawDepth));
}

#endif
