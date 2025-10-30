using Beakstorm.Gameplay.Enemies;
using UnityEngine;

namespace Beakstorm.Gameplay.Encounters.Procedural
{
    public class EnemySpawnPoint : MonoBehaviour
    {
        [SerializeField] public EnemySO enemy;
        [SerializeField] public float spawnDelay;

        public bool IsValid => enemy;

        public TransformData TransformData => new (transform);
    }
}
