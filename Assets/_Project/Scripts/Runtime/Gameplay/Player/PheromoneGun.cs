using System;
using Beakstorm.Inputs;
using Beakstorm.Simulation.Particles;
using UltEvents;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Beakstorm.Gameplay.Player
{
    public class PheromoneGun : MonoBehaviour
    {
        [SerializeField, Range(0, 5f)] private float shootOffset = 3f;
        [SerializeField] private LayerMask layerMask;

        [SerializeField] private UltEvent onShoot;
        
        private PlayerInputs _inputs;
        private Camera _camera;

        private void Awake()
        {
            _inputs = PlayerInputs.Instance;
            _camera = Camera.main;
        }

        private void OnEnable()
        {
            _inputs.shootAction.performed += OnShootActionPerformed;
        }

        private void OnDisable()
        {
            
            _inputs.shootAction.performed -= OnShootActionPerformed;
        }

        private void Update()
        {
            UpdateWeapon(Time.deltaTime);
            
            return;
            
            if (_inputs.whistleAction.IsPressed())
            {
                BoidGridManager.Instance.RefreshWhistle(transform.position, 1f);
            }
        }

        private void UpdateWeapon(float deltaTime)
        {
            if (!PlayerController.Instance || !PlayerController.Instance.SelectedWeapon)
                return;
            
            PlayerController.Instance.SelectedWeapon.UpdateWeapon(deltaTime);
        }
        
        private void OnShootActionPerformed(InputAction.CallbackContext callback)
        {
            if (!PlayerController.Instance || !PlayerController.Instance.SelectedWeapon)
                return;

            Vector3 pos = transform.position;
            Vector3 lookDir = GetShootDirection(pos);

            Vector3 dir = transform.forward;
            
            Vector3 targetNormal = Vector3.up;
            Vector3 targetPos = GetTargetPos(out targetNormal);
            
            pos += dir * shootOffset;

            FireInfo fireInfo = new FireInfo(pos, dir, lookDir, targetPos, targetNormal, 0);
            
            if (PlayerController.Instance.SelectedWeapon.Fire(fireInfo))
                onShoot?.Invoke();
        }

        private Vector3 GetShootDirection(Vector3 shootPosition)
        {
            if (!_camera) return transform.forward;
            
            Ray ray = _camera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
            if (Physics.Raycast(ray, out RaycastHit hit, Single.MaxValue, layerMask))
            {
                return (hit.point - shootPosition).normalized;
            }

            return _camera.transform.forward;
        }
        
        private Vector3 GetTargetPos(out Vector3 targetNormal)
        {
            targetNormal = -transform.forward;
            if (!_camera) return transform.position + transform.forward * 500;

            Ray ray = _camera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));

            Vector3 shotPosition = ray.origin + ray.direction * 500f;
            targetNormal = -ray.direction;
            
            if (Physics.Raycast(ray, out RaycastHit hit, Single.MaxValue, layerMask))
            {
                targetNormal = hit.normal;
                return hit.point;
            }

            return shotPosition;
        }
    }

    public struct FireInfo
    {
        public Vector3 InitialPosition;
        public Vector3 InitialDirection;
        public Vector3 LookDirection;
        public Vector3 TargetPosition;
        public Vector3 TargetNormal;
        public float Speed;

        public FireInfo(Vector3 position, Vector3 direction, Vector3 lookDirection, Vector3 targetPosition, Vector3 targetNormal, float speed)
        {
            InitialPosition = position;
            InitialDirection = direction;
            LookDirection = lookDirection;
            TargetPosition = targetPosition;
            TargetNormal = targetNormal;
            Speed = speed;
        }
    }
}