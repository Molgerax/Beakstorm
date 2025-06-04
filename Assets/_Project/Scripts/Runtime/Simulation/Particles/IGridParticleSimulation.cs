using UnityEngine;

namespace Beakstorm.Simulation.Particles
{
    public interface IGridParticleSimulation
    {
        public bool Initialized { get; }
        
        public GraphicsBuffer AgentBufferRead {get;}
        public GraphicsBuffer AgentBufferWrite {get;}
        public int AgentBufferStride { get; }
        
        public GraphicsBuffer SpatialIndicesBuffer {get;}
        public GraphicsBuffer SpatialOffsetsBuffer {get;}
        public GraphicsBuffer PositionBuffer {get;}
        public GraphicsBuffer OldPositionBuffer {get;}
        public GraphicsBuffer DataBuffer {get;}
        public int AgentCount {get;}
        public float CellSize {get;}
        
        public Vector3 SimulationCenter { get; }
        public Vector3 SimulationSize { get; }
    }
}
