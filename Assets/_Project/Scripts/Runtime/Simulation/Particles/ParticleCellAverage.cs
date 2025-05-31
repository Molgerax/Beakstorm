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

        
        private GraphicsBuffer _cellBuffer;

        private Vector3Int _dimensions;
        private int _cellCount;
        
        
        private AsyncGPUReadbackRequest _request;
        private NativeArray<ParticleCell> _cellArray;
        public ParticleCell[] CellArray; 

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
   
            _cellBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, _cellCount, sizeof(float) * 10 + sizeof(uint) * 1);


            _cellArray = new NativeArray<ParticleCell>(_cellCount, Allocator.Persistent);
            _cellBuffer.SetData(_cellArray);
            CellArray = new ParticleCell[_cellCount];
        }

        private void Release()
        {
            _cellBuffer?.Dispose();
            
            _request.WaitForCompletion();
            _cellArray.Dispose();
        }
        
        private void RequestGpuData()
        {
            if (CellArray == null)
                return;
            
            if (_request.done)
            {
                if (!_request.hasError)
                {
                    _cellArray.CopyTo(CellArray);
                }
                _request = AsyncGPUReadback.RequestIntoNativeArray(ref _cellArray, _cellBuffer);
            }
        }

        private void CollectParticleValues()
        {
            if (!compute || sim == null)
                return;
            
            int kernel = compute.FindKernel("CollectValues");
            
            compute.SetBuffer(kernel, PropertyIDs.CellBuffer, _cellBuffer);
            
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
            compute.SetVector(PropertyIDs.SimulationCenter, sim.SimulationCenter);
            
            compute.DispatchExact(kernel, _cellCount);
        }

        private Vector3Int Index1DTo3D(int index)
        {
            Vector3Int cellId = Vector3Int.zero;
            cellId.z = index / (_dimensions.x * _dimensions.y);
            cellId.y = index / _dimensions.x - cellId.z * _dimensions.y;
            cellId.x = index - cellId.y * _dimensions.x - cellId.z * _dimensions.x * _dimensions.y;
            
            return cellId;
        }

        private bool IsIndexInDimensions(Vector3Int cellId)
        {
            if (cellId.x < 0 || cellId.y < 0 || cellId.z < 0)
                return false;
            if (cellId.x >= _dimensions.x || cellId.y >= _dimensions.y || cellId.z >= _dimensions.z)
                return false;
            return true;
        }
        
        private int Index3DTo1D(Vector3Int cellId)
        {
            if (!IsIndexInDimensions(cellId))
                return -1;
            
            int index = 
                + cellId.x
                + cellId.y * _dimensions.x
                + cellId.z * _dimensions.x * _dimensions.y;
            
            return index;
        }

        private bool IsPositionInBounds(Vector3 pos)
        {
            Bounds bounds = new Bounds(Vector3.zero, sim.SimulationSpace);
            return bounds.Contains(pos);
        }
        
        private Vector3Int GetIndex3D(Vector3 pos)
        {
            Vector3Int cellId = new(
                Mathf.FloorToInt((pos.x / sim.SimulationSpace.x + 0.5f) * _dimensions.x), 
                Mathf.FloorToInt((pos.y / sim.SimulationSpace.y + 0.5f) * _dimensions.y), 
                Mathf.FloorToInt((pos.z / sim.SimulationSpace.z + 0.5f) * _dimensions.z));
            
            return cellId;
        }

        private int GetIndex1D(Vector3 pos) => Index3DTo1D(GetIndex3D(pos));
        
        private Vector3 GetPosition(int index)
        {
            Vector3Int cellId = Index1DTo3D(index);
            Vector3 pos = new(
                (cellId.x + 0.5f) / _dimensions.x, 
                (cellId.y + 0.5f) / _dimensions.y, 
                (cellId.z + 0.5f) / _dimensions.z);
            pos = (pos - Vector3.one * 0.5f);
            pos.x *= sim.SimulationSpace.x;
            pos.y *= sim.SimulationSpace.y;
            pos.z *= sim.SimulationSpace.z;

            return pos;
        }

        public bool GetCellData(Vector3 pos, out ParticleCell cellData)
        {
            cellData = new ParticleCell();

            if (CellArray == null)
                return false;
            
            if (!IsPositionInBounds(pos))
                return false;

            int index = GetIndex1D(pos);
            if (index < 0 || index >= _cellCount)
                return false;

            cellData = CellArray[index];
            return true;
        }
        
        private void OnDrawGizmosSelected()
        {
            if (CellArray == null)
                return;
            
            Gizmos.color = Color.magenta;

            for (int i = 0; i < _cellCount; i++)
            {
                ParticleCell cell = CellArray[i];
                
                if (cell.Count == 0)
                    continue;

                Vector3 pos = cell.Position;
                Vector3 vel = cell.Velocity;
                
                Gizmos.DrawSphere(pos, 1f);
                Gizmos.DrawLine(pos, pos + vel);
            }
        }
        
        public struct ParticleCell
        {
            public Vector3 Position;
            public Vector3 Velocity;
            public Vector4 Data;
            public uint Count;
        };

        public static class PropertyIDs
        {
            public static readonly int TotalCount = Shader.PropertyToID("_TotalCount");
            public static readonly int HashCellSize = Shader.PropertyToID("_HashCellSize");
            public static readonly int SimulationCenter = Shader.PropertyToID("_SimulationCenter");
            public static readonly int SimulationSpace = Shader.PropertyToID("_SimulationSpace");
            public static readonly int Time = Shader.PropertyToID("_Time");
            public static readonly int DeltaTime = Shader.PropertyToID("_DeltaTime");
            
            public static readonly int CellBuffer = Shader.PropertyToID("_CellBuffer");
            
            
            public static readonly int ParticlePositionBuffer = Shader.PropertyToID("_ParticlePositionBuffer");
            public static readonly int ParticleOldPositionBuffer = Shader.PropertyToID("_ParticleOldPositionBuffer");
            public static readonly int ParticleDataBuffer = Shader.PropertyToID("_ParticleDataBuffer");
            public static readonly int ParticleCount = Shader.PropertyToID("_ParticleCount");
            
            public static readonly int SpatialIndices = Shader.PropertyToID("_SpatialIndices");
            public static readonly int SpatialOffsets = Shader.PropertyToID("_SpatialOffsets");
        }
    }
}
