using UnityEngine;

namespace Beakstorm.Simulation
{
    public class UseAttractorSystem : MonoBehaviour
    {
        public static bool UseAttractors { get; private set; }
        public static string UseAttractorsString => UseAttractors ? "Attractors" : "Pheromones";

        [SerializeField] private bool enable;

        private void OnEnable()
        {
            UseAttractors = enable;
        }
    }
}
