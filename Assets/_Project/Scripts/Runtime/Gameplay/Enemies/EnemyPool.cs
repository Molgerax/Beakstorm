using System;
using UnityEngine;
using UnityEngine.Pool;
using Object = UnityEngine.Object;

namespace Beakstorm.Gameplay.Enemies
{
    public class EnemyPool : IDisposable
    {
        private readonly EnemySO _enemySo;

        private readonly ObjectPool<EnemyController> _objectPool;
        private readonly Transform _poolParentTransform;
        
        public EnemyController GetEnemyObject() => _objectPool.Get();

        public void ReturnToPool(EnemyController enemy) => _objectPool.Release(enemy);

        public EnemyPool(EnemySO enemySo, EnemyPoolManager manager)
        {
            _enemySo = enemySo;
            var defaultCapacity = 16;
            var maxCapacity = 32;
            
            var poolParent = new GameObject($"{_enemySo.name}_Pool");
            poolParent.transform.parent = manager.transform;
            _poolParentTransform = poolParent.transform;

            _objectPool = new ObjectPool<EnemyController>(CreateEnemy, OnGetFromPool, OnReleaseToPool,
                OnDestroyPooledObject, false, defaultCapacity, maxCapacity);

        }
        
        public void Dispose()
        {
            if (_poolParentTransform)
                Object.Destroy(_poolParentTransform.gameObject);
                
            _objectPool.Dispose();
        }

        private EnemyController CreateEnemy()
        {
            var enemyInstance = Object.Instantiate(_enemySo.Prefab, _poolParentTransform, true);
            enemyInstance.Create(this);
            return enemyInstance;
        }

        private void OnGetFromPool(EnemyController enemy)
        {
            enemy.gameObject.SetActive(true);
        }

        private void OnReleaseToPool(EnemyController enemy)
        {
            enemy.gameObject.SetActive(false);
            enemy.transform.SetParent(_poolParentTransform);
        }

        private void OnDestroyPooledObject(EnemyController enemy)
        {
            if (enemy)
                Object.Destroy(enemy.gameObject);
        }
    }
}
