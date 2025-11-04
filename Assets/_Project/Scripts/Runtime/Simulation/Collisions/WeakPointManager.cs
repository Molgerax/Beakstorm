using System;
using System.Collections.Generic;
using System.Text;
using Beakstorm.ComputeHelpers;
using Beakstorm.Pausing;
using Beakstorm.Simulation.Particles;
using Beakstorm.Utility;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;

namespace Beakstorm.Simulation.Collisions
{
    [DefaultExecutionOrder(-100)]
    public class WeakPointManager : MonoBehaviour
    {
        [SerializeField] private ComputeShader compute;

        [SerializeField] private ImpactParticleManager impact;
        
        [SerializeField] private bool logDebugInfo = false;
        [SerializeField] private bool logDebugInfoPos = false;

        private const int INIT_BUFFER_SIZE = 16;
        
        public static WeakPointManager Instance;
        
        public GraphicsBuffer WeakPointBuffer;
        public GraphicsBuffer DamageBuffer;
        private GraphicsBuffer _flushDamageBuffer;
        
        private static AutoFilledArray<WeakPoint> WeakPoints = new AutoFilledArray<WeakPoint>(INIT_BUFFER_SIZE); 

        private Vector4[] _weakPointPositions = new Vector4[INIT_BUFFER_SIZE];
        private int _bufferSize = INIT_BUFFER_SIZE;

        private AsyncGPUReadbackRequest _request;
        private NativeArray<int> _damageArray;

        private StringBuilder _logBuilder;

        private bool _pauseForResize;

        private int WeakPointCount => Mathf.Min(WeakPoints.Count, _bufferSize);
        
        private void Awake()
        {
            Instance = this;
            Initialize();
        }

        private void Initialize()
        {
            _bufferSize = WeakPoints.Size;
            
            WeakPointBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, _bufferSize, sizeof(float) * 4);
            DamageBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, _bufferSize, sizeof(int) * 1);
            _flushDamageBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, _bufferSize, sizeof(int) * 1);
         
            _damageArray = new NativeArray<int>(_bufferSize, Allocator.Persistent);
            DamageBuffer.SetData(_damageArray);
            _flushDamageBuffer.SetData(_damageArray);
        }


        public static void AddWeakPoint(WeakPoint weakPoint)
        {
            WeakPoints.AddElement(weakPoint);
        }
        
        public static void RemoveWeakPoint(WeakPoint weakPoint)
        {
            WeakPoints.RemoveElement(weakPoint); 
        }

        private void Release()
        {
            WeakPointBuffer?.Dispose();
            DamageBuffer?.Dispose();
            _flushDamageBuffer?.Dispose();

            _request.WaitForCompletion();
            _damageArray.Dispose();
        }
        
        private void OnDestroy()
        {
            Release();
        }

        private void Update()
        {
            WeakPoints.UpdateArray();
            
            UpdatePositions();
            
            RequestDamageValues();
            
            CollideBoids();

            if (_pauseForResize)
            {
                UpdateBufferSize();
            }
        }

        private void UpdatePositions()
        {
            if (logDebugInfoPos)
            {
                _logBuilder ??= new StringBuilder();
                _logBuilder.Append("Weak Point Positions:\n");
            }
            
            for (int i = 0; i < WeakPointCount; i++)
            {
                WeakPoint wp = WeakPoints[i];
                Vector4 pos = Vector4.zero;

                if (wp)
                    pos = wp.IsValid ? wp.PositionRadius : Vector4.zero;

                _weakPointPositions[i] = pos;
                if (logDebugInfoPos)
                    _logBuilder.Append($"{i}: {_weakPointPositions[i]}\n");
            }
            WeakPointBuffer.SetData(_weakPointPositions);

            if (logDebugInfoPos)
            {
                Debug.Log(_logBuilder, this);
                _logBuilder.Clear();
            }
        }


        private void RequestDamageValues()
        {
            if (_pauseForResize)
            {
                _request = AsyncGPUReadback.RequestIntoNativeArray(ref _damageArray, DamageBuffer);
                _pauseForResize = false;
                return;
            }
            
            if (_request.done)
            {
                if (logDebugInfo)
                {
                    _logBuilder ??= new StringBuilder();
                    _logBuilder.Append("Request Damage Info:\n");
                    if (_request.hasError) 
                        _logBuilder.Append($"!!Has Error!!");
                }
                
                if (!_request.hasError)
                {
                    bool flush = false;
                    int count = WeakPointCount;
                    for (int i = 0; i < count; i++)
                    {
                        WeakPoint weakPoint = WeakPoints[i];
                        int damage = _damageArray[i];
                        
                        if (!weakPoint)
                        {
                            if (logDebugInfo)
                                _logBuilder.Append($"{i}: {damage} - Destroyed\n");
                            continue;
                        }
                        
                        if (damage > 0)
                        {
                            weakPoint.ApplyDamage(damage);
                            flush = true;
                        }

                        if (damage < 0)
                            flush = true;
                        
                        if (logDebugInfo)
                            _logBuilder.Append($"{i}: {damage}\n");
                    }
                    if (flush)
                        FlushDamage();
                }
                
                
                if (logDebugInfo)
                {
                    Debug.Log(_logBuilder, this);
                    _logBuilder.Clear();
                }
                
                if (WeakPoints.Count > _bufferSize)
                {
                    _pauseForResize = true;
                    return;
                }
                
                _request = AsyncGPUReadback.RequestIntoNativeArray(ref _damageArray, DamageBuffer);
            }
        }
        
        private void UpdateBufferSize()
        {
            if (WeakPoints.Count > _bufferSize)
            {
                while (WeakPoints.Count > _bufferSize)
                {
                    _bufferSize *= 2;
                }
                
                WeakPointBuffer?.Dispose();
                WeakPointBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, _bufferSize, sizeof(float) * 4);
                
                DamageBuffer?.Dispose();
                DamageBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, _bufferSize, sizeof(int) * 1);
                
                _flushDamageBuffer?.Dispose();
                _flushDamageBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, _bufferSize, sizeof(int) * 1);

                ClearBuffer(DamageBuffer);
                
                // TODO: time this, can lead to problems
                // Not anymore tho?
                _damageArray.Dispose();
                _damageArray = new NativeArray<int>(_bufferSize, Allocator.Persistent);

                _weakPointPositions = new Vector4[_bufferSize];
                WeakPoints.Resize(_bufferSize);
            }
        }

        private void FlushDamage()
        {
            _flushDamageBuffer.SetData(_damageArray);
            
            int kernel = compute.FindKernel("FlushDamage");
            compute.SetBuffer(kernel, PropertyIDs.DamageBuffer, DamageBuffer);
            compute.SetBuffer(kernel, PropertyIDs.FlushDamageBuffer, _flushDamageBuffer);
            compute.SetInt(PropertyIDs.WeakPointCount, WeakPointCount);
            compute.SetInt(PropertyIDs.BufferSize, _bufferSize);
            
            compute.DispatchExact(kernel, _bufferSize);
        }


        private void ClearBuffer(GraphicsBuffer buffer)
        {
            int kernel = compute.FindKernel("Clear");
            compute.SetBuffer(kernel, PropertyIDs.DamageBuffer, buffer);
            compute.SetInt(PropertyIDs.WeakPointCount, _bufferSize);
            
            compute.DispatchExact(kernel, _bufferSize);
        }
        
        private void CollideBoids()
        {
            BoidGridManager manager = BoidGridManager.Instance;
            if (!manager || !manager.Initialized)
                return;

            if (PauseManager.IsPaused)
                return;

            int kernel = compute.FindKernel("CollideBoids");
            
            compute.SetBuffer(kernel, PropertyIDs.WeakPointBuffer, WeakPointBuffer);
            compute.SetBuffer(kernel, PropertyIDs.DamageBuffer, DamageBuffer);
            compute.SetInt(PropertyIDs.WeakPointCount, WeakPointCount);
            
            compute.SetFloat(PropertyIDs.Time, Time.time);
            compute.SetFloat(PropertyIDs.DeltaTime, SimulationTime.DeltaTime);

            compute.SetFloat(PropertyIDs.HashCellSize, manager.CellSize);
            compute.SetInt(PropertyIDs.TotalCount, manager.AgentCount);
            
            manager.Hash.SetShaderProperties(compute);
            compute.SetBuffer(kernel, PropertyIDs.GridOffsetBuffer, manager.Hash.GridOffsetBuffer);
            compute.SetBuffer(kernel, PropertyIDs.BoidBuffer, manager.AgentBufferRead);

            if (impact && impact.Initialized)
            {
                impact.SetImpactBuffer(compute, kernel);
            }
            
            compute.DispatchExact(kernel, _bufferSize);
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = new(0, 1, 1, 0.5f);
            for (var i = 0; i < _weakPointPositions.Length; i++)
            {
                Vector4 position = _weakPointPositions[i];
                if (position == Vector4.zero)
                    continue;

                if (i >= WeakPointCount)
                    continue;
                
                Gizmos.DrawWireSphere(position, position.w + 0.5f);
            }
        }


        private static class PropertyIDs
        {
            public static readonly int TotalCount = Shader.PropertyToID("_TotalCount");
            public static readonly int HashCellSize = Shader.PropertyToID("_HashCellSize");
            
            public static readonly int Time = Shader.PropertyToID("_Time");
            public static readonly int DeltaTime = Shader.PropertyToID("_DeltaTime");
            public static readonly int WeakPointBuffer = Shader.PropertyToID("_WeakPointBuffer");
            public static readonly int DamageBuffer = Shader.PropertyToID("_DamageBuffer");
            public static readonly int FlushDamageBuffer = Shader.PropertyToID("_FlushDamageBuffer");
            public static readonly int WeakPointCount = Shader.PropertyToID("_WeakPointCount");
            public static readonly int BufferSize = Shader.PropertyToID("_BufferSize");
            
            public static readonly int GridOffsetBuffer = Shader.PropertyToID("_GridOffsetBuffer");
            public static readonly int BoidBuffer = Shader.PropertyToID("_BoidBuffer");
        }
    }
}
