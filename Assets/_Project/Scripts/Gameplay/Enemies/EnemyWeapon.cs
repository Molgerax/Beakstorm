using System;
using Beakstorm.Gameplay.Player;
using Beakstorm.Gameplay.Projectiles;
using UnityEngine;

namespace Beakstorm.Gameplay.Enemies
{
    public class EnemyWeapon : MonoBehaviour
    {
        [SerializeField] private EnemyWeaponData weaponData;

        [SerializeField] private Transform weaponPivot;

        private float _chargeTime = 0;


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
            
            weaponPivot.rotation = Quaternion.LookRotation(direction.normalized);
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
            
            weaponData.Fire(pos, direction.normalized);
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
            
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(transform.position, weaponData.DetectionRange);
        }
    }
}
