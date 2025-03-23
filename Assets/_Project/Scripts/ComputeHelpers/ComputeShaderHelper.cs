using UnityEngine;

namespace Beakstorm.ComputeHelpers
{
    /// <summary>
    /// Helper functions and extension methods for <see cref="ComputeShader"/>
    /// </summary>
    public static class ComputeShaderHelper
    {
        // Extension methods

        /// <summary>
        /// Execute a compute shader with exact number of threads.
        /// </summary>
        /// <param name="cs">Compute Shader</param>
        /// <param name="kernelIndex">Kernel to execute</param>
        /// <param name="totalThreadCountX">Threads to be executed on "X axis"</param>
        /// <param name="totalThreadCountY">Threads to be executed on "Y axis"</param>
        /// <param name="totalThreadCountZ">Threads to be executed on "Z axis"</param>
        public static void DispatchExact(this ComputeShader cs, int kernelIndex,
            int totalThreadCountX = 1, int totalThreadCountY = 1, int totalThreadCountZ = 1)
        {
            Vector3Int threadGroupSize = cs.GetKernelThreadGroupSizes(kernelIndex);
            int numGroupsX = Mathf.CeilToInt(totalThreadCountX / (float)threadGroupSize.x);
            int numGroupsY = Mathf.CeilToInt(totalThreadCountY / (float)threadGroupSize.y);
            int numGroupsZ = Mathf.CeilToInt(totalThreadCountZ / (float)threadGroupSize.z);
            cs.Dispatch(kernelIndex, numGroupsX, numGroupsY, numGroupsZ);
        }

        /// <summary>
        /// Execute a compute shader with exact number of threads.
        /// </summary>
        /// <param name="cs">Compute Shader</param>
        /// <param name="kernelIndex">Kernel to execute</param>
        /// <param name="totalThreadCounts">Threads to be executed</param>
        public static void DispatchExact(this ComputeShader cs, int kernelIndex, Vector3Int totalThreadCounts)
        {
            cs.DispatchExact(kernelIndex, totalThreadCounts.x, totalThreadCounts.y, totalThreadCounts.z);
        }

        public static Vector3Int GetKernelThreadGroupSizes(this ComputeShader cs, int kernelIndex = 0)
        {
            cs.GetKernelThreadGroupSizes(kernelIndex, out uint x, out uint y, out uint z);
            return new Vector3Int((int)x, (int)y, (int)z);
        }

        /// <summary>
        /// Sets Vector3Int as integer array data without allocating garbage.
        /// </summary>
        /// <param name="cs"></param>
        /// <param name="nameID"></param>
        /// <param name="value"></param>
        public static void SetInts(this ComputeShader cs, int nameID, Vector3Int value)
        {
            cs.SetInts(nameID, value.ToArray());
        }


        // Vector3Int Extensions

        private static readonly int[] Vector3ToArray = new int[3];
        
        /// <summary>
        /// Turns a Vector3Int into an int array. Only intended to be used for passing an integer array once, as
        /// it uses a static array to cache the values without allocating garbage, e.g. <see cref="ComputeShader.SetInts()"/>
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static int[] ToArray(this Vector3Int value)
        {
            for (int i = 0; i < 3; i++)
                Vector3ToArray[i] = value[i];
            return Vector3ToArray;
        }
    }
}
