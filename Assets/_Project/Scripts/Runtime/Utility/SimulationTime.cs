using UnityEngine;

namespace Beakstorm.Utility
{
    public static class SimulationTime
    {
        public static float MaxDeltaTime = 0.02f;
        public static float DeltaTime => Mathf.Min(Time.deltaTime, MaxDeltaTime);
    }
}