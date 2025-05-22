using Beakstorm.Gameplay.Projectiles;
using Beakstorm.Simulation.Particles;
using UnityEngine;

namespace Beakstorm.Gameplay.Player.Weapons
{
    [CreateAssetMenu(menuName = "Beakstorm/PlayerWeaponData/SimpleProjectile", fileName = "SimplePlayerWeapon")]
    public class SimplePlayerWeapon : ScriptableObject
    {
        [SerializeField] protected float fireDelay = 0.5f;
        [SerializeField] protected int ammoCost = 1;
        [SerializeField] protected float initialVelocity = 100;
        
        [SerializeField] protected Projectile projectilePrefab;

        protected ProjectilePool _pool;

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
                movementHandler.SetVelocity(direction * initialVelocity);
            
            if (projectileInstance.TryGetComponent(out PheromoneEmitter emitter))
                emitter.ResetEmitter();
        }
    }
}
