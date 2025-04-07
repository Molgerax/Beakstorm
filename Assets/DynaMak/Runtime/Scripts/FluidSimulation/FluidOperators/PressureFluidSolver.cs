using UnityEngine;
using UnityEngine.Rendering;
using DynaMak.Utility;

namespace DynaMak.Volumes.FluidSimulation
{
    public class PressureFluidSolver : FluidFieldOperator
    {
        #region Serialize Fields
        [Header("Pressure Solver Settings")] 
        [SerializeField] [Min(0)] private int iterationCount = 10;
        #endregion

        #region Private Fields

        private RenderTexture _divergence, _pressurePing, _pressurePong;
        private bool _pingIsResult;
        private bool _initialized = false;

        #endregion

        #region Shader Property IDs
        private int divergenceFieldID = Shader.PropertyToID("_DivergenceField");
        private int pressurePingID = Shader.PropertyToID("_PressureField_Ping");
        private int pressurePongID = Shader.PropertyToID("_PressureField_Pong");
        #endregion

        
        #region Override Functions
        
        public override string ComputeShaderPath => "FluidSimulation/Fluid_PressureSolver";
        
        public override void ApplyOperation(VolumeTexture volumeTexture)
        {
            if(iterationCount <= 0) return;
            
            if(!_initialized) InitializeBuffers(volumeTexture);
            
            DispatchDivergence(volumeTexture);
            
            DispatchClearPressure(volumeTexture);

            bool pingIsResult = DispatchJacobiSolver(volumeTexture);
            
            DispatchProject(volumeTexture, pingIsResult);
        }
        #endregion

        #region Mono Methods
        private void OnDestroy()
        {
            ReleaseBuffers();
        }
        private void OnDisable()
        {
            ReleaseBuffers();
        }
        #endregion

        #region Dispatch Function

        void DispatchDivergence(VolumeTexture volumeTexture)
        {
            _computeShader.SetVolume(0, volumeTexture, fluidVolumeID, fluidCenterID, fluidBoundsID, fluidResolutionID);
            _computeShader.SetTexture(0, divergenceFieldID, _divergence);
            _computeShader.Dispatch(0, volumeTexture.Resolution, ThreadBlockSize);
        }

        void DispatchClearPressure(VolumeTexture volumeTexture)
        {
            _computeShader.SetTexture(1, pressurePongID, _pressurePing);
            _computeShader.Dispatch(1, volumeTexture.Resolution, ThreadBlockSize);
            
            _computeShader.SetTexture(1, pressurePongID, _pressurePong);
            _computeShader.Dispatch(1, volumeTexture.Resolution, ThreadBlockSize);
        }

        bool DispatchJacobiSolver(VolumeTexture volumeTexture)
        {
            bool pingIsResult = false;
            
            _computeShader.SetTexture(2, divergenceFieldID, _divergence);
            
            for (int i = 0; i < iterationCount; i++)
            {
                pingIsResult = !pingIsResult;
            
                if(!pingIsResult)
                {
                    _computeShader.SetTexture(2, pressurePingID, _pressurePing);
                    _computeShader.SetTexture(2, pressurePongID, _pressurePong);
                }
                else
                {
                    _computeShader.SetTexture(2, pressurePingID, _pressurePong);
                    _computeShader.SetTexture(2, pressurePongID, _pressurePing);
                }

                _computeShader.Dispatch(2, volumeTexture.Resolution, ThreadBlockSize);
            }

            return pingIsResult;
        }

        void DispatchProject(VolumeTexture volumeTexture, bool pingIsResult)
        {
            _computeShader.SetVolume(3, volumeTexture, fluidVolumeID, fluidCenterID, fluidBoundsID, fluidResolutionID);
            _computeShader.SetTexture(3, pressurePingID, pingIsResult ? _pressurePing : _pressurePong);
            _computeShader.Dispatch(3, volumeTexture.Resolution, ThreadBlockSize);
        }
        
        #endregion 
        
        
        #region Private Functions

        void InitializeBuffers(VolumeTexture volumeTexture)
        {
            ReleaseBuffers();
            
            _divergence = new RenderTexture(volumeTexture.Resolution.x, volumeTexture.Resolution.y, 0,
                RenderTextureFormat.RHalf);
            _divergence.dimension = TextureDimension.Tex3D;
            _divergence.volumeDepth = volumeTexture.Resolution.z;
            _divergence.enableRandomWrite = true;
            _divergence.Create();
            
            _pressurePing = new RenderTexture(volumeTexture.Resolution.x, volumeTexture.Resolution.y, 0,
                RenderTextureFormat.RHalf);
            _pressurePing.dimension = TextureDimension.Tex3D;
            _pressurePing.volumeDepth = volumeTexture.Resolution.z;
            _pressurePing.enableRandomWrite = true;
            _pressurePing.Create();
            
            _pressurePong = new RenderTexture(volumeTexture.Resolution.x, volumeTexture.Resolution.y, 0,
                RenderTextureFormat.RHalf);
            _pressurePong.dimension = TextureDimension.Tex3D;
            _pressurePong.volumeDepth = volumeTexture.Resolution.z;
            _pressurePong.enableRandomWrite = true;
            _pressurePong.Create();

            _initialized = true;
        }
        

        void ReleaseBuffers()
        {
            if(_divergence) _divergence.Release();
            if(_pressurePing) _pressurePing.Release();
            if(_pressurePong) _pressurePong.Release();
        }

        #endregion
    }
}