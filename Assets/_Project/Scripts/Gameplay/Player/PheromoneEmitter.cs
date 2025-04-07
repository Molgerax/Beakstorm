using Beakstorm.Inputs;
using DynaMak.Volumes.FluidSimulation;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Beakstorm.Gameplay.Player
{
    public class PheromoneEmitter : MonoBehaviour
    {
        [SerializeField] private FluidFieldAddBase fluidAdd;

        private PlayerInputs _inputs;

        private void Awake()
        {
            _inputs = PlayerInputs.Instance;

            _inputs.emitAction.performed += OnEmitActionPerformed;
            _inputs.emitAction.canceled += OnEmitActionCancelled;

            fluidAdd.enabled = false;
        }

        private void OnEmitActionPerformed(InputAction.CallbackContext callback)
        {
            fluidAdd.enabled = true;
        }

        private void OnEmitActionCancelled(InputAction.CallbackContext callback)
        {
            fluidAdd.enabled = false;
        }
    }
}