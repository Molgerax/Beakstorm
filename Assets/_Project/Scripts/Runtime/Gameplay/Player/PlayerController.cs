using Beakstorm.Gameplay.Damaging;
using Beakstorm.Pausing;
using UnityEngine;

namespace Beakstorm.Gameplay.Player
{
    public class PlayerController : MonoBehaviour, IDamageable
    {
        public static PlayerController Instance;

        [SerializeField] private int maxHealth = 100;

        private int _damageTaken = 0;
        private int _health;
        
        private Vector3 _oldPosition;
        private Vector3 _position;

        private Vector3 _velocity;
        

        public Vector3 Position => _position;
        public Vector3 Velocity => _velocity;
        public int Health => _health;
        public int DamageTaken => _damageTaken;

        private void Awake()
        {
            Instance = this;
            _health = maxHealth;
        }

        private void Update()
        {
            if (PauseManager.IsPaused)
                return;
            
            UpdatePosition();
        }

        private void UpdatePosition()
        {
            _oldPosition = _position;
            _position = transform.position;

            _velocity = (_position - _oldPosition) / Time.deltaTime;
        }

        public bool CanTakeDamage()
        {
            return true;
            return _health > 0;
        }

        public void TakeDamage(int damage)
        {
            _damageTaken += damage;
            _health -= damage;
            transform.position += Vector3.up * damage;
        }
    }
}
