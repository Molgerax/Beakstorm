#ifndef MESH_UTILITY
#define MESH_UTILITY


// Macros for Property Lookup

#define FRACTURED_MESH(name) ByteAddressBuffer name##VertexBuffer;\
    uint name##VertexStride;\
    uint name##VertexCount;\
    ByteAddressBuffer name##IndexBuffer;\
    Texture2D<float4> name##IndexTex;\
    Texture2D<float4> name##PivotTex;\
    float3 name##BoundsCenter;\
    float3 name##BoundsSize;

#define MESH(name) ByteAddressBuffer name##VertexBuffer;\
    uint name##VertexStride;\
    uint name##VertexCount;\
    ByteAddressBuffer name##IndexBuffer;

#define SKINNED_MESH(name) ByteAddressBuffer name##VertexBuffer;\
    uint name##VertexStride;\
    uint name##VertexCount;\
    ByteAddressBuffer name##TexCoordBuffer;\
    uint name##TexCoordStride;\
    ByteAddressBuffer name##IndexBuffer;\
    uint name##IndexCount; \
    uint name##IndexStride; \
    uint4 name##SubMeshStart[8]; \
    int name##SubMeshCount; \
    int name##PositionOffset; \
    int name##NormalOffset; \
    int name##TangentOffset; \
    int name##TexCoordOffset; \
    int name##ColorOffset; \
    float4x4 name##WorldMatrix; \
    float4x4 name##WorldMatrixInverse; \
    float4x4 name##BoneMatrices[128];\
    float4x4 name##BoneMatricesInverse[128];\
    int name##BoneCount;

// Accessing Mesh Data

float4 GetVertexData(ByteAddressBuffer buffer, uint vertexIndex, uint vertexStreamStride, uint vertexStreamOffset)
{
    uint strided = vertexIndex * vertexStreamStride + vertexStreamOffset;
    uint4 data = buffer.Load4(strided);
    return asfloat(data);
}

uint GetByteAddressBuffer(ByteAddressBuffer buffer, uint index, uint stride, uint offset)
{
    uint strided = index * stride + offset;
    return  buffer.Load(strided);
}

uint GetSubMeshIndex(uint vertexIndex, uint4 subMeshStartIndices[8], int subMeshCount)
{
    uint index = 0;

    for (int i = 0; i < 8 && i < subMeshCount; i++)
    {   
        if( vertexIndex >= asuint( subMeshStartIndices[i].x))
            index = i;
        else return index;
    }
    return index;
}

#define GET_SUBMESH_INDEX(subMeshIndex, vertexIndex, subMeshStartIndices) \
    uint subMeshIndex = 0;\
    for(int subMeshIndex##iterator = 0; subMeshIndex##iterator < 8; subMeshIndex##iterator++) \
    { \
        if(vertexIndex >= (uint) subMeshStartIndices[subMeshIndex##iterator].x)\
            subMeshIndex = subMeshIndex##iterator;\
        else break; \
    } \

SamplerState sampler_linear_repeat;

#define GET_VERTEX_DATA(name, index, position, normal, tangent) float3 position = GetVertexData(name##VertexBuffer, index, name##VertexStride, 0).xyz;\
    float3 normal = GetVertexData(name##VertexBuffer, index, name##VertexStride, 12).xyz;\
    float4 tangent = GetVertexData(name##VertexBuffer, index, name##VertexStride, 24).xyzw;\

#define GET_SKINNED_VERTEX_DATA(name, index, position, normal, tangent) \
    float3 position = mul( name##WorldMatrix, float4(GetVertexData(name##VertexBuffer, index, name##VertexStride, name##PositionOffset).xyz, 1)).xyz;\
    float3 normal = GetVertexData(name##VertexBuffer, index, name##VertexStride, name##NormalOffset).xyz;\
    float4 tangent = GetVertexData(name##VertexBuffer, index, name##VertexStride, name##TangentOffset).xyzw;\

#define MESH_SAMPLE_TEXTURE(meshName, textureName, vertexIndex, color) \
    float4 color = textureName.SampleLevel(sampler_linear_repeat,\
        GetVertexData(meshName##TexCoordBuffer, vertexIndex, meshName##TexCoordStride, meshName##TexCoordOffset).xy, 0);


float3 Get_Triangle_Center(StructuredBuffer<float3> vertexBuffer, uint triangleIndex)
{
    float3 v0 = vertexBuffer[triangleIndex * 3 + 0];
    float3 v1 = vertexBuffer[triangleIndex * 3 + 1];
    float3 v2 = vertexBuffer[triangleIndex * 3 + 2];
    return (v0 + v1 + v2) / 3.0;
}

// Helper Functions

float3 Get_Triangle_Normal(float3 v0, float3 v1, float3 v2)
{
    float3 dir = cross(v2 - v0, v1 - v0);
    return normalize(dir);
}

#endif