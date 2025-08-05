using Beakstorm.Gameplay.Player;
using UnityEngine;

namespace Beakstorm.Simulation.Particles
{
    public class ParticleAverageSoundSource : MonoBehaviour
    {
        [SerializeField] private string dopplerRtpc;
        [SerializeField] private float dopplerFactor = 1;
        [SerializeField] private float exposureCutoff = 0f;
        [SerializeField] private bool muteAbove = false;
    
        private AkPositionArray _emitterArray;
        private ushort _emitterCount = 600;

        private void OnEnable()
        {
            _emitterArray = new AkPositionArray(_emitterCount);
        }

        private void OnDisable()
        {
            _emitterArray.Dispose();
        }

        private void Update()
        {
            SetPositions();
        }

        private float CalculateDoppler(Vector3 posA, Vector3 velA, Vector3 posB, Vector3 velB, float dopplerFactor)
        {
            const float SpeedOfSound = 343;
            
            Vector3 diff = posB - posA;

            float relativeSpeedA = Vector3.Dot(diff, velA) / Mathf.Max(0.001f, diff.magnitude);
            float relativeSpeedB = Vector3.Dot(diff, velB) / Mathf.Max(0.001f, diff.magnitude);

            relativeSpeedA = Mathf.Min(relativeSpeedA, (SpeedOfSound / dopplerFactor));
            relativeSpeedB = Mathf.Min(relativeSpeedB, (SpeedOfSound / dopplerFactor));

            float dopplerPitch = (SpeedOfSound + (relativeSpeedB * dopplerFactor)) /
                                 (SpeedOfSound + (relativeSpeedA * dopplerFactor));

            return dopplerPitch;
        }
        
        private void SetPositions()
        {
            if (!ParticleCellAverage.Instance)
                return;
            
            _emitterArray.Reset();

            ushort count = 0;

            Vector3 velocity = Vector3.zero;
            Vector3 position = Vector3.zero;
            uint boidCount = 0;
            
            for (int i = 0; i < ParticleCellAverage.Instance.CellCount; i++)
            {
                if (count >= _emitterCount)
                    break;
                
                if (ParticleCellAverage.Instance.GetCellData(i, out ParticleCellAverage.ParticleCell cellData))
                {
                    if (cellData.Count <= 0 || cellData.Velocity.magnitude == 0)
                        continue;

                    float exposure = cellData.Data.x;
                    
                    if (exposure > exposureCutoff && muteAbove)
                        continue;
                    if (exposure < exposureCutoff && !muteAbove)
                        continue;

                    boidCount += cellData.Count;
                    velocity += cellData.Velocity * cellData.Count;
                    position += cellData.Position * cellData.Count;
                    
                    float exposureFactor = muteAbove ? (exposureCutoff - exposure) / exposureCutoff : (exposure - exposureCutoff) / (1 - exposureCutoff);
                    
                    int addCount = Mathf.FloorToInt(Mathf.Clamp(cellData.Count * exposureFactor / 64, 1, 4));

                    Vector3 fwd = cellData.Velocity.normalized;
                    Vector3 right = Vector3.Cross(Vector3.up, fwd);
                    if (right.sqrMagnitude == 0)
                        right = Vector3.right;
                    Vector3 up = Vector3.Cross(fwd, right).normalized;

                    for (int j = 0; j < addCount; j++)
                    {
                        _emitterArray.Add(cellData.Position, fwd, up);
                        count++;
                    }
                }
            }

            AkUnitySoundEngine.SetMultiplePositions(gameObject, _emitterArray, (ushort)_emitterArray.Count,
                AkMultiPositionType.MultiPositionType_MultiSources);
            
            //AkUnitySoundEngine.SetMultiplePositions(gameObject, _emitterArray, (ushort)_emitterArray.Count,
            //    AkMultiPositionType.MultiPositionType_MultiDirections);

            velocity /= boidCount;
            position /= boidCount;

            if (!PlayerController.Instance)
                return;
            
            if (string.IsNullOrEmpty(dopplerRtpc))
                return;
            
            float doppler = CalculateDoppler(PlayerController.Instance.Position, PlayerController.Instance.Velocity,
                position, velocity, dopplerFactor);

            AkUnitySoundEngine.SetRTPCValue(dopplerRtpc, doppler, gameObject);
        }
    }
}
