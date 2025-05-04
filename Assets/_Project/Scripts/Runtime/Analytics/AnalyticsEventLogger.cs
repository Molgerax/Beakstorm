using System.Collections.Generic;
using AptabaseSDK;
using UnityEngine;

namespace Beakstorm.Analytics
{
    public class AnalyticsEventLogger : MonoBehaviour
    {
        public void TrackEvent(string eventName, string key, string value)
        {
            Aptabase.TrackEvent(eventName, new Dictionary<string, object>()
            {
                {key, value}
            });
        }
    }
}
