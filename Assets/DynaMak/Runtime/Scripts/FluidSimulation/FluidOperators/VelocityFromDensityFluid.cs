using UnityEngine;
using DynaMak.Utility;

namespace DynaMak.Volumes.FluidSimulation
{
    public class VelocityFromDensityFluid : FluidFieldOperator
    {
        #region Serialize Fields
        [Header("Advection Settings")]
        [SerializeField] [Range(0f, 10f)] private float _minimumDensity = 1f;
        [SerializeField] [Range(0f, 1f)] private float _velocityStrength = 1f;
        #endregion

        #region Shader Property IDs
        private int minimumDensityID = Shader.PropertyToID("_MinimumDensity");
        private int velocityStrengthID = Shader.PropertyToID("_VelocityStrength");
        #endregion

        #region Override Functions
        public override string ComputeShaderPath => "FluidSimulation/Fluid_DensityVelocity";
        
        public override void ApplyOperation(VolumeTexture volumeTexture)
        {
            base.ApplyOperation(volumeTexture);
            
            _computeShader.SetFloat(minimumDensityID, _minimumDensity);
            _computeShader.SetFloat(velocityStrengthID, _velocityStrength);
            _computeShader.Dispatch(0, volumeTexture.Resolution, ThreadBlockSize);
        }

        #endregion
    }
}