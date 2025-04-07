using UnityEngine;
using DynaMak.Utility;

namespace DynaMak.Volumes.FluidSimulation
{
    public class VelocityFluidBoundarySDF : FluidFieldOperator
    {
        #region Serialize Fields
        [Header("Velocity Boundary SDF Settings")] 
        [SerializeField] private VolumeComponent sdfComponent;
        [SerializeField, Range(0f,1f)] private float reboundStrength;
        #endregion

        #region Shader Property IDs
        private int sdfVolumeID = Shader.PropertyToID("_SDFVolume");
        private int sdfCenterID = Shader.PropertyToID("_SDFCenter");
        private int sdfBoundsID = Shader.PropertyToID("_SDFBounds");
        
        private int reboundID = Shader.PropertyToID("_Rebound");
        #endregion

        
        #region Override Functions
        
        public override string ComputeShaderPath => "FluidSimulation/Fluid_VelocityBoundarySDF";
        
        public override void ApplyOperation(VolumeTexture volumeTexture)
        {
            base.ApplyOperation(volumeTexture);

            if (sdfComponent)
            {
                _computeShader.SetVolume(0, sdfComponent.GetVolumeTexture(), sdfVolumeID, sdfCenterID, sdfBoundsID);
                _computeShader.SetFloat(reboundID, reboundStrength);
                _computeShader.Dispatch(0, volumeTexture.Resolution, ThreadBlockSize);
            }
            
        }
        #endregion
    }
}