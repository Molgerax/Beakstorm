using UltEvents;
using UnityEngine;

namespace Beakstorm.Gameplay.Enemies
{
    public class EnemySpawner : MonoBehaviour
    {
        [SerializeField] private EnemyController enemyPrefab;

        [SerializeField] private UltEvent onSpawnEnemy;
        [SerializeField] public UltEvent onDefeat;

        [SerializeField] private WaitCondition waitCondition;
        [SerializeField] private float spawnDelay;

        public bool IsDefeated;
        
        private EnemyController _enemy;

        private AwaitableCompletionSource _completionSource = new AwaitableCompletionSource();

        public void SpawnEnemy()
        {
            if (IsDefeated)
                return;
            
            _enemy = Instantiate(enemyPrefab, transform.position, transform.rotation);
            _enemy.Spawn(this);
            _completionSource.Reset();
            IsDefeated = false;
            
            onSpawnEnemy?.Invoke();
        }

        public void OnDefeat()
        {
            IsDefeated = true;
            _completionSource.SetResult();
            onDefeat?.Invoke();
        }

        public Awaitable GetWaitCondition()
        {
            if (waitCondition == WaitCondition.Null)
                return Awaitable.EndOfFrameAsync();

            if (waitCondition == WaitCondition.WaitForDelay)
                return Awaitable.WaitForSecondsAsync(spawnDelay);

            if (waitCondition == WaitCondition.WaitUntilDefeated)
            {
                return _completionSource.Awaitable;
            }
            return Awaitable.EndOfFrameAsync();
        }


        enum WaitCondition
        {
            Null = 0,
            WaitForDelay = 1,
            WaitUntilDefeated = 2
        }
    }
}
