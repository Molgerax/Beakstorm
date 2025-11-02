using Unity.Mathematics;
using UnityEngine;

namespace Beakstorm.Simulation.Collisions.SDF.Shapes
{
    public class SdfTorus : AbstractSdfShape
    {
        [SerializeField, Min(0)] private float radius = 1;
        [SerializeField, Min(0)] private float thickness = 1;
        [SerializeField, Min(0)] private float height = 1;

        protected override SdfShapeType Type() => SdfShapeType.Torus;

        private float3 Position => T.position;
        private float3 _up;
        
        private float Radius => radius * T.localScale.x;
        private float Thickness => thickness * T.localScale.x;
        private float Height => height * T.localScale.x;
        
        private void Update()
        {
            float3 pos = T.position;
            float4x4 m = T.worldToLocalMatrix;
            float3x3 s = new float3x3(m.c0.xyz, m.c1.xyz, m.c2.xyz);
            s = math.transpose(s);
            
            float3 x = math.normalize(s.c0);
            float3 y = math.normalize(s.c1);
            float3 z = math.normalize(s.c2);


            _up = y;

            float3 data = new float3(Radius, Thickness, Height);
            
            _sdfData = new AbstractSdfData(x, y, z, pos, data , GetTypeData());
            
            CalculateBounds(pos);
        }

        private void CalculateBounds(float3 center)
        {
            BoundingBox bounds = new BoundingBox(center, center);

            float3 scaler = new float3(Radius + Thickness, Height + Thickness, Radius + Thickness);
            float3x3 rot = new float3x3(_sdfData.XAxis, _sdfData.YAxis, _sdfData.ZAxis);
            
            for (int i = 0; i < 8; i++)
            {
                float3 p = math.mul(rot, Corners[i] * scaler) + center;
                bounds.GrowToInclude(p, p);
            }

            float distToTop = math.dot(_up, new float3(0, Height + Thickness + Radius * 0.5f, 0));
            float3 up = center + new float3(0, distToTop, 0);
            float3 down = center - new float3(0, distToTop, 0);
            
            bounds.GrowToInclude(up, down);
            
            _boundsMin = bounds.Min;
            _boundsMax = bounds.Max;
        }
        
        private static readonly float3[] Corners = new float3[8]
        {
            new(-1, -1, -1),
            new(+1, -1, -1),
            new(+1, +1, -1),
            new(+1, +1, +1),
            new(-1, +1, +1),
            new(-1, -1, +1),
            new(-1, +1, -1),
            new(+1, -1, +1),
        };

        public void OnDrawGizmos()
        {
            return;
        }
    }
}
