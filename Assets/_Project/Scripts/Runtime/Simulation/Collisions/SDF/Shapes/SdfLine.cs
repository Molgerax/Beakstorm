using Unity.Mathematics;
using UnityEngine;

namespace Beakstorm.Simulation.Collisions.SDF.Shapes
{
    public class SdfLine : AbstractSdfShape
    {
        [SerializeField, Min(0)] private float radius = 1;
        [SerializeField, Min(0)] private float height = 1;

        protected override SdfShapeType Type() => SdfShapeType.Line;
        
        private float3 _pointA;
        private float3 _pointB;
        
        private float Radius => radius * T.localScale.x;
        private float Height => Mathf.Max(height * T.localScale.x - Radius * 2, Radius * 2);
        
        private void OnValidate()
        {
            Vector3 scale = T.localScale;
            T.localScale = Vector3.one * Mathf.Max(scale.x, Mathf.Max(scale.y, scale.z));
        }

        private void Update()
        {
            float3 pos = T.position;
            float4x4 m = T.worldToLocalMatrix;
            float3x3 s = new float3x3(m.c0.xyz, m.c1.xyz, m.c2.xyz);
            s = math.transpose(s);
            
            float3 x = math.normalize(s.c0);
            float3 y = math.normalize(s.c1);
            float3 z = math.normalize(s.c2);

            _pointA = pos - z * Height * 0.5f;
            _pointB = pos + z * Height * 0.5f;

            CalculateBounds(_pointA, _pointB);
            
            
            float3 data = new float3(Radius, Height, 0);
            
            _sdfData = new AbstractSdfData(x, y, z, pos, data , GetTypeData());
        }

        private void CalculatePoints()
        {
            float3 pos = T.position;
            float4x4 m = T.worldToLocalMatrix;
            float3x3 s = new float3x3(m.c0.xyz, m.c1.xyz, m.c2.xyz);
            s = math.transpose(s);
            
            float3 x = math.normalize(s.c0);
            float3 y = math.normalize(s.c1);
            float3 z = math.normalize(s.c2);

            x = new float3(math.length(s.c0), math.length(s.c1), math.length(s.c2));
            
            _pointA = pos - z * Height * 0.5f;
            _pointB = pos + z * Height * 0.5f;
        }

        private void CalculateBounds(float3 pointA, float3 pointB)
        {
            float3 r = new float3(Radius, Radius, Radius);
            
            BoundingBox bounds = new BoundingBox(pointA - r, pointA + r);
            bounds.GrowToInclude(pointB - r, pointB + r);
            
            _boundsMin = bounds.Min;
            _boundsMax = bounds.Max;
        }

        public void OnDrawGizmos()
        {
            CalculatePoints();
            
            Gizmos.color = new(1, 0, 0, 0.5f);
            
            Gizmos.DrawWireSphere(_pointA, Radius);
            Gizmos.DrawWireSphere(_pointB, Radius);

            float3 fwd = _pointB - _pointA;
            float3 up = new float3(0, 1, 0);
            float3 right = math.cross(up, fwd);
            if (math.length(right) == 0)
            {
                up = new float3(0.1f, 1, 0);
                right = math.cross(up, fwd);
            }

            up = math.normalize(math.cross(fwd, right));
            right = math.normalize(math.cross(up, fwd));

            for (int i = 0; i < 4; i++)
            {
                float3 displace = up * Radius * Mathf.Sin(Mathf.PI * 0.5f * i) + right * Radius * Mathf.Cos(Mathf.PI * 0.5f * i);
                Gizmos.DrawLine(_pointA + displace, _pointB + displace);
            }
        }
    }
}
