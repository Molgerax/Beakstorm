using Beakstorm.Gameplay.Messages;
using TinyGoose.Tremble;
using UnityEngine;

namespace Beakstorm.Mapping.Triggers
{
    [PointEntity("message", "misc", colour:"0.0 0.5 0.0", size:16)]
    public class MessageSender : TriggerBehaviour
    {
        [SerializeField] private string message = "A secret has been discovered";
        [SerializeField] private float time = -1;
        
        public override void Trigger()
        {
            Message m = new Message(message, time <= 0, time);
            MessageManager.AddMessage(m);
        }
    }
}
