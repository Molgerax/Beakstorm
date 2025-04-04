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

        [SerializeField] private BoidManager boidManager;
        
        public static WeakPointManager Instance;

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
            compute.SetBuffer(kernel, "_DamageBuffer", DamageBuffer);
            compute.SetBuffer(kernel, "_FlushDamageBuffer", _flushDamageBuffer);
            compute.SetInt("_Count", WeakPoints.Count);
            
            compute.Dispatch(kernel, 1, 1, 1);
        }
        

        private void CollideBoids()
        {
            if (!boidManager)
                return;
            
            int kernel = compute.FindKernel("CollideBoids");
            
            compute.SetBuffer(kernel, "_WeakPointPositions", WeakPointBuffer);
            compute.SetBuffer(kernel, "_DamageBuffer", DamageBuffer);
            compute.SetInt("_Count", WeakPoints.Count);

            compute.SetBuffer(kernel, "_SpatialIndices", boidManager.SpatialIndicesBuffer);
            compute.SetBuffer(kernel, "_SpatialOffsets", boidManager.SpatialOffsetsBuffer);
            compute.SetBuffer(kernel, "_BoidPositionBuffer", boidManager.PositionBuffer);
            compute.SetBuffer(kernel, "_BoidOldPositionBuffer", boidManager.OldPositionBuffer);
            compute.SetFloat("_HashCellSize", boidManager.HashCellSize);
            compute.SetFloat("_NumBoids", boidManager.Capacity);
            
            compute.DispatchExact(kernel, _bufferSize);
        }
    }
}
