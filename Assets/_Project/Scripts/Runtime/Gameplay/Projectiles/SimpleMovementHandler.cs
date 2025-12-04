using UltEvents;
using UnityEngine;

namespace Beakstorm.Gameplay.Projectiles
{
    public class SimpleMovementHandler : MonoBehaviour
    {
        [SerializeField] private float gravity = 9.81f;
        [SerializeField] private float drag = 0.01f;

        [Header("Collisions")]
        [SerializeField] private LayerMask layerMask;
        [SerializeField] private UltEvent<Vector3, Quaternion> onCollision;

        private Vector3 _velocity;

        private Vector3 _momentaryForce;
        private Vector3 _oldPosition;

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
            _oldPosition = transform.position;
            _velocity += _momentaryForce * deltaTime;
            _velocity += Vector3.down * (gravity * deltaTime);

            _velocity = _velocity * (1 - drag * _velocity.magnitude * deltaTime);
            
            _momentaryForce = Vector3.zero;

            CheckForCollision(deltaTime);
            transform.position = _oldPosition + _velocity * deltaTime;;
        }

        private void CheckForCollision(float deltaTime)
        {
            if (_velocity.sqrMagnitude == 0)
                return;
            
            Ray ray = new Ray(_oldPosition, _velocity);
            if (Physics.Raycast(ray, out RaycastHit hit, _velocity.magnitude * deltaTime, layerMask,
                QueryTriggerInteraction.Ignore))
            {
                onCollision?.Invoke(hit.point, Quaternion.LookRotation(hit.normal));
            }
        }
    }
}