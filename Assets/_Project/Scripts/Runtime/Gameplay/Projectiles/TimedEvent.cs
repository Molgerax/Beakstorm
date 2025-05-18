using UltEvents;
using UnityEngine;

namespace Beakstorm.Gameplay.Projectiles
{
    public class TimedEvent : MonoBehaviour
    {
        [SerializeField] private float duration;
        [SerializeField] private bool resetOnDisable = true;

        [SerializeField] private UltEvent onTimerElapsed;
        [SerializeField] private UltEvent<float> completionPercentage;

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
            if (Time.deltaTime == 0f)
                return;
            
            if (_hasFired)
                return;

            _elapsedTime += Time.deltaTime;

            float percentage = Mathf.Clamp01(_elapsedTime / duration);
            completionPercentage?.Invoke(percentage);

            if (_elapsedTime >= duration)
                CompleteTimer();
        }
    }
}