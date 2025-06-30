#ifndef _INCLUDE_PHEROMONE_PARAMETERS_
#define _INCLUDE_PHEROMONE_PARAMETERS_

#include "../SpatialGrid/SpatialGridSampling.hlsl"

struct Pheromone
{
    float3 pos;
    float life;
    float3 oldPos;
    float maxLife;
    float4 data;
};


float _LifeTime;

float _Time;
float _DeltaTime;

float _SmoothingRadius;

float3 _WorldPos;
float4x4 _WorldMatrix;

float3 _SpawnPos;
float3 _SpawnPosOld;

RWStructuredBuffer<float3> _PositionBuffer;
RWStructuredBuffer<float3> _OldPositionBuffer;
RWStructuredBuffer<float4> _DataBuffer;
RWStructuredBuffer<float> _AliveBuffer;

RWStructuredBuffer<float3> _PredictedPositionBuffer;

AppendStructuredBuffer<uint> _DeadIndexBuffer;
ConsumeStructuredBuffer<uint> _AliveIndexBuffer;

RWStructuredBuffer<uint> _DeadCountBuffer;
uint _TargetEmitCount;
uint _ParticlesPerEmit;


RWStructuredBuffer<Pheromone> _PheromoneBuffer;
StructuredBuffer<Pheromone> _PheromoneBufferRead;

struct SortEntry
{
    uint index;
    float dist;
};
RWStructuredBuffer<SortEntry> _PheromoneSortingBuffer;

RWStructuredBuffer<uint> _InstancedArgsBuffer;

StructuredBuffer<uint> _PheromoneSpatialOffsets;

SamplerState sampler_linear_clamp;


// GETTER FUNCTION

/// <summary>
/// Number of particles in the dead index buffer.
/// </summary>
inline uint DeadParticleCount() { return _DeadCountBuffer[3]; }

/// <summary>
/// Number of calls to emit kernel this frame.
/// </summary>
inline uint CurrentEmissionCount() { return _DeadCountBuffer[0]; }

bool IsAlive(uint index)
{
    return _AliveBuffer[index] > 0;
    
    uint id = index / 32;
    uint bit = index % 32;
    uint read = _AliveBuffer[id];
    return ((read >> bit) & 1) == 1;
}

bool IsAlive(Pheromone p)
{
    return p.life > 0;
}

// SETTER FUNCTIONS

void SetAlive(uint index)
{
    _AliveBuffer[index] = 1;
    return;
    
    uint id = index / 32;
    uint bit = index % 32;
    uint value = 1u << bit;
    InterlockedOr(_AliveBuffer[id], value);
}

void SetDead(uint index)
{
    _AliveBuffer[index] = 0;
    return;
    
    uint id = index / 32;
    uint bit = index % 32;
    uint value = 1u << bit;
    InterlockedAnd(_AliveBuffer[id], ~value);
}

float DepleteLife(uint index, float time)
{
    float life = _AliveBuffer[index];
    if (life <= 0)
        return life;

    life -= time;
    if (life <= 0)
    {
        life = 0;
        _DeadIndexBuffer.Append(index);
    }
    _AliveBuffer[index] = life;
    return life;
}

void KillParticle(uint index)
{
    if (!IsAlive(index))
        return;
    
    _DeadIndexBuffer.Append(index);
    SetDead(index);
}


float3 GetBoundsUV(float3 worldPos)
{
    return (worldPos - _SimulationCenter) / _SimulationSize * 0.5;
}


#endif