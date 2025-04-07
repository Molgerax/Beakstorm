#ifndef PARTICLE_UTILITY
#define PARTICLE_UTILITY

#include "../Commons.cginc"
#include "../SDFUtility.hlsl"

// =================================
// ---Macros for Property Parsing---
#define TRANSFORM(name) \
    float3 name##Position; \
    float3 name##PositionOld;\
    float4x4 name##WorldMatrix;

#define TIME_PROPERTY(name) \
    float name;

#define TIME_PASSED(name) (_time - name)


#define TIME_ARRAY(name) \
    float4 name##Times[8]; \
    int name##Count;

#define TIME_PASSED_ARRAY(name, i) (_time - name##Times[i / 4][i % 4])

#define EMISSION_PROPERTIES_START
#define EMISSION_PROPERTIES_END


// ===========================
// ---------Functions---------
float Random(float3 co)
{
    return frac(sin(dot(co.xyz, float3(12.9898, 78.233, 45.5432))) * 43758.5453);
}



// EMISSION
float3 GetRandomBoxPos(float3 boxBounds, float seed)
{
    return lerp(-boxBounds, boxBounds, float3(Random(seed), Random(seed * 2 + 47), Random(seed * 3 + 17)));
}

float3 GetInheritedVelocity(float3 position, float3 previousPosition, float velocityInheritance, float deltaTime, float seed)
{
    return lerp(0, position - previousPosition, velocityInheritance) * Random(seed) * deltaTime;
}

/// <summary>
///  Reduce "clumping" of spawned particles by spreading them
///  between the old and current position of the emitter
/// </summary>
/// <param name="position">Current position of emitter</param>
/// <param name="previousPosition">Previous position of emitter</param>
/// <param name="seed">Seed for randomness</param>
float3 GetInterpolatedLocation(float3 position, float3 previousPosition, float seed)
{
    return lerp(0, previousPosition - position, Random(seed));
}

int GetRandomChainLength(uint minimumChainLength, uint maximumChainLength, float seed)
{
    return (int) floor(lerp(minimumChainLength, maximumChainLength, Random(seed)));
}


// -------- UPDATE --------------

float3 Move_Towards(float3 position, float3 targetPosition, float maxDistance)
{
    float3 diff = targetPosition - position;
    float dist = length(diff);
    if(dist == 0) return position;
    return position + (diff / dist) * min(dist, maxDistance);
}


// COLLISION

void Collision_PlaneY(inout float3 position, inout float3 oldPosition, float yPlane,
    float collisionBounce = 1.0, float particleThickness = 0.0)
{
    float groundColl =  yPlane + particleThickness - position.y;
    oldPosition.y = lerp(oldPosition.y, lerp(yPlane + particleThickness, position.y, collisionBounce), step(0, groundColl));
    position.y = lerp(position.y, yPlane + particleThickness, step(0, groundColl));     
}

void Collision_BoundingBox(inout float3 position, inout float3 oldPosition, float3 center, float3 bounds,
    float collisionBounce = 1.0, float particleThickness = 0.0)
{
    float3 boxPoint = ClosestPointOnBox(center, bounds, position);
    float3 inside = InsideBox(center, bounds, position);
    float3 distToBox = boxPoint - position;
    float3 groundColl = step(particleThickness, distToBox) * inside;
    if(dot( position-boxPoint, position-boxPoint) == 0) return;
    
    float3 boxNormal = normalize(position - boxPoint);
    
    oldPosition = lerp(oldPosition, lerp(boxPoint + boxNormal * particleThickness * 2, position, collisionBounce), groundColl);
    position = lerp(position, boxPoint + boxNormal * particleThickness, groundColl);     
}

void Collision_Points(inout float3 position, inout float3 oldPosition, float3 colliderPosition, float collisionBounce = 0.1, float size = 0)
{   
    float3 offset = colliderPosition - position;
    if(all(offset) == 0) return;

    float collisionLen = length(offset);
    float3 collisionNormal = -offset / collisionLen;
    float collisionDistance = collisionLen - size * 2;
    if(collisionDistance < 0)
    {
        float3 flattenVelocity = dot(position - oldPosition - offset, - collisionNormal) * collisionNormal;
        float3 mostBounceVelocity = dot(position - oldPosition, collisionNormal) * collisionNormal;
           
        oldPosition = lerp(oldPosition - flattenVelocity, oldPosition + mostBounceVelocity, clamp(collisionBounce, 0, 1));
        position -= collisionNormal * collisionDistance;
    }
}

#endif