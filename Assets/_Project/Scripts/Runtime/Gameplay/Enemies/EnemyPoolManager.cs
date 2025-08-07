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
            if (Instance != this)
                return;
            
            
            Instance = null;
        }

        public EnemyPool GetEnemyPool(EnemySO enemySo)
        {
            if (_enemyPools.ContainsKey(enemySo))
                return _enemyPools[enemySo];

            var enemyPool = new EnemyPool(enemySo, this);
            _enemyPools.Add(enemySo, enemyPool);
            return enemyPool;
        }

        public EnemyController GetEnemy(EnemySO enemySo)
        {
            var enemyPool = GetEnemyPool(enemySo);
            return enemyPool.GetEnemyObject();
        }
    }
}
