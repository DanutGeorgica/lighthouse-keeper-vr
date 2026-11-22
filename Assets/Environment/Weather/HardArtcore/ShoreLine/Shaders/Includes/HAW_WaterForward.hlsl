#ifndef HAW_WATER_FORWARD_INCLUDED
#define HAW_WATER_FORWARD_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
#include "HAW_Version.hlsl"
#include "HAW_Input.hlsl"
#include "HAW_Depth.hlsl"
#include "HAW_Waves.hlsl"
#include "HAW_Lighting.hlsl"
#include "HAW_Foam.hlsl"
#include "HAW_Refraction.hlsl"
#include "HAW_Caustics.hlsl"
#include "HAW_SurfaceEffects.hlsl"

struct Attributes
{
    float4 positionOS : POSITION;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings
{
    float4 positionCS : SV_POSITION;
    float3 positionWS : TEXCOORD0;
    half3 normalWS    : TEXCOORD1;
    float4 screenPos  : TEXCOORD2;
    half crest        : TEXCOORD3; // 0..1 normalized wave height, for crest foam
    UNITY_VERTEX_OUTPUT_STEREO
};

Varyings WaterVert(Attributes input)
{
    Varyings output = (Varyings)0;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

    float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
    float3 normalWS = float3(0.0, 1.0, 0.0);

#if defined(_HAW_WAVES)
    float4 waves[4] = { _WaveA, _WaveB, _WaveC, _WaveD };
    float3 waveNormal;
    float3 waveOffset = HAW_GerstnerOffset(positionWS, waves, (int)_WaveCount, _WaveSpeed, _Time.y, waveNormal);
    // Fade waves out with horizontal camera distance (ocean mode: must end inside the fine inner grid).
    float distanceXZ = distance(positionWS.xz, _WorldSpaceCameraPos.xz);
    float fade = 1.0 - saturate((distanceXZ - _WaveFadeStart) / max(_WaveFadeEnd - _WaveFadeStart, 1e-3));
#if defined(_HAW_WAVE_DAMPING)
    // Waves calm down in shallow water, so a shoreline on flat ground doesn't follow the straight wave crests.
    fade *= HAW_ShallowWaterDamping(HAW_VertexWaterDepth(positionWS), _WaveDampingDepth);
#endif
    positionWS += waveOffset * fade;
    normalWS = normalize(lerp(normalWS, waveNormal, fade));
    output.crest = saturate(waveOffset.y * fade / max(HAW_GerstnerMaxAmplitude(waves, (int)_WaveCount), 1e-3));
#endif

    // Add-on Surface Effects (ripples, wakes): uniform branch, no variants, skipped when no source is active.
    UNITY_BRANCH
    if (_HAW_SurfaceEffectsEnabled > 0.5)
        positionWS.y += HAW_SampleSurfaceEffectsLOD(positionWS).r;

    output.positionWS = positionWS;
    output.normalWS = normalWS;
    output.positionCS = TransformWorldToHClip(positionWS);
    output.screenPos = ComputeScreenPos(output.positionCS);
    return output;
}

// Two scrolling world-space normal map samples, applied on top of the geometric (wave) normal.
// (A one-sample option was measured on the OnePlus 3T: ~1 fps, within noise, so it isn't offered.)
half3 HAW_DetailNormal(float3 positionWS, half3 geometricNormalWS)
{
    float2 uv = positionWS.xz * _NormalTiling;
    float t = _Time.y * _NormalSpeed;
    half3 n1 = UnpackNormalScale(SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, uv + t * float2(1.0, 0.3)), _NormalStrength);
    half3 n2 = UnpackNormalScale(SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, uv * 0.77 - t * float2(0.4, 1.0)), _NormalStrength);
    half3 normalTS = normalize(half3(n1.xy + n2.xy, n1.z * n2.z));

    // World-space TBN around the geometric normal (the mesh has no tangents).
    half3 tangentWS = normalize(cross(geometricNormalWS, half3(0.0, 0.0, 1.0)));
    half3 bitangentWS = cross(tangentWS, geometricNormalWS);
    return normalize(tangentWS * normalTS.x + bitangentWS * normalTS.y + geometricNormalWS * normalTS.z);
}

half4 WaterFrag(Varyings input) : SV_Target
{
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

    float2 screenUV = input.screenPos.xy / input.screenPos.w;
    float rawDepth = SampleSceneDepth(screenUV);
    float3 sceneWS = HAW_ReconstructWorldPos(screenUV, rawDepth);
    float waterDepth = HAW_IsFarPlane(rawDepth) ? 1e4 : HAW_WaterDepth(input.positionWS, sceneWS);

    // Per-fragment view direction: ring triangles can be a kilometer long.
    half3 viewDirWS = SafeNormalize(GetWorldSpaceViewDir(input.positionWS));
    half3 normalWS = HAW_DetailNormal(input.positionWS, normalize(input.normalWS));

    float4 effects = 0.0;
    UNITY_BRANCH
    if (_HAW_SurfaceEffectsEnabled > 0.5)
    {
        effects = HAW_SampleSurfaceEffects(input.positionWS);
        normalWS = normalize(normalWS + half3(effects.g, 0.0, effects.b));
    }

    half depthT = saturate(waterDepth / max(_DepthDistance, 1e-3));
    half4 baseColor = lerp(_ShallowColor, _DeepColor, depthT);
    half fresnel = HAW_Fresnel(normalWS, viewDirWS, _HorizonPower);
    baseColor.rgb = lerp(baseColor.rgb, _HorizonColor.rgb, fresnel * _HorizonColor.a);

#if defined(_HAW_RECEIVE_SHADOWS)
    Light mainLight = GetMainLight(TransformWorldToShadowCoord(input.positionWS));
#else
    Light mainLight = GetMainLight();
#endif
    half3 color = HAW_StylizedDiffuse(baseColor.rgb, normalWS, mainLight);
    color += HAW_StylizedSpecular(normalWS, viewDirWS, mainLight, _Smoothness, _SpecularColor.rgb);

#if defined(_HAW_REFLECTIONS)
    // Environment reflection (reflection probes / skybox), stronger at grazing angles.
    half3 reflectDir = reflect(-viewDirWS, normalWS);
    half3 reflection = GlossyEnvironmentReflection(reflectDir, input.positionWS, 1.0 - _Smoothness, 1.0, screenUV);
    color += reflection * (fresnel * _ReflectionStrength);
#endif

    half alpha = baseColor.a;
    alpha *= _EdgeFade > 0.0 ? saturate(waterDepth / _EdgeFade) : 1.0;

    // Foam: intersection, surface and shore bands combine with max and are lit like the water.
    half foam = 0.0;
#if defined(_HAW_INTERSECTION_FOAM) || defined(_HAW_SURFACE_FOAM) || defined(_HAW_SHORE_WAVES)
    float2 uvWS = input.positionWS.xz;
    float2 foamScroll = _Time.y * _FoamSpeed * float2(1.0, 0.6);
#endif
#if defined(_HAW_INTERSECTION_FOAM)
    foam = max(foam, HAW_IntersectionFoam(waterDepth, uvWS, TEXTURE2D_ARGS(_FoamTexture, sampler_FoamTexture),
        _IntersectionFoamWidth, _IntersectionFoamSoftness, _FoamTiling, foamScroll));
#endif
#if defined(_HAW_SURFACE_FOAM)
    foam = max(foam, HAW_SurfaceFoam(uvWS, input.crest, TEXTURE2D_ARGS(_FoamTexture, sampler_FoamTexture),
        _SurfaceFoamAmount, _SurfaceFoamCrest, _FoamTiling, foamScroll));
#endif
#if defined(_HAW_SHORE_WAVES)
    half shoreNoise = SAMPLE_TEXTURE2D(_FoamTexture, sampler_FoamTexture, uvWS * (_FoamTiling * 0.3)).r;
    foam = max(foam, HAW_ShoreWaves(waterDepth, uvWS, shoreNoise, _ShoreWaveCount, _ShoreWaveSpeed,
        _ShoreWaveWidth, _ShoreWaveRange, _Time.y));
#endif
    foam = max(foam, (half)saturate(effects.a));
    foam *= _FoamColor.a;
    color = lerp(color, HAW_StylizedDiffuse(_FoamColor.rgb, normalWS, mainLight) * 1.6, foam);
    alpha = lerp(alpha, 1.0, foam);

    // What is seen through the water: caustic light on the scene below, optionally refracted.
    half3 caustics = 0.0;
#if defined(_HAW_CAUSTICS)
    caustics = HAW_Caustics(sceneWS, waterDepth, TEXTURE2D_ARGS(_CausticsTexture, sampler_CausticsTexture),
        _CausticsTiling, _CausticsSpeed, _CausticsStrength, _CausticsDepth, _Time.y) * mainLight.color;
#endif
    // Fog per fragment: linear fog saturates per vertex, which is wrong across kilometer-long ring triangles.
    float viewZ = -TransformWorldToView(input.positionWS).z;
    half fogFactor = ComputeFogFactorZ0ToFar(max(viewZ - _ProjectionParams.y, 0.0));

    bool refract = false;
#if defined(_HAW_REFRACTION)
    // Falls back to alpha blending when this camera has no opaque texture (never samples an unset texture).
    refract = _HAW_OpaqueTextureAvailable > 0.5;
#endif
    UNITY_BRANCH
    if (refract)
    {
        // The opaque texture is already fogged: fog only the water's own color, then blend over it.
        half3 behind = HAW_Refraction(screenUV, normalWS, _RefractionStrength, viewZ) + caustics * ComputeFogIntensity(fogFactor);
        color = lerp(behind, MixFog(color, fogFactor), alpha);
        alpha = 1.0;
    }
    else
    {
        // Alpha blending shows the framebuffer through (1 - alpha). Adding k*(1-a)/a here yields exactly
        // a*c + (1-a)*(dst + k) after blending (clamped on LDR targets, so caustics only get weaker).
        color += caustics * ((1.0 - alpha) / max(alpha, 0.05));
        color = MixFog(color, fogFactor);
    }
    return half4(color, alpha);
}

#endif
