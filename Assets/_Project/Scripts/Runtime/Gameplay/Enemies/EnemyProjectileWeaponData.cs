using Beakstorm.Gameplay.Projectiles;
using UnityEngine;

namespace Beakstorm.Gameplay.Enemies
{
    [CreateAssetMenu(menuName = "Beakstorm/Enemies/Weapon/Projectile", fileName = "EnemyWeaponData")]
    public class EnemyProjectileWeaponData : EnemyWeaponData
    {
        [SerializeField] protected Projectile projectilePrefab;
        [SerializeField] protected Projectile explosionLightPrefab;

        [SerializeField] protected AK.Wwise.Event fireSound;
        
        [SerializeField] private bool lifeTimeToPlayer = false;
        
        private ProjectilePool _pool;
        private ProjectilePool _lightPool;

        public override void Fire(Vector3 position, Vector3 direction, Vector3 targetPos, Transform t = null)
        {
            var projectileInstance = _pool.GetProjectile();
            var projTransform = projectileInstance.transform;
            projTransform.position = position;
            projTransform.rotation = Quaternion.LookRotation(direction);
            
            if (projectileInstance.TryGetComponent(out SimpleMovementHandler movementHandler))
                movementHandler.SetVelocity(direction * initialVelocity);
            
            if (lifeTimeToPlayer && projectileInstance.TryGetComponent(out TimedEvent timedEvent))
                timedEvent.Duration = Vector3.Distance(position, targetPos) / initialVelocity;

            projectileInstance.Spawn();

            SpawnLight(position, direction);

            if (t && fireSound != null)
                fireSound.Post(t.gameObject);
        }

        public override void OnMonoEnable()
        {
            _pool = ProjectileManager.Instance.GetPool(projectilePrefab);
            
            if (explosionLightPrefab)
                _lightPool = ProjectileManager.Instance.GetPool(explosionLightPrefab);
        }

        private void SpawnLight(Vector3 position, Vector3 direction)
        {
            if (!explosionLightPrefab)
                return;
                
            var light = _lightPool.GetProjectile();
            var projTransform = light.transform;
            projTransform.position = position;
            projTransform.rotation = Quaternion.LookRotation(direction);
            light.Spawn();
        }
    }
}
