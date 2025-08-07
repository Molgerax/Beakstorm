using UnityEngine;

namespace Beakstorm.Gameplay.Enemies
{
    [CreateAssetMenu(fileName = "Enemy", menuName = "Beakstorm/Enemies/Enemy")]
    [System.Serializable]
    public class EnemySO : ScriptableObject
    {
        [SerializeField] private EnemyController prefab;
        [SerializeField] private int dangerRating = 1;

        public EnemyController Prefab => prefab;
        public int DangerRating => dangerRating;
    }
}
