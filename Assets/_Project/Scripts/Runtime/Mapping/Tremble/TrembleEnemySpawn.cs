using Beakstorm.Gameplay.Encounters.Procedural;
using Beakstorm.Gameplay.Enemies;
using TinyGoose.Tremble;
using UnityEngine;
using UnityEngine.Rendering;

namespace Beakstorm.Mapping.Tremble
{
    [PrefabEntity(category:"enemy")]
    public class TrembleEnemySpawn : MonoBehaviour, IOnImportFromMapEntity
    {
        [SerializeField] private int waveIndex = 0;
        [SerializeField] private float spawnDelay = 0;

        [Tremble("target")] private WaveData _waveData;
        
        [SerializeField, HideInInspector, NoTremble]
        public EnemySO Enemy;
        
        public void OnImportFromMapEntity(MapBsp mapBsp, BspEntity entity)
        {
            GameObject newGameObject = new GameObject(gameObject.name + "_Spawn");
            newGameObject.transform.SetParent(transform.parent);
            newGameObject.transform.SetLocalPositionAndRotation(transform.localPosition, transform.localRotation);

            EnemySpawnPoint spawnPoint = newGameObject.AddComponent<EnemySpawnPoint>();
            spawnPoint.Init(Enemy, waveIndex, spawnDelay);
            
            
            WaveData waveData = FindOrCreateWaveData();
            if (waveData)
                waveData.AddSpawnPoint(spawnPoint);
            
            CoreUtils.Destroy(gameObject);
        }

        private WaveData FindOrCreateWaveData()
        {
            if (_waveData)
                return _waveData;
            
            WaveData waveData = GetComponentInParent<WaveData>();
            if (waveData == null)
                waveData = transform.parent.gameObject.AddComponent<WaveData>();
            return waveData;
        }
    }
}