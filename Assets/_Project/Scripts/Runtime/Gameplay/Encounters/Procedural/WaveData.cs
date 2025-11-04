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
    public class WaveData : TriggerBehaviour, IWaveData
    {
        [SerializeField] private List<EnemySpawnPoint> spawnPoints = new(4);
        [SerializeField, Range(1, 5)] private int intensity = 1;
        
        [Header("Events")]
        [SerializeField] private UltEvent onFinish;
        [SerializeField, Tremble("target")] private TriggerBehaviour[] target;

        private UniTask _task;
        private CancellationTokenSource _tokenSource;

        public void AddSpawnPoint(EnemySpawnPoint spawnPoint)
        {
            spawnPoints ??= new List<EnemySpawnPoint>(4);

            for (int i = spawnPoints.Count - 1; i >= 0; i--)
            {
                if (!spawnPoints[i])
                    spawnPoints.RemoveAt(i);
            }
            
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
                if (entry && entry.IsValid)
                    i += entry.Enemy.DangerRating;
            }
            return i;
        }
        
        public int EnemyCount()
        {
            int i = 0;
            foreach (EnemySpawnPoint entry in spawnPoints)
            {
                if (entry && entry.IsValid)
                    i++;
            }
            return i;
        }
        
        private void Awake()
        {
            _tokenSource = new();
        }

        private void Update()
        {
            if (spawnPoints == null)
                return;
            
            for (int i = spawnPoints.Count - 1; i >= 0; i--)
            {
                if (!spawnPoints[i])
                    spawnPoints.RemoveAt(i);
            }
        }

        private void OnDestroy()
        {
            _tokenSource?.Cancel();
            _tokenSource?.Dispose();
        }
        

        public override void Trigger()
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
