using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Threading;
using Beakstorm.Gameplay.Enemies;
using UnityEngine;

namespace Beakstorm.Gameplay.Encounters.Procedural
{
    public class WaveHandler : IDisposable
    {
        private EncounterManager _manager;
        private WaveDataSO _waveData;
        
        private float _timer;
        private bool _finishedSpawning;

        private bool _defeated;
        public bool Defeated => _defeated;

        private UniTask _task;
        private CancellationTokenSource _tokenSource;

        private List<EnemyController> _activeEnemies;

        public event Action OnDefeatedAll;

        public float ElapsedTime => _timer;
        
        public WaveHandler(EncounterManager manager, WaveDataSO waveData)
        {
            _manager = manager;
            _waveData = waveData;
            
            _activeEnemies = new List<EnemyController>(_waveData.EnemyCount());
            _tokenSource = new();

            _timer = 0;
            _defeated = false;
            _finishedSpawning = false;
        }

        public void Reset(EncounterManager manager, WaveDataSO waveData)
        {
            Dispose();
            
            _manager = manager;
            _waveData = waveData;

            _activeEnemies = new List<EnemyController>(_waveData.EnemyCount());
            _tokenSource = new();

            _timer = 0;
            _defeated = false;
            _finishedSpawning = false;
        }

        public void Spawn()
        {
            if (_timer > 0)
            {
                Debug.LogError($"WaveHandler has not been reset.");
                return;
            }
            
            _task = SpawnAll(_tokenSource.Token);
        }
        
        
        public void Dispose()
        {
            _tokenSource?.Cancel();
            _tokenSource?.Dispose();

            OnDefeatedAll = null;
        }

        private async UniTaskVoid RunTimer(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                token.ThrowIfCancellationRequested();
                await UniTask.NextFrame(token);
                _timer += Time.deltaTime;
            }
        }

        public async UniTask SpawnAll(CancellationToken token)
        {
            _timer = 0;
            RunTimer(token).Forget();

            await SpawnAllLoop(token);
        }

        private async UniTask SpawnAllLoop(CancellationToken token)
        {
            foreach (EnemySpawnDataEntry entry in _waveData.SpawnDataEntries)
            {
                if (!entry.IsValid)
                    continue;

                await entry.GetWaitCondition(token);
                
                EnemyController enemy = entry.Spawn();
                enemy.OnHealthZero += OnEnemyDefeated;
                _activeEnemies.Add(enemy);
            }
            _finishedSpawning = true;
        }

        private void OnEnemyDefeated()
        {
            bool allActiveDefeated = CheckEnemies();

            if (_finishedSpawning && allActiveDefeated)
            {
                DefeatedAll();
            }
        }

        private void DefeatedAll()
        {
            _defeated = true;
            
            _tokenSource?.Cancel();
            
            OnDefeatedAll?.Invoke();
            OnDefeatedAll = null;
        }

        private bool CheckEnemies()
        {
            bool isDefeated = true;
            
            for (int i = _activeEnemies.Count - 1; i >= 0; i--)
            {
                var enemy = _activeEnemies[i];
                if (!enemy)
                {
                    //_activeEnemies.RemoveAt(i);
                    continue;
                }
                if (enemy.IsDefeated  == false)
                    isDefeated = false;
            }
            
            return isDefeated;
        }
    }
}