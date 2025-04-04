#ifndef _INCLUDE_SDF_COLLISIONS_
#define _INCLUDE_SDF_COLLISIONS_

#ifndef SDF_DATA
#define SDF_DATA float4
#endif

#ifdef SDF_DATA

struct BVHNode
{
    float3 boundsMin;
    float3 boundsMax;
    // index refers to triangles if is leaf node (triangleCount > 0)
    // otherwise it is the index of the first child node
    int startIndex;
    int itemCount;
};

struct SdfQueryInfo
{
    float dist;
    float3 normal;
};

StructuredBuffer<BVHNode> _NodeBuffer;
StructuredBuffer<SDF_DATA> _SdfBuffer;
int _NodeCount;


SdfQueryInfo TestAgainstSdf(float3 pos, SDF_DATA data)
{
    SdfQueryInfo result;
    float3 p = data.xyz;
    result.dist = length(pos - p) - data.w;
    result.normal = normalize(pos - p);
    
    return result;
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
                SDF_DATA data = _SdfBuffer[node.startIndex + i];
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

#undef SDF_DATA

#endif
#endif