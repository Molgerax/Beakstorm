using DynaMak.Utility;
using UnityEngine;

namespace DynaMak.Properties
{
    [AddComponentMenu(Constants.k_DynaProperty + "Vector2")]
    public class DynaVector2Binder : DynaPropertyBinderBase<Vector2>
    {
        public override void SetProperty(ComputeShader cs, int kernelIndex)
        {
            cs.SetVector(_propertyID, Value);
        }
        
        public override string[] DictKeys => new[] {"half2", "float2", "double2"};
    }
}
