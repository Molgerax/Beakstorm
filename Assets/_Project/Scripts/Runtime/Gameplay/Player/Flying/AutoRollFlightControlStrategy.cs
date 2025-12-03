using Beakstorm.Core.Variables;
using Beakstorm.Utility.Extensions;
using Unity.Mathematics;
using UnityEngine;

namespace Beakstorm.Gameplay.Player.Flying
{
    [CreateAssetMenu(menuName = "Beakstorm/Player/FlightControlStrategy/AutoRoll")]
    public class AutoRollFlightControlStrategy : FlightControlStrategy
    {
        [SerializeField] private float maxSpeed = 60f;
        [SerializeField] private float minSpeed = 10f;
        [SerializeField] private float stallSpeed = 10f;

        [SerializeField] private float cruiseSpeed = 40;
        
        [SerializeField] private float mass = 100;

        [SerializeField] private Vector3 turnTorque = new(90, 25, 45);
        [Tooltip("Angle at which airplane banks fully into target.")] public float aggressiveTurnAngle = 10f;
        
        [Header("Thrust")]
        [SerializeField] private float throttleSpeed = 1f;
        [SerializeField] private float throttleReset = 1f;
        [SerializeField] private float minThrust = 10f;
        [SerializeField] private float idleThrust = 20f;
        [SerializeField] private float maxThrust = 100f;
        
        [Header("OverCharge")]
        [SerializeField, Min(0)] private float chargeRate = 1;
        [SerializeField, Min(0)] private float dischargeRate = 1;
        [SerializeField, Min(0)] private float dischargeMult = 1;
        [SerializeField, Min(0)] private float chargeCapacity = 10;

        [Header("Drag")] 
        [SerializeField] private float breakDrag = 1f;

        [SerializeField] private AnimationCurve angleOfAttackCurve = AnimationCurve.EaseInOut(-90, 0, 90, 0);

        [SerializeField] private AnimationCurve aoaMaxSpeed = AnimationCurve.EaseInOut(0, 1, 90, 1);

        [SerializeField] private float gravity = 10;
        
        [SerializeField] private float rollSpeed = 3;

        [SerializeField] private float steerSpeed = 60;
        [SerializeField] private AnimationCurve steerThrustCurve = AnimationCurve.Constant(0, 100, 1);

        [SerializeField] private AnimationCurve debugCurve;


        private bool _flipping;

        [SerializeField] private float _a;
        [SerializeField] private float _b;
        [SerializeField] private float _c;

        private float _windXVel;
        private float _windYVel;

        private float _fov;
        private float _fovSpeed;

        private Vector3 _pointerDirection;
        private Vector3 _lastInput;

        private Vector3 _pointerEuler;
        
        public override float Speed01(float speed) => (speed - minSpeed) / (maxSpeed * 2 - minSpeed);

        public override bool UseMouseAim => true;

        public float GetSteerSpeed(GliderController glider)
        {
            float thrustReduction = steerThrustCurve.Evaluate(glider.Thrust01);

            float overChargeEnhancement = glider.Discharging ? 0 : Mathf.Clamp01(glider.OverCharge / (chargeCapacity));

            float factor = Mathf.Clamp(thrustReduction + overChargeEnhancement, 0, 1.5f);
            
            return factor * steerSpeed;
        }


        public override void Initialize(GliderController glider, float dt)
        {
            glider.Speed = minSpeed;

            glider.Rigidbody.isKinematic = true;
            glider.Rigidbody.useGravity = false;

            glider.EulerAngles = glider.T.eulerAngles;
            
            glider.Velocity = Vector3.forward * stallSpeed;

            _pointerDirection = glider.T.forward;
            
            _fov = 0;
            CalculateCoefficients();
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
            glider.SpeedVariable.Max = maxSpeed * 2;
            
            glider.OverChargeVariable.Min = 0;
            glider.OverChargeVariable.Max = chargeCapacity;

            glider.OverChargeVariable.Value = glider.OverCharge;

            _fov = Mathf.SmoothDamp(_fov,
                glider.OverCharge > 0 && !glider.Discharging
                    ? Mathf.Clamp01(glider.Speed01 - (glider.OverCharge / chargeCapacity))
                    : glider.Speed01, 
                ref _fovSpeed, 0.05f);
            
            glider.FovFactor = _fov;
            //glider.FovFactor = glider.Speed01;
        }

        private void UpdateSteering(GliderController glider, float dt)
        {
            Vector2 inputVector = glider.MoveInput;

            Vector3 forwards = glider.T.forward;
            Vector3 ups = glider.T.up;


            _pointerEuler.x += inputVector.y * GetSteerSpeed(glider) * dt;
            _pointerEuler.y += inputVector.x * GetSteerSpeed(glider) * dt;
            _pointerDirection = Quaternion.Euler(_pointerEuler) * Vector3.forward;
            //_pointerDirection = Camera.main.transform.forward;

            Vector3 error = _pointerDirection;
            error = Quaternion.Inverse(glider.T.rotation) * error;

            Vector3 errorDir = error.normalized;
            Vector3 pitchError = error.With(x: 0).normalized;
            Vector3 yawError = error.With(y: 0).normalized;
            Vector3 rollError = error.Add(y: 0).With(z: 0).normalized;

            Vector3 targetInput = new();

            float pitch = Vector3.SignedAngle(Vector3.forward, pitchError, Vector3.right);
            float yaw = Vector3.SignedAngle(Vector3.forward, yawError, Vector3.up);
            float roll = Vector3.SignedAngle(Vector3.up, rollError, Vector3.forward);
            
            
            if (MouseAimController.Instance)
                RunAutoPilot(glider, MouseAimController.Instance.MouseAimPos, out pitch, out yaw, out roll);
            else            
                RunAutoPilot(glider, _pointerDirection + glider.T.position, out pitch, out yaw, out roll);

            targetInput.x = pitch;
            targetInput.y = yaw;
            targetInput.z = -roll;
            
            //targetInput.x = Mathf.Clamp(targetInput.x, -1, 1);
            //targetInput.y = Mathf.Clamp(targetInput.y, -1, 1);
            //targetInput.z = Mathf.Clamp(targetInput.z, -1, 1);

            //var input = Vector3.MoveTowards(_lastInput, targetInput, 20 * dt);
            //_lastInput = input;

            targetInput.x *= turnTorque.x * dt;// GetSteerSpeed(glider) * dt;
            targetInput.y *= turnTorque.y * dt;// GetSteerSpeed(glider) * dt;
            targetInput.z *= turnTorque.z * dt;// GetSteerSpeed(glider) * dt;

            targetInput = glider.T.rotation * targetInput;

            glider.T.rotation = Quaternion.AngleAxis(targetInput.magnitude, targetInput.normalized) * glider.T.rotation;
            //glider.T.localRotation = Quaternion.Euler(glider.EulerAngles);
        }

        private void RunAutoPilot(GliderController glider, Vector3 flyTarget, out float pitch, out float yaw, out float roll)
        {
            var localFlyTarget = glider.T.InverseTransformPoint(flyTarget).normalized;
            var angleOffTarget = Vector3.Angle(glider.T.forward, flyTarget - glider.T.position);
            
            // IMPORTANT!
            // These inputs are created proportionally. This means it can be prone to
            // overshooting. The physics in this example are tweaked so that it's not a big
            // issue, but in something with different or more realistic physics this might
            // not be the case. Use of a PID controller for each axis is highly recommended.

            // ====================
            // PITCH AND YAW
            // ====================

            // Yaw/Pitch into the target so as to put it directly in front of the aircraft.
            // A target is directly in front the aircraft if the relative X and Y are both
            // zero. Note this does not handle for the case where the target is directly behind.
            yaw = Mathf.Clamp(localFlyTarget.x, -1f, 1f);
            pitch = -Mathf.Clamp(localFlyTarget.y, -1f, 1f);

            // ====================
            // ROLL
            // ====================

            // Roll is a little special because there are two different roll commands depending
            // on the situation. When the target is off axis, then the plane should roll into it.
            // When the target is directly in front, the plane should fly wings level.

            // An "aggressive roll" is input such that the aircraft rolls into the target so
            // that pitching up (handled above) will put the nose onto the target. This is
            // done by rolling such that the X component of the target's position is zeroed.
            var agressiveRoll = Mathf.Clamp(localFlyTarget.x, -1f, 1f);

            // A "wings level roll" is a roll commands the aircraft to fly wings level.
            // This can be done by zeroing out the Y component of the aircraft's right.
            var wingsLevelRoll = glider.T.right.y;

            // Blend between auto level and banking into the target.
            var wingsLevelInfluence = Mathf.InverseLerp(0f, aggressiveTurnAngle, angleOffTarget);
            roll = Mathf.Lerp(wingsLevelRoll, agressiveRoll, wingsLevelInfluence);
        }
        
        private void UpdateAcceleration(GliderController glider, float dt)
        {
            Vector3 forward = glider.T.forward;

            Vector3 flatForward = forward;
            flatForward.y = 0f;

            if (Vector3.Dot(glider.T.up, Vector3.up) < 0)
                flatForward *= -1;
            
            
            float force = 0;
            
            float angle = Vector3.SignedAngle(flatForward,forward,glider.T.right);

            float gravityPull = Vector3.Dot(Vector3.up, forward) * gravity;
            
            //angleStrength = Vector3.Dot(forward, Vector3.up);
            
            forward = Vector3.forward;
            
            UpdateThrustAndOverCharge(glider, dt, ref force);
            
            float appliedThrust = glider.Thrust;

            force += appliedThrust * mass;
            force += GetThrustFromWind(glider.T.forward, glider.ExternalWind) * mass;

            glider.ThrustVariable.Min = minThrust;
            glider.ThrustVariable.Max = maxThrust;
            glider.ThrustVariable.Set(glider.Thrust);

            force -= gravityPull * mass * angleOfAttackCurve.Evaluate(angle);
            //force += (angleStrength * gravity * mass);
            
            float drag = CalculateDrag(glider.Velocity, glider.BreakInput ? breakDrag : 0);
            float maxSpeedAoa = aoaMaxSpeed.Evaluate(angle) * maxSpeed;

            //force *= Mathf.Clamp01(1 - glider.Speed / maxSpeedAoa);

            force += drag * mass;
            
            //if (glider.Speed > maxSpeedAoa)
            //    force -= Mathf.Min((glider.Speed - maxSpeedAoa) / dt, maxSpeedReduction);
            
            glider.Speed += (force / mass) * dt;

            
            //_speed += (inputStrength + angleStrength * Mathf.Abs(angleStrength)) * acceleration * Time.deltaTime;
            glider.Speed = Mathf.Clamp(glider.Speed, minSpeed, maxSpeed * 2);

            glider.Velocity = glider.T.forward * glider.Speed;
        }

        private void UpdateThrustAndOverCharge(GliderController glider, float dt, ref float force)
        {
            float inputStrength = 0;
            inputStrength += glider.ThrustInput ? 1 : 0;
            if (glider.BreakInput)
                inputStrength = -1;

            if (glider.ThrustInput && glider.BreakInput && !glider.Discharging)
            {
                glider.OverCharge = Mathf.MoveTowards(glider.OverCharge, chargeCapacity, dt * chargeRate);
            }
            else if (glider.OverCharge > 0 && glider.Discharging)
            {
                float chargeBonus = Mathf.Clamp01(glider.OverCharge / chargeCapacity);

                float newCharge = Mathf.MoveTowards(glider.OverCharge, 0, dt * dischargeRate);
                float excess = Mathf.Max(glider.OverCharge - newCharge, 0);
                glider.OverCharge = newCharge;
                glider.Thrust += dt * throttleSpeed * 8;

                force += Mathf.Pow(chargeBonus, 1) * dischargeMult * mass;

                if (glider.OverCharge == 0)
                    glider.Discharging = false;
            }
            else if (glider.OverCharge > 0 && !glider.Discharging)
            {
                if (glider.BreakInput)
                    glider.OverCharge = 0;
                else
                    glider.Discharging = true;
            }
            
            //if (Mathf.Abs(inputStrength) < 0.1f)
            glider.Thrust =
                Mathf.Lerp(glider.Thrust, idleThrust, 1 - Mathf.Exp(-throttleReset * dt));
            
            
            
            glider.Thrust += inputStrength * dt * throttleSpeed;
            glider.Thrust = Mathf.Clamp(glider.Thrust, minThrust, maxThrust);

            glider.Thrust01 = glider.Thrust / maxThrust;

        }

        private float CalculateDrag(Vector3 velocity, float coAdditive = 0)
        {
            float v = velocity.magnitude;

            if (_a < 0)
            {
                float d = - (_b / _a) * 0.5f;
                v = Mathf.Clamp(v, 0, d);
            }
            
            float thrust = _a * v * v + _b * v + _c;
            
            thrust = (1 + coAdditive) * v * (_a * v + _b) + _c;
            return -thrust;
        }


        private float GetThrustFromWind(Vector3 heading, Vector3 wind)
        {
            if (wind.magnitude < 0.1f)
                return 0;
            
            float alignment = Vector3.Dot(heading.normalized, wind);
            return Mathf.Max(alignment, -minThrust * 0.5f);
        }

        
        [ContextMenu("Calculate Coefficients")]
        private void CalculateCoefficients()
        {
            Vector2 p0, p1, p2;
            p0 = new(minSpeed, minThrust);
            p1 = new(cruiseSpeed, idleThrust);
            p2 = new(maxSpeed, maxThrust);
            CalculateQuadraticCoefficients(p0, p1, p2, out _a, out _b, out _c);

            Keyframe[] keyframes = new Keyframe[32];

            for (var index = 0; index < keyframes.Length; index++)
            {
                float t = index / (keyframes.Length - 1f);
                var keyframe = keyframes[index];
                float x = t * maxSpeed;
                keyframe.time = x;
                keyframe.value = _a * x * x + _b * x + _c;
                keyframe.weightedMode = WeightedMode.None;
                keyframes[index] = keyframe;
            }

            debugCurve = new AnimationCurve(keyframes);
        }

        private void CalculateQuadraticCoefficients(Vector2 p0, Vector2 p1, Vector2 p2, out float a, out float b,
            out float c)
        {
            float3x3 matrix = new(
                new float3(p0.x * p0.x, p0.x, 1),
                new float3(p1.x * p1.x, p1.x, 1),
                new float3(p2.x * p2.x, p2.x, 1));

            matrix = math.transpose(matrix);
            
            matrix = math.inverse(matrix);
            float3 result = math.mul(matrix, new float3(p0.y, p1.y, p2.y));

            a = result.x;
            b = result.y;
            c = result.z;
        }
    }
}
