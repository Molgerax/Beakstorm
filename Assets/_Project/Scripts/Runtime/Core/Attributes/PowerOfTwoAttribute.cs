using UnityEngine;

namespace Beakstorm.Core.Attributes
{
    public class PowerOfTwoAttribute : PropertyAttribute
    {
        public int Min { get; private set; } = 0;
        public int Max { get; private set; } = 0;

        public int MinExp => Mathf.RoundToInt(Mathf.Log(Min, 2));
        public int MaxExp => Mathf.RoundToInt(Mathf.Log(Max, 2));
        
        public PowerOfTwoAttribute(int min, int max)
        {
            Min = Mathf.NextPowerOfTwo(Mathf.Min(min, max));
            Max = Mathf.NextPowerOfTwo(Mathf.Max(min, max));
        }
    }
}