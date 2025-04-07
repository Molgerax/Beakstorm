using System;
using UnityEngine;
using DynaMak.Utility;

namespace DynaMak.Volumes.FluidSimulation
{
    public abstract class FluidFieldAddBase : FluidFieldOperator
    {
        #region References
        [SerializeField] protected FluidField fluidField;
        #endregion


        #region Private Fields

        private Vector3 _currentPosition, _previousPosition;

        #endregion
        
        
        #region Shader Property IDs
    
        protected int worldMatrixID = Shader.PropertyToID("_WorldMatrix");
        protected int worldPosID = Shader.PropertyToID("_WorldPos");
        protected int worldPosOldID = Shader.PropertyToID("_WorldPosOld");

        #endregion
        
        
        #region Subscription
        protected virtual void OnEnable()
        {
            if(!fluidField) return;
            
            Initialize();
            fluidField.AddOperator(this);
        }
        
        protected virtual void OnDisable()
        {
            if(!fluidField) return;

            fluidField.RemoveOperator(this);
        }


        protected virtual void Update()
        {
            _previousPosition = _currentPosition;
            _currentPosition = transform.position;
        }

        public void SetFluidField(FluidField fluid)
        {
            fluidField = fluid;
            
            if(isActiveAndEnabled) OnEnable();
        }

        #endregion

        
        #region Set Properties

        protected virtual void Initialize()
        {
            _currentPosition = transform.position;
            _previousPosition = _currentPosition;
        }

        protected virtual void SetProperties()
        {
            _computeShader.SetVector(worldPosID, _currentPosition);
            _computeShader.SetVector(worldPosOldID, _previousPosition);
            _computeShader.SetMatrix(worldMatrixID, transform.localToWorldMatrix);
        }

        public override void ApplyOperation(VolumeTexture volumeTexture)
        {
            if(!isActiveAndEnabled) return;
            
            base.ApplyOperation(volumeTexture);
            SetProperties();
            _computeShader.Dispatch(0, volumeTexture.Resolution, ThreadBlockSize);
        }

        #endregion
        
    }
}
