using System;
using System.Collections.Generic;
using UnityEngine;

namespace Beakstorm.Gameplay.Damaging
{
    public class DamageHitbox : MonoBehaviour
    {
        [SerializeField, Min(0)] private int damageValue;
        
        private List<IDamageable> _damagedObjects;


        private void OnEnable()
        {
            _damagedObjects = new();
        }


        private void OnTriggerStay(Collider other)
        {
            if (!other.TryGetComponent(out IDamageable damageable))
                return;
            
            if (!damageable.CanTakeDamage())
                return;
            if (_damagedObjects.Contains(damageable))
                return;
            
            damageable.TakeDamage(damageValue);
            _damagedObjects.Add(damageable);
        }
    }
}
