using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Beakstorm.Gameplay.Encounters.Procedural
{
    public class EncounterList : MonoBehaviour
    {
        [SerializeField] private WaveDescription[] waves;

        private UniTask _task;
        private CancellationTokenSource _tokenSource;

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
                    EncounterManager.Instance.BeginWave(wave.WaveData);

                    while (EncounterManager.Instance.IsWaveActive)
                    {
                        token.ThrowIfCancellationRequested();
                        await UniTask.Yield(token);
                    }
                }
            }
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
