#ifndef _INCLUDE_BOID_STATEMACHINE_
#define _INCLUDE_BOID_STATEMACHINE_

#include "BoidParameters.hlsl"

#define STATE_SETTINGS(name) float4 name##StateWeight;\
    float4 name##StateRadius;\
    float4 name##StateSpeed;

// x : separation \n y : alignment \n z : cohesion \n w : detection
// x : minSpeed \n y : maxSpeed \n z : maxForce \n w : empty

/// x : separation \n y : alignment \n z : cohesion \n w : detection
STATE_SETTINGS(_Neutral)


/// x : separation \n y : alignment \n z : cohesion \n w : detection
STATE_SETTINGS(_Exposed)

float GetInterpolator(uint index)
{
#ifdef BOID_BUFFER
    return _BoidBufferRead[index].exposure;
#else
    float3 pos = _PositionBuffer[index];
    float4 data = _DataBuffer[index];

    return saturate(data.w);
    return 1;
    
    float t = (pos.x / _SimulationSpace.x);
    return step(0, t);
    return saturate(t + 0.5);
#endif
}

float4 GetBoidSettings(uint index)
{
    float t = GetInterpolator(index);
    return lerp(_NeutralStateWeight, _ExposedStateWeight, t);
}

float4 GetBoidRadius(uint index)
{
    float t = GetInterpolator(index);
    return lerp(_NeutralStateRadius, _ExposedStateRadius, t);
}

float GetMinSpeed(uint index)
{
    float t = GetInterpolator(index);
    return lerp(_NeutralStateSpeed.x, _ExposedStateSpeed.x, t);
}

float GetMaxSpeed(uint index)
{
    float t = GetInterpolator(index);
    return lerp(_NeutralStateSpeed.y, _ExposedStateSpeed.y, t);
}

float GetMaxForce(uint index)
{
    float t = GetInterpolator(index);
    return lerp(_NeutralStateSpeed.z, _ExposedStateSpeed.z, t);
}

#endif