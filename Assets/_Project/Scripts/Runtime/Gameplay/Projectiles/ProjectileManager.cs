using System.Collections.Generic;
using UnityEngine;

namespace Beakstorm.Gameplay.Projectiles
{
    public class ProjectileManager : MonoBehaviour
    {
        private static ProjectileManager _instance;
        public static ProjectileManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    var gameObject = new GameObject($"{nameof(ProjectileManager)}");
                    _instance = gameObject.AddComponent<ProjectileManager>();
                }
                return _instance;
            }
        }
        
        private Dictionary<Projectile, ProjectilePool> ProjectilePools = new();

        private void Awake()
        {
            _instance = this;
        }

        private void OnDestroy()
        {
            foreach (var projectilePool in ProjectilePools)
            {
                projectilePool.Value?.Dispose();
            }
        }

        public static ProjectilePool GetPool(Projectile prefab)
        {
            ProjectilePool pool;
            
            if (Instance.ProjectilePools.TryGetValue(prefab, out pool))
                return pool;
            
            pool = new ProjectilePool(prefab);
            Instance.ProjectilePools.Add(prefab, pool);
            return pool;
        }
    }
}
