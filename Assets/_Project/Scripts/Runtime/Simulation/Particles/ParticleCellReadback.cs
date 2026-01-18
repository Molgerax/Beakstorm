using System.Collections.Generic;
using Beakstorm.ComputeHelpers;
using Beakstorm.Core.Interfaces;
using Beakstorm.Pausing;
using Beakstorm.Utility;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

namespace Beakstorm.Simulation.Particles
{
    public class ParticleCellReadback : MonoBehaviour
    {
        public static ParticleCellReadback Instance;
        
        [SerializeField] private ComputeShader compute;
        [SerializeField] private int maxCellCount = 16;

        [SerializeField, Range(1, 4)] private int cellsPerCapture = 1;
        [SerializeField] private InterfaceReference<IGridParticleSimulation> sim;

        [SerializeField] private int lifeCount = 20;
        
        private IGridParticleSimulation _sim => sim?.Value;

        private GraphicsBuffer _cellBuffer;
        private GraphicsBuffer _cellRequestBuffer;

        private int _cellCount;
        
        private AsyncGPUReadbackRequest _request;
        private NativeArray<ParticleCell> _cellArray;
        
        public ParticleCell[] CellArray;


        private AutoFilledArray<CellRequest> _cellRequests;
        private Dictionary<Vector3Int, CellRequest> _cellRequestLookup = new();

        private CellRequestStruct[] _cellRequestArray;
        
        private bool _initialized;
        public bool Initialized => _initialized;

        public int CellCount => _cellRequests?.IterateCount ?? _cellCount;
        
        
        #region Adding And Removing Requests

        private bool TryAddCellRequest(Vector3Int globalId)
        {
            if (!_cellRequestLookup.TryGetValue(globalId, out var request))
            {
                if (_cellRequests.NeedsResize)
                    return false;
                
                var cellRequest = new CellRequest(globalId, lifeCount);
                _cellRequestLookup.Add(globalId, cellRequest);
                _cellRequests.AddElement(cellRequest);
                return true;
            }

            request.LifeSteps = lifeCount;
            
            return false;
        }
        
        public bool TryAddCellRequest(Vector3 position)
        {
            if (_sim?.Hash == null)
                return false;
            
            Vector3Int globalId = PositionToGlobalCellId(position);
            return TryAddCellRequest(globalId);
        }

        
        private void RemoveCellRequest(Vector3Int globalId)
        {
            if (_cellRequestLookup.TryGetValue(globalId, out var cellRequest))
            {
                _cellRequests.RemoveElement(cellRequest);
                _cellRequestLookup.Remove(globalId);
            }
        }
        
        public void RemoveCellRequest(Vector3 position)
        {
            if (_sim?.Hash == null)
                return;
            
            Vector3Int globalId = PositionToGlobalCellId(position); 
            RemoveCellRequest(globalId);
        }

        private Vector3Int PositionToGlobalCellId(Vector3 position)
        {
            return _sim.Hash.GetGlobalGridCellId(position / cellsPerCapture);
        }

        public bool TryGetCellData(int i, out ParticleCell cell)
        {
            cell = default;
            
            if (i < 0 || i > Mathf.Min(_cellRequests.Count, CellArray.Length))
                return false;
            
            cell = CellArray[i];
            return true;
        }

        public bool TryGetCellData(Vector3 position, out ParticleCell cell)
        {
            cell = default;
            
            var id = PositionToGlobalCellId(position);
            if (_cellRequestLookup.TryGetValue(id, out var request))
            {
                if (_cellRequests.TryGetIndex(request, out int index) && request.Status == CellRequestStatus.Finished)
                {
                    cell = CellArray[index];
                    return true;
                }
            }

            return false;
        }
        
        #endregion
        
        

        private void Initialize()
        {
            if (_sim == null || _sim.Initialized == false)
                return;

            _cellCount = maxCellCount;
            _cellRequests = new AutoFilledArray<CellRequest>(_cellCount, true);
            _cellRequestArray = new CellRequestStruct[_cellCount];
            _cellRequestBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, _cellCount, sizeof(float) * 4);
            
            _cellBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, _cellCount, sizeof(float) * 12);
            _cellArray = new NativeArray<ParticleCell>(_cellCount, Allocator.Persistent);
            _cellBuffer.SetData(_cellArray);
            CellArray = new ParticleCell[_cellCount];
            
            _initialized = true;
        }

        private void Release()
        {
            _cellBuffer?.Release();
            _cellRequestBuffer?.Release();
            
            _request.WaitForCompletion();
            _cellArray.Dispose();
        }

        private void UpdatePositionBuffer()
        {
            for (int i = 0; i < _cellRequests.IterateCount; i++)
            {
                CellRequest request = _cellRequests[i];
                CellRequestStruct requestStruct = default;

                if (request != null)
                    requestStruct = new(request.GlobalCellId);

                _cellRequestArray[i] = requestStruct;
            }
            _cellRequestBuffer.SetData(_cellRequestArray);
        }


        private void IterateOnAllRequests()
        {
            bool freed = false;
            for (int i = 0; i < _cellRequests.IterateCount; i++)
            {
                var cellRequest = _cellRequests[i];
                if (cellRequest == null)
                    continue;
                
                if (cellRequest.Status == CellRequestStatus.Finished && (cellRequest.LifeSteps <= 0 || (!freed && _cellRequests.NeedsResize)))
                {
                    RemoveCellRequest(cellRequest.GlobalCellId);
                    freed = true;
                }
            }
        }
        
        
        private void Awake()
        {
            Instance = this;
            Initialize();
        }

        private void OnDestroy()
        {
            Instance = null;
            Release();
        }

        private void Update()
        {
            if (!_initialized)
                Initialize();
            if (!_initialized)
                return;
            
            IterateOnAllRequests();
            _cellRequests.UpdateArray();
            UpdatePositionBuffer();
            
            RequestGpuData();
        }

        
        private void RequestGpuData()
        {
            if (CellArray == null)
                return;
            
            if (PauseManager.IsPaused)
                return;
            
            if (_request.done)
            {
                if (!_request.hasError)
                {
                    _cellArray.CopyTo(CellArray);
                    
                    int count = _cellRequests.IterateCount;
                    for (int i = 0; i < count; i++)
                    {
                        var cellRequest = _cellRequests[i];
                        if (cellRequest == null)
                            continue;
                        
                        if (cellRequest.Status == CellRequestStatus.InProgress || cellRequest.Status == CellRequestStatus.Finished)
                        {
                            cellRequest.Status = CellRequestStatus.Finished;
                            cellRequest.LifeSteps -= 1;
                        }

                        if (cellRequest.Status == CellRequestStatus.Inactive)
                        {
                            cellRequest.Status = CellRequestStatus.InProgress;
                        }
                    }
                }
                CollectParticleValues();
                _request = AsyncGPUReadback.RequestIntoNativeArray(ref _cellArray, _cellBuffer);
            }
        }

        private void CollectParticleValues()
        {
            if (!compute || _sim == null)
                return;
            
            int kernel = compute.FindKernel("CollectValues");

            compute.SetBuffer(kernel, PropertyIDs.CellBuffer, _cellBuffer);
            
            compute.SetBuffer(kernel, PropertyIDs.CellRequestBuffer, _cellRequestBuffer);
            compute.SetInt(PropertyIDs.CaptureCellCount, _cellRequests.IterateCount);
            compute.SetInt(PropertyIDs.CaptureCellSize, cellsPerCapture);

            compute.SetFloat(PropertyIDs.Time, Time.time);
            compute.SetFloat(PropertyIDs.DeltaTime, SimulationTime.DeltaTime);

            _sim.Hash.SetShaderProperties(compute);
            
            compute.SetBuffer(kernel, PropertyIDs.GridOffsetBuffer, _sim.GridOffsetsBuffer);
            compute.SetBuffer(kernel, PropertyIDs.ParticleBuffer, _sim.AgentBufferRead);

            compute.DispatchExact(kernel, _cellCount);
        }

        

        #region Gizmos

        private void OnDrawGizmosSelected()
        {
            if (_sim?.Hash == null)
                return;

            if (_cellRequests == null)
                return;

            for (int i = 0; i < _cellRequests.Size; i++)
            {
                var cellRequest = _cellRequests[i];
                if (cellRequest == null)
                    continue;

                Color col = StatusToColor(cellRequest.Status);
                DrawGizmoGridCube(cellRequest.GlobalCellId, col);
            }

            for (int i = 0; i < _cellRequests.IterateCount; i++)
            {
                var cell = CellArray[i];
                if (cell.Count == 0)
                    continue;

                Vector3 pos = cell.Position;
                Vector3 vel = cell.Velocity;
                
                Gizmos.DrawSphere(pos, 1f);
                Gizmos.DrawLine(pos, pos + vel);
            }
        }

        
        private void DrawGizmoGridCube(Vector3Int globalCellId)
        {
            DrawGizmoGridCube(globalCellId, _sim.Hash.IsGlobalCellInsideBounds((int3) (float3) (Vector3) globalCellId) ? Color.green : Color.red);
        }

        private void DrawGizmoGridCube(Vector3Int globalCellId, Color color)
        {
            Vector3 posCenter = (globalCellId + Vector3.one * 0.5f) * _sim.CellSize * cellsPerCapture;
            Vector3 size = Vector3.one * _sim.CellSize * cellsPerCapture;
            Gizmos.color = color;
            Gizmos.DrawWireCube(posCenter, size);
            color.a *= 0.5f;
            Gizmos.color = color;
            Gizmos.DrawCube(posCenter, size);
        }
        

        #endregion


        private static class PropertyIDs
        {
            public static readonly int CellBuffer = Shader.PropertyToID("_CellBuffer");
            public static readonly int CellRequestBuffer = Shader.PropertyToID("_CellRequestBuffer");
            
            public static readonly int CaptureCellCount = Shader.PropertyToID("_CaptureCellCount");
            public static readonly int CaptureCenter = Shader.PropertyToID("_CaptureCenter");
            public static readonly int CaptureDimensions = Shader.PropertyToID("_CaptureDimensions");
            public static readonly int CaptureCellSize = Shader.PropertyToID("_CaptureCellSize");
            
            public static readonly int Time = Shader.PropertyToID("_Time");
            public static readonly int DeltaTime = Shader.PropertyToID("_DeltaTime");
            

            public static readonly int ParticleBuffer = Shader.PropertyToID("_ParticleBuffer");
            public static readonly int GridOffsetBuffer = Shader.PropertyToID("_GridOffsetBuffer");
        }

        private class CellRequest
        {
            public readonly Vector3Int GlobalCellId;
            public CellRequestStatus Status;
            public int LifeSteps = 0;

            public CellRequest(Vector3Int globalCellId, int lifeSteps = 20)
            {
                GlobalCellId = globalCellId;
                Status = CellRequestStatus.Inactive;
                LifeSteps = lifeSteps;
            }
            
            public CellRequest(Vector3 position, IGridParticleSimulation sim)
            {
                GlobalCellId = sim.Hash.GetGlobalGridCellId(position);
                Status = CellRequestStatus.Inactive;
            }
        }

        private struct CellRequestStruct
        {
            public Vector3Int GlobalCellId;
            public int IsAlive;

            public CellRequestStruct(Vector3Int id)
            {
                GlobalCellId = id;
                IsAlive = 1;
            }
        }

        enum CellRequestStatus
        {
            Inactive = 0,
            InProgress = 1,
            Finished = 2,
        }

        private Color StatusToColor(CellRequestStatus status)
        {
            switch (status)
            {
                case CellRequestStatus.Inactive:
                    return Color.red;
                case CellRequestStatus.InProgress:
                    return Color.yellow;
                case CellRequestStatus.Finished:
                    return Color.green;
                default:
                    return Color.black;
            }
        }
    }
    
    public struct ParticleCell
    {
        public Vector3 Position;
        public uint Count;
        public Vector3 Velocity;
        public uint Padding;
        public Vector4 Data;
    };
    
    public struct ParticleCellInterpolated
    {
        public Vector3 Position;
        public float Count;
        public Vector3 Velocity;
        public Vector4 Data;

        public ParticleCellInterpolated(ParticleCell cell)
        {
            Position = cell.Position;
            Count = cell.Count;
            Velocity = cell.Velocity;
            Data = cell.Data;
        }
    };
}