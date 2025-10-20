using UnityEngine;

namespace Beakstorm.Gameplay.Player.Flying
{
    [CreateAssetMenu(menuName = "Beakstorm/Player/FlightControlStrategy/Realistic")]
    public class RealisticFlightControlStrategy : FlightControlStrategy
    {
        [SerializeField] private float maxThrust = 10f;
        
        public override float Speed01(float speed) => speed;

        public override void Initialize(GliderController glider, float dt)
        {
            glider.Rigidbody.isKinematic = false;
            glider.Rigidbody.useGravity = true;
        }

        public override void FixedUpdateFlight(GliderController glider, float dt)
        {
            
            
            ApplyThrust(glider, dt);
        }


        private void CalculateState(GliderController glider, float dt)
        {
            
        }
        
        private void CalculateAngleOfAttack(GliderController glider)
        {
            
        }
        
        private void CalculateGForce(GliderController glider, float dt)
        {
            
        }
        
        private void ApplyThrust(GliderController glider, float dt)
        {
            float throttle = glider.ThrustInput ? 1 : 0;
            throttle *= dt;
            glider.Rigidbody.AddRelativeForce(Vector3.forward * (maxThrust * throttle));
        }
    }
}
