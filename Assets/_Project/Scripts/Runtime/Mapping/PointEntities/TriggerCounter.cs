using Beakstorm.Utility.Extensions;
using TinyGoose.Tremble;
using UnityEngine;

namespace Beakstorm.Mapping.PointEntities
{
    [PointEntity("counter", "trigger", colour:"1.0 0.5 0.0", size:16)]
    public class TriggerCounter : TriggerSender, ITriggerTarget
    {
        [SerializeField] private int count = 1;

        private int _counter;
        private bool _triggered;
        
        public void Trigger()
        {
            if (_triggered)
                return;

            _counter++;
            if (_counter >= count)
                OnCountReached();
        }


        private void OnCountReached()
        {
            _triggered = true;
            
            SendTrigger();
        }
    }
}
