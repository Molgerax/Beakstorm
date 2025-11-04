using System.Collections.Generic;
using Beakstorm.Audio;
using UltEvents;
using UnityEngine;

namespace Beakstorm.Gameplay.Encounters.Procedural
{
    public class EncounterManager : MonoBehaviour
    {
        [SerializeField] private UltEvent onFinishEncounter;
        
        public static EncounterManager Instance;
        
        private readonly List<WaveHandler> _waveHandlers = new();
        
        private void Awake()
        {
            Instance = this;
        }

        private int _dangerRating = 0;

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;

            foreach (WaveHandler handler in _waveHandlers)
            {
                handler?.Dispose();
            }
        }

        public void SetPeace(int intensity)
        {
            MusicStateManager.Instance.SetPeace(intensity - 1);
        }

        public void SetWar(int intensity)
        {
            MusicStateManager.Instance.SetWar(intensity - 1);
        }


        public WaveHandler BeginWave(IWaveData waveData)
        {
            WaveHandler waveHandler = null;
            foreach (WaveHandler handler in _waveHandlers)
            {
                if (handler == null)
                    continue;
                
                if (handler.Defeated)
                    waveHandler = handler;
            }

            if (waveHandler != null)
                waveHandler.Reset(this, waveData);
            else
            {
                waveHandler = new WaveHandler(this, waveData);
                _waveHandlers.Add(waveHandler);
            }
            
            waveHandler.Spawn();
            EvaluateDanger();
            waveHandler.OnDefeatedAll += EvaluateDanger;
            return waveHandler;
        }

        private void EvaluateDanger()
        {
            _dangerRating = 0;
            foreach (var handler in _waveHandlers)
            {
                if (handler != null)
                {
                    if (!handler.Defeated)
                        _dangerRating = Mathf.Max(_dangerRating, handler.WaveData.DangerRating());
                }
            }
            
            if (_dangerRating == 0)
                SetPeace(1);
            else
                SetWar(_dangerRating);
        }
        
        public void FinishEncounter()
        {
            onFinishEncounter?.Invoke();
        }
    }
}
