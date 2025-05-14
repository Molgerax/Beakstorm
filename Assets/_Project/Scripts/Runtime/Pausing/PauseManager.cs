using System;
using Beakstorm.Inputs;
using UnityEngine;

namespace Beakstorm.Pausing
{
    public class PauseManager : MonoBehaviour
    {
        public static PauseManager Instance;

        public static event Action OnPauseAction = delegate { };
        public static event Action OnUnpauseAction = delegate { };

        public static bool IsPaused { get; private set; }
        
        private void Awake()
        {
            Instance = this;
        }

        private void OnEnable()
        {
            PlayerInputs.Instance.PauseAction += OnPause;
            Unpause();
        }
        
        private void OnDisable()
        {
            PlayerInputs.Instance.PauseAction -= OnPause;
        }

        private void OnPause()
        {
            if (IsPaused)
                Unpause();
            else
                Pause();
        }


        private void Pause()
        {
            Time.timeScale = 0;
            IsPaused = true;
            
            OnPauseAction?.Invoke();
        }

        private void Unpause()
        {
            Time.timeScale = 1f;
            IsPaused = false;
            
            OnUnpauseAction?.Invoke();
        }
    }
}
