using UnityEngine;
using DynaMak.Utility;

namespace DynaMak.Volumes
{
    [System.Serializable]
    public class DensityEmitterVoxelizer : DensityEmitterBase
    {
        #region Serialize Fields

        [Header("Emission Settings")] 
        [SerializeField, SerializeReference] protected VolumeComponent _voxelVolumeComponent;

        #endregion

        #region Shader Property IDs

        private int voxelVolumeID = Shader.PropertyToID("_VoxelVolume");
        private int voxelCenterID = Shader.PropertyToID("_VoxelCenter");
        private int voxelBoundsID = Shader.PropertyToID("_VoxelBounds");

        #endregion

        // -------------------------------

        #region Override Functions

        protected override void SetOtherComputeValues()
        {
            _computeShader.SetVolume(0, _voxelVolumeComponent.GetVolumeTexture(), voxelVolumeID, voxelCenterID, voxelBoundsID);
        }

        #endregion


        #region Editor

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(_voxelVolumeComponent.GetVolumeTexture().Center, _voxelVolumeComponent.GetVolumeTexture().Bounds * 2);
        }

        #endregion
    }
}