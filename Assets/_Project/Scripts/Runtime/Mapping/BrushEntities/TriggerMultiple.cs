using Beakstorm.Utility.Extensions;
using TinyGoose.Tremble;
using UnityEngine;

namespace Beakstorm.Mapping.BrushEntities
{
    [BrushEntity("multiple", "trigger", BrushType.Trigger)]
    public class TriggerMultiple : TriggerSender
    {
        [SerializeField] private LayerMask layerMask = 64;
        [SerializeField, Tremble("wait")] private float waitDelay = 0.2f;

        private float _timer = 0f;

        
        private void OnTriggerEnter(Collider other)
        {
            if (!layerMask.Contains(other))
                return;

            SendTrigger();
            _timer = 0f;
        }

        private void OnTriggerStay(Collider other)
        {
            if (!layerMask.Contains(other))
                return;

            _timer += Time.deltaTime;

            if (_timer > waitDelay)
            {
                _timer = 0;
                SendTrigger();
            }
        }
        
        public override void OnImportFromMapEntity(MapBsp mapBsp, BspEntity entity)
        {
            base.OnImportFromMapEntity(mapBsp, entity);
            layerMask = LayerMask.GetMask("Player");
        }
    }
}
