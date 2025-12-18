using System;
using Beakstorm.Simulation.Particles;
using Beakstorm.Simulation.Settings;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

namespace Beakstorm.ComputeHelpers
{
    public class SpatialGrid : IDisposable
    {
        private readonly ComputeShader _cs;
        private readonly IGridParticleSimulation _simulation;
        
        private readonly int _agentCount;
        private readonly int _agentStride;
        private readonly float _cellSize;
        
        private Vector3 _center;
        private Vector3 _size;

        private readonly Vector3Int _cellDimensions;
        private readonly int _cellCount;

        private int[] _dimensionsArray;

        public int[] Dimensions => _dimensionsArray;
        public int CellCount => _cellCount;

        public Vector3 Center => _snappedCenter;
        public Vector3 Size => _size;

        private readonly uint _threadGroupSize;
        private readonly int _threadGroupCount;
        
        public GraphicsBuffer GridBuffer;
        public GraphicsBuffer GridOffsetBuffer;
        public GraphicsBuffer GridSumsBuffer;
        
        public GraphicsBuffer GridOffsetBufferRead;
        public GraphicsBuffer GridSumsBufferRead;

        private GraphicsBuffer _aliveCountBuffer;
        private readonly bool _useAliveCount;

        private LocalKeyword _aliveCountKeyword;

        private Vector3 _snappedCenter;

        public SpatialGrid(ComputeShader cs, IGridParticleSimulation simulation, GraphicsBuffer aliveCountBuffer = null)
        {
            _cs = cs;
            _simulation = simulation;
            _agentCount = simulation.AgentCount;
            _agentStride = simulation.AgentBufferStride;
            _cellSize = simulation.CellSize;

            _aliveCountBuffer = aliveCountBuffer;
            _useAliveCount = _aliveCountBuffer != null;

            _aliveCountKeyword = new LocalKeyword(_cs, "ALIVE_COUNT");

            _center = simulation.SimulationCenter;
            _size = simulation.SimulationSize;

            _snappedCenter = _center;

            _cellDimensions = Vector3Int.CeilToInt(_size / _cellSize);
            _cellCount = _cellDimensions.x * _cellDimensions.y * _cellDimensions.z;
            
            int3 gridId = GetGridCellId(_center);
            float3 pos = GetPosFromGridCell(gridId);
            
            Debug.Log($"Center: {_center}, id: {gridId}, Pos: {pos}");
            
            _dimensionsArray = new[] {_cellDimensions.x, _cellDimensions.y, _cellDimensions.z};

            _cs.GetKernelThreadGroupSizes(0, out _threadGroupSize, out _, out _);
            _threadGroupCount = Mathf.CeilToInt((float)_cellCount / _threadGroupSize);

            GridBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, _agentCount, 2 * sizeof(int));
            GridOffsetBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, _cellCount, 1 * sizeof(int));
            GridSumsBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, _threadGroupCount, 1 * sizeof(int));

            GridOffsetBufferRead = new GraphicsBuffer(GraphicsBuffer.Target.Structured, _cellCount, 1 * sizeof(int));
            GridSumsBufferRead = new GraphicsBuffer(GraphicsBuffer.Target.Structured, _threadGroupCount, 1 * sizeof(int));
        }

        public void Dispose()
        {
            GridBuffer?.Dispose();
            GridOffsetBuffer?.Dispose();
            GridSumsBuffer?.Dispose();
            
            GridOffsetBufferRead?.Dispose();
            GridSumsBufferRead?.Dispose();
        }
        

        public void Update()
        {
            CalculateSnappedCenter();

            ClearGridOffsets();
            UpdateGrid();
            PrefixSum();
            bool swapBuffers = SumThreadGroups();
            AddSums(swapBuffers);
            ReorderBoids();
        }

        private void CalculateSnappedCenter()
        {
            int3 gridId = GetGridCellId(_center);
            float3 offset = (float3)_center - GetPosFromGridCell(gridId);

            Vector3 newPos = ParticleSimulationCenter.Instance
                ? ParticleSimulationCenter.Instance.transform.position
                : _simulation.SimulationCenter;
            
            var border = ParticleSimulationBorders.Instance;
            if (border)
            {
                newPos = border.SnapCenterToBounds(newPos, _size);
            }

            newPos.y = _center.y;
            
            gridId = GetGridCellId(newPos);
            _snappedCenter = offset + GetPosFromGridCell(gridId);
        }
        
        public void SetShaderProperties(ComputeShader cs)
        {
            cs.SetInt(PropertyIDs.AgentCount, _agentCount);
            cs.SetInt(PropertyIDs.CellCount, _cellCount);
            cs.SetInts(PropertyIDs.CellDimensions, _dimensionsArray);
            cs.SetFloat(PropertyIDs.HashCellSize, _cellSize);
            cs.SetVector(PropertyIDs.SimulationCenter, _snappedCenter);
            cs.SetVector(PropertyIDs.SimulationSize, _size);
        }
        
        public void SetShaderProperties(Material mat)
        {
            mat.SetInt(PropertyIDs.AgentCount, _agentCount);
            mat.SetInt(PropertyIDs.CellCount, _cellCount);
            mat.SetVector(PropertyIDs.CellDimensions, new Vector3(_cellDimensions.x, _cellDimensions.y, _cellDimensions.z));
            mat.SetFloat(PropertyIDs.HashCellSize, _cellSize);
            mat.SetVector(PropertyIDs.SimulationCenter, _snappedCenter);
            mat.SetVector(PropertyIDs.SimulationSize, _size);
        }
        
        public void SetShaderProperties(MaterialPropertyBlock propBlock)
        {
            propBlock.SetInt(PropertyIDs.AgentCount, _agentCount);
            propBlock.SetInt(PropertyIDs.CellCount, _cellCount);
            propBlock.SetVector(PropertyIDs.CellDimensions, new Vector3(_cellDimensions.x, _cellDimensions.y, _cellDimensions.z));
            propBlock.SetFloat(PropertyIDs.HashCellSize, _cellSize);
            propBlock.SetVector(PropertyIDs.SimulationCenter, _snappedCenter);
            propBlock.SetVector(PropertyIDs.SimulationSize, _size);
        }
        
        private void UpdateGrid()
        {
            int kernelId = _cs.FindKernel("UpdateGrid"); 
            
            _cs.SetKeyword(_aliveCountKeyword, _useAliveCount);
            
            _cs.SetInt(PropertyIDs.AgentCount, _agentCount);
            _cs.SetInt(PropertyIDs.AgentStride, _agentStride);
            _cs.SetInt(PropertyIDs.CellCount, _cellCount);
            _cs.SetInts(PropertyIDs.CellDimensions, _dimensionsArray);
            _cs.SetFloat(PropertyIDs.HashCellSize, _cellSize);
            _cs.SetVector(PropertyIDs.SimulationCenter, _snappedCenter);
            _cs.SetVector(PropertyIDs.SimulationSize, _size);
            
            _cs.SetBuffer(kernelId, PropertyIDs.GridBuffer, GridBuffer);
            _cs.SetBuffer(kernelId, PropertyIDs.GridOffsetBuffer, GridOffsetBufferRead);
            _cs.SetBuffer(kernelId, PropertyIDs.AgentBufferRead, _simulation.AgentBufferRead);
            
            if (_useAliveCount)
                _cs.SetBuffer(kernelId, PropertyIDs.AliveCountBuffer, _aliveCountBuffer);

            _cs.DispatchExact(kernelId, _agentCount);
        }

        private void ClearGridOffsets()
        {
            int kernelId = _cs.FindKernel("ClearGridOffsets"); 
            
            _cs.SetInt(PropertyIDs.CellCount, _cellCount);
            _cs.SetBuffer(kernelId, PropertyIDs.GridOffsetBuffer, GridOffsetBufferRead);
            
            _cs.DispatchExact(kernelId, _cellCount);
        }

        private void PrefixSum()
        {
            int kernelId = _cs.FindKernel("PrefixSum");

            _cs.SetInt(PropertyIDs.AgentCount, _agentCount);
            _cs.SetInt(PropertyIDs.CellCount, _cellCount);
            _cs.SetBuffer(kernelId, PropertyIDs.GridOffsetBuffer, GridOffsetBuffer);
            _cs.SetBuffer(kernelId, PropertyIDs.GridOffsetBufferRead, GridOffsetBufferRead);
            _cs.SetBuffer(kernelId, PropertyIDs.GridSumsBuffer, GridSumsBufferRead);

            _cs.DispatchExact(kernelId, _cellCount);
        }

        
        private bool SumThreadGroups()
        {
            int kernelId = _cs.FindKernel("SumThreadGroups");

            bool swapBuffer = false;
            
            _cs.SetInt(PropertyIDs.ThreadGroupCount, _threadGroupCount);

            for (int i = 1; i < _threadGroupCount; i *= 2)
            {
                _cs.SetInt(PropertyIDs.SumOffset, i);
                
                _cs.SetBuffer(kernelId, PropertyIDs.GridSumsBufferRead, 
                    swapBuffer ? GridSumsBuffer : GridSumsBufferRead);
                _cs.SetBuffer(kernelId, PropertyIDs.GridSumsBuffer, 
                    swapBuffer ? GridSumsBufferRead : GridSumsBuffer);

                _cs.DispatchExact(kernelId, _threadGroupCount);
                
                swapBuffer = !swapBuffer;
            }

            return swapBuffer;
        }
        
        private void AddSums(bool swapBuffers)
        {
            int kernelId = _cs.FindKernel("AddSums");

            _cs.SetInt(PropertyIDs.CellCount, _cellCount);
            _cs.SetBuffer(kernelId, PropertyIDs.GridOffsetBuffer, GridOffsetBuffer);
            _cs.SetBuffer(kernelId, PropertyIDs.GridSumsBufferRead, 
                swapBuffers ? GridSumsBuffer : GridSumsBufferRead);

            _cs.DispatchExact(kernelId, _cellCount);
        }
        
        
        private void ReorderBoids()
        {
            int kernelId = _cs.FindKernel("ReorderBoids");

            _cs.SetKeyword(_aliveCountKeyword, _useAliveCount);
            
            _cs.SetInt(PropertyIDs.AgentCount, _agentCount);
            _cs.SetInt(PropertyIDs.AgentStride, _agentStride);
            _cs.SetBuffer(kernelId, PropertyIDs.GridBuffer, GridBuffer);
            _cs.SetBuffer(kernelId, PropertyIDs.GridOffsetBuffer, GridOffsetBuffer);
            
            _cs.SetBuffer(kernelId, PropertyIDs.AgentBufferRead, _simulation.AgentBufferRead);
            _cs.SetBuffer(kernelId, PropertyIDs.AgentBufferWrite, _simulation.AgentBufferWrite);

            if (_useAliveCount)
                _cs.SetBuffer(kernelId, PropertyIDs.AliveCountBuffer, _aliveCountBuffer);
            
            _cs.DispatchExact(kernelId, _agentCount);
        }

        private float3 GetPosFromGridCell(int3 cellId)
        {
            float3 minBound = _center - _size * 0.5f;
            float3 pos = (float3)cellId * _cellSize;
            pos = pos + minBound;
            return pos;
        }

        private int3 GetGridCellId(float3 pos)
        {
            float3 minBound = _center - _size * 0.5f;
            pos = pos - minBound; 
            return (int3)math.floor( pos / _cellSize);
        }


        private static class PropertyIDs
        {
            public static readonly int AgentCount = Shader.PropertyToID("_AgentCount");
            public static readonly int CellCount = Shader.PropertyToID("_CellCount");
            public static readonly int ThreadGroupCount = Shader.PropertyToID("_ThreadGroupCount");
            public static readonly int CellDimensions = Shader.PropertyToID("_CellDimensions");
            public static readonly int HashCellSize = Shader.PropertyToID("_HashCellSize");
            public static readonly int SimulationSize = Shader.PropertyToID("_SimulationSize");
            public static readonly int SimulationCenter = Shader.PropertyToID("_SimulationCenter");
            
            public static readonly int BoidBuffer = Shader.PropertyToID("_BoidBuffer");
            public static readonly int BoidBufferRead = Shader.PropertyToID("_BoidBufferRead");
            
            
            public static readonly int AgentBufferWrite = Shader.PropertyToID("_AgentBufferWrite");
            public static readonly int AgentBufferRead = Shader.PropertyToID("_AgentBufferRead");
            
            public static readonly int SumOffset = Shader.PropertyToID("_SumOffset");
            
            public static readonly int GridBuffer = Shader.PropertyToID("_GridBuffer");
            public static readonly int GridOffsetBuffer = Shader.PropertyToID("_GridOffsetBuffer");
            public static readonly int GridSumsBuffer = Shader.PropertyToID("_GridSumsBuffer");

            public static readonly int GridOffsetBufferRead = Shader.PropertyToID("_GridOffsetBufferRead");
            public static readonly int GridSumsBufferRead = Shader.PropertyToID("_GridSumsBufferRead");
            
            public static readonly int AliveCountBuffer = Shader.PropertyToID("_AliveCountBuffer");
            
            public static readonly int AgentStride = Shader.PropertyToID("_AgentStride");
        }
    }
}