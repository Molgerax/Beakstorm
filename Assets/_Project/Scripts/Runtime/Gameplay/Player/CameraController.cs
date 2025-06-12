using System;
using Beakstorm.Inputs;
using Beakstorm.Pausing;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Beakstorm.Gameplay.Player
{
    [DefaultExecutionOrder(-20)]
    public class CameraController : MonoBehaviour
    {
        #region SerializeFields
        
        [SerializeField] private Transform playerTarget;
        [SerializeField] private Transform cameraHead;

        [SerializeField] private Vector2 maxAngles = new Vector2(-80f, 90f);

        [SerializeField]
        [Range(0f, 5f)] private float centerSpeed = 10;

        [SerializeField]
        [Range(0f, 2f)] private float centerCooldown = 1f;

        [SerializeField]
        [Range(1f, 25f)] private float movementAlpha = 10;
        [SerializeField]
        [Range(0f, 1f)] private float yAlpha = 0.5f;

        #endregion

        private PlayerInputs _inputs;
        private Vector2 _look;
        private Vector2 _lookAverage;
        private Quaternion _fixedRotation;
        private Quaternion _normalRotation;

        private Quaternion _cachedNormalRotation;
        private Quaternion _outputRotation;
        
        private float _pitch;

        private float _timeReturnView;

        private bool _useManualCamera;
        private float _moveInputSum;

        private float _roll;

        private float _timeSinceCenter;

        private Vector3 _headOffset;
        
        #region Mono Methods
        private void Awake()
        {
            _inputs = PlayerInputs.Instance;
            _headOffset = cameraHead.localPosition;
        }

        private void OnEnable()
        {
            _inputs.switchCameraAction.performed += OnSwitchCameraInput;
        }

        private void OnDisable()
        {
            _inputs.switchCameraAction.performed -= OnSwitchCameraInput;
        }

        private void Update()
        {
            if (PauseManager.IsPaused)
                return;
            
            transform.position = playerTarget.position;
            
            OnLookInput();

            //cameraHead.localPosition = playerTarget.TransformDirection(_headOffset);
        }

        #endregion


        private void InputTimeHandling()
        {
            _timeSinceCenter = Mathf.Max(0, _timeSinceCenter - Time.deltaTime);
            
            if (_inputs.LookInputRaw.magnitude > _timeSinceCenter / centerCooldown)
            {
                _useManualCamera = true;
            }
            
            if (_useManualCamera)
            {
                _timeReturnView = 0;
                _cachedNormalRotation = _normalRotation * Quaternion.Inverse(_fixedRotation);
            }
        }
        
        private void OnLookInput()
        {
            InputTimeHandling();
            
            HandleFixedCamera();
            HandleDefaultCamera();

            if (_useManualCamera)
            {
                _outputRotation = _normalRotation;
            }
            else
            {
                if (Mathf.Abs(_timeReturnView - 1f) > 0.01f)
                    _timeReturnView = Mathf.Lerp(_timeReturnView, 1f, SlerpT(centerSpeed));
                else
                    _timeReturnView = 1f;
                
                
                if (_timeReturnView < 1f)
                    _outputRotation = Quaternion.Slerp(_cachedNormalRotation * _fixedRotation, _fixedRotation, _timeReturnView);
                else
                    _outputRotation = _fixedRotation;
            }

            cameraHead.localRotation = _outputRotation;
        }

        private void OnSwitchCameraInput(InputAction.CallbackContext callbackContext)
        {
            _useManualCamera = false;
            _timeSinceCenter = centerCooldown;
        }

        private float SlerpT(float t) => 1f - Mathf.Exp(-t * Time.deltaTime);

        private void HandleFixedCamera()
        {
            Vector3 forward = playerTarget.forward;
            Quaternion playerRotation = playerTarget.rotation;

            Vector3 eulerAngles = playerRotation.eulerAngles;
            eulerAngles.z = 0;
            playerRotation = Quaternion.Euler(eulerAngles);

            playerRotation = Quaternion.Slerp(playerRotation, Quaternion.LookRotation(new Vector3(forward.x, 0, forward.z).normalized, Vector3.up), yAlpha);
            _fixedRotation = Quaternion.Slerp(_fixedRotation, playerRotation, SlerpT(movementAlpha));
        }

        private void HandleDefaultCamera()
        {
            Vector2 inputVector = _inputs.LookInput * Time.deltaTime;
            
            var localEulerAngles = cameraHead.localEulerAngles;

            float pitch = localEulerAngles.x;

            pitch -= inputVector.y;
            
            if (pitch > 180)
                pitch -= 360;
            
            pitch = Mathf.Clamp(pitch, maxAngles.x, maxAngles.y);

            localEulerAngles.x = pitch;

            localEulerAngles.y += inputVector.x;
            if (localEulerAngles.y > 180)
                localEulerAngles.y -= 360;

            localEulerAngles.z = Mathf.SmoothDampAngle(localEulerAngles.z, 0, ref _pitch, 0.25f);
            
            _normalRotation = Quaternion.Euler(localEulerAngles);
        }
    }
}