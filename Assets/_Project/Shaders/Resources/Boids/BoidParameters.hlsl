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
StructuredBuffer<uint> _PheromoneAliveBuffer;
float _PheromoneHashCellSize;
uint _PheromoneTotalCount;

float3 _SimulationSpace;
float _HashCellSize;

SamplerState sampler_linear_clamp;

#endif