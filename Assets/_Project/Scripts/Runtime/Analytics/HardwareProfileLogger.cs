using System.Collections.Generic;
using AptabaseSDK;
using UnityEngine;

namespace Beakstorm.Analytics
{
    public class HardwareProfileLogger : MonoBehaviour
    {
        private void Start()
        {
            Aptabase.TrackEvent("hardware_analysis", new Dictionary<string, object>()
            {
                {"graphicsDevice", SystemInfo.graphicsDeviceName},
                {"graphicsDeviceVendor", SystemInfo.graphicsDeviceVendor},
                {"graphicsMemorySize", $"{SystemInfo.graphicsMemorySize}MB"},
                {"graphicsBufferMaxSize", GetBytes(SystemInfo.maxGraphicsBufferSize)},
            });
        }

        private string GetBytes(long bytes)
        {
            return $"{bytes}";
        }
    }
}
