using Unity.Mathematics;
using UnityEngine;

namespace Beakstorm.Simulation.Collisions.SDF.Shapes
{
    public class SdfBox : AbstractSdfShape
    {
        [SerializeField] private float3 scale;

        private void Update()
        {
            float3 pos = T.position;
            _boundsMin = pos - scale;
            _boundsMax = pos + scale;
            
            float3 x = new float3(scale.x, 0, 0);
            float3 y = new float3(0, scale.y, 0);
            float3 z = new float3(0, 0, scale.z);
            
            _sdfData = new AbstractSdfData(x, y, z, pos, SdfShapeType.Box);
        }
        
        public void OnDrawGizmos()
        {
            Gizmos.color = new(1, 0, 0, 0.5f);
            Gizmos.DrawWireCube(T.position, scale);
        }
    }
}
