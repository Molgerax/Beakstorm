using UltEvents;
using UnityEngine;

namespace Beakstorm.Gameplay.Projectiles
{
    public class ShockWave : MonoBehaviour
    {
        [SerializeField, Min(0.01f)] private float duration;
        [SerializeField] private float size;
        [SerializeField] private AnimationCurve falloff;

        [SerializeField] private UltEvent onFinish;
        
        private float _time;

        public float CurrentScale => falloff.Evaluate(Mathf.Clamp01(_time / duration)) * size;
        
        private void OnEnable()
        {
            _time = 0;
            transform.localScale = Vector3.one * 0.001f;
        }

        private void Update()
        {
            Tick();
        }

        private void Tick()
        {
            _time += Time.deltaTime;
            
            transform.localScale = Vector3.one * Mathf.Max(0.001f, CurrentScale);

            if (_time > duration)
            {
                onFinish?.Invoke();
            }
        }
    }
}
