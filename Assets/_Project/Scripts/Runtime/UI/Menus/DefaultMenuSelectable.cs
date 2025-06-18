using Beakstorm.Inputs;
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
            UsesController(CheckForPointer());
        }

        private void Reset()
        {
            if (!selectable)
                selectable = GetComponent<Selectable>();
        }

        private void Update()
        {
            UsesController(CheckForPointer());
        }

        private bool CheckForPointer()
        {
            return PlayerInputs.Instance.UseButtonsInMenu;
        }
        
        private void UsesController(bool value)
        {
            if (_usesController == value)
                return;

            _usesController = value;
            
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
