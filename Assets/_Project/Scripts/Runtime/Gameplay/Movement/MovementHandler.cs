using System;
using UnityEngine;

namespace Beakstorm.Gameplay.Movement
{
    public class MovementHandler : MonoBehaviour
    {
        [SerializeField] private MovementBehaviour[] movementBehaviours;
        
        public event Action onBeforeMove;
        public event Action onAfterMove;

        
        public void Initialize()
        {
            if (movementBehaviours == null || movementBehaviours.Length == 0)
                return;

            onBeforeMove?.Invoke();
            foreach (MovementBehaviour movementBehaviour in movementBehaviours)
            {
                if (!movementBehaviour || !movementBehaviour.enabled)
                    continue;
                
                movementBehaviour.Initialize(transform);
            }
            onAfterMove?.Invoke();
        }
        
        private void Update()
        {
            ApplyMovementBehaviours();
        }

        private void Reset()
        {
            movementBehaviours = GetComponents<MovementBehaviour>();
        }

        private void ApplyMovementBehaviours()
        {
            if (movementBehaviours == null || movementBehaviours.Length == 0)
                return;

            onBeforeMove?.Invoke();
            foreach (MovementBehaviour movementBehaviour in movementBehaviours)
            {
                if (!movementBehaviour || !movementBehaviour.enabled)
                    continue;
                
                movementBehaviour.ApplyMovement(transform);
            }
            onAfterMove?.Invoke();
        }
        

        public void ApplyTransform(Action<Transform> action)
        {
            onBeforeMove?.Invoke();
            action.Invoke(transform);
            onAfterMove?.Invoke();
        }
    }
}
