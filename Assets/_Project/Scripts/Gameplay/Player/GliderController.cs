using Beakstorm.Inputs;
using UnityEngine;

namespace Beakstorm.Gameplay.Player
{
    public class GliderController : MonoBehaviour
    {
        [SerializeField] private Vector2 maxAngles = new Vector2(0f, 90f);
        [SerializeField] private float maxSpeed = 20f;
        [SerializeField] private float minSpeed = 10f;
        [SerializeField] private float acceleration = 5f;
        
        private PlayerInputs _inputs;
        private Vector3 _eulerAngles;
        
        [SerializeField] private float _speed;

        private float _roll;
        
        private Transform t;

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
            SteerInput();
            HandleAcceleration();
            Move();
        }

        #endregion


        private void SteerInput()
        {
            Vector2 inputVector = _inputs.MoveInput;

            _eulerAngles = t.localEulerAngles;

            _eulerAngles.x -= inputVector.y;

            if (_eulerAngles.x > 180)
                _eulerAngles.x -= 360;
            
            _eulerAngles.x = Mathf.Max(_eulerAngles.x, maxAngles.x);
            _eulerAngles.x = Mathf.Min(_eulerAngles.x, maxAngles.y);

            _roll = Mathf.Lerp(_roll, -inputVector.x * 30f, 0.01f);
            
            _eulerAngles.z = _roll;

            t.localEulerAngles = _eulerAngles;
            
            t.Rotate(0.0f, inputVector.x, 0.0f, Space.World);
        }

        private void HandleAcceleration()
        {
            Vector3 flatForward = t.forward;
            flatForward.y = 0f;
            
            float angle = Vector3.SignedAngle(t.forward, flatForward, t.right);
            float strength = angle / 360f;
            _speed -= strength * Mathf.Abs(strength) * acceleration * Time.deltaTime;
            _speed = Mathf.Clamp(_speed, minSpeed, maxSpeed);
        }
        
        private void Move()
        {
            t.position += t.forward * (_speed * Time.deltaTime);
        }
    }
}