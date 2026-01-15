using System;
using System.Collections.Generic;
using Beakstorm.Core.Variables;
using Beakstorm.Gameplay.Damaging;
using Beakstorm.Inputs;
using Beakstorm.Mapping.BrushEntities;
using Beakstorm.Pausing;
using Beakstorm.SceneManagement;
using UnityEngine;

namespace Beakstorm.Gameplay.Player.Flying
{
    [DefaultExecutionOrder(-40)]
    public class GliderController : MonoBehaviour, IOnSceneLoad
    {
        [field:SerializeField] public Transform T { get; private set; }
        
        [field:SerializeField] public Transform Model { get; private set; }

        [SerializeField] private FlightControlStrategy controlStrategy;

        [SerializeField] private RangeVariable speedVariable;
        [SerializeField] private RangeVariable thrustVariable;
        [SerializeField] private RangeVariable overChargeVariable;

        [SerializeField] private float inputResponseTime = 0.2f;
        
        private LayerMask _layerMask;

        private Vector3 _position;
        private Vector3 _oldPosition;

        private PlayerInputs _inputs;

        private Vector3 _velocity;
        private Vector3 _oldVelocity;

        private bool _acceleratePressed;
        private bool _brakePressed;
        
        public RangeVariable ThrustVariable => thrustVariable;
        public RangeVariable SpeedVariable => speedVariable;
        public RangeVariable OverChargeVariable => overChargeVariable;
        
        [NonSerialized] public Rigidbody Rigidbody;

        [NonSerialized] public Quaternion LocalRotation;
        [NonSerialized] public Vector3 EulerAngles;
        [NonSerialized] public Vector3 Velocity;
        [NonSerialized] public Vector3 OldVelocity;
        [NonSerialized] public float Speed;
        [NonSerialized] public float Roll;
        [NonSerialized] public float Thrust;

        [NonSerialized] public float OverCharge;
        [NonSerialized] public bool Discharging;
        
        [NonSerialized] public float FovFactor;

        [NonSerialized] public Vector3 ExternalWind;
        
        public float Speed01 => controlStrategy.Speed01(Speed);

        [NonSerialized] public float Thrust01;
        
        public Vector2 MoveInput => _moveInputCached;
        public bool BreakInput => _brakePressed;
        public bool ThrustInput => _acceleratePressed;

        private bool _initialized;

        public SceneLoadCallbackPoint SceneLoadCallbackPoint => SceneLoadCallbackPoint.WhenLevelStarts;

        private List<WindDraft> _drafts = new();


        private Vector2 _moveInputCached;
        private Vector2 _moveInputVel;
        private float _moveInputMagnitude;
        
        public FlightControlStrategy ControlStrategy
        {
            get => controlStrategy;
            set
            {
                controlStrategy = value;
                if (controlStrategy != null)
                {
                    controlStrategy.Apply();
                }
            }
        }
        

        #region Mono Methods

        public void OnSceneLoaded()
        {
            PlayerStartPosition.SetPlayer(T);
            _position = T.position;
            _oldPosition = _position;
            
            controlStrategy.Initialize(this, Time.deltaTime);
            controlStrategy.Apply();
            _initialized = true;
        }
        
        private void Awake()
        {
            _layerMask = int.MaxValue;
            _layerMask &= ~(1 << gameObject.layer);
            
            _inputs = PlayerInputs.Instance;
            Rigidbody = GetComponent<Rigidbody>();

            if (!T)
                T = transform;
            speedVariable.Set(0);
            
            GlobalSceneLoader.ExecuteWhenLoaded(this);
        }


        private void Update()
        {
            if (!_initialized)
                return;
            
            if (PauseManager.IsPaused)
                return;

            float dt = Time.deltaTime;
            if (dt <= 0)
                return;

            HandleMoveInput();
            
            ApplyWind();
            controlStrategy.UpdateFlight(this, dt);
            
            HandleCollision(dt);

            if (CameraFOV.Instance)
            {
                CameraFOV.Instance.SetFoV(FovFactor);
                CameraFOV.Instance.SetCameraDistance(FovFactor);
            }
            
            speedVariable.Set(Speed);
        }

        private void HandleMoveInput()
        {
            Vector2 moveInput = _inputs.MoveInput;

            if (moveInput.magnitude == 0)
            {
                _moveInputCached = Vector2.zero;
                _moveInputMagnitude = 0;
                return;
            }
            
            _moveInputMagnitude = Mathf.MoveTowards(_moveInputMagnitude, 1f, Time.deltaTime / inputResponseTime);
            _moveInputCached = Vector2.MoveTowards(_moveInputCached, moveInput, _moveInputMagnitude * _moveInputMagnitude);
        }

        private void FixedUpdate()
        {
            if (PauseManager.IsPaused)
                return;

            float dt = Time.fixedDeltaTime;
            
            controlStrategy.FixedUpdateFlight(this, dt);
        }

        private void OnValidate()
        {
            if (controlStrategy)
                controlStrategy.Apply();
        }

        private void OnEnable()
        {
            PlayerInputs.Instance.Accelerate += OnAccelerate;
            PlayerInputs.Instance.Brake += OnBrake;
        }
        
        private void OnDisable()
        {
            PlayerInputs.Instance.Accelerate -= OnAccelerate;
            PlayerInputs.Instance.Brake -= OnBrake;
        }

        private void OnAccelerate(bool performed) => _acceleratePressed = performed;
        private void OnBrake(bool performed) => _brakePressed = performed;

        #endregion


        private void ApplyWind()
        {
            ExternalWind = Vector3.zero;
            foreach (var draft in _drafts)
            {
                if (draft)
                    ExternalWind += draft.Force;
            }
        }
        
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

        private void OnTriggerEnter(Collider other)
        {
            if (other.TryGetComponent(out WindDraft draft))
            {
                _drafts.Add(draft);
            }
        }
        
        private void OnTriggerExit(Collider other)
        {
            if (other.TryGetComponent(out WindDraft draft))
            {
                _drafts.Remove(draft);
            }
        }
    }
}