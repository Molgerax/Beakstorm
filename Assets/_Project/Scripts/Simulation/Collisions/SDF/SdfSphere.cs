using System;
using Unity.Mathematics;
using UnityEngine;

namespace Beakstorm.Simulation.Collisions.SDF
{
    public class SdfSphere : MonoBehaviour, IBounds, ISdfData<float4>
    {
        [SerializeField] private float radius = 1;
        
        private Transform _t;
        public Transform T {
            get {
                if (_t == false) _t = transform;
                return _t;
            }
        }

        private SdfSphereManager _manager;
        
        private float AdjustedRadius()
        {
            Vector3 scale = T.localScale;
            return radius * Mathf.Max(scale.x, Mathf.Max(scale.y, scale.z));
        }

        private float3 _boundsMin;
        private float3 _boundsMax;
        private float4 _sdfData;
        
        public float3 BoundsMin() => _boundsMin;
        public float3 BoundsMax() => _boundsMax;
        public float4 SdfData() => _sdfData;

        private void OnEnable() => SdfSphereManager.Instance.AddSphere(this);
        private void OnDisable() => SdfSphereManager.Instance.RemoveSphere(this);

        private void Update()
        {
            float3 pos = T.position;
            
            float sdfGrow = SdfSphereManager.Instance.SdfGrowBounds;
            float r = AdjustedRadius() + sdfGrow;
            
            _boundsMin = pos - new float3(r, r, r); 
            _boundsMax = pos + new float3(r, r, r);

            _sdfData = new float4(pos, r);
        }

        public void OnDrawGizmos()
        {
            Gizmos.color = new(1, 0, 0, 0.5f);
            Gizmos.DrawWireSphere(T.position, AdjustedRadius());
        }
    }
}
