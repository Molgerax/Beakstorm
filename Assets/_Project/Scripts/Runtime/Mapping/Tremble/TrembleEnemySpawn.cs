using Beakstorm.Gameplay.Encounters.Procedural;
using Beakstorm.Gameplay.Enemies;
using TinyGoose.Tremble;
using UnityEngine;
using UnityEngine.Rendering;

namespace Beakstorm.Mapping.Tremble
{
    //[PrefabEntity(category:"enemy")]
    public class TrembleEnemySpawn : MonoBehaviour, IOnImportFromMapEntity, IOnLiveUpdate
    {
        [SerializeField] private int waveIndex = 0;
        [SerializeField] private float spawnDelay = 0;

        [SerializeField]
        [Tremble("target")] private WaveData _waveData;
        
        [SerializeField, HideInInspector, NoTremble]
        public EnemySO Enemy;

        public float SpawnDelay => spawnDelay;
        public WaveData WaveData => _waveData;

        public void OnLiveUpdated()
        {
            StripAndReplace();
        }
        
        public void OnImportFromMapEntity(MapBsp mapBsp, BspEntity entity)
        {
            StripAndReplace();
            return;
            
            
            GameObject newGameObject = new GameObject(gameObject.name + "_Spawn");
            newGameObject.transform.SetParent(transform.parent);
            newGameObject.transform.SetLocalPositionAndRotation(transform.localPosition, transform.localRotation);

            AttachEnemySpawnPoint(newGameObject);
            
            CoreUtils.Destroy(gameObject);
        }

        private void StripAndReplace()
        {
            StripGameObject();
            AttachEnemySpawnPoint(gameObject);
            CoreUtils.Destroy(this);
        }

        private void StripGameObject()
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                Transform t = transform.GetChild(i);
                CoreUtils.Destroy(t.gameObject);
            }
            var components = gameObject.GetComponents<Component>();
            for (int i = components.Length - 1; i >= 0; i--)
            {
                var c = components[i];
                
                if(c == this || c == c.transform)
                    continue;
                CoreUtils.Destroy(c);
            }
        }

        private EnemySpawnPoint AttachEnemySpawnPoint(GameObject go)
        {
            EnemySpawnPoint spawnPoint = go.AddComponent<EnemySpawnPoint>();
            spawnPoint.Init(Enemy, waveIndex, spawnDelay);
            
            
            WaveData waveData = FindOrCreateWaveData();
            if (waveData)
                waveData.AddSpawnPoint(spawnPoint);

            return spawnPoint;
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