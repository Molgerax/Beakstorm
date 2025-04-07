using UnityEngine;
using DynaMak.Utility;

namespace DynaMak.Volumes.FluidSimulation
{
    public class AdvectFluid : FluidFieldOperator
    {
        #region Serialize Fields
        [Header("Advection Settings")]
        [SerializeField] [Range(0f, 10f)] private float _advectionFactor = 1f;
        #endregion

        #region Shader Property IDs
        private int advectionFactorID = Shader.PropertyToID("_AdvectionFactor");
        #endregion

        #region Override Functions
        public override string ComputeShaderPath => "FluidSimulation/Fluid_Advect";
        
        public override void ApplyOperation(VolumeTexture volumeTexture)
        {
            base.ApplyOperation(volumeTexture);
            
            _computeShader.SetFloat(advectionFactorID, _advectionFactor);
            _computeShader.Dispatch(0, volumeTexture.Resolution, ThreadBlockSize);
        }

        #endregion
    }
}