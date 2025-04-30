using System;
using System.Collections.Generic;
using Beakstorm.ComputeHelpers;
using Beakstorm.Simulation.Particles;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;

namespace Beakstorm.Simulation.Collisions
{
    [DefaultExecutionOrder(-100)]
    public class WeakPointManager : MonoBehaviour
    {
        [SerializeField] private ComputeShader compute;

        public static WeakPointManager Instance;
        
        private BoidManager _boidManager;

        public List<WeakPoint> WeakPoints = new List<WeakPoint>(16);
        public GraphicsBuffer WeakPointBuffer;
        public GraphicsBuffer DamageBuffer;
        private GraphicsBuffer _flushDamageBuffer;
        
        private Vector4[] _weakPointPositions = new Vector4[16];
        private int _bufferSize = 16;

        private AsyncGPUReadbackRequest _request;
        private NativeArray<int> _damageArray;

        private void Awake()
        {
            Instance = this;
            WeakPointBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, _bufferSize, sizeof(float) * 4);
            DamageBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, _bufferSize, sizeof(int) * 1);
            _flushDamageBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, _bufferSize, sizeof(int) * 1);
            
            _damageArray = new NativeArray<int>(_bufferSize, Allocator.Persistent);
        }

        private void Start()
        {
            _boidManager = BoidManager.Instance;
        }

        private void OnDestroy()
        {
            WeakPointBuffer?.Dispose();
            DamageBuffer?.Dispose();
            _flushDamageBuffer?.Dispose();

            _request.WaitForCompletion();
            _damageArray.Dispose();
        }

        private void Update()
        {
            UpdateBufferSize();
            UpdatePositions();
            
            RequestDamageValues();
            
            CollideBoids();
        }

        private void UpdatePositions()
        {
            for (int i = 0; i < WeakPoints.Count; i++)
            {
                _weakPointPositions[i] = WeakPoints[i].PositionRadius;
            }
            WeakPointBuffer.SetData(_weakPointPositions);
        }


        private void RequestDamageValues()
        {
            if (_request.done)
            {
                if (!_request.hasError)
                {
                    bool flush = false;
                    for (int i = 0; i < WeakPoints.Count; i++)
                    {
                        int damage = _damageArray[i];
                        if (damage > 0)
                        {
                            WeakPoints[i].ApplyDamage(damage);
                            flush = true;
                        }
                    }
                    if (flush)
                        FlushDamage();
                }
                _request = AsyncGPUReadback.RequestIntoNativeArray(ref _damageArray, DamageBuffer);
            }
        }
        
        private void UpdateBufferSize()
        {
            if (WeakPoints.Count > _bufferSize)
            {
                _bufferSize *= 2;
                
                WeakPointBuffer?.Dispose();
                WeakPointBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, _bufferSize, sizeof(float) * 4);
                
                DamageBuffer?.Dispose();
                DamageBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, _bufferSize, sizeof(int) * 1);
                
                _flushDamageBuffer?.Dispose();
                _flushDamageBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, _bufferSize, sizeof(int) * 1);

                _damageArray.Dispose();
                _damageArray = new NativeArray<int>(_bufferSize, Allocator.Persistent);
                
                _weakPointPositions = new Vector4[_bufferSize];
            }
        }

        private void FlushDamage()
        {
            _flushDamageBuffer.SetData(_damageArray);
            
            int kernel = compute.FindKernel("FlushDamage");
            compute.SetBuffer(kernel, PropertyIDs.DamageBuffer, DamageBuffer);
            compute.SetBuffer(kernel, PropertyIDs.FlushDamageBuffer, _flushDamageBuffer);
            compute.SetInt(PropertyIDs.WeakPointCount, WeakPoints.Count);
            
            compute.Dispatch(kernel, 1, 1, 1);
        }
        

        private void CollideBoids()
        {
            if (!_boidManager)
                return;
            
            int kernel = compute.FindKernel("CollideBoids");
            
            compute.SetBuffer(kernel, PropertyIDs.WeakPointBuffer, WeakPointBuffer);
            compute.SetBuffer(kernel, PropertyIDs.DamageBuffer, DamageBuffer);
            compute.SetInt(PropertyIDs.WeakPointCount, WeakPoints.Count);
            
            compute.SetFloat(PropertyIDs.Time, Time.time);
            compute.SetFloat(PropertyIDs.DeltaTime, Time.deltaTime);

            compute.SetBuffer(kernel, PropertyIDs.SpatialIndices, _boidManager.SpatialIndicesBuffer);
            compute.SetBuffer(kernel, PropertyIDs.SpatialOffsets, _boidManager.SpatialOffsetsBuffer);
            compute.SetBuffer(kernel, PropertyIDs.PositionBuffer, _boidManager.PositionBuffer);
            compute.SetBuffer(kernel, PropertyIDs.OldPositionBuffer, _boidManager.OldPositionBuffer);
            
            compute.SetFloat(PropertyIDs.HashCellSize, _boidManager.HashCellSize);
            compute.SetFloat(PropertyIDs.TotalCount, _boidManager.Capacity);
            
            compute.DispatchExact(kernel, _bufferSize);
        }
        
        
        
        public static class PropertyIDs
        {
            public static readonly int TotalCount              = Shader.PropertyToID("_TotalCount");
            public static readonly int HashCellSize            = Shader.PropertyToID("_HashCellSize");
            
            public static readonly int Time                    = Shader.PropertyToID("_Time");
            public static readonly int DeltaTime               = Shader.PropertyToID("_DeltaTime");
            public static readonly int PositionBuffer          = Shader.PropertyToID("_PositionBuffer");
            public static readonly int OldPositionBuffer       = Shader.PropertyToID("_OldPositionBuffer");
            public static readonly int WeakPointBuffer          = Shader.PropertyToID("_WeakPointBuffer");
            public static readonly int DamageBuffer            = Shader.PropertyToID("_DamageBuffer");
            public static readonly int FlushDamageBuffer            = Shader.PropertyToID("_FlushDamageBuffer");
            public static readonly int WeakPointCount              = Shader.PropertyToID("_WeakPointCount");
            
            public static readonly int SpatialIndices              = Shader.PropertyToID("_BoidSpatialIndices");
            public static readonly int SpatialOffsets              = Shader.PropertyToID("_BoidSpatialOffsets");
        }
    }
}
