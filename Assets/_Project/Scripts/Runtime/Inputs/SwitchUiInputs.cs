using UnityEngine;

namespace Beakstorm.Inputs
{
    public class SwitchUiInputs : MonoBehaviour
    {
        private void OnEnable() => PlayerInputs.Instance.EnableUiInputs();

        private void OnDisable() => PlayerInputs.Instance.EnablePlayerInputs();

        private void Update()
        {
            PlayerInputs.Instance.EnableUiInputs();
        }
    }
}
