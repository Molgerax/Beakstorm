using System;
using UnityEngine;
using UnityEngine.Pool;

namespace Beakstorm.Gameplay.Projectiles
{
    public class ProjectilePool : IDisposable
    {
        private readonly Projectile _prefab;
        private readonly int _defaultCapacity;
        private readonly int _maxCapacity;

        private readonly ObjectPool<Projectile> _objectPool;
        private readonly Transform _poolParentTransform;
        
        public Projectile GetProjectile() => _objectPool.Get();


        public ProjectilePool(Projectile prefab, int defaultCapacity = 16, int maxCapacity = 128)
        {
            _prefab = prefab;
            _defaultCapacity = defaultCapacity;
            _maxCapacity = maxCapacity;
            
            
            var poolParent = new GameObject($"{_prefab.gameObject.name}_Pool");
            _poolParentTransform = poolParent.transform;

            _objectPool = new ObjectPool<Projectile>(CreateProjectile, OnGetFromPool, OnReleaseToPool,
                OnDestroyPooledObject, false, _defaultCapacity, _maxCapacity);

        }
        
        public void Dispose()
        {
            _objectPool.Dispose();
        }

        private Projectile CreateProjectile()
        {
            var projectileInstance = UnityEngine.Object.Instantiate(_prefab, _poolParentTransform, true);
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
            if (projectile)
                UnityEngine.Object.Destroy(projectile.gameObject);
        }
    }
}
