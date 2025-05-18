using Beakstorm.Pausing;
using UnityEngine;

namespace Beakstorm.Simulation.Particles
{
    public class PheromoneEmitter : MonoBehaviour
    {
        [SerializeField, Min(0)] private float emissionRate = 60;
        [SerializeField, Range(0, 10)] private float lifeTime = 3;

        public float EmissionRate
        {
            get => emissionRate;
            set => emissionRate = value;
        }
        
        public float LifeTime
        {
            get => lifeTime;
            set => lifeTime = value;
        }
        
        private float _initEmissionRate;
        private float _initLifeTime;
        
        private float _remainder = 0;

        private Vector3 _position;
        private Vector3 _oldPosition;

        private float _deltaTime;

        private void Awake()
        {
            _initEmissionRate = emissionRate;
            _initLifeTime = lifeTime;
        }

        private void OnEnable()
        {
            ResetEmitter();
        }

        private void Update()
        {
            if (PauseManager.IsPaused)
                return;
         
            UpdatePositions();
            EmitOverTime();
        }


        private void EmitOverTime()
        {
            float emissionPerFrame = emissionRate * _deltaTime;
            emissionPerFrame += _remainder;
            _remainder = emissionPerFrame % 1;

            int emissionCount = Mathf.FloorToInt(emissionPerFrame);
            Emit(emissionCount);
        }
        
        
        private void UpdatePositions()
        {
            _oldPosition = _position;
            _position = transform.position;
            _deltaTime = Time.deltaTime;
        }

        public void Emit(int count)
        {
            if (PheromoneManager.Instance)
                PheromoneManager.Instance.EmitParticles(count, _position, _oldPosition, lifeTime, _deltaTime);
        }

        public void ResetEmitter()
        {
            _position = transform.position;
            _remainder = 0;

            LifeTime = _initLifeTime;
            EmissionRate = _initEmissionRate;
            
            UpdatePositions();
        }
    }
}
