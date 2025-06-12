using System;
using Beakstorm.Gameplay.Damaging;
using Beakstorm.Gameplay.Player.Weapons;
using Beakstorm.Pausing;
using UnityEngine;

namespace Beakstorm.Gameplay.Player
{
    public class PlayerController : MonoBehaviour, IDamageable
    {
        public static PlayerController Instance;

        [SerializeField] private int maxHealth = 100;
        [SerializeField] private SimplePlayerWeapon[] weapons = new SimplePlayerWeapon[0];
        
        private int _damageTaken = 0;
        private int _health;
        private int _selectedWeaponIndex;
        
        private Vector3 _oldPosition;
        private Vector3 _position;

        private Vector3 _velocity;
        

        public Vector3 Position => _position;
        public Vector3 Velocity => _velocity;
        public int Health => _health;
        public int DamageTaken => _damageTaken;

        public SimplePlayerWeapon SelectedWeapon => weapons.Length == 0 ? null : weapons[_selectedWeaponIndex];
        public int SelectedWeaponIndex
        {
            get => _selectedWeaponIndex;
            set => _selectedWeaponIndex = (weapons.Length == 0) ? 0 : (value % weapons.Length + weapons.Length) % weapons.Length;
        }

        private void Awake()
        {
            _health = maxHealth;
        }

        private void OnEnable()
        {
            Instance = this;
            
            foreach (SimplePlayerWeapon weapon in weapons)
            {
                weapon.OnMonoEnable();
            }
        }
        
        private void OnDisable()
        {
            foreach (SimplePlayerWeapon weapon in weapons)
            {
                weapon.OnMonoDisable();
            }

            if (Instance == this)
                Instance = null;
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
            //transform.position += Vector3.up * damage;
        }
    }
}
