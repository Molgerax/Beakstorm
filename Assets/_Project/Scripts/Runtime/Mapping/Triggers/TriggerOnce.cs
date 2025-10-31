using Beakstorm.Utility.Extensions;
using TinyGoose.Tremble;
using UnityEngine;

namespace Beakstorm.Mapping.Triggers
{
    [BrushEntity("once", "trigger", BrushType.Trigger)]
    public class TriggerOnce : MonoBehaviour
    {
        [SerializeField] private Component target;
        [SerializeField] private LayerMask layerMask = 64;

        [SerializeField, NoTremble] private bool _triggered = false;
        
        private void OnTriggerEnter(Collider other)
        {
            if (_triggered)
                return;
            
            //if (!layerMask.Contains(other))
            //    return;

            target.TryTrigger();
            
            Debug.Log("Triggered!");
            
            _triggered = true;
        }
    }
}
