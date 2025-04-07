#ifndef _INCLUDE_RANDOM_FUNCTION_LIBRARY_
#define _INCLUDE_RANDOM_FUNCTION_LIBRARY_


// Hash Functions
const uint k = 1103515245U; // GLIB C
//const uint k = 134775813U;   // Delphi and Turbo Pascal
//const uint k = 20170906U;    // Today's date (use three days ago's dateif you want a prime)
//const uint k = 1664525U;     // Numerical Recipes

float3 hash(uint3 x)
{
    x = ((x >> 8U) ^ x.yzx) * k;
    x = ((x >> 8U) ^ x.yzx) * k;
    x = ((x >> 8U) ^ x.yzx) * k;
    
    return float3(x) * (1.0 / float(0xffffffffU));
}




// Based on https://github.com/keijiro/NoiseBall3/blob/master/Assets/NoiseBall3.shader
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

// Random number (0-1)
float Random(uint seed)
{
    return float(Hash(seed)) / 4294967295.0; // 2^32-1
}

// Random number from float (0-1)
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