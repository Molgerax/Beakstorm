using TinyGoose.Tremble;
using UnityEngine;

namespace Beakstorm.Mapping.PointEntities
{
    [PointEntity("relay", "trigger")]
    public class TriggerRelay : TriggerSender, ITriggerTarget
    {
        [Tremble("delay")] private float _delay;
        
        private bool _triggered;

        public void Trigger(TriggerData data)
        {
            if (_triggered)
                return;

            _triggered = true;
            
            SendTrigger(data);
        }
    }
}
