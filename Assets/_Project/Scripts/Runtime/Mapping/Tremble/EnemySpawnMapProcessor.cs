using System.Collections.Generic;
using Beakstorm.Gameplay.Encounters.Procedural;
using TinyGoose.Tremble;
using UnityEngine;
using UnityEngine.Rendering;

namespace Beakstorm.Mapping.Tremble
{
    public class EnemySpawnMapProcessor : MapProcessorBase
    {
        private List<TrembleEnemySpawn> _enemySpawns;
        
        public override void ProcessPrefabEntity(MapBsp mapBsp, BspEntity entity, GameObject prefab)
        {
            _enemySpawns ??= new();
        
            if (!prefab.TryGetComponent(out TrembleEnemySpawn spawn))
                return;

            _enemySpawns.Add(spawn);
            return;
        }

        public override void OnProcessingCompleted(GameObject root, MapBsp mapBsp)
        {
            for (int i = _enemySpawns.Count - 1; i >= 0; i--)
            {
                var spawn = _enemySpawns[i];
                StripGameObject(spawn.gameObject);
                AttachEnemySpawnPoint(spawn, spawn.gameObject);
                CoreUtils.Destroy(spawn);
                _enemySpawns.RemoveAt(i);
            }
        }

        private void StripGameObject(GameObject gameObject)
        {
            for (int i = gameObject.transform.childCount - 1; i >= 0; i--)
            {
                Transform t = gameObject.transform.GetChild(i);
                CoreUtils.Destroy(t.gameObject);
            }
            var components = gameObject.GetComponents<Component>();
            for (int i = components.Length - 1; i >= 0; i--)
            {
                var c = components[i];
                
                if(c == gameObject.GetComponent<TrembleEnemySpawn>() || c == c.transform)
                    continue;
                CoreUtils.Destroy(c);
            }
        }

        private void AttachEnemySpawnPoint(TrembleEnemySpawn spawn, GameObject go)
        {
            EnemySpawnPoint spawnPoint = go.AddComponent<EnemySpawnPoint>();
            spawnPoint.Init(spawn.Enemy, 0, spawn.SpawnDelay);


            if (spawn.WaveData)
            {
                spawn.WaveData.AddSpawnPoint(spawnPoint);
                return;
            }

            WaveData waveData = FindOrCreateWaveData(spawn, go);
            if (waveData)
                waveData.AddSpawnPoint(spawnPoint);
        }
        
        private WaveData FindOrCreateWaveData(TrembleEnemySpawn spawn, GameObject go)
        {
            WaveData waveData = go.GetComponentInParent<WaveData>();
            if (waveData == null)
                waveData = go.transform.parent.gameObject.AddComponent<WaveData>();
            return waveData;
        }
    }
}