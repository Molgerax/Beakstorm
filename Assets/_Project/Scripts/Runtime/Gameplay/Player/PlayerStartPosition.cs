using UnityEngine;

namespace Beakstorm.Gameplay.Player
{
    [DefaultExecutionOrder(-50)]
    public class PlayerStartPosition : MonoBehaviour
    {
        private static Vector3 _startPosition;
        private static Quaternion _startRotation = Quaternion.identity;

        public static void SetPlayer(Transform t)
        {
            t.SetPositionAndRotation(_startPosition, _startRotation);
        }

        private void Awake()
        {
            _startPosition = transform.position;
            _startRotation = transform.rotation;
        }
    }
}
