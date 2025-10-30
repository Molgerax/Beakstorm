using System.Threading;
using Beakstorm.Gameplay.Enemies;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Beakstorm.Gameplay.Encounters.Procedural
{
    [System.Serializable]
    public struct EnemySpawnDataEntry : IEnemySpawnData
    {
        [SerializeField] private EnemySO enemy;
        [SerializeField] private TransformData transformData;
        [SerializeField] private WaitCondition waitCondition;
        [SerializeField] private float spawnDelay;

        public EnemySO Enemy => enemy;

        public TransformData TransformData
        {
            get => transformData;
            set => transformData = value;
        }

        WaitCondition IEnemySpawnData.WaitCondition => waitCondition;
        public float SpawnDelay => spawnDelay;

        public bool IsValid => enemy;
        
        public EnemySpawnDataEntry(EnemySpawner spawner, float delay = 0, WaitCondition waitCondition = WaitCondition.WaitForDelay)
        {
            this.enemy = spawner.EnemySo;
            this.transformData = new TransformData(spawner.transform);
            this.spawnDelay = delay;
            this.waitCondition = waitCondition;
        }


        public EnemyController Spawn()
        {
            EnemyController e = enemy.GetEnemyInstance();
            e.Spawn(transformData);
            return e;
        }

        public async UniTask GetWaitCondition(CancellationToken token)
        {
            if (waitCondition == WaitCondition.Null)
            {
                await UniTask.Yield(token);
            }
            
            else if (waitCondition == WaitCondition.WaitForDelay)
            {
                await UniTask.Delay(Mathf.RoundToInt(spawnDelay * 1000), DelayType.DeltaTime, cancellationToken: token);
            }
            
            else if (waitCondition == WaitCondition.WaitUntilDefeated)
            {
                await UniTask.Delay(Mathf.RoundToInt(spawnDelay * 1000), DelayType.DeltaTime, cancellationToken: token);
            }
            else
            {
                await UniTask.Yield(token);
            }
        }


        public enum WaitCondition
        {
            Null = 0,
            WaitForDelay = 1,
            WaitUntilDefeated = 2
        }
    }
    
    [System.Serializable]
    public struct TransformData
    {
        public Vector3 Position;
        public Quaternion Rotation;

        public TransformData(Transform t)
        {
            Position = t.position;
            Rotation = t.rotation;
        }

        public TransformData(Vector3 pos, Quaternion rot)
        {
            Position = pos;
            Rotation = rot;
        }
            
        public TransformData(Vector3 pos)
        {
            Position = pos;
            Rotation = Quaternion.identity;
        }
    }
}
