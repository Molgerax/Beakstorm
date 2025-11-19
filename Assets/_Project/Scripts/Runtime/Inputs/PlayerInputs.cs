using System;
using System.Collections.Generic;
using Beakstorm.Settings;
using Beakstorm.Utility;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Beakstorm.Inputs
{
    [DefaultExecutionOrder(-100)]
    public class PlayerInputs : MonoBehaviour
    {
        public static PlayerInputs Instance;
        
        #region Serialize Fields

        [SerializeField] [Range(0.0f, 0.5f)] private float inputGrace = 0.2f;

        #endregion

        #region Public Fields

        public InputAction moveAction;
        public InputAction lookAction;
        public InputAction confirmAction;
        public InputAction cancelAction;
        public InputAction shootAction;
        public InputAction emitAction;
        public InputAction switchCameraAction;
        
        public InputAction accelerateAction;
        public InputAction brakeAction;
        public InputAction whistleAction;
        
        public InputAction selectPheromoneAction;

        public Action PauseAction;

        public bool UseButtonsInMenu { get; private set; } = true;

        #endregion

        #region Private Fields

        private PlayerInputActions _inputs;

        private InputBuffered _confirmBuffered;
        private InputBuffered _cancelBuffered;
        private InputBuffered _shootBuffered;

        private static InputDevice _lastActiveDevice;

        #endregion

        #region Properties

        public Vector2 MoveInput => moveAction.ReadValue<Vector2>() * GameplaySettings.Instance.FlightAxisInversion;
        public Vector2 LookInput => lookAction.ReadValue<Vector2>() * GameplaySettings.Instance.LookAxisInversion * GameplaySettings.Instance.MouseSensitivity * 60;
        public Vector2 LookInputRaw => lookAction.ReadValue<Vector2>();

        public bool ConfirmBuffered => _confirmBuffered;
        public bool CancelBuffered => _cancelBuffered;
        public bool ShootBuffered => _shootBuffered;

        public PlayerInputActions InputActions => _inputs;

        public static InputDevice LastActiveDevice => _lastActiveDevice;

        public static event Action ActiveDeviceChangeEvent;

        #endregion


        #region Mono Methods

        private void Awake()
        {
            Instance = this;
            
            _inputs = new PlayerInputActions();
            _inputs.Enable();

            //if (InputSystem.GetDevice(typeof(Gamepad)) != null)
            //    _inputs.bindingMask = InputBinding.MaskByGroup(_inputs.ControllerScheme.bindingGroup);
            
            moveAction = _inputs.Player.Move;
            lookAction = _inputs.Player.Look;
            confirmAction = _inputs.UI.Click;
            cancelAction = _inputs.UI.Cancel;
            shootAction = _inputs.Player.Shoot;
            emitAction = _inputs.Player.Emit;
            switchCameraAction = _inputs.Player.SwitchCameraHandling;
            
            accelerateAction = _inputs.Player.Accelerate;
            brakeAction = _inputs.Player.Brake;
            whistleAction = _inputs.Player.Whistle;

            selectPheromoneAction = _inputs.Player.SwitchPheromone;
            
            _confirmBuffered = new InputBuffered(inputGrace);
            _cancelBuffered = new InputBuffered(inputGrace);
            _shootBuffered = new InputBuffered(inputGrace);
        }

        private void OnEnable()
        {
            confirmAction.AddListener(OnConfirmButton);
            cancelAction.AddListener(OnCancelButton);
            shootAction.AddListener(OnShootButton);
            
            _inputs.Player.Pause.AddListener(OnPauseButton);
            _inputs.UI.Pause.AddListener(OnPauseButton);
            
            _inputs.UI.Navigate.AddListener(OnMoveUI);
            _inputs.UI.Point.AddListener(OnPointUI);
            
            InputSystem.onActionChange += OnActionChange;
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
                Debug.LogFormat("Current Control {0}", activeControl);

                var newDevice = activeControl.device;

                // we detected a change
                if (_lastActiveDevice != newDevice)
                {
                    _lastActiveDevice = newDevice;
                    // fire an event to anyone listening
                    ActiveDeviceChangeEvent?.Invoke();
                }
            }
        }

        public InputAction GetAction(string actionName)
        {
            return _inputs.FindAction(actionName);
        }

        public InputBinding GetBinding(string actionName)
        {
            InputAction action = GetAction(actionName);

            int id = 0;
            InputControlScheme? currentScheme = null;
            for (int i = 0; i < _inputs.controlSchemes.Count; i++)
            {
                var scheme = _inputs.controlSchemes[i];
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
            for (int i = 0; i < _inputs.controlSchemes.Count; i++)
            {
                var scheme = _inputs.controlSchemes[i];
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
            InputActions.Player.Enable();
            InputActions.UI.Disable();

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        
        public void EnableUiInputs()
        {
            InputActions.Player.Disable();
            InputActions.UI.Enable();

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }


        public void EnableNavigation()
        {
            _inputs.UI.Navigate.Enable();
        }
        
        
        public void DisableNavigation()
        {
            _inputs.UI.Navigate.Disable();
        }

        #region Input Callbacks

        public void OnConfirmButton(InputAction.CallbackContext context)
        {
            if (!context.performed) return;
        }

        public void OnCancelButton(InputAction.CallbackContext context)
        {
            if (!context.performed) return;
        }

        public void OnShootButton(InputAction.CallbackContext context)
        {
            if (!context.performed) return;
        }
        
        public void OnPauseButton(InputAction.CallbackContext context)
        {
            if (!context.performed) return;
            
            PauseAction?.Invoke();
        }

        public void OnMoveUI(InputAction.CallbackContext context)
        {
            UseButtonsInMenu = true;
        }
        
        public void OnPointUI(InputAction.CallbackContext context)
        {
            UseButtonsInMenu = false;
        }

        #endregion
    }

    public class InputBuffered
    {
        private readonly float _inputGrace;
        public double timeAtLastInput;
        public bool bufferActive;
        public bool checkedThisFrame;

        public InputBuffered(float inputGrace)
        {
            _inputGrace = inputGrace;
            bufferActive = false;
            checkedThisFrame = false;
        }

        /// <summary>
        /// Activates the input and saves the time of this input
        /// </summary>
        public void TriggerInput()
        {
            timeAtLastInput = Time.unscaledTimeAsDouble;
            bufferActive = true;
            checkedThisFrame = false;
        }

        /// <summary>
        /// Cancels the input.
        /// </summary>
        public void CancelInput()
        {
            bufferActive = false;
        }

        /// <summary>
        /// Checks the input buffer. If the time since input falls within the input grace, returns true.
        /// If it has already been checked, returns false.
        /// </summary>
        /// <returns></returns>
        public bool CheckOnce()
        {
            if (!bufferActive) return false;
            if (checkedThisFrame) return false;

            checkedThisFrame = true;
            return Time.unscaledTimeAsDouble - timeAtLastInput < _inputGrace;
        }

        /// <summary>
        /// Checks the input buffer. If the time since input falls within the input grace, returns true.
        /// Can be called without resetting the buffer.
        /// </summary>
        /// <returns></returns>
        public bool Check()
        {
            //if (!bufferActive) return false;

            return Time.unscaledTimeAsDouble - timeAtLastInput < _inputGrace;
        }


        public static implicit operator bool(InputBuffered inputBuffered)
        {
            return inputBuffered.CheckOnce();
        }
    }
}