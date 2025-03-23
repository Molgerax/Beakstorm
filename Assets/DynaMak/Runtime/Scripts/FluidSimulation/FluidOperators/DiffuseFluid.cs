using UnityEngine;
using DynaMak.Utility;

namespace DynaMak.Volumes.FluidSimulation
{
    public class DiffuseFluid : FluidFieldOperator
    {
        #region Serialize Fields
        [Header("Advection Settings")]
        [SerializeField] [Range(0f, 10f)] private float _diffusionFactor = 1f;
        [SerializeField] [Range(0f, 1f)] private float _diffuseVelocity = 1f;
        #endregion

        #region Shader Property IDs
        private int diffusionFactorID = Shader.PropertyToID("_DiffusionFactor");
        private int diffuseVelocityID = Shader.PropertyToID("_DiffuseVelocity");
        #endregion

        #region Override Functions
        public override string ComputeShaderPath => "FluidSimulation/Fluid_Diffuse";
        
        public override void ApplyOperation(VolumeTexture volumeTexture)
        {
            base.ApplyOperation(volumeTexture);
            
            _computeShader.SetFloat(diffusionFactorID, _diffusionFactor);
            _computeShader.SetFloat(diffuseVelocityID, _diffuseVelocity);
            _computeShader.Dispatch(0, volumeTexture.Resolution, ThreadBlockSize);
        }

        #endregion
    }
}