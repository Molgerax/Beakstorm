using System.Collections.Generic;
using Beakstorm.Gameplay.Enemies;
using UnityEngine;

namespace Beakstorm.Gameplay.Encounters.Procedural
{
    public class EncounterManager : MonoBehaviour
    {
        public static EncounterManager Instance;
        
        private List<EnemyController> _activeEnemies = new List<EnemyController>(16);
        
        private WaveDataSO _activeWave;

        private WaveHandler _waveHandler;
        
        private void Awake()
        {
            Instance = this;
        }
        
        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
            
            _waveHandler?.Dispose();
        }

        public bool BeginWave(WaveDataSO waveData)
        {
            if (_waveHandler != null && !_waveHandler.Defeated)
                return false;

            if (_waveHandler == null)
                _waveHandler = new WaveHandler(this, waveData);
            else 
                _waveHandler.Reset(this, waveData);
            
            _waveHandler.Spawn();
            return true;
        }
    }
}
