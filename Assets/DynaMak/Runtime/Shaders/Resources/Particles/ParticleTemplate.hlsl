#ifndef PARTICLE_TEMPLATE
#define PARTICLE_TEMPLATE

#define ThreadBlockSize 256
#define EPSILON 1e-3
#define PI 3.1415926535

// DEFAULT PARAMETERS
uniform float _time;
uniform float _DeltaTime;
uniform float3 _WorldPosition;
uniform float3 _WorldPositionOld;
uniform float4x4 _WorldMatrix;
uniform float4x4 _WorldMatrixInverse;


#ifdef PARTICLE_STRUCT
AppendStructuredBuffer<uint> _DeadIndexBuffer;
ConsumeStructuredBuffer<uint> _AliveIndexBuffer;
RWStructuredBuffer<uint> _DeadCountBuffer;
uint _TargetEmitCount;
uint _ParticlesPerEmit;
uint _MaxParticleCount;

RWStructuredBuffer<PARTICLE_STRUCT> _ParticleBuffer;

#ifdef PARTICLE_PONG_BUFFER
RWStructuredBuffer<Particle> _PongBuffer;
#endif



// SETTER FUNCTION
void SetParticle(uint index, PARTICLE_STRUCT particle)
{
#ifdef PARTICLE_PONG_BUFFER
    _PongBuffer[index] = particle;
#else
    _ParticleBuffer[index] = particle;
#endif
}

// GETTER FUNCTION

/// <summary>
/// Number of particles in the dead index buffer.
/// </summary>
inline uint DeadParticleCount() { return _DeadCountBuffer[3]; }

/// <summary>
/// Number of calls to emit kernel this frame.
/// </summary>
inline uint CurrentEmissionCount() { return _DeadCountBuffer[0]; }


// KERNEL FUNCTION DEFINE TEMPLATES

#define KERNEL_FUNCTION(function) \
    [numthreads(ThreadBlockSize, 1, 1)] \
    void function##Kernel (uint3 id : SV_DispatchThreadID)


#define INITIALIZE \
    [numthreads(ThreadBlockSize, 1, 1)] \
    void InitializeKernel (uint3 id : SV_DispatchThreadID) \
    { \
        const uint index = id.x; \
        _ParticleBuffer[index].alive = false; \
        _DeadIndexBuffer.Append( index ); \
    }

#define INIT \
    [numthreads(ThreadBlockSize, 1, 1)] \
    void InitializeKernel (uint3 id : SV_DispatchThreadID) 

#define KILL \
    [numthreads(ThreadBlockSize, 1, 1)] \
    void KillKernel (uint3 id : SV_DispatchThreadID) \
    { \
        const uint index = id.x; \
        if(_ParticleBuffer[index].alive) _DeadIndexBuffer.Append(index); \
        _ParticleBuffer[index] = (PARTICLE_STRUCT)0; \
    }

#define UPDATE \
    [numthreads(ThreadBlockSize, 1, 1)] \
    void UpdateKernel (uint3 id : SV_DispatchThreadID) 


#define RENDER \
    [numthreads(ThreadBlockSize, 1, 1)] \
    void RenderKernel (uint3 id : SV_DispatchThreadID) 


#define EMIT \
    [numthreads(1, 1, 1)] \
    void EmitKernel (uint3 id : SV_DispatchThreadID, uint3 groupId : SV_GroupID)


#define SET_EMISSION_TO_DEADCOUNT \
    [numthreads(1, 1, 1)] \
    void EmissionCountKernel (uint3 id : SV_DispatchThreadID) \
    { \
        uint totalDead = _DeadCountBuffer[0]; \
        uint finalEmit = min(_TargetEmitCount, floor(totalDead / _ParticlesPerEmit)); \
        _DeadCountBuffer[0] = finalEmit;\
        _DeadCountBuffer[3] = totalDead;\
    }


#endif
#endif