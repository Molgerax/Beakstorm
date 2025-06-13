using UltEvents;
using UnityEngine;

namespace Beakstorm.Gameplay.Projectiles
{
    public class TimedEvent : MonoBehaviour
    {
        [SerializeField] private float duration;
        [SerializeField] private bool resetOnDisable = true;

        [SerializeField] private UltEvent onTimerElapsed;
        [SerializeField] private bool autoStart = true;

        private float _elapsedTime;
        private bool _hasFired;

        private bool _isStarted;

        public float Duration
        {
            get => duration;
            set => duration = value;
        }
        
        private void Update()
        {
            Tick();
        }

        private void OnEnable()
        {
            if (resetOnDisable)
                ResetTimer();
            
            if (autoStart)
                StartTimer();
        }

        public void StartTimer()
        {
            ResetTimer();
            _isStarted = true;
        }

        public void ResetTimer()
        {
            _hasFired = false;
            _elapsedTime = 0f;
            _isStarted = false;
        }

        public void CompleteTimer()
        {
            _hasFired = true;
            _elapsedTime = duration;
            _isStarted = false;

            onTimerElapsed?.Invoke();
        }

        private void Tick()
        {
            if (!_isStarted)
                return;
            
            if (Time.deltaTime == 0f)
                return;
            
            if (_hasFired)
                return;

            _elapsedTime += Time.deltaTime;

            if (_elapsedTime >= duration)
                CompleteTimer();
        }
    }
}