using Beakstorm.Gameplay.Player.Weapons;
using UnityEngine;

namespace Beakstorm.Simulation.Particles
{
    public class PheromoneEmitter : MonoBehaviour
    {
        [SerializeField, Min(0)] private float emissionRate = 60;
        [SerializeField, Range(0, 10)] private float lifeTime = 3;

        private PheromoneBehaviourData _behaviourData;

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
        private float _duration;
        
        private Vector3 _position;
        private Vector3 _oldPosition;

        public Vector3 Position => _position;
        public Vector3 OldPosition => _oldPosition;
        
        private void Awake()
        {
            _initEmissionRate = emissionRate;
            _initLifeTime = lifeTime;
        }

        public void SetBehaviourData(PheromoneBehaviourData data)
        {
            _behaviourData = data;
        }
        
        protected virtual void OnEnable()
        {
            ResetEmitter();
            
            if (!PheromoneGridManager.Emitters.Contains(this))
                PheromoneGridManager.Emitters.Add(this);
        }

        protected virtual void OnDisable()
        {
            if (PheromoneGridManager.Emitters.Contains(this))
                PheromoneGridManager.Emitters.Remove(this);
        }

        public void EmitOverTime(float deltaTime)
        {
            UpdatePositions();
            ApplyBehaviour(deltaTime);

            float emissionPerFrame = EmissionRate * deltaTime;
            emissionPerFrame += _remainder;
            _remainder = emissionPerFrame % 1;

            int emissionCount = Mathf.FloorToInt(emissionPerFrame);
            Emit(emissionCount);
        }
        
        
        private void UpdatePositions()
        {
            _oldPosition = _position;
            _position = transform.position;
        }

        public void Emit(int count, float life)
        {
            if (PheromoneGridManager.Instance)
                PheromoneGridManager.Instance.AddEmissionRequest(count, _position, _position , life);
        }
        
        private void Emit(int count)
        {
            if (PheromoneGridManager.Instance)
                PheromoneGridManager.Instance.EmitParticles(count, _position, _oldPosition, lifeTime);
        }

        private void ApplyBehaviour(float deltaTime)
        {
            _duration += deltaTime;
            
            if (!_behaviourData)
                return;

            LifeTime = _behaviourData.GetPheromoneLife(_duration);
            EmissionRate = _behaviourData.GetPheromoneEmission(_duration);
        }

        public void ResetEmitter()
        {
            _position = transform.position;
            _remainder = 0;
            _duration = 0;

            LifeTime = _initLifeTime;
            EmissionRate = _initEmissionRate;

            UpdatePositions();
        }
    }
}
