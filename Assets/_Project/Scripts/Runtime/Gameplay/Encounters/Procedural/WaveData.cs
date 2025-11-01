using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Beakstorm.Mapping;
using Beakstorm.Utility.Extensions;
using Cysharp.Threading.Tasks;
using TinyGoose.Tremble;
using UltEvents;
using UnityEngine;

namespace Beakstorm.Gameplay.Encounters.Procedural
{
    [PointEntity("wave", colour:"1.0 0.5 0", size:16)]
    public class WaveData : MonoBehaviour, IWaveData, ITriggerable
    {
        [SerializeField] private List<EnemySpawnPoint> spawnPoints = new(4);
        [SerializeField, Range(1, 5)] private int intensity = 1;
        
        [Header("Events")]
        [SerializeField] private UltEvent onFinish;
        [SerializeField] private Component target;

        private UniTask _task;
        private CancellationTokenSource _tokenSource;

        public void AddSpawnPoint(EnemySpawnPoint spawnPoint)
        {
            spawnPoints ??= new List<EnemySpawnPoint>(4);
            if (spawnPoint.IsValid && !spawnPoints.Contains(spawnPoint))
                spawnPoints.Add(spawnPoint);
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
        

        public void Trigger()
        {
            StartEncounter();
        }
        
        public void StartEncounter()
        {
            _task = WaveLoop(_tokenSource.Token);
        }
        

        private async UniTask WaveLoop(CancellationToken token)
        {
            if (!EncounterManager.Instance)
                return;
            
            EncounterManager.Instance.SetWar(intensity);
            EncounterManager.Instance.BeginWave(this);

            while (EncounterManager.Instance && EncounterManager.Instance.IsWaveActive)
            {
                token.ThrowIfCancellationRequested();
                await UniTask.Yield(token);
            }

            FinishEncounter();
        }
        
        private void FinishEncounter()
        {
            onFinish?.Invoke();
            target.TryTrigger();
        }
    }
}
