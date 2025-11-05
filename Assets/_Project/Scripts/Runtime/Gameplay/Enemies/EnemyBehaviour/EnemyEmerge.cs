using Beakstorm.Mapping.Tremble;
using UnityEngine;

namespace Beakstorm.Gameplay.Enemies.EnemyBehaviour
{
    public class EnemyEmerge : MonoBehaviour
    {
        [SerializeField] private float emergeTime = 10f;


        private float _emergeTimer;
        private Vector3 _emergePos, _spawnPos;

        private float LowerBound => MapWorldSpawn.Instance ? MapWorldSpawn.Instance.MapLowerBound : -256;

        public void Initialize()
        {
            _spawnPos = transform.position;
            _emergePos = _spawnPos;
            _emergePos.y = LowerBound;
            transform.position = _emergePos;

            _emergeTimer = emergeTime;
        }
    
        private void Update()
        {
            if (_emergeTimer > 0)
            {
                float t = 1 - (_emergeTimer / emergeTime);

                t = 1 - (1-t) * (1-t);
                
                transform.position = Vector3.Lerp(_emergePos, _spawnPos, t);
                _emergeTimer -= Time.deltaTime;

                if (_emergeTimer <= 0)
                    transform.position = _spawnPos;
            }
        }
    }
}
