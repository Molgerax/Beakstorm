using Beakstorm.Simulation.Collisions;
using UltEvents;
using UnityEngine;

namespace Beakstorm.Gameplay.Enemies
{
    public class EnemyController : MonoBehaviour
    {
        [SerializeField] private WeakPoint[] weakPoints;
        [SerializeField] private UltEvent onHealthZero;

        [SerializeField] private float emergeTime = 10;
        
        private int _maxHealth;
        private int _currentHealth;

        private Vector3 _spawnPos;
        private Vector3 _emergePos;
        private float _emergeTimer;
        
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

        private void Update()
        {
            if (_emergeTimer > 0)
            {
                float t = 1 - (_emergeTimer / emergeTime);

                t = 1 - (1-t) * (1-t);
                
                transform.position = Vector3.Lerp(_emergePos, _spawnPos, t);
                _emergeTimer -= Time.deltaTime;

                if (_emergeTimer <= 0)
                    transform.position = _spawnPos;
            }
        }


        public void Spawn(EnemySpawner spawner)
        {
            _spawner = spawner;

            _emergeTimer = emergeTime;

            _spawnPos = transform.position;
            _emergePos = _spawnPos;
            _emergePos.y = -256;
            transform.position = _emergePos;
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
