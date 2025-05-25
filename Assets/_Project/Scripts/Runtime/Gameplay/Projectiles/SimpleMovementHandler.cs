using UnityEngine;

namespace Beakstorm.Gameplay.Projectiles
{
    public class SimpleMovementHandler : MonoBehaviour
    {
        [SerializeField] private float gravity = 9.81f;
        [SerializeField] private float drag = 0.01f;

        private Vector3 _velocity;

        private Vector3 _momentaryForce;

        public float Gravity
        {
            get => gravity;
            set => gravity = value;
        }
        
        private void OnEnable()
        {
            ResetVelocity();
        }

        private void Update()
        {
            TickMovement(Time.deltaTime);
        }

        public void ResetVelocity()
        {
            _velocity = Vector3.zero;
            _momentaryForce = Vector3.zero;
        }

        public void AddForce(Vector3 force)
        {
            _momentaryForce += force;
        }

        public void SetVelocity(Vector3 velocity)
        {
            _velocity = velocity;
            _momentaryForce = Vector3.zero;
        }

        private void TickMovement(float deltaTime)
        {
            _velocity += _momentaryForce * deltaTime;
            _velocity += Vector3.down * (gravity * deltaTime);

            _velocity = _velocity * (1 - drag * _velocity.magnitude * deltaTime);
            
            _momentaryForce = Vector3.zero;

            transform.position += _velocity * deltaTime;
        }
    }
}