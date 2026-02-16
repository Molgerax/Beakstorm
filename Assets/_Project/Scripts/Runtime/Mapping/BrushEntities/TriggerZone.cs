using Beakstorm.Utility.Extensions;
using TinyGoose.Tremble;
using UnityEngine;

namespace Beakstorm.Mapping.BrushEntities
{
    [BrushEntity("zone", "trigger", BrushType.Trigger)]
    public class TriggerZone : TriggerSender
    {
        [SerializeField] private LayerMask layerMask = 64;
        
        private void OnTriggerEnter(Collider other)
        {
            if (!layerMask.Contains(other))
                return;

            SendTrigger(TriggerData.Active);
        }
        
        private void OnTriggerExit(Collider other)
        {
            if (!layerMask.Contains(other))
                return;

            SendTrigger(TriggerData.Deactive);
        }

        public override void OnImportFromMapEntity(MapBsp mapBsp, BspEntity entity)
        {
            base.OnImportFromMapEntity(mapBsp, entity);
            layerMask = LayerMask.GetMask("Player");
        }
    }
}
