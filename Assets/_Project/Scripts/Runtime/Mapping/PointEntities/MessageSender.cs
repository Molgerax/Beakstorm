using System.Collections.Generic;
using System.Linq;
using Beakstorm.Gameplay.Messages;
using TinyGoose.Tremble;
using UnityEngine;

namespace Beakstorm.Mapping.PointEntities
{
    [PointEntity("message", "misc", colour:"0.0 0.5 0.0", size:16)]
    public class MessageSender : TriggerSender, ITriggerTarget, IOnImportFromMapEntity
    {
        [SerializeField, Tremble] private string message = "A secret has been discovered";
        [SerializeField, Tremble] private float time = -1;

        [SerializeField, NoTremble] private List<Component> targetList;
        
        public void Trigger()
        {
            Message m = new Message(message, time <= 0, time, targetList);
            MessageManager.AddMessage(m);
        }

        public override void OnImportFromMapEntity(MapBsp mapBsp, BspEntity entity)
        {
            base.OnImportFromMapEntity(mapBsp, entity);
            targetList = targets?.ToList();
        }
    }
}
