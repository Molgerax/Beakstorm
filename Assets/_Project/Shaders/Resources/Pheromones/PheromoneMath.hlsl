#ifndef _INCLUDE_PHEROMONE_MATH_
#define _INCLUDE_PHEROMONE_MATH_

#define PI 3.1415926

float _PressureMultiplier;
float _TargetDensity;

float SmoothingKernelPoly6(float dist, float h)
{
    float factor = 315 / (64 * PI * pow(abs(h), 9));
    float t = max(h * h - dist * dist, 0);
    return t * t * t * factor;
}

float SmoothingKernelPoly6Derivative(float dist, float h)
{
    float factor = 945 / (32 * PI * pow(abs(h), 9));
    float t = max(h * h - dist * dist, 0);
    return -t * t * dist * factor;
}

float SmoothingKernelPow3(float dist, float h)
{
    float factor = 15 / (PI * pow(h, 6));
    float t = max(h - dist, 0);
    return t * t * t * factor;
}

float SmoothingKernelPow3Derivative(float dist, float h)
{
    float factor = 45 / (PI * pow(h, 6));
    float t = max(h - dist, 0);
    return -t * t * factor;
}


float DensityToPressure(float density)
{
    float error = (density - _TargetDensity);
    error = max(error, 0);
    float pressure = error * _PressureMultiplier;
    return pressure;
}

float GetDensityFromParticle(float3 p, float3 particlePos, float smoothingRadius)
{
    float3 diff = (p - particlePos);
    float dist = length(diff);
    return SmoothingKernelPow3(dist, smoothingRadius);
}

float3 GetDensityDerivativeFromParticle(float3 p, float3 particlePos, float smoothingRadius)
{
    float3 diff = p - particlePos;
    float distSquared = dot(diff, diff);

    float dist = sqrt(distSquared);
    float3 direction = float3(0, 1, 0);
    
    if (distSquared != 0)
        direction = diff / dist;
    
    float slope = SmoothingKernelPow3Derivative(dist, smoothingRadius);
    float density = SmoothingKernelPow3(dist, smoothingRadius);

    if (density == 0)
        return 0;
    
    return -DensityToPressure(density) * slope * direction / density;
}



#endif