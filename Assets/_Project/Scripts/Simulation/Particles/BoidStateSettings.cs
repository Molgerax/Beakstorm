using UnityEngine;

namespace Beakstorm.Simulation.Particles
{
    [CreateAssetMenu(fileName = "BoidState", menuName = "Beakstorm/Boid State Settings")]
    public class BoidStateSettings : ScriptableObject
    {
        [Header("Weights")]
        [SerializeField, Range(0, 1)] private float separation = 0.5f;
        [SerializeField, Range(0, 1)] private float alignment = 0.5f;
        [SerializeField, Range(0, 1)] private float cohesion = 0.5f;
        [SerializeField, Range(0, 1)] private float detection = 0.5f;

        [Header("Radius")]
        [SerializeField, Range(0, 10)] private float separationRadius = 1;
        [SerializeField, Range(0, 10)] private float alignmentRadius = 1;
        [SerializeField, Range(0, 10)] private float cohesionRadius = 1;
        [SerializeField, Range(0, 10)] private float detectionRadius = 1;

        [Header("Speed")]
        [SerializeField, Range(0, 1)] private float minSpeed = 0;
        [SerializeField] private float maxSpeed = 10;
        [SerializeField] private float maxForce = 10;
        //[SerializeField, Range(0, 1)] private float minForce;

        public Vector4 Weight => new(separation, alignment, cohesion, detection);
        public Vector4 Radius => new(separationRadius, alignmentRadius, cohesionRadius, detectionRadius);
        public Vector4 Speed => new(minSpeed * maxSpeed, maxSpeed, maxForce, 0);
    }

    public static class BoidStateSettingsExtensionMethods
    {
        public static void SetBoidStateSettings(this ComputeShader cs, string name, BoidStateSettings settings)
        {
            bool isNull = settings == false;
            
            cs.SetVector($"{name}StateWeight", isNull ? Vector4.zero : settings.Weight);
            cs.SetVector($"{name}StateRadius", isNull ? Vector4.zero : settings.Radius);
            cs.SetVector($"{name}StateSpeed", isNull ? Vector4.zero : settings.Speed);
        }
    }
}
