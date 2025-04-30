using Beakstorm.Simulation.Collisions;
using UltEvents;
using UnityEngine;

namespace Beakstorm.Gameplay.Enemies
{
    public class EnemyController : MonoBehaviour
    {
        [SerializeField] private WeakPoint[] weakPoints;
        [SerializeField] private UltEvent onHealthZero;
        
        private int _maxHealth;
        private int _currentHealth;

        private EnemySpawner _spawner;
        
        public int CurrentHealth()
        {
            int health = 0;
            int maxHealth = 0;
            foreach (WeakPoint weakPoint in weakPoints)
            {
                if (weakPoint == null)
                    continue;

                health += weakPoint.CurrentHealth;
                maxHealth += weakPoint.MaxHealth;
            }

            _currentHealth = health;
            _maxHealth = maxHealth;
            return health;
        }
        
        private void OnEnable()
        {
            foreach (WeakPoint weakPoint in weakPoints)
            {
                if (weakPoint == null)
                    continue;
                
                weakPoint.onHealthZero += CheckHealth;
            }
        }

        public void Spawn(EnemySpawner spawner)
        {
            _spawner = spawner;
        }
        
        public void CheckHealth()
        {
            if (CurrentHealth() <= 0)
            {
                HealthZero();
            }
        }

        private void HealthZero()
        {
            if (_spawner)
                _spawner.OnDefeat();
            
            onHealthZero?.Invoke();
        }
    }
}
