using UnityEngine;

namespace Beakstorm.Mapping.Tremble.Properties
{
    [System.Serializable]
    public struct QuakeAngle
    {
        public int Value;

        public QuakeAngle(int value)
        {
            Value = value;
        }

        public static implicit operator int(QuakeAngle quakeAngle) => quakeAngle.Value;
        public static implicit operator QuakeAngle(int f) => new (f);
        
        public static implicit operator Vector3(QuakeAngle quakeAngle) => quakeAngle.ToDirection;
        
        public Vector3 ToDirection
        {
            get
            {
                if (Value == -1)
                    return Vector3.up;
                if (Value == -2)
                    return Vector3.down;
                return Quaternion.Euler(Vector3.up * Value) * Vector3.right;
            }
        }
    }
}