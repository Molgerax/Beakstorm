using UltEvents;
using UnityEngine;

namespace Beakstorm.Gameplay.Enemies
{
    public class EnemySpawner : MonoBehaviour
    {
        [SerializeField] private EnemyController enemyPrefab;

        [SerializeField] private UltEvent onSpawnEnemy;
        [SerializeField] private UltEvent onDefeat;


        private EnemyController _enemy;

        private void OnEnable()
        {
            SpawnEnemy();
        }

        public void SpawnEnemy()
        {
            _enemy = Instantiate(enemyPrefab, transform.position, transform.rotation);
            _enemy.Spawn(this);
            onSpawnEnemy?.Invoke();
        }

        public void OnDefeat()
        {
            onDefeat?.Invoke();
        }
    }
}
