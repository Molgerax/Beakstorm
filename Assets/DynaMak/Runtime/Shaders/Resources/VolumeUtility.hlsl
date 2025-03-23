#ifndef VOLUME_UTILITY
#define VOLUME_UTILITY

struct FluidSimCell
{
    float3 Velocity;
    float Density;
};

SamplerState sampler_linear_clamp;
SamplerState sampler_point_clamp;


// PROPERTY MACROS
#define VOLUME(textureType, name) \
    textureType name##Volume; \
    float3 name##Bounds; \
    float3 name##Center;\
    uint3 name##Resolution;


// ------ CONVERT IDs

/// <summary>
/// Converts a 3D voxel id to a 1D array index.
/// </summary>
/// <param name="id">3D id in the voxel volume</param>
/// <param name="resolution">Dimensions of the voxel volume</param>
uint id3Dto1D(uint3 id, uint3 resolution)
{
    return clamp(id.x, 0, resolution.x - 1) + clamp(id.y, 0, resolution.y - 1) * resolution.x + clamp(id.z, 0, resolution.z - 1) * resolution.x * resolution.y;
}

/// <summary>
/// Converts 1D array index to a 3D voxel id.
/// </summary>
/// <param name="id">Array index</param>
/// <param name="resolution">Dimensions of the voxel volume</param>
uint3 id1Dto3D(uint id, uint3 resolution)
{
    uint3 o;
    o.z = id / (resolution.x * resolution.y);
    o.y = id / resolution.x - o.z * resolution.y;
    o.x = id - o.y * resolution.x - o.z * resolution.x * resolution.y;

    return o;
}



// ----- VOLUME SAMPLING

/// <summary>
/// Get the size of a single voxel in a voxel volume.
/// </summary>
inline float3 CellSize(uint3 resolution, float3 bounds)
{
    return bounds / float3( resolution);
}
#define CELL_SIZE(volume) volume##Bounds / float3( volume##Resolution);

/// <summary>
/// Get the volume UV of a voxel volume, in the range from 0.0 to 1.0
/// </summary>
inline float3 VolumeUVsFromId(uint3 id, uint3 resolution)
{
    return float3( (id + 0.5) / (float3) (resolution));
}

/// <summary>
/// Get the closest voxel Id of a voxel volume.
/// </summary>
inline uint3 IdFromVolumeUVs(float3 uv, uint3 resolution)
{
    return  (uint3) floor(uv * resolution);
}

/// <summary>
/// Convert a world-space position into a UV coordinate of a volume.
/// </summary>
float3 VolumeUVsFromWorldPos(float3 center, float3 bounds, float3 worldPos)
{
    if (bounds.x * bounds.y * bounds.z == 0) return float3(0,0,0);
    return (((worldPos - center) / bounds) + 0.5);
}

#define VOLUME_UV_FROM_WORLD(volume, worldPos) VolumeUVsFromWorldPos(volume##Center, volume##Bounds, worldPos);


/// <summary>
/// Convert a volume UV coordinate into a world-space position.
/// </summary>
inline float3 WorldPosFromVolumeUVs(float3 center, float3 bounds, float3 uv)
{
    return (uv - 0.5) * bounds + center;
}


/// <summary>
/// Convert a voxel Id into a world-space position.
/// </summary>
inline float3 WorldPosFromId(uint3 id, uint3 resolution, float3 volumeCenter, float3 volumeBounds)
{
    return WorldPosFromVolumeUVs(volumeCenter, volumeBounds,VolumeUVsFromId(id, resolution));
}


/// <summary>
/// Returns true if a world-space position is inside a volume.
/// </summary>
bool WorldPosInsideVolume(float3 center, float3 size, float3 worldPos)
{
    float3 uv = VolumeUVsFromWorldPos(center, size, worldPos);
    return (uv.x >= 0 && uv.y >= 0 && uv.z >= 0 && uv.x <= 1 && uv.y <= 1 && uv.z <= 1);
}



float4 Sample_RWVolume(RWTexture3D<float4> RW_Volume, float3 uv, bool clamp = false, int3 resolution = int3(8, 8, 8))
{
    float4 o;
    if(clamp) uv = min(uv, resolution - 1); 
    float3 lerp_factors = uv - floor(uv);
    
    o = lerp(lerp(lerp(RW_Volume[int3(floor(uv.xyz)) + int3(0, 0, 0)], RW_Volume[int3(floor(uv.xyz)) + int3(1, 0, 0)], lerp_factors.x),
                  lerp(RW_Volume[int3(floor(uv.xyz)) + int3(0, 1, 0)], RW_Volume[int3(floor(uv.xyz)) + int3(1, 1, 0)], lerp_factors.x),
                  lerp_factors.y),
             lerp(lerp(RW_Volume[int3(floor(uv.xyz)) + int3(0, 0, 1)], RW_Volume[int3(floor(uv.xyz)) + int3(1, 0, 1)], lerp_factors.x),
                  lerp(RW_Volume[int3(floor(uv.xyz)) + int3(0, 1, 1)], RW_Volume[int3(floor(uv.xyz)) + int3(1, 1, 1)], lerp_factors.x),
                  lerp_factors.y),
             lerp_factors.z);    
    return o;
}


half4 Sample_RWVolume(RWTexture3D<half4> RW_Volume, float3 uv, bool clamp = false, int3 resolution = int3(8, 8, 8))
{
    half4 o;
    if(clamp) uv = min(uv, resolution - 1); 
    float3 lerp_factors = uv - floor(uv);
    
    o = lerp(lerp(lerp(RW_Volume[int3(floor(uv.xyz)) + int3(0, 0, 0)], RW_Volume[int3(floor(uv.xyz)) + int3(1, 0, 0)], lerp_factors.x),
                  lerp(RW_Volume[int3(floor(uv.xyz)) + int3(0, 1, 0)], RW_Volume[int3(floor(uv.xyz)) + int3(1, 1, 0)], lerp_factors.x),
                  lerp_factors.y),
             lerp(lerp(RW_Volume[int3(floor(uv.xyz)) + int3(0, 0, 1)], RW_Volume[int3(floor(uv.xyz)) + int3(1, 0, 1)], lerp_factors.x),
                  lerp(RW_Volume[int3(floor(uv.xyz)) + int3(0, 1, 1)], RW_Volume[int3(floor(uv.xyz)) + int3(1, 1, 1)], lerp_factors.x),
                  lerp_factors.y),
             lerp_factors.z);    
    return o;
}


half Sample_RWVolume(RWTexture3D<half> RW_Volume, float3 uv, bool clamp = false, int3 resolution = int3(8, 8, 8))
{
    half o;
    if(clamp) uv = min(uv, resolution - 1); 
    float3 lerp_factors = uv - floor(uv);
    
    o = lerp(lerp(lerp(RW_Volume[int3(floor(uv.xyz)) + int3(0, 0, 0)], RW_Volume[int3(floor(uv.xyz)) + int3(1, 0, 0)], lerp_factors.x),
                  lerp(RW_Volume[int3(floor(uv.xyz)) + int3(0, 1, 0)], RW_Volume[int3(floor(uv.xyz)) + int3(1, 1, 0)], lerp_factors.x),
                  lerp_factors.y),
             lerp(lerp(RW_Volume[int3(floor(uv.xyz)) + int3(0, 0, 1)], RW_Volume[int3(floor(uv.xyz)) + int3(1, 0, 1)], lerp_factors.x),
                  lerp(RW_Volume[int3(floor(uv.xyz)) + int3(0, 1, 1)], RW_Volume[int3(floor(uv.xyz)) + int3(1, 1, 1)], lerp_factors.x),
                  lerp_factors.y),
             lerp_factors.z);    
    return o;
}


float4 Sample_Volume(Texture3D<float4> volume, float3 center, float3 size, float3 worldPos, bool cutoffEdge = true, float cutoffResult = 0)
{
    float4 o = cutoffResult.xxxx;
    float3 uv = VolumeUVsFromWorldPos(center, size, worldPos);    
    if(!cutoffEdge || (uv.x >= 0 && uv.y >= 0 && uv.z >= 0 && uv.x <= 1 && uv.y <= 1 && uv.z <= 1))
    {
        o = volume.SampleLevel(sampler_linear_clamp, uv, 0);
    }
    return o;
}

half4 Sample_Volume(Texture3D<half4> volume, float3 center, float3 size, float3 worldPos, bool cutoffEdge = true, half cutoffResult = 0)
{
    half4 o = cutoffResult.xxxx;
    float3 uv = VolumeUVsFromWorldPos(center, size, worldPos);    
    if(!cutoffEdge || (uv.x >= 0 && uv.y >= 0 && uv.z >= 0 && uv.x <= 1 && uv.y <= 1 && uv.z <= 1))
    {
        o = volume.SampleLevel(sampler_linear_clamp, uv, 0);
    }
    return o;
}

half Sample_Volume(Texture3D<half> volume, float3 center, float3 size, float3 worldPos, bool cutoffEdge = true, half cutoffResult = 0)
{
    half o = cutoffResult;
    float3 uv = VolumeUVsFromWorldPos(center, size, worldPos);    
    if(!cutoffEdge || (uv.x >= 0 && uv.y >= 0 && uv.z >= 0 && uv.x <= 1 && uv.y <= 1 && uv.z <= 1))
    {
        o = volume.SampleLevel(sampler_linear_clamp, uv, 0);
    }
    return o;
}

uint Sample_Volume(Texture3D<uint> volume, float3 center, float3 size, float3 worldPos, uint3 resolution)
{
    uint o = 0;
    float3 uv = VolumeUVsFromWorldPos(center, size, worldPos);
    uint4 id = uint4( uv * resolution, 0);
    
    if((uv.x >= 0 && uv.y >= 0 && uv.z >= 0 && uv.x <= 1 && uv.y <= 1 && uv.z <= 1))
    {
        o = volume.Load(id);
    }
    return o;
}

bool GetBitFromSliceMap(uint slice, uint depth)
{
    uint shifted = slice >> depth;
    return shifted & 1;
}

bool Sample_VolumeBool(Texture2D<uint> volume, float3 center, float3 size, float3 worldPos, uint3 resolution)
{
    bool o = false;
    float3 uv3D = VolumeUVsFromWorldPos(center, size, worldPos);
    uint3 id = IdFromVolumeUVs(uv3D, resolution);
    uint3 uv = uint3(id.xz, 0);
    uint depth = (uint) uv3D.y * resolution.y;
    
    if(uv3D.x >= 0 && uv3D.y >= 0 && uv3D.z >= 0 && uv3D.x <= 1 && uv3D.y <= 1 && uv3D.z <= 1)
    {
        uint sample = volume.Load(uv);
        o = GetBitFromSliceMap(sample, depth);
    }
    return o;
}


// ---- VECTOR FIELD OPERATORS 

float3 Gradient_Volume(Texture3D<float4> scalar_field, float3 center, float3 bounds, float3 worldPos, float partial_xyz)
{
    float left     = Sample_Volume(scalar_field, center, bounds, worldPos - float3(1,0,0) * partial_xyz * 0.5).w;
    float right    = Sample_Volume(scalar_field, center, bounds, worldPos + float3(1,0,0) * partial_xyz * 0.5).w;
    float bottom   = Sample_Volume(scalar_field, center, bounds, worldPos - float3(0,1,0) * partial_xyz * 0.5).w;
    float top      = Sample_Volume(scalar_field, center, bounds, worldPos + float3(0,1,0) * partial_xyz * 0.5).w;
    float back     = Sample_Volume(scalar_field, center, bounds, worldPos - float3(0,0,1) * partial_xyz * 0.5).w;
    float front    = Sample_Volume(scalar_field, center, bounds, worldPos + float3(0,0,1) * partial_xyz * 0.5).w;

    return float3(right - left, top - bottom, front - back) / partial_xyz;
}

half3 Gradient_Volume(Texture3D<half4> scalar_field, float3 center, float3 bounds, float3 worldPos, float partial_xyz)
{
    float left     = Sample_Volume(scalar_field, center, bounds, worldPos - float3(1,0,0) * partial_xyz * 0.5).w;
    float right    = Sample_Volume(scalar_field, center, bounds, worldPos + float3(1,0,0) * partial_xyz * 0.5).w;
    float bottom   = Sample_Volume(scalar_field, center, bounds, worldPos - float3(0,1,0) * partial_xyz * 0.5).w;
    float top      = Sample_Volume(scalar_field, center, bounds, worldPos + float3(0,1,0) * partial_xyz * 0.5).w;
    float back     = Sample_Volume(scalar_field, center, bounds, worldPos - float3(0,0,1) * partial_xyz * 0.5).w;
    float front    = Sample_Volume(scalar_field, center, bounds, worldPos + float3(0,0,1) * partial_xyz * 0.5).w;

    return half3(right - left, top - bottom, front - back) / partial_xyz;
}

half3 GetGradient(Texture3D<half> scalar_field, uint3 coord, float partial_xyz)
{
    half left     = scalar_field[coord - float3(1,0,0)];
    half right    = scalar_field[coord + float3(1,0,0)];
    half bottom   = scalar_field[coord - float3(0,1,0)];
    half top      = scalar_field[coord + float3(0,1,0)];
    half back     = scalar_field[coord - float3(0,0,1)];
    half front    = scalar_field[coord + float3(0,0,1)];

    return half3(right - left, top - bottom, front - back) / partial_xyz;
}

half3 GetGradient(Texture3D<half4> scalar_field, uint3 coord, float partial_xyz)
{
    half left     = scalar_field[coord - uint3(1,0,0)].w;
    half right    = scalar_field[coord + uint3(1,0,0)].w;
    half bottom   = scalar_field[coord - uint3(0,1,0)].w;
    half top      = scalar_field[coord + uint3(0,1,0)].w;
    half back     = scalar_field[coord - uint3(0,0,1)].w;
    half front    = scalar_field[coord + uint3(0,0,1)].w;

    return half3(right - left, top - bottom, front - back) / partial_xyz;
}

float4 GetGradient(RWTexture3D<float4> scalar_field, float partial_xyz, uint3 coord)
{    
    float left     = scalar_field[(coord - uint3(1, 0, 0))].w;
    float right    = scalar_field[(coord + uint3(1, 0, 0))].w;
    float bottom   = scalar_field[(coord - uint3(0, 1, 0))].w;
    float top      = scalar_field[(coord + uint3(0, 1, 0))].w;
    float back     = scalar_field[(coord - uint3(0, 0, 1))].w;
    float front    = scalar_field[(coord + uint3(0, 0, 1))].w;

    return float4(right - left, top - bottom, front - back, 0.0)  / partial_xyz;
}

half4 GetGradient(RWTexture3D<half4> scalar_field, float partial_xyz, uint3 coord)
{    
    half left     = scalar_field[(coord - uint3(1, 0, 0))].w;
    half right    = scalar_field[(coord + uint3(1, 0, 0))].w;
    half bottom   = scalar_field[(coord - uint3(0, 1, 0))].w;
    half top      = scalar_field[(coord + uint3(0, 1, 0))].w;
    half back     = scalar_field[(coord - uint3(0, 0, 1))].w;
    half front    = scalar_field[(coord + uint3(0, 0, 1))].w;

    return float4(right - left, top - bottom, front - back, 0.0)  / partial_xyz;
}



float GetDivergence(RWTexture3D<float4> vectorField, uint3 cellId, uint3 resolution)
{
    float div = 0;

    div += (vectorField[cellId.xyz + uint3(1, 0, 0)].x - vectorField[cellId.xyz - uint3(1, 0, 0)].x) * step(1, cellId.x) * step(cellId.x, resolution.x - 2);
    div += (vectorField[cellId.xyz + uint3(0, 1, 0)].y - vectorField[cellId.xyz - uint3(0, 1, 0)].y) * step(1, cellId.y) * step(cellId.y, resolution.y - 2);
    div += (vectorField[cellId.xyz + uint3(0, 0, 1)].z - vectorField[cellId.xyz - uint3(0, 0, 1)].z) * step(1, cellId.z) * step(cellId.z, resolution.z - 2);

    return div;
}

half GetDivergence(RWTexture3D<half4> vectorField, uint3 cellId, uint3 resolution)
{
    half div = 0;

    div += (vectorField[cellId.xyz + uint3(1, 0, 0)].x - vectorField[cellId.xyz - uint3(1, 0, 0)].x) * step(1, cellId.x) * step(cellId.x, resolution.x - 2);
    div += (vectorField[cellId.xyz + uint3(0, 1, 0)].y - vectorField[cellId.xyz - uint3(0, 1, 0)].y) * step(1, cellId.y) * step(cellId.y, resolution.y - 2);
    div += (vectorField[cellId.xyz + uint3(0, 0, 1)].z - vectorField[cellId.xyz - uint3(0, 0, 1)].z) * step(1, cellId.z) * step(cellId.z, resolution.z - 2);

    return div;
}





half3 GetCurl(RWTexture3D<half4> vectorField, uint3 cellId, float3 partialXYZ)
{
    half3 curl = 0;

    curl.x = (vectorField[cellId.xyz + uint3(0, 1, 0)].z - vectorField[cellId.xyz - uint3(0, 1, 0)].z
           - (vectorField[cellId.xyz + uint3(0, 0, 1)].y - vectorField[cellId.xyz - uint3(0, 0, 1)].y)) / partialXYZ.x;

    curl.y = (vectorField[cellId.xyz + uint3(0, 0, 1)].x - vectorField[cellId.xyz - uint3(0, 0, 1)].x
           - (vectorField[cellId.xyz + uint3(1, 0, 0)].z - vectorField[cellId.xyz - uint3(1, 0, 0)].z)) / partialXYZ.y;

    curl.z = (vectorField[cellId.xyz + uint3(1, 0, 0)].y - vectorField[cellId.xyz - uint3(1, 0, 0)].y
           - (vectorField[cellId.xyz + uint3(0, 1, 0)].x - vectorField[cellId.xyz - uint3(0, 1, 0)].x)) / partialXYZ.z;

    return curl;
}


#endif