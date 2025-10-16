using Beakstorm.Gameplay.Player;
using UnityEngine;

namespace Beakstorm.Gameplay.Projectiles.Movement
{
    [CreateAssetMenu(fileName = "SimpleProjectileMoveData", menuName = "Beakstorm/Projectiles/SimpleProjectileMoveData")]
    public class SimpleProjectileMoveData : ProjectileMoveData
    {
        [SerializeField] private float gravity = 9.81f;
        [SerializeField] private float drag = 0.01f;


        public override void Initialize(ProjectileMovementHandler movementHandler, FireInfo fireInfo)
        {
            movementHandler.SetVelocity(fireInfo.LookDirection * fireInfo.Speed);
        }

        public override void Tick(ProjectileMovementHandler movementHandler, float deltaTime)
        {
            movementHandler.Velocity += movementHandler.MomentaryForce * deltaTime;
            
            
            movementHandler.Velocity += Vector3.down * (gravity * deltaTime);

            movementHandler.Velocity = movementHandler.Velocity * (1 - drag * movementHandler.Velocity.magnitude * deltaTime);
            
            movementHandler.MomentaryForce = Vector3.zero;

            movementHandler.transform.position += movementHandler.Velocity * deltaTime;   
        }
    }
}
