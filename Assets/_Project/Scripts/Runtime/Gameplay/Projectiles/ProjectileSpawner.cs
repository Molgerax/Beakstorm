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

        public void Spawn()
        {
            Projectile projectile = GetProjectile();
            var t = transform;
            var projectileTransform = projectile.transform;
            projectileTransform.position = t.position;
            projectileTransform.rotation = t.rotation;
        }

        public void SpawnWithScale()
        {
            Projectile projectile = GetProjectile();
            var t = transform;
            var projectileTransform = projectile.transform;
            projectileTransform.position = t.position;
            projectileTransform.rotation = t.rotation;
            projectileTransform.localScale = t.lossyScale;
        }
    }
}