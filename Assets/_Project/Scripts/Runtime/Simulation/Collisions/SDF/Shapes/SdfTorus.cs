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
            UpdateSdfData();
        }

        private void OnValidate()
        {
            UpdateSdfData();
        }

        private void UpdateSdfData()
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
            Gizmos.color = new (1, 0, 0, 0.5f);

            if (height < 0.01f)
            {
                TorusGizmo(_sdfData.Translate, _sdfData.YAxis, _sdfData.ZAxis, Radius, Thickness);
                return;
            }

            TorusGizmo(_sdfData.Translate - _sdfData.YAxis * Height, _sdfData.YAxis, _sdfData.ZAxis, Radius, Thickness);
            TorusGizmo(_sdfData.Translate + _sdfData.YAxis * Height, _sdfData.YAxis, _sdfData.ZAxis, Radius, Thickness);
            ConnectionGizmo(_sdfData.Translate, _sdfData.YAxis, _sdfData.ZAxis, Radius, Thickness, Height);
        }

        private void TorusGizmo(float3 center, float3 normal, float3 tangent, float rad, float thick, int res = 8)
        {
            float3 bitangent = math.cross(normal, tangent);
            
            
#if UNITY_EDITOR
            UnityEditor.Handles.color = Gizmos.color;
            UnityEditor.Handles.DrawWireDisc(center, normal, rad + thick);
            UnityEditor.Handles.DrawWireDisc(center, normal, rad - thick);
#endif
            
            for (int i = 0; i < res; i++)
            {
                float t = (float)i / res * Mathf.PI * 2;

                float3 tan = math.cos(t) * bitangent - math.sin(t) * tangent;

                float3 p = center + math.sin(t) * rad * bitangent + math.cos(t) * rad * tangent;
         
#if UNITY_EDITOR
                UnityEditor.Handles.color = Gizmos.color;
                UnityEditor.Handles.DrawWireDisc(p, tan, thick);
#else
                Gizmos.DrawWireSphere(p, thick);
#endif

            }
        }
        
        private void ConnectionGizmo(float3 center, float3 normal, float3 tangent, float rad, float thick, float h, int res = 8)
        {
            float3 bitangent = math.cross(normal, tangent);
            
            for (int i = 0; i < res; i++)
            {
                float t = (float)i / res * Mathf.PI * 2;
                
                float3 offset = math.sin(t) * (rad + thick) * bitangent + math.cos(t) * (rad + thick) * tangent;

                float3 p1 = center + normal * h + offset;
                float3 p2 = center - normal * h + offset;
                
                Gizmos.DrawLine(p1, p2);
            }
        }
    }
}
