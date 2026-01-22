using Beakstorm.Gameplay.Targeting;
using Beakstorm.Inputs;
using Beakstorm.Pausing;
using Beakstorm.Utility;
using UnityEngine;

namespace Beakstorm.Gameplay.Player
{
    [DefaultExecutionOrder(-20)]
    public class CameraController : MonoBehaviour
    {
        public static CameraController Instance;
        
        #region SerializeFields
        
        [SerializeField] private Transform playerTarget;
        [SerializeField] private Transform cameraHead;
        [SerializeField] private Transform lookAheadTransform;

        [SerializeField] private Vector2 maxAngles = new Vector2(-80f, 90f);

        [SerializeField]
        [Range(0f, 5f)] private float centerSpeed = 10;

        [SerializeField]
        [Range(0f, 2f)] private float centerCooldown = 1f;

        [SerializeField] 
        [Range(0.1f, 1f)] private float lookCenterTime = 0.5f;
        
        [SerializeField]
        [Range(1f, 25f)] private float movementAlpha = 10;
        
        [SerializeField]
        [Min(0f)] private float lookThreshold = 0.5f;

        [SerializeField] private bool disableRotation = false;

        [SerializeField] private TargetingManager targetingManager;

        [Header("Look Ahead")] 
        [SerializeField] private float lookAheadDistance = 100;

        #endregion

        private PlayerInputs _inputs;
        private Vector2 _look;
        private Vector2 _lookAverage;
        private Quaternion _fixedRotation;
        private Quaternion _freeLookRotation;
        private Quaternion _targetRotation;

        private Quaternion _outputRotation;
        private Quaternion _outputFinalRotation;
        
        private float _pitch;

        private float _timeReturnView;

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
        private bool _targetLock;
        private bool _wasTargetLock;
        
        private Vector3 _cachedTargetPosition;
        private Vector3 _previousTargetPosition;
        private float _targetPositionTime;

        private Vector3 _headOffset;

        private CameraMode _mode = CameraMode.Fixed;

        private Quaternion _cachedRotation;

        private enum CameraMode
        {
            Fixed = 0,
            FreeLook = 1,
            Target = 2,
        }
        
        public static bool UseManualCamera { get; private set; }

        Vector3 LookAheadPosition()
        {
            Vector3 pos = cameraHead.position + playerTarget.forward * lookAheadDistance;
            return pos;
        }
        
        #region Mono Methods
        private void Awake()
        {
            _inputs = PlayerInputs.Instance;
            _headOffset = cameraHead.localPosition;

            Instance = this;

            _targetPositionTime = 1;
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
            _inputs.ToggleLookAtTarget += OnToggleLookAtTargetInput;
        }


        private void OnDisable()
        {
            _inputs.SwitchCamera -= OnSwitchCameraInput;
            _inputs.FreeLook -= OnFreeLookInput;
            _inputs.LookAtTarget -= OnLookAtTargetInput;
            _inputs.ToggleLookAtTarget -= OnToggleLookAtTargetInput;
        }

        private void Update()
        {
            if (PauseManager.IsPaused)
                return;
            
            transform.position = playerTarget.position;
            
            if (_headOffset.sqrMagnitude > 0)
                cameraHead.localPosition = playerTarget.TransformDirection(_headOffset);
            else
                cameraHead.localPosition = Vector3.zero;
            
            OnLookInput();
            LookTowardsLookAhead();
            UseManualCamera = _mode == CameraMode.FreeLook;
        }

        #endregion

        public void SetRotationEnabled(bool value) => disableRotation = !value;

        private void InputTimeHandling()
        {
            _timeSinceCenter = Mathf.Max(0, _timeSinceCenter - Time.deltaTime);

            if (_inputs.LookInput.magnitude == 0 && _mode == CameraMode.FreeLook && !_freeLook) 
            {
                _timeSinceNoLook += Time.deltaTime;

                if (_timeSinceNoLook > lookCenterTime)
                {
                    _timeSinceNoLook = 0;
                    if (_targetLock)
                        SetMode(CameraMode.Target);
                    else
                        SetMode(CameraMode.Fixed);
                }
            }
            
            if (_inputs.LookInput.magnitude * 0.1f > Mathf.Max(_timeSinceCenter / centerCooldown, lookThreshold))
            {
                SetMode(CameraMode.FreeLook);
                _timeSinceNoLook = 0;
            }
            
            if (_mode == CameraMode.FreeLook)
            {
                _timeReturnView = 0;
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
            _targetLock = performed;

            if (performed)
                SetMode(CameraMode.Target);
            else
                SetMode(CameraMode.Fixed);
        }
        
        private void OnToggleLookAtTargetInput(bool performed)
        {
            if (!performed)
                return;

            _targetLock = !_targetLock;
            
            if (_mode == CameraMode.Target)
                SetMode(CameraMode.Fixed);
            else
                SetMode(CameraMode.Target);
        }

        private void SetMode(CameraMode mode)
        {
            if (_mode != mode)
            {
                if (mode == CameraMode.Target && !targetingManager.CurrentTarget)
                    return;
                
                _timeReturnView = 0f;
                _cachedRotation = GetRotation(_mode) * Quaternion.Inverse(GetRotation(mode));
            }
            
            _mode = mode;
        }
        
        private Quaternion GetRotation(CameraMode mode)
        {
            switch (mode)
            {
                case CameraMode.Fixed:
                    return _fixedRotation;
                case CameraMode.FreeLook:
                    return _freeLookRotation;
                case CameraMode.Target:
                    return _targetRotation;
                default:
                    return _fixedRotation;
            }
        }
        
        private void OnLookInput()
        {
            InputTimeHandling();
            
            HandleFixedCamera();
            HandleDefaultCamera();
            
            HandleTargetLookAt();

            if (_mode == CameraMode.FreeLook)
            {
                _outputRotation = _freeLookRotation;
                _outputFinalRotation = _outputRotation;
                
                cameraHead.localRotation = _outputFinalRotation;
            }
            else
            {
                if (Mathf.Abs(_timeReturnView - 1f) > 0.01f)
                    _timeReturnView = Mathf.Lerp(_timeReturnView, 1f, SlerpT(centerSpeed));
                else
                    _timeReturnView = 1f;
                
                
                
                //if (_timeReturnView < 1f)
                //    _outputRotation = Quaternion.Slerp(_cachedFreeLookRotation * GetRotation(_mode), GetRotation(_mode), _timeReturnView);
                //else
                _outputRotation = GetRotation(_mode);
                

                _outputFinalRotation = Quaternion.Slerp(_cachedRotation * _outputRotation, _outputRotation, _timeReturnView);

                //if (_switchCamera)
                //    final = Quaternion.LookRotation(-(_fixedRotation * Vector3.forward), _fixedRotation * Vector3.up);
             
                if (!disableRotation)
                    cameraHead.localRotation = _outputFinalRotation;
            }
            
            if (_switchCamera)
                cameraHead.localRotation = Quaternion.LookRotation(-(_fixedRotation * Vector3.forward), _fixedRotation * Vector3.up);
        }

        private float SlerpT(float t) => 1f - Mathf.Exp(-t * Time.deltaTime);


        private void HandleTargetLookAt()
        {
            Vector3 pos = transform.position;

            if (targetingManager.HasTargetChanged)
            {
                _targetPositionTime = 0;
                _previousTargetPosition = _cachedTargetPosition;
                targetingManager.HasTargetChanged = false;
            }
            
            if (targetingManager.CurrentTarget)
                _cachedTargetPosition = targetingManager.CurrentTarget.Position;
            else
            {
                if (_targetLock)
                {
                    _targetLock = false;
                    if (_mode == CameraMode.Target)
                        SetMode(CameraMode.Fixed);
                }
            }
            
            if (Mathf.Abs(_targetPositionTime - 1f) > 0.01f)
                _targetPositionTime = Mathf.Lerp(_targetPositionTime, 1f, SlerpT(centerSpeed));
            else
                _targetPositionTime = 1f;
            
            float t = _targetPositionTime;
            Vector3 targetPos = Vector3.Lerp(_previousTargetPosition, _cachedTargetPosition, t);
            
            _targetRotation = Quaternion.LookRotation(targetPos - pos);
        }

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

            lookAheadForward = playerForward;
            
            Quaternion targetRotation = Quaternion.LookRotation(lookAheadForward, up);
            _fixedRotation = Quaternion.Slerp(_fixedRotation, targetRotation, SlerpT(movementAlpha));
        }

        private void HandleDefaultCamera()
        {
            Vector2 inputVector = _inputs.LookInput * Time.deltaTime;
            
            var localEulerAngles = _outputFinalRotation.eulerAngles;

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
            
            _freeLookRotation = Quaternion.Euler(localEulerAngles);
        }

        private void LookTowardsLookAhead()
        {
            _lookAheadSmooth = Vector3.SmoothDamp(_lookAheadSmooth, LookAhead, ref _lookAheadSmoothSpeed, 0.1f);
            lookAheadTransform.localPosition = _lookAheadSmooth;

            var lookPos = LookAheadPosition();
            var camFwdPos = cameraHead.position + cameraHead.forward * lookAheadDistance;

            Quaternion rot = Quaternion.LookRotation(camFwdPos - lookAheadTransform.position, cameraHead.up);
            lookAheadTransform.rotation = rot;
        }
    }
}