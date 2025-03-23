using UnityEngine;
using UnityEngine.Rendering;
using DynaMak.Utility;

namespace DynaMak.Volumes.FluidSimulation
{
    public class VorticityFluidConfinement : FluidFieldOperator
    {
        #region Serialize Fields
        [Header("Vorticity Confinement Settings")]
        [SerializeField] [Range(0f, 20f)] private float _confinementFactor = 1f;
        #endregion

        #region Shader Property IDs
        private int confinementFactorID = Shader.PropertyToID("_ConfinementFactor");
        private int curlID = Shader.PropertyToID("_CurlField");
        #endregion

        #region Private Properties

        private RenderTexture _curl;
        private bool _initialized = false;
        
        #endregion

        #region Override Functions

        public override string ComputeShaderPath => "FluidSimulation/Fluid_VorticityConfinement";
        
        public override void ApplyOperation(VolumeTexture volumeTexture)
        {
            if(!_initialized) InitializeBuffers(volumeTexture);
            
            base.ApplyOperation(volumeTexture);
            
            _computeShader.SetFloat(confinementFactorID, _confinementFactor);
            _computeShader.SetTexture(0, curlID, _curl);
            _computeShader.Dispatch(0, volumeTexture.Resolution, ThreadBlockSize);
            
            _computeShader.SetTexture(1, curlID, _curl);
            _computeShader.SetTexture(1, fluidVolumeID, volumeTexture.Texture);
            _computeShader.Dispatch(1, volumeTexture.Resolution, ThreadBlockSize);
        }

        #endregion


        #region Private Functions


        void InitializeBuffers(VolumeTexture volumeTexture)
        {
            ReleaseBuffers();
            
            _curl = new RenderTexture(volumeTexture.Resolution.x, volumeTexture.Resolution.y, 0,
                RenderTextureFormat.ARGBHalf);
            _curl.dimension = TextureDimension.Tex3D;
            _curl.volumeDepth = volumeTexture.Resolution.z;
            _curl.enableRandomWrite = true;
            _curl.Create();

            _initialized = true;
        }

        void ReleaseBuffers()
        {
            if(_curl) _curl.Release();
        }
        
        #endregion
    }
}