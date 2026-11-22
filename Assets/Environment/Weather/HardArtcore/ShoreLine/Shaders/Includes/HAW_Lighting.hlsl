#ifndef HAW_LIGHTING_INCLUDED
#define HAW_LIGHTING_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

// Sharp-edged stylized sun glint (Blinn-Phong lobe pushed through a smoothstep).
half3 HAW_StylizedSpecular(half3 normalWS, half3 viewDirWS, Light light, half smoothness, half3 specColor)
{
    // float, not half: pow(nh, up to 2048) quantizes nh near 1 in half precision and the glint turns blocky on mobile.
    float3 halfDir = SafeNormalize(float3(light.direction) + float3(viewDirWS));
    float nh = saturate(dot(float3(normalWS), halfDir));
    float power = exp2(10.0 * saturate(smoothness) + 1.0);
    half lobe = smoothstep(0.3, 0.7, pow(nh, power));
    return specColor * light.color * (lobe * light.distanceAttenuation * light.shadowAttenuation);
}

// Wrapped diffuse plus ambient, kept soft for a stylized look.
half3 HAW_StylizedDiffuse(half3 albedo, half3 normalWS, Light light)
{
    half wrap = saturate(dot(normalWS, light.direction) * 0.5 + 0.5);
    half3 direct = light.color * (wrap * light.distanceAttenuation * light.shadowAttenuation);
    return albedo * (direct * 0.75 + SampleSH(normalWS) * 0.5);
}

// Fresnel term for the horizon color: 0 looking straight down, 1 at grazing angles.
half HAW_Fresnel(half3 normalWS, half3 viewDirWS, half power)
{
    return pow(1.0 - saturate(dot(normalWS, viewDirWS)), max(power, 0.01));
}

#endif
