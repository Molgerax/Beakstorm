using UltEvents;
using UnityEngine;

namespace Beakstorm.Simulation.Collisions
{
    public class WeakPoint : MonoBehaviour
    {
        [SerializeField] private int maxHealth = 100;
        [SerializeField] private int currentHealth;
        [SerializeField] private float radius = 1;

        [SerializeField] public UltEvent onHealthZero;

        public Vector4 PositionRadius => new(transform.position.x, transform.position.y, transform.position.z, radius);
        public int MaxHealth => maxHealth;
        public int CurrentHealth => currentHealth;
        public float CurrentHealth01 => (float) currentHealth / maxHealth;
        public bool IsDestroyed => currentHealth <= 0;
        
        private void OnEnable()
        {
            currentHealth = maxHealth;
            Subscribe();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void Subscribe()
        {
            if (!WeakPointManager.Instance.WeakPoints.Contains(this))
                WeakPointManager.Instance.WeakPoints.Add(this);
        }

        private void Unsubscribe()
        {   
            if (WeakPointManager.Instance.WeakPoints.Contains(this))
                WeakPointManager.Instance.WeakPoints.Remove(this);
        }
        
        public void ApplyDamage(int value)
        {
            currentHealth -= value;
            if (currentHealth <= 0)
                HealthZero();
        }
        
        public void HealthZero()
        {
            currentHealth = 0;
            onHealthZero?.Invoke();
            Unsubscribe();
        }
    }
}
