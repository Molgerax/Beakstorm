using UnityEngine;

namespace Beakstorm.Utility.Extensions
{
    public static class Vector3Extensions
    {
        #region With

        public static Vector4 With(this Vector4 vector, float? x = null, float? y = null, float? z = null, float? w = null)
        {
            return new (x ?? vector.x, y ?? vector.y, z ?? vector.z, w ?? vector.w);
        }
        
        public static Vector3 With(this Vector3 vector, float? x = null, float? y = null, float? z = null)
        {
            return new (x ?? vector.x, y ?? vector.y, z ?? vector.z);
        }
        
        public static Vector2 With(this Vector2 vector, float? x = null, float? y = null)
        {
            return new (x ?? vector.x, y ?? vector.y);
        }
     
        #endregion


        #region Add

        public static Vector4 Add(this Vector4 vector, float x = 0, float y = 0, float z = 0, float w = 0)
        {
            return new (vector.x + x, vector.y + y, vector.z + z, vector.w + w);
        }
        
        public static Vector3 Add(this Vector3 vector, float x = 0, float y = 0, float z = 0)
        {
            return new (vector.x + x, vector.y + y, vector.z + z);
        }
        
        public static Vector2 Add(this Vector2 vector, float x = 0, float y = 0)
        {
            return new (vector.x + x, vector.y + y);
        }
        
        #endregion


        #region Slerp

        public static Vector3 SlerpOffset(this Vector3 a, Vector3 b, float t, Vector3 center)
        {
            return Vector3.Slerp(a - center, b - center, t) + center;
        }
        
        #endregion
    }
}
