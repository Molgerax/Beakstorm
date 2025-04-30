using UnityEngine;

namespace Beakstorm.Gameplay.Enemies
{
    public abstract class EnemyWeaponData : ScriptableObject
    {
        [SerializeField, Min(0)] protected float chargeTime = 5f;
        [SerializeField, Min(0)] protected float detectionRange = 100f;
        [SerializeField, Range(0, 45)] protected float weaponSpread = 10f;
        [SerializeField] protected float initialVelocity = 10;

        public float ChargeTime => chargeTime;
        public float DetectionRange => detectionRange;
        public float WeaponSpread => weaponSpread;
        public float InitialVelocity => initialVelocity;

        
        public virtual void OnMonoEnable() {}
        public virtual void OnMonoDisable() {}
        
        public abstract void Fire(Vector3 position, Vector3 direction);
    }
}
