using Unity.Mathematics;
using UnityEngine;

namespace Beakstorm.Simulation.Collisions.SDF
{
    public struct BoundingBox : IBounds
    {
        public float3 Min;
        public float3 Max;
        private bool _initialized;

        public float3 BoundsMin() => Min;
        public float3 BoundsMax() => Max;

        public float3 Center => (Min + Max) / 2;
        public float3 Size => Max - Min;
        
        public BoundingBox(float3 min, float3 max)
        {
            Min = min;
            Max = max;
            _initialized = true;
        }

        public BoundingBox(IBounds b)
        {
            Min = b.BoundsMin();
            Max = b.BoundsMax();
            _initialized = true;
        }
        
        public void GrowToInclude(float3 min, float3 max)
        {
            if (!_initialized)
            {
                _initialized = true;
                Min = min;
                Max = max;
            }
            else
            {
                Min.x = min.x < Min.x ? min.x : Min.x;
                Min.y = min.y < Min.y ? min.y : Min.y;
                Min.z = min.z < Min.z ? min.z : Min.z;
                Max.x = max.x > Max.x ? max.x : Max.x;
                Max.y = max.y > Max.y ? max.y : Max.y;
                Max.z = max.z > Max.z ? max.z : Max.z;
            }
        }
    }
    
    public interface IBounds
    {
        float3 BoundsMin();
        float3 BoundsMax();
    }

    public interface ISdfData<T>
    {
        T SdfData();
    }
    
    public static class BoundsExtensionMethods
    {
        public static BoundingBox ToBoundingBox(this IBounds b) => new(b.BoundsMin(), b.BoundsMax());
    }
}
