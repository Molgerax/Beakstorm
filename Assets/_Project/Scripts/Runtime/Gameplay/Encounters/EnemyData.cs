using Beakstorm.Gameplay.Enemies;
using UnityEngine;

namespace Beakstorm.Gameplay.Encounters
{
    [CreateAssetMenu(menuName = "Beakstorm/Enemies/EnemyData", fileName = "EnemyData")]
    public class EnemyData : ScriptableObject
    {
        [SerializeField] private EnemyController enemyPrefab;

        public EnemyController EnemyPrefab => enemyPrefab;
    }
}
