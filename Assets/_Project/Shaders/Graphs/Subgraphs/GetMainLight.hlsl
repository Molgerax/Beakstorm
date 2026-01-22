#ifndef SHADERGRAPH_PREVIEW
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#endif

float _DimFactor;
uint _DimMask;

#define DIM_FACTOR (1.0 - _DimFactor)

#ifndef SHADERGRAPH_PREVIEW
struct Surface
{
    float3 normal;
    float3 view;
    float shininess;
    float smoothness;
    float3 bakedGI;
};

float linearstep(float a, float b, float t)
{
    return saturate((t - a) / (b - a));
}

float3 CalculateGlobalIllumination(Surface s)
{
    return s.bakedGI * DIM_FACTOR;
}

float3 CalculateCelShading(Light l, Surface s, float step)
{
    float shadowAtten = saturate(linearstep(0, step, l.shadowAttenuation));// + 0.25);
    float distanceAtten = linearstep(0, step, l.distanceAttenuation);
    float attenuation = shadowAtten * distanceAtten;
    float diffuse = saturate(dot(s.normal, l.direction));
    diffuse = smoothstep(0, step, diffuse);
    diffuse *= attenuation;
    
    float3 h = SafeNormalize(l.direction + s.view);
    float specular = saturate(dot(s.normal, h));
    specular = pow(specular, s.shininess);
    specular *= diffuse * s.smoothness;

    float dimming = ((l.layerMask) & _DimMask) > 0 ? 1 : DIM_FACTOR;
    
    return (diffuse + specular) * l.color * dimming;
}

float3 CalculateLambertShading(Light l, Surface s, float ao)
{
    float shadowAtten = saturate(linearstep(0, 0.1, l.shadowAttenuation));// + 0.25);
    float distanceAtten = linearstep(0, 0.1, l.distanceAttenuation);
    shadowAtten = shadowAtten * 0.5 + 0.5;
    float attenuation = shadowAtten * distanceAtten * ao;
    float diffuse = saturate(dot(s.normal, l.direction) * 0.5 + 0.5);
    //diffuse = smoothstep(0, 0.1, diffuse);
    diffuse *= attenuation;
    
    float3 h = SafeNormalize(l.direction + s.view);
    float specular = saturate(dot(s.normal, h));
    specular = pow(specular, s.shininess);
    specular *= diffuse * s.smoothness;

    float dimming = ((l.layerMask) & _DimMask) > 0 ? 1 : DIM_FACTOR;
    
    return (diffuse + specular) * l.color * dimming;
}
#endif

void GetDimLight_float(out float3 Color)
{
    Color = 1;
    
    Color = DIM_FACTOR;
}


void LightingCelShaded_float(float3 Position, float2 ScreenPosition, float3 Normal, float3 View, float Smoothness, float Step, out float3 Color)
{
#ifndef SHADERGRAPH_PREVIEW
    Surface s = (Surface)0;
    s.normal = SafeNormalize(Normal);
    s.view = SafeNormalize(View);
    s.shininess = exp2(10 * Smoothness + 1);
    s.smoothness = Smoothness;

    float3 vertexSH;
    OUTPUT_SH(Normal, vertexSH);
    // This function calculates the final baked lighting from light maps or probes
    s.bakedGI = SAMPLE_GI(lightmapUV, vertexSH, Normal);
    
#if SHADOWS_SCREEN
    float4 clipPos = TransformWorldToHClip(Position);
    float4 shadowCoord = ComputeScreenPos(clipPos);
#else
    float4 shadowCoord = TransformWorldToShadowCoord(Position);
#endif

    Light light = GetMainLight(shadowCoord);
    MixRealtimeAndBakedGI(light, s.normal, s.bakedGI);
    Color = CalculateGlobalIllumination(s);
    Color += CalculateCelShading(light, s, Step);

    int pixelLightCount = GetAdditionalLightsCount();
    
    #if USE_CLUSTER_LIGHT_LOOP
    InputData inputData = (InputData)0;
    inputData.normalizedScreenSpaceUV = ScreenPosition;
    inputData.positionWS = Position;
    #endif

    #if USE_CLUSTER_LIGHT_LOOP
    UNITY_LOOP for (uint lightIndex = 0; lightIndex < min(URP_FP_DIRECTIONAL_LIGHTS_COUNT, MAX_VISIBLE_LIGHTS); lightIndex++)
    {
        Light additionalLight = GetAdditionalLight(lightIndex, inputData.positionWS, half4(1,1,1,1));
        Color += CalculateCelShading(additionalLight, s, Step);
    }
    #endif
    
    LIGHT_LOOP_BEGIN(pixelLightCount)
        #if !USE_CLUSTER_LIGHT_LOOP
            lightIndex = GetPerObjectLightIndex(lightIndex);
        #endif
        light = GetAdditionalLight(lightIndex, Position, 1);
        Color += saturate(CalculateCelShading(light, s, Step));
    LIGHT_LOOP_END
    
    
#else
    Color = 1;
#endif    
}


void MainLight_float(out float3 Direction, out float3 Color, out float DistanceAtten, out float ShadowAtten, out uint LayerMask)
{
#if SHADERGRAPH_PREVIEW
    Direction = 0.5;
    Color = 1;
    DistanceAtten = 1;
    ShadowAtten = 1;
    LayerMask = 0;
#else
    Light light = GetMainLight();
    Direction = light.direction;
    Color = light.color;
    DistanceAtten = light.distanceAttenuation;
    ShadowAtten = light.shadowAttenuation;
    LayerMask = light.layerMask;
#endif
}

void GlobalIllumination_float(float3 Position, float3 Normal, float3 View, float Smoothness, out float3 Color)
{
#ifndef SHADERGRAPH_PREVIEW
    Surface s = (Surface)0;
    s.normal = SafeNormalize(Normal);
    s.view = SafeNormalize(View);
    s.shininess = exp2(10 * Smoothness + 1);
    s.smoothness = Smoothness;

    float3 vertexSH;
    OUTPUT_SH(Normal, vertexSH);
    // This function calculates the final baked lighting from light maps or probes
    s.bakedGI = SAMPLE_GI(lightmapUV, vertexSH, Normal);
    
    #if SHADOWS_SCREEN
    float4 clipPos = TransformWorldToHClip(Position);
    float4 shadowCoord = ComputeScreenPos(clipPos);
    #else
    float4 shadowCoord = TransformWorldToShadowCoord(Position);
    #endif

    Light light = GetMainLight(shadowCoord);
    MixRealtimeAndBakedGI(light, s.normal, s.bakedGI);
    Color = CalculateGlobalIllumination(s);

#else
    Color = 1;
#endif    
}





void LightingLambertShaded_float(float3 Position, float2 ScreenPosition, float3 Normal, float3 View, float Smoothness, float AmbientOcclusion, out float3 Color)
{
    #ifndef SHADERGRAPH_PREVIEW
    Surface s = (Surface)0;
    s.normal = SafeNormalize(Normal);
    s.view = SafeNormalize(View);
    s.shininess = exp2(10 * Smoothness + 1);
    s.smoothness = Smoothness;

    float3 vertexSH;
    OUTPUT_SH(Normal, vertexSH);
    // This function calculates the final baked lighting from light maps or probes
    s.bakedGI = SAMPLE_GI(lightmapUV, vertexSH, Normal);
    
    #if SHADOWS_SCREEN
    float4 clipPos = TransformWorldToHClip(Position);
    float4 shadowCoord = ComputeScreenPos(clipPos);
    #else
    float4 shadowCoord = TransformWorldToShadowCoord(Position);
    #endif

    Light light = GetMainLight(shadowCoord);
    MixRealtimeAndBakedGI(light, s.normal, s.bakedGI);
    Color = CalculateGlobalIllumination(s);
    Color += CalculateLambertShading(light, s, AmbientOcclusion);

    int pixelLightCount = GetAdditionalLightsCount();
    
    #if USE_CLUSTER_LIGHT_LOOP
    InputData inputData = (InputData)0;
    inputData.normalizedScreenSpaceUV = ScreenPosition;
    inputData.positionWS = Position;
    #endif

    #if USE_CLUSTER_LIGHT_LOOP
    UNITY_LOOP for (uint lightIndex = 0; lightIndex < min(URP_FP_DIRECTIONAL_LIGHTS_COUNT, MAX_VISIBLE_LIGHTS); lightIndex++)
    {
        Light additionalLight = GetAdditionalLight(lightIndex, inputData.positionWS, half4(1,1,1,1));
        Color += CalculateLambertShading(additionalLight, s, AmbientOcclusion);
    }
    #endif
    
    LIGHT_LOOP_BEGIN(pixelLightCount)
        #if !USE_CLUSTER_LIGHT_LOOP
            lightIndex = GetPerObjectLightIndex(lightIndex);
    #endif
    light = GetAdditionalLight(lightIndex, Position, 1);
    Color += saturate(CalculateLambertShading(light, s, AmbientOcclusion));
    LIGHT_LOOP_END
    
    
#else
    Color = 1;
    #endif    
}


void LightingLambert_NoShadow_float(float3 Position, float2 ScreenPosition, float3 Normal, float3 View, float Smoothness, out float3 Color)
{
    #ifndef SHADERGRAPH_PREVIEW
    Surface s = (Surface)0;
    s.normal = SafeNormalize(Normal);
    s.view = SafeNormalize(View);
    s.shininess = exp2(10 * Smoothness + 1);
    s.smoothness = Smoothness;

    float3 vertexSH;
    OUTPUT_SH(Normal, vertexSH);
    // This function calculates the final baked lighting from light maps or probes
    s.bakedGI = SAMPLE_GI(lightmapUV, vertexSH, Normal);
    
    #if SHADOWS_SCREEN
    float4 clipPos = TransformWorldToHClip(Position);
    float4 shadowCoord = ComputeScreenPos(clipPos);
    #else
    float4 shadowCoord = TransformWorldToShadowCoord(Position);
    #endif

    Light light = GetMainLight(shadowCoord);
    light.shadowAttenuation = 1;
    MixRealtimeAndBakedGI(light, s.normal, s.bakedGI);
    Color = CalculateGlobalIllumination(s);
    Color += CalculateLambertShading(light, s, 1);

    int pixelLightCount = GetAdditionalLightsCount();
    
    #if USE_CLUSTER_LIGHT_LOOP
    InputData inputData = (InputData)0;
    inputData.normalizedScreenSpaceUV = ScreenPosition;
    inputData.positionWS = Position;
    #endif

    #if USE_CLUSTER_LIGHT_LOOP
    UNITY_LOOP for (uint lightIndex = 0; lightIndex < min(URP_FP_DIRECTIONAL_LIGHTS_COUNT, MAX_VISIBLE_LIGHTS); lightIndex++)
    {
        Light additionalLight = GetAdditionalLight(lightIndex, inputData.positionWS, half4(1,1,1,1));
        Color += CalculateLambertShading(additionalLight, s, 1);
    }
    #endif
    
    LIGHT_LOOP_BEGIN(pixelLightCount)
        #if !USE_CLUSTER_LIGHT_LOOP
            lightIndex = GetPerObjectLightIndex(lightIndex);
    #endif
    light = GetAdditionalLight(lightIndex, Position, 1);
    Color += saturate(CalculateLambertShading(light, s, 1));
    LIGHT_LOOP_END
    
    
#else
    Color = 1;
    #endif    
}
