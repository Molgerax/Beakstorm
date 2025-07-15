using UltEvents;
using UnityEngine;

namespace Beakstorm.Simulation.Collisions
{
    public class WeakPoint : MonoBehaviour
    {
        [SerializeField] private int maxHealth = 100;
        [SerializeField] private int currentHealth;
        [SerializeField] public UltEvent onHealthZero;
        
        [Header("Transform")]
        [SerializeField] private Vector3 offset;
        [SerializeField] private float radius = 1;

        [SerializeField] private Renderer meshRenderer;


        public Vector4 PositionRadius
        {
            get
            {
                Vector3 pos = Position;
                return new(pos.x, pos.y, pos.z, Radius);
            }
        }

        public float Radius => radius * AverageScale();

        private float AverageScale()
        {
            Vector3 lossyScale = transform.lossyScale;
            return (lossyScale.x + lossyScale.y + lossyScale.z) / 3f;
        }

        public Vector3 Position => transform.TransformPoint(offset);
        public int MaxHealth => maxHealth;
        public int CurrentHealth => currentHealth;
        public float CurrentHealth01 => (float) currentHealth / maxHealth;
        public bool IsDestroyed => currentHealth <= 0;

        private MaterialPropertyBlock _propBlock;
        
        private void OnEnable()
        {
            currentHealth = maxHealth;
            Subscribe();
            
            _propBlock ??= new MaterialPropertyBlock();
            
            if (!meshRenderer)
                meshRenderer = GetComponent<MeshRenderer>();
            
            UpdateRenderer();
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

            UpdateRenderer();
            
            if (currentHealth <= 0)
                HealthZero();
        }

        private void UpdateRenderer()
        {
            if (meshRenderer)
            {
                meshRenderer.GetPropertyBlock(_propBlock);
                //_propBlock.SetColor("_BaseColor", new Color(CurrentHealth01, 0, 0, 1));
                _propBlock.SetFloat("_Health", CurrentHealth01);
                meshRenderer.SetPropertyBlock(_propBlock);
            }
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
            Gizmos.DrawWireSphere(Position, Radius);
        }
    }
}
