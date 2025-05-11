using System;
using System.Collections.Generic;
using System.Threading;
using AptabaseSDK;
using Beakstorm.Gameplay.Enemies;
using Beakstorm.Gameplay.Player;
using UltEvents;
using UnityEngine;

namespace Beakstorm.Gameplay.Encounters
{
    public class EncounterWave : MonoBehaviour
    {
        [SerializeField] private int waveIndex = 0;
        [SerializeField] private EnemySpawner[] spawners;

        [SerializeField] private UltEvent onDefeatAll;

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


        public async void SpawnAllLoop()
        {
            for (int i = 0; i < spawners.Length; i++)
            {
                spawners[i].onDefeat.DynamicCalls += OnDefeatedEnemy;
                spawners[i].SpawnEnemy();
                await spawners[i].GetWaitCondition();
            }
        }

        public async void RunTimer(CancellationToken token)
        {
            _timer = 0;
            
            while (!token.IsCancellationRequested)
            {
                _timer += Time.deltaTime;
                await Awaitable.NextFrameAsync(token);
            }
        }
        
        private void OnDefeatedEnemy()
        {
            if (_defeatedAll)
                return;

            if (AllEnemiesDefeated())
            {
                _defeatedAll = true;
                _tokenSource.Cancel();
                onDefeatAll?.Invoke();
                SendWaveCompletedEvent();
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

        private void SendWaveCompletedEvent()
        {
            Dictionary<string, object> dict = new Dictionary<string, object>()
            {
                {"index", waveIndex}, 
                {"time", _timer},
                {"damage", PlayerController.Instance.DamageTaken}
            };
            Aptabase.TrackEvent("wave_completed", dict);
        }
    }
}
