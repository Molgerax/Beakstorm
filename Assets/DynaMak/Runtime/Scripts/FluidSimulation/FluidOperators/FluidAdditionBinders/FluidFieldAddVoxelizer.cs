using UnityEngine;
using DynaMak.Utility;

namespace DynaMak.Volumes.FluidSimulation
{
    [System.Serializable]
    public class FluidFieldAddVoxelizer : FluidFieldAddBase
    {
        #region Serialize Fields

        [Header("Addition Settings")]
        [SerializeField, SerializeReference] protected VolumeComponent voxelVolumeComponent;

        [SerializeField] [Range(0f, 3f)] private float density = 0.5f; 

        #endregion

        #region Shader Property IDs

        private int voxelVolumeID = Shader.PropertyToID("_VoxelVolume");
        private int voxelCenterID = Shader.PropertyToID("_VoxelCenter");
        private int voxelBoundsID = Shader.PropertyToID("_VoxelBounds");
        
        private int addDensityID = Shader.PropertyToID("_AddDensity");

        #endregion

        // -------------------------------

        #region Override Functions
        
        public override string ComputeShaderPath => "FluidSimulation/FluidAddOperators/Fluid_AddVoxelizer";

        protected override void SetProperties()
        {
            base.SetProperties();
            
            _computeShader.SetVolume(0, voxelVolumeComponent.GetVolumeTexture(), voxelVolumeID, voxelCenterID, voxelBoundsID);
            
            _computeShader.SetFloat(addDensityID, density);
        }

        #endregion
    }
}