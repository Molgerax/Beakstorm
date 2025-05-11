using Beakstorm.Gameplay.Enemies;
using UltEvents;
using UnityEngine;

namespace Beakstorm.Gameplay.Encounters
{
    public class EncounterWave : MonoBehaviour
    {
        [SerializeField] private EnemySpawner[] spawners;

        [SerializeField] private UltEvent onDefeatAll;

        private Coroutine _spawnSequence;
        private bool _defeatedAll;
        
        public void SpawnAll()
        {
            if (_defeatedAll)
                return;
            
            SpawnAllLoop();
        }
        
        public async void SpawnAllLoop()
        {
            for (int i = 0; i < spawners.Length; i++)
            {
                spawners[i].onDefeat.DynamicCalls += OnDefeatedEnemy;
                spawners[i].SpawnEnemy();
                await spawners[i].GetWaitCondition();
            }
        }

        private void OnDefeatedEnemy()
        {
            if (_defeatedAll)
                return;

            if (AllEnemiesDefeated())
            {
                _defeatedAll = true;
                onDefeatAll?.Invoke();
            }
        }

        public bool AllEnemiesDefeated()
        {
            bool defeatedAll = true;
            for (int i = 0; i < spawners.Length; i++)
            {
                if (spawners[i].IsDefeated == false) defeatedAll = false;
            }

            return defeatedAll;
        }

    }
}
