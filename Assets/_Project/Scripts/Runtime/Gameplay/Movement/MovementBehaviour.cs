using UnityEngine;

namespace Beakstorm.Gameplay.Movement
{
    public abstract class MovementBehaviour : MonoBehaviour
    {
        public virtual void Initialize(Transform t) {}
        
        public abstract void ApplyMovement(Transform t);
    }
}