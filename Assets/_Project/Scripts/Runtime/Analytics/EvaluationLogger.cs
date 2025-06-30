using System;
using System.Collections.Generic;
using AptabaseSDK;
using UnityEngine;

namespace Beakstorm.Analytics
{
    public class EvaluationLogger : MonoBehaviour
    {
        public float handlingPheromone {get; set;} = -1;
        public float handlingAttractor {get; set;} = -1;
        public float aestheticPheromone {get; set;} = -1;
        public float aestheticAttractor {get; set;} = -1;
        public float preference {get; set;} = -1;

        public string handlingPheromoneFeedback {get; set;} = String.Empty;
        public string handlingAttractorFeedback {get; set;} = String.Empty;
        public string aestheticPheromoneFeedback {get; set;} = String.Empty;
        public string aestheticAttractorFeedback {get; set;} = String.Empty;
        public string preferenceFeedback {get; set;} = String.Empty;
        public string otherFeedback {get; set;} = String.Empty;

        public void LogEvaluation()
        {
            if (handlingPheromone < 0)
                handlingPheromone = 3;
            
            if (handlingAttractor < 0)
                handlingAttractor = 3;
            
            if (aestheticPheromone < 0)
                aestheticPheromone = 3;
            
            if (aestheticAttractor < 0)
                aestheticAttractor = 3;
            
            if (preference < 0)
                preference = 3;

            Dictionary<string, object> dictionary = new Dictionary<string, object>()
            {
                {"handlingPheromone", Mathf.RoundToInt(handlingPheromone)},
                {"handlingAttractor", Mathf.RoundToInt(handlingAttractor)},

                {"aestheticPheromone", Mathf.RoundToInt(aestheticPheromone)},
                {"aestheticAttractor", Mathf.RoundToInt(aestheticAttractor)},
                {"preference", Mathf.RoundToInt(preference)},
            };
            Aptabase.TrackEvent("evaluation", dictionary);
            
            Aptabase.TrackEvent("evaluation_feedback", new Dictionary<string, object>()
            {
                {"handlingPheromoneFeedback", handlingPheromoneFeedback},
                {"handlingAttractorFeedback", handlingAttractorFeedback},
                {"aestheticPheromoneFeedback", aestheticPheromoneFeedback},
                {"aestheticAttractorFeedback", aestheticAttractorFeedback},
                {"preferenceFeedback", preferenceFeedback},
                {"otherFeedback", otherFeedback},
            });
            
            Aptabase.Flush();
        }
    }
}
