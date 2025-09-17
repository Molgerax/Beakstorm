using System;
using Beakstorm.Gameplay.Encounters.Procedural;
using Beakstorm.Simulation.Collisions;
using UltEvents;
using UnityEngine;

namespace Beakstorm.Gameplay.Enemies
{
    public class EnemyController : MonoBehaviour
    {
        [SerializeField] private WeakPoint[] weakPoints;
        [SerializeField] private UltEvent onInitialize;
        [SerializeField] private UltEvent onHealthZero;

        [SerializeField, HideInInspector] private Bounds bounds;
        
        public event Action OnHealthZero;
        
        private int _maxHealth;
        private int _currentHealth;

        private Vector3 _spawnPos;

        private EnemyPool _pool;
        private EnemySpawner _spawner;

        private bool _isDefeated;
        public bool IsDefeated => _isDefeated;
        
        public int Health => _currentHealth;
        public float Health01 => (float) _currentHealth / _maxHealth;

        public Bounds Bounds => bounds;
        
        public void Create(EnemyPool pool)
        {
            _pool = pool;
            transform.position = Vector3.down * 512;
            gameObject.SetActive(false);
        }

        public void Spawn(Transform spawnPoint) => Spawn(spawnPoint.position, spawnPoint.rotation);
        public void Spawn(TransformData spawnPoint) => Spawn(spawnPoint.Position, spawnPoint.Rotation);
        public void Spawn(Vector3 position, Quaternion rotation)
        {
            _spawnPos = position;
            transform.SetPositionAndRotation(_spawnPos, rotation);
            
            foreach (WeakPoint weakPoint in weakPoints)
            {
                if (weakPoint == null)
                    continue;
                
                weakPoint.Initialize();
            }
            
            onInitialize?.Invoke();
            _isDefeated = false;
        }


        public void Despawn()
        {
            _pool.ReturnToPool(this);
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
        
        private void OnDisable()
        {
            foreach (WeakPoint weakPoint in weakPoints)
            {
                if (weakPoint == null)
                    continue;
                
                weakPoint.onHealthZero -= CheckHealth;
            }

            OnHealthZero = null;
        }

        private void OnValidate()
        {
            CalculateBounds();
        }

        private void CalculateBounds()
        {
            Bounds b = new Bounds();
            bool init = false;
            foreach (var render in GetComponentsInChildren<Renderer>())
            {
                if (init == false)
                {
                    b = render.bounds;
                    init = true;
                }
                b.Encapsulate(render.bounds);
            }

            bounds = b;
        }

        
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
        
        public void CheckHealth()
        {
            if (CurrentHealth() <= 0)
            {
                HealthZero();
            }
        }

        private void HealthZero()
        {
            _isDefeated = true;
            
            if (_spawner)
                _spawner.OnDefeat();
            
            OnHealthZero?.Invoke();
            onHealthZero?.Invoke();

            OnHealthZero = null;
        }
    }
}
