using System.Collections.Generic;
using AptabaseSDK;
using Beakstorm.Simulation;
using UnityEngine;

namespace Beakstorm.Analytics
{
    public class GameOverEventLogger : MonoBehaviour
    {
        public void LogGameOver()
        {
            Aptabase.TrackEvent("game_over", new Dictionary<string, object>()
            {
                {"system", UseAttractorSystem.UseAttractorsString}
            });
            Aptabase.Flush();
        }
    }
}
