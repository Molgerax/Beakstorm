using UnityEngine;

namespace Beakstorm.ComputeHelpers
{
    /// <summary>
    /// Static class to perform Bitonic Merge Sort for Spatial Hash Grids via compute shader.
    /// </summary>
    public static class GPUBitonicMergeSort
    {
        private const string SORT_KERNEL = "SortKernel";
        private const string CALCULATE_OFFSETS_KERNEL = "CalculateOffsetsKernel";

        // Shader Property IDs
        private static readonly int _entriesPropertyID = Shader.PropertyToID("_Entries");
        private static readonly int _offsetsPropertyID = Shader.PropertyToID("_Offsets");
        private static readonly int _entryCountPropertyID = Shader.PropertyToID("_EntryCount");
        private static readonly int _groupWidthPropertyID = Shader.PropertyToID("_GroupWidth");
        private static readonly int _groupHeightPropertyID = Shader.PropertyToID("_GroupHeight");
        private static readonly int _stepIndexPropertyID = Shader.PropertyToID("_StepIndex");

        private static int SortKernel = 0;
        private static int CalculateOffsetsKernel = 1;


        private static void GetKernelIndices(ComputeShader cs)
        {
            SortKernel = cs.FindKernel(SORT_KERNEL);
            CalculateOffsetsKernel = cs.FindKernel(CALCULATE_OFFSETS_KERNEL);
        }

        private static void SetBuffers(ComputeShader cs, GraphicsBuffer indexBuffer, GraphicsBuffer offsetBuffer)
        {
            cs.SetBuffer(SortKernel, _entriesPropertyID, indexBuffer);
            
            cs.SetBuffer(CalculateOffsetsKernel, _entriesPropertyID, indexBuffer);
            
            if (offsetBuffer != null)
                cs.SetBuffer(CalculateOffsetsKernel, _offsetsPropertyID, offsetBuffer);
        }

        // Sorts given buffer of integer values using bitonic merge sort
        private static void Sort(ComputeShader cs, GraphicsBuffer indexBuffer)
        {
            int indexBufferCount = indexBuffer.count;
            cs.SetInt(_entryCountPropertyID, indexBufferCount);

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

                    cs.SetInt(_groupWidthPropertyID, groupWidth);
                    cs.SetInt(_groupHeightPropertyID, groupHeight);
                    cs.SetInt(_stepIndexPropertyID, stepIndex);

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
        public static void SortAndCalculateOffsets(ComputeShader cs, GraphicsBuffer indexBuffer, GraphicsBuffer offsetBuffer, bool noHashValue = false)
        {
            if (noHashValue)
                cs.EnableKeyword("NO_HASH");
            else    
                cs.DisableKeyword("NO_HASH");
            
            GetKernelIndices(cs);
            SetBuffers(cs, indexBuffer, offsetBuffer);
            Sort(cs, indexBuffer);
            
            if(offsetBuffer != null) 
                cs.DispatchExact(CalculateOffsetsKernel, indexBuffer.count);
        }
    }
}