#ifndef _INCLUDE_BOID_PARAMETERS_
#define _INCLUDE_BOID_PARAMETERS_

#include "../SpatialHashing/SpatialHashGrids.hlsl"

uint _TotalCount;

float _Time;
float _DeltaTime;

float3      _WorldPos;
float4x4    _WorldMatrix;

RWStructuredBuffer<float3> _PositionBuffer;
RWStructuredBuffer<float3> _OldPositionBuffer;
RWStructuredBuffer<float3> _VelocityBuffer;
RWStructuredBuffer<float3> _NormalBuffer;
RWStructuredBuffer<float4> _DataBuffer;

SPATIAL_HASH_BUFFERS(_Boid)

SPATIAL_HASH_BUFFERS_READ(_Pheromone)

StructuredBuffer<float3> _PheromonePositionBuffer;
StructuredBuffer<float4> _PheromoneDataBuffer;
StructuredBuffer<float> _PheromoneAliveBuffer;

StructuredBuffer<uint> _PheromoneDeadCountBuffer;
inline uint DeadParticleCount() { return _PheromoneDeadCountBuffer[3]; }



float _PheromoneHashCellSize;
uint _PheromoneTotalCount;

float3 _SimulationSpace;
float3 _SimulationCenter;

float _HashCellSize;

float4 _WhistleSource;

SamplerState sampler_linear_clamp;


float3 GetBoundsUV(float3 worldPos)
{
    return (worldPos - _SimulationCenter) / _SimulationSpace * 0.5;
}




#endif