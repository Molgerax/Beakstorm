using UnityEngine;

namespace Beakstorm.ComputeHelpers
{
    /// <summary>
    /// Static class to perform Bitonic Merge Sort for Spatial Hash Grids via compute shader.
    /// </summary>
    public static class SpatialHash
    {
        public static void UpdateSpatialHash(ComputeShader cs, int capacity, float hashCellSize, GraphicsBuffer spatialIndicesBuffer, GraphicsBuffer spatialOffsetsBuffer, GraphicsBuffer positionBuffer)
        {
            int kernelId;
            try { kernelId = cs.FindKernel("UpdateSpatialHash"); }
            catch { return; }

            cs.SetInt(PropertyIDs.TotalCount, capacity);
            cs.SetFloat(PropertyIDs.HashCellSize, hashCellSize);
            cs.SetBuffer(kernelId, PropertyIDs.SpatialIndices, spatialIndicesBuffer);
            cs.SetBuffer(kernelId, PropertyIDs.SpatialOffsets, spatialOffsetsBuffer);
            cs.SetBuffer(kernelId, PropertyIDs.PositionBuffer, positionBuffer);
            cs.DispatchExact(kernelId, capacity);
        }


        private static class PropertyIDs
        {
            public static readonly int TotalCount = Shader.PropertyToID("_TotalCount");
            public static readonly int HashCellSize = Shader.PropertyToID("_HashCellSize");
            public static readonly int SpatialIndices = Shader.PropertyToID("_SpatialIndices");
            public static readonly int SpatialOffsets = Shader.PropertyToID("_SpatialOffsets");
            public static readonly int PositionBuffer = Shader.PropertyToID("_PositionBuffer");
        }
    }
}