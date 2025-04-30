using DynaMak.Volumes.FluidSimulation;
using UnityEngine;

namespace Beakstorm.Gameplay.Projectiles
{
    public class PheromoneCapsule : MonoBehaviour
    {
        [SerializeField] private FluidFieldAddBase fluidFieldAdd;

        private void OnTriggerEnter(Collider other)
        {
            if (other.TryGetComponent(out FluidFieldAddSphereList list))
            {
                if (fluidFieldAdd is FluidFieldAddSphere sphere)
                {
                    list.AddSphereEmitter(sphere);
                    return;
                }
            }
            
            if (other.TryGetComponent(out FluidField fluidField)) 
                fluidField.AddOperator(fluidFieldAdd);
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.TryGetComponent(out FluidFieldAddSphereList list))
            {
                if (fluidFieldAdd is FluidFieldAddSphere sphere)
                {
                    list.RemoveSphereEmitter(sphere);
                    return;
                }
            }
            
            if (other.TryGetComponent(out FluidField fluidField)) 
                fluidField.RemoveOperator(fluidFieldAdd);
        }
    }
}