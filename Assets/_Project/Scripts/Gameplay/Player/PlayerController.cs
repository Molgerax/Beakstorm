using UnityEngine;

namespace Beakstorm.Gameplay.Player
{
    public class PlayerController : MonoBehaviour
    {
        public static PlayerController Instance;

        private Vector3 _oldPosition;
        private Vector3 _position;

        private Vector3 _velocity;
        

        public Vector3 Position => _position;
        public Vector3 Velocity => _velocity;
        

        private void Awake()
        {
            Instance = this;
        }

        private void Update()
        {
            UpdatePosition();
        }

        private void UpdatePosition()
        {
            _oldPosition = _position;
            _position = transform.position;

            _velocity = (_position - _oldPosition) / Time.deltaTime;
        }
    }
}
