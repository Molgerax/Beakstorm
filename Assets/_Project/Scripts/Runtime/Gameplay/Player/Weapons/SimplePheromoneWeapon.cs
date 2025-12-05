using Beakstorm.Gameplay.Projectiles;
using Beakstorm.Gameplay.Projectiles.Movement;
using Beakstorm.Simulation.Particles;
using UnityEngine;

namespace Beakstorm.Gameplay.Player.Weapons
{
    [CreateAssetMenu(menuName = "Beakstorm/PheromoneWeapon/SimplePheromoneWeapon", fileName = "SimplePlayerWeapon")]
    public class SimplePheromoneWeapon : PheromoneWeapon
    {
        [Header("Data")]
        [SerializeField] protected float initialVelocity = 100;
        
        [SerializeField] protected Projectile projectilePrefab;
        [SerializeField] protected PheromoneBehaviourData behaviourData;
        [SerializeField] protected ProjectileMoveData moveData;
        
        
        public override void FireProjectile(FireInfo fireInfo)
        {
            fireInfo.Speed = initialVelocity;
            
            var projectileInstance = ProjectileManager.Instance.GetProjectile(projectilePrefab);
            var projTransform = projectileInstance.transform;
            projTransform.position = fireInfo.InitialPosition;
            projTransform.rotation = Quaternion.LookRotation(fireInfo.InitialDirection);
            projectileInstance.Spawn();

            if (projectileInstance.TryGetComponent(out SimpleMovementHandler movementHandler))
            {
                movementHandler.SetVelocity(fireInfo.LookDirection * initialVelocity);
            }
            
            if (projectileInstance.TryGetComponent(out ProjectileMovementHandler projectileMovementHandler))
            {
                if (moveData)
                {
                    projectileMovementHandler.SetMovementData(moveData);
                    projectileMovementHandler.Initialize(fireInfo);
                }
            }


            if (projectileInstance.TryGetComponent(out PheromoneEmitter emitter))
            {
                emitter.SetBehaviourData(behaviourData);
                emitter.ResetEmitter();
            }
            
            if (projectileInstance.TryGetComponent(out TimedEvent timedEvent))
            {
                if (behaviourData)
                {
                    timedEvent.Duration = behaviourData.ProjectileLifeTime;
                }
            }
        }
    }
}
