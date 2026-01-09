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
            onEmitStop?.Invoke();
        }

        private void OnEnable()
        {
            _inputs.Emit += OnEmitAction;
        }

        private void OnDisable()
        {
            _inputs.Emit -= OnEmitAction;
        }

        private void OnEmitAction(bool performed)
        {
            if (performed)
                onEmitStart?.Invoke();
            else
                onEmitStop?.Invoke();
                
        }
    }
}