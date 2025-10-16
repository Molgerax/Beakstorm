using UnityEngine;

namespace Beakstorm.Utility
{
    public static class BezierMath
    {
        public static Vector3 BezierPos(Vector3 a, Vector3 b, Vector3 c, Vector3 d, float t)
        {
            float tMinus = 1 - t;
            return
                (tMinus * tMinus * tMinus) * a +
                (3 * tMinus * tMinus * t) * b +
                (3 * tMinus * t * t) * c +
                (t * t * t) * d;
        }

        public static Vector3 BezierDerivative(Vector3 a, Vector3 b, Vector3 c, Vector3 d, float t)
        {
            float tMinus = 1 - t;
            return
                (3 * tMinus * tMinus) * (b - a) +
                (6 * tMinus * t) * (c - b) +
                (3 * t * t) * (d - c);
        }
    }
}