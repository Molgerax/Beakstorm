using Beakstorm.Pausing;
using UnityEngine;

namespace Beakstorm.Rendering.ExplosionLights
{
    [RequireComponent(typeof(Light))]
    public class ExplosionLight : MonoBehaviour
    {
        [SerializeField, HideInInspector] private new Light light;
        [SerializeField, Range(0, 1)] private float dimFactor = 0.1f;

        [SerializeField, Range(0, 10)] private float jiggle = 1f;
        
        
        [SerializeField, Min(0.01f)] private float lifeTime = 0.5f;
        
        private float _initialIntensity;
        private float _currentStrength;

        private float _timer;

        private Vector3 _initialPosition;
        private bool _initialized;
        
        public float DimFactor => 1f - dimFactor * _currentStrength;

        public void SetStrength(float t01)
        {
            _currentStrength = Mathf.Clamp01(t01);
            light.intensity = _initialIntensity * _currentStrength;
        }

        public void Initialize()
        {
            _initialPosition = transform.position;
            _initialized = true;
        }
        
        private void Update()
        {
            if (PauseManager.IsPaused)
                return;
            
            if (!_initialized)
                return;
            
            _timer -= Time.deltaTime;
            SetStrength(_timer / lifeTime);
            
            transform.position = _initialPosition + Random.insideUnitSphere * jiggle;
        }

        private void Reset()
        {
            light = GetComponent<Light>();
        }

        private void OnEnable()
        {
            if (!light)
                light = GetComponent<Light>();
            
            _initialIntensity = light.intensity;
            LightDimController.ExplosionLights.Add(this);

            _timer = lifeTime;
            _initialized = false;
        }

        private void OnDisable()
        {
            LightDimController.ExplosionLights.Remove(this);
            light.intensity = _initialIntensity;
            _initialized = false;
        }
    }
}
