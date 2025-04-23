using Beakstorm.Inputs;
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

        [SerializeField] private Vector2 maxAngles = new Vector2(0f, 90f);
        
        [SerializeField] private Vector2 lookAngles = new Vector2(170f, 90f);


        [SerializeField]
        [Range(0.1f, 1f)] private float mouseSensitivity = 1;
        
        [SerializeField] private float inputFactor = 60;

        [SerializeField]
        [Range(0f, 1f)] private float lookAlpha = 1;
        [SerializeField]
        [Range(0f, 1f)] private float movementAlpha = 1;
        [SerializeField]
        [Range(0f, 1f)] private float yAlpha = 1;

        [SerializeField] private bool useFixedCamera;
        
        #endregion

        private PlayerInputs _inputs;
        private Vector3 _eulerAngles;

        private Vector2 _look;
        private Vector2 _lookAverage;
        private Quaternion _playerAverage;
        
        private float _pitch;
        
        #region Mono Methods
        private void Awake()
        {
            _inputs = PlayerInputs.Instance;
            _inputs.switchCameraAction.performed += OnSwitchCameraInput;
            
            
            _eulerAngles = cameraHead.localEulerAngles;
            _pitch = _eulerAngles.x;
        }

        private void Update()
        {
            transform.position = playerTarget.position;
            OnLookInput();
        }

        private void LateUpdate()
        {
            //transform.position = playerTarget.position;
        }

        #endregion


        
        
        private void OnLookInput()
        {
            if (useFixedCamera)
                HandleFixedCamera();
            else
                HandleDefaultCamera();
        }

        private void OnSwitchCameraInput(InputAction.CallbackContext callbackContext)
        {
            useFixedCamera = !useFixedCamera;
        }
        
        private void HandleFixedCamera()
        {
            Vector2 inputVector = _inputs.LookInput;

            Vector2 targetLookAngle = Vector2.Scale(inputVector, lookAngles);

            _lookAverage = Vector2.Lerp(_lookAverage, targetLookAngle, lookAlpha);

            Quaternion playerRotation = playerTarget.rotation;
            
            playerRotation = Quaternion.Slerp(playerRotation, Quaternion.LookRotation(new Vector3(playerTarget.forward.x, 0, playerTarget.forward.z).normalized), yAlpha);
            
            _playerAverage = Quaternion.Slerp(_playerAverage, playerRotation, movementAlpha);
            
            
            Quaternion rotation = Quaternion.Euler(-_lookAverage.y, _lookAverage.x, 0);

            _pitch -= inputVector.y * mouseSensitivity;
            _pitch = Mathf.Clamp(_pitch, maxAngles.x, maxAngles.y);
            _eulerAngles = cameraHead.localEulerAngles;
       
            //cameraHead.localEulerAngles = new Vector3(_pitch, _eulerAngles.y, _eulerAngles.z);

            //ransform.Rotate(0.0f, inputVector.x, 0.0f, Space.World);

            cameraHead.localRotation =  _playerAverage * rotation;
        }

        private void HandleDefaultCamera()
        {
            Vector2 inputVector = _inputs.LookInput;

            Vector2 targetLookAngle = Vector2.Scale(inputVector, lookAngles);

            _lookAverage = Vector2.Lerp(_lookAverage, targetLookAngle, lookAlpha);

            Quaternion playerRotation = playerTarget.rotation;
            
            playerRotation = Quaternion.Slerp(playerRotation, Quaternion.LookRotation(new Vector3(playerTarget.forward.x, 0, playerTarget.forward.z).normalized), yAlpha);
            
            _playerAverage = Quaternion.Slerp(_playerAverage, playerRotation, movementAlpha);

            _playerAverage = cameraHead.rotation;
            
            Quaternion rotation = Quaternion.Euler(-_lookAverage.y, _lookAverage.x, 0);

            _pitch -= inputVector.y * mouseSensitivity * Time.deltaTime * inputFactor;
            _pitch = Mathf.Clamp(_pitch, maxAngles.x, maxAngles.y);
            _eulerAngles = cameraHead.localEulerAngles;

            cameraHead.localEulerAngles = new Vector3(_pitch, _eulerAngles.y + inputVector.x * mouseSensitivity * Time.deltaTime * inputFactor, 0);
            //cameraHead.Rotate(0.0f, inputVector.x * mouseSensitivity, 0.0f, Space.World);
        }
    }
}