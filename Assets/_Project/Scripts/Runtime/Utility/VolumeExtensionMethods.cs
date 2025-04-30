using Beakstorm.Volumes;
using UnityEngine;

namespace Beakstorm.Utility
{
    public static class VolumeExtensionMethods
    {
        #region Vector3

        public static Vector3 PointWiseProduct(this Vector3 lhs, Vector3 rhs)
        {
            return new(lhs.x * rhs.x, lhs.y * rhs.y, lhs.z * rhs.z);
        }

        #endregion


        #region Mesh

        public static uint GetTotalIndexCount(this Mesh mesh)
        {
            uint count = 0;
            for (var i = 0; i < mesh.subMeshCount; i++) count += mesh.GetIndexCount(i);
            return count;
        }

        #endregion

        #region ComputerShader

        public static void SetVolume(this ComputeShader cs, int kernelIndex, VolumeTexture volumeTexture, int textureID,
            int centerID, int boundsID)
        {
            if (volumeTexture.IsInitialized == false)
            {
                cs.SetTexture(kernelIndex, textureID, Texture2D.blackTexture);
                cs.SetVector(centerID, Vector4.zero);
                cs.SetVector(boundsID, Vector4.zero);
                return;
            }

            cs.SetTexture(kernelIndex, textureID, volumeTexture.Texture);
            cs.SetVector(centerID, volumeTexture.Center);
            cs.SetVector(boundsID, volumeTexture.Bounds);
        }


        private static readonly int[] IntArray3 = {1, 1, 1};

        public static void SetVolume(this ComputeShader cs, int kernelIndex, VolumeTexture volumeTexture, int textureID,
            int centerID, int boundsID, int resolutionID)
        {
            if (volumeTexture.IsInitialized == false)
            {
                cs.SetTexture(kernelIndex, textureID, Texture2D.blackTexture);
                cs.SetVector(centerID, Vector4.zero);
                cs.SetVector(boundsID, Vector4.zero);
                cs.SetInts(resolutionID, IntArray3);
                return;
            }

            cs.SetTexture(kernelIndex, textureID, volumeTexture.Texture);
            cs.SetVector(centerID, volumeTexture.Center);
            cs.SetVector(boundsID, volumeTexture.Bounds);
            cs.SetInts(resolutionID, volumeTexture.ResolutionArray);
        }

        public static void SetVolume(this ComputeShader cs, int kernelIndex, VolumeTexture volumeTexture, string prefix,
            bool useResolution = true)
        {
            cs.SetTexture(kernelIndex, prefix + "Texture", volumeTexture.Texture);
            cs.SetVector(prefix + "Center", volumeTexture.Center);
            cs.SetVector(prefix + "Bounds", volumeTexture.Bounds);

            if (useResolution) cs.SetInts(prefix + "Resolution", volumeTexture.ResolutionArray);
        }


        public static void Dispatch(this ComputeShader cs, int kernelIndex, Vector3Int resolution, int threadBlockSize)
        {
            cs.Dispatch(kernelIndex, resolution.x / threadBlockSize, resolution.y / threadBlockSize,
                resolution.z / threadBlockSize);
        }

        #endregion

        #region MaterialPropertyBlock

        public static void SetVolume(this MaterialPropertyBlock propertyBlock, VolumeTexture volumeTexture,
            int textureID, int centerID, int boundsID)
        {
            propertyBlock.SetTexture(textureID, volumeTexture.Texture);
            propertyBlock.SetVector(centerID, volumeTexture.Center);
            propertyBlock.SetVector(boundsID, volumeTexture.Bounds);
        }

        public static void SetVolume(this MaterialPropertyBlock propertyBlock, VolumeTexture volumeTexture,
            int textureID, int centerID, int boundsID, int resolutionID)
        {
            propertyBlock.SetTexture(textureID, volumeTexture.Texture);
            propertyBlock.SetVector(centerID, volumeTexture.Center);
            propertyBlock.SetVector(boundsID, volumeTexture.Bounds);
            //propertyBlock.SetInts(resolutionID, volumeTexture.Resolution.x, volumeTexture.Resolution.y, volumeTexture.Resolution.z);
        }

        public static void SetVolume(this MaterialPropertyBlock propertyBlock, VolumeTexture volumeTexture,
            string prefix, bool useResolution = true)
        {
            propertyBlock.SetTexture(prefix + "Texture", volumeTexture.Texture);
            propertyBlock.SetVector(prefix + "Center", volumeTexture.Center);
            propertyBlock.SetVector(prefix + "Bounds", volumeTexture.Bounds);

            //if(useResolution) propertyBlock.SetInts(prefix + "Resolution", volumeTexture.Resolution.x, volumeTexture.Resolution.y, volumeTexture.Resolution.z);
        }

        #endregion

        #region Vector3Int

        public static int[] ToArray(this Vector3Int value, ref int[] array)
        {
            for (var i = 0; i < 3; i++)
                array[i] = value[i];

            return array;
        }

        private static readonly int[] _vector3ToArray = new int[3];

        /// <summary>
        ///     Turns a Vector3Int into an int array. Only intended to be used for passing an integer array once, as
        ///     it uses a static array to cache the values without allocating garbage, e.g. <see cref="ComputeShader.SetInts()" />
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static int[] ToArray(this Vector3Int value)
        {
            for (var i = 0; i < 3; i++)
                _vector3ToArray[i] = value[i];
            return _vector3ToArray;
        }


        public static Vector3Int PointWiseProduct(this Vector3Int lhs, Vector3Int rhs)
        {
            return new(lhs.x * rhs.x, lhs.y * rhs.y, lhs.z * rhs.z);
        }

        #endregion
    }
}