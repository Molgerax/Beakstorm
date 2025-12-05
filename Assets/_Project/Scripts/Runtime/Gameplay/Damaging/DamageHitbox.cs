using System.Collections.Generic;
using UltEvents;
using UnityEngine;

namespace Beakstorm.Gameplay.Damaging
{
    public class DamageHitbox : MonoBehaviour
    {
        [SerializeField, Min(0)] private int damageValue;
        [SerializeField] private UltEvent onCollide;
        
        private List<IDamageable> _damagedObjects;


        private void OnEnable()
        {
            if (_damagedObjects != null)
                _damagedObjects.Clear();
            else
                _damagedObjects = new(4);
        }


        private void Collide(IDamageable damageable)
        {
            if (!damageable.CanTakeDamage())
                return;
            if (_damagedObjects.Contains(damageable))
                return;
            
            damageable.TakeDamage(damageValue);
            _damagedObjects.Add(damageable);
            
            onCollide?.Invoke();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.TryGetComponent(out IDamageable damageable))
                Collide(damageable);
        }
        
        private void OnTriggerStay(Collider other)
        {
            if (other.TryGetComponent(out IDamageable damageable))
                Collide(damageable);
        }
    }
}
