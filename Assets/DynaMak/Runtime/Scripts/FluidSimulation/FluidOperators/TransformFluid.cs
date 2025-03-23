using UnityEngine;
using DynaMak.Utility;
using UnityEngine.Rendering;

namespace DynaMak.Volumes.FluidSimulation
{
    public class TransformFluid : FluidFieldOperator
    {
        #region Serialize Fields
        [Header("Transform Settings")]
        #endregion

        #region Shader Property IDs
        private int fluidCenterOldID = Shader.PropertyToID("_FluidCenterOld");
        private int fluidBoundsOldID = Shader.PropertyToID("_FluidBoundsOld");
        private int pingFieldID = Shader.PropertyToID("_PingField");
        #endregion

        #region Private Fields

        private RenderTexture _pingTexture;
        private bool _initialized;
        private Vector3 _oldVolumeCenter;
        private Vector3 _oldVolumeBounds;

        #endregion
        

        #region Private Functions

        void CalculateFieldShift(VolumeTexture volumeTexture)
        {
            Vector3 pos = transform.position;
            Vector3 bounds = transform.localScale;

            _oldVolumeCenter = volumeTexture.Center;
            if (pos != _oldVolumeCenter || bounds != _oldVolumeBounds)
            {
                volumeTexture.SetTransforms(pos, bounds);
                _computeShader.SetVector(fluidCenterOldID, _oldVolumeCenter);
                _computeShader.SetVector(fluidBoundsOldID, _oldVolumeBounds);
                
                _computeShader.SetVector(fluidCenterID, pos);
                _computeShader.SetVector(fluidBoundsID, bounds);
                
                _computeShader.SetTexture(0, pingFieldID, volumeTexture.Texture);
                _computeShader.SetTexture(0, fluidVolumeID, _pingTexture);
                _computeShader.Dispatch(0, volumeTexture.Resolution, ThreadBlockSize);
                
                
                _computeShader.SetTexture(1, pingFieldID, _pingTexture);
                _computeShader.SetTexture(1, fluidVolumeID, volumeTexture.Texture);
                _computeShader.Dispatch(1, volumeTexture.Resolution, ThreadBlockSize);
            }

            _oldVolumeBounds = volumeTexture.Bounds;
        }

        #endregion
        
        
        #region Override Functions

        public override string ComputeShaderPath => "FluidSimulation/Fluid_Transform";

        public override void ApplyOperation(VolumeTexture volumeTexture)
        {
            base.ApplyOperation(volumeTexture);
            if(!_initialized) InitializeBuffers(volumeTexture);
            
            CalculateFieldShift(volumeTexture);
        }
        #endregion
        
        
        #region Private Functions

        void InitializeBuffers(VolumeTexture volumeTexture)
        {
            ReleaseBuffers();
            
            _pingTexture = new RenderTexture(volumeTexture.Resolution.x, volumeTexture.Resolution.y, 0,
                RenderTextureFormat.ARGBHalf);
            _pingTexture.dimension = TextureDimension.Tex3D;
            _pingTexture.volumeDepth = volumeTexture.Resolution.z;
            _pingTexture.enableRandomWrite = true;
            _pingTexture.Create();

            _initialized = true;
        }
        

        void ReleaseBuffers()
        {
            if(_pingTexture) _pingTexture.Release();
        }

        #endregion
    }
}