
Shader "Custom/BinocularMask"
{
    Properties
    {
        _MaskColor ("Mask Color", Color) = (0, 0, 0, 1)
        _ApertureCenter ("Aperture Center", Vector) = (0.5, 0.5, 0, 0)
        _ApertureRadius ("Aperture Radius", Range(0.05, 0.49)) = 0.36
        _EdgeSoftness ("Edge Softness", Range(0.001, 0.05)) = 0.012
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Overlay"
            "RenderType" = "Transparent"
        }

        Pass
        {
            Name "BinocularMask"
            Tags { "LightMode" = "SRPDefaultUnlit" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            ZTest Always
            Cull Off

            HLSLPROGRAM

            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _MaskColor;
                float4 _ApertureCenter;
                float _ApertureRadius;
                float _EdgeSoftness;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;

                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

                OUT.positionHCS =
                    TransformObjectToHClip(IN.positionOS.xyz);

                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);

                float2 screenUV =
                    IN.positionHCS.xy / _ScreenParams.xy;

                float2 delta =
                    screenUV - _ApertureCenter.xy;

                // Keep the aperture circular regardless of screen aspect ratio.
                delta.x *= _ScreenParams.x / _ScreenParams.y;

                float distanceFromCenter = length(delta);

                float edge = max(_EdgeSoftness, 0.0001);

                float aperture = 1.0 - smoothstep(
                    _ApertureRadius - edge,
                    _ApertureRadius + edge,
                    distanceFromCenter
                );

                float alpha = (1.0 - aperture) * _MaskColor.a;

                return half4(_MaskColor.rgb, alpha);
            }

            ENDHLSL
        }
    }
}
