using UnityEngine;

namespace Beakstorm.Gameplay.Enemies
{
    public abstract class EnemyWeaponData : ScriptableObject
    {
        [SerializeField, Min(0)] protected float chargeTime = 5f;
        [SerializeField, Min(0)] protected float fireRate = 5f;
        [SerializeField, Min(1)] protected int projectilesInBurst = 1;
        [SerializeField, Min(0)] protected float detectionRange = 100f;
        [SerializeField, Range(0, 45)] protected float weaponSpread = 10f;
        [SerializeField] protected float initialVelocity = 10;

        [SerializeField] protected float rotationSpeed = 180;
        [SerializeField, Range(0.1f, 180)] protected float fireAngleDifference = 20;
        
        public float ChargeTime => chargeTime;
        public float FireRate => fireRate;
        public int ProjectilesInBurst => projectilesInBurst;
        public float DetectionRange => detectionRange;
        public float WeaponSpread => weaponSpread;
        public float InitialVelocity => initialVelocity;
        public float RotationSpeed => rotationSpeed;
        public double FireAngleDifference => fireAngleDifference;


        public virtual void OnMonoEnable() {}
        public virtual void OnMonoDisable() {}
        
        public abstract void Fire(Vector3 position, Vector3 direction, Vector3 targetPos, Transform t = null);
    }
}
