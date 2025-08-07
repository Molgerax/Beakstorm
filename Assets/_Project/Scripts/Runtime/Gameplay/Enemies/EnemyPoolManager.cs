using System.Collections.Generic;
using UnityEngine;

namespace Beakstorm.Gameplay.Enemies
{
    public class EnemyPoolManager : MonoBehaviour
    {
        public static EnemyPoolManager Instance;
        
        private Dictionary<EnemySO, EnemyPool> _enemyPools = new Dictionary<EnemySO, EnemyPool>(8);

        private void Awake()
        {
            Instance = this;
        }

        private void OnDestroy()
        {
            foreach (var enemyPool in _enemyPools)
            {
                enemyPool.Value?.Dispose();
            }
            
            if (Instance == this)
                Instance = null;
        }

        public EnemyPool GetEnemyPool(EnemySO enemySo)
        {
            if (_enemyPools.TryGetValue(enemySo, out EnemyPool pool))
                return pool;

            pool = new EnemyPool(enemySo, this);
            _enemyPools.Add(enemySo, pool);
            return pool;
        }

        public EnemyController GetEnemy(EnemySO enemySo)
        {
            var enemyPool = GetEnemyPool(enemySo);
            return enemyPool.GetEnemyObject();
        }
    }
}
