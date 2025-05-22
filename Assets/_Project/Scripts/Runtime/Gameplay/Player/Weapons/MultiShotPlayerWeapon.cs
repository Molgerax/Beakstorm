using Beakstorm.Gameplay.Projectiles;
using Beakstorm.Simulation.Particles;
using UnityEngine;

namespace Beakstorm.Gameplay.Player.Weapons
{
    [CreateAssetMenu(menuName = "Beakstorm/PlayerWeaponData/MultiShotProjectile", fileName = "MultiShotPlayerWeapon")]
    public class MultiShotPlayerWeapon : SimplePlayerWeapon
    {
        [SerializeField, Range(1, 5)] private int count = 1;
        [SerializeField, Range(1f, 90f)] private float spreadAngle = 25f;
        [SerializeField, Range(0f, 3f)] private float offset = 1f;
        
        public override void Fire(Vector3 position, Vector3 direction)
        {
            for (int i = 0; i < count; i++)
            {
                Vector3 shotDirection = GetShotDirection(i, direction);
                
                base.Fire(position + shotDirection * offset, shotDirection);
                continue;
                
                var projectileInstance = _pool.GetProjectile();
                var projTransform = projectileInstance.transform;
                projTransform.position = position + shotDirection * offset;

                if (projectileInstance.TryGetComponent(out SimpleMovementHandler movementHandler))
                    movementHandler.SetVelocity(shotDirection * initialVelocity);

                if (projectileInstance.TryGetComponent(out PheromoneEmitter emitter))
                    emitter.ResetEmitter();
            }
        }

        private Vector3 GetShotDirection(int index, Vector3 initDirection)
        {
            Vector3 right = Vector3.Cross(Vector3.up, initDirection);
            Vector3 rotationAxis = Vector3.Cross(initDirection, right).normalized;

            float angle = GetAngle(index);
            return Quaternion.AngleAxis(angle, rotationAxis) * initDirection;
        }

        private float GetAngle(int index)
        {
            if (count == 1) 
                return 0;
            
            float t = (float)index / (count - 1) - 0.5f;
            return t * spreadAngle;
        }
    }
}