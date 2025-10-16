using Beakstorm.Gameplay.Player;
using UnityEngine;

namespace Beakstorm.Gameplay.Projectiles.Movement
{
    public abstract class AbstractProjectileMoveData : ScriptableObject
    {
        public abstract void Initialize(ProjectileMovementHandler movementHandler, FireInfo fireInfo);

        public abstract void Tick(ProjectileMovementHandler movementHandler, float deltaTime);
    }
}
