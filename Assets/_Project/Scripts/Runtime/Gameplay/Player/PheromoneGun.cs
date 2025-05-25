using Beakstorm.Inputs;
using Beakstorm.Simulation.Particles;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Beakstorm.Gameplay.Player
{
    public class PheromoneGun : MonoBehaviour
    {
        private PlayerInputs _inputs;
        
        private void Awake()
        {
            _inputs = PlayerInputs.Instance;

            _inputs.shootAction.performed += OnShootActionPerformed;
        }

        private void Update()
        {
            if (_inputs.whistleAction.IsPressed())
            {
                BoidManager.Instance.RefreshWhistle(transform.position, 1f);
            }
        }

        private void OnShootActionPerformed(InputAction.CallbackContext callback)
        {
            if (!PlayerController.Instance || !PlayerController.Instance.SelectedWeapon)
                return;
            
            PlayerController.Instance.SelectedWeapon.Fire(transform.position, transform.forward);
        }
    }
}