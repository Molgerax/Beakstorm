using UnityEngine;
using DynaMak.Utility;
using UnityEngine.Rendering;

namespace DynaMak.Volumes.FluidSimulation
{
    public class RelocateFluid : FluidFieldOperator
    {
        #region Serialize Fields
        [Header("Relocate Settings")]
        #endregion

        #region Shader Property IDs
        private int fieldShiftID = Shader.PropertyToID("_FieldShift");
        private int pingFieldID = Shader.PropertyToID("_PingField");
        #endregion

        #region Private Fields

        private RenderTexture _pingTexture;
        private bool _initialized;
        private Vector3 _oldVolumeCenter;

        #endregion
        

        #region Private Functions

        void CalculateFieldShift(VolumeTexture volumeTexture)
        {
            Vector3 pos = transform.position;
            Vector3 cellSize = new Vector3(volumeTexture.Bounds.x / volumeTexture.Resolution.x, volumeTexture.Bounds.y / volumeTexture.Resolution.y, volumeTexture.Bounds.z / volumeTexture.Resolution.z);
            _oldVolumeCenter = volumeTexture.Center;
            Vector3 snappedCenter = pos - new Vector3(pos.x % cellSize.x, pos.y % cellSize.y, pos.z % cellSize.z);
            volumeTexture.SetCenter(snappedCenter);
            
            if (_oldVolumeCenter != snappedCenter)
            {
                int[] offset = new int[3];
                offset[0] = Mathf.RoundToInt((snappedCenter.x - _oldVolumeCenter.x) / cellSize.x);
                offset[1] = Mathf.RoundToInt((snappedCenter.y - _oldVolumeCenter.y) / cellSize.y);
                offset[2] = Mathf.RoundToInt((snappedCenter.z - _oldVolumeCenter.z) / cellSize.z);
                _computeShader.SetInts(fieldShiftID, offset);
                
                _computeShader.SetTexture(0, pingFieldID, volumeTexture.Texture);
                _computeShader.SetTexture(0, fluidVolumeID, _pingTexture);
                _computeShader.Dispatch(0, volumeTexture.Resolution, ThreadBlockSize);
                
                
                _computeShader.SetTexture(1, pingFieldID, _pingTexture);
                _computeShader.SetTexture(1, fluidVolumeID, volumeTexture.Texture);
                _computeShader.Dispatch(1, volumeTexture.Resolution, ThreadBlockSize);
            }
        }

        #endregion
        
        
        #region Override Functions

        public override string ComputeShaderPath => "FluidSimulation/Fluid_Relocate";

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