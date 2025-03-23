#ifndef _INCLUDE_TENTACLE_UITLITY_
#define _INCLUDE_TENTACLE_UTILITY_

#include "Packages/com.kingart.dow.shaders/Public/HLSL/RandomFunctions.hlsl"

float3 GetBezierPosition(float3 p0, float3 p1, float3 p2, float3 p3, float t)
{
    float t_ = 1 - t;
    return
        (t_ * t_ * t_)      * p0 +
        (3 * t_ * t_ * t)   * p1 +
        (3 * t_ * t * t)    * p2 +
        (t * t * t)         * p3;
}

float3 GetBezierDerivative(float3 p0, float3 p1, float3 p2, float3 p3, float t)
{
    float t_ = 1 - t;
    return
        (3 * t_ * t_)   * (p1 - p0) +
        (6 * t_ * t)    * (p2 - p1) +
        (3 * t * t)     * (p3 - p2);
}


float Noise(float seed, float t)
{
    return GeneratePerlinNoise2D(float2(seed, t)).x;
}

float3 RandomOffset(float2 noiseSeed, float freq, float amp, float t)
{
    float envelope = 1 - (1 - 2 * t) * (1 - 2 * t);

    float3 offset = 0;
    offset.y = amp * envelope * Noise( noiseSeed.x, freq * t);
    offset.x = amp * envelope * Noise( noiseSeed.y, freq * t);
    return offset;
}

#endif