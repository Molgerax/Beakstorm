using UnityEngine;

namespace Beakstorm.Mapping.Tremble.Properties
{
    [System.Serializable]
    public struct QuakeFlags
    {
        public ulong Value;

        public QuakeFlags(ulong value)
        {
            Value = value;
        }

        public static implicit operator ulong(QuakeFlags q) => q.Value;
        public static implicit operator QuakeFlags(ulong i) => new(i);


        public bool GetFlag(int bit)
        {
            return (Value & (1u << bit)) > 0;
        }

        public void SetFlag(int bit, bool value)
        {
            if (value)
                SetFlag(bit);
            else
                UnsetFlag(bit);
        }

        public void SetFlag(int bit)
        {
            Value = Value | (1u << bit);
        }

        public void UnsetFlag(int bit)
        {
            Value = Value & (~(1u << bit));
        }
    }
}