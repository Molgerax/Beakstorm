using System;
using Beakstorm.Gameplay.Player;
using Beakstorm.Gameplay.Projectiles;
using Beakstorm.Pausing;
using UltEvents;
using UnityEngine;

namespace Beakstorm.Gameplay.Enemies
{
    public class EnemyWeapon : MonoBehaviour
    {
        [SerializeField] private EnemyWeaponData weaponData;

        [SerializeField] private Transform weaponPivot;

        [SerializeField, Range(0, 180)] private float limitAngle = 90;
        [SerializeField] private UltEvent onFire;
        
        private float _chargeTime = 0;

        private Vector3 _initForward;
        private float _currentAngle;
        


        private void Awake()
        {
            _initForward = transform.forward;
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
            AdjustPivot();
        }


        private void Tick(float deltaTime)
        {
            if (!weaponData)
                return;
            
            if (IsPlayerInRange())
            {
                _chargeTime += deltaTime;
            }
            else
            {
                _chargeTime -= deltaTime;
                _chargeTime = Mathf.Max(_chargeTime, 0);
            }
            
            if (_chargeTime > weaponData.ChargeTime)
            {
                _chargeTime = 0;
                Fire();
            }
        }

        private void AdjustPivot()
        {
            if (!weaponPivot)
                return;
            if (!IsPlayerInRange())
                return;
            
            Vector3 playerPos = PlayerController.Instance.Position;
            Vector3 playerVel = PlayerController.Instance.Velocity;
            
            Vector3 pos = transform.position;

            Vector3 predictedPos = playerPos + playerVel * Vector3.Distance(playerPos, pos) / weaponData.InitialVelocity;
            
            Vector3 direction = predictedPos - pos;

            Vector3 clampedDirection = Vector3.RotateTowards(_initForward, direction.normalized, limitAngle * Mathf.Deg2Rad, 1);

            weaponPivot.rotation = Quaternion.LookRotation(clampedDirection);
        }
        
        private void Fire()
        {
            if (!weaponData)
                return;
            
            if (!PlayerController.Instance)
                return ;

            Vector3 playerPos = PlayerController.Instance.Position;
            Vector3 playerVel = PlayerController.Instance.Velocity;
            
            Vector3 pos = transform.position;

            Vector3 predictedPos = playerPos + playerVel * Vector3.Distance(playerPos, pos) / weaponData.InitialVelocity;
            
            Vector3 direction = predictedPos - pos;
            
            _currentAngle = Vector3.Angle(_initForward, direction);
            if (_currentAngle > limitAngle)
                return;
            
            weaponData.Fire(pos, direction.normalized);
            
            onFire?.Invoke();
        }
        
        
        private bool IsPlayerInRange()
        {
            if (!weaponData)
                return false;

            if (!PlayerController.Instance)
                return false;
            
            Vector3 playerPos = PlayerController.Instance.transform.position;

            float dist = Vector3.Distance(playerPos, transform.position);
            return dist < weaponData.DetectionRange;
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
