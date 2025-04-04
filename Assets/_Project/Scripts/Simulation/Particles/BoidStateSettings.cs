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

        private string _cachedPrefix;

        private int _weightPropertyId;
        private int _radiusPropertyId;
        private int _speedPropertyId; 
        
        public Vector4 Weight => new(separation, alignment, cohesion, detection);
        public Vector4 Radius => new(separationRadius, alignmentRadius, cohesionRadius, detectionRadius);
        public Vector4 Speed => new(minSpeed * maxSpeed, maxSpeed, maxForce, 0);

        public void SetComputeShaderProperties(ComputeShader cs, string prefix)
        {
            if (prefix != _cachedPrefix)
            {
                _cachedPrefix = prefix;
                _weightPropertyId = Shader.PropertyToID($"{prefix}StateWeight");
                _radiusPropertyId = Shader.PropertyToID($"{prefix}StateRadius");
                _speedPropertyId = Shader.PropertyToID($"{prefix}StateSpeed");
            }
            cs.SetVector(_weightPropertyId, Weight);
            cs.SetVector(_radiusPropertyId, Radius);
            cs.SetVector(_speedPropertyId, Speed);
        }
    }

    public static class BoidStateSettingsExtensionMethods
    {
        public static void SetBoidStateSettings(this ComputeShader cs, string name, BoidStateSettings settings)
        {
            if (settings == false)
                return;
            
            settings.SetComputeShaderProperties(cs, name);
        }
    }
}
