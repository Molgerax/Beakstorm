#ifndef _INCLUDE_SDF_COLLISIONS_
#define _INCLUDE_SDF_COLLISIONS_

#include "SdfUtility.hlsl"

#ifndef RAY_UTILITY
#define RAY_UTILITY

float2 rayBoxDist(float3 boundsMin, float3 boundsMax, float3 rayOrigin, float3 rayDir)
{
    float3 t0 = (boundsMin - rayOrigin) / rayDir;
    float3 t1 = (boundsMax - rayOrigin) / rayDir;
    float3 tmin = min(t0, t1);
    float3 tmax = max(t0, t1);

    float dstA = max(max(tmin.x, tmin.y), tmin.z);
    float dstB = min(tmax.x, min(tmax.y, tmax.z));


    float dstToBox = max(0, dstA);
    float dstInsideBox = max(0, dstB - dstToBox);
    
    return float2(dstToBox, dstInsideBox);
}

#endif

struct BVHNode
{
    float3 boundsMin;
    float3 boundsMax;
    // index refers to items if it is leaf node (itemCount > 0)
    // otherwise it is the index of the first child node
    int startIndex;
    int itemCount;
};


StructuredBuffer<BVHNode> _NodeBuffer;
StructuredBuffer<AbstractSdfData> _SdfBuffer;
int _NodeCount;

Texture3D<float> _SdfAtlasTexture;
uint3 _SdfAtlasResolution;

float3 clampSamplePos(float3 pos, float3 startVoxel, float3 resolution, float3 derivative)
{
    return clamp(pos, startVoxel + 0.5, startVoxel + resolution - 0.5 - derivative);
}

SamplerState sampler_pointClamp;
SamplerState sampler_linearClamp;
SdfQueryInfo sdfTextureAtlasLookUp(float3 p, AbstractSdfData data)
{
    SdfQueryInfo result = (SdfQueryInfo)0;
    result.normal.y = 1;
    result.matIndex = (data.Type >> 4) & 0x0F;

    float3 uvw = saturate(((p - data.Translate) / data.XAxis) + 0.5);

    float boxDist = sdfBox(data.Translate, data.XAxis * 0.5, p);
    float3 boxNormal = sdfBoxNormal(data.Translate, data.XAxis * 0.5, p);
    if (dot(boxNormal, boxNormal) > 0)
        boxNormal = normalize(boxNormal);
    
    float3 resolution = floor(data.YAxis);
    float3 startVoxel = floor(data.Data);

    float3 derivative = 0.25;
    //derivative = 0.01;
    
    float3 pixelPos = startVoxel + uvw * (resolution - 1) + 0.5;
    pixelPos = clampSamplePos(pixelPos - 0.5, startVoxel, resolution, derivative);

    float3 samplePos = pixelPos / (_SdfAtlasResolution - 1);
    
    float4 offset = 0;
    offset.xyz = derivative / (_SdfAtlasResolution - 1);
    
    float dist  = _SdfAtlasTexture.SampleLevel(sampler_linearClamp, samplePos, 0);
    float distX = _SdfAtlasTexture.SampleLevel(sampler_linearClamp, samplePos + offset.xww, 0);
    float distY = _SdfAtlasTexture.SampleLevel(sampler_linearClamp, samplePos + offset.wyw, 0);
    float distZ = _SdfAtlasTexture.SampleLevel(sampler_linearClamp, samplePos + offset.wwz, 0);

    result.normal = (float3(distX - dist, distY - dist, distZ - dist));
    if (dot(result.normal, result.normal > 0))
        result.normal = normalize(result.normal);

    result.dist = dist;

    float3 addedDistance = result.dist * result.normal + max(0, boxDist) * boxNormal;
    //addedDistance = result.dist * boxNormal + max(0, boxDist) * boxNormal;
    
    //if (any(step(uvw, 0) + step(1, uvw)))// || any(step(1, uvw)))
    if (boxDist > 0)
    {
        result.dist = length(addedDistance);
        result.normal = normalize(addedDistance);
        //result.dist = 0;
    }
    
    return result;
}



SdfQueryInfo TestAgainstSdf(float3 pos, AbstractSdfData data)
{
    [branch]
    if ((data.Type & 0x0F) == SDF_TEXTURE)
        return sdfTextureAtlasLookUp(pos, data);
    else
        return sdfGeneric(pos, data);
}

float distanceToBoundingBox(float3 pos, float3 bmin, float3 bmax)
{
    float3 c = (bmax + bmin) / 2;
    float3 b = (bmax - bmin) / 2;

    float3 q = abs(pos - c) - b;
    return length(max(q, 0.0)) + min(max(q.x, max(q.y, q.z)), 0);
}



SdfQueryInfo GetClosestDistance(float3 pos, int nodeOffset, uint ignoreMask = 0)
{
    SdfQueryInfo result = (SdfQueryInfo)0;
    result.dist = 1.#INF;
    
    int stack[32];
    uint stackIndex = 0;
    stack[stackIndex++] = nodeOffset + 0;

    int limiter = 0;
    
    while (stackIndex > 0 && limiter < 256)
    {
        stackIndex = min(31, stackIndex);
        limiter++;
        
        BVHNode node = _NodeBuffer[stack[--stackIndex]];
        bool isLeaf = node.itemCount > 0;

        if (isLeaf)
        {
            for (int i = 0; i < node.itemCount; i++)
            {
                AbstractSdfData data = _SdfBuffer[node.startIndex + i];
                SdfQueryInfo sdfInfo = TestAgainstSdf(pos, data);

                if (sdfInfo.dist < result.dist && (((1 << sdfInfo.matIndex) & ignoreMask) == 0))
                {
                    result = sdfInfo;
                }
            }
        }
        else
        {
            int childIndexA = nodeOffset + node.startIndex + 0;
            int childIndexB = nodeOffset + node.startIndex + 1;
            BVHNode childA = _NodeBuffer[childIndexA];
            BVHNode childB = _NodeBuffer[childIndexB];

            float dstA = distanceToBoundingBox(pos, childA.boundsMin, childA.boundsMax);
            float dstB = distanceToBoundingBox(pos, childB.boundsMin, childB.boundsMax);
						
            // We want to look at closest child node first, so push it last
            bool isNearestA = dstA <= dstB;
            float dstNear = isNearestA ? dstA : dstB;
            float dstFar = isNearestA ? dstB : dstA;
            int childIndexNear = isNearestA ? childIndexA : childIndexB;
            int childIndexFar = isNearestA ? childIndexB : childIndexA;

            if (dstFar < result.dist) stack[stackIndex++] = childIndexFar;
            if (dstNear < result.dist) stack[stackIndex++] = childIndexNear;
        }
    }
    
    return result;
}

float2 GetMinimumRayDistance(float3 rayPos, float3 rayDir, int nodeOffset, inout int nodeIndex)
{
    float2 result = 1.#INF;
    
    int stack[32];
    uint stackIndex = 0;
    stack[stackIndex++] = nodeOffset + 0;

    int limiter = 0;
    nodeIndex = 0;
    float closest = result.x;
    
    while (stackIndex > 0 && limiter < 256)
    {
        stackIndex = min(31, stackIndex);
        limiter++;

        int nodeId = stack[--stackIndex];
        BVHNode node = _NodeBuffer[nodeId];
        bool isLeaf = node.itemCount > 0;

        if (isLeaf)
        {
            float2 rayDist = rayBoxDist(node.boundsMin, node.boundsMax, rayPos, rayDir);

            if (rayDist.x < result.x)
                result.x = rayDist.x;

            if (rayDist.y > result.y)
                result.y = rayDist.y;
            
            for (int i = 0; i < node.itemCount; i++)
            {
                AbstractSdfData data = _SdfBuffer[node.startIndex + i];
                SdfQueryInfo sdfInfo = TestAgainstSdf(rayPos + rayDir * (rayDist.x + rayDir.y), data);

                if (sdfInfo.dist < closest)
                {
                    closest = sdfInfo.dist;
                    //result = rayDist;
                    if (sdfInfo.dist < 0)
                        nodeIndex = nodeId;
                }
            }
        }
        else
        {
            int childIndexA = nodeOffset + node.startIndex + 0;
            int childIndexB = nodeOffset + node.startIndex + 1;
            BVHNode childA = _NodeBuffer[childIndexA];
            BVHNode childB = _NodeBuffer[childIndexB];

            float dstA = rayBoxDist(childA.boundsMin, childA.boundsMax, rayPos, rayDir).x;
            float dstB = rayBoxDist(childB.boundsMin, childB.boundsMax, rayPos, rayDir).x;
						
            // We want to look at closest child node first, so push it last
            bool isNearestA = dstA <= dstB;
            float dstNear = isNearestA ? dstA : dstB;
            float dstFar = isNearestA ? dstB : dstA;
            int childIndexNear = isNearestA ? childIndexA : childIndexB;
            int childIndexFar = isNearestA ? childIndexB : childIndexA;

            if (dstFar < result.x) stack[stackIndex++] = childIndexFar;
            if (dstNear < result.x) stack[stackIndex++] = childIndexNear;
        }
    }
    
    return result;
}


#endif