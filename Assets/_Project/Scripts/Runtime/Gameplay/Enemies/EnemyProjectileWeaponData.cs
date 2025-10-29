using Beakstorm.Gameplay.Projectiles;
using UnityEngine;

namespace Beakstorm.Gameplay.Enemies
{
    [CreateAssetMenu(menuName = "Beakstorm/Enemies/Weapon/Projectile", fileName = "EnemyWeaponData")]
    public class EnemyProjectileWeaponData : EnemyWeaponData
    {
        [SerializeField] protected Projectile projectilePrefab;

        private ProjectilePool _pool;
        

        public override void Fire(Vector3 position, Vector3 direction)
        {
            var projectileInstance = _pool.GetProjectile();
            var projTransform = projectileInstance.transform;
            projTransform.position = position;

            if (projectileInstance.TryGetComponent(out SimpleMovementHandler movementHandler))
                movementHandler.SetVelocity(direction * initialVelocity);
            
            projectileInstance.Spawn();
        }

        public override void OnMonoEnable()
        {
            _pool = ProjectileManager.Instance.GetPool(projectilePrefab);
        }
    }
}
