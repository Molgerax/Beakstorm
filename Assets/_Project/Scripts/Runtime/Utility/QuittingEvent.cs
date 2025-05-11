using UltEvents;
using UnityEngine;

namespace Beakstorm.Utility
{
    public class QuittingEvent : MonoBehaviour
    {
        [SerializeField] private UltEvent onQuitting;
        private void OnEnable()
        {
            Application.quitting += OnQuitting;
        }

        private void OnDisable()
        {
            Application.quitting -= OnQuitting;
        }

        private void OnQuitting()
        {
            onQuitting?.Invoke();
        }
    }
}
