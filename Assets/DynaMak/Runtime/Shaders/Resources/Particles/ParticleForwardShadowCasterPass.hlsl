
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

struct Attributes {
    float3 positionOS : POSITION;
    float3 normalOS : NORMAL;
};

struct Interpolators {
    float4 positionCS : SV_POSITION;
};

// These are set by Unity for the light currently "rendering" this shadow caster pass
float3 _LightDirection;

// This function offsets the clip space position by the depth and normal shadow biases
float4 GetShadowCasterPositionCS(float3 positionWS, float3 normalWS) {
    float3 lightDirectionWS = _LightDirection;
    // From URP's ShadowCasterPass.hlsl
    float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, lightDirectionWS));
    // We have to make sure that the shadow bias didn't push the shadow out of
    // the camera's view area. This is slightly different depending on the graphics API
    #if UNITY_REVERSED_Z
    positionCS.z = min(positionCS.z, UNITY_NEAR_CLIP_VALUE);
    #else
    positionCS.z = max(positionCS.z, UNITY_NEAR_CLIP_VALUE);
    #endif
    positionCS = ApplyShadowClamping(positionCS);
    return positionCS;
}

Interpolators Vertex(uint vertex_id: SV_VertexID, uint instance_id: SV_InstanceID)
{
    Interpolators output;
    
    RenderTri tri = _TriangleBufferShader[instance_id];
    RenderVertex vert = tri.v[2 - vertex_id];
    
    output.positionCS = GetShadowCasterPositionCS(vert.positionWS, vert.normalWS);

    if(tri.subMeshId != _SubMeshId)
        output.positionCS.w = 0.0 / 0.0;

    return output;
}


float4 Fragment(Interpolators input) : SV_TARGET {
    return 0;
}