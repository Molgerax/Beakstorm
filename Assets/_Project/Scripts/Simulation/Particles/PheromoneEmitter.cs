using UnityEngine;

namespace Beakstorm.Simulation.Particles
{
    public class PheromoneEmitter : MonoBehaviour
    {
        [SerializeField, Min(0)] private float emissionRate = 60;

        private float _remainder = 0;

        private void Update()
        {
            Emit();
        }

        private void Emit()
        {
            float emissionPerFrame = emissionRate * Time.deltaTime;
            emissionPerFrame += _remainder;
            _remainder = emissionPerFrame % 1;

            int emissionCount = Mathf.FloorToInt(emissionPerFrame);
            
            if (PheromoneManager.Instance)
                PheromoneManager.Instance.EmitParticles(emissionCount, transform.position);
        }
    }
}
