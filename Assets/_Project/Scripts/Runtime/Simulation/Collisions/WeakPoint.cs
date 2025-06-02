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

        private MeshRenderer _renderer;
        private MaterialPropertyBlock _propBlock;
        
        private void OnEnable()
        {
            currentHealth = maxHealth;
            Subscribe();
            
            _propBlock ??= new MaterialPropertyBlock();
            _renderer = GetComponent<MeshRenderer>();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void Subscribe()
        {
            if (!WeakPointManager.WeakPoints.Contains(this))
                WeakPointManager.WeakPoints.Add(this);
        }

        private void Unsubscribe()
        {   
            if (WeakPointManager.WeakPoints.Contains(this))
                WeakPointManager.WeakPoints.Remove(this);
        }
        
        public void ApplyDamage(int value)
        {
            currentHealth -= value;

            if (_renderer)
            {
                _renderer.GetPropertyBlock(_propBlock);
                _propBlock.SetColor("_BaseColor", new Color(CurrentHealth01, 0, 0, 1));
                _renderer.SetPropertyBlock(_propBlock);
            }
            
            if (currentHealth <= 0)
                HealthZero();
        }
        
        public void HealthZero()
        {
            currentHealth = 0;
            onHealthZero?.Invoke();
            Unsubscribe();
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, radius);
        }
    }
}
