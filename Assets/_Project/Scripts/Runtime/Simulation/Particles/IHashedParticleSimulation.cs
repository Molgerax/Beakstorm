using UnityEngine;

namespace Beakstorm.Simulation.Particles
{
    public interface IHashedParticleSimulation
    {
        public bool Initialized { get; }
        
        public GraphicsBuffer SpatialIndicesBuffer {get;}
        public GraphicsBuffer SpatialOffsetsBuffer {get;}
        public GraphicsBuffer PositionBuffer {get;}
        public GraphicsBuffer OldPositionBuffer {get;}
        public GraphicsBuffer DataBuffer {get;}
        public int Capacity {get;}
        public float HashCellSize {get;}
        
        public Vector3 SimulationCenter { get; }
        public Vector3 SimulationSpace { get; }
    }
}
