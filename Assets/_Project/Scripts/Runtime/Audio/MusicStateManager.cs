using UnityEngine;

namespace Beakstorm.Audio
{
    public class MusicStateManager : MonoBehaviour
    {
        public static MusicStateManager Instance;
        
        [Header("Peace")]
        [SerializeField] private AK.Wwise.State peaceState;
        [SerializeField] private AK.Wwise.State[] peaceStates;
        
        [Header("War")]
        [SerializeField] private AK.Wwise.State warState;
        [SerializeField] private AK.Wwise.State[] warStates;

        private int _defaultPeaceIndex = 0;

        private bool _war;
        
        private void Awake()
        {
            Instance = this;
            
            SetPeace(0);
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        public void SetWar(int intensity)
        {
            warState.SetValue();

            _war = true;

            int index = Mathf.Clamp(intensity, 0, warStates.Length - 1);
            warStates[index].SetValue();
        }

        public void SetPeace() => SetPeace(_defaultPeaceIndex);
        
        public void SetPeace(int intensity)
        {
            peaceState.SetValue();

            _war = false;
            
            int index = Mathf.Clamp(intensity, 0, warStates.Length - 1);
            peaceStates[index].SetValue();
        }

        public void SetDefaultPeace(int intensity)
        {
            _defaultPeaceIndex = intensity;
            
            if (!_war)
                SetPeace();
        }
    }
}
