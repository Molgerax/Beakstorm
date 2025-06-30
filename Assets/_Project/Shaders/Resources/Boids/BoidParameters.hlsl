#ifndef _INCLUDE_BOID_PARAMETERS_
#define _INCLUDE_BOID_PARAMETERS_

#include "../SpatialHashing/SpatialHashGridsCellOrdering.hlsl"

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

uint3 _Dimensions;
RWStructuredBuffer<uint> _BoidSpatialIndices;
RWStructuredBuffer<uint> _BoidSpatialOffsets;



struct Pheromone
{
    float3 pos;
    float life;
    float3 oldPos;
    float maxLife;
    float4 data;
};

StructuredBuffer<Pheromone> _PheromoneBuffer;
StructuredBuffer<uint> _PheromoneArgs;
StructuredBuffer<uint> _PheromoneSpatialOffsets;

float _PheromoneSmoothingRadius;

float3 _PheromoneCenter;
float3 _PheromoneSize;
uint3 _PheromoneCellDimensions;

StructuredBuffer<float3> _PheromonePositionBuffer;
StructuredBuffer<float4> _PheromoneDataBuffer;
StructuredBuffer<float> _PheromoneAliveBuffer;

StructuredBuffer<uint> _PheromoneDeadCountBuffer;
inline uint DeadParticleCount() { return _PheromoneDeadCountBuffer[3]; }



float _PheromoneHashCellSize;
uint _PheromoneTotalCount;


float4 _WhistleSource;

SamplerState sampler_linear_clamp;


float3 GetBoundsUV(float3 worldPos)
{
    return (worldPos - _SimulationCenter) / _SimulationSize * 0.5;
}




#endif