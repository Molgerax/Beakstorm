using Beakstorm.Inputs;
using Beakstorm.Pausing;
using UnityEngine;

namespace Beakstorm.Gameplay.Player
{
    [DefaultExecutionOrder(-40)]
    public class GliderController : MonoBehaviour
    {
        [SerializeField] private Vector2 maxAngles = new Vector2(0f, 90f);
        [SerializeField] private float maxSpeed = 20f;
        [SerializeField] private float minSpeed = 10f;
        [SerializeField] private float acceleration = 5f;

        [SerializeField] private float steerSpeed = 60;

        [SerializeField] private float rollSpeed = 20;
        
        private PlayerInputs _inputs;
        private Vector3 _eulerAngles;
        
        [SerializeField] private float _speed;

        private float _roll;
        
        private Transform t;
        
        public float Speed01 => (_speed - minSpeed) / (maxSpeed - minSpeed);

        #region Mono Methods
        
        private void Awake()
        {
            _inputs = PlayerInputs.Instance;

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
            
            if (CameraFOV.Instance)
                CameraFOV.Instance.SetFoV(Speed01);
        }

        #endregion


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