using Beakstorm.Inputs;
using Beakstorm.Pausing;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Beakstorm.Gameplay.Player
{
    [DefaultExecutionOrder(-20)]
    public class CameraController : MonoBehaviour
    {
        public static CameraController Instance;
        
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
        
        [SerializeField]
        [Min(0f)] private float lookThreshold = 0.5f;

        [SerializeField] private bool disableRotation = false;

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


        private float _timeSinceNoLook;
        private float _timeSinceCenter;

        [System.NonSerialized] public Vector3 LookAhead;
        [System.NonSerialized] public bool FlipHard;

        private Vector3 _lookAheadSmooth;
        private Vector3 _lookAheadSmoothSpeed;

        private bool _switchCamera;
        private bool _freeLook;
        private bool _lookAtTarget;

        [SerializeField] private Vector3 _headOffset;
        
        public static bool UseManualCamera { get; private set; }
        
        #region Mono Methods
        private void Awake()
        {
            _inputs = PlayerInputs.Instance;
            _headOffset = cameraHead.localPosition;

            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        private void OnEnable()
        {
            _inputs.SwitchCamera += OnSwitchCameraInput;
            _inputs.FreeLook += OnFreeLookInput;
            _inputs.LookAtTarget += OnLookAtTargetInput;
        }



        private void OnDisable()
        {
            _inputs.SwitchCamera -= OnSwitchCameraInput;
            _inputs.FreeLook -= OnFreeLookInput;
            _inputs.LookAtTarget -= OnLookAtTargetInput;
        }

        private void Update()
        {
            if (PauseManager.IsPaused)
                return;
            
            transform.position = playerTarget.position;
            
            OnLookInput();
            UseManualCamera = _useManualCamera;

            if (_headOffset.sqrMagnitude > 0)
                cameraHead.localPosition = playerTarget.TransformDirection(_headOffset);
            else
                cameraHead.localPosition = Vector3.zero;
        }

        #endregion

        public void SetRotationEnabled(bool value) => disableRotation = !value;

        private void InputTimeHandling()
        {
            //_useManualCamera = false;
            //return;
            
            _timeSinceCenter = Mathf.Max(0, _timeSinceCenter - Time.deltaTime);

            if (_inputs.LookInput.magnitude == 0 && _useManualCamera && !_freeLook) 
            {
                _timeSinceNoLook += Time.deltaTime;

                if (_timeSinceNoLook > 0.15f)
                {
                    _timeSinceNoLook = 0;
                    _useManualCamera = false;
                }
            }
            
            if (_inputs.LookInput.magnitude * 0.1f > Mathf.Max(_timeSinceCenter / centerCooldown, lookThreshold))
            {
                _useManualCamera = true;
                _timeSinceNoLook = 0;
            }
            
            if (_useManualCamera)
            {
                _timeReturnView = 0;
                _cachedNormalRotation = _normalRotation * Quaternion.Inverse(_fixedRotation);
            }
        }
        
        
        private void OnFreeLookInput(bool performed)
        {
            _freeLook = performed;
        }
        
        private void OnSwitchCameraInput(bool performed)
        {
            _switchCamera = performed;
        }
        
        private void OnLookAtTargetInput(bool performed)
        {
            _lookAtTarget = performed;
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

            Quaternion final = _outputRotation;

            if (_switchCamera)
                final = Quaternion.LookRotation(-(_outputRotation * Vector3.forward), _outputRotation * Vector3.up);

            if (!disableRotation)
                cameraHead.localRotation = final;
        }

        private float SlerpT(float t) => 1f - Mathf.Exp(-t * Time.deltaTime);

        private void HandleFixedCamera()
        {
            Vector3 up = Vector3.Dot(playerTarget.up, Vector3.up) > 0 ? Vector3.up : Vector3.down;

            if (FlipHard)
                up = playerTarget.up;
            
            
            _lookAheadSmooth = Vector3.SmoothDamp(_lookAheadSmooth, LookAhead, ref _lookAheadSmoothSpeed, 0.1f);
            _lookAheadSmooth = Vector3.zero;
            
            Vector3 playerForward = playerTarget.forward;
            Vector3 playerRight = Vector3.Cross(up, playerForward).normalized;
            Vector3 playerUp = Vector3.Cross(playerForward, playerRight).normalized;
            
            Vector3 lookAheadForward = (playerForward + playerRight * _lookAheadSmooth.x + playerUp * _lookAheadSmooth.y + playerForward * _lookAheadSmooth.z);
            lookAheadForward.Normalize();
            
            Quaternion playerRotation = playerTarget.rotation;

            Vector3 eulerAngles = playerRotation.eulerAngles;
            eulerAngles.z = 0;
            //playerRotation = Quaternion.Euler(eulerAngles);


            Quaternion targetRotation =
                Quaternion.LookRotation(new Vector3(playerForward.x, 0, playerForward.z).normalized, up);
            targetRotation = Quaternion.LookRotation(lookAheadForward, up);

            playerRotation = Quaternion.Slerp(playerRotation, targetRotation, yAlpha);
            playerRotation = targetRotation;
            
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