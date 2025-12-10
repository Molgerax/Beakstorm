using Beakstorm.Mapping;
using Beakstorm.Simulation.Collisions.SDF;
using Beakstorm.Utility.Extensions;
using TinyGoose.Tremble;
using UltEvents;
using Unity.Mathematics;
using UnityEngine;

namespace Beakstorm.Simulation.Collisions
{
    public class WeakPoint : MonoBehaviour
    {
        [SerializeField] private int maxHealth = 100;
        [SerializeField, NoTremble] private UltEvent onInitialize;
        [SerializeField, NoTremble] public UltEvent onHealthZero;
        [SerializeField, NoTremble] private UltEvent onDamageTaken;

        [SerializeField] private bool autoInitialize = false;
        
        [Header("Transform")]
        [SerializeField] private Vector3 offset;
        [SerializeField] private float radius = 1;

        [SerializeField] private Renderer meshRenderer;

        [SerializeField] private TriggerBehaviour[] triggerTargets;

        private bool _destroyed = true;
        private int _currentHealth;

        private AbstractSdfShape _sdfShape;

        public AbstractSdfData SdfData
        {
            get
            {
                if (!IsValid)
                    return new AbstractSdfData();
                return _sdfShape ? _sdfShape.SdfData() : new AbstractSdfData(Position, new float3(Radius, 0, 0), 0);
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
        public int CurrentHealth => _currentHealth;
        public float CurrentHealth01 => (float) _currentHealth / maxHealth;
        public bool IsDestroyed => _currentHealth <= 0 || _destroyed;

        public bool IsValid
        {
            get
            {
                if (IsDestroyed || !isActiveAndEnabled)
                    return false;
                return true;
            }
        }

        private MaterialPropertyBlock _propBlock;
        
        public void Initialize()
        {
            if (_destroyed == false)
                return;
            
            _currentHealth = maxHealth;
            Subscribe();
            onInitialize?.Invoke();
            
            _propBlock ??= new MaterialPropertyBlock();
            
            if (!meshRenderer)
                meshRenderer = GetComponent<MeshRenderer>();
            
            UpdateRenderer();

            _destroyed = false;
        }

        private void OnEnable()
        {
            if (autoInitialize)
                Initialize();

            _sdfShape = GetComponent<AbstractSdfShape>();
        }

        private void OnDisable()
        {
            Unsubscribe();
            _destroyed = true;
        }

        private void Subscribe()
        {
            WeakPointManager.AddWeakPoint(this);
        }

        private void Unsubscribe()
        {
            WeakPointManager.RemoveWeakPoint(this);
        }
        
        public void ApplyDamage(int value)
        {
            if (_destroyed)
                return;
            
            if (value > 0)
                onDamageTaken?.Invoke();
            
            _currentHealth -= value;

            UpdateRenderer();
            
            if (_currentHealth <= 0)
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
            if (_destroyed)
                return;
            _destroyed = true;
            
            _currentHealth = 0;
            onHealthZero?.Invoke();
            triggerTargets.TryTrigger();
            Unsubscribe();
        }

        public void SetFromTremble(TriggerBehaviour[] target, int health)
        {
            maxHealth = health;
            triggerTargets = target;
            autoInitialize = true;
        }
        
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(Position, Radius);
        }
    }
}
