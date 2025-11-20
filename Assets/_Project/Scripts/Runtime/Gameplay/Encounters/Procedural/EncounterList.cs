using System.Threading;
using Cysharp.Threading.Tasks;
using UltEvents;
using UnityEngine;

namespace Beakstorm.Gameplay.Encounters.Procedural
{
    public class EncounterList : MonoBehaviour
    {
        [SerializeField] private WaveDescription[] waves;

        [SerializeField] private UltEvent onFinishEncounter;
        
        private UniTask _task;
        private CancellationTokenSource _tokenSource;

        public WaveDescription[] Waves => waves;

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


        private void FinishEncounter()
        {
            onFinishEncounter?.Invoke();
            EncounterManager.Instance.FinishEncounter();
        }
        
        private async UniTask WaveLoop(CancellationToken token)
        {
            for (var index = 0; index < waves.Length; index++)
            {
                WaveDescription wave = waves[index];
                if (wave.WaveData == null)
                {
                    EncounterManager.Instance.SetPeace(wave.CalmIndex);
                    await UniTask.Delay(wave.CalmDuration * 1000, cancellationToken: token);
                }
                else
                {
                    EncounterManager.Instance.SetWar(wave.CalmIndex);
                    var handler = EncounterManager.Instance.BeginWave(wave.WaveData);

                    while (!handler.Defeated)
                    {
                        token.ThrowIfCancellationRequested();
                        await UniTask.Yield(token);
                    }
                }
            }

            FinishEncounter();
        }



        [System.Serializable]
        public struct WaveDescription
        {
            public WaveDataSO WaveData;
            public int CalmIndex;
            public int CalmDuration;

            public WaveDescription(WaveDataSO data)
            {
                WaveData = data;
                CalmIndex = 1;
                CalmDuration = 15;
            }
        }
    }
}
