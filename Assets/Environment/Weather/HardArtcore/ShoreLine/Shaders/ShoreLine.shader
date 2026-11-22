Shader "HardArtcore/ShoreLine"
{
    Properties
    {
        [Header(Color)]
        _ShallowColor ("Shallow Color", Color) = (0.25, 0.85, 0.8, 0.55)
        _DeepColor ("Deep Color", Color) = (0.02, 0.2, 0.4, 0.95)
        _HorizonColor ("Horizon Color (A = strength)", Color) = (0.6, 0.85, 1, 0.5)
        _DepthDistance ("Depth Distance (m)", Float) = 4
        _HorizonPower ("Horizon Power", Range(0.5, 10)) = 4
        _EdgeFade ("Edge Fade (m, 0 = off)", Float) = 0.1

        [Header(Normals and Lighting)]
        [Normal][NoScaleOffset] _NormalMap ("Normal Map", 2D) = "bump" {}
        _NormalStrength ("Normal Strength", Range(0, 2)) = 0.5
        _NormalTiling ("Normal Tiling (per m)", Float) = 0.15
        _NormalSpeed ("Normal Speed", Float) = 0.05
        _SpecularColor ("Specular Color", Color) = (1, 1, 1, 1)
        _Smoothness ("Smoothness", Range(0, 1)) = 0.85

        [Header(Waves)]
        [Toggle(_HAW_WAVES)] _WavesEnabled ("Waves", Float) = 1
        _WaveA ("Wave A (dirX, dirZ, steepness, wavelength)", Vector) = (1, 0, 0.2, 12)
        _WaveB ("Wave B", Vector) = (0.6, 0.8, 0.15, 7)
        _WaveC ("Wave C", Vector) = (-0.4, 0.9, 0.1, 4)
        _WaveD ("Wave D", Vector) = (0.9, -0.3, 0.08, 2.5)
        _WaveCount ("Wave Count", Range(1, 4)) = 2
        _WaveSpeed ("Wave Speed", Float) = 1
        _WaveFadeStart ("Wave Fade Start (m)", Float) = 150
        _WaveFadeEnd ("Wave Fade End (m)", Float) = 400
        [Toggle(_HAW_WAVE_DAMPING)] _WaveDampingEnabled ("Shallow Water Damping", Float) = 1
        _WaveDampingDepth ("Damping Depth (m)", Float) = 2

        [Header(Foam)]
        [NoScaleOffset] _FoamTexture ("Foam Noise", 2D) = "white" {}
        _FoamColor ("Foam Color (A = strength)", Color) = (1, 1, 1, 1)
        _FoamTiling ("Foam Tiling (per m)", Float) = 0.4
        _FoamSpeed ("Foam Speed", Float) = 0.05
        [Toggle(_HAW_INTERSECTION_FOAM)] _IntersectionFoamEnabled ("Intersection Foam", Float) = 1
        _IntersectionFoamWidth ("Intersection Width (m)", Float) = 0.4
        _IntersectionFoamSoftness ("Intersection Softness", Range(0.01, 0.5)) = 0.1
        [Toggle(_HAW_SURFACE_FOAM)] _SurfaceFoamEnabled ("Surface Foam", Float) = 0
        _SurfaceFoamAmount ("Surface Foam Amount", Range(0, 1)) = 0.1
        _SurfaceFoamCrest ("Crest Foam", Range(0, 1)) = 0.5

        [Header(Shore Waves)]
        [Toggle(_HAW_SHORE_WAVES)] _ShoreWavesEnabled ("Shore Waves", Float) = 1
        _ShoreWaveCount ("Band Count", Range(1, 8)) = 3
        _ShoreWaveSpeed ("Band Speed", Float) = 0.25
        _ShoreWaveWidth ("Band Width", Range(0.05, 1)) = 0.25
        _ShoreWaveRange ("Range (m of depth)", Float) = 1

        [Header(Refraction and Caustics)]
        [Toggle(_HAW_REFRACTION)] _RefractionEnabled ("Refraction", Float) = 1
        _RefractionStrength ("Refraction Strength", Range(0, 0.2)) = 0.04
        [Toggle(_HAW_CAUSTICS)] _CausticsEnabled ("Caustics", Float) = 0
        [NoScaleOffset] _CausticsTexture ("Caustics Texture", 2D) = "black" {}
        _CausticsStrength ("Caustics Strength", Range(0, 3)) = 1
        _CausticsTiling ("Caustics Tiling (per m)", Float) = 0.5
        _CausticsSpeed ("Caustics Speed", Float) = 0.05
        _CausticsDepth ("Caustics Max Depth (m)", Float) = 3

        [Header(Reflections and Shadows)]
        [Toggle(_HAW_REFLECTIONS)] _ReflectionsEnabled ("Environment Reflections", Float) = 1
        _ReflectionStrength ("Reflection Strength", Range(0, 1)) = 0.5
        [Toggle(_HAW_RECEIVE_SHADOWS)] _ReceiveShadowsEnabled ("Receive Shadows", Float) = 0

        [HideInInspector] _QualityTier ("Quality Tier", Float) = 1
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "IgnoreProjector" = "True"
        }

        Pass
        {
            Name "UniversalForward"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Back

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex WaterVert
            #pragma fragment WaterFrag

            #pragma multi_compile_fog
            #pragma multi_compile_instancing
            // URP 17.1 (Unity 6.1) renamed _FORWARD_PLUS to _CLUSTER_LIGHT_LOOP; the old keyword is no longer
            // enabled at runtime. UNITY_VERSION is major*10000 + minor*10000 + patch (6000.0.84 = 60000084,
            // 6000.3.28 = 60030028), so 60010000 = 6000.1.0.
            #if UNITY_VERSION >= 60010000
                #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
            #else
                #pragma multi_compile _ _FORWARD_PLUS
            #endif
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BLENDING
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BOX_PROJECTION
            // URP strips the shadow variants the pipeline asset doesn't use.
            #pragma multi_compile_fragment _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH

            #pragma shader_feature_local _HAW_WAVES
            #pragma shader_feature_local_vertex _HAW_WAVE_DAMPING
            #pragma shader_feature_local_fragment _HAW_INTERSECTION_FOAM
            #pragma shader_feature_local_fragment _HAW_SURFACE_FOAM
            #pragma shader_feature_local_fragment _HAW_SHORE_WAVES
            #pragma shader_feature_local_fragment _HAW_REFRACTION
            #pragma shader_feature_local_fragment _HAW_CAUSTICS
            #pragma shader_feature_local_fragment _HAW_RECEIVE_SHADOWS
            #pragma shader_feature_local_fragment _HAW_REFLECTIONS

            // Transparent: sample the shadow map directly, never the screen-space shadow texture (which holds opaque depth).
            #define _SURFACE_TYPE_TRANSPARENT 1

            #include "Includes/HAW_WaterForward.hlsl"
            ENDHLSL
        }
    }

    FallBack Off
    CustomEditor "HardArtcore.ShoreLine.Editor.ShoreLineShaderGUI"
}
