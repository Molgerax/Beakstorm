using Beakstorm.Core.Variables;
using Beakstorm.Utility;
using Beakstorm.Utility.Extensions;
using Unity.Mathematics;
using UnityEngine;

namespace Beakstorm.Gameplay.Player.Flying
{
    [CreateAssetMenu(menuName = "Beakstorm/Player/FlightControlStrategy/SpeedBasedNewRoll")]
    public class SpeedBasedNewRollFlightControlStrategy : SpeedBasedFlightControlStrategy
    {
        protected override void UpdateSteering(GliderController glider, float dt)
        {
            Vector2 inputVector = glider.MoveInput;

            Vector3 forwards = glider.T.forward;
            Vector3 ups = glider.T.up;

            Vector3 localEulerAngles = glider.T.localEulerAngles;
            localEulerAngles = glider.EulerAngles;

            Vector3 ogAngles = localEulerAngles;
            
            float stalling = 1 - Mathf.Clamp01((glider.Speed - minSpeed) / (stallSpeed - minSpeed));

            float pitch = Vector3.SignedAngle(Vector3.up, forwards, glider.T.right);
            
            // Rotate Pitch directly by y-input
            localEulerAngles.x -= inputVector.y * dt * GetSteerSpeed(glider);

            // Stalling to pitch down when too slow
            if (glider.Speed < stallSpeed && pitch > 0 && pitch < 170)
            {
                localEulerAngles.x += dt * stalling * steerSpeed;
            }

            // pitching down when overhead
            if (pitch < 0)
            {
                localEulerAngles.x -= dt * steerSpeed * 0.125f;
            }
            
            // reset rotations
            if (localEulerAngles.x > 180)
                localEulerAngles.x -= 360;

            if (localEulerAngles.x < -180)
                localEulerAngles.x += 360;

            
            //if (pitch < 0 && !CameraController.UseManualCamera)
            //    inputVector.x *= -1;
            
            // yaw accel directly from input
            float yAcceleration = inputVector.x * dt * GetSteerSpeed(glider);

            Vector3 up = Vector3.up;

            // forces to calculate the roll of the glider, visual only
            Vector3 forces = Vector3.zero;
            forces += Vector3.down * gravity;

            Vector3 angularVelocity = Vector3.up * (yAcceleration * Mathf.Deg2Rad) / dt;
            float radius = Mathf.Sqrt(glider.Velocity.sqrMagnitude / angularVelocity.sqrMagnitude);
            Vector3 centripetalForce = Vector3.Cross(angularVelocity,  glider.T.forward * glider.Speed);
            //centripetalForce = (glider.Velocity - glider.OldVelocity) / dt;
            forces += centripetalForce;
            
            float targetRoll = -Vector3.SignedAngle(-up, forces.normalized, glider.Velocity.normalized);

            glider.Roll = Mathf.Lerp(glider.Roll, targetRoll, 1 - Mathf.Exp(-rollSpeed * dt));

            // reset flipping
            //if (_flipping && pitch > 0 && Mathf.Abs(glider.Roll) < 10f)
            //    _flipping = false;
            
            // handle flipping, so glider resets
            //if (pitch < 0 && Mathf.Abs(yAcceleration) > 0.5f && !_flipping && Mathf.Abs(glider.Roll) < 10f)
            //{
            //    float angleDist = 180 - (localEulerAngles.x % 180) * 2;
            //    localEulerAngles.x += angleDist;
            //    localEulerAngles.y += 180;
            //
            //    angleDist = 180 - (glider.Roll % 180) * 2;
            //    glider.Roll += angleDist * Mathf.Sign(yAcceleration);
            //    _flipping = true;
            //}
            

            localEulerAngles.y += yAcceleration;
            localEulerAngles.z = glider.Roll;

            // turn based on wind
            Vector3 wind = glider.ExternalWind;
            Vector3 velocity = glider.Speed * glider.T.forward;
            Vector3 windDirection = (wind + velocity).normalized;

            Vector3 windProjectForward = Vector3.ProjectOnPlane(windDirection, glider.T.right);
            float windX = Vector3.SignedAngle(glider.T.forward, windProjectForward, glider.T.right);
            float windY = Vector3.SignedAngle(glider.T.forward.With(y: 0), windDirection.With(y:0), Vector3.up);

            localEulerAngles.x =
                Mathf.SmoothDampAngle(localEulerAngles.x, localEulerAngles.x + windX, ref _windXVel, 1f);
            localEulerAngles.y =
                Mathf.SmoothDampAngle(localEulerAngles.y, localEulerAngles.y + windY, ref _windYVel, 1f);


            Vector3 angleDiff = localEulerAngles - ogAngles;
            CameraController.Instance.LookAhead.x = Mathf.Sin(angleDiff.y * Mathf.Deg2Rad) * lookAheadDist;
            CameraController.Instance.LookAhead.y = -Mathf.Sin(angleDiff.x * Mathf.Deg2Rad) * lookAheadDist;
            
            //glider.EulerAngles = localEulerAngles;

            Quaternion appliedRotation = Quaternion.AngleAxis(-inputVector.y * GetSteerSpeed(glider) * dt, glider.T.right);
            appliedRotation = Quaternion.AngleAxis(inputVector.x * GetSteerSpeed(glider) * dt, glider.T.up) * appliedRotation;
            
            glider.T.rotation = appliedRotation * glider.T.rotation;

            glider.Roll = 0;
            glider.Model.localRotation = Quaternion.Euler(new Vector3(0, 0, glider.Roll));
            
            Quaternion rightedRotation = Quaternion.LookRotation(glider.T.forward, Vector3.up);
            
            if (inputVector.magnitude < 0.1f)
                glider.T.rotation = SmoothDamp.Rotate(glider.T.rotation, rightedRotation, rollSpeed, dt);
            
            return;
            glider.T.localRotation = Quaternion.Euler(glider.EulerAngles);
            glider.T.localRotation = Quaternion.LookRotation(glider.EulerAngles, Vector3.up);
        }
    }
}
