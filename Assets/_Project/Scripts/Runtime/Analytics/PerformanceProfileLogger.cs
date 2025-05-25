using System.Collections.Generic;
using AptabaseSDK;
using UnityEngine;

namespace Beakstorm.Analytics
{
    public class PerformanceProfileLogger : MonoBehaviour
    {
        [SerializeField, Min(30)] private float profilingInterval = 60;
        [SerializeField, Range(0.1f, 2f)] private float averageDuration = 1f;

        private float _duration = 0;
        private int _frameCount = 0;

        private int _belowCount;

        private float _averageDuration = 0;
        private int _averageFrameCount = 0;

        private float _averageFrameTime;
        private float _highestFrameTime;
        private float _lowestFrameTime;

        private float _highestAverageFrameTime;

        private void Start()
        {
            ResetValues();
            
            Aptabase.TrackEvent("hardware_analysis", new Dictionary<string, object>()
            {
                {"graphicsDevice", SystemInfo.graphicsDeviceName},
                {"graphicsDeviceVendor", SystemInfo.graphicsDeviceVendor},
                {"graphicsMemorySize", $"{SystemInfo.graphicsMemorySize}MB"},
                {"graphicsBufferMaxSize", GetBytes(SystemInfo.maxGraphicsBufferSize)},
            });
        }

        private void Update()
        {
            Tick(Time.unscaledDeltaTime);
        }

        private void Tick(float deltaTime)
        {
            _duration += deltaTime;
            _averageDuration += deltaTime;
            _frameCount++;
            _averageFrameCount++;
            
            float averagedFrameTime = _averageDuration / _averageFrameCount;
            _highestAverageFrameTime = Mathf.Max(_highestAverageFrameTime, averagedFrameTime);

            if (1 / deltaTime < 58f)
                _belowCount++;
            
            if (_averageDuration >= averageDuration)
            {
                _averageDuration = 0;
                _averageFrameCount = 0;
            }
            
            _averageFrameTime = _duration / _frameCount;
            _highestFrameTime = Mathf.Max(_highestFrameTime, deltaTime);
            _lowestFrameTime = Mathf.Min(_lowestFrameTime, deltaTime);

            if (_duration >= profilingInterval)
            {
                SendAnalyticsEvent();
                ResetValues();
            }
        }

        private void SendAnalyticsEvent()
        {
            Aptabase.TrackEvent("performance_profile", new Dictionary<string, object>()
            {
                //{"timeElapsed", _duration},
                {"averageFps", (1 / _averageFrameTime)},
                {"lowestFps", 1 / _highestFrameTime},
                {"lowestAverageFps", 1 / _highestAverageFrameTime},
                {"highestFps", 1 / _lowestFrameTime},                
                {"framesBelowTarget", (float)_belowCount / _frameCount},
            });
        }
        
        private void ResetValues()
        {
            _duration = 0;
            _frameCount = 0;
            _belowCount = 0;
            _averageFrameTime = 0;
            _highestFrameTime = 0;
            _highestAverageFrameTime = 0;
            _lowestFrameTime = float.MaxValue;
        }


        private string GetBytes(long bytes)
        {
            return $"{bytes}";
        }
    }
}
