using System.Collections.Generic;
using Beakstorm.Gameplay.Encounters.Procedural;
using TinyGoose.Tremble;
using UnityEngine;

namespace Beakstorm.Mapping.Tremble.MapProcessors
{
    public class EnemySpawnMapProcessor : MapProcessorBase
    {
        private List<EnemySpawnPoint> _enemySpawns;
        
        public override void ProcessPrefabEntity(MapBsp mapBsp, BspEntity entity, GameObject prefab)
        {
            _enemySpawns ??= new();
        
            if (!prefab.TryGetComponent(out EnemySpawnPoint spawn))
                return;

            _enemySpawns.Add(spawn);
        }

        public override void OnProcessingCompleted(GameObject root, MapBsp mapBsp)
        {
            if (_enemySpawns == null)
                return;

            WaveData waveData = null;
            
            MapWorldSpawn worldSpawn = GetWorldspawn<MapWorldSpawn>();
            foreach (EnemySpawnPoint spawn in _enemySpawns)
            {
                if (spawn.WaveData)
                    continue;

                if (!waveData)
                {
                    waveData = worldSpawn.gameObject.AddComponent<WaveData>();
                    waveData.SetAutoStart(true);
                }
                
                waveData.AddSpawnPoint(spawn);
            }
        }

    }
}