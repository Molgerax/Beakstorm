using System.Collections.Generic;
using Beakstorm.Mapping.PointEntities;
using Beakstorm.Simulation.Settings;
using Beakstorm.Utility.Extensions;
using TinyGoose.Tremble;
using UnityEngine;

namespace Beakstorm.Mapping.Tremble.MapProcessors
{
    public class ParticleSimulationBorderMapProcessor : MapProcessorBase
    {
        private List<TrembleParticleBorderPoint> _borderPoints;

        public override void ProcessPointEntity(MapBsp mapBsp, BspEntity entity, GameObject point)
        {
            _borderPoints ??= new();
        
            if (!point.TryGetComponent(out TrembleParticleBorderPoint spawn))
                return;

            _borderPoints.Add(spawn);
        }

        public override void OnProcessingCompleted(GameObject root, MapBsp mapBsp)
        {
            ParticleSimulationBorders border = null;
            Bounds bounds = new();
            bool init = false;

            if (_borderPoints == null || _borderPoints.Count < 2)
            {
                return;

                border = root.AddComponent<ParticleSimulationBorders>();
                var colliders = root.GetComponentsInChildren<MeshCollider>();

                foreach (var collider in colliders)
                {
                    if (!init)
                    {
                        bounds = collider.bounds;
                        init = true;
                    }
                    
                    bounds.Encapsulate(collider.bounds);
                }

                bounds.center = bounds.center.With(y: 0);
                bounds.size = bounds.size.With(y: 1024);
                
                border.SetBorders(bounds);
                return;
            }
            border = root.AddComponent<ParticleSimulationBorders>();
            
            foreach (var point in _borderPoints)
            {
                if (!init)
                {
                    bounds = new Bounds(point.transform.position, Vector3.zero);
                    init = true;
                }
                
                bounds.Encapsulate(point.transform.position);
            }
            
            border.SetBorders(bounds);
        }
    }
}