using Beakstorm.Gameplay.Enemies;
using UnityEngine;

namespace Beakstorm.Gameplay.Encounters
{
    [System.Serializable]
    public class EnemySpawnData
    {
        [SerializeField] public EnemySpawner spawner;

        [SerializeField] public WaitCondition waitCondition;
        [SerializeField] public float spawnDelay;

        [System.NonSerialized]
        private AwaitableCompletionSource _completionSource;
        
        public EnemySpawnData(EnemySpawner spawner)
        {
            this.spawner = spawner;
        }

        public bool IsDefeated()
        {
            return spawner == null || spawner.isDefeated;
        }
        
        public void SpawnEnemy()
        {
            _completionSource ??= new();
            _completionSource.Reset();
            
            
            spawner.OnDefeatAction += OnDefeat;
            spawner.SpawnEnemy();
        }

        private void OnDefeat()
        {
            spawner.OnDefeatAction -= OnDefeat;
            _completionSource.SetResult();
        }


        public Awaitable GetWaitCondition()
        {
            if (waitCondition == WaitCondition.Null)
                return Awaitable.EndOfFrameAsync();

            if (waitCondition == WaitCondition.WaitForDelay)
                return Awaitable.WaitForSecondsAsync(spawnDelay);

            if (waitCondition == WaitCondition.WaitUntilDefeated)
            {
                _completionSource ??= new AwaitableCompletionSource();
                return _completionSource.Awaitable;
            }

            return Awaitable.EndOfFrameAsync();
        }


        public enum WaitCondition
        {
            Null = 0,
            WaitForDelay = 1,
            WaitUntilDefeated = 2
        }
    }
}
