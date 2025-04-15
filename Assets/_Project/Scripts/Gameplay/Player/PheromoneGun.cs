using Beakstorm.Gameplay.Projectiles;
using Beakstorm.Inputs;
using Beakstorm.Simulation.Particles;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Beakstorm.Gameplay.Player
{
    public class PheromoneGun : MonoBehaviour
    {
        [SerializeField] private ProjectileSpawner spawner;
        [SerializeField] private float initialVelocity = 10f;

        private PlayerInputs _inputs;

        private void Awake()
        {
            _inputs = PlayerInputs.Instance;

            _inputs.shootAction.performed += OnShootActionPerformed;
        }

        private void OnShootActionPerformed(InputAction.CallbackContext callback)
        {
            var spawnTransform = spawner.transform;
            Fire(spawnTransform.position, spawnTransform.forward);
        }


        public void Fire(Vector3 position, Vector3 direction)
        {
            if (!spawner)
                return;

            var projectileInstance = spawner.GetProjectile();
            var projTransform = projectileInstance.transform;
            projTransform.position = position;

            if (projectileInstance.TryGetComponent(out SimpleMovementHandler movementHandler))
                movementHandler.SetVelocity(direction * initialVelocity);
            
            if (projectileInstance.TryGetComponent(out PheromoneEmitter emitter))
                emitter.ResetEmitter();
        }
    }
}