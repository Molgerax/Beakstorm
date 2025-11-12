using Beakstorm.Core.Variables;
using UnityEngine;

namespace Beakstorm.Gameplay.Player.Flying
{
    [CreateAssetMenu(menuName = "Beakstorm/Player/FlightControlStrategy/SpeedBased")]
    public class SpeedBasedFlightControlStrategy : FlightControlStrategy
    {
        [SerializeField] private Vector2 maxAngles = new Vector2(-80f, 70f);
        [SerializeField] private float maxSpeed = 60f;
        [SerializeField] private float minSpeed = 10f;
        [SerializeField] private float stallSpeed = 10f;

        [SerializeField] private float mass = 100;

        [Header("Thrust")]
        [SerializeField] private float throttleSpeed = 1f;
        [SerializeField] private float throttleReset = 1f;
        [SerializeField] private float minThrust = 10f;
        [SerializeField] private float maxThrust = 100f;

        [SerializeField] private AnimationCurve thrustAoaCurve = AnimationCurve.Constant(-90, 90, 0);
        
        [Header("Drag")] 
        [SerializeField] private float breakDrag = 1f;
        [SerializeField] private float dragPower = 1f;
        [SerializeField] private float dragMult = 1f;
        [SerializeField] private AnimationCurve dragCurve = AnimationCurve.Constant(0, 100, 1);

        [SerializeField] private float liftPower = 100f;
        [SerializeField] private AnimationCurve angleOfAttackCurve = AnimationCurve.EaseInOut(-90, 0, 90, 0);

        
        [SerializeField] private float gravity = 10;
        
        [SerializeField] private float rollSpeed = 3;

        [SerializeField] private float steerSpeed = 60;
        [SerializeField] private AnimationCurve steerSpeedCurve = AnimationCurve.Constant(0, 100, 1);


        private bool _flipping;
        
        public override float Speed01(float speed) => (speed - minSpeed) / (maxSpeed - minSpeed);

        public float GetSteerSpeed(float speed) => steerSpeedCurve.Evaluate((speed)) * steerSpeed;


        public override void Initialize(GliderController glider, float dt)
        {
            glider.Speed = minSpeed;

            glider.Rigidbody.isKinematic = true;
            glider.Rigidbody.useGravity = false;

            glider.EulerAngles = glider.T.eulerAngles;
            
            glider.Velocity = Vector3.forward * stallSpeed;
        }

        public override void UpdateFlight(GliderController glider, float dt)
        {
            UpdateSteering(glider, dt);
            
            glider.OldVelocity = glider.Velocity;
            UpdateAcceleration(glider, dt);

            Vector3 up = glider.T.up;;
            Vector3 forward = glider.Velocity.normalized;
            //glider.T.rotation = Quaternion.LookRotation(forward, up);

            glider.T.position += glider.Velocity * dt;

            glider.Speed = glider.Velocity.magnitude;

            glider.SpeedVariable.Min = 0;
            glider.SpeedVariable.Max = maxSpeed;
        }

        private void UpdateSteering(GliderController glider, float dt)
        {
            Vector2 inputVector = glider.MoveInput;

            Vector3 forwards = glider.T.forward;
            Vector3 ups = glider.T.up;

            Vector3 localEulerAngles = glider.T.localEulerAngles;
            localEulerAngles = glider.EulerAngles;
            Quaternion localRotation = glider.T.localRotation;

            Quaternion removeRoll = Quaternion.AngleAxis(glider.Roll, localRotation * Vector3.forward);
            localRotation = Quaternion.Inverse(removeRoll) * localRotation;
            
            float stalling = 1 - Mathf.Clamp01((glider.Speed - minSpeed) / (stallSpeed - minSpeed));

            float pitch = Vector3.SignedAngle(Vector3.up, forwards, glider.T.right);

            localRotation *= Quaternion.Euler(-Vector3.right * inputVector.y * dt * GetSteerSpeed(glider.Thrust01 * 100));
            localEulerAngles.x -= inputVector.y * dt * GetSteerSpeed(glider.Thrust01 * 100);

            if (glider.Speed < stallSpeed && pitch > 0 && pitch < 170)
            {
                localRotation *= Quaternion.Euler(Vector3.right * dt * stalling * steerSpeed);
                localEulerAngles.x += dt * stalling * steerSpeed;
            }

            if (pitch < 0)
            {
                localRotation *= Quaternion.Euler(-Vector3.right * dt * steerSpeed * 0.125f);
                localEulerAngles.x -= dt * steerSpeed * 0.125f;
            }
            
            if (localEulerAngles.x > 180)
                localEulerAngles.x -= 360;

            if (localEulerAngles.x < -180)
                localEulerAngles.x += 360;
            
            //localEulerAngles.x = Mathf.Max(localEulerAngles.x, maxAngles.x);
            //localEulerAngles.x = Mathf.Min(localEulerAngles.x, maxAngles.y);

            float yAcceleration = inputVector.x * dt * GetSteerSpeed(glider.Thrust01 * 100);

            
            float rollAngle = Mathf.Lerp(20f, 80f, glider.Speed01);

            
            
            Vector3 up = Vector3.up;

            Vector3 forces = Vector3.zero;
            forces += Vector3.down * gravity;

            Vector3 angularVelocity = Vector3.up * (yAcceleration * Mathf.Deg2Rad) / dt;
            float radius = Mathf.Sqrt(glider.Velocity.sqrMagnitude / angularVelocity.sqrMagnitude);
            
            Vector3 centripetalForce = Vector3.Cross(angularVelocity,  glider.T.forward * glider.Speed);

            //centripetalForce = (glider.Velocity - glider.OldVelocity) / dt;
            
            forces += centripetalForce;
            
            float targetRoll = -Vector3.SignedAngle(-up, forces.normalized, glider.Velocity.normalized);

            //glider.Roll = Mathf.Lerp(glider.Roll, -inputVector.x * rollAngle, 1 - Mathf.Exp(-rollSpeed * dt));
            glider.Roll = Mathf.Lerp(glider.Roll, targetRoll, 1 - Mathf.Exp(-rollSpeed * dt));
            //glider.Roll = targetRoll;

            
            //yAcceleration *= pitch < 0 ? -1 : 1;
            Quaternion yaw = Quaternion.Euler(0, yAcceleration, 0);
            
            localRotation *= Quaternion.Inverse(localRotation) * yaw * localRotation;

            if (_flipping && pitch > 0 && Mathf.Abs(glider.Roll) < 10f)
                _flipping = false;
            
            
            if (pitch < 0 && Mathf.Abs(yAcceleration) > 0.5f && !_flipping && Mathf.Abs(glider.Roll) < 10f)
            {
                float angleDist = 180 - (localEulerAngles.x % 180) * 2;
                localEulerAngles.x += angleDist;
                localEulerAngles.y += 180;

                angleDist = 180 - (glider.Roll % 180) * 2;
                glider.Roll += angleDist * Mathf.Sign(yAcceleration);
                _flipping = true;
            }


            localEulerAngles.y += yAcceleration;
            localEulerAngles.z = glider.Roll;

            glider.LocalRotation = localRotation;
            
            localRotation = Quaternion.AngleAxis(glider.Roll, localRotation * Vector3.forward) * localRotation;
            glider.T.localRotation = localRotation;

            glider.EulerAngles = localEulerAngles;

            //glider.T.localEulerAngles = localEulerAngles;
            glider.T.localRotation = Quaternion.Euler(glider.EulerAngles);

            //glider.T.Rotate(0.0f, yAcceleration, 0.0f, Space.World);
        }

        private void UpdateAcceleration(GliderController glider, float dt)
        {
            Vector3 forward = glider.T.forward;
            Vector3 flatForward = forward;
            flatForward.y = 0f;

            if (Vector3.Dot(glider.T.up, Vector3.up) < 0)
                flatForward *= -1;
            
            float force = 0;
            
            float angle = Vector3.SignedAngle(forward, flatForward, glider.T.right);

            float gravityPull = Vector3.Dot(Vector3.up, forward) * gravity;
            
            //angleStrength = Vector3.Dot(forward, Vector3.up);
            
            forward = Vector3.forward;
            
            float inputStrength = 0;
            inputStrength += glider.ThrustInput ? 1 : 0;
            //inputStrength -= glider.BreakInput ? 1 : 0;

            //if (Mathf.Abs(inputStrength) < 0.1f)
            glider.Thrust =
                    Mathf.Lerp(glider.Thrust, minThrust, 1 - Mathf.Exp(-throttleReset * dt));
            
            
            
            glider.Thrust += inputStrength * dt * throttleSpeed;
            glider.Thrust = Mathf.Clamp(glider.Thrust, minThrust, maxThrust);

            glider.Thrust01 = glider.Thrust / maxThrust;

            float appliedThrust = glider.Thrust;
            float maxSpeedForThrust = (1 + thrustAoaCurve.Evaluate(angle)) * maxSpeed * 0.5f;
            float thrustStrength = Mathf.Clamp01((glider.Speed / (maxSpeedForThrust)));
            thrustStrength *= thrustStrength;

            thrustStrength = 1 - thrustStrength;
            thrustStrength = Mathf.Lerp(0.1f, 1f, thrustStrength);
            
            force += appliedThrust * mass * thrustStrength;
            force += GetThrustFromWind(glider.T.forward, glider.ExternalWind) * mass;

            glider.ThrustVariable.Min = minThrust;
            glider.ThrustVariable.Max = maxThrust;
            glider.ThrustVariable.Set(glider.Thrust);

            force -= gravityPull * mass * angleOfAttackCurve.Evaluate(angle);
            //force += (angleStrength * gravity * mass);
            
            float drag = CalculateDrag(glider.Velocity, glider.BreakInput ? breakDrag : 0);
            force += drag;
            
            glider.Speed += (force / mass) * dt;

            //_speed += (inputStrength + angleStrength * Mathf.Abs(angleStrength)) * acceleration * Time.deltaTime;
            glider.Speed = Mathf.Clamp(glider.Speed, minSpeed, maxSpeed);

            glider.Velocity = glider.T.forward * glider.Speed;
        }

        private float CalculateDrag(Vector3 velocity, float coAdditive = 0)
        {
            float vel2 = velocity.magnitude;
            vel2 = Mathf.Pow(vel2, dragPower);

            float coefficient = (dragCurve.Evaluate(velocity.magnitude) + coAdditive) * dragMult;
            
            return -coefficient * vel2;
        }
        
        private Vector3 CalculateLift(GliderController glider, Vector3 velocity)
        {
            float vel2 = velocity.sqrMagnitude;

            float coefficient = angleOfAttackCurve.Evaluate(velocity.magnitude);
            
            return coefficient * vel2 * -velocity.normalized;
        }

        private float GetThrustFromWind(Vector3 heading, Vector3 wind)
        {
            if (wind.magnitude < 0.1f)
                return 0;
            
            float alignment = Vector3.Dot(heading.normalized, wind);
            return Mathf.Max(alignment, -minThrust * 0.5f);
        }
    }
}
