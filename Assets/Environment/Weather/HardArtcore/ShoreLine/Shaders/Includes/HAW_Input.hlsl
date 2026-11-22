#ifndef HAW_INPUT_INCLUDED
#define HAW_INPUT_INCLUDED

// Water material properties. Only HAW_Input.hlsl and HAW_WaterForward.hlsl read these;
// every other include takes its inputs as parameters so add-on shaders can reuse it.

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

CBUFFER_START(UnityPerMaterial)
    half4 _ShallowColor;
    half4 _DeepColor;
    half4 _HorizonColor;
    float _DepthDistance;
    half _HorizonPower;
    float _EdgeFade;

    float _NormalStrength;
    float _NormalTiling;
    float _NormalSpeed;

    half4 _SpecularColor;
    half _Smoothness;

    float4 _WaveA;
    float4 _WaveB;
    float4 _WaveC;
    float4 _WaveD;
    float _WaveCount;
    float _WaveSpeed;
    float _WaveFadeStart;
    float _WaveFadeEnd;
    float _WaveDampingDepth;

    half4 _FoamColor;
    float _IntersectionFoamWidth;
    float _IntersectionFoamSoftness;
    float _SurfaceFoamAmount;
    float _SurfaceFoamCrest;
    float _FoamTiling;
    float _FoamSpeed;
    float _ShoreWaveCount;
    float _ShoreWaveSpeed;
    float _ShoreWaveWidth;
    float _ShoreWaveRange;

    float _RefractionStrength;
    float _CausticsStrength;
    float _CausticsTiling;
    float _CausticsSpeed;
    float _CausticsDepth;
    half _ReflectionStrength;

    float _QualityTier;
CBUFFER_END

TEXTURE2D(_NormalMap);
SAMPLER(sampler_NormalMap);
TEXTURE2D(_FoamTexture);
SAMPLER(sampler_FoamTexture);
TEXTURE2D(_CausticsTexture);
SAMPLER(sampler_CausticsTexture);

#endif
