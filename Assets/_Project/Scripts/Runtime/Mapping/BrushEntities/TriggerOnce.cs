using Beakstorm.Utility.Extensions;
using TinyGoose.Tremble;
using UnityEngine;

namespace Beakstorm.Mapping.BrushEntities
{
    [BrushEntity("once", "trigger", BrushType.Trigger)]
    public class TriggerOnce : TriggerSender
    {
        [SerializeField] private LayerMask layerMask = 64;

        private bool _triggered = false;
        
        private void OnTriggerEnter(Collider other)
        {
            if (_triggered)
                return;
            
            if (!layerMask.Contains(other))
                return;

            _triggered = true;

            SendTrigger();
        }

        public override void OnImportFromMapEntity(MapBsp mapBsp, BspEntity entity)
        {
            base.OnImportFromMapEntity(mapBsp, entity);
            layerMask = LayerMask.GetMask("Player");
        }
    }
}
