using System;
using System.Collections.Generic;
using Beakstorm.Inputs;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Beakstorm.UI.Menus
{
    public class DefaultMenuSelectable : MonoBehaviour
    {
        [SerializeField] private Selectable selectable;

        private static List<DefaultMenuSelectable> _activeSelectables = new();
        private bool _isCurrentDefault;
        
        private static event Action _activateNewSelectable; 
        
        private bool _usesController = false;

        private Selectable _lastSelected;
        
        private void OnEnable()
        {
            PlayerInputs.ActiveDeviceChangeEvent += OnActiveDeviceChanged;
            _activateNewSelectable += OnDefaultSelectablesChanged;

            _activeSelectables.Insert(0, this);
            _activateNewSelectable?.Invoke();
        }

        private void OnDisable()
        {
            _activateNewSelectable -= OnDefaultSelectablesChanged;
            PlayerInputs.ActiveDeviceChangeEvent -= OnActiveDeviceChanged;
            _activeSelectables.Remove(this);
            _activateNewSelectable?.Invoke();
        }

        private void OnActiveDeviceChanged()
        {
            SelectIfControllerChanged(PlayerInputs.Instance.UseButtonsInMenu);
        }

        private void OnDefaultSelectablesChanged()
        {
            if (_activeSelectables[0] == this)
            {
                _isCurrentDefault = true;
                SelectWithDelay(PlayerInputs.Instance.UseButtonsInMenu);
            }
            else if (_isCurrentDefault)
            {
                _isCurrentDefault = false;
            }
        }

        private async void SelectWithDelay(bool useButtonsInMenu)
        {
            await UniTask.WaitForEndOfFrame();
            Select(useButtonsInMenu);
        }

        private void Update()
        {
            if (_isCurrentDefault)
            {
                GameObject go = EventSystem.current.currentSelectedGameObject;
                if (go)
                {
                    if (go.TryGetComponent(out Selectable select))
                        _lastSelected = select;
                }
            }
        }

        private void Reset()
        {
            if (!selectable)
                selectable = GetComponent<Selectable>();
        }

        private void SelectIfControllerChanged(bool value)
        {
            if (_usesController == value)
                return;

            _usesController = value;
            
            Select(_usesController);
        }

        private void Select(bool value)
        {
            if (EventSystem.current)
            {
                if (EventSystem.current.currentSelectedGameObject)
                {
                    if (EventSystem.current.currentSelectedGameObject.TryGetComponent(out TMP_InputField field))
                        return;
                }
            }
            
            if (value)
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
            if (_lastSelected)
            {
                Debug.Log($"Last Selected: {_lastSelected.gameObject.name}, {_lastSelected.isActiveAndEnabled}");
                if (_lastSelected.isActiveAndEnabled)
                {
                    _lastSelected.Select();
                    _lastSelected = null;
                    return;
                }
                _lastSelected = null;
            }
            
            if (selectable)
                selectable.Select();
        }
    }
}
