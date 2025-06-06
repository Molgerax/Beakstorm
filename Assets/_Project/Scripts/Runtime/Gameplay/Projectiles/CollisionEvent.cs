using UltEvents;
using UnityEngine;

namespace Beakstorm.Gameplay.Projectiles
{
    public class CollisionEvent : MonoBehaviour
    {
        [SerializeField] private LayerMask layerMask;

        [SerializeField] private UltEvent onTriggerEnter;
        [SerializeField] private UltEvent onCollisionEnter;


        private void OnTriggerEnter(Collider other)
        {
            if ((layerMask.value & (1 << other.gameObject.layer)) != 0)
                onTriggerEnter?.Invoke();
        }

        private void OnCollisionEnter(Collision other)
        {
            if ((layerMask.value & (1 << other.gameObject.layer)) != 0)
                onCollisionEnter?.Invoke();
        }
    }
}
