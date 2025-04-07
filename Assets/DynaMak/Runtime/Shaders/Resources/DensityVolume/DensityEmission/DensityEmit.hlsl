#ifndef __DENSITY_EMIT_UTILITY__
#define __DENSITY_EMIT_UTILITY__

#include "../../VolumeUtility.hlsl"
#include "../../SDFUtility.hlsl"

// General Inputs

RWTexture3D<half> _Volume;

uint3 _VolumeResolution;
float3 _VolumeBounds; 
float3 _VolumeCenter;

float _EmissionStrength;
float _EmissionSpeed;
float _EmissionMaxDensity;

float _dt;

float3 _WorldPos;
float3 _WorldPosOld;


// Functions

inline half AddDensity(half sourceDensity, half destinationDensity, bool useTime = false)
{
    return min(_EmissionMaxDensity, destinationDensity + sourceDensity * lerp(1, _dt * _EmissionSpeed, useTime));
}

inline half SetDensity(half sourceDensity, half destinationDensity, bool useTime = false)
{
    return min(_EmissionMaxDensity,
        max(destinationDensity, lerp(sourceDensity, destinationDensity + sourceDensity * _dt * _EmissionSpeed, useTime)));
}

#endif