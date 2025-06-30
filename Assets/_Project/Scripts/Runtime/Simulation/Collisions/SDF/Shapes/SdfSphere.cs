using Unity.Mathematics;
using UnityEngine;

namespace Beakstorm.Simulation.Collisions.SDF.Shapes
{
    public class SdfSphere : AbstractSdfShape
    {
        [SerializeField, Min(0)] private float radius = 1;

        protected override SdfShapeType Type() => SdfShapeType.Sphere;

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
            _sdfData = new AbstractSdfData(pos, data, GetTypeData());
        }

        public void OnDrawGizmos()
        {
            Gizmos.color = new(1, 0, 0, 0.5f);
            Gizmos.DrawWireSphere(T.position, AdjustedRadius());
        }
        
        
        public new static bool TestSdf(float3 pos, AbstractSdfData data, out float dist, out Vector3 normal)
        {
            dist = -data.Data.x;
            normal = Vector3.up;

            float3 diff = pos - data.Translate;
            if (math.dot(diff, diff) == 0)
                return true;
    
            float len = math.length(diff);
            dist = len - data.Data.x;
    
            normal = diff / len;
            return dist <= 0;
        }
    }
}
