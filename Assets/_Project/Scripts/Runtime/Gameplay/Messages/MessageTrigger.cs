using Beakstorm.Utility.Extensions;
using TinyGoose.Tremble;
using UnityEngine;

namespace Beakstorm.Gameplay.Messages
{
    [PrefabEntity("message", "trigger")]
    public class MessageTrigger : MonoBehaviour
    {
        [SerializeField, Tremble] private string message = "A secret has been discovered";
        [SerializeField, Tremble] private float time = -1;

        [SerializeField, NoTremble] private LayerMask layerMask = 64;
        
        private bool _triggered;


        private void Awake()
        {
            _triggered = false;
        }

        private void Trigger()
        {
            Message m = new Message(message, time <= 0, time);
            MessageManager.AddMessage(m);

            _triggered = true;
            
            gameObject.SetActive(false);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (_triggered)
                return;
            
            if (!layerMask.Contains(other.gameObject.layer))
                return;
                
            Trigger();
        }
    }
}
