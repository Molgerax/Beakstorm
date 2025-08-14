using Beakstorm.Inputs;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Beakstorm.UI.Menus
{
    public class BackButton : MonoBehaviour
    {
        [SerializeField] private Button button;

        private void Reset()
        {
            if (!button)
                button = GetComponentInChildren<Button>();
        }

        private void OnEnable() => PlayerInputs.Instance.cancelAction.performed += OnCancel;
        private void OnDisable() => PlayerInputs.Instance.cancelAction.performed -= OnCancel;

        private void OnCancel(InputAction.CallbackContext obj)
        {
            if (button)
                button.OnPointerClick(new PointerEventData(EventSystem.current));
        }
    }
}