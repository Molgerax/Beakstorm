using System.Threading;
using Beakstorm.Gameplay.Enemies;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Beakstorm.Gameplay.Encounters.Procedural
{
    public class EnemySpawnPoint : MonoBehaviour, IEnemySpawnData
    {
        [SerializeField] public EnemySO enemy;
        [SerializeField, Min(0)] public float spawnDelay;

        public bool IsValid => enemy;

        public EnemySO Enemy => enemy;
        public TransformData TransformData => new (transform);
        public EnemySpawnDataEntry.WaitCondition WaitCondition => EnemySpawnDataEntry.WaitCondition.WaitForDelay;
        public float SpawnDelay => spawnDelay;
        
        public async UniTask GetWaitCondition(CancellationToken token)
        {
            await UniTask.Delay(Mathf.RoundToInt(spawnDelay * 1000), DelayType.DeltaTime, cancellationToken: token);
        }
    }
}
