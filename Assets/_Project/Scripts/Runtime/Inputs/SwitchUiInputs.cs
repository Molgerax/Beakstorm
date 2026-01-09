using UnityEngine;

namespace Beakstorm.Inputs
{
    public class SwitchUiInputs : MonoBehaviour
    {
        private static int _currentInstances = 0;


        private int CurrentInstances
        {
            get => _currentInstances;
            set
            {
                if (_currentInstances == 0 && value > 0)
                {
                    PlayerInputs.Instance.EnableUiInputs();
                }
                else if (_currentInstances > 0 && value == 0)
                {
                    PlayerInputs.Instance.EnablePlayerInputs();
                }
                
                _currentInstances = value;
            }
        }

        private void OnEnable() => CurrentInstances++;

        private void OnDisable() => CurrentInstances--;
    }
}
