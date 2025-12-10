using UnityEngine;

namespace Beakstorm.Gameplay.Movement
{
    public class MoveResetParentMovement : MonoBehaviour
    {
        [SerializeField] private MovementHandler parentHandler;

        private Vector3 _cachedPosition;
        private Quaternion _cachedRotation;

        private void Reset()
        {
            parentHandler = GetComponentInParent<MovementHandler>();
        }

        private void OnEnable()
        {
            if (parentHandler)
            {
                parentHandler.onBeforeMove += SaveTransform;
                parentHandler.onAfterMove += RestoreTransform;
            }
        }

        private void OnDisable()
        {
            if (parentHandler)
            {
                parentHandler.onBeforeMove -= SaveTransform;
                parentHandler.onAfterMove -= RestoreTransform;
            }
        }
        

        private void SaveTransform()
        {
            _cachedPosition = transform.position;
            _cachedRotation = transform.rotation;
        }

        private void RestoreTransform()
        {
            transform.SetPositionAndRotation(_cachedPosition, _cachedRotation);
        }
    }
}