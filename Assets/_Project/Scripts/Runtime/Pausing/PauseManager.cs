using Beakstorm.Inputs;
using UnityEngine;

namespace Beakstorm.Pausing
{
    public class PauseManager : MonoBehaviour
    {
        public static PauseManager Instance;

        public static bool IsPaused { get; private set; }
        
        private void Awake()
        {
            Instance = this;
        }

        private void OnEnable()
        {
            PlayerInputs.Instance.PauseAction += OnPause;
        }

        private void OnPause()
        {
            if (IsPaused)
                Unpause();
            else
                Pause();
        }
        
        
        public void Pause()
        {
            PlayerInputs.Instance.EnableUiInputs();
            Time.timeScale = 0;
            IsPaused = true;
        }

        public void Unpause()
        {
            PlayerInputs.Instance.EnablePlayerInputs();
            Time.timeScale = 1f;
            IsPaused = false;
        }
    }
}
