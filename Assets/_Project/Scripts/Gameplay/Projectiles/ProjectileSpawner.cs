using UnityEngine;
using UnityEngine.Pool;

namespace Beakstorm.Gameplay.Projectiles
{
    public class ProjectileSpawner : MonoBehaviour
    {
        [SerializeField] private Projectile projectilePrefab;
        [SerializeField] private int defaultCapacity = 20;
        [SerializeField] private int maxCapacity = 100;

        private IObjectPool<Projectile> _objectPool;
        private Transform _poolParentTransform;
        
        public Projectile GetProjectile() => _objectPool.Get();
        
        private void Awake()
        {
            var poolParent = new GameObject("PoolParent");
            _poolParentTransform = poolParent.transform;

            _objectPool = new ObjectPool<Projectile>(CreateProjectile, OnGetFromPool, OnReleaseToPool,
                OnDestroyPooledObject, false, defaultCapacity, maxCapacity);
        }


        private Projectile CreateProjectile()
        {
            var projectileInstance = Instantiate(projectilePrefab, _poolParentTransform, true);
            projectileInstance.ObjectPool = _objectPool;
            return projectileInstance;
        }

        private void OnGetFromPool(Projectile projectile)
        {
            projectile.gameObject.SetActive(true);
        }

        private void OnReleaseToPool(Projectile projectile)
        {
            projectile.gameObject.SetActive(false);
        }

        private void OnDestroyPooledObject(Projectile projectile)
        {
            Destroy(projectile.gameObject);
        }
    }
}