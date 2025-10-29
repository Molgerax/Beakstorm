using UnityEngine;

namespace Beakstorm.Gameplay.Projectiles
{
    public class TrailSpawner : MonoBehaviour
    {
        [SerializeField] private Projectile projectilePrefab;

        private ProjectilePool _pool;

        private Projectile _spawnedProjectile;
        
        private Projectile GetProjectile() => _pool?.GetProjectile();
        
        private void Awake()
        {
            _pool = ProjectileManager.Instance.GetPool(projectilePrefab);
        }

        private void ReturnIfSpawned()
        {
            if (_spawnedProjectile)
                _spawnedProjectile.Deactivate();
            _spawnedProjectile = null;
        }

        public Projectile Spawn()
        {
            ReturnIfSpawned();
            
            _spawnedProjectile = GetProjectile();

            var t = transform;
            var projectileTransform = _spawnedProjectile.transform;
            projectileTransform.SetParent(t, true);
            projectileTransform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            //projectileTransform.position = t.position;
            //projectileTransform.rotation = t.rotation;

            _spawnedProjectile.Spawn();
            return _spawnedProjectile;
        }

        public void Detach()
        {
            if (_spawnedProjectile)
                _spawnedProjectile.Detach();
            _spawnedProjectile = null;
        }
        
        public void Despawn()
        {
            ReturnIfSpawned();
        }
    }
}
