using UnityEngine;

namespace Beakstorm.Simulation.Particles
{
    public class ParticleReadbackMarker : MonoBehaviour
    {
        [SerializeField] private float radius = 16;

        private void Update()
        {
            if (!ParticleCellReadback.Instance || !ParticleCellReadback.Instance.Initialized)
                return;


            for (int i = 0; i < 20; i++)
            {
                Vector3 pos = transform.position + Random.insideUnitSphere * radius;
                ParticleCellReadback.Instance.TryAddCellRequest(pos);
            }
        }
    }
}
