using Beakstorm.Core.Variables;
using UnityEngine;

namespace Beakstorm.Gameplay.Player.Flying
{
    [CreateAssetMenu(menuName = "Beakstorm/Player/FlightControlStrategy/Advanced")]
    public class AdvancedFlightControlStrategy : FlightControlStrategy
    {
        [SerializeField] private Vector2 maxAngles = new Vector2(-80f, 70f);
        [SerializeField] private float maxSpeed = 60f;
        [SerializeField] private float minSpeed = 10f;

        [SerializeField] private float mass = 100;

        [SerializeField] private float throttleSpeed = 1f;
        [SerializeField] private float minThrust = 10f;
        [SerializeField] private float maxThrust = 100f;
        [SerializeField] private AnimationCurve dragCurve = AnimationCurve.Constant(0, 100, 1);

        [SerializeField] private float liftPower = 100f;
        [SerializeField] private AnimationCurve angleOfAttackCurve = AnimationCurve.EaseInOut(-90, 0, 90, 0);

        
        [SerializeField] private float gravity = 10;
        
        [SerializeField] private float rollSpeed = 3;

        [SerializeField] private float steerSpeed = 60;
        [SerializeField] private AnimationCurve steerSpeedCurve = AnimationCurve.Constant(0, 100, 1);
        
        
        public override float Speed01(float speed) => (speed - minSpeed) / (maxSpeed - minSpeed);

        public float GetSteerSpeed(float speed) => steerSpeedCurve.Evaluate((speed)) * steerSpeed;


        public override void Initialize(GliderController glider, float dt)
        {
            glider.Speed = minSpeed;

            glider.Rigidbody.isKinematic = true;
            glider.Rigidbody.useGravity = false;

            glider.Velocity = Vector3.forward * minSpeed;
        }

        public override void UpdateFlight(GliderController glider, float dt)
        {
            UpdateSteering(glider, dt);
            
            UpdateAcceleration(glider, dt);

            Vector3 up = glider.T.up;;
            Vector3 forward = glider.Velocity.normalized;
            //glider.T.rotation = Quaternion.LookRotation(forward, up);

            glider.T.position += glider.T.rotation * glider.Velocity * dt;

            glider.Speed = glider.Velocity.magnitude;

            glider.SpeedVariable.Min = 0;
            glider.SpeedVariable.Max = maxSpeed;
        }

        private void UpdateSteering(GliderController glider, float dt)
        {
            Vector2 inputVector = glider.MoveInput;

            Vector3 localEulerAngles = glider.T.localEulerAngles;
            
            localEulerAngles.x -= inputVector.y * dt * GetSteerSpeed(glider.Speed);

            if (localEulerAngles.x > 180)
                localEulerAngles.x -= 360;
            
            localEulerAngles.x = Mathf.Max(localEulerAngles.x, maxAngles.x);
            localEulerAngles.x = Mathf.Min(localEulerAngles.x, maxAngles.y);

            float yAcceleration = inputVector.x * dt * GetSteerSpeed(glider.Speed);

            float rollAngle = Mathf.Lerp(20f, 80f, glider.Speed01);

            
            
            Vector3 up = Vector3.up;

            Vector3 forces = Vector3.zero;
            forces += Vector3.down * gravity;

            Vector3 angularVelocity = Vector3.up * (yAcceleration * Mathf.Deg2Rad);
            float radius = Mathf.Sqrt(glider.Velocity.sqrMagnitude / angularVelocity.sqrMagnitude);
            
            Vector3 centripetalForce = Vector3.Cross(angularVelocity.normalized,  glider.T.forward) * angularVelocity.magnitude * glider.Speed;
            forces += centripetalForce;
            
            float targetRoll = -Vector3.SignedAngle(-up, forces.normalized, glider.T.rotation * glider.Velocity.normalized);

            //glider.Roll = Mathf.Lerp(glider.Roll, -inputVector.x * rollAngle, 1 - Mathf.Exp(-rollSpeed * dt));
            glider.Roll = Mathf.Lerp(glider.Roll, targetRoll, 1 - Mathf.Exp(-rollSpeed * dt));
            //glider.Roll = targetRoll;
            
            localEulerAngles.z = glider.Roll;

            
            
            glider.T.localEulerAngles = localEulerAngles;
            glider.T.Rotate(0.0f, yAcceleration, 0.0f, Space.World);
        }

        private void UpdateAcceleration(GliderController glider, float dt)
        {
            Vector3 forward = glider.T.forward;
            Vector3 flatForward = forward;
            flatForward.y = 0f;


            Vector3 force = Vector3.zero;
            
            float angle = Vector3.SignedAngle(forward, flatForward, glider.T.right);
            float angleStrength = -angle / 180f;

            //angleStrength = Vector3.Dot(forward, Vector3.up);
            
            forward = Vector3.forward;
            
            float inputStrength = 0;
            inputStrength += glider.ThrustInput ? 1 : 0;
            inputStrength -= glider.BreakInput ? 1 : 0;

            glider.Thrust =
                Mathf.MoveTowards(glider.Thrust, (maxThrust + minThrust) * 0.5f, throttleSpeed * dt * 0.125f);

            glider.Thrust += inputStrength * dt * throttleSpeed;
            glider.Thrust = Mathf.Clamp(glider.Thrust, minThrust, maxThrust);

            glider.Thrust01 = glider.Thrust / maxThrust;


            glider.ThrustVariable.Min = minThrust;
            glider.ThrustVariable.Max = maxThrust;
            glider.ThrustVariable.Set(glider.Thrust);
            
            force += forward * (angleStrength * gravity * mass);

            force += forward * glider.Thrust * mass;

            Vector3 drag = CalculateDrag(glider.Velocity, glider.BreakInput ? 1 : 0);
            force += drag;
            
            glider.Velocity += (force / mass) * dt;
            glider.Speed = glider.Velocity.magnitude;

            //_speed += (inputStrength + angleStrength * Mathf.Abs(angleStrength)) * acceleration * Time.deltaTime;
            glider.Speed = Mathf.Clamp(glider.Speed, minSpeed, maxSpeed);

            glider.Velocity = glider.Velocity.normalized * glider.Speed;
        }

        private Vector3 CalculateDrag(Vector3 velocity, float coAdditive = 0)
        {
            float vel2 = velocity.sqrMagnitude;

            float coefficient = dragCurve.Evaluate(velocity.magnitude) + coAdditive;
            
            return coefficient * vel2 * -velocity.normalized;
        }
        
        private Vector3 CalculateLift(GliderController glider, Vector3 velocity)
        {
            float vel2 = velocity.sqrMagnitude;

            float coefficient = angleOfAttackCurve.Evaluate(velocity.magnitude);
            
            return coefficient * vel2 * -velocity.normalized;
        }
    }
}
