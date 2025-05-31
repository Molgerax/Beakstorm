#ifndef _INCLUDE_SPATIAL_HASH_GRIDS_CELL_ORDERING_
#define _INCLUDE_SPATIAL_HASH_GRIDS_CELL_ORDERING_

// --Parameters for Spatial Hash usage--

#define ITERATE_GRID_NEIGHBORS(sideLength, cellOffset, gridOffset) \
for(int iterator = 0; iterator < sideLength * sideLength * sideLength; iterator++)\
{ \
int3 gridOffset = GetIntegerOffsets3D(sideLength, iterator) + cellOffset; \


inline int GetCellCoverageSideLength(float checkRadius, float cellSize)
{
    return ceil(2 * checkRadius / cellSize) + 1;
}

int3 GetIntegerOffsets3D(int sideLength, int index)
{
    index %= sideLength * sideLength * sideLength;
    int3 offset = 0;
    offset.z = index / (sideLength * sideLength);
    offset.y = index / sideLength - offset.z * sideLength;
    offset.x = index - offset.y * sideLength - offset.z * sideLength * sideLength;
    return offset - floor(sideLength / 2.0);
}

int3 GetCellOffset(float3 position, int sideLength, float cellSize)
{
    if (sideLength % 2 != 0)
        return 0;

    int3 diff = ceil(position / cellSize) - round(position / cellSize);
    return -diff;
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