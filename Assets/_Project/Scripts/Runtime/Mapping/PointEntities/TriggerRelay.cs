using TinyGoose.Tremble;
using UnityEngine;

namespace Beakstorm.Mapping.PointEntities
{
    [PointEntity("relay", "trigger", size:16)]
    public class TriggerRelay : TriggerSender, ITriggerTarget
    {
        [SerializeField] private float delay = 0f;

        private float _timer;
        private bool _isRunning;
        private bool _triggered;
        
        public void Trigger()
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
            
            SendTrigger();
        }
    }
}
