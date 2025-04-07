#ifndef COMMONS
#define COMMONS


// MATH

float max(float a, float b, float c)
{
    return max(a, max(b, c));
}

float max(float a, float b, float c, float d)
{
    return max(max(a,b), max(c, d));
}

float max(float3 v)
{
    return max(v.x, v.y, v.z);
}

float max(float4 v)
{
    return max(v.x, v.y, v.z, v.w);
}




// REMAPPING VALUES

inline half withinRange(half a, half b, half x)
{
    return abs(step(a,x)-step(b,x));
}

inline half linearStep(half a, half b, half x)
{
    return saturate((x - a)/(b - a));
}

half quadraticStep(half a, half b, half x)
{
    half t = linearStep(a,b,x);
    return t * t;
}

half quarticStep(half a, half b, half x)
{
    half t = linearStep(a,b,x);
    return t * t * t * t;
}


#define STEP_MIRROR(a, b, t, func)\
    func(a,a+(abs(a-b)*0.5), t) * withinRange(a,a+(abs(a-b)*0.5),t) + func(b,b-(abs(a-b)*0.5), t) * withinRange(b,b-(abs(a-b)*0.5),t);

half smoothstepMirrored(half a, half b, half t)
{
    half interval = abs(a-b) * 0.5; 
    return smoothstep(a,a+interval, t) * withinRange(a,a+interval,t) + smoothstep(b,b-interval, t) * withinRange(b,b-interval,t);
}


inline half lerp2(half a, half b, half c, half x)
{
    return lerp(lerp(a, b, x), lerp(b, c, x - 1), step(1, x));
}

inline half lerp3(half a, half b, half c, half d, half x)
{
    return lerp(lerp(lerp(a, b, x), lerp(b, c, x - 1), step(1, x)),
                lerp(c, d, x-2), step(2,x));
}

inline half lerp4(half a, half b, half c, half d, half e, half x)
{
    return lerp(lerp(lerp(a, b, x),     lerp(b, c, x - 1), step(1, x)),
                lerp(lerp(c, d, x - 2), lerp(d, e, x - 3), step(3, x)), step(2,x));
}




// RANDOMIZATION

// Hash function from H. Schechter & R. Bridson, goo.gl/RXiKaH
uint Hash(uint s)
{
    s ^= 2747636419u;
    s *= 2654435769u;
    s ^= s >> 16;
    s *= 2654435769u;
    s ^= s >> 16;
    s *= 2654435769u;
    return s;
}

float Random(uint seed)
{
    return float(Hash(seed)) / 4294967295.0; // 2^32-1
}

float Random(float seed)
{
    return float(Hash(asuint(seed))) / 4294967295.0; // 2^32-1
}

// Uniformaly distributed points on a unit sphere
// http://mathworld.wolfram.com/SpherePointPicking.html
float3 RandomUnitVector(uint seed)
{
    float PI2 = 6.28318530718;
    float z = 1 - 2 * Random(seed);
    float xy = sqrt(1.0 - z * z);
    float sn, cs;
    sincos(PI2 * Random(seed + 1), sn, cs);
    return float3(sn * xy, cs * xy, z);
}

float3 RandomUnitVector(float seed)
{
    float PI2 = 6.28318530718;
    float z = 1 - 2 * Random(seed);
    float xy = sqrt(1.0 - z * z);
    float sn, cs;
    sincos(PI2 * Random(seed + 1), sn, cs);
    return float3(sn * xy, cs * xy, z);
}

// Uniformaly distributed points inside a unit sphere
float3 RandomVector(uint seed)
{
    return RandomUnitVector(seed) * sqrt(Random(seed + 2));
}

float3 RandomVector(float seed)
{
    return RandomUnitVector(seed) * sqrt(Random(seed + 2));
}

// Uniformaly distributed points inside a unit cube
float3 RandomVector01(uint seed)
{
    return float3(Random(seed), Random(seed + 1), Random(seed + 2));
}

float3 RandomVector01(float seed)
{
    return float3(Random(seed), Random(seed + 1), Random(seed + 2));
}


#endif