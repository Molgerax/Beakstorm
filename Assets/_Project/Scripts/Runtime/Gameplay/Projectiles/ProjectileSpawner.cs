using UnityEngine;

namespace Beakstorm.Gameplay.Projectiles
{
    public class ProjectileSpawner : MonoBehaviour
    {
        [SerializeField] private Projectile projectilePrefab;

        private ProjectilePool _pool;
        
        public Projectile GetProjectile() => _pool?.GetProjectile();
        
        private void Awake()
        {
            _pool = ProjectileManager.Instance.GetPool(projectilePrefab);
        }

        public Projectile Spawn()
        {
            Projectile projectile = GetProjectile();
            var t = transform;
            var projectileTransform = projectile.transform;
            projectileTransform.position = t.position;
            projectileTransform.rotation = t.rotation;

            projectile.Spawn();
            return projectile;
        }

        public Projectile SpawnWithParent(Transform parent)
        {
            Projectile projectile = GetProjectile();
            var t = transform;
            parent = parent ? parent : t;
            
            var projectileTransform = projectile.transform;
            projectileTransform.position = t.position;
            projectileTransform.rotation = t.rotation;
            projectileTransform.SetParent(parent, true);
            
            projectile.Spawn();
            return projectile;
        }
        
        public Projectile SpawnWithScale()
        {
            Projectile projectile = GetProjectile();
            var t = transform;
            var projectileTransform = projectile.transform;
            projectileTransform.position = t.position;
            projectileTransform.rotation = t.rotation;
            projectileTransform.localScale = t.lossyScale;
            
            projectile.Spawn();
            return projectile;
        }
    }
}