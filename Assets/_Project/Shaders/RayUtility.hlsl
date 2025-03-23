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