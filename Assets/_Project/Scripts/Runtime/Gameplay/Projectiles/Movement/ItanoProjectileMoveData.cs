using Beakstorm.Gameplay.Player;
using Beakstorm.Utility;
using Unity.Mathematics;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Beakstorm.Gameplay.Projectiles.Movement
{
    [CreateAssetMenu(fileName = "ItanoProjectileMoveData", menuName = "Beakstorm/Projectiles/ItanoProjectileMoveData")]
    public class ItanoProjectileMoveData : AbstractProjectileMoveData
    {
        [SerializeField, Min(0)] private float noiseFrequency = 2f;
        [SerializeField, Min(0)] private float noiseAmplitude = 8f;

        [SerializeField, Min(0)] private float controlPointDistA = 10f;
        [SerializeField, Min(0)] private float controlPointDistB = 5f;

        public override void Initialize(ProjectileMovementHandler movementHandler, FireInfo fireInfo)
        {
            movementHandler.SetVelocity(fireInfo.InitialDirection * fireInfo.Speed);
            movementHandler.Speed = fireInfo.Speed;

            Transform transform = movementHandler.transform;
            transform.rotation = Quaternion.LookRotation(fireInfo.InitialDirection);
            transform.position = fireInfo.InitialPosition;
            
            float distance = Vector3.Distance(fireInfo.InitialPosition, fireInfo.TargetPosition);
            float time = distance / fireInfo.Speed;

            movementHandler.ElapsedTime = 0;
            movementHandler.TargetTime = time;
            movementHandler.Random1 = Random.value;
            movementHandler.Random2 = Random.value;
        }

        public override void Tick(ProjectileMovementHandler movementHandler, float deltaTime)
        {
            movementHandler.ElapsedTime += deltaTime;
            
            var transform = movementHandler.transform;
            Vector3 position = transform.position;

            float t = Mathf.Clamp01(movementHandler.ElapsedTime / movementHandler.TargetTime);
            
            Vector3 localOffset = Vector3.zero;

            float envelope = 1 - (1 - 2 * t) * (1 - 2 * t);

            var seedX = movementHandler.Random1;
            var seedY = movementHandler.Random2;

            localOffset.x = noiseAmplitude * envelope *
                            noise.snoise(new float2(seedX, noiseFrequency * movementHandler.ElapsedTime));
            localOffset.y = noiseAmplitude * envelope *
                            noise.snoise(new float2(seedY, noiseFrequency * movementHandler.ElapsedTime));

            UpdateRotationFrame(movementHandler, position, t);
            
            GetBezierPoints(movementHandler, out var a, out var b, out var c, out var d);
            position = BezierMath.BezierPos(a, b, c, d, t) + movementHandler.RotationFrame * localOffset;
            transform.position = position;
        }

        private void UpdateRotationFrame(ProjectileMovementHandler mh, Vector3 position, float t)
        {
            // starting normal and tangent
            Vector3 n0 = mh.RotationFrame * Vector3.up;
            Vector3 t0 = mh.RotationFrame * Vector3.forward;

            GetBezierPoints(mh, out var a, out var b, out var c, out var d);

            // target tangent
            Vector3 t1 = BezierMath.BezierDerivative(a, b, c, d, t).normalized;
            
            // first reflection
            Vector3 v1 = BezierMath.BezierPos(a, b, c, d, t) - position;
            float c1 = v1.sqrMagnitude;
            Vector3 n0_1 = n0 - (2 / c1) * Vector3.Dot(v1, n0) * v1;
            Vector3 t0_1 = t0 - (2 / c1) * Vector3.Dot(v1, n0) * v1;
            
            // second reflection
            Vector3 v2 = t1 - t0_1;
            float c2 = v2.sqrMagnitude;
            Vector3 n1 = n0_1 - (2 / c2) * Vector3.Dot(v2, n0_1) * v2;
            
            // build rotation with target normal for up
            mh.RotationFrame = Quaternion.LookRotation(t1, n1);
        }

        private void GetBezierPoints(ProjectileMovementHandler mh, out Vector3 a, out Vector3 b, out Vector3 c,
            out Vector3 d)
        {
            a = mh.FireInfo.InitialPosition;
            b = mh.FireInfo.InitialPosition + mh.FireInfo.InitialDirection * controlPointDistA;
            c = mh.FireInfo.TargetPosition + mh.FireInfo.TargetNormal * controlPointDistB;
            d = mh.FireInfo.TargetPosition;
        }
    }
}
