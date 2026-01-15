using System;
using UnityEngine;

namespace Beakstorm.Gameplay.Player.Weapons
{
    public class PheromoneWeaponInstance : IEquatable<PheromoneWeapon>
    {
        private float _cooldown;
        private int _count;

        private float _reloadTime;

        public float Cooldown => _cooldown;
        public float Cooldown01 => _cooldown / _weapon.FireDelay;
        public int Count => _count;

        public float ReloadTime => _reloadTime;
        public float ReloadTime01 => _autoReload ? _reloadTime / _weapon.ReloadTime : 0;
        
        private readonly PheromoneWeapon _weapon;
        public PheromoneWeapon Weapon => _weapon;

        private readonly bool _autoReload;
        public bool AutoReload => _autoReload;

        public PheromoneWeaponInstance(PheromoneWeapon weapon, bool autoReload = false)
        {
            _weapon = weapon;
            _cooldown = 0;
            _count = weapon.PickupCount;

            _autoReload = autoReload;
            if (autoReload)
                _count = weapon.MaxReloadCount;
        }

        public void Update(float dt)
        {
            _cooldown = Mathf.MoveTowards(_cooldown, 0, dt);
            TickReload(dt);
        }

        private void TickReload(float dt)
        {
            if (!_autoReload)
                return;

            if (_count >= _weapon.MaxReloadCount)
            {
                _count = _weapon.MaxReloadCount;
                _reloadTime = 0;
                return;
            }

            _reloadTime = Mathf.MoveTowards(_reloadTime, _weapon.ReloadTime, dt);

            if (_reloadTime >= _weapon.ReloadTime)
            {
                _count++;
                _reloadTime = 0;
            }
        }

        public bool TryFire(FireInfo fireInfo)
        {
            if (_cooldown > 0 || _count == 0)
                return false;
            
            _weapon.FireProjectile(fireInfo);
            if (_count > 0)
                _count = Mathf.Max(0, _count - _weapon.AmmoCost);
            _cooldown = _weapon.FireDelay;
            return true;
        }

        public void AddWeapon(PheromoneWeapon weapon)
        {
            if (weapon == _weapon)
                _count += _weapon.PickupCount;
        }

        public override bool Equals(object obj)
        {
            if (obj is PheromoneWeaponInstance instance)
                return _weapon == instance.Weapon;
            
            if (obj is PheromoneWeapon weapon)
                return _weapon == weapon;
            
            return false;
        }

        public bool Equals(PheromoneWeapon other)
        {
            return _weapon.Equals(other) == other;
        }
        
        public override int GetHashCode()
        {
            return _weapon.GetHashCode();
        }

        public static implicit operator PheromoneWeapon(PheromoneWeaponInstance w) => w?.Weapon;
    }
}