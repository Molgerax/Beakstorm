using Beakstorm.Simulation.Collisions.SDF;
using UnityEngine;

namespace Beakstorm.ComputeHelpers
{
    /// <summary>
    /// Static class to perform Bitonic Merge Sort for Spatial Hash Grids via compute shader.
    /// </summary>
    public static class BitonicMergeSort
    {
        private const string SORT_KERNEL = "SortKernel";
        private const string CALCULATE_OFFSETS_KERNEL = "CalculateOffsetsKernel";

        // Shader Property IDs

        private static int SortKernel = 0;
        private static int CalculateOffsetsKernel = 1;


        private static void GetKernelIndices(ComputeShader cs)
        {
            SortKernel = cs.FindKernel(SORT_KERNEL);
            CalculateOffsetsKernel = cs.FindKernel(CALCULATE_OFFSETS_KERNEL);
        }

        // Sorts given buffer of integer values using bitonic merge sort
        private static void Sort(ComputeShader cs, GraphicsBuffer indexBuffer, GraphicsBuffer cellIdBuffer)
        {
            int indexBufferCount = indexBuffer.count;
            cs.SetInt(PropertyIDs.EntryCount, indexBufferCount);

            cs.SetBuffer(SortKernel, PropertyIDs.IndexBuffer, indexBuffer);
            cs.SetBuffer(SortKernel, PropertyIDs.CellIdBuffer, cellIdBuffer);

            // Number of steps of bitonic merge:
            // log2(n) * (log2(n) + 1) / 2
            // n => nearest power of 2 equal or greater to number of inputs
            int numStages = (int)Mathf.Log(Mathf.NextPowerOfTwo(indexBufferCount), 2);

            for (int stageIndex = 0; stageIndex < numStages; stageIndex++)
            {
                for (int stepIndex = 0; stepIndex < stageIndex + 1; stepIndex++)
                {
                    // Calculate the pattern seen in https://en.wikipedia.org/wiki/Bitonic_sorter#How_the_algorithm_works
                    int groupWidth = 1 << (stageIndex - stepIndex);
                    int groupHeight = 2 * groupWidth - 1;

                    cs.SetInt(PropertyIDs.GroupWidth, groupWidth);
                    cs.SetInt(PropertyIDs.GroupHeight, groupHeight);
                    cs.SetInt(PropertyIDs.StepIndex, stepIndex);

                    // Run current sorting step
                    cs.DispatchExact(SortKernel, Mathf.NextPowerOfTwo(indexBufferCount) / 2);
                }
            }
        }

        /// <summary>
        /// Sorts an index buffer of a spatial hash grid using bitonic merge sort and calculates the start indices of the hash grid cells.
        /// </summary>
        /// <param name="cs">Sorting Compute Shader</param>
        /// <param name="indexBuffer">Buffer containing data entries, which each contain a data index, a cell hash and a key</param>
        /// <param name="offsetBuffer">Buffer to store the start indices of each hash grid cell</param>
        public static void SortAndCalculateOffsets(ComputeShader cs, GraphicsBuffer indexBuffer, GraphicsBuffer cellIdBuffer, GraphicsBuffer offsetBuffer, bool noHashValue = false)
        {
            if (noHashValue)
                cs.EnableKeyword("NO_HASH");
            else    
                cs.DisableKeyword("NO_HASH");
            
            GetKernelIndices(cs);
            
            Sort(cs, indexBuffer, cellIdBuffer);
            
            cs.SetBuffer(CalculateOffsetsKernel, PropertyIDs.IndexBuffer, indexBuffer);
            cs.SetBuffer(CalculateOffsetsKernel, PropertyIDs.CellIdBuffer, cellIdBuffer);
            cs.SetBuffer(CalculateOffsetsKernel, PropertyIDs.PointerBuffer, offsetBuffer);

            cs.SetInt(PropertyIDs.CellCount, offsetBuffer.count);
            cs.DispatchExact(CalculateOffsetsKernel, offsetBuffer.count);
        }

        public static class PropertyIDs
        {
            public static readonly int IndexBuffer = Shader.PropertyToID("_IndexBuffer");
            public static readonly int CellIdBuffer = Shader.PropertyToID("_CellIdBuffer");
            public static readonly int PointerBuffer = Shader.PropertyToID("_PointerBuffer");
            public static readonly int CellCount = Shader.PropertyToID("_CellCount");
            public static readonly int EntryCount = Shader.PropertyToID("_EntryCount");
            public static readonly int GroupWidth = Shader.PropertyToID("_GroupWidth");
            public static readonly int GroupHeight = Shader.PropertyToID("_GroupHeight");
            public static readonly int StepIndex = Shader.PropertyToID("_StepIndex");
        }
    }
}