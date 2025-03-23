#ifndef _INCLUDE_MULTIPLE_POSITION_MAP_
#define _INCLUDE_MULTIPLE_POSITION_MAP_

// --Parameters for Multiple Position Map--

// Tex2D    PositionMapTexture
// uint     PositionMapCount
// float    PositionMapProgress
// float    PositionMapBezierSmooth
// float    PositionMapBezierMinSmooth

// outputs:
// float3   Bezier
// float3   BezierTangent
// float3   Linear

// -------------------------------------

#define POSITION_MAP(name) Texture2D<float4> name##PositionMapTexture;\
uint name##PositionMapCount;\
float name##PositionMapLength;\
StructuredBuffer<float4x4> name##PositionMapBuffer;

#define SAMPLE_POSITION_MAP(name, index) PositionMapSample(name##PositionMapCount, index, name##PositionMapTexture)

#define SAMPLE_POSITION_MAP_FLOAT(name, progress) PositionMapSampleFloat(name##PositionMapCount, (float) progress, name##PositionMapTexture)


#define SAMPLE_POSITION_BUFFER(name, index) PositionBufferSample(name##PositionMapCount, index, name##PositionMapBuffer)

float3 PositionMapSampleFloat(uint positionCount, float progress, Texture2D<float4> positionMap)
{
    uint maxIndex = positionCount - 1;
    uint index = progress * maxIndex;
    uint fraction = frac( progress * index );
    
    uint sampleIndex = clamp(index, 0, maxIndex);
    
    float4 posMapSample = positionMap.Load(int3(sampleIndex, 0, 0));

    return posMapSample.xyz; 
}

float3 PositionMapSample(uint positionCount, uint index, Texture2D<float4> positionMap)
{
    uint maxIndex = positionCount - 1;
    uint sampleIndex = clamp(index, 0, maxIndex);
    
    float4 posMapSample = positionMap.Load(int3(sampleIndex, 0, 0));

    return posMapSample.xyz;
}

float4x4 PositionBufferSample(uint positionCount, uint index, StructuredBuffer<float4x4> buffer)
{
    uint maxIndex = positionCount - 1;
    uint sampleIndex = clamp(index, 0, maxIndex);
    
    float4x4 bufferSample = buffer[sampleIndex];

    return bufferSample;
}


#endif