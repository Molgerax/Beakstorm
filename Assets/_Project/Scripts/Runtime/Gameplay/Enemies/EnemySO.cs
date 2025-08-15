using UnityEngine;

namespace Beakstorm.Gameplay.Enemies
{
    [CreateAssetMenu(fileName = "Enemy", menuName = "Beakstorm/Enemies/Enemy")]
    [System.Serializable]
    public class EnemySO : ScriptableObject
    {
        [SerializeField] private EnemyController prefab;
        [SerializeField] private int dangerRating = 1;

        [SerializeField, HideInInspector] private Bounds bounds;

        public EnemyController Prefab => prefab;
        public int DangerRating => dangerRating;

        public void SetBounds(Bounds b)
        {
            bounds = b;
        }
        
        public EnemyController GetEnemyInstance()
        {
            return EnemyPoolManager.Instance.GetEnemy(this);
        }
    }
}
