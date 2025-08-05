using System;
using UnityEngine;

namespace Beakstorm.Simulation.Particles
{
    public class ParticleAverageSoundSource : MonoBehaviour
    {
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

        private void SetPositions()
        {
            if (!ParticleCellAverage.Instance)
                return;
            
            _emitterArray.Reset();

            ushort count = 0;

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
        }
    }
}
