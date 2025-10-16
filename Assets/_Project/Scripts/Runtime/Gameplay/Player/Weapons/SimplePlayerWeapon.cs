using Beakstorm.Gameplay.Projectiles;
using Beakstorm.Gameplay.Projectiles.Movement;
using Beakstorm.Simulation.Particles;
using UnityEngine;

namespace Beakstorm.Gameplay.Player.Weapons
{
    [CreateAssetMenu(menuName = "Beakstorm/PlayerWeaponData/SimpleProjectile", fileName = "SimplePlayerWeapon")]
    public class SimplePlayerWeapon : ScriptableObject
    {
        [Header("Display")]
        [SerializeField] protected Sprite displaySprite;
        [SerializeField] protected string displayName;
        
        [Header("Data")]
        [SerializeField] protected float fireDelay = 0.5f;
        [SerializeField] protected int ammoCost = 1;
        [SerializeField] protected float initialVelocity = 100;
        
        [SerializeField] protected Projectile projectilePrefab;
        [SerializeField] protected PheromoneBehaviourData behaviourData;
        [SerializeField] protected ProjectileMoveData moveData;

        protected float _fireCooldown;
        
        public Sprite DisplaySprite => displaySprite;
        public string DisplayName => displayName;
        public float FireDelay => fireDelay;
        public int AmmoCost => ammoCost;

        public float Cooldown01 => _fireCooldown / fireDelay;

        public virtual void OnMonoEnable() {}

        public virtual void OnMonoDisable() {}

        public virtual void UpdateWeapon(float deltaTime)
        {
            _fireCooldown = Mathf.Max(0, _fireCooldown - deltaTime);
        }
        
        protected virtual void FireSingleProjectile(FireInfo fireInfo)
        {
            fireInfo.Speed = initialVelocity;
            
            var projectileInstance = ProjectileManager.Instance.GetProjectile(projectilePrefab);
            var projTransform = projectileInstance.transform;
            projTransform.position = fireInfo.InitialPosition;


            if (projectileInstance.TryGetComponent(out SimpleMovementHandler movementHandler))
            {
                movementHandler.SetVelocity(fireInfo.LookDirection * initialVelocity);
                
                if (behaviourData)
                    movementHandler.Gravity = behaviourData.Gravity;
            }
            
            if (projectileInstance.TryGetComponent(out ProjectileMovementHandler projectileMovementHandler))
            {
                if (moveData)
                {
                    projectileMovementHandler.SetMovementData(moveData);
                    projectileMovementHandler.Initialize(fireInfo);
                }
            }


            if (projectileInstance.TryGetComponent(out PheromoneEmitter emitter))
            {
                emitter.SetBehaviourData(behaviourData);
                emitter.ResetEmitter();
            }
            
            if (projectileInstance.TryGetComponent(out TimedEvent timedEvent))
            {
                if (behaviourData)
                {
                    timedEvent.Duration = behaviourData.ProjectileLifeTime;
                }
            }
        }
        
        public virtual bool Fire(FireInfo fireInfo)
        {
            if (_fireCooldown > 0)
                return false;
        
            FireSingleProjectile(fireInfo);
            _fireCooldown = FireDelay;
            return true;
        }
    }
}
