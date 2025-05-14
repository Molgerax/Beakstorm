using System;
using Beakstorm.ComputeHelpers;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;

namespace Beakstorm.Simulation.Particles
{
    public class ParticleCellAverage : MonoBehaviour
    {
        [SerializeField] private ComputeShader compute;
        private IHashedParticleSimulation sim;

        private GraphicsBuffer _positionBuffer;
        private GraphicsBuffer _velocityBuffer;
        private GraphicsBuffer _dataBuffer;
        private GraphicsBuffer _particleCountBuffer;

        private Vector3Int _dimensions;
        private int _cellCount;
        
        
        private AsyncGPUReadbackRequest _request;
        private NativeArray<int> _particleCountArray;
        public int[] ParticleCountArray; 

        private void Awake()
        {
            Initialize();
        }

        private void Update()
        {
            CollectParticleValues();
            RequestGpuData();
        }

        private void OnDestroy()
        {
            Release();
        }


        private void Initialize()
        {
            sim = GetComponent<IHashedParticleSimulation>();
            if (sim == null)
                return;

            _dimensions = Vector3Int.CeilToInt(sim.SimulationSpace / sim.HashCellSize);
            _cellCount = _dimensions.x * _dimensions.y * _dimensions.z;

            _particleCountBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, _cellCount, sizeof(int) * 1);

            _particleCountArray = new NativeArray<int>(_cellCount, Allocator.Persistent);
            _particleCountBuffer.SetData(_particleCountArray);
            ParticleCountArray = new int[_cellCount];
        }

        private void Release()
        {
            _positionBuffer?.Dispose();
            _velocityBuffer?.Dispose();
            _dataBuffer?.Dispose();
            _particleCountBuffer?.Dispose();
            
            _request.WaitForCompletion();
            _particleCountArray.Dispose();
        }
        
        private void RequestGpuData()
        {
            if (ParticleCountArray == null)
                return;
            
            if (_request.done)
            {
                if (!_request.hasError)
                {
                    _particleCountArray.CopyTo(ParticleCountArray);
                }
                _request = AsyncGPUReadback.RequestIntoNativeArray(ref _particleCountArray, _particleCountBuffer);
            }
        }

        private void CollectParticleValues()
        {
            if (!compute || sim == null)
                return;
            
            int kernel = compute.FindKernel("CollectValues");
            
            compute.SetBuffer(kernel, PropertyIDs.CountBuffer, _particleCountBuffer);
            
            compute.SetInt(PropertyIDs.TotalCount, _cellCount);

            compute.SetFloat(PropertyIDs.Time, Time.time);
            compute.SetFloat(PropertyIDs.DeltaTime, Time.deltaTime);

            compute.SetBuffer(kernel, PropertyIDs.SpatialIndices, sim.SpatialIndicesBuffer);
            compute.SetBuffer(kernel, PropertyIDs.SpatialOffsets, sim.SpatialOffsetsBuffer);
            compute.SetBuffer(kernel, PropertyIDs.ParticlePositionBuffer, sim.PositionBuffer);
            compute.SetBuffer(kernel, PropertyIDs.ParticleOldPositionBuffer, sim.OldPositionBuffer);
            compute.SetBuffer(kernel, PropertyIDs.ParticleDataBuffer, sim.DataBuffer);
            
            compute.SetFloat(PropertyIDs.HashCellSize, sim.HashCellSize);
            compute.SetInt(PropertyIDs.ParticleCount, sim.Capacity);
            compute.SetVector(PropertyIDs.SimulationSpace, sim.SimulationSpace);
            
            compute.DispatchExact(kernel, _cellCount);
        }

        private Vector3Int GetIndex3D(int index)
        {
            Vector3Int cellId = Vector3Int.zero;
            cellId.z = index % _dimensions.z; 
            cellId.y = (index / _dimensions.z) % _dimensions.y;
            cellId.x = index / (_dimensions.y * _dimensions.z);

            return cellId;
        }

        private Vector3 GetPosition(int index)
        {
            Vector3Int cellId = GetIndex3D(index);
            Vector3 pos = new(
                (cellId.x + 0.5f) / _dimensions.x, 
                (cellId.y + 0.5f) / _dimensions.y, 
                (cellId.z + 0.5f) / _dimensions.z);
            pos = (pos * 2f - Vector3.one);
            pos.x *= sim.SimulationSpace.x;
            pos.y *= sim.SimulationSpace.y;
            pos.z *= sim.SimulationSpace.z;

            return pos;
        }

        private void OnDrawGizmosSelected()
        {
            if (ParticleCountArray == null)
                return;
            
            Gizmos.color = Color.magenta;

            for (int i = 0; i < _cellCount; i++)
            {
                int count = ParticleCountArray[i];
                
                if (count == 0)
                    continue;
                
                Gizmos.DrawCube(GetPosition(i), Vector3.one * 0.1f * count);
            }
        }

        public static class PropertyIDs
        {
            public static readonly int TotalCount = Shader.PropertyToID("_TotalCount");
            public static readonly int HashCellSize = Shader.PropertyToID("_HashCellSize");
            public static readonly int SimulationSpace = Shader.PropertyToID("_SimulationSpace");
            public static readonly int Time = Shader.PropertyToID("_Time");
            public static readonly int DeltaTime = Shader.PropertyToID("_DeltaTime");
            
            public static readonly int PositionBuffer = Shader.PropertyToID("_PositionBuffer");
            public static readonly int VelocityBuffer = Shader.PropertyToID("_VelocityBuffer");
            public static readonly int DataBuffer = Shader.PropertyToID("_DataBuffer");
            public static readonly int CountBuffer = Shader.PropertyToID("_CountBuffer");
            
            
            public static readonly int ParticlePositionBuffer = Shader.PropertyToID("_ParticlePositionBuffer");
            public static readonly int ParticleOldPositionBuffer = Shader.PropertyToID("_ParticleOldPositionBuffer");
            public static readonly int ParticleDataBuffer = Shader.PropertyToID("_ParticleDataBuffer");
            public static readonly int ParticleCount = Shader.PropertyToID("_ParticleCount");
            
            public static readonly int SpatialIndices = Shader.PropertyToID("_SpatialIndices");
            public static readonly int SpatialOffsets = Shader.PropertyToID("_SpatialOffsets");
        }
    }
}
