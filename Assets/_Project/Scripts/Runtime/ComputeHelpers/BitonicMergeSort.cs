using UnityEngine;

namespace Beakstorm.ComputeHelpers
{
    public static class BitonicMergeSort
    {
        /// <summary>
        /// Sorts a graphics buffer
        /// </summary>
        /// <param name="cs">BitonicMergeSort compute shader</param>
        /// <param name="buffer">Must be of pairs of two values, the first being the value to sort by.</param>
        public static void SortBuffer(ComputeShader cs, GraphicsBuffer buffer)
        {
            int sortKernel = cs.FindKernel("SortKernel");
            
            int count = buffer.count;
            int roundedCount = Mathf.NextPowerOfTwo(count);
            
            cs.SetBuffer(sortKernel, PropertyIDs.SortingBuffer, buffer);
            cs.SetInt(PropertyIDs.Count, count);

            // Needs log_2(n) stages
            int numStages = (int)Mathf.Log(roundedCount, 2);

            for (int stage = 0; stage < numStages; stage++)
            {
                //each stage requires the same number of steps as the current stage index
                for (int k = 0; k < stage + 1; k++)
                {
                    int groupWidth = 1 << (stage - k);

                    cs.SetInt(PropertyIDs.GroupWidth, groupWidth);
                    cs.SetInt(PropertyIDs.StepIndex, k);
                    
                    // As two values are compared using one operation, we need half of the item count in threads
                    cs.DispatchExact(sortKernel, roundedCount / 2);
                }
            }
        }

        private static class PropertyIDs
        {
            public static readonly int SortingBuffer = Shader.PropertyToID("_SortBuffer");
            public static readonly int Count = Shader.PropertyToID("_Count");
            public static readonly int GroupWidth = Shader.PropertyToID("_GroupWidth");
            public static readonly int StepIndex = Shader.PropertyToID("_StepIndex");
        }
    }
}