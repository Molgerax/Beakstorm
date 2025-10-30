using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UltEvents;
using UnityEngine;

namespace Beakstorm.Gameplay.Encounters.Procedural
{
    public class AutoWaveData : MonoBehaviour, IWaveData
    {
        [SerializeField] private List<EnemySpawnPoint> spawnPoints;
        [SerializeField, Range(1, 5)] private int intensity = 1;

        [SerializeField, Min(0)] private int waveIndex;
        [SerializeField] private GameObject parent;
        
        [Header("Events")] 
        [SerializeField] private UltEvent onFinish;


        private UniTask _task;
        private CancellationTokenSource _tokenSource;

        private GameObject Parent => parent ? parent : gameObject;

        private void OnValidate()
        {
            var spawns = Parent.GetComponentsInChildren<EnemySpawnPoint>();
            spawnPoints ??= new();
            spawnPoints.Clear();

            foreach (EnemySpawnPoint enemySpawnPoint in spawns)
            {
                if (enemySpawnPoint.WaveIndex == waveIndex)
                    spawnPoints.Add(enemySpawnPoint);
            }
        }

        public IEnumerator<IEnemySpawnData> GetEnumerator()
        {
            return spawnPoints.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public int DangerRating()
        {
            int i = 0;
            foreach (EnemySpawnPoint entry in spawnPoints)
            {
                if (entry.IsValid)
                    i += entry.Enemy.DangerRating;
            }
            return i;
        }
        
        public int EnemyCount()
        {
            int i = 0;
            foreach (EnemySpawnPoint entry in spawnPoints)
            {
                if (entry.IsValid)
                    i++;
            }
            return i;
        }
        
        private void Awake()
        {
            _tokenSource = new();
        }

        private void OnDestroy()
        {
            _tokenSource?.Cancel();
            _tokenSource?.Dispose();
        }
        

        public void StartEncounter()
        {
            _task = WaveLoop(_tokenSource.Token);
        }
        

        private async UniTask WaveLoop(CancellationToken token)
        {
            EncounterManager.Instance.SetWar(intensity);
            EncounterManager.Instance.BeginWave(this);

            while (EncounterManager.Instance.IsWaveActive)
            {
                token.ThrowIfCancellationRequested();
                await UniTask.Yield(token);
            }

            FinishEncounter();
        }
        
        private void FinishEncounter()
        {
            onFinish?.Invoke();
        }
    }
}
