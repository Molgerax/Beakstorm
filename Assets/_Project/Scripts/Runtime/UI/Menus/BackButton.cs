using Beakstorm.Inputs;
using UnityEngine;
using UnityEngine.EventSystems;
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

        private void OnEnable() => PlayerInputs.Instance.Cancel += OnCancel;
        private void OnDisable() => PlayerInputs.Instance.Cancel -= OnCancel;

        private void OnCancel(bool performed)
        {
            if (!performed)
                return;

            if (button)
            {
                button.OnPointerClick(new PointerEventData(EventSystem.current));
            }
        }
    }
}