using UnityEngine;

namespace Beakstorm.Gameplay.Player.Flying
{
    [CreateAssetMenu(menuName = "Beakstorm/Player/FlightControlStrategy/Realistic")]
    public class RealisticFlightControlStrategy : FlightControlStrategy
    {
        [SerializeField] private float maxThrust = 10f;
        [SerializeField] private Vector3 turnAcceleration;
        [SerializeField] private Vector3 turnSpeed;


        
        [SerializeField] private float gLimit = 8;
        
        [SerializeField] private float liftPower = 1f;
        [SerializeField] private AnimationCurve liftAoaCurve = AnimationCurve.EaseInOut(-90, 0, 90, 1);

        [SerializeField] private float inducedDrag = 1f;
        [SerializeField] private AnimationCurve inducedDragCurve = AnimationCurve.EaseInOut(0, 0, 100, 1);

        [SerializeField] private Vector3 angularDrag;
        
        [SerializeField] private AnimationCurve steeringCurve = AnimationCurve.EaseInOut(0, 0, 100, 1);

        [Header("Drag")] 
        [SerializeField] private AnimationCurve dragForward = AnimationCurve.EaseInOut(0, 0, 100, 1);
        [SerializeField] private AnimationCurve dragUp = AnimationCurve.EaseInOut(0, 0, 100, 1);
        [SerializeField] private AnimationCurve dragRight = AnimationCurve.EaseInOut(0, 0, 100, 1);

        public override float Speed01(float speed) => speed;

        public override void Initialize(GliderController glider, float dt)
        {
            glider.Rigidbody.isKinematic = false;
            glider.Rigidbody.useGravity = true;
        }

        public override void FixedUpdateFlight(GliderController glider, float dt)
        {
            Vector3 velocity = glider.Rigidbody.linearVelocity;
            Quaternion invRotation = Quaternion.Inverse(glider.T.rotation);
            Vector3 localVelocity = invRotation * velocity;
            Vector3 localAngularVelocity = invRotation * glider.Rigidbody.angularVelocity;

            GetAngleOfAttack(localVelocity, out var angleOfAttack, out var angleOfAttackYaw);
            
            ApplyThrust(glider);
            ApplyLift(glider, localVelocity, angleOfAttack, angleOfAttackYaw);
            Vector3 input = UpdateSteering(glider, localVelocity, localAngularVelocity, dt);
            
            
            ApplyDrag(glider, localVelocity);
            ApplyAngularDrag(glider, localAngularVelocity);
            
            Vector3 up = glider.Rigidbody.rotation * Vector3.up;
            Vector3 forward = glider.Rigidbody.linearVelocity.normalized;
            glider.Rigidbody.rotation = Quaternion.LookRotation(forward, up);
        }
        
        
        private void ApplyThrust(GliderController glider)
        {
            float throttle = glider.ThrustInput ? 1 : 0;
            glider.Rigidbody.AddRelativeForce(Vector3.forward * (maxThrust * throttle));
        }

        private void ApplyDrag(GliderController glider, Vector3 localVelocity)
        {
            Vector3 lv = localVelocity;
            float lv2 = lv.sqrMagnitude;
            
            Vector3 coefficient = Vector3.Scale(lv.normalized, new Vector3(
                dragRight.Evaluate(Mathf.Abs(lv.x)),
                dragUp.Evaluate(Mathf.Abs(lv.y)),
                dragForward.Evaluate(Mathf.Abs(lv.z))));
            
            
            Vector3 drag = coefficient.magnitude * lv2 * -lv.normalized;
            
            glider.Rigidbody.AddRelativeForce(drag);
        }
        
        void ApplyAngularDrag(GliderController glider, Vector3 localAngularVelocity) {
            var av = localAngularVelocity;
            var drag = av.sqrMagnitude * -av.normalized;    //squared, opposite direction of angular velocity
            glider.Rigidbody.AddRelativeTorque(Vector3.Scale(drag, angularDrag), ForceMode.Acceleration);  //ignore rigidbody mass
        }

        private void GetAngleOfAttack(Vector3 localVelocity, out float aoa, out float aoaYaw)
        {
            aoa = 0;
            aoaYaw = 0;
            if (localVelocity.sqrMagnitude < 0.1f)
                return;
            
            aoa = Mathf.Atan2(-localVelocity.y, localVelocity.z);
            aoaYaw = Mathf.Atan2(localVelocity.x, localVelocity.z);
        }

        private void ApplyLift(GliderController glider, Vector3 localVelocity, float aoa, float aoaYaw)
        {
            if (localVelocity.sqrMagnitude < 1f) return;

            Vector3 liftForce = CalculateLift(localVelocity,
                aoa, Vector3.right,
                liftPower, liftAoaCurve, inducedDragCurve);
            
            
            glider.Rigidbody.AddRelativeForce(liftForce);
        }
        
        private Vector3 CalculateLift(Vector3 localVelocity, float aoa, Vector3 rightAxis, float liftPower, AnimationCurve aoaCurve, AnimationCurve inducedDragCurve)
        {
            Vector3 liftVelocity = Vector3.ProjectOnPlane(localVelocity, rightAxis);
            float v2 = liftVelocity.sqrMagnitude;

            //lift = velocity^2 * coefficient * liftPower
            //coefficient varies with AOA
            float liftCoefficient = aoaCurve.Evaluate(aoa * Mathf.Rad2Deg);
            float liftForce = v2 * liftCoefficient * liftPower;
            
            //lift is perpendicular to velocity
            Vector3 liftDirection = Vector3.Cross(liftVelocity.normalized, rightAxis);
            Vector3 lift = liftDirection * liftForce;

            //induced drag varies with square of lift coefficient
            float dragForce = liftCoefficient * liftCoefficient;
            Vector3 dragDirection = -liftVelocity.normalized;
            Vector3 inducedDragResult = dragDirection * (v2 * dragForce * inducedDrag * inducedDragCurve.Evaluate(Mathf.Max(0, localVelocity.z)));

            return lift + inducedDragResult;
        }

        Vector3 CalculateGForce(Vector3 angularVelocity, Vector3 velocity) {
            //estimate G Force from angular velocity and velocity
            //Velocity = AngularVelocity * Radius
            //G = Velocity^2 / R
            //G = (Velocity * AngularVelocity * Radius) / Radius
            //G = Velocity * AngularVelocity
            //G = V cross A
            return Vector3.Cross(angularVelocity, velocity);
        }
        
        private float CalculateSteering(float dt, float angularVelocity, float targetVelocity, float acceleration) 
        {
            var error = targetVelocity - angularVelocity;
            var accel = acceleration * dt;
            return Mathf.Clamp(error, -accel, accel);
        }

        Vector3 CalculateGForceLimit(Vector3 input) {
            return Vector3.Scale(input,
                Vector3.one * gLimit
                ) * 9.81f;
        }
        
        float CalculateGLimiter(Vector3 localVelocity, Vector3 controlInput, Vector3 maxAngularVelocity) 
        {
            if (controlInput.magnitude < 0.01f) {
                return 1;
            }

            //if the player gives input with magnitude less than 1, scale up their input so that magnitude == 1
            var maxInput = controlInput.normalized;

            var limit = CalculateGForceLimit(maxInput);
            Vector3 maxGForce = CalculateGForce(Vector3.Scale(maxInput, maxAngularVelocity), localVelocity);

            if (maxGForce.magnitude > limit.magnitude) {
                //example:
                //maxGForce = 16G, limit = 8G
                //so this is 8 / 16 or 0.5
                return limit.magnitude / maxGForce.magnitude;
            }

            return 1;
        }
        
        private Vector3 UpdateSteering(GliderController glider, Vector3 localVelocity, Vector3 localAngularVelocity, float dt) 
        {
            var speed = Mathf.Max(0, localVelocity.z);
            var steeringPower = steeringCurve.Evaluate(speed);

            var controlInput = new Vector3(glider.MoveInput.x, glider.MoveInput.y, 0);
            
            var gForceScaling = CalculateGLimiter(localVelocity, controlInput, turnSpeed * Mathf.Deg2Rad * steeringPower);

            var targetAV = Vector3.Scale(controlInput, turnSpeed * steeringPower * gForceScaling);
            var av = localAngularVelocity * Mathf.Rad2Deg;

            var correction = new Vector3(
                CalculateSteering(dt, av.x, targetAV.x, turnAcceleration.x * steeringPower),
                CalculateSteering(dt, av.y, targetAV.y, turnAcceleration.y * steeringPower),
                CalculateSteering(dt, av.z, targetAV.z, turnAcceleration.z * steeringPower)
            );

            glider.Rigidbody.AddRelativeTorque(correction * Mathf.Deg2Rad, ForceMode.VelocityChange);    //ignore rigidbody mass

            var correctionInput = new Vector3(
                Mathf.Clamp((targetAV.x - av.x) / turnAcceleration.x, -1, 1),
                Mathf.Clamp((targetAV.y - av.y) / turnAcceleration.y, -1, 1),
                Mathf.Clamp((targetAV.z - av.z) / turnAcceleration.z, -1, 1)
            );

            var effectiveInput = (correctionInput + controlInput) * gForceScaling;

            Vector3 EffectiveInput = new Vector3(
                Mathf.Clamp(effectiveInput.x, -1, 1),
                Mathf.Clamp(effectiveInput.y, -1, 1),
                Mathf.Clamp(effectiveInput.z, -1, 1)
            );

            return EffectiveInput;
        }
    }
}
