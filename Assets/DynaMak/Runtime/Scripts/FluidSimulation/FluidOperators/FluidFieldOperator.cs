using System;
using UnityEngine;
using DynaMak.Utility;
using UnityEditor;

namespace DynaMak.Volumes.FluidSimulation
{
    public abstract class FluidFieldOperator : MonoBehaviour
    {
        #region Serialized Properties
        [SerializeField] protected ComputeShader _computeShader;
        #endregion

        #region Protected Properties

        protected const int ThreadBlockSize = 2;

        #endregion
        
        #region Shader Property IDs
        
        protected int fluidVolumeID = Shader.PropertyToID("_FluidVolume");
        protected int fluidCenterID = Shader.PropertyToID("_FluidCenter");
        protected int fluidBoundsID = Shader.PropertyToID("_FluidBounds");
        protected int fluidResolutionID = Shader.PropertyToID("_FluidResolution");

        protected int dtID = Shader.PropertyToID("_dt");

        #endregion
        

        #region Overridable Functions

        public virtual string ComputeShaderPath => String.Empty;

        public virtual void ApplyOperation(VolumeTexture volumeTexture)
        {
            _computeShader.SetFloat(dtID, Time.deltaTime);
            _computeShader.SetVolume(0, volumeTexture, fluidVolumeID, fluidCenterID, fluidBoundsID, fluidResolutionID);
        }

        protected virtual void Reset()
        {
            _computeShader = Resources.Load<ComputeShader>(ComputeShaderPath);
        }

        #endregion
    }
}