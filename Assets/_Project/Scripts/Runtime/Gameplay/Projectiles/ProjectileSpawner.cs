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
    }
}