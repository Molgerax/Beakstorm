using UnityEngine;

namespace Beakstorm.Simulation.Particles
{
    [CreateAssetMenu(fileName = "BoidState", menuName = "Beakstorm/Boid State Settings")]
    public class BoidStateSettings : ScriptableObject
    {
        [SerializeField, Range(0, 1)] private float separation;
        [SerializeField, Range(0, 1)] private float alignment;
        [SerializeField, Range(0, 1)] private float cohesion;
        [SerializeField, Range(0, 1)] private float detection;

        public Vector4 ToVector => new(separation, alignment, cohesion, detection);

        public static implicit operator Vector4(BoidStateSettings b) => b == null ? Vector4.zero : b.ToVector;
    }
}
