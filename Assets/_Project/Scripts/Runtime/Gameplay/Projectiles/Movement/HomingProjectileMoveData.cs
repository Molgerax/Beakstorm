using Beakstorm.Gameplay.Player;
using Beakstorm.Utility;
using UnityEngine;

namespace Beakstorm.Gameplay.Projectiles.Movement
{
    [CreateAssetMenu(fileName = "HomingProjectileMoveData", menuName = "Beakstorm/Projectiles/HomingProjectileMoveData")]
    public class HomingProjectileMoveData : ProjectileMoveData
    {
        [SerializeField] private float angularForce = 1;

        public override void Initialize(ProjectileMovementHandler movementHandler, FireInfo fireInfo)
        {
            movementHandler.SetVelocity(fireInfo.InitialDirection * fireInfo.Speed);
            movementHandler.Speed = fireInfo.Speed;

            movementHandler.transform.rotation = Quaternion.LookRotation(fireInfo.InitialDirection);
            
            float distance = Vector3.Distance(fireInfo.InitialPosition, movementHandler.FireInfo.TargetPosition);
            float time = distance / fireInfo.Speed;

            movementHandler.ElapsedTime = 0;
            movementHandler.TargetTime = time;
        }

        public override void Tick(ProjectileMovementHandler movementHandler, float deltaTime)
        {
            var transform = movementHandler.transform;
            Vector3 position = transform.position;

            movementHandler.Velocity = transform.forward * movementHandler.Speed;

            Vector3 heading = movementHandler.FireInfo.TargetPosition - position;

            transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(heading),
                angularForce * deltaTime);
            
            position += movementHandler.Velocity * deltaTime;
            transform.position = position;
        }
    }
}
