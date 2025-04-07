using System;
using System.Collections.Generic;
using UnityEngine;
using DynaMak.Utility;
using Unity.Collections;

namespace DynaMak.Volumes.FluidSimulation
{
    public class FluidFieldAddSphereList : FluidFieldOperator
    {
        #region References
        [SerializeField] protected FluidField fluidField;
        #endregion


        #region Private Fields

        private Vector3 _currentPosition, _previousPosition;
        
        private List<FluidFieldAddSphere> _subscribedSpheres = new();

        private ComputeBuffer _sphereBuffer;
        private int _bufferLength;
        
        #endregion
        
        
        #region Shader Property IDs
    
        protected int worldMatrixID = Shader.PropertyToID("_WorldMatrix");
        protected int worldPosID = Shader.PropertyToID("_WorldPos");
        protected int worldPosOldID = Shader.PropertyToID("_WorldPosOld");
        
        private int _infoBufferID = Shader.PropertyToID("_InfoBuffer");
        private int _infoLengthID = Shader.PropertyToID("_InfoLength");

        #endregion

        #region Subscribers

        public void AddSphereEmitter(FluidFieldAddSphere sphere)
        {
            _subscribedSpheres ??= new List<FluidFieldAddSphere>();
            
            if (_subscribedSpheres.Contains(sphere) == false)
                _subscribedSpheres.Add(sphere);
        }
        
        public void RemoveSphereEmitter(FluidFieldAddSphere sphere)
        {
            _subscribedSpheres ??= new List<FluidFieldAddSphere>();
            
            if (_subscribedSpheres.Contains(sphere))
                _subscribedSpheres.Remove(sphere);
        }

        #endregion
        
        
        #region Subscription
        
        public override string ComputeShaderPath => "FluidSimulation/FluidAddOperators/Fluid_AddSphereList";
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

        private void OnDestroy()
        {
            Release();
        }

        #endregion

        
        #region Set Properties

        protected virtual void Initialize()
        {
            _currentPosition = transform.position;
            _previousPosition = _currentPosition;
        }

        private void UpdateBuffer()
        {
            _sphereBuffer ??= new ComputeBuffer(8, sizeof(float) * 9, ComputeBufferType.Structured, 
                ComputeBufferMode.SubUpdates);
            
            if (_subscribedSpheres.Count > _sphereBuffer.count)
            {
                int count = _sphereBuffer.count * 2;
                Release();
                _sphereBuffer = new ComputeBuffer(count, sizeof(float) * 9, ComputeBufferType.Structured, 
                    ComputeBufferMode.SubUpdates);
            }

            //NativeArray<Vector3> array = new NativeArray<Vector3>(_sphereBuffer.count * 3, Allocator.Temp,
            //    NativeArrayOptions.UninitializedMemory);

            NativeArray<Vector3> array = _sphereBuffer.BeginWrite<Vector3>(0, _bufferLength * 3);
            
            int j = 0;
            for (int i = _subscribedSpheres.Count - 1; i >= 0; i--)
            {
                FluidFieldAddSphere sphere = _subscribedSpheres[i];
                if (sphere == null)
                {
                    _subscribedSpheres.RemoveAt(i);
                    _bufferLength--;
                    continue;
                }
                
                Transform t = sphere.transform;
                Vector3 pos = t.position;
                Vector3 dir = t.forward;
                array[j * 3 + 0] = pos;
                array[j * 3 + 1] = dir;
                array[j * 3 + 2] = new(sphere.Strength, sphere.Radius, sphere.Density);

                j++;
            }
            
            _sphereBuffer.EndWrite<Vector3>(_bufferLength * 3);
            
            //_sphereBuffer.SetData(array);
            //array.Dispose();
        }

        public override void ApplyOperation(VolumeTexture volumeTexture)
        {
            if(!isActiveAndEnabled) return;
            
            _bufferLength = _subscribedSpheres.Count;
            if (_subscribedSpheres.Count == 0)
                return;
            
            base.ApplyOperation(volumeTexture);
            
            UpdateBuffer();
            
            _computeShader.SetVector(worldPosID, _currentPosition);
            _computeShader.SetVector(worldPosOldID, _previousPosition);
            _computeShader.SetMatrix(worldMatrixID, transform.localToWorldMatrix);
            
            _computeShader.SetInt(_infoLengthID, _bufferLength);
            _computeShader.SetBuffer(0, _infoBufferID, _sphereBuffer);
            
            _computeShader.Dispatch(0, volumeTexture.Resolution, ThreadBlockSize);
        }

        private void Release()
        {
            if (_sphereBuffer != null)
            {
                _sphereBuffer.Release();
                _sphereBuffer = null;
            }
        }

        #endregion
    }
}