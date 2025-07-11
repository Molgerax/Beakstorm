using Unity.Mathematics;
using UnityEngine;

namespace Beakstorm.Simulation.Collisions.SDF.Shapes
{
    public class SdfBox : AbstractSdfShape
    {
        [SerializeField] private float3 scale = new float3(1, 1, 1);

        protected override SdfShapeType Type() => SdfShapeType.Box;
        
        private float3 AdjustedScale()
        {
            float4x4 m = T.localToWorldMatrix;
            float3 s = new float3(math.length(m.c0.xyz), math.length(m.c1.xyz), math.length(m.c2.xyz));
            return s * scale;
        }

        private void Update()
        {
            float3 pos = T.position;
            float4x4 m = T.worldToLocalMatrix;
            float3x3 s = new float3x3(m.c0.xyz, m.c1.xyz, m.c2.xyz);
            
            s = math.transpose(s);

            CalculateBounds(pos);


            float3 x = math.normalize(s.c0);
            float3 y = math.normalize(s.c1);
            float3 z = math.normalize(s.c2);

            _sdfData = new AbstractSdfData(x, y, z, pos, AdjustedScale() * 0.5f, GetTypeData());
        }

        private void CalculateBounds(float3 center)
        {
            float4x4 m = T.localToWorldMatrix;
            float3x3 rot = new float3x3(m.c0.xyz, m.c1.xyz, m.c2.xyz);

            float3 adjustedScale = AdjustedScale() * 0.5f;
            BoundingBox bounds = new BoundingBox(center, center);

            for (int i = 0; i < 8; i++)
            {
                float3 p = math.mul(rot, Corners[i] * scale * 0.5f) + center;
                bounds.GrowToInclude(p, p);
            }

            _boundsMin = bounds.Min;
            _boundsMax = bounds.Max;
        }

        public void OnDrawGizmos()
        {
            Gizmos.color = new(1, 0, 0, 0.5f);
            Gizmos.matrix = T.localToWorldMatrix;
            Gizmos.DrawWireCube(Vector3.zero, scale);
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
        
        public static float3 GetLargest(float3 value)
        {
            float3 firstTest = math.step(value.yzx, value);
            float3 secondTest = math.step(value.zxy, value);
            return firstTest * secondTest;
        }
        
        public new static bool TestSdf(float3 pos, AbstractSdfData data, out float dist, out Vector3 normal)
        {
            normal = Vector3.up;
            
            float3x3 rot = new float3x3(data.XAxis, data.YAxis, data.ZAxis);
    
            float3 q = math.mul(rot, pos - data.Translate);
            float3 diff = math.abs(q) - data.Data;
    
            dist = math.length(math.max(diff, 0)) + math.min(math.max(diff.x, math.max(diff.y, diff.z)), 0);
    
            float3 norm = (GetLargest(diff) + math.max(diff, 0)) * math.sign(q);
            if (math.dot(norm, norm) == 0)
                norm = new float3(0,1,0);
            normal = math.mul(math.transpose(rot), math.normalize(norm));

            return dist <= 0;
        }
    }
}
