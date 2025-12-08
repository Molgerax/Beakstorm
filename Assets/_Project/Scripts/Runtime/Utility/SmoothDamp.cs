using UnityEngine;

namespace Beakstorm.Utility
{
    public static class SmoothDamp
    {
        public static Quaternion Rotate(Quaternion a, Quaternion b, float lambda, float dt)
        {
            return Quaternion.Slerp(a, b, 1 - Mathf.Exp(- lambda * dt));
        }
        
        
        public static Vector4 Move(Vector4 a, Vector4 b, float lambda, float dt)
        {
            return Vector4.Lerp(a, b, 1 - Mathf.Exp(- lambda * dt));
        }
        
        
        public static Vector3 Move(Vector3 a, Vector3 b, float lambda, float dt)
        {
            return Vector3.Lerp(a, b, 1 - Mathf.Exp(- lambda * dt));
        }
        
        public static Vector3 Slerp(Vector3 a, Vector3 b, float lambda, float dt)
        {
            return Vector3.Slerp(a, b, 1 - Mathf.Exp(- lambda * dt));
        }
        
        public static Vector2 Move(Vector2 a, Vector2 b, float lambda, float dt)
        {
            return Vector2.Lerp(a, b, 1 - Mathf.Exp(- lambda * dt));
        }
        
        public static float Move(float a, float b, float lambda, float dt)
        {
            return Mathf.Lerp(a, b, 1 - Mathf.Exp(- lambda * dt));
        }
        
        public static float MoveAngle(float a, float b, float lambda, float dt)
        {
            float delta = Mathf.Repeat((b - a), 360.0F);
            if (delta > 180.0F)
                delta -= 360.0F;
            b = a + delta;
            return Mathf.Lerp(a, b, 1 - Mathf.Exp(- lambda * dt));
        }
    }
}