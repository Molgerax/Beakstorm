using UnityEngine;

namespace Beakstorm.Gameplay.Player.Flying
{
    public abstract class FlightControlStrategy : ScriptableObject
    {
        public virtual void Initialize(GliderController glider, float dt) { }
        public virtual void UpdateFlight(GliderController glider, float dt) { }
        public virtual void FixedUpdateFlight(GliderController glider, float dt) { }

        public virtual float Speed01(float speed) => speed;
    }
}
