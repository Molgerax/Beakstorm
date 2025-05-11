using UltEvents;
using UnityEngine;

namespace Beakstorm.Pausing
{
    public class PauseCallbacks : MonoBehaviour
    {
        [SerializeField] private UltEvent onPause;
        [SerializeField] private UltEvent onUnpause;

        private void OnEnable()
        {
            PauseManager.OnPauseAction += OnPause;
            PauseManager.OnUnpauseAction += OnUnpause;
        }
        
        private void OnDisable()
        {
            PauseManager.OnPauseAction -= OnPause;
            PauseManager.OnUnpauseAction -= OnUnpause;
        }

        private void OnPause() => onPause?.Invoke();
        private void OnUnpause() => onUnpause?.Invoke();
    }
}
