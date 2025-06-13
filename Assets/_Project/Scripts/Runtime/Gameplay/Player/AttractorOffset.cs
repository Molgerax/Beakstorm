using Beakstorm.Simulation;
using UnityEngine;

namespace Beakstorm.Gameplay.Player
{
    public class AttractorOffset : MonoBehaviour
    {
        [SerializeField] private Transform t;
        [SerializeField] private float normalOffset = 2;
        [SerializeField] private float attractorOffset = 8;

        private void Update()
        {
            if (!t)
                return;
            
            t.localPosition = Vector3.back * (UseAttractorSystem.UseAttractors ? attractorOffset : normalOffset);
        }
    }
}
