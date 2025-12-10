using Beakstorm.Mapping.Tremble;
using UnityEngine;

namespace Beakstorm.Gameplay.Movement
{
    public class MoveEmerge : MovementBehaviour
    {
        [SerializeField] private float emergeTime = 10f;


        private float _emergeTimer;
        private Vector3 _emergePos, _spawnPos;

        private float LowerBound => MapWorldSpawn.Instance ? MapWorldSpawn.Instance.MapLowerBound : -256;

        public override void Initialize(Transform t)
        {
            _spawnPos = t.position;
            _emergePos = _spawnPos;
            _emergePos.y = LowerBound;
            t.position = _emergePos;

            _emergeTimer = emergeTime;
        }
    
        public override void ApplyMovement(Transform tr)
        {
            if (_emergeTimer > 0)
            {
                float t = 1 - (_emergeTimer / emergeTime);

                t = 1 - (1-t) * (1-t);
                
                tr.position = Vector3.Lerp(_emergePos, _spawnPos, t);
                _emergeTimer -= Time.deltaTime;

                if (_emergeTimer <= 0)
                    tr.position = _spawnPos;
            }
        }
    }
}
