using UnityEngine;
using UnityEngine.Events;

namespace Beakstorm.Gameplay.Projectiles
{
    public class TimedEvent : MonoBehaviour
    {
        [SerializeField] private float duration;
        [SerializeField] private bool resetOnDisable = true;

        [SerializeField] private UnityEvent onTimerElapsed;

        private float _elapsedTime;
        private bool _hasFired;

        private void Update()
        {
            Tick();
        }

        private void OnEnable()
        {
            if (resetOnDisable)
                ResetTimer();
        }


        public void ResetTimer()
        {
            _hasFired = false;
            _elapsedTime = 0f;
        }

        public void CompleteTimer()
        {
            _hasFired = true;
            _elapsedTime = duration;

            onTimerElapsed?.Invoke();
        }

        private void Tick()
        {
            if (_hasFired)
                return;

            _elapsedTime += Time.deltaTime;

            if (_elapsedTime >= duration)
                CompleteTimer();
        }
    }
}