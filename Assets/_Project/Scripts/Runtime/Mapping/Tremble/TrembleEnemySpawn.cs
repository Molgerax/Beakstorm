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

        [SerializeField, HideInInspector, NoTremble]
        public EnemySO Enemy;
        
        public void OnImportFromMapEntity(MapBsp mapBsp, BspEntity entity)
        {
            GameObject newGameObject = new GameObject(gameObject.name);
            newGameObject.transform.SetParent(transform.parent);
            newGameObject.transform.SetLocalPositionAndRotation(transform.localPosition, transform.localRotation);

            EnemySpawnPoint spawnPoint = newGameObject.AddComponent<EnemySpawnPoint>();
            spawnPoint.Init(Enemy, waveIndex, spawnDelay);
            
            CoreUtils.Destroy(gameObject);
        }
    }
}