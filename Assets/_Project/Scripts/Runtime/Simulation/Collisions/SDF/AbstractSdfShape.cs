using System;
using Unity.Mathematics;
using UnityEngine;

namespace Beakstorm.Simulation.Collisions.SDF
{
    [ExecuteAlways]
    public abstract class AbstractSdfShape : MonoBehaviour,  IBounds, ISdfData<AbstractSdfData>
    {
        [SerializeField] private SdfMaterialType materialType = SdfMaterialType.None;
        
        protected abstract SdfShapeType Type();

        private Transform _t;
        public Transform T {
            get {
                if (_t == false) _t = transform;
                return _t;
            }
        }

        protected float3 _boundsMin;
        protected float3 _boundsMax;
        protected AbstractSdfData _sdfData;

        public float3 GrowBounds
        {
            get
            {
                float grow = SdfShapeManager.Instance.SdfGrowBounds;
                return new float3(grow, grow, grow);
            }
        }
        
        protected uint GetTypeData()
        {
            uint result = (uint)Type();
            result |= ((uint)materialType) << 4;
            return result;
        }

        
        public float3 BoundsMin() => _boundsMin - GrowBounds;
        public float3 BoundsMax() => _boundsMax + GrowBounds;
        public AbstractSdfData SdfData() => _sdfData;

        protected virtual void OnEnable() => SdfShapeManager.AddShape(this);
        protected virtual void OnDisable() => SdfShapeManager.RemoveShape(this);

        public static bool TestSdf(Vector3 pos, AbstractSdfData data, out float dist, out Vector3 normal)
        {
            dist = Single.PositiveInfinity;
            normal = new(1, 0, 1);
            return false;
        }
    }


    public enum SdfShapeType
    {
        Sphere = 0,
        Box = 1,
        Line = 2,
        Cone = 3,
        Torus = 4,
    }
    
    public enum SdfMaterialType
    {
        None = 0,
        Weak = 1,
        Metal = 2,
        Wood = 3,
        Fabric = 4,
        Glass = 5,
    }
    
    public struct AbstractSdfData
    {
        public float3 XAxis;
        public float3 YAxis;
        public float3 ZAxis;
        public float3 Translate;
        public float3 Data;
        public uint Type;

        public AbstractSdfData(float3 pos, uint type)
        {
            XAxis = new float3(1, 0, 0);
            YAxis = new float3(0, 1, 0);
            ZAxis = new float3(0, 0, 1);
            Translate = pos;
            Data = float3.zero;
            Type = type;
        }
        
        public AbstractSdfData(float3 pos, float3 data, uint type)
        {
            XAxis = new float3(1, 0, 0);
            YAxis = new float3(0, 1, 0);
            ZAxis = new float3(0, 0, 1);
            Translate = pos;
            Data = data;
            Type = type;
        }
        
        public AbstractSdfData(float3 right, float3 up, float3 fwd, float3 pos, uint type)
        {
            XAxis = right;
            YAxis = up;
            ZAxis = fwd;
            Translate = pos;
            Data = float3.zero;
            Type = type;
        }
        
        public AbstractSdfData(float3 right, float3 up, float3 fwd, float3 pos, float3 data, uint type)
        {
            XAxis = right;
            YAxis = up;
            ZAxis = fwd;
            Translate = pos;
            Data = data;
            Type = type;
        }
    }
}
