using TinyGoose.Tremble;
using UltEvents;
using UnityEngine;

namespace Beakstorm.Mapping.Tremble
{
    [PrefabEntity]
    public class TrembleTriggerPrefab : TriggerBehaviour
    {
        [SerializeField, NoTremble] private UltEvent onTrigger;
        [SerializeField, NoTremble] private UltEvent onTriggerAgain;
        
        [SerializeField, Tremble] private bool onlyOnce = true; 
        
        private int _triggerCount;
        
        public override void Trigger()
        {
            if (_triggerCount > 0 && onlyOnce)
                return;

            if (_triggerCount == 0)
                onTrigger?.Invoke();
            else
                onTriggerAgain?.Invoke();

            _triggerCount++;
        }
    }
}