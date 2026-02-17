using Beakstorm.Audio;
using Beakstorm.Gameplay.Player;
using Beakstorm.Utility;
using UnityEngine;
using AudioSettings = Beakstorm.Settings.AudioSettings;

namespace Beakstorm.Simulation.Particles
{
    public class ParticleAverageSoundSource : MonoBehaviour
    {
        [SerializeField] private string dopplerRtpc;
        [SerializeField] private float dopplerFactor = 1;
        [SerializeField] private float exposureCutoff = 0f;
        [SerializeField] private bool muteAbove = false;

        [SerializeField] private int minimumThreshold = 16;
        
        [SerializeField, Min(1)] private float attenuationMax = 64;

        [SerializeField] private bool useMultipleSources;

        [SerializeField] private float maximumAttenuationDistance = 120;
        [SerializeField, Range(0.1f, 25)] private float interpolationTime = 0.5f;
        
        private AkPositionArray _emitterArray;
        [SerializeField] private ushort _emitterCount = 64;

        private SoundCell[] _soundCells;

        private AkVector64 _vector64;
        
        struct SoundCell
        {
            public float Count;
            public Vector3 Position;
            public Vector3 Velocity;
            public float Exposure;

            public SoundCell(ParticleCell cell, bool muteAbove, float exposureCutoff)
            {
                Count = cell.Count;
                Position = cell.Position;
                Velocity = cell.Velocity;
                Exposure = cell.Data.x;
                
                
                float exposureFactor = muteAbove
                    ? (exposureCutoff - Exposure) / exposureCutoff
                    : (Exposure - exposureCutoff) / (1 - exposureCutoff);

                Count = Mathf.Max(0, (Count) * exposureFactor);
            }

            public void SmoothLerp(SoundCell target, float lambda, float deltaTime)
            {
                if (target.Count < 0)
                    target.Count = 0;
                
                if (Count <= 0.99f)
                {
                    Position = target.Position;
                    Velocity = target.Velocity;
                    Exposure = target.Exposure;
                }
                
                Count = SmoothDamp.Move(Count, target.Count, lambda, deltaTime);

                if (target.Count <= 0.99f)
                    return;
                
                Exposure = SmoothDamp.Move(Exposure, target.Exposure, lambda, deltaTime);
                Position = SmoothDamp.Move(Position, target.Position, lambda, deltaTime);
                Velocity = SmoothDamp.Move(Velocity, target.Velocity, lambda, deltaTime);
            }
        }

        struct SoundOffset
        {
            public float Attenuation;
            public Vector3 Direction;

            public SoundOffset(float attenuation, Vector3 direction)
            {
                Attenuation = attenuation;
                Direction = direction;
            }

            public void SmoothLerp(float attenuation, Vector3 direction, float lambda, float deltaTime)
            {
                Attenuation = SmoothDamp.Move(Attenuation, attenuation, lambda, deltaTime);
                Direction = SmoothDamp.Slerp(Direction, direction, lambda, deltaTime);
            }
        }

        private SoundOffset[] _soundOffsets;
        
        private void OnEnable()
        {
            _emitterArray = new AkPositionArray(_emitterCount);
            _soundCells = new SoundCell[_emitterCount];
            _soundOffsets = new SoundOffset[_emitterCount];

            for (int i = 0; i < _emitterCount; i++)
            {
                _soundOffsets[i] = new SoundOffset(300, Vector3.forward);
            }
        }

        private void OnDisable()
        {
            _emitterArray.Dispose();
        }

        private void Update()
        {
            AdjustSoundCells();
        }

        private Vector3 PlacementFromSoundCell(Vector3 listenerPos, SoundCell cell)
        {
            Vector3 diff = cell.Position - listenerPos;
            float diffLength = diff.magnitude;

            if (diffLength == 0)
                diff = Vector3.down;
            
            float quietness = Mathf.Clamp01((attenuationMax - cell.Count) / attenuationMax);

            float customAttenuation = Mathf.Lerp(Mathf.Min(diffLength, maximumAttenuationDistance), (maximumAttenuationDistance), quietness);
            //customAttenuation = diffLength * Mathf.Lerp(1, 16, quietness);

            Vector3 displacement = diff.normalized * customAttenuation;
            return displacement;
        }

        private float CalculateDoppler(Vector3 posA, Vector3 velA, Vector3 posB, Vector3 velB, float dopplerFactor)
        {
            const float SpeedOfSound = 343;
            
            Vector3 diff = posB - posA;

            float relativeSpeedA = Vector3.Dot(diff, velA) / Mathf.Max(0.001f, diff.magnitude);
            float relativeSpeedB = Vector3.Dot(diff, velB) / Mathf.Max(0.001f, diff.magnitude);

            relativeSpeedA = Mathf.Min(relativeSpeedA, (SpeedOfSound / dopplerFactor));
            relativeSpeedB = Mathf.Min(relativeSpeedB, (SpeedOfSound / dopplerFactor));

            float dopplerPitch = (SpeedOfSound + (relativeSpeedA * dopplerFactor)) /
                                 (SpeedOfSound + (relativeSpeedB * dopplerFactor));

            return dopplerPitch;
        }
        
        private void AdjustSoundCells()
        {
            if (AudioSettings.Instance.BirdsVolume == 0)
                return;
            
            if (!ParticleCellReadback.Instance || !ParticleCellReadback.Instance.Initialized)
                return;
            
            _emitterArray.Reset();

            ushort count = 0;

            Vector3 velocity = Vector3.zero;
            Vector3 position = Vector3.zero;
            float boidCount = 0;
            float exposure = 0;

            Vector3 positionAverage = Vector3.zero;

            for (int i = 0; i < _emitterCount; i++)
            {
                if (count >= _emitterCount)
                    break;


                bool isCellDataValid = (ParticleCellReadback.Instance.TryGetCellData(i, out ParticleCell cellData));

                //float exposure = cellData.Data.x;
                //float exposureFactor = muteAbove
                //    ? (exposureCutoff - exposure) / exposureCutoff
                //    : (exposure - exposureCutoff) / (1 - exposureCutoff);

                //if (isCellDataValid)
                //    soundCellTarget.Count = Mathf.Max(0, (cellData.Count - minimumThreshold) * exposureFactor);
                
                var soundCellTarget = new SoundCell(cellData, muteAbove, exposureCutoff);
                _soundCells[i].SmoothLerp(soundCellTarget, interpolationTime, Time.deltaTime);

                var cell = _soundCells[i];

                boidCount += cell.Count;
                velocity += cell.Velocity * cell.Count;
                position += cell.Position * cell.Count;
                exposure += cell.Exposure * cell.Count;

                Vector3 listenerPos = MainListener.Transform.position;
                
                Vector3 customPosition = listenerPos + PlacementFromSoundCell(listenerPos, cell);
                Vector3 displacement = PlacementFromSoundCell(listenerPos, cell);
                
                _soundOffsets[i].SmoothLerp(displacement.magnitude, displacement.normalized, interpolationTime, Time.deltaTime);

                customPosition = _soundOffsets[i].Direction * _soundOffsets[i].Attenuation + listenerPos;
                
                Vector3 fwd = cell.Velocity.magnitude > 0 ? cell.Velocity.normalized : Vector3.forward;
                
                Vector3 right = Vector3.Cross(Vector3.up, fwd);
                if (right.sqrMagnitude == 0)
                    right = Vector3.right;
                Vector3 up = Vector3.Cross(fwd, right).normalized;

                
                if (_vector64 == null)
                    _vector64 = customPosition;
                else 
                {
                    _vector64.X = customPosition.x;
                    _vector64.Y = customPosition.y;
                    _vector64.Z = customPosition.z;
                }
                
                _emitterArray.Add(_vector64, fwd, up);
                count++;

            }

            if (useMultipleSources)
            {
                AkUnitySoundEngine.SetMultiplePositions(gameObject, _emitterArray, count,
                    AkMultiPositionType.MultiPositionType_MultiSources);
            }
            else
            {
                AkUnitySoundEngine.SetMultiplePositions(gameObject, _emitterArray, count, 
                    AkMultiPositionType.MultiPositionType_MultiDirections);
            }

            if (boidCount > 0)
            {
                velocity /= boidCount;
                position /= boidCount;
                exposure /= boidCount;
            }

            var soundCell = new SoundCell();
            soundCell.Velocity = velocity;
            soundCell.Position = position;
            soundCell.Exposure = exposure;
            soundCell.Count = count;

            var position1 = MainListener.Transform.position;
            Vector3 newPos = position1 + PlacementFromSoundCell(position1, soundCell);

            //_emitterArray.Reset();
            Vector3 f = soundCell.Velocity.magnitude > 0 ? soundCell.Velocity.normalized : Vector3.forward;
                
            Vector3 r = Vector3.Cross(Vector3.up, f);
            if (r.sqrMagnitude == 0)
                r = Vector3.right;
            Vector3 u = Vector3.Cross(f, r).normalized;
            
            //_emitterArray.Add(newPos, f, u);
            //AkUnitySoundEngine.SetMultiplePositions(gameObject, _emitterArray, 1, 
            //    AkMultiPositionType.MultiPositionType_MultiSources);
            
            if (!PlayerController.Instance)
                return;
            
            if (string.IsNullOrEmpty(dopplerRtpc))
                return;
            
            float doppler = CalculateDoppler(PlayerController.Instance.Position, PlayerController.Instance.Velocity,
                position, velocity, dopplerFactor);

            AkUnitySoundEngine.SetRTPCValue(dopplerRtpc, doppler, gameObject);
        }
        
        private void SetPositions()
        {
            if (!ParticleCellReadback.Instance || !ParticleCellReadback.Instance.Initialized)
                return;
            
            _emitterArray.Reset();

            ushort count = 0;

            Vector3 velocity = Vector3.zero;
            Vector3 position = Vector3.zero;
            uint boidCount = 0;

            for (int i = 0; i < ParticleCellReadback.Instance.CellCount; i++)
            {
                if (count >= _emitterCount)
                    break;
                
                if (ParticleCellReadback.Instance.TryGetCellData(i, out ParticleCell cellData))
                {
                    if (cellData.Count <= 0 || cellData.Velocity.magnitude == 0)
                        continue;

                    if (cellData.Count < minimumThreshold)
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

                    Vector3 listenerPos = MainListener.Transform.position;
                    Vector3 diff = cellData.Position - listenerPos;

                    float diffLength = diff.magnitude;
                    if (diffLength == 0)
                        continue;


                    float quietness = (attenuationMax - (cellData.Count - minimumThreshold) * exposureFactor) / attenuationMax;

                    float customAttenuation = Mathf.Lerp(diffLength, Mathf.Max(120, diffLength), quietness);

                    customAttenuation = diffLength * Mathf.Lerp(1, 16, quietness);

                    Vector3 customPosition = cellData.Position;
                    customPosition = listenerPos + diff.normalized * customAttenuation;
                    
                    Vector3 fwd = cellData.Velocity.normalized;
                    Vector3 right = Vector3.Cross(Vector3.up, fwd);
                    if (right.sqrMagnitude == 0)
                        right = Vector3.right;
                    Vector3 up = Vector3.Cross(fwd, right).normalized;

                    _emitterArray.Add(customPosition, fwd, up);
                    count++;
                }
            }

            if (useMultipleSources)
            {
                AkUnitySoundEngine.SetMultiplePositions(gameObject, _emitterArray, count,
                    AkMultiPositionType.MultiPositionType_MultiSources);
            }
            else
            {
                AkUnitySoundEngine.SetMultiplePositions(gameObject, _emitterArray, count, 
                    AkMultiPositionType.MultiPositionType_MultiDirections);
            }

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


        private void OnDrawGizmos()
        {
            if (_soundCells == null)
                return;

            foreach (SoundOffset soundOffset in _soundOffsets)
            {
                if (!MainListener.Transform)
                    break;
                
                Gizmos.color = Color.blue;

                Vector3 diff = soundOffset.Direction * soundOffset.Attenuation;
                Vector3 pos = MainListener.Transform.position;
                Gizmos.DrawRay(pos, diff);
                Gizmos.DrawSphere(pos + diff, 4f);
            }
            
            foreach (SoundCell cell in _soundCells)
            {
                if (cell.Count <= 0)
                    continue;

                Gizmos.color = Color.Lerp(Color.green, Color.red, cell.Exposure);
                Gizmos.DrawSphere(cell.Position, Mathf.Min(16, cell.Count * 0.125f));
                Gizmos.DrawRay(cell.Position, cell.Velocity);
            }
        }
    }
}
