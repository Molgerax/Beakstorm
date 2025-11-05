using System.Threading;
using Beakstorm.Gameplay.Enemies;
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
        [SerializeField, Min(0)] private int waveIndex;
        [SerializeField, Min(0)] private float spawnDelay;

        [SerializeField, Tremble("parent")] private Transform spawnParent;
        
        [Tremble("target")] private WaveData _waveData;

        public WaveData WaveData => _waveData;
        
        public void Init(EnemySO enemy, int waveIndex, float spawnDelay)
        {
            this.enemy = enemy;
            this.waveIndex = waveIndex;
            this.spawnDelay = spawnDelay;
        }
        
        public bool IsValid => enemy;
        public int WaveIndex => waveIndex;
        
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
        
        public void OnImportFromMapEntity(MapBsp mapBsp, BspEntity entity)
        {
            AddToWaveData();
            
            if (gameObject.transform.GetChild(0))
                CoreUtils.Destroy(gameObject.transform.GetChild(0).gameObject);
        }
    }
}
