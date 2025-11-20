using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Beakstorm.Gameplay.Encounters.Procedural
{
    [CreateAssetMenu(fileName = "WaveData", menuName = "Beakstorm/Encounter/WaveData")]
    [System.Serializable]
    public class WaveDataSO : ScriptableObject, IWaveData
    {
        [SerializeField] private List<EnemySpawnDataEntry> spawnDataEntries = new List<EnemySpawnDataEntry>();
        
        public List<EnemySpawnDataEntry> SpawnDataEntries => spawnDataEntries;
        
        
        public IEnumerator<IEnemySpawnData> GetEnumerator()
        {
            return new WaveDataSoEnumerator(spawnDataEntries);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
        
        public int DangerRating()
        {
            int i = 0;
            foreach (EnemySpawnDataEntry entry in spawnDataEntries)
            {
                if (entry.IsValid)
                    i += entry.Enemy.DangerRating;
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

        class WaveDataSoEnumerator : IEnumerator<IEnemySpawnData>
        {
            private readonly IReadOnlyList<EnemySpawnDataEntry> _spawnDataEntries;
            private int _index = -1;
            private IEnemySpawnData _current;

            public WaveDataSoEnumerator(List<EnemySpawnDataEntry> entries)
            {
                _spawnDataEntries = entries;
            }
            
            public bool MoveNext()
            {
                _index++;
                return _index < _spawnDataEntries.Count;
            }

            public void Reset()
            {
                _index = -1;
            }

            object IEnumerator.Current => Current;

            public IEnemySpawnData Current => _spawnDataEntries[_index];

            public void Dispose() { }
        }
    }
}