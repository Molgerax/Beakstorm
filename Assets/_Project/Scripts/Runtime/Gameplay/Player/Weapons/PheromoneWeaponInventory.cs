using System;
using System.Collections.Generic;
using UnityEngine;

namespace Beakstorm.Gameplay.Player.Weapons
{
    public class PheromoneWeaponInventory : MonoBehaviour
    {
        [SerializeField] private PheromoneWeapon[] initWeapons = new PheromoneWeapon[0];

        public static PheromoneWeaponInventory Instance;
        
        private List<PheromoneWeaponInstance> _weaponInstances = new();
        public IReadOnlyList<PheromoneWeaponInstance> WeaponInstances => _weaponInstances;

        private int _selectedIndex;

        public event Action<PheromoneWeapon> OnSelectWeapon; 
        
        public int SelectedIndex
        {
            get => _selectedIndex;
            set
            {
                int previous = _selectedIndex;
                _selectedIndex = (_weaponInstances.Count == 0)
                    ? 0
                    : (value % _weaponInstances.Count + _weaponInstances.Count) % _weaponInstances.Count;
                
                if (_selectedIndex != previous)
                    OnSelectWeapon?.Invoke(_weaponInstances.Count == 0 ? null : _weaponInstances[_selectedIndex]);
            }
        }

        public PheromoneWeaponInstance SelectedWeapon
        {
            get
            {
                //SelectedIndex = Mathf.Clamp(SelectedIndex, 0, _weaponInstances.Count - 1);
                SelectedIndex += 0;
                
                if (_weaponInstances.Count == 0)
                    return null;

                return _weaponInstances[_selectedIndex];
            }
        }
        
        public void Initialize()
        {
            _weaponInstances = new List<PheromoneWeaponInstance>();

            for (var index = 0; index < initWeapons.Length; index++)
            {
                PheromoneWeapon weapon = initWeapons[index];
                _weaponInstances.Add(new PheromoneWeaponInstance(weapon, index == 0));
            }
            OnSelectWeapon?.Invoke(_weaponInstances.Count == 0 ? null : _weaponInstances[0]);
        }

        private void Awake()
        {
            Instance = this;
            Initialize();
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        private void Update()
        {
            UpdateWeapons(Time.deltaTime);
        }
        
        private void UpdateWeapons(float dt)
        {
            for (var index = _weaponInstances.Count - 1; index >= 0; index--)
            {
                PheromoneWeaponInstance instance = _weaponInstances[index];
                instance.Update(dt);

                if (!instance.AutoReload && instance.Count == 0 && !ReferenceEquals(SelectedWeapon, instance))
                    _weaponInstances.RemoveAt(index);
            }
        }

        public void GiveWeapon(PheromoneWeapon weapon)
        {
            foreach (var instance in _weaponInstances)
            {
                if (instance.Equals(weapon))
                {
                    instance.AddWeapon(weapon);
                    return;
                }
            }
            _weaponInstances.Add(new(weapon));
        }
    }
}
