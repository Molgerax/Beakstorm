#ifndef _INCLUDE_SPATIAL_HASH_GRIDS_CELL_ORDERING_
#define _INCLUDE_SPATIAL_HASH_GRIDS_CELL_ORDERING_

// --Parameters for Spatial Hash usage--

#define ITERATE_GRID_NEIGHBORS(sideLength, cellOffset, gridOffset) \
for(int iterator = 0; iterator < sideLength * sideLength * sideLength; iterator++)\
{ \
int3 gridOffset = GetIntegerOffsets3D(sideLength, iterator) + cellOffset; \


int3 Index1Dto3D(uint index, uint3 dimensions)
{
    int3 offset = 0;
    offset.z = index / (dimensions.x * dimensions.y);
    offset.y = index / dimensions.x - offset.z * dimensions.y;
    offset.x = index - offset.y * dimensions.x - offset.z * dimensions.x * dimensions.y;
    return offset;
}

inline uint Index3Dto1D(uint3 id, uint3 dimensions)
{
    id = clamp(id, 0, dimensions - 1);
    return (id.x) + (id.y * dimensions.x) + (id.z * dimensions.x * dimensions.y);
}

inline int GetCellCoverageSideLength(float checkRadius, float cellSize)
{
    return ceil(2 * checkRadius / cellSize) + 1;
}

int3 GetIntegerOffsets3D(uint sideLength, uint index)
{
    index %= sideLength * sideLength * sideLength;
    int3 offset = Index1Dto3D(index, sideLength);
    return offset - floor(sideLength / 2.0);
}

int3 GetCellOffset(float3 position, uint sideLength, float cellSize)
{
    if (sideLength % 2 != 0)
        return 0;

    int3 diff = ceil(position / cellSize) - round(position / cellSize);
    return -diff;
}

int3 GetGridCellId(float3 pos, float cellSize, float3 boundsCenter, float3 boundsSize)
{
    float3 minBound = boundsCenter - boundsSize * 0.5;
    pos = pos - minBound; 
    return floor( pos / cellSize);
}

uint KeyFromCellId(int3 cellId, int3 cellDimensions)
{
    return Index3Dto1D(cellId, cellDimensions);
}



/// Updates SpatialIndices buffer by storing the indices along with their new keys
void SetSpatialHash_CellOrdered(uint index, float3 position, uint totalCount, float cellSize, RWStructuredBuffer<uint3> indices, RWStructuredBuffer<uint> offsets)
{ 
    if (index >= totalCount)
        return;
	
    // Reset offsets
    offsets[index] = totalCount;
}



#endif