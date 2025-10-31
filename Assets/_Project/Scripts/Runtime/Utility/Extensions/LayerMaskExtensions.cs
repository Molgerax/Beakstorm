using UnityEngine;

namespace Beakstorm.Utility.Extensions
{
    public static class LayerMaskExtensions
    {
        public static bool Contains(this LayerMask layerMask, int layer)
        {
            return ((layerMask.value & layer) > 0);
        }
     
        public static bool Contains(this LayerMask layerMask, GameObject gameObject) =>
            layerMask.Contains(gameObject.layer);

        public static bool Contains(this LayerMask layerMask, Component component) =>
            layerMask.Contains(component.gameObject);

        
        public static bool ContainsOverlap(this LayerMask lhs, LayerMask rhs)
        {
            return lhs.And(rhs) > 0;
        }
        
        public static LayerMask And(this LayerMask lhs, LayerMask rhs)
        {
            return lhs.value & rhs.value;
        }

        public static LayerMask Or(this LayerMask lhs, LayerMask rhs)
        {
            return lhs.value | rhs.value;
        }
    }
}
