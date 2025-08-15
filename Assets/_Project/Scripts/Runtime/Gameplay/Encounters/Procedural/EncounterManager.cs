using System;
using Beakstorm.Audio;
using UnityEngine;

namespace Beakstorm.Gameplay.Encounters.Procedural
{
    public class EncounterManager : MonoBehaviour
    {
        public static EncounterManager Instance;
        
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

        public void SetPeace(int intensity)
        {
            MusicStateManager.Instance.SetPeace(intensity - 1);
        }

        public void SetWar(int intensity)
        {
            MusicStateManager.Instance.SetWar(intensity - 1);
        }

        public bool IsWaveActive
        {
            get
            {
                if (_waveHandler == null)
                    return false;

                return _waveHandler.Defeated == false;
            }
        }

        public bool BeginWave(WaveDataSO waveData)
        {
            if (IsWaveActive)
                return false;

            if (_waveHandler == null)
                _waveHandler = new WaveHandler(this, waveData);
            else 
                _waveHandler.Reset(this, waveData);
            
            _waveHandler.Spawn();
            _waveHandler.OnDefeatedAll += WaveHandlerOnDefeatedAll;
            return true;
        }

        private void WaveHandlerOnDefeatedAll()
        {
            Debug.Log("Wave defeated");
            _waveHandler.Dispose();
            _waveHandler = null;
        }
    }
}
