using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Beakstorm.Utility
{
    public static class ExtensionMethods
    {
        public static void AddListener(this InputAction inputAction, Action<InputAction.CallbackContext> action)
        {
            inputAction.started += action;
            inputAction.performed += action;
            inputAction.canceled += action;
        }
        
        public static void RemoveListener(this InputAction inputAction, Action<InputAction.CallbackContext> action)
        {
            inputAction.started -= action;
            inputAction.performed -= action;
            inputAction.canceled -= action;
        }

        public static Vector3 FlattenY(this Vector3 value)
        {
            return new Vector3(value.x, 0, value.z);
        }
        
        public static float MaxComp(this Vector3 value)
        {
            return Mathf.Max(value.x, Mathf.Max(value.y, value.z));
        }
    }
}