using Beakstorm.Utility.Extensions;
using TinyGoose.Tremble;
using UnityEngine;

namespace Beakstorm.Mapping.Triggers
{
    [PointEntity("relay", "trigger", colour:"1.0 0.5 0.0", size:16)]
    public class TriggerRelay : TriggerBehaviour
    {
        [SerializeField, Tremble("target")] private TriggerBehaviour[] target;
        [SerializeField] private float delay = 0f;

        private float _timer;
        private bool _isRunning;
        private bool _triggered;
        
        public override void Trigger()
        {
            if (_triggered)
                return;

            _triggered = true;
            _isRunning = true;
            _timer = 0f;
        }

        private void Update()
        {
            if (!_isRunning)
                return;

            _timer += Time.deltaTime;
            
            if (_timer >= delay)
                OnTimerOver();
        }

        public void OnTimerOver()
        {
            _isRunning = false;
            
            target.TryTrigger();
        }
    }
}
