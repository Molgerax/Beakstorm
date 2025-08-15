using System.Collections.Generic;
using UnityEngine;

namespace Beakstorm.Gameplay.Encounters.Procedural
{
    [CreateAssetMenu(fileName = "WaveData", menuName = "Beakstorm/Encounter/WaveData")]
    [System.Serializable]
    public class WaveDataSO : ScriptableObject
    {
        [SerializeField] private List<EnemySpawnDataEntry> spawnDataEntries = new List<EnemySpawnDataEntry>();
        
        public List<EnemySpawnDataEntry> SpawnDataEntries => spawnDataEntries;
        
        
        public int DangerRating()
        {
            int i = 0;
            foreach (EnemySpawnDataEntry entry in spawnDataEntries)
            {
                if (entry.IsValid)
                    i += entry.enemy.DangerRating;
            }
            return i;
        }
        
        public int EnemyCount()
        {
            int i = 0;
            foreach (EnemySpawnDataEntry entry in spawnDataEntries)
            {
                if (entry.IsValid)
                    i++;
            }
            return i;
        }
        
        
    }
}