using System.Collections.Generic;
using System.Text;
using Beakstorm.ComputeHelpers;
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

        public static WeakPointManager Instance;
        
        public static List<WeakPoint> WeakPoints = new List<WeakPoint>(16);
        public GraphicsBuffer WeakPointBuffer;
        public GraphicsBuffer DamageBuffer;
        private GraphicsBuffer _flushDamageBuffer;
        
        private Vector4[] _weakPointPositions = new Vector4[16];
        private int _bufferSize = 16;

        private AsyncGPUReadbackRequest _request;
        private NativeArray<int> _damageArray;

        private StringBuilder _logBuilder;

        private void Awake()
        {
            Instance = this;
            Initialize();
        }

        private void Initialize()
        {
            WeakPointBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, _bufferSize, sizeof(float) * 4);
            DamageBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, _bufferSize, sizeof(int) * 1);
            _flushDamageBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, _bufferSize, sizeof(int) * 1);
         
            _damageArray = new NativeArray<int>(_bufferSize, Allocator.Persistent);
            DamageBuffer.SetData(_damageArray);
            _flushDamageBuffer.SetData(_damageArray);
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
            UpdateBufferSize();
            UpdatePositions();
            
            RequestDamageValues();
            
            CollideBoids();
        }

        private void UpdatePositions()
        {
            if (logDebugInfo)
            {
                _logBuilder ??= new StringBuilder();
                _logBuilder.Append("Weak Point Positions:\n");
            }
            
            for (int i = 0; i < WeakPoints.Count; i++)
            {
                _weakPointPositions[i] = WeakPoints[i].PositionRadius;

                if (logDebugInfo)
                    _logBuilder.Append($"{i}: {_weakPointPositions[i]}\n");
            }
            WeakPointBuffer.SetData(_weakPointPositions);

            if (logDebugInfo)
            {
                Debug.Log(_logBuilder, this);
                _logBuilder.Clear();
            }
            
        }


        private void RequestDamageValues()
        {
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
                    for (int i = 0; i < WeakPoints.Count; i++)
                    {
                        int damage = _damageArray[i];
                        if (damage > 0)
                        {
                            WeakPoints[i].ApplyDamage(damage);
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
                _request = AsyncGPUReadback.RequestIntoNativeArray(ref _damageArray, DamageBuffer);
                
                if (logDebugInfo)
                {
                    Debug.Log(_logBuilder, this);
                    _logBuilder.Clear();
                }
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

                // TODO: time this, can lead to problems
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
            BoidGridManager manager = BoidGridManager.Instance;
            if (!manager || !manager.Initialized)
                return;

            
            
            int kernel = compute.FindKernel("CollideBoids");
            
            compute.SetBuffer(kernel, PropertyIDs.WeakPointBuffer, WeakPointBuffer);
            compute.SetBuffer(kernel, PropertyIDs.DamageBuffer, DamageBuffer);
            compute.SetInt(PropertyIDs.WeakPointCount, WeakPoints.Count);
            
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
        
        
        private static class PropertyIDs
        {
            public static readonly int TotalCount              = Shader.PropertyToID("_TotalCount");
            public static readonly int HashCellSize            = Shader.PropertyToID("_HashCellSize");
            
            public static readonly int Time                    = Shader.PropertyToID("_Time");
            public static readonly int DeltaTime               = Shader.PropertyToID("_DeltaTime");
            public static readonly int WeakPointBuffer          = Shader.PropertyToID("_WeakPointBuffer");
            public static readonly int DamageBuffer            = Shader.PropertyToID("_DamageBuffer");
            public static readonly int FlushDamageBuffer            = Shader.PropertyToID("_FlushDamageBuffer");
            public static readonly int WeakPointCount              = Shader.PropertyToID("_WeakPointCount");
            
            public static readonly int GridOffsetBuffer              = Shader.PropertyToID("_GridOffsetBuffer");
            public static readonly int BoidBuffer              = Shader.PropertyToID("_BoidBuffer");
        }
    }
}
