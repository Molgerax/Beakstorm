using Beakstorm.ComputeHelpers;
using Beakstorm.Utility;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;

namespace Beakstorm.Simulation.Particles
{
    public class ParticleCellAverage : MonoBehaviour
    {
        [SerializeField] private ComputeShader compute;
        [SerializeField] private Vector3Int capturingArea;
        private IGridParticleSimulation sim;

        
        private GraphicsBuffer _cellBuffer;

        private Vector3Int _dimensions;
        private int _cellCount;
        
        private int[] _dimensionsArray = new int[3]; 
        
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
            sim = GetComponent<IGridParticleSimulation>();
            if (sim == null)
                return;

            _dimensions = Vector3Int.CeilToInt(sim.SimulationSize / sim.CellSize);
            _dimensions = capturingArea;
            _cellCount = _dimensions.x * _dimensions.y * _dimensions.z;

            _dimensionsArray[0] = _dimensions.x;
            _dimensionsArray[1] = _dimensions.y;
            _dimensionsArray[2] = _dimensions.z;
   
            _cellBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, _cellCount, sizeof(float) * 12);


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

            compute.SetInts(PropertyIDs.Dimensions, _dimensionsArray);

            compute.SetFloat(PropertyIDs.Time, Time.time);
            compute.SetFloat(PropertyIDs.DeltaTime, SimulationTime.DeltaTime);

            compute.SetBuffer(kernel, PropertyIDs.GridOffsetBuffer, sim.GridOffsetsBuffer);
            compute.SetBuffer(kernel, PropertyIDs.ParticleBuffer, sim.AgentBufferRead);
            
            compute.SetFloat(PropertyIDs.HashCellSize, sim.CellSize);
            compute.SetInt(PropertyIDs.AgentCount, sim.AgentCount);
            compute.SetVector(PropertyIDs.SimulationSize, sim.SimulationSize);
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
            Bounds bounds = new Bounds(Vector3.zero, sim.SimulationSize);
            return bounds.Contains(pos);
        }
        
        private Vector3Int GetIndex3D(Vector3 pos)
        {
            Vector3Int cellId = new(
                Mathf.FloorToInt((pos.x / sim.SimulationSize.x + 0.5f) * _dimensions.x), 
                Mathf.FloorToInt((pos.y / sim.SimulationSize.y + 0.5f) * _dimensions.y), 
                Mathf.FloorToInt((pos.z / sim.SimulationSize.z + 0.5f) * _dimensions.z));
            
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
            pos.x *= sim.SimulationSize.x;
            pos.y *= sim.SimulationSize.y;
            pos.z *= sim.SimulationSize.z;

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

        private static class PropertyIDs
        {
            public static readonly int TotalCount = Shader.PropertyToID("_TotalCount");
            public static readonly int Dimensions = Shader.PropertyToID("_Dimensions");
            public static readonly int HashCellSize = Shader.PropertyToID("_HashCellSize");
            public static readonly int SimulationCenter = Shader.PropertyToID("_SimulationCenter");
            public static readonly int SimulationSize = Shader.PropertyToID("_SimulationSize");
            public static readonly int Time = Shader.PropertyToID("_Time");
            public static readonly int DeltaTime = Shader.PropertyToID("_DeltaTime");
            
            public static readonly int CellBuffer = Shader.PropertyToID("_CellBuffer");
            
            
            public static readonly int ParticleBuffer = Shader.PropertyToID("_ParticleBuffer");
            public static readonly int AgentCount = Shader.PropertyToID("_AgentCount");
            
            public static readonly int GridOffsetBuffer = Shader.PropertyToID("_GridOffsetBuffer");
        }
    }
}
