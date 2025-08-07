using System.Collections.Generic;
using UnityEngine;

namespace Beakstorm.Gameplay.Projectiles
{
    public class ProjectileManager : MonoBehaviour
    {
        public static ProjectileManager Instance;

        private Dictionary<Projectile, ProjectilePool> _projectilePools = new();

        private void Awake()
        {
            Instance = this;
        }

        private void OnDestroy()
        {
            foreach (var projectilePool in _projectilePools)
            {
                projectilePool.Value?.Dispose();
            }

            if (Instance == this)
                Instance = null;
        }

        public ProjectilePool GetPool(Projectile prefab)
        {
            if (_projectilePools.TryGetValue(prefab, out ProjectilePool pool))
                return pool;
            
            pool = new ProjectilePool(prefab, this);
            _projectilePools.Add(prefab, pool);
            return pool;
        }
        
        public Projectile GetProjectile(Projectile prefab)
        {
            var pool = GetPool(prefab);
            return pool.GetProjectile();
        }
    }
}
