using System.Threading;
using Beakstorm.Gameplay.Enemies;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Beakstorm.Gameplay.Encounters.Procedural
{
    public interface IEnemySpawnData
    {
        public EnemySO Enemy { get; }
        public TransformData TransformData { get; }

        public EnemySpawnDataEntry.WaitCondition WaitCondition { get; }
        public float SpawnDelay { get; }

        public UniTask GetWaitCondition(CancellationToken token);

        public EnemyController Spawn();
    }
}
