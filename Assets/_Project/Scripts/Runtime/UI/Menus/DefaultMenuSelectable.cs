using Beakstorm.Inputs;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Beakstorm.UI.Menus
{
    public class DefaultMenuSelectable : MonoBehaviour
    {
        [SerializeField] private Selectable selectable;

        private bool _usesController = false;
        
        private void OnEnable()
        {
            SelectDefault();
            
            PlayerInputs.ActiveDeviceChangeEvent += OnActiveDeviceChanged;
        }

        private void OnDisable()
        {
            PlayerInputs.ActiveDeviceChangeEvent -= OnActiveDeviceChanged;
        }


        private void OnActiveDeviceChanged()
        {
            UsesController(PlayerInputs.Instance.UseButtonsInMenu);
        }


        private void Reset()
        {
            if (!selectable)
                selectable = GetComponent<Selectable>();
        }

        private void UsesController(bool value)
        {
            if (_usesController == value)
                return;

            _usesController = value;
            
            if (EventSystem.current)
            {
                if (EventSystem.current.currentSelectedGameObject)
                {
                    if (EventSystem.current.currentSelectedGameObject.TryGetComponent(out TMP_InputField field))
                        return;
                }
            }
            
            if (_usesController)
            {
                SelectDefault();
            }
            else
            {
                if (EventSystem.current)
                    EventSystem.current.SetSelectedGameObject(null);
            }
        }

        private void SelectDefault()
        {
            if (selectable)
                selectable.Select();
        }
    }
}
