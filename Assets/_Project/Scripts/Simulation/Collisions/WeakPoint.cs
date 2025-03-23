using System;
using UnityEngine;

namespace Beakstorm.Simulation.Collisions
{
    public class WeakPoint : MonoBehaviour
    {
        [SerializeField] private int maxHealth = 100;
        [SerializeField] private int currentHealth;

        [SerializeField] private float radius = 1;

        public Vector4 PositionRadius => new Vector4(transform.position.x, transform.position.y, transform.position.z, radius);
        
        private void OnEnable()
        {
            currentHealth = maxHealth;
            
            WeakPointManager.Instance.WeakPoints.Add(this);
        }

        public void ApplyDamage(int value)
        {
            currentHealth = Math.Max(0, currentHealth - value);
        }
        
        private void OnDisable()
        {
            WeakPointManager.Instance.WeakPoints.Remove(this);
        }
    }
}
