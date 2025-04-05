using Unity.Mathematics;
using UnityEngine;

namespace Beakstorm.Simulation.Collisions.SDF.Shapes
{
    public class SdfSphere : AbstractSdfShape
    {
        [SerializeField] private float radius = 1;


        private float AdjustedRadius()
        {
            Vector3 scale = T.lossyScale;
            return radius * Mathf.Max(scale.x, Mathf.Max(scale.y, scale.z));
        }
        
        private void Update()
        {
            float3 pos = T.position;
            float r = AdjustedRadius();
            
            _boundsMin = pos - new float3(r, r, r); 
            _boundsMax = pos + new float3(r, r, r);

            float3 data = new float3(r, 0, 0);
            _sdfData = new AbstractSdfData(pos, data, SdfShapeType.Sphere);
        }

        public void OnDrawGizmos()
        {
            Gizmos.color = new(1, 0, 0, 0.5f);
            Gizmos.DrawWireSphere(T.position, AdjustedRadius());
        }
    }
}
