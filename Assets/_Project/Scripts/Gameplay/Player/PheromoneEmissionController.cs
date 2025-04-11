using Beakstorm.Inputs;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

namespace Beakstorm.Gameplay.Player
{
    public class PheromoneEmissionController : MonoBehaviour
    {
        [SerializeField] private UnityEvent onEmitStart;
        [SerializeField] private UnityEvent onEmitStop;

        private PlayerInputs _inputs;

        private void Awake()
        {
            _inputs = PlayerInputs.Instance;

            _inputs.emitAction.performed += OnEmitActionPerformed;
            _inputs.emitAction.canceled += OnEmitActionCancelled;

            onEmitStop?.Invoke();
        }

        private void OnEmitActionPerformed(InputAction.CallbackContext callback)
        {
            onEmitStart?.Invoke();
        }

        private void OnEmitActionCancelled(InputAction.CallbackContext callback)
        {
            onEmitStop?.Invoke();
        }
    }
}