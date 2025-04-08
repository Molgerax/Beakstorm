#ifndef _INCLUDE_PHEROMONE_PARAMETERS_
#define _INCLUDE_PHEROMONE_PARAMETERS_


uint _TotalCount;

float _Time;
float _DeltaTime;

float3      _WorldPos;
float4x4    _WorldMatrix;

RWStructuredBuffer<float3> _PositionBuffer;
RWStructuredBuffer<float3> _OldPositionBuffer;
RWStructuredBuffer<float4> _DataBuffer;

RWStructuredBuffer<float3> _PredictedPositionBuffer;

AppendStructuredBuffer<uint> _DeadIndexBuffer;
ConsumeStructuredBuffer<uint> _AliveIndexBuffer;



float3 _SimulationSpace;
float _HashCellSize;

SamplerState sampler_linear_clamp;

#endif