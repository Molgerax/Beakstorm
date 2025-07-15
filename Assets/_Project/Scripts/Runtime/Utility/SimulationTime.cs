using UnityEngine;

namespace Beakstorm.Utility
{
    public static class SimulationTime
    {
        public static float MaxDeltaTime = 0.02f;
        public static float MinDeltaTime = 0.001f;
        public static float DeltaTime => Mathf.Clamp(Time.deltaTime, MinDeltaTime, MaxDeltaTime);
    }
}