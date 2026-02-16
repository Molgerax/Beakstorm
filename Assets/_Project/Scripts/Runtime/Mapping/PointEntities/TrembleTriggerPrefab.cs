using TinyGoose.Tremble;
using UltEvents;
using UnityEngine;

namespace Beakstorm.Mapping.PointEntities
{
    [PrefabEntity]
    public class TrembleTriggerPrefab : MonoBehaviour, ITriggerTarget
    {
        [SerializeField, NoTremble] private UltEvent onTriggerActivate;
        [SerializeField, NoTremble] private UltEvent onTriggerDeactivate;
        
        [SerializeField, Tremble] private bool onlyOnce = true; 
        
        private bool _triggered;

        public void Trigger(TriggerData data)
        {
            if (onlyOnce)
            {
                if (data.Activate && !_triggered)
                {
                    onTriggerActivate?.Invoke();
                    _triggered = true;
                }
                if (!data.Activate && _triggered)
                {
                    onTriggerDeactivate?.Invoke();
                    _triggered = false;
                }
            }
            else
            {
                if (data.Activate)
                    onTriggerActivate?.Invoke();
                else
                    onTriggerDeactivate?.Invoke();
            }
        }
    }
}