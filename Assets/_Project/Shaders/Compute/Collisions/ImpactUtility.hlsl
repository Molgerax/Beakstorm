#ifndef _INCLUDE_IMPACT_UTILITY_
#define _INCLUDE_IMPACT_UTILITY_

#include "SdfCollisions.hlsl"

struct Impact
{
    float3 position;
    float time;
    float3 normal;
    uint data;
};

RWStructuredBuffer<Impact> _ImpactBufferWrite;
StructuredBuffer<Impact> _ImpactBufferRead;
StructuredBuffer<uint> _ImpactArgsBuffer;
uint _ImpactCount;

uint _MaxFrames;

uint CalculateDamage(float3 velocity)
{
    float sqrSpeed = dot(velocity, velocity);
    int damage = floor((sqrt(sqrSpeed) - 10) / 10);
    return clamp(damage, 0, 10);
}

uint GetMaterialIndex(Impact impact)
{
    return (impact.data >> 8) & 0xF;
}

uint GetDamage(Impact impact)
{
    return (impact.data) & 0xFF;
}

float GetMaxTime(Impact impact)
{
    return ((impact.data >> 16) & 0xFF) / 64.0;
}

Impact CreateImpact(float3 pos, float3 normal, uint matIndex, uint damage, float time)
{
    Impact impact = (Impact)0;
    impact.position = pos;
    impact.normal = normal;
    impact.time = time;

    uint maxTime = (uint)(time * 64) & 0xFF;
    
    impact.data = (maxTime << 16) | (matIndex << 8) | (damage & 0xFF); 
    
    return impact;
}

Impact CreateImpact(float3 pos, float3 normal, uint matIndex, uint damage)
{
    return CreateImpact(pos, normal, matIndex, damage, _MaxFrames * 0.05);
}

void AddImpact(Impact impact)
{
    if (GetDamage(impact) <= 0)
        return;
    if (GetMaterialIndex(impact)==0)
        return;
    
    uint index = _ImpactBufferWrite.IncrementCounter();

    if (index >= _ImpactCount) return;

    index %= _ImpactCount;
    _ImpactBufferWrite[index] = impact;
}


void AddImpact(float3 pos, float3 normal, uint damage)
{
    Impact impact = CreateImpact(pos, normal, 1, damage);
    AddImpact(impact);
}

void AddImpact(float3 pos, SdfQueryInfo info, uint damage, float time)
{
    if (info.dist >= 0)
        return;
    
    Impact impact = CreateImpact(pos - info.normal * info.dist, info.normal, info.matIndex, damage, time);
    
    AddImpact(impact);
}


#endif