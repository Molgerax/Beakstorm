using System;
using System.Threading;
using Beakstorm.Gameplay.Enemies;
using Beakstorm.Gameplay.Movement;
using Beakstorm.Mapping.Waypoints;
using Cysharp.Threading.Tasks;
using TinyGoose.Tremble;
using UnityEngine;
using UnityEngine.Rendering;

namespace Beakstorm.Gameplay.Encounters.Procedural
{
    [PrefabEntity(category:"enemy")]
    public class EnemySpawnPoint : MonoBehaviour, IEnemySpawnData, IOnImportFromMapEntity
    {
        [SerializeField, NoTremble] private EnemySO enemy;
        [SerializeField, Min(0)] private float spawnDelay;

        [SerializeField, Tremble("parent")] private Transform spawnParent;
        [Tremble("waypoint")] private Waypoint _waypoint;
        [Tremble, SpawnFlags] private bool _skipEmerge;
        
        [Tremble("target")] private WaveData _waveData;

        [SerializeField, NoTremble] private SpawnAuxiliaryData auxiliaryData;
        
        public WaveData WaveData => _waveData;
        
        public void Init(EnemySO enemy, float spawnDelay)
        {
            this.enemy = enemy;
            this.spawnDelay = spawnDelay;
        }
        
        public bool IsValid => enemy;

        public AuxiliaryData AuxiliaryData => auxiliaryData;

        public EnemySO Enemy => enemy;
        public TransformData TransformData => new (transform, spawnParent);
        public EnemySpawnDataEntry.WaitCondition WaitCondition => EnemySpawnDataEntry.WaitCondition.WaitForDelay;
        public float SpawnDelay => spawnDelay;
        
        public async UniTask GetWaitCondition(CancellationToken token)
        {
            await UniTask.Delay(Mathf.RoundToInt(spawnDelay * 1000), DelayType.DeltaTime, cancellationToken: token);
        }

        public void AddToWaveData()
        {
            if (_waveData) 
                _waveData.AddSpawnPoint(this);
        }

        private void Awake()
        {
            ReturnChildPrefabToPool();
        }



        public void OnImportFromMapEntity(MapBsp mapBsp, BspEntity entity)
        {
            AddToWaveData();

            auxiliaryData = new SpawnAuxiliaryData(_waypoint, _skipEmerge);
            
            //DeleteChildPrefab();
        }

        private void DeleteChildPrefab()
        {
            EnemyController e = GetComponentInChildren<EnemyController>();
            
            if (e)
                CoreUtils.Destroy(e.gameObject);
            return;
            
            if (gameObject.transform.GetChild(0))
                CoreUtils.Destroy(gameObject.transform.GetChild(0).gameObject);
        }
        
        
        private void ReturnChildPrefabToPool()
        {
            EnemyController e = GetComponentInChildren<EnemyController>();
            
            if (!e)
                return;
            e.Despawn();
        }

        [System.Serializable]
        public class SpawnAuxiliaryData : AuxiliaryData
        {
            public Waypoint waypoint;
            public bool SkipEmerge;

            public SpawnAuxiliaryData(Waypoint wp, bool skipEmerge = false)
            {
                waypoint = wp;
                SkipEmerge = skipEmerge;
            }
            
            public override void Apply(EnemyController enemy)
            {
                if (enemy.TryGetComponent(out MoveFollowPath followPath))
                {
                    followPath.SetWaypoint(waypoint);
                }
                
                if (enemy.TryGetComponent(out MoveEmerge emerge))
                {
                    emerge.enabled = !SkipEmerge;
                }
            }
        }
    }
}
