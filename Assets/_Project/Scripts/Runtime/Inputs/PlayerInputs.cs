using System;
using System.Collections.Generic;
using Beakstorm.Settings;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using static PlayerInputActions;

namespace Beakstorm.Inputs
{
    [DefaultExecutionOrder(-100)]
    public class PlayerInputs : MonoBehaviour, IPlayerActions, IUIActions
    {
        public static PlayerInputs Instance;
        
        #region Serialize Fields

        [SerializeField] [Range(0.0f, 0.5f)] private float inputGrace = 0.2f;

        #endregion

        #region Events

        public event Action<bool> Shoot = delegate {  };
        public event Action<bool> Emit = delegate {  };
        public event Action<bool> Accelerate = delegate {  };
        public event Action<bool> Brake = delegate {  };
        public event Action<bool> SwitchCamera = delegate {  };
        public event Action<bool> LockOn = delegate {  };
        public event Action<bool> FreeLook = delegate {  };
        public event Action<bool> Whistle = delegate {  };
        public event Action<bool> LookAtTarget = delegate {  };
        public event Action<bool> CameraViewPoint = delegate {  };
        
        
        public event Action<bool> Cancel = delegate {  };
        
        
        #endregion
        
        #region Public Fields

        public InputAction selectPheromoneAction;
        public InputAction cycleTabsAction;
        
        public Action PauseAction;

        public bool UseButtonsInMenu => _lastActiveDevice is not Mouse;

        #endregion

        #region Private Fields

        public PlayerInputActions Inputs;

        private static InputDevice _lastActiveDevice;

        private bool _cursorVisible;
        private bool _queueMousePointDisable;
        
        private static InputControlScheme _currentControlScheme;
        
        #endregion

        #region Properties

        public Vector2 MoveInput
        {
            get
            {
                Vector2 value = Inputs.Player.Move.ReadValue<Vector2>();

                if (_lastActiveDevice is Mouse && !Inputs.Player.FreeLook.IsPressed() )
                    value += LookInputRaw * GameplaySettings.Instance.MouseSensitivity;

                value = Vector2.ClampMagnitude(value, 1f);
                
                return value * GameplaySettings.Instance.FlightAxisInversion;
            }
        }

        public Vector2 LookInput
        {
            get
            {
                Vector2 value = Inputs.Player.Look.ReadValue<Vector2>();

                if (_lastActiveDevice is Mouse && !Inputs.Player.FreeLook.IsPressed())
                    value *= 0;

                
                return value * GameplaySettings.Instance.LookAxisInversion * (GameplaySettings.Instance.MouseSensitivity * 60);
            }
        }

        public Vector2 LookInputRaw => Inputs.Player.Look.ReadValue<Vector2>();

        public PlayerInputActions InputActions => Inputs;

        public static InputDevice LastActiveDevice => _lastActiveDevice;

        public static InputControlScheme CurrentControlScheme => _currentControlScheme;
        public static event Action ActiveDeviceChangeEvent;

        #endregion


        #region Mono Methods

        private void Awake()
        {
            Instance = this;
            
            Inputs = new PlayerInputActions();
            Inputs.Player.SetCallbacks(this);
            Inputs.UI.SetCallbacks(this);
            Inputs.Enable();

            //if (InputSystem.GetDevice(typeof(Gamepad)) != null)
            //    _inputs.bindingMask = InputBinding.MaskByGroup(_inputs.ControllerScheme.bindingGroup);

            cycleTabsAction = Inputs.UI.CycleTabs;
            
            selectPheromoneAction = Inputs.Player.SwitchPheromone;
        }

        private void OnEnable()
        {
            InputSystem.onActionChange += OnActionChange;
            
            SetEventSystemInputAsset();
        }

        private void OnDisable()
        {
            InputSystem.onActionChange -= OnActionChange;
        }

        #endregion

        
        private void OnActionChange(object obj, InputActionChange change)
        {
            if (change == InputActionChange.ActionPerformed)
            {
                InputAction inputAction = (InputAction)obj;
                InputControl activeControl = inputAction.activeControl;
                //Debug.LogFormat("Current Control {0}", activeControl);

                var newDevice = activeControl.device;


                if (_lastActiveDevice is Mouse)
                    _queueMousePointDisable = false;
                else if (newDevice is not Mouse)
                    _queueMousePointDisable = false;

                // we detected a change
                if (_lastActiveDevice != newDevice && !_queueMousePointDisable)
                {
                    _lastActiveDevice = newDevice;
                    // fire an event to anyone listening
                    ActiveDeviceChangeEvent?.Invoke();
                    Debug.Log($"Change to device {newDevice.name} for action {inputAction.name}");
                    EnableCursorForMouseOnly();
                }
                _queueMousePointDisable = false;
            }
        }

        private void EnableCursorForMouseOnly()
        {
            if (!_cursorVisible)
            {
                Cursor.visible = false;
                Cursor.lockState = CursorLockMode.Locked;
            }
            else
            {
                Debug.Log($"Enabling Cursor for device: {_lastActiveDevice}");
                
                if (_lastActiveDevice is Mouse)
                {
                    Cursor.visible = true;
                    Cursor.lockState = CursorLockMode.None;
                }
                else
                {
                    Cursor.visible = false;
                    Cursor.lockState = CursorLockMode.Locked;
                }
            }
        }


        private void SetEventSystemInputAsset()
        {
            var eventSystem = EventSystem.current;
            if (eventSystem == null)
            {
                Debug.LogWarning("No EventSystem found in scene.");
                return;
            }

            var uiModule = eventSystem.GetComponent<InputSystemUIInputModule>();
            if (uiModule == null)
            {
                Debug.LogWarning("No InputSystemUIInputModule found in scene.");
                return;
            }

            if (uiModule.actionsAsset != Inputs.asset)
            {
                uiModule.actionsAsset = Inputs.asset;
                Debug.Log("Successfully assigned Inputs.asset to InputSystemUIInputModule.");
            }
        }
        
        public InputAction GetAction(string actionName)
        {
            return Inputs.FindAction(actionName);
        }

        public InputBinding GetBinding(string actionName)
        {
            InputAction action = GetAction(actionName);

            int id = 0;
            InputControlScheme? currentScheme = null;
            for (int i = 0; i < Inputs.controlSchemes.Count; i++)
            {
                var scheme = Inputs.controlSchemes[i];
                if (scheme.SupportsDevice(_lastActiveDevice))
                {
                    currentScheme = scheme;
                    id = i;
                }
            }
            
            if (currentScheme == null)
                return action.bindings[id];
            
            foreach (InputBinding binding in action.bindings)
            {
                if (binding.groups.Contains(currentScheme.Value.name))
                    return binding;
            }

            return action.bindings[id];
        }
        
        public List<InputBinding> GetBindings(string actionName)
        {
            List<InputBinding> bindings = new();
            InputAction action = GetAction(actionName);

            int id = 0;
            InputControlScheme? currentScheme = null;
            for (int i = 0; i < Inputs.controlSchemes.Count; i++)
            {
                var scheme = Inputs.controlSchemes[i];
                if (scheme.SupportsDevice(_lastActiveDevice))
                {
                    currentScheme = scheme;
                    id = i;
                }
            }

            if (currentScheme == null)
                return null;

            foreach (InputBinding binding in action.bindings)
            {
                if (string.IsNullOrEmpty(binding.groups))
                    continue;
                
                if (binding.groups.Contains(currentScheme.Value.name))
                    bindings.Add(binding);
            }
            
            return bindings;
        }
        
        public void EnablePlayerInputs()
        {
            InputActions.UI.Disable();
            InputActions.Player.Enable();

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            _cursorVisible = false;
            
            Debug.Log("Enabled Player Inputs");
            
        }
        
        public void EnableUiInputs()
        {
            InputActions.Player.Disable();
            InputActions.UI.Enable();

            _cursorVisible = true;
            
            Debug.Log("Enabled UI Inputs");

            _queueMousePointDisable = true;
            
            EnableCursorForMouseOnly();
        }


        public void EnableNavigation()
        {
            Inputs.UI.Navigate.Enable();
        }
        
        
        public void DisableNavigation()
        {
            Inputs.UI.Navigate.Disable();
        }

        #region Input Callbacks

        public void OnPauseButton(InputAction.CallbackContext context)
        {
            if (!context.performed) return;
            
            PauseAction?.Invoke();
        }

        #endregion

        private void ButtonInputHandle(InputAction.CallbackContext context, Action<bool> action)
        {
            if (context.performed)
                action.Invoke(true);
            if (context.canceled)
                action.Invoke(false);
        }
        
        #region Player Callbacks

        
        void IPlayerActions.OnMove(InputAction.CallbackContext context)
        {
            
        }

        void IPlayerActions.OnLook(InputAction.CallbackContext context)
        {
            
        }

        void IPlayerActions.OnShoot(InputAction.CallbackContext context)
        {
            ButtonInputHandle(context, Shoot);
        }

        void IPlayerActions.OnEmit(InputAction.CallbackContext context)
        {
            ButtonInputHandle(context, Emit);
        }

        void IPlayerActions.OnSwitchPheromone(InputAction.CallbackContext context)
        {
        }

        void IPlayerActions.OnSwitchCameraHandling(InputAction.CallbackContext context)
        {
            ButtonInputHandle(context, SwitchCamera);
        }

        void IPlayerActions.OnAccelerate(InputAction.CallbackContext context)
        {
            ButtonInputHandle(context, Accelerate);
        }

        void IPlayerActions.OnBrake(InputAction.CallbackContext context)
        {
            ButtonInputHandle(context, Brake);
        }

        void IPlayerActions.OnConfirm(InputAction.CallbackContext context)
        {
        }

        void IPlayerActions.OnCancel(InputAction.CallbackContext context)
        {
        }

        void IPlayerActions.OnWhistle(InputAction.CallbackContext context)
        {
            ButtonInputHandle(context, Whistle);
        }

        void IPlayerActions.OnPause(InputAction.CallbackContext context)
        {
            OnPauseButton(context);
        }

        void IPlayerActions.OnFreeLook(InputAction.CallbackContext context)
        {
            ButtonInputHandle(context, FreeLook);
        }

        void IPlayerActions.OnLockOn(InputAction.CallbackContext context)
        {
            ButtonInputHandle(context, LockOn);
        }

        void IPlayerActions.OnLookAtTarget(InputAction.CallbackContext context)
        {
            ButtonInputHandle(context, LookAtTarget);
        }
        
        void IPlayerActions.OnCameraViewPoint(InputAction.CallbackContext context)
        {
            ButtonInputHandle(context, CameraViewPoint);
        }
        
        #endregion

        #region UI Callbacks

        void IUIActions.OnNavigate(InputAction.CallbackContext context)
        {
        }

        void IUIActions.OnSubmit(InputAction.CallbackContext context)
        {
        }

        void IUIActions.OnCancel(InputAction.CallbackContext context)
        {
            ButtonInputHandle(context, Cancel);
        }

        void IUIActions.OnPause(InputAction.CallbackContext context)
        {
            OnPauseButton(context);
        }

        void IUIActions.OnPoint(InputAction.CallbackContext context)
        {
        }

        void IUIActions.OnClick(InputAction.CallbackContext context)
        {
        }

        void IUIActions.OnRightClick(InputAction.CallbackContext context)
        {
        }

        void IUIActions.OnMiddleClick(InputAction.CallbackContext context)
        {
        }

        void IUIActions.OnScrollWheel(InputAction.CallbackContext context)
        {
        }

        void IUIActions.OnTrackedDevicePosition(InputAction.CallbackContext context)
        {
        }

        void IUIActions.OnTrackedDeviceOrientation(InputAction.CallbackContext context)
        {
        }

        void IUIActions.OnCycleTabs(InputAction.CallbackContext context)
        {
        }

        #endregion
    }
}