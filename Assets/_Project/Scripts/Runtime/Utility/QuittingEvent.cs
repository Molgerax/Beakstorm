using AptabaseSDK;
using UltEvents;
using UnityEngine;

namespace Beakstorm.Utility
{
    public class QuittingEvent : MonoBehaviour
    {
        [SerializeField] private UltEvent onQuitting;

        private bool _hasQuit = false;
        
        private void OnEnable()
        {
            Application.quitting += OnQuitting;
        }

        private void OnDisable()
        {
            Application.quitting -= OnQuitting;
        }

        private void OnApplicationQuit()
        {
            if (_hasQuit)
                return;

            _hasQuit = true;
            onQuitting?.Invoke();
            Aptabase.Flush();
        }

        private void OnQuitting()
        {
            if (_hasQuit)
                return;

            _hasQuit = true;
            onQuitting?.Invoke();
            Aptabase.Flush();
        }
    }
}
