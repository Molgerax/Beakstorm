#ifndef _INCLUDE_BOID_PARAMETERS_
#define _INCLUDE_BOID_PARAMETERS_


uint _NumBoids;

float _Time;
float _DeltaTime;

float3      _WorldPos;
float4x4    _WorldMatrix;

RWStructuredBuffer<float3> _BoidPositionBuffer;
RWStructuredBuffer<float3> _BoidOldPositionBuffer;
RWStructuredBuffer<float3> _BoidVelocityBuffer;
RWStructuredBuffer<float3> _BoidNormalBuffer;
RWStructuredBuffer<float4> _BoidDataBuffer;


float3 _SimulationSpace;
float _HashCellSize;

SamplerState sampler_linear_clamp;

#endif