using System;
using UnityEngine;
using UnityEngine.Pool;
using Object = UnityEngine.Object;

namespace Beakstorm.Gameplay.Projectiles
{
    public class ProjectilePool : IDisposable
    {
        private readonly Projectile _prefab;

        private readonly ObjectPool<Projectile> _objectPool;
        private readonly Transform _poolParentTransform;
        
        public Projectile GetProjectile() => _objectPool.Get();


        public ProjectilePool(Projectile prefab, ProjectileManager manager)
        {
            _prefab = prefab;
            var defaultCapacity = prefab.DefaultCapacity;
            var maxCapacity = prefab.MaximumCapacity;
            
            
            var poolParent = new GameObject($"{_prefab.gameObject.name}_Pool");
            poolParent.transform.parent = manager.transform;
            _poolParentTransform = poolParent.transform;

            _objectPool = new ObjectPool<Projectile>(CreateProjectile, OnGetFromPool, OnReleaseToPool,
                OnDestroyPooledObject, false, defaultCapacity, maxCapacity);

        }
        
        public void Dispose()
        {
            if (_poolParentTransform)
                Object.Destroy(_poolParentTransform.gameObject);
                
            _objectPool.Dispose();
        }

        private Projectile CreateProjectile()
        {
            var projectileInstance = Object.Instantiate(_prefab, _poolParentTransform, true);
            projectileInstance.ObjectPool = _objectPool;
            projectileInstance.PoolTransform = _poolParentTransform;
            return projectileInstance;
        }

        private void OnGetFromPool(Projectile projectile)
        {
            projectile.ReParentToPoolTransform();
            projectile.gameObject.SetActive(true);
        }

        private void OnReleaseToPool(Projectile projectile)
        {
            projectile.gameObject.SetActive(false);
            projectile.ReParentToPoolTransform();
        }

        private void OnDestroyPooledObject(Projectile projectile)
        {
            if (projectile)
                Object.Destroy(projectile.gameObject);
        }
    }
}
