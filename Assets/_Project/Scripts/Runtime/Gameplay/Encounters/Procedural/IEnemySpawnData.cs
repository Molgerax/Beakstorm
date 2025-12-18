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
        
        public AuxiliaryData AuxiliaryData { get; } 
    }

    public class AuxiliaryData
    {
        public virtual void Apply(EnemyController enemy) { }
    }
    
    public static class EnemySpawnDataExtensions
    {
        public static EnemyController Spawn(this IEnemySpawnData data)
        {
            EnemyController e = data.Enemy.GetEnemyInstance();
            data.AuxiliaryData?.Apply(e);
            e.Spawn(data.TransformData);
            return e;
        }
    }
}
