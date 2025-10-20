using UnityEngine;

namespace Beakstorm.Gameplay.Player.Flying
{
    [CreateAssetMenu(menuName = "Beakstorm/Player/FlightControlStrategy/Simple")]
    public class SimpleFlightControlStrategy : FlightControlStrategy
    {
        [SerializeField] private Vector2 MaxAngles = new Vector2(0f, 90f);
        [SerializeField] private float MaxSpeed = 20f;
        [SerializeField] private float MinSpeed = 10f;
        [SerializeField] private float Acceleration = 5f;
        [SerializeField] private float Gravity = 10;
        [SerializeField] private float SteerSpeed = 60;
        [SerializeField] private float RollSpeed = 20;

        [SerializeField] private AnimationCurve steerSpeedCurve = AnimationCurve.Constant(0, 1, 1);
        
        
        public override float Speed01(float speed) => (speed - MinSpeed) / (MaxSpeed - MinSpeed);


        public float GetSteerSpeed(float speed) => steerSpeedCurve.Evaluate(Speed01(speed)) * SteerSpeed;


        public override void Initialize(GliderController glider, float dt)
        {
            glider.Speed = MinSpeed;

            glider.Rigidbody.isKinematic = true;
            glider.Rigidbody.useGravity = false;
        }

        public override void UpdateFlight(GliderController glider, float dt)
        {
            UpdateSteering(glider, dt);
            UpdateAcceleration(glider, dt);
            
            glider.T.position += glider.T.forward * (glider.Speed * dt);
        }

        private void UpdateSteering(GliderController glider, float dt)
        {
            Vector2 inputVector = glider.MoveInput;

            Vector3 localEulerAngles = glider.T.localEulerAngles;
            
            localEulerAngles.x -= inputVector.y * dt * GetSteerSpeed(glider.Speed);

            if (localEulerAngles.x > 180)
                localEulerAngles.x -= 360;
            
            localEulerAngles.x = Mathf.Max(localEulerAngles.x, MaxAngles.x);
            localEulerAngles.x = Mathf.Min(localEulerAngles.x, MaxAngles.y);

            float yAcceleration = inputVector.x * dt * GetSteerSpeed(glider.Speed);

            float rollAngle = Mathf.Lerp(20f, 80f, glider.Speed01);
            
            glider.Roll = Mathf.Lerp(glider.Roll, -inputVector.x * rollAngle, 1 - Mathf.Exp(-RollSpeed * dt));
            
            localEulerAngles.z = glider.Roll;

            glider.T.localEulerAngles = localEulerAngles;
            
            glider.T.Rotate(0.0f, yAcceleration, 0.0f, Space.World);
        }

        private void UpdateAcceleration(GliderController glider, float dt)
        {
            Vector3 flatForward = glider.T.forward;
            flatForward.y = 0f;
            
            float angle = Vector3.SignedAngle(glider.T.forward, flatForward, glider.T.right);
            float angleStrength = -angle / 180f;

            angleStrength = Vector3.Dot(glider.T.forward, Vector3.up);
            
            float inputStrength = 0;
            //inputStrength += _inputs.accelerateAction.IsPressed() ? 1 : 0;
            inputStrength -= glider.BreakInput ? 1 : -1;
            

            glider.Speed += inputStrength * Acceleration * dt;
            glider.Speed -= angleStrength * Gravity * dt;
            
            //_speed += (inputStrength + angleStrength * Mathf.Abs(angleStrength)) * acceleration * Time.deltaTime;
            glider.Speed = Mathf.Clamp(glider.Speed, MinSpeed, MaxSpeed);
        }
    }
}
