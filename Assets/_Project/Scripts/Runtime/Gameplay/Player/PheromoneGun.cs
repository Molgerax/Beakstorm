using Beakstorm.Gameplay.Player.Weapons;
using Beakstorm.Gameplay.Projectiles;
using Beakstorm.Inputs;
using Beakstorm.Simulation.Particles;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Beakstorm.Gameplay.Player
{
    public class PheromoneGun : MonoBehaviour
    {
        [SerializeField] private SimplePlayerWeapon weaponData;
        [SerializeField] private ProjectileSpawner spawner;

        private PlayerInputs _inputs;

        
        private void OnEnable()
        {
            weaponData.OnMonoEnable();
        }
        
        private void OnDisable()
        {
            weaponData.OnMonoDisable();
        }
        
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
            if (!weaponData)
                return;

            Transform weaponPivot = spawner.transform;
            
            weaponData.Fire(weaponPivot.position, weaponPivot.forward);
        }
    }
}