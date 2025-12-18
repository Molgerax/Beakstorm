using UnityEngine;

namespace Beakstorm.Simulation.Settings
{
    public class ParticleSimulationCenter : MonoBehaviour
    {
        public static ParticleSimulationCenter Instance { get; private set; }

        private Transform _t;
        public Transform T
        {
            get
            {
                if (!_t)
                    _t = transform;
                return _t;
            }
        }

        private void OnEnable()
        {
            Instance = this;
        }

        private void OnDisable()
        {
            if (Instance == this)
                Instance = null;
        }
    }
}