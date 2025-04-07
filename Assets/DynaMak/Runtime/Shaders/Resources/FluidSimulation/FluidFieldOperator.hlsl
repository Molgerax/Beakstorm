#ifndef __FLUID_OPERATOR_UTILITY__
#define __FLUID_OPERATOR_UTILITY__

#include "../VolumeUtility.hlsl"

#define ThreadBlockSize 2
#define EPSILON 1e-3

// General Input
RWTexture3D<half4> _FluidVolume;
uint3 _FluidResolution;
float3 _FluidCenter;
float3 _FluidBounds;

float _dt;



float4x4 _WorldMatrix;
float3 _WorldPos;
float3 _WorldPosOld;


// Functions

inline float falloffSquare(float input, float radius)
{
    return (1-saturate(input / radius)) * (1-saturate(input / radius));
}

#endif