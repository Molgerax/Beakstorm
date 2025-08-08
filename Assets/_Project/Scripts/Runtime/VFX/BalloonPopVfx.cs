using UltEvents;
using UnityEngine;

namespace Beakstorm.VFX
{
    public class BalloonPopVfx : MonoBehaviour
    {
        [SerializeField] private Vector3 offset;
        [SerializeField, Min(0.01f)] private float duration;
        [SerializeField] private Vector2 size;
        [SerializeField] private AnimationCurve scale;
        [SerializeField] private AnimationCurve health;


        [SerializeField] private MeshRenderer[] renderers;
        
        [SerializeField] private UltEvent onFinish;
        
        private float _time;
        private MaterialPropertyBlock _propertyBlock;
        
        
        public float CurrentScale => scale.Evaluate(Mathf.Clamp01(_time / duration)) * size.y + size.x;
        public float CurrentHealth => health.Evaluate(Mathf.Clamp01(_time / duration));
        
        private void OnEnable()
        {
            _time = 0;
            Tick(0);
        }

        private void Update()
        {
            Tick(Time.deltaTime);
        }

        private void Tick(float deltaTime)
        {
            _time += deltaTime;

            foreach (var meshRenderer in renderers)
            {
                if (!meshRenderer)
                    continue;
                
                _propertyBlock ??= new MaterialPropertyBlock();
                
                meshRenderer.GetPropertyBlock(_propertyBlock);
                _propertyBlock.SetFloat("_Health", CurrentHealth);
                _propertyBlock.SetFloat("_Radius", CurrentScale);
                _propertyBlock.SetVector("_CenterOffset", offset);
                meshRenderer.SetPropertyBlock(_propertyBlock);
            }
            
            if (_time > duration)
            {
                onFinish?.Invoke();
            }
        }


        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.TransformPoint(offset), size.x);
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(transform.TransformPoint(offset), size.y);
        }
    }
}
