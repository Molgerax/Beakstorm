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


SdfQueryInfo TestAgainstSdf(float3 pos, AbstractSdfData data)
{
    return sdfGeneric(pos, data);
}

float distanceToBoundingBox(float3 pos, float3 bmin, float3 bmax)
{
    float3 c = (bmax + bmin) / 2;
    float3 b = (bmax - bmin) / 2;

    float3 q = abs(pos - c) - b;
    return length(max(q, 0.0)) + min(max(q.x, max(q.y, q.z)), 0);
}



SdfQueryInfo GetClosestDistance(float3 pos, int nodeOffset)
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

                if (sdfInfo.dist < result.dist)
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