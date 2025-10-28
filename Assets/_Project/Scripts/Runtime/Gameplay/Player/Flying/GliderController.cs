using System;
using Beakstorm.Core.Variables;
using Beakstorm.Gameplay.Damaging;
using Beakstorm.Inputs;
using Beakstorm.Pausing;
using UnityEngine;

namespace Beakstorm.Gameplay.Player.Flying
{
    [DefaultExecutionOrder(-40)]
    public class GliderController : MonoBehaviour
    {
        [field:SerializeField] public Transform T { get; private set; }

        [SerializeField] private FlightControlStrategy controlStrategy;

        [SerializeField] private RangeVariable speedVariable;
        [SerializeField] private RangeVariable thrustVariable;
        
        private LayerMask _layerMask;

        private Vector3 _position;
        private Vector3 _oldPosition;

        private PlayerInputs _inputs;

        private Vector3 _velocity;
        private Vector3 _oldVelocity;

        public RangeVariable ThrustVariable => thrustVariable;
        public RangeVariable SpeedVariable => speedVariable;
        
        [NonSerialized] public Rigidbody Rigidbody;

        [NonSerialized] public Quaternion LocalRotation;
        [NonSerialized] public Vector3 EulerAngles;
        [NonSerialized] public Vector3 Velocity;
        [NonSerialized] public Vector3 OldVelocity;
        [NonSerialized] public float Speed;
        [NonSerialized] public float Roll;
        [NonSerialized] public float Thrust;
        
        public float Speed01 => controlStrategy.Speed01(Speed);

        [NonSerialized] public float Thrust01;
        
        public Vector2 MoveInput => _inputs.MoveInput;
        public bool BreakInput => _inputs.brakeAction.IsPressed();
        public bool ThrustInput => _inputs.accelerateAction.IsPressed();

        #region Mono Methods
        
        private void Awake()
        {
            _layerMask = int.MaxValue;
            _layerMask &= ~(1 << gameObject.layer);
            
            _inputs = PlayerInputs.Instance;
            Rigidbody = GetComponent<Rigidbody>();

            if (!T)
                T = transform;
            
            PlayerStartPosition.SetPlayer(T);

            _position = transform.position;
            _oldPosition = _position;
            
            controlStrategy.Initialize(this, Time.deltaTime);

            speedVariable.Set(0);
        }


        private void Update()
        {
            if (PauseManager.IsPaused)
                return;

            float dt = Time.deltaTime;
            
            controlStrategy.UpdateFlight(this, dt);
            
            HandleCollision(dt);
            
            if (CameraFOV.Instance)
                CameraFOV.Instance.SetFoV(Speed01);
            
            speedVariable.Set(Speed);
        }

        private void FixedUpdate()
        {
            if (PauseManager.IsPaused)
                return;

            float dt = Time.fixedDeltaTime;
            
            controlStrategy.FixedUpdateFlight(this, dt);
        }

        #endregion


        
        private void HandleCollision(float dt)
        {
            float depth = 2f;
            float bounce = 0.4f;
            
            Vector3 forward = T.forward;
            Vector3 pos = T.position;

            Ray ray = new Ray(pos - forward * depth, forward);
            if (Physics.Raycast(ray, out RaycastHit hit, Speed * dt * 2 + depth, _layerMask, QueryTriggerInteraction.Ignore))
            {
                float penetrationDepth = hit.distance - depth - Speed * dt;

                float angle = Vector3.Angle(forward, hit.normal);

                float collisionStrength = Mathf.Clamp01((angle - 90) / 90);

                if (TryGetComponent(out IDamageable damageable))
                {
                    int damage = Mathf.CeilToInt(collisionStrength * Speed * 0.75f);
                
                    damageable.TakeDamage(Mathf.Clamp(damage, 0, 25));
                }

                //Speed = Mathf.Clamp(Speed - (controlStrategy.MaxSpeed - controlStrategy.MinSpeed) * collisionStrength, controlStrategy.MinSpeed, controlStrategy.MaxSpeed);
                Speed = Speed *= (1 - collisionStrength * 0.75f);
                
                pos += (hit.normal * (Mathf.Max(0,penetrationDepth) * (1 + bounce)));


                forward = Vector3.Lerp(
                    Vector3.ProjectOnPlane(forward, hit.normal),
                    Vector3.Reflect(forward, hit.normal), bounce);
                //forward = forward - (1 + bounce) * Vector3.Dot(forward, hit.normal) * hit.normal;
                //forward = Vector3.Reflect(forward, hit.normal);

                if (forward.magnitude == 0)
                    forward = Vector3.Cross(hit.normal, Vector3.up);


                Quaternion rotation = Quaternion.LookRotation(forward.normalized, T.up);

                EulerAngles = rotation.eulerAngles;
                
                T.position = pos;
                //t.forward = forward.normalized;
                T.rotation = rotation;
            }
        }

        private void OnCollisionEnter(Collision other)
        {
            ContactPoint p = other.GetContact(0);

            Vector3 forward = T.forward;
            forward = Vector3.Reflect(forward, p.normal);
            Vector3 pos = T.position;
            pos += (p.normal * p.separation * 2);

            T.position = pos;
            T.forward = forward;
        }
    }
}