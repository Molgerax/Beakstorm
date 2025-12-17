using Beakstorm.Utility.Extensions;
using TinyGoose.Tremble;
using UnityEngine;

namespace Beakstorm.Mapping.Triggers
{
    [BrushEntity("once", "trigger", BrushType.Trigger)]
    public class TriggerOnce : MonoBehaviour
    {
        [SerializeField, Tremble("target")] private TriggerBehaviour[] target;
        [SerializeField] private LayerMask layerMask = 64;

        [SerializeField, NoTremble] private bool _triggered = false;
        
        private void OnTriggerEnter(Collider other)
        {
            if (_triggered)
                return;
            
            if (!layerMask.Contains(other))
                return;

            _triggered = true;
            
            Debug.Log("Triggered", this);
            
            target.TryTrigger();
        }
    }
}
