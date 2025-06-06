using System;
using System.Collections.Generic;
using System.Threading;
using AptabaseSDK;
using Beakstorm.Gameplay.Player;
using UnityEngine;

namespace Beakstorm.Gameplay.Encounters
{
    public class Encounter : MonoBehaviour
    {
        [SerializeField] private EncounterWave[] waves;
        
        private bool _defeatedAll;

        private float _timer;

        private CancellationTokenSource _tokenSource;
        
        public void SpawnAll()
        {
            if (_defeatedAll)
                return;

            _tokenSource = new CancellationTokenSource();
            
            SpawnAllLoop();
            RunTimer(_tokenSource.Token);
        }

        private void OnDestroy()
        {
            _tokenSource?.Dispose();
        }


        private async void SpawnAllLoop()
        {
            for (int i = 0; i < waves.Length; i++)
            {
                await waves[i].SpawnAll();
            }

            _tokenSource.Cancel();
            SendEncounterCompletedEvent();
        }

        private async void RunTimer(CancellationToken token)
        {
            _timer = 0;
            
            while (!token.IsCancellationRequested)
            {
                _timer += Time.deltaTime;
                try
                {
                    await Awaitable.NextFrameAsync(token);
                }
                catch (OperationCanceledException e)
                {
                    Debug.LogError(e, this);
                }
            }
        }
        
        
        private void SendEncounterCompletedEvent()
        {
            Dictionary<string, object> dict = new Dictionary<string, object>()
            {
                {"index", gameObject.name}, 
                {"time", _timer},
                {"damage", PlayerController.Instance.DamageTaken}
            };
            Aptabase.TrackEvent("encounter_completed", dict);
        }
    }
}
