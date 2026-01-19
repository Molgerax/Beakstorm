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
        
        private readonly int _defaultCapacity;
        private readonly int _maxCapacity;

        public int Capacity => _defaultCapacity;
        public int MaxCapacity => _maxCapacity;


        public ProjectilePool(Projectile prefab, ProjectileManager manager)
        {
            _prefab = prefab;
            _defaultCapacity = prefab.DefaultCapacity;
            _maxCapacity = prefab.MaximumCapacity;
            
            
            var poolParent = new GameObject($"{_prefab.gameObject.name}_Pool");
            poolParent.transform.parent = manager.transform;
            _poolParentTransform = poolParent.transform;

            _objectPool = new ObjectPool<Projectile>(CreateProjectile, OnGetFromPool, OnReleaseToPool,
                OnDestroyPooledObject, false, _defaultCapacity, _maxCapacity);

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
            //projectileInstance.gameObject.name = _prefab.gameObject.name + _objectPool.CountAll;
            projectileInstance.ObjectPool = _objectPool;
            projectileInstance.PoolTransform = _poolParentTransform;
            return projectileInstance;
        }

        private void OnGetFromPool(Projectile projectile)
        {
            projectile.ReParentToPoolTransform();
            projectile.Released = false;
            projectile.OnGetFromPool();
        }

        private void OnReleaseToPool(Projectile projectile)
        {
            projectile.OnReleaseToPool();
            projectile.ReParentToPoolTransform();
            projectile.Released = true;
        }

        private void OnDestroyPooledObject(Projectile projectile)
        {
            if (projectile)
                Object.Destroy(projectile.gameObject);
        }
    }
}
