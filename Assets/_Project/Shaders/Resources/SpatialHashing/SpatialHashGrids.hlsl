#ifndef _INCLUDE_SPATIAL_HASH_GRIDS_
#define _INCLUDE_SPATIAL_HASH_GRIDS_

// --Parameters for Spatial Hash usage--

//RWStructuredBuffer<uint3> _SpatialIndices;
//RWStructuredBuffer<uint> _SpatialOffsets;

// -------------------------------------

#define SPATIAL_HASH_BUFFERS(name) \
RWStructuredBuffer<uint3> name##SpatialIndices; \
RWStructuredBuffer<uint> name##SpatialOffsets; \
void SetSpatialHash##name(uint index, float3 position, uint totalCount, float cellSize)\
{ \
    if (index >= totalCount)\
        return;\
    name##SpatialOffsets[index] = totalCount;\
    int3 cell = GetCell3D(position, cellSize);\
    uint hashCell = HashCell3D(cell);\
    uint key = KeyFromHash(hashCell, totalCount);\
    name##SpatialIndices[index] = uint3(index, hashCell, key);\
}\

#define SPATIAL_HASH_BUFFERS_READ(name) \
StructuredBuffer<uint3> name##SpatialIndices; \
StructuredBuffer<uint> name##SpatialOffsets; \


static const int3 offsets3D[27] =
{
    int3(-1, -1, -1),
    int3(-1, -1, 0),
    int3(-1, -1, 1),
    int3(-1, 0, -1),
    int3(-1, 0, 0),
    int3(-1, 0, 1),
    int3(-1, 1, -1),
    int3(-1, 1, 0),
    int3(-1, 1, 1),
    int3(0, -1, -1),
    int3(0, -1, 0),
    int3(0, -1, 1),
    int3(0, 0, -1),
    int3(0, 0, 0),
    int3(0, 0, 1),
    int3(0, 1, -1),
    int3(0, 1, 0),
    int3(0, 1, 1),
    int3(1, -1, -1),
    int3(1, -1, 0),
    int3(1, -1, 1),
    int3(1, 0, -1),
    int3(1, 0, 0),
    int3(1, 0, 1),
    int3(1, 1, -1),
    int3(1, 1, 0),
    int3(1, 1, 1)
};

// Constants used for hashing
static const uint hashK1 = 15823;
static const uint hashK2 = 9737333;
static const uint hashK3 = 440817757;

// Convert floating point position into an integer cell coordinate
int3 GetCell3D(float3 position, float radius)
{
    return (int3) floor(position / radius);
}

// Hash cell coordinate to a single unsigned integer
uint HashCell3D(int3 cell)
{
    cell = (uint3) cell;
    return (cell.x * hashK1) + (cell.y * hashK2) + (cell.z * hashK3);
}

uint KeyFromHash(uint hash, uint tableSize)
{
    return hash % tableSize;
}


/// Updates SpatialIndices buffer by storing the indices along with their new keys
void SetSpatialHash(uint index, float3 position, uint totalCount, float cellSize, RWStructuredBuffer<uint3> indices, RWStructuredBuffer<uint> offsets)
{ 
    if (index >= totalCount)
        return;
	
    // Reset offsets
    offsets[index] = totalCount;
    int3 cell = GetCell3D(position, cellSize);
    uint hashCell = HashCell3D(cell);
    uint key = KeyFromHash(hashCell, totalCount);
    indices[index] = uint3(index, hashCell, key);
}




#endif