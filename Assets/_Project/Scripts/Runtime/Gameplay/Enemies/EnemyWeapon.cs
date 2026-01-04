using Beakstorm.Gameplay.Player;
using Beakstorm.Pausing;
using UltEvents;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Beakstorm.Gameplay.Enemies
{
    public class EnemyWeapon : MonoBehaviour
    {
        [SerializeField] private EnemyWeaponData weaponData;

        [SerializeField] private Transform[] firePoints;

        [SerializeField] private Transform weaponPivot;

        [SerializeField, Range(0, 180)] private float limitAngle = 90;
        [SerializeField] private UltEvent onFire;
        
        private float _chargeTime = 0;
        private int _ammoCount;
        private float _fireDelay;

        private float _currentAngle;

        private Transform WeaponPivot => weaponPivot ? weaponPivot : transform;

        private int _firePointIndex;

        private Transform GetFirePoint()
        {
            if (firePoints == null || firePoints.Length == 0)
                return WeaponPivot;
            
            _firePointIndex = _firePointIndex % firePoints.Length;
            Transform t = firePoints[_firePointIndex];
            _firePointIndex = (_firePointIndex + 1) % firePoints.Length;
            
            if (t == null)
                return WeaponPivot;
            return t;
        }

        private void OnEnable()
        {
            weaponData.OnMonoEnable();
        }
        
        private void OnDisable()
        {
            weaponData.OnMonoDisable();
        }

        private void Update()
        {
            if (PauseManager.IsPaused)
                return;
         
            Tick(Time.deltaTime);
            AdjustPivot(Time.deltaTime);
        }


        private void Tick(float deltaTime)
        {
            if (!weaponData)
                return;
            
            if (IsPlayerInRange() && _ammoCount > 0)
            {
                _fireDelay += deltaTime;

                if (_fireDelay > weaponData.FireRate)
                {
                    Fire();
                    _fireDelay = 0;
                    _ammoCount--;
                }
            }
            else
            {
                _fireDelay = Mathf.MoveTowards(_fireDelay, 0, deltaTime);
                
                if (_ammoCount < weaponData.ProjectilesInBurst)
                    _chargeTime += deltaTime;
            }

            if (_ammoCount == 0)
                _chargeTime += deltaTime;
                
            if (_chargeTime > weaponData.ChargeTime)
            {
                _chargeTime = 0;
                _ammoCount = weaponData.ProjectilesInBurst;
            }
        }

        private void AdjustPivot(float deltaTime)
        {
            if (!weaponPivot)
                return;
            if (!IsPlayerInRange())
                return;

            Vector3 direction = GetPredictionDirection(weaponPivot);
            
            Vector3 clampedDirection = Vector3.RotateTowards(transform.forward, direction.normalized, limitAngle * Mathf.Deg2Rad, 1);

            Quaternion targetLook = Quaternion.LookRotation(clampedDirection);
            
            weaponPivot.rotation = Quaternion.RotateTowards(weaponPivot.rotation, targetLook, weaponData.RotationSpeed * deltaTime);
        }

        private Vector3 GetPredictedPosition(Vector3 firePos)
        {
            if (!PlayerController.Instance)
                return Vector3.zero;
        
            Vector3 playerPos = PlayerController.Instance.Position;
            Vector3 playerVel = PlayerController.Instance.Velocity;

            Vector3 predictedPos = playerPos + playerVel * Vector3.Distance(playerPos, firePos) / weaponData.InitialVelocity;
            return predictedPos;
        }
        
        private Vector3 GetPredictionDirection(Transform firePoint = null)
        {
            if (!firePoint)
                firePoint = WeaponPivot;
            
            if (!PlayerController.Instance)
                return firePoint.forward;

            var position = firePoint.position;
            Vector3 predictedPos = GetPredictedPosition(position);
            
            Vector3 direction = (predictedPos - position).normalized;
            return direction;
        }
        
        private void Fire()
        {
            if (!weaponData)
                return;
            
            if (!PlayerController.Instance)
                return ;

            Transform t = GetFirePoint();
            Vector3 pos = t.position;

            Vector3 direction = GetPredictionDirection(t);
            Vector3 predictedPos = GetPredictedPosition(pos);
            
            direction = WeaponPivot.forward;
            float offset = Vector3.Angle(direction, predictedPos - pos);
            
            direction = GetShotDirection(direction);
            
            _currentAngle = Vector3.Angle(transform.forward, direction);
            if (_currentAngle > limitAngle)
                return;
            
            if (offset > weaponData.FireAngleDifference)
                return;
            
            weaponData.Fire(pos, direction.normalized, predictedPos, t);
            
            onFire?.Invoke();
        }
        
        private Vector3 GetShotDirection(Vector3 initDirection)
        {
            float t = Random.Range(0f, weaponData.WeaponSpread) / 360f;
            
            float inclination = Mathf.Acos(1 - 2 * t);
            float azimuth = Random.Range(0, Mathf.PI * 2);

            float x = Mathf.Sin(inclination) * Mathf.Cos(azimuth);
            float y = Mathf.Sin(inclination) * Mathf.Sin(azimuth);
            float z = Mathf.Cos(inclination);

            Vector3 target = new Vector3(x, y, z);
            Quaternion initDir = Quaternion.LookRotation(initDirection);

            return initDir * target;
        }
        
        private bool IsPlayerInRange()
        {
            if (!weaponData)
                return false;

            if (!PlayerController.Instance)
                return false;
            
            Vector3 playerPos = PlayerController.Instance.transform.position;

            float dist = Vector3.Distance(playerPos, transform.position);
            if (dist > weaponData.DetectionRange)
                return false;

            Vector3 dir = playerPos - transform.position;
            Ray ray = new(transform.position, dir);
            return true; //Physics.Raycast(ray, dir.magnitude - 10, Int32.MaxValue, QueryTriggerInteraction.Ignore) == false;
        }

        private void OnDrawGizmosSelected()
        {
            if (!weaponData)
                return;
            
            var position = transform.position;
            var forward = transform.forward;
            
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(position, weaponData.DetectionRange);
            
            Gizmos.color = Color.green;
            Gizmos.DrawRay(position, forward * 5);
            Gizmos.DrawRay(position, Quaternion.AngleAxis(limitAngle, transform.right) * forward * 5);
        }
    }
}
