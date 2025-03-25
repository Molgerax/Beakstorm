#ifndef _INCLUDE_TENTACLE_UITLITY_
#define _INCLUDE_TENTACLE_UTILITY_

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

float rand2(float2 n)
{
    return frac(sin(dot(n, float2(12.9898, 4.1414))) * 43758.5453);
}

float Noise(float seed, float t)
{
    return rand2(float2(seed, t));
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