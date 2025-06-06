using System;
using Beakstorm.Inputs;
using Beakstorm.Simulation.Particles;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Beakstorm.Gameplay.Player
{
    public class PheromoneGun : MonoBehaviour
    {
        [SerializeField, Range(0, 5f)] private float shootOffset = 3f;
        [SerializeField] private LayerMask layerMask;
        
        private PlayerInputs _inputs;
        private Camera _camera;

        private void Awake()
        {
            _inputs = PlayerInputs.Instance;
            _camera = Camera.main;
            
            _inputs.shootAction.performed += OnShootActionPerformed;
        }

        private void Update()
        {
            UpdateWeapon(Time.deltaTime);
            
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
            Vector3 dir = GetShootDirection(pos);

            pos += dir * shootOffset;
            
            PlayerController.Instance.SelectedWeapon.Fire(pos, dir);
        }

        private Vector3 GetShootDirection(Vector3 shootPosition)
        {
            if (!_camera) return transform.forward;
            
            if (Physics.Raycast(_camera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0)), out RaycastHit hit, Single.MaxValue, layerMask))
            {
                return (hit.point - shootPosition).normalized;
            }

            return _camera.transform.forward;
        }
    }
}