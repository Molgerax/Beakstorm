using Unity.Mathematics;
using UnityEngine;

namespace Beakstorm.Gameplay.Projectiles
{
    public static class ProjectileMath
    {
        public static bool PredictLinearPath(Vector3 startPos, float startSpeed, Vector3 targetPos, Vector3 targetVel, out Vector3 startDirection)
        {
            startDirection = (targetPos - startPos).normalized;
            
            float a = Vector3.Dot(targetVel, targetVel) - startSpeed * startSpeed;
            float b = 2 * Vector3.Dot(targetPos, targetVel);
            float c = Vector3.Dot(targetPos, targetPos);

            if (a == 0)
                return false;
            
            float squaredTerm = b * b - 4 * a * c;

            if (squaredTerm < 0)
                return false;
            
            float travelTimeA = (-b + Mathf.Sqrt(squaredTerm)) / (2 * a);
            float travelTimeB = (-b + Mathf.Sqrt(squaredTerm)) / (2 * a);

            float travelTime = Mathf.Max(travelTimeA, travelTimeB);
            
            startDirection = Divide((targetPos + targetVel * travelTime), startPos * travelTime);
            return true;
        }


        public static Vector3 Divide(Vector3 lhs, Vector3 rhs) => (float3) lhs / rhs;
    }
}
