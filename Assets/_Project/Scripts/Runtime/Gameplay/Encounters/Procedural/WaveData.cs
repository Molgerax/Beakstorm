using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Beakstorm.Mapping;
using Beakstorm.SceneManagement;
using Cysharp.Threading.Tasks;
using TinyGoose.Tremble;
using UltEvents;
using UnityEngine;

namespace Beakstorm.Gameplay.Encounters.Procedural
{
    [PointEntity("wave", "func")]
    public class WaveData : TriggerSender, ITriggerTarget, IWaveData, IOnSceneLoad
    {
        [SerializeField] private List<EnemySpawnPoint> spawnPoints = new(4);
        [SerializeField, Range(1, 5)] private int intensity = 1;
        [SerializeField] private bool autoStartOnLoad = false;
        
        [Header("Events")]
        [SerializeField] private UltEvent onFinish;

        private UniTask _task;
        private CancellationTokenSource _tokenSource;

        public void SetAutoStart(bool value) => autoStartOnLoad = value;

        private bool _started = false;
        
        public SceneLoadCallbackPoint SceneLoadCallbackPoint => SceneLoadCallbackPoint.AfterAll;

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
            return intensity;
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
            
            GlobalSceneLoader.ExecuteWhenLoaded(this);
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
        

        public void Trigger(TriggerData data)
        {
            StartEncounter();
        }
        
        public void StartEncounter()
        {
            if (_started)
                return;
            
            _task = WaveLoop(_tokenSource.Token);
        }
        

        private async UniTask WaveLoop(CancellationToken token)
        {
            if (!EncounterManager.Instance)
                return;

            _started = true;
            
            var handler = EncounterManager.Instance.BeginWave(this);

            while (!handler.Defeated)
            {
                token.ThrowIfCancellationRequested();
                await UniTask.Yield(token);
            }

            FinishEncounter();
        }
        
        private void FinishEncounter()
        {
            onFinish?.Invoke();
            SendTrigger();
        }

        public void OnSceneLoaded()
        {
            if (autoStartOnLoad)
                Trigger(default);
        }
    }
}
