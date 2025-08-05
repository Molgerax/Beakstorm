using System;
using Beakstorm.Gameplay.Damaging;
using Beakstorm.Inputs;
using Beakstorm.Pausing;
using UnityEngine;

namespace Beakstorm.Gameplay.Player
{
    [DefaultExecutionOrder(-40)]
    public class GliderController : MonoBehaviour
    {
        [SerializeField] private Transform t;
        [SerializeField] private Vector2 maxAngles = new Vector2(0f, 90f);
        [SerializeField] private float maxSpeed = 20f;
        [SerializeField] private float minSpeed = 10f;
        [SerializeField] private float acceleration = 5f;

        [SerializeField] private float steerSpeed = 60;

        [SerializeField] private float rollSpeed = 20;

        private LayerMask _layerMask;
        
        private Rigidbody _rb;
        private PlayerInputs _inputs;
        private Vector3 _eulerAngles;
        
        private float _speed;

        private float _roll;
        
        public float Speed01 => (_speed - minSpeed) / (maxSpeed - minSpeed);

        public float Speed => _speed;

        #region Mono Methods
        
        private void Awake()
        {
            _layerMask = int.MaxValue;
            _layerMask &= ~(1 << gameObject.layer);
            
            _inputs = PlayerInputs.Instance;
            _rb = GetComponent<Rigidbody>();

            if (!t)
                t = transform;
            
            _eulerAngles = t.localEulerAngles;
            _speed = minSpeed;
        }


        private void Update()
        {
            if (PauseManager.IsPaused)
                return;
            
            SteerInput();
            HandleAcceleration();
            Move();

            HandleCollision();
            
            if (CameraFOV.Instance)
                CameraFOV.Instance.SetFoV(Speed01);
        }

        #endregion


        
        private void HandleCollision()
        {
            float depth = 2f;
            float bounce = 0.4f;
            
            Vector3 forward = t.forward;
            Vector3 pos = t.position;

            Ray ray = new Ray(pos - forward * depth, forward);
            if (Physics.Raycast(ray, out RaycastHit hit, _speed * Time.deltaTime * 2 + depth, _layerMask, QueryTriggerInteraction.Ignore))
            {
                float penetrationDepth = hit.distance - depth - _speed * Time.deltaTime;

                float angle = Vector3.Angle(forward, hit.normal);

                float collisionStrength = Mathf.Clamp01((angle - 90) / 90);

                if (TryGetComponent(out IDamageable damageable))
                {
                    damageable.TakeDamage(Mathf.CeilToInt(collisionStrength * _speed * 0.75f));
                }

                _speed = Mathf.Clamp(_speed - (maxSpeed - minSpeed) * collisionStrength, minSpeed, maxSpeed);
                
                pos += (hit.normal * (Mathf.Max(0,penetrationDepth) * (1 + bounce)));


                forward = Vector3.Lerp(
                    Vector3.ProjectOnPlane(forward, hit.normal),
                    Vector3.Reflect(forward, hit.normal), bounce);
                //forward = forward - (1 + bounce) * Vector3.Dot(forward, hit.normal) * hit.normal;
                //forward = Vector3.Reflect(forward, hit.normal);

                if (forward.magnitude == 0)
                    forward = Vector3.Cross(hit.normal, Vector3.up);


                Quaternion rotation = Quaternion.LookRotation(forward.normalized, t.up);
                
                t.position = pos;
                //t.forward = forward.normalized;
                t.rotation = rotation;
            }
        }

        private void OnCollisionEnter(Collision other)
        {
            ContactPoint p = other.GetContact(0);

            Vector3 forward = t.forward;
            forward = Vector3.Reflect(forward, p.normal);
            Vector3 pos = t.position;
            pos += (p.normal * p.separation * 2);

            t.position = pos;
            t.forward = forward;
        }

        private void SteerInput()
        {
            Vector2 inputVector = _inputs.MoveInput;

            _eulerAngles = t.localEulerAngles;

            _eulerAngles.x -= inputVector.y * Time.deltaTime * steerSpeed;

            if (_eulerAngles.x > 180)
                _eulerAngles.x -= 360;
            
            _eulerAngles.x = Mathf.Max(_eulerAngles.x, maxAngles.x);
            _eulerAngles.x = Mathf.Min(_eulerAngles.x, maxAngles.y);

            float yAcceleration = inputVector.x * Time.deltaTime * steerSpeed;

            float rollAngle = Mathf.Lerp(20f, 80f, Speed01);
            
            _roll = Mathf.Lerp(_roll, -inputVector.x * rollAngle, 1 - Mathf.Exp(-rollSpeed * Time.deltaTime));
            
            _eulerAngles.z = _roll;

            t.localEulerAngles = _eulerAngles;
            
            t.Rotate(0.0f, yAcceleration, 0.0f, Space.World);
        }

        private void HandleAcceleration()
        {
            Vector3 flatForward = t.forward;
            flatForward.y = 0f;
            
            float angle = Vector3.SignedAngle(t.forward, flatForward, t.right);
            float angleStrength = -angle / 180f;
            
            float inputStrength = 0;
            inputStrength += _inputs.accelerateAction.IsPressed() ? 1 : 0;
            inputStrength -= _inputs.brakeAction.IsPressed() ? 1 : 0;
            
            _speed += (inputStrength + angleStrength * Mathf.Abs(angleStrength)) * acceleration * Time.deltaTime;
            _speed = Mathf.Clamp(_speed, minSpeed, maxSpeed);
        }
        
        private void Move()
        {
            t.position += t.forward * (_speed * Time.deltaTime);
        }
    }
}