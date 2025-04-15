using UnityEngine;

namespace Beakstorm.Simulation.Particles
{
    public class PheromoneEmitter : MonoBehaviour
    {
        [SerializeField, Min(0)] private float emissionRate = 60;

        private float _remainder = 0;

        private Vector3 _position;
        private Vector3 _oldPosition;

        private float _deltaTime;
        
        private void OnEnable()
        {
            ResetEmitter();
        }

        private void Update()
        {
            UpdatePositions();
            EmitOverTime();
        }


        private void EmitOverTime()
        {
            float emissionPerFrame = emissionRate * _deltaTime;
            emissionPerFrame += _remainder;
            _remainder = emissionPerFrame % 1;

            int emissionCount = Mathf.FloorToInt(emissionPerFrame);
            
            if (PheromoneManager.Instance)
                PheromoneManager.Instance.EmitParticles(emissionCount, _position, _oldPosition, _deltaTime);
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
                PheromoneManager.Instance.EmitParticles(count, _position, _oldPosition, _deltaTime);
        }

        public void ResetEmitter()
        {
            _position = transform.position;
            _remainder = 0;
            UpdatePositions();
        }
    }
}
