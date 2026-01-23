using System.Collections.Generic;
using System.Linq;
using Beakstorm.Gameplay.Messages;
using TinyGoose.Tremble;
using UnityEngine;

namespace Beakstorm.Mapping.PointEntities
{
    [PointEntity("message", "misc", colour:"0.0 0.5 0.0", size:16)]
    public class MessageSender : TriggerBehaviour, IOnImportFromMapEntity
    {
        [SerializeField, Tremble] private string message = "A secret has been discovered";
        [SerializeField, Tremble] private float time = -1;

        [Tremble("target")] private TriggerBehaviour[] _targets;

        [SerializeField, NoTremble] private List<TriggerBehaviour> targets;
        
        public override void Trigger()
        {
            Message m = new Message(message, time <= 0, time, targets);
            MessageManager.AddMessage(m);
        }

        public void OnImportFromMapEntity(MapBsp mapBsp, BspEntity entity)
        {
            targets = _targets?.ToList();
        }
    }
}
