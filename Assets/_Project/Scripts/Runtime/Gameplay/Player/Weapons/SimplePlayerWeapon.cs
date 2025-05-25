using Beakstorm.Gameplay.Projectiles;
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

        protected ProjectilePool _pool;

        public Sprite DisplaySprite => displaySprite;
        public string DisplayName => displayName;
        public float FireDelay => fireDelay;
        public int AmmoCost => ammoCost;

        public virtual void OnMonoEnable()
        {
            _pool = ProjectileManager.GetPool(projectilePrefab);
        }

        public virtual void OnMonoDisable() {}
        
        public virtual void Fire(Vector3 position, Vector3 direction)
        {
            var projectileInstance = _pool.GetProjectile();
            var projTransform = projectileInstance.transform;
            projTransform.position = position;

            if (projectileInstance.TryGetComponent(out SimpleMovementHandler movementHandler))
            {
                movementHandler.SetVelocity(direction * initialVelocity);
                
                if (behaviourData)
                    movementHandler.Gravity = behaviourData.Gravity;
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
    }
}
