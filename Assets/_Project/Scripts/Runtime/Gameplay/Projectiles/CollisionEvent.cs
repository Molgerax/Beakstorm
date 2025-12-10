using UltEvents;
using UnityEngine;

namespace Beakstorm.Gameplay.Projectiles
{
    public class CollisionEvent : MonoBehaviour
    {
        [SerializeField] private LayerMask layerMask;
        [SerializeField] private bool allowOtherTrigger = false;
        
        [SerializeField] private UltEvent<Transform> onTriggerEnter;
        [SerializeField] private UltEvent onCollisionEnter;


        private void OnTriggerEnter(Collider other)
        {
            if (!allowOtherTrigger && other.isTrigger)
                return;
        
            if ((layerMask.value & (1 << other.gameObject.layer)) != 0)
                onTriggerEnter?.Invoke(other.transform);
        }

        private void OnCollisionEnter(Collision other)
        {
            if (!allowOtherTrigger && other.collider.isTrigger)
                return;
            
            if ((layerMask.value & (1 << other.gameObject.layer)) != 0)
                onCollisionEnter?.Invoke();
        }
    }
}
